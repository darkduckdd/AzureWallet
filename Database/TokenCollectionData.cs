using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace WorkWithDB.Database
{
    public class TokenCollectionData
    {
        public string id { get; set; }
        public HexBigInteger TransactionIndex { get; set; }
        public string TransactionHash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Address { get; set; }
        public decimal Value { get; set; }
        public BigInteger ValueInWei { get; set; }
        public string BlockHash { get; set; }
        public HexBigInteger BlockNumber { get; set; }
        public bool Accrued { get; set; }

        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string LogIndex { get; set; }
        public string PlayfabID { get; set; }
        public string Direction { get; set; }
        public string TokenType { get; set; }

    }
}
