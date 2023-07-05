using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json;
using PlayFab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WorkWithDB.Data;
using WorkWithDB.Database;

namespace WorkWithDB.Token
{
    public static class SendTokentoCryptoWallet
    {
        private static string SessionKey;
        private static string TitleName;
        private static string TokenType;
        private static int Amount;
        private static string ToAddress;
        private static string TokenID;

        public static PlayFabAuthenticationContext AuthContext;
        public static string PlayFabID;
        public static string TitleID;

        [FunctionName("SendTokentoCryptoWallet")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (requestBody == null)
            {
                return new OkObjectResult("body is null");
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);


            string playfabID = data?.PlayfabId;

            string tokenType = data?.TokenType;
            tokenType = tokenType?.ToUpper();
            string amountString = data?.Amount;
            string toAddress = data?.ToAddress;

            string entityToken = data?.EntityToken;
            string entityID = data?.EntityId;
            string entityType = data?.EntityType;
            string titleID = data?.TitleId;


            string DeveloperKey = await DatabaseManager.GetDeveloperSecretKey(titleID);

            if (DeveloperKey == null)
            {
                return new OkObjectResult("Key not found");
            }

            PlayFabSettings.staticSettings.DeveloperSecretKey = DeveloperKey;
            PlayFabSettings.staticSettings.TitleId = titleID;

            var authContext = new PlayFabAuthenticationContext
            {
                EntityToken = entityToken,
                EntityType = entityType,
                PlayFabId = playfabID,
                EntityId = entityID
            };

            AuthContext = authContext;
            PlayFabID = playfabID;
            TitleID = titleID;


            int amount = 0;
            if (!int.TryParse(amountString, out amount))
            {
                return new BadRequestObjectResult("amount is invalid ");
            }
            if (amount <= 0)
            {
                return new BadRequestObjectResult("amount is invalid");
            }


            string tokenID = null;
            if (tokenType == Data.TokenType.IGT)
            {
                tokenID = Data.TokenType.IG;
            }
            else if (tokenType == Data.TokenType.IGC)
            {
                tokenID = Data.TokenType.CO;
            }

            if (tokenID == null)
            {
                return new BadRequestObjectResult("tokenType invalid");
            }

            Amount = amount;



            TokenType = tokenType;
            ToAddress = toAddress;
            TokenID = tokenID;

            var userInventory = await PlayFabServerAPI.GetUserInventoryAsync(new PlayFab.ServerModels.GetUserInventoryRequest
            {
                PlayFabId = playfabID,
                AuthenticationContext = authContext,
            });


            var isEnought = HaveWK(userInventory.Result.VirtualCurrency);
            var tokenAmount = GetTokenAmount(userInventory.Result.VirtualCurrency, tokenID);

            if (isEnought && tokenAmount >= amount)
            {
                var substractWK = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new PlayFab.ServerModels.SubtractUserVirtualCurrencyRequest
                {
                    AuthenticationContext = authContext,
                    Amount = 1,
                    PlayFabId = playfabID,
                    VirtualCurrency = Data.TokenType.WK
                });

                var substractToken = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new PlayFab.ServerModels.SubtractUserVirtualCurrencyRequest
                {
                    AuthenticationContext = authContext,
                    Amount = amount,
                    PlayFabId = playfabID,
                    VirtualCurrency = tokenID
                });
                bool isSubstractWK = false, isSubstractToken = false;
                if (substractWK.Result != null)
                {
                    isSubstractWK = true;

                }

                if (substractToken.Result != null)
                {
                    isSubstractToken = true;
                }

                if (isSubstractToken && isSubstractWK)
                {

                    return await SendTokenToCryptoWallet(amount, toAddress, titleID, playfabID);
                }
                else
                {
                    if (!isSubstractWK)
                    {
                        await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                        {
                            AuthenticationContext = authContext,
                            Amount = 1,
                            PlayFabId = playfabID,
                            VirtualCurrency = Data.TokenType.WK
                        });
                    }
                    if (!isSubstractToken)
                    {
                        await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                        {
                            AuthenticationContext = authContext,
                            Amount = amount,
                            PlayFabId = playfabID,
                            VirtualCurrency = tokenID
                        });

                    }
                    return new OkObjectResult($"WK: {substractWK}, token: {substractToken} ");
                }

            }
            return new BadRequestObjectResult($"Not enought token or wk");
        }

        private static async Task<IActionResult> SendTokenToCryptoWallet(int amount, string toAddress, string titleID, string playfabID)
        {
            string contractAddress;
            var ethProviderAddress = ContractData.EthProviderAddress;
            string ABI;
            string privateKey = ContractData.PrivateKey;
            string fromAddress = ContractData.OwnerAddress;

            if (TokenType == Data.TokenType.IGT)
            {
                contractAddress = ContractData.IGTContractAddress;
                ABI = ContractData.IGTAbi;
            }
            else
            {
                contractAddress = ContractData.IGCContractAddress;
                ABI = ContractData.IGCAbi;
            }
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            var web3 = new Web3(account, ethProviderAddress);
            var contract = web3.Eth.GetContract(ABI, contractAddress);
            var gasPrice = new HexBigInteger(Web3.Convert.ToWei(5, UnitConversion.EthUnit.Gwei));
            var transferFunction = contract.GetFunction("transfer");


            var balance = await Blockchain.GetTokenBalance(TokenType);
            var amountInWei = new HexBigInteger(Web3.Convert.ToWei(amount, UnitConversion.EthUnit.Ether)).Value;
            if (balance < amountInWei)
            {
                return new BadRequestObjectResult($"insufficient funds");
            }

            var transactionInput = new TransactionInput
            {
                From = fromAddress,
                To = contractAddress,
                Gas = ContractData.Gas,
                GasPrice = gasPrice,
                Value = ContractData.Value,
                ChainId = ContractData.ChainID,
                Data = transferFunction.GetData(toAddress, amountInWei)
            };
            var signedTransaction = await account.TransactionManager.SignTransactionAsync(transactionInput);
            try
            {
                var transactionHash = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedTransaction);

                var transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                var count = 0;
                var delayTime = 5000;
                if (transactionReceipt == null)
                {
                    while (count <= 5)
                    {
                        await Task.Delay(delayTime);
                        transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                        if (transactionReceipt != null)
                        {
                            break;
                        }
                        count++;
                    }
                }

                if (transactionReceipt != null && transactionReceipt.Status.Value == 1)
                {
                    var saveResult = await DatabaseManager.SaveTokenHashInDB(titleID, transactionReceipt, true, playfabID, Direction.ToWallet, TokenType);

                    return new OkObjectResult($"{transactionHash}");
                }
            }
            catch (Exception ex)
            {
                await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                {
                    AuthenticationContext = AuthContext,
                    Amount = 1,
                    PlayFabId = playfabID,
                    VirtualCurrency = Data.TokenType.WK
                });

                await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                {
                    AuthenticationContext = AuthContext,
                    Amount = amount,
                    PlayFabId = playfabID,
                    VirtualCurrency = TokenID
                });

                return new BadRequestObjectResult($"Ошибка: {ex}");

            }
            return new BadRequestObjectResult($"Ошибка");
        }


        private static bool HaveWK(Dictionary<string, int> virtualCurrency)
        {
            if (virtualCurrency != null)
            {
                foreach (var currency in virtualCurrency)
                {
                    if (currency.Key == Data.TokenType.WK)
                    {
                        if (currency.Value > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static int GetTokenAmount(Dictionary<string, int> virtualCurrency, string tokenID)
        {
            if (virtualCurrency != null)
            {


                foreach (var currency in virtualCurrency)
                {
                    if (currency.Key == tokenID)
                    {
                        if (currency.Value > 0)
                        {
                            return currency.Value;
                        }
                    }
                }
            }
            return 0;
        }
    }

}
