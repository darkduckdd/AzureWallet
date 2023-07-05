using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Newtonsoft.Json;
using PlayFab;
using System;
using System.IO;
using System.Threading.Tasks;
using WorkWithDB.Data;
using WorkWithDB.Database;

namespace WorkWithDB.Token
{
    public static class SendTokenToIngame
    {
        [FunctionName("SendTokenToIngame")]
        public static async Task<IActionResult> Run(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            var ethProviderAddress = ContractData.EthProviderAddress;



            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);


            string playfabID = data?.PlayfabId;

            string tokenType = data?.TokenType;

            string entityToken = data?.EntityToken;
            string entityID = data?.EntityId;
            string entityType = data?.EntityType;
            string titleID = data?.TitleId;
            tokenType = tokenType?.ToUpper();
            string hash = data?.Hash;


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

            string tokenID = "";
            if (tokenType == TokenType.IGT)
            {
                tokenID = TokenType.IG;
            }
            else
            {
                tokenID = TokenType.CO;
            }

            var web3 = new Web3(ethProviderAddress);

            var transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
            int count = 0;
            int delayTime = 5000;
            if (transactionReceipt == null)
            {
                while (count <= 5)
                {
                    await Task.Delay(delayTime);
                    transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
                    if (transactionReceipt != null)
                    {
                        break;
                    }
                    count++;
                }
            }
            if (transactionReceipt != null && transactionReceipt.Status.Value == 1)
            {
                var amountDecimal = Blockchain.GetValueFromReceipt(transactionReceipt);
                int amount = Convert.ToInt32(amountDecimal);
                var saveResult = await DatabaseManager.SaveTokenHashInDB(titleID, transactionReceipt, true, playfabID, Direction.ToIngame, tokenType);
                if (saveResult)
                {
                    var addToken = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                    {
                        Amount = amount,
                        VirtualCurrency = tokenID,
                        AuthenticationContext = authContext,
                        PlayFabId = playfabID
                    });
                }
                return new OkObjectResult($"Save transaction Hash: {hash}, save status: {saveResult}");
            }


            return new BadRequestObjectResult($"Ошибка");
        }

    }
}
