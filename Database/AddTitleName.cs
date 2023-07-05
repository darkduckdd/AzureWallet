using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WorkWithDB.Database
{
    public static class AddTitleName
    {
        [FunctionName("AddTitleName")]
        public static async Task<IActionResult> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string containerName = DatabaseData.TitleNameContainer;
            string titleID = data?.titleID;
            string titleName = data?.titleName;

            string URI = DatabaseData.URI;
            string privateKey = DatabaseData.PrivateKey;
            string databaseName = DatabaseData.WalletDatabaseName;


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


            bool isExistHash = await CheckIfRecordExistsAsync(titleID, container);
            if (isExistHash)
            {
                return new BadRequestObjectResult("Record exists");
            }
            var document = new
            {
                id = DateTime.UtcNow.Ticks.ToString() + Guid.NewGuid().ToString(),
                TitleID = titleID,
                TitleName = titleName

            };
            var response = await container.CreateItemAsync(document);

            return new OkObjectResult(response.Resource);
        }

        private static async Task<bool> CheckIfRecordExistsAsync(string titleID, Container container)
        {
            string query = $"SELECT c.id FROM c WHERE c.TitleID = '{titleID}'";

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
    }
}
