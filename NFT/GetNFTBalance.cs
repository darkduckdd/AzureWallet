using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Newtonsoft.Json;
using PlayFab;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using WorkWithDB.Data;
using WorkWithDB.Database;

namespace WorkWithDB.NFT
{
    public static class GetNFTBalance
    {
        [FunctionName("GetNFTBalance")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
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
            string address = data?.Address;
            string nftId = data?.NFTId;


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

            string nftContractAddress = "0x24CB6623b4a57535cbB4134B48dd09c1bd61cC02";
            string abi = "[\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"constructor\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"approved\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"ApprovalForAll\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"amount\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"mint\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"amounts\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"mintBatch\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"previousOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"newOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"OwnershipTransferred\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"name\": \"renounceOwnership\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"amounts\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"safeBatchTransferFrom\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"amount\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"safeTransferFrom\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"approved\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"setApprovalForAll\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"values\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"TransferBatch\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"newOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"transferOwnership\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"value\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"TransferSingle\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"string\",\r\n\t\t\t\t\"name\": \"value\",\r\n\t\t\t\t\"type\": \"string\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"URI\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"balanceOf\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address[]\",\r\n\t\t\t\t\"name\": \"accounts\",\r\n\t\t\t\t\"type\": \"address[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"balanceOfBatch\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"isApprovedForAll\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"name\": \"owner\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes4\",\r\n\t\t\t\t\"name\": \"interfaceId\",\r\n\t\t\t\t\"type\": \"bytes4\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"supportsInterface\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"uri\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"string\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"string\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t}\r\n]";


            var web3 = new Web3(ethProviderAddress);
            var nftContract = web3.Eth.GetContract(abi, nftContractAddress);
            var balanceOfBatchFunction = nftContract.GetFunction("balanceOf");
            var balance = await balanceOfBatchFunction.CallAsync<BigInteger>(address, nftId);
            return new OkObjectResult(balance);

        }
    }
}
