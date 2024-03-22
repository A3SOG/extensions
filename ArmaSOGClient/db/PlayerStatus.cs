using LiteDB;

namespace ArmaSOGClient
{
    internal class PlayerStatus
    {
#pragma warning disable IDE1006 // Naming Styles
        public ObjectId _id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string Uid { get; set; }
        public string Status { get; set; }
    }
}
