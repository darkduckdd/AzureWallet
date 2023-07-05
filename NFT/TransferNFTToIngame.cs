using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC1155.ContractDefinition;
using Nethereum.Web3;
using Newtonsoft.Json;
using PlayFab;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WorkWithDB.Data;
using WorkWithDB.Database;

namespace WorkWithDB.NFT
{
    public static class TransferNFTToIngame
    {
        [FunctionName("TransferNFTToIngame")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var ethProviderAddress = ContractData.NFTEthProvierAddress;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);


            string playfabID = data?.PlayfabId;
            string entityToken = data?.EntityToken;
            string entityID = data?.EntityId;
            string entityType = data?.EntityType;
            string titleID = data?.TitleId;
            string hash = data?.Hash;

            string ownerAddress = ContractData.OwnerAddress;


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
                var eventDTO = transactionReceipt.DecodeAllEvents<TransferSingleEventDTO>().FirstOrDefault();
                if (eventDTO != null)
                {
                    BigInteger tokenId = eventDTO.Event.Id;
                    string toAddress = eventDTO.Event.To;
                    var amount = eventDTO.Event.Value;
                    if (toAddress == ownerAddress)
                    {
                        var skinId = await DatabaseManager.GetSkinID(titleID, tokenId);

                        var saveResult = await DatabaseManager.SaveNFTHashInDB(titleID, transactionReceipt, playfabID, Direction.ToWallet, skinId);
                        if (saveResult)
                        {

                            List<string> itemIds = new List<string>();
                            for (int i = 0; i < amount; i++)
                            {
                                itemIds.Add(skinId);
                            }
                            var grant = PlayFabServerAPI.GrantItemsToUserAsync(new PlayFab.ServerModels.GrantItemsToUserRequest
                            {
                                AuthenticationContext = authContext,
                                CatalogVersion = "Items",
                                PlayFabId = playfabID,
                                ItemIds = itemIds
                            });
                            return new OkObjectResult($"granted success: {grant.Result}");

                        }
                        else
                        {
                            return new OkObjectResult($"hash error");

                        }
                    }

                }
            }


            return new OkObjectResult($"Îøèáêà");
        }
    }
}
