using System.Collections.Generic;

namespace ArmaSOGClient
{
    public class NewsResponse
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string CreatedAt { get; set; }
    }

    public class WSSResponseMessage
    {
        public string Event { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
