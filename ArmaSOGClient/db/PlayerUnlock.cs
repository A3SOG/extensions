using LiteDB;

namespace ArmaSOGClient
{
    internal class PlayerUnlock
    {
#pragma warning disable IDE1006 // Naming Styles
        public ObjectId _id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string ClassName { get; set; }
        public int TypeInt { get; set; }
    }
}
