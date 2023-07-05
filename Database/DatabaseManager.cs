using Microsoft.Azure.Cosmos;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC1155.ContractDefinition;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;

namespace WorkWithDB.Database
{
    public static class DatabaseManager
    {
        public static async Task<bool> SaveTokenHashInDB(string titleID, TransactionReceipt receipt, bool accreud, string playfabID, string direction, string tokenType)
        {
            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.WalletDatabaseName;
            var cosmosClient = new CosmosClient(URI, privateKey);
            var database = cosmosClient.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(titleID, "/id");
            Container container = null;
            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                container = cosmosClient.GetContainer(databaseName, titleID);
            }
            else if (containerResponse.StatusCode == HttpStatusCode.OK)
            {
                container = cosmosClient.GetContainer(databaseName, titleID);
            }

            if (container == null)
            {
                int count = 0;
                int timeDelay = 5000;
                while (count < 5)
                {
                    await Task.Delay(timeDelay);
                    container = cosmosClient.GetContainer(databaseName, titleID);
                    if (container != null)
                    {
                        break;
                    }
                    count++;
                }
            }


            bool isExistHash = await CheckIfRecordHashExistsAsync(receipt, container);
            if (isExistHash)
            {
                return false;
            }
            DateTime now = DateTime.UtcNow;
            string formattedDate = now.ToString("dd-MM-yyyy HH:mm:ss");

            var eventDTO = receipt.DecodeAllEvents<TransferEventDTO>().ToList();
            BigInteger transferTokenAmountInWei = 0;
            decimal transferTokenAmount = 0;
            string address = "";
            string logIndex = "";

            foreach (var transferLog in eventDTO)
            {
                transferTokenAmountInWei = transferLog.Event.Value;
                transferTokenAmount = Web3.Convert.FromWei(transferLog.Event.Value, 18);
            }
            foreach (var log in receipt.Logs)
            {
                address = log["address"].ToString();
                logIndex = log["logIndex"].ToString();
            }

            var document = new TokenCollectionData
            {
                id = DateTime.UtcNow.Ticks.ToString() + Guid.NewGuid().ToString(),
                TransactionHash = receipt.TransactionHash,
                TransactionIndex = receipt.TransactionIndex,
                From = receipt.From,
                To = receipt.To,
                Address = address,
                Value = transferTokenAmount,
                ValueInWei = transferTokenAmountInWei,
                BlockHash = receipt.BlockHash,
                BlockNumber = receipt.BlockNumber,
                Accrued = accreud,
                CreatedAt = formattedDate,
                UpdatedAt = formattedDate,
                LogIndex = logIndex,
                Direction = direction,
                PlayfabID = playfabID,
                TokenType = tokenType
            };


            var response = await container.CreateItemAsync(document);
            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> SaveNFTHashInDB(string titleID, TransactionReceipt receipt, string playfabID, string direction, string skinID)
        {
            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.NFTDatabaseName;
            var cosmosClient = new CosmosClient(URI, privateKey);
            var database = cosmosClient.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(titleID, "/id");
            Container container = null;
            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                container = cosmosClient.GetContainer(databaseName, titleID);
            }
            else if (containerResponse.StatusCode == HttpStatusCode.OK)
            {
                container = cosmosClient.GetContainer(databaseName, titleID);
            }

            if (container == null)
            {
                await Task.Delay(10000);
                container = cosmosClient.GetContainer(databaseName, titleID);
            }


            bool isExistHash = await CheckIfRecordHashExistsAsync(receipt, container);
            if (isExistHash)
            {
                return false;
            }
            DateTime now = DateTime.UtcNow;
            string formattedDate = now.ToString("dd-MM-yyyy HH:mm:ss");

            string address = "";
            string logIndex = "";
            foreach (var log in receipt.Logs)
            {
                address = log["address"].ToString();
                logIndex = log["logIndex"].ToString();
            }
            var eventDTO = receipt.DecodeAllEvents<TransferSingleEventDTO>().FirstOrDefault();
            if (eventDTO != null)
            {
                var document = new NFTCollectionData
                {
                    id = DateTime.UtcNow.Ticks.ToString() + Guid.NewGuid().ToString(),
                    TransactionHash = receipt.TransactionHash,
                    TransactionIndex = receipt.TransactionIndex,
                    From = eventDTO.Event.From,
                    To = eventDTO.Event.To,
                    Address = address,
                    Value = (int)eventDTO.Event.Value,
                    BlockHash = receipt.BlockHash,
                    BlockNumber = receipt.BlockNumber,
                    CreatedAt = formattedDate,
                    UpdatedAt = formattedDate,
                    LogIndex = logIndex,
                    Direction = direction,
                    PlayfabID = playfabID,
                    NFTID = eventDTO.Event.Id.ToString(),
                    SkinId = skinID
                };


                var response = await container.CreateItemAsync(document);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private static async Task<bool> CheckIfRecordHashExistsAsync(TransactionReceipt transactionReceipt, Container container)
        {
            string query = $"SELECT c.id FROM c WHERE c.TransactionHash = '{transactionReceipt.TransactionHash}'";

            QueryDefinition queryDefinition = new QueryDefinition(query);

            FeedIterator<dynamic> queryResultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (var result in currentResultSet)
                {
                    // Если ответ не пустой, значит запись с таким именем существует в базе
                    if (result != null)
                    {
                        return true;
                    }
                }
            }

            // Если прошли все результаты и не нашли запись, значит её нет в базе
            return false;
        }

        public static async Task<string> GetDeveloperSecretKey(string titleID)
        {
            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.SecretKeysDB;
            string containerName = DatabaseData.SecretKeysContainer;
            // string partitionKey = "/id";


            var cosmosClient = new CosmosClient(URI, privateKey);
            var database = cosmosClient.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, "/id");
            Container container = null;
            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }
            else if (containerResponse.StatusCode == HttpStatusCode.OK)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }

            if (container == null)
            {
                await Task.Delay(10000);
                container = cosmosClient.GetContainer(databaseName, containerName);
            }


            try
            {
                ItemResponse<JObject> response = await container.ReadItemAsync<JObject>(titleID, new PartitionKey(titleID));
                JObject item = response.Resource;
                return item["SecretKey"].ToString();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public static async Task<string> GetTokenID(string titleID, string itemID)
        {
            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.NFT_Token_DB;
            string containerName = titleID;


            var cosmosClient = new CosmosClient(URI, privateKey);
            var database = cosmosClient.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, "/id");
            Container container = null;
            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }
            else if (containerResponse.StatusCode == HttpStatusCode.OK)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }

            if (container == null)
            {
                await Task.Delay(10000);
                container = cosmosClient.GetContainer(databaseName, containerName);
            }


            try
            {

                ItemResponse<JObject> response = await container.ReadItemAsync<JObject>(itemID, new PartitionKey(itemID));
                JObject item = response.Resource;

                return item["TokenID"].ToString();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public static async Task<string> GetSkinID(string titleID, BigInteger tokenID)
        {
            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.NFT_Token_DB;
            string containerName = titleID;


            var cosmosClient = new CosmosClient(URI, privateKey);
            var database = cosmosClient.GetDatabase(databaseName);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(containerName, "/id");
            Container container = null;
            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }
            else if (containerResponse.StatusCode == HttpStatusCode.OK)
            {
                container = cosmosClient.GetContainer(databaseName, containerName);
            }

            if (container == null)
            {
                await Task.Delay(10000);
                container = cosmosClient.GetContainer(databaseName, containerName);
            }


            try
            {

                string query = $"SELECT c.id FROM c WHERE c.TokenID = {tokenID}";

                QueryDefinition queryDefinition = new QueryDefinition(query);

                FeedIterator<JObject> queryResultIterator = container.GetItemQueryIterator<JObject>(queryDefinition);

                while (queryResultIterator.HasMoreResults)
                {
                    var response = await queryResultIterator.ReadNextAsync();
                    if (response.Count > 0)
                    {
                        var result = response.First();
                        return result["id"].ToString();
                    }

                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            return null;
        }
    }


}
