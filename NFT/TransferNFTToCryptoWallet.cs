using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC1155.ContractDefinition;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using WorkWithDB.Data;
using WorkWithDB.Database;

namespace WorkWithDB.NFT
{

    public static class TransferNFTToCryptoWallet
    {
        private static string SessionKey;
        private static string PlayfabID;
        private static string TitleName;
        private static string TokenType;
        private static int Amount;
        private static string ToAddress;
        private static string TokenID;

        public static PlayFabAuthenticationContext AuthContext;
        public static string PlayFabID;
        public static string TitleID;

        [FunctionName("TransferNFTToCryptoWallet")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (requestBody == null)
            {
                return new OkObjectResult("body is null");
            }

            string ethProviderAddress = ContractData.NFTEthProvierAddress;
            string privateKey = ContractData.PrivateKey;
            string address = "0x80cd6a56f57f2AF03e67cA0feB4509fbc16F296C"; // ContractData.OwnerAddress;
            string nftContractAddress = "0x24CB6623b4a57535cbB4134B48dd09c1bd61cC02";
            string abi = "[\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"constructor\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"approved\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"ApprovalForAll\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"amount\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"mint\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"amounts\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"mintBatch\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"previousOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"newOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"OwnershipTransferred\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"name\": \"renounceOwnership\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"amounts\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"safeBatchTransferFrom\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"amount\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes\",\r\n\t\t\t\t\"name\": \"data\",\r\n\t\t\t\t\"type\": \"bytes\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"safeTransferFrom\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"approved\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"setApprovalForAll\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"values\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"TransferBatch\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"newOwner\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"transferOwnership\",\r\n\t\t\"outputs\": [],\r\n\t\t\"stateMutability\": \"nonpayable\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"from\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"to\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"value\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"TransferSingle\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"anonymous\": false,\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": false,\r\n\t\t\t\t\"internalType\": \"string\",\r\n\t\t\t\t\"name\": \"value\",\r\n\t\t\t\t\"type\": \"string\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"indexed\": true,\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"URI\",\r\n\t\t\"type\": \"event\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"id\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"balanceOf\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address[]\",\r\n\t\t\t\t\"name\": \"accounts\",\r\n\t\t\t\t\"type\": \"address[]\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"ids\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"balanceOfBatch\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256[]\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256[]\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"account\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t},\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"operator\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"isApprovedForAll\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [],\r\n\t\t\"name\": \"owner\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"address\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"address\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bytes4\",\r\n\t\t\t\t\"name\": \"interfaceId\",\r\n\t\t\t\t\"type\": \"bytes4\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"supportsInterface\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"bool\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"bool\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t},\r\n\t{\r\n\t\t\"inputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"uint256\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"uint256\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"name\": \"uri\",\r\n\t\t\"outputs\": [\r\n\t\t\t{\r\n\t\t\t\t\"internalType\": \"string\",\r\n\t\t\t\t\"name\": \"\",\r\n\t\t\t\t\"type\": \"string\"\r\n\t\t\t}\r\n\t\t],\r\n\t\t\"stateMutability\": \"view\",\r\n\t\t\"type\": \"function\"\r\n\t}\r\n]";
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string playfabID = data?.PlayfabId;
            string amountString = data?.Amount;
            string entityToken = data?.EntityToken;
            string entityID = data?.EntityId;
            string entityType = data?.EntityType;
            string titleID = data?.TitleId;
            string toAddress = data?.ToAddress;
            string itemID = data?.ItemId;

            if (data == null || playfabID == null || amountString == null || entityToken == null || entityID == null || entityType == null || titleID == null || toAddress == null)
            {
                return new OkObjectResult("arguments is invalid");
            }
            int sendAmount = 0;
            if (!int.TryParse(amountString, out sendAmount))
            {
                return new OkObjectResult("amount is invalid ");
            }
            if (sendAmount <= 0)
            {
                return new OkObjectResult("amount is invalid");
            }


            string DeveloperKey = await DatabaseManager.GetDeveloperSecretKey(titleID);

            if (DeveloperKey == null)
            {
                return new OkObjectResult("Key not found");
            }
            var TokenID = await DatabaseManager.GetTokenID(titleID, itemID);
            if (TokenID == null)
            {
                return new OkObjectResult("TokenID not found");
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

            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            var web3 = new Web3(account, ethProviderAddress);
            var nftContract = web3.Eth.GetContract(abi, nftContractAddress);
            var balanceOfBatchFunction = nftContract.GetFunction("balanceOf");
            var tokenId = BigInteger.Parse(TokenID);
            // получаем количество NFT на адресе для каждого идентификатора токена
            var balance = await balanceOfBatchFunction.CallAsync<BigInteger>(address, tokenId);
            if (balance < sendAmount)
            {
                return new OkObjectResult($"insufficient funds");
            }
            var userInventory = await PlayFabServerAPI.GetUserInventoryAsync(new GetUserInventoryRequest()
            {
                AuthenticationContext = authContext,
                PlayFabId = playfabID
            });
            int revokeAmount = 0;
            int skinRemainingUses = 0;
            int CurrentAmountWK = 0;
            var inventory = userInventory.Result.Inventory;
            var virtualCurrency = userInventory.Result.VirtualCurrency;

            foreach (var currency in virtualCurrency)
            {
                if (currency.Key == Data.TokenType.WK)
                {
                    CurrentAmountWK = currency.Value;
                }
            }

            if (CurrentAmountWK <= 0)
            {
                return new OkObjectResult($"user WK amount is zero");
            }

            ItemInstance itemInstance = null;
            List<RevokeInventoryItem> revokeItemList = new List<RevokeInventoryItem>();
            foreach (var item in inventory)
            {
                if (item.ItemId == itemID)
                {

                    if (item.RemainingUses == null)
                    {
                        revokeAmount++;
                        revokeItemList.Add(new RevokeInventoryItem { ItemInstanceId = item.ItemInstanceId, PlayFabId = playfabID });
                    }
                    else
                    {
                        skinRemainingUses = (int)item.RemainingUses;
                        itemInstance = item;
                    }
                }
            }


            if ((revokeAmount + skinRemainingUses) == 0 || revokeAmount + skinRemainingUses < sendAmount)
            {
                return new OkObjectResult($"user item amount is zero");
            }

            PlayFabResult<RevokeInventoryItemsResult> revokeResult = null;
            if (revokeItemList.Count > 0)
            {
                List<RevokeInventoryItem> revokeItems = new List<RevokeInventoryItem>();
                for (int i = 0; i < sendAmount; i++)
                {
                    revokeItems.Add(revokeItemList[i]);
                }
                revokeResult = await PlayFabServerAPI.RevokeInventoryItemsAsync(new RevokeInventoryItemsRequest
                {
                    AuthenticationContext = authContext,
                    Items = revokeItems
                });
            }

            int consumeAmount = sendAmount - revokeItemList.Count;
            PlayFabResult<ConsumeItemResult> consumeResult = null;
            if (skinRemainingUses > 0)
            {
                List<RevokeInventoryItem> items = new List<RevokeInventoryItem>();
                for (int i = 0; i < consumeAmount; i++)
                {
                    var item = new RevokeInventoryItem() { PlayFabId = playfabID, ItemInstanceId = itemInstance.ItemInstanceId };
                    items.Add(item);
                }
                consumeResult = await PlayFabServerAPI.ConsumeItemAsync(new ConsumeItemRequest
                {
                    ConsumeCount = items.Count,
                    AuthenticationContext = authContext,
                    PlayFabId = playfabID,
                    ItemInstanceId = itemInstance.ItemInstanceId
                });
            }



            var wkSubtract = await PlayFabServerAPI.SubtractUserVirtualCurrencyAsync(new SubtractUserVirtualCurrencyRequest
            {
                Amount = 1,
                AuthenticationContext = authContext,
                PlayFabId = playfabID,
                VirtualCurrency = Data.TokenType.WK
            });

            if (revokeResult != null || consumeResult != null)
            {
                var gasPrice = new HexBigInteger(Web3.Convert.ToWei(10, UnitConversion.EthUnit.Gwei));
                var transferFunction = new SafeTransferFromFunction()
                {
                    From = address,
                    To = toAddress,
                    Id = tokenId,
                    Amount = new BigInteger(sendAmount),
                    Data = new byte[0]
                };

                var callData = transferFunction.CreateCallInput(nftContractAddress).Data;

                var transactionInput = new TransactionInput
                {
                    From = address,
                    To = nftContractAddress,
                    Gas = ContractData.GasNFT,// new HexBigInteger(100000),
                    GasPrice = gasPrice,
                    Value = ContractData.Value,// new HexBigInteger(0),
                    ChainId = ContractData.ChainID,
                    Data = callData
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
                        var saveResult = await DatabaseManager.SaveNFTHashInDB(titleID, transactionReceipt, playfabID, Direction.ToWallet, itemID);

                        return new OkObjectResult($"{transactionHash}");
                    }
                }
                catch (Exception ex)
                {
                    List<string> itemIDs = new List<string>();
                    for (int i = 0; i < sendAmount; i++)
                    {
                        itemIDs.Add(itemID);
                    }
                    var grant = await PlayFabServerAPI.GrantItemsToUserAsync(new GrantItemsToUserRequest
                    {
                        ItemIds = itemIDs,
                        AuthenticationContext = authContext,
                        PlayFabId = playfabID,
                        CatalogVersion = "Skins"
                    });

                    var addWK = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new AddUserVirtualCurrencyRequest
                    {
                        Amount = 1,
                        AuthenticationContext = authContext,
                        VirtualCurrency = Data.TokenType.WK,
                        PlayFabId = playfabID,
                    });
                    return new OkObjectResult($"Ошибка: {ex}");

                }
                return new OkObjectResult($"Ошибка");

            }
            return new OkObjectResult($"Ошибка");
        }



    }

}
