using Nethereum.Hex.HexTypes;

namespace WorkWithDB.Database
{
    public class NFTCollectionData
    {
        public string id { get; set; }
        public HexBigInteger TransactionIndex { get; set; }
        public string TransactionHash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Address { get; set; }
        public int Value { get; set; }
        public string BlockHash { get; set; }
        public HexBigInteger BlockNumber { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string LogIndex { get; set; }
        public string PlayfabID { get; set; }
        public string Direction { get; set; }
        public string NFTID { get; set; }

        public string SkinId { get; set; }
    }
}
