using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ArmaSOGClient
{
    public class DllEntry
    {
        private const string ASC_Version = "0.0.1";
        public static string ASC_Host = "127.0.0.1";
        public static int ASC_Port = 8080;
        public static string ASC_Uri = "http://localhost:3002/rss/latest";
        public static bool ASC_ContextLog = false;
        public static bool ASC_Debug = false;
        public static bool ASC_InitCheck = false;
        public static IntPtr ASC_Context;
        public static string ASC_LogFolder = "\\@sog_client\\logs";
        public static string SteamID = "";
        public static ExtensionCallback callback;
        public delegate int ExtensionCallback(string name, string function, string data);

        public static void ASC_Init()
        {
            char[] separator = new char[1] { '=' };
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            string str = "";
            for (int index = 0; index < commandLineArgs.Length; ++index)
            {
                string[] strArray = commandLineArgs[index].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (strArray[0].ToLower() == "-asc")
                {
                    str = strArray[1];
                    break;
                }
            }
            if (str == "")
                str = "@sog_client\\config.xml";
            if (File.Exists(Environment.CurrentDirectory + "\\" + str))
            {
                List<string> stringList = new List<string>();
                List<string> list = XElement.Load(Environment.CurrentDirectory + "\\" + str).Elements().Select<XElement, string>((Func<XElement, string>) (eintrag => (string) eintrag)).ToList<string>();
                ASC_Host = list[0];
                ASC_Port = Convert.ToInt32(list[1]);
                if (bool.TryParse(list[2], out bool result))
                    ASC_Debug = result;
                ASC_Actionlog(string.Format("Config file found! Debug Mode: {2}! Changed Server Settings to: {0}:{1}!", (object)ASC_Host, (object)ASC_Port, (object)ASC_Debug));
            }
            else
                ASC_Errorlog("Config file not found! Default Settings loaded.");
            ASC_InitCheck = true;
        }

        public static void Log(string msg, string logType)
        {
            if (!ASC_Debug)
                return;
            string logFileName = logType + ".log";
            string path = Environment.CurrentDirectory + ASC_LogFolder;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(path, logFileName), true))
            {
                streamWriter.WriteLine(DateTime.Now.ToString() + " >> " + msg);
            }
        }

        public static void ASC_Errorlog(string msg)
        {
            Log(msg, "error");
        }

        public static void ASC_Actionlog(string msg)
        {
            Log(msg, "action");
        }

        public static void ASC_Contextlog(string msg)
        {
            Log(msg, "context");
        }

        public static void ASC_Debuglog(string msg)
        {
            Log(msg, "debug");
        }

        [DllExport("RVExtensionRegisterCallback", CallingConvention = CallingConvention.Winapi)]
        public static void RvExtensionRegisterCallback(ExtensionCallback func)
        {
            callback = func;
        }

        [DllExport("RVExtensionVersion", CallingConvention = CallingConvention.Winapi)]
        public static void RvExtensionVersion(StringBuilder output, int outputSize)
        {
            output.Append(ASC_Version, 0, Math.Min(ASC_Version.Length, outputSize));
        }

        [DllExport("RVExtensionContext", CallingConvention = CallingConvention.Winapi)]
        public static void RVExtensionContext(IntPtr args, int argsCnt)
        {
            if (!(ASC_Context != args))
                return;
            ASC_Context = args;
            string[] strArray = new string[argsCnt];
            int size = IntPtr.Size;
            for (int index = 0; index < argsCnt; ++index)
                strArray[index] = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(args, index * size));
            SteamID = strArray[0].ToString();
            if (!ASC_ContextLog)
                return;
            ASC_Contextlog("StreamID: " + strArray[0].ToString());
            ASC_Contextlog("MissionName: " + strArray[2].ToString());
            ASC_Contextlog("ServerName: " + strArray[3].ToString());
        }

        [DllExport("RVExtension", CallingConvention = CallingConvention.Winapi)]
#pragma warning disable IDE0060 // Remove unused parameter
        public static void RvExtension(StringBuilder output, int outputSize, string function)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (!ASC_InitCheck)
                ASC_Init();
            switch (function.ToLower())
            {
                case "time":
                    DateTime timeNow = DateTime.Now;
                    int day = timeNow.Day;
                    int month = timeNow.Month;
                    int year = timeNow.Year;
                    int hour = timeNow.Hour;
                    int minute = timeNow.Minute;
                    int second = timeNow.Second;
                    output.Append(day.ToString() + ":" + month.ToString() + ":" + year.ToString() + ":" + hour.ToString() + ":" + minute.ToString() + ":" + second.ToString());
                    break;
                default:
                    output.Append(function);
                    break;
            }
        }

        [DllExport("RVExtensionArgs", CallingConvention = CallingConvention.Winapi)]
        public static int RVExtensionArgs(StringBuilder output, int outputSize, string function, IntPtr args, int argsCnt)
        {
            var argsArr = new string[argsCnt];
            var argSize = IntPtr.Size;
            for (var i = 0; i < argsCnt; i++)
            {
                argsArr[i] = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(args, i * argSize));
            }

            if (function == "fnc1")
            {
                //--- Manually assemble output array
                var strBuilder = new StringBuilder("[");
                if (argsCnt > 0)
                {
                    strBuilder.Append(argsArr[0]);
                }

                for (var i = 1; i < argsCnt; i++)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(argsArr[i], 0, Math.Min(argsArr[i].Length, outputSize));
                }

                strBuilder.Append("]");

                //--- Extension result
                output.Append(strBuilder.ToString());

                //--- Extension return code
                return 100;
            }

            if (function == "fnc2")
            {
                //--- Parse args into list
                var list = argsArr.ToList();

                var strBuilder = new StringBuilder();
                if (list.Any())
                {
                    //--- Assemble output array
                    strBuilder.Append("[");
                    strBuilder.Append(string.Join(",", list));
                    strBuilder.Append("]");
                }

                //--- Extension result
                output.Append(strBuilder.ToString());

                //--- Extension return code
                return 100;
            }

            if (function == "fnc3")
            {
                //--- Parse args into list
                var list = argsArr.ToList();

                var strBuilder = new StringBuilder();
                if (list.Any())
                {
                    //--- Assemble output array
                    strBuilder.Append("[");
                    strBuilder.Append(string.Join(",", list));
                    strBuilder.Append("]");
                }

                //--- Extension result
                output.Append("Async");

                Task.Run(() =>
                {
                    Task.Delay(1000);
                    callback("ArmaSOGClient", function, strBuilder.ToString());
                });

                //output.Append(strBuilder.ToString(), 0, Math.Min(strBuilder.Length, outputSize));

                //--- Extension return code
                return 200;
            }

            if (function == "fnc4")
            {
                //--- Parse args into list
                var list = argsArr.ToList();

                var strBuilder = new StringBuilder();
                if (list.Any())
                {
                    //--- Assemble output array
                    strBuilder.Append("[");
                    strBuilder.Append(string.Join(",", list));
                    strBuilder.Append("]");
                }

                //--- Extension result
                output.Append("Async");
                var res = callback("ArmaSOGClient", function, strBuilder.ToString());
                ASC_Debuglog($"{res}");

                //output.Append(strBuilder.ToString(), 0, Math.Min(strBuilder.Length, outputSize));

                //--- Extension return code
                return 100;
            }

            if (function == "fetch_news")
            {
                Task.Run(async () =>
                {
                    string response = await FetchLatestNews();
                    callback("ArmaSOGClient", "sog_client_ext_fnc_fetchNews", response);
                });
                output.Append("Fetching News");
                return 100;
            }

            if (function == "fetch_unlocks" && argsCnt >= 1)
            {
                string typeCol = argsArr[0];

                Task.Run(async () => await FetchUnlocks(typeCol));
                output.Append("Fetching unlocks from LiteDB");
                return 100;
            }

            if (function == "first_login")
            {
                Task.Run(async () => await FirstLogin());
                output.Append("Creating LiteDB Collections");
                return 100;
            }

            if (function == "save_status" && argsCnt >= 2)
            {
                string uid = argsArr[0];
                string status = argsArr[1];

                Task.Run(async () => await SaveStatus(uid, status));
                output.Append("Saving status to LiteDB");
                return 100;
            }

            if (function == "save_unlock" && argsCnt >= 3)
            {
                string typeCol = argsArr[0];
                string className = argsArr[1];
                string typeInt = argsArr[2];

                Task.Run(async () => await SaveUnlock(typeCol, className, typeInt));
                output.Append("Saving unlock to LiteDB");
                return 100;
            }

            output.Append("Available Functions: fnc1, fnc2, fnc3, fnc4, fetch_news, fetch_unlocks, first_login, save_status, save_unlock");
            return -1;
        }

        private static async Task<string> FetchLatestNews()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(ASC_Uri);
                    if (response.IsSuccessStatusCode)
                    {
                        ASC_Actionlog("Connected to API.");
                        string content = await response.Content.ReadAsStringAsync();
                        ASC_Actionlog("GET response: " + content);

                        var newsResponse = JsonConvert.DeserializeObject<NewsResponse>(content);

                        DateTime date = DateTime.Parse(newsResponse.CreatedAt);
                        string formattedDate = date.ToString("MMMM dd, yyyy HH:mm:ss");

                        var news = new string[]
                        {
                            newsResponse.Title,
                            formattedDate,
                            newsResponse.Description,
                            newsResponse.Url
                        };

                        string json = JsonConvert.SerializeObject(news);

                        ASC_Debuglog($"Latest News: {json}");
                        return json;
                    }
                    else
                    {
                        ASC_Errorlog($"Error: Unable to fetch latest news. {response.StatusCode} {response.ReasonPhrase}");
                        return $"Error: Unable to fetch latest news. {response.StatusCode} {response.ReasonPhrase}";
                    }
                }
                catch (Exception ex)
                {
                    ASC_Errorlog($"Error: {ex.Message} {ex.StackTrace}");
                    return $"Error: {ex.Message} {ex.StackTrace}";
                }
            }
        }

        private static async Task<string> FetchUnlocks(string typeCol)
        {
            DBHandler dbHandler = DBHandler.Instance();

            try
            {
                await dbHandler.FetchUnlocks(typeCol.Trim('"'));
                return "Success";
            }
            catch (Exception ex)
            {
                ASC_Errorlog($"Error: {ex.Message} {ex.StackTrace}");
                return $"Error: {ex.Message} {ex.StackTrace}";
            }
        }

        private static async Task<string> FirstLogin()
        {
            DBHandler dbHandler = DBHandler.Instance();

            try
            {

                await dbHandler.FirstLoginAsync();
                return "Success";
            }
            catch (Exception ex)
            {
                ASC_Errorlog($"Error: {ex.Message} {ex.StackTrace}");
                return $"Error: {ex.Message} {ex.StackTrace}";
            }
        }

        private static async Task<string> SaveStatus(string uid, string status)
        {
            DBHandler dbHandler = DBHandler.Instance();

            try
            {
                var playerStatus = new PlayerStatus
                {
                    Uid = uid.Trim('"'),
                    Status = status.Trim('"')
                };

                await dbHandler.SaveStatusAsync(playerStatus);
                return "Success";
            }
            catch (Exception ex)
            {
                ASC_Errorlog($"Error: {ex.Message} {ex.StackTrace}");
                return $"Error: {ex.Message} {ex.StackTrace}";
            }
        }

        private static async Task<string> SaveUnlock(string typeCol, string className, string typeInt)
        {
            DBHandler dbHandler = DBHandler.Instance();

            try
            {
                var playerUnlock = new PlayerUnlock
                {
                    ClassName = className.Trim('"'),
                    TypeInt = Convert.ToInt32(typeInt.Trim('"'))
                };

                await dbHandler.SaveUnlockAsync(typeCol.Trim('"'), playerUnlock);
                return "Success";
            }
            catch (Exception ex)
            {
                ASC_Errorlog($"Error: {ex.Message} {ex.StackTrace}");
                return $"Error: {ex.Message} {ex.StackTrace}";
            }
        }
    }
}
