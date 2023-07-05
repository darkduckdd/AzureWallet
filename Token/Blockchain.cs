using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors;
using Nethereum.StandardTokenEIP20;
using System.Numerics;
using Nethereum.Util;
using System;
using WorkWithDB.Data;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Contracts;
using System.Linq;

namespace WorkWithDB.Token
{
    public static class Blockchain
    {
        public static async Task<BigInteger> GetTokenBalance(string tokenType)
        {
            string contractAddress;
            string ethProviderAddress = ContractData.EthProviderAddress;
            string privateKey;
            string ownerAddress;

            if (tokenType == TokenType.IGC)
            {
                contractAddress = ContractData.IGCContractAddress;
                ownerAddress = ContractData.OwnerAddress;

                privateKey = ContractData.PrivateKey;
            }
            else
            {
                contractAddress = ContractData.IGTContractAddress;
                ownerAddress = ContractData.OwnerAddress;

                privateKey = ContractData.PrivateKey;
            }

            var account = new Account(privateKey); ////работает получает данные
            var web3 = new Web3(account, ethProviderAddress);
            var tokenService = new StandardTokenService(web3, contractAddress);
            // Получение десятичных знаков для токена
            var balance = await tokenService.BalanceOfQueryAsync(ownerAddress);

            return balance;
        }
        public static async Task<decimal> GetBNBBalance()
        {
            string ethProviderAddress = ContractData.EthProviderAddress;
            string address = ContractData.OwnerAddress;
            var web3 = new Web3(ethProviderAddress);

            var balanceInWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            var balanceInBNB = UnitConversion.Convert.FromWei(balanceInWei.Value, 18);
            return balanceInBNB;
        }

        public static decimal GetValueFromReceipt(TransactionReceipt transactionReceipt)
        {
            var transferLogs = transactionReceipt.DecodeAllEvents<TransferEventDTO>().ToList();
            decimal transferTokenAmount = 0;

            foreach (var transferLog in transferLogs)
            {
                transferTokenAmount = Web3.Convert.FromWei(transferLog.Event.Value, 18);
            }
            return transferTokenAmount;
        }
    }
}
