using LiteDB;

namespace ArmaSOGClient
{
    internal class PlayerData
    {
#pragma warning disable IDE1006 // Naming Styles
        public ObjectId _id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string DataType { get; set; }
        public string DataValue { get; set; }
    }
}
