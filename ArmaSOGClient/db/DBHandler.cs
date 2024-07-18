using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;

namespace ArmaSOGClient
{
    internal class DBHandler : IDisposable
    {
        private static readonly object _lock = new object();
        private LiteDatabase _db;
        private static readonly string ASC_DBFolder = Path.Combine(Environment.CurrentDirectory, "@sog_client", "db");
        private static readonly string ASC_DBPath = Path.Combine(ASC_DBFolder, "asc_player.db");
        public static ILiteCollection<PlayerUnlock> ArmoryCollection;
        public static ILiteCollection<PlayerUnlock> GarageCollection;
        public static ILiteCollection<PlayerStatus> StatusCollection;
        public static ILiteCollection<PlayerData> PlayerCollection;

        private DBHandler() {}

        private static DBHandler _instance;

        public static DBHandler Instance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DBHandler();
                        _instance.InitializeLiteDB();
                        _instance.InitializeCollections();
                    }
                }
            }
            return _instance;
        }

        private void InitializeLiteDB()
        {
            if (!Directory.Exists(ASC_DBFolder))
            {
                Directory.CreateDirectory(ASC_DBFolder);
                DllEntry.ASC_Actionlog($"Created directory at: {ASC_DBFolder}");
            }

            var ASC_DBStr = $"Filename = {ASC_DBPath}; Password = xyz123; Connection Type = Shared;";
            _db = new LiteDatabase(ASC_DBStr);
            DllEntry.ASC_Actionlog("Connected to LiteDB.");
        }

        public void InitializeCollections()
        {
            ArmoryCollection = _db.GetCollection<PlayerUnlock>("armory");
            GarageCollection = _db.GetCollection<PlayerUnlock>("garage");
            PlayerCollection = _db.GetCollection<PlayerData>("player");
            StatusCollection = _db.GetCollection<PlayerStatus>("status");
        }

        public async Task FirstLoginAsync()
        {
            await DBDefaults_Armory.AddArmoryUnlocksAsync();
            await DBDefaults_Garage.AddGarageUnlocksAsync();
        }

        public async Task SaveStatusAsync(PlayerStatus playerStatus)
        {
            await Task.Run(() =>
            {
                var record = StatusCollection.FindOne(x => x.Uid == playerStatus.Uid);
                if (record != null)
                {
                    record.Status = playerStatus.Status;
                    StatusCollection.Update(record);
                }
                else
                {
                    StatusCollection.Insert(playerStatus);
                    StatusCollection.EnsureIndex(x => x.Uid, unique: true);
                }

                DllEntry.ASC_Actionlog($"Saved player status: {playerStatus.Uid}.");

                var msgObj = new object[] { "statusUpdated", playerStatus.Uid, playerStatus.Status };
                string msg = JsonConvert.SerializeObject(msgObj);

                DllEntry.ASC_Debuglog($"Player Status: {msg}");
                DllEntry.callback("ArmaSOGClient", "sog_client_ext_fnc_fetchStatus", msg);
            });
        }

        public async Task SaveUnlockAsync(string typeCol, PlayerUnlock playerUnlock)
        {
            await Task.Run(() =>
            {
                var collection = typeCol == "armory" ? ArmoryCollection : GarageCollection;
                var record = collection.FindOne(x => x.ClassName == playerUnlock.ClassName);

                if (record != null)
                {
                    DllEntry.ASC_Debuglog($"{playerUnlock.ClassName} already exists, aborting add unlock.");
                    return;
                }

                var unlock = new PlayerUnlock { ClassName = playerUnlock.ClassName, TypeInt = playerUnlock.TypeInt };
                collection.Insert(unlock);

                DllEntry.ASC_Actionlog($"Player unlock saved: {playerUnlock.ClassName}");
            });
        }

        public async Task<List<List<string>>> FetchUnlocksAsync(string typeCol) => await Task.Run(() =>
        {
            try
            {

                var collection = typeCol == "armory" ? ArmoryCollection : GarageCollection;
                var expectedItemTypes = typeCol == "armory" ? new[] { 0, 1, 2, 3 } : new[] { 0, 1, 2, 3, 4, 5 };

                var data = collection.FindAll().OrderBy(x => x.TypeInt).ToList();
                var groupedItems = data.GroupBy(x => x.TypeInt)
                                    .ToDictionary(g => g.Key, g => g.Select(x => x.ClassName).ToList());

                var unlocks = expectedItemTypes
                            .Select(itemType => groupedItems.ContainsKey(itemType) ? groupedItems[itemType] : new List<string>())
                            .ToList();

                DllEntry.ASC_Actionlog("Fetched player unlocks.");

                var json = JsonConvert.SerializeObject(unlocks);
                DllEntry.ASC_Debuglog($"{char.ToUpper(typeCol[0]) + typeCol.Substring(1).ToLower()} Unlocks: {json}");

                string functionName = typeCol == "armory" ? "sog_client_ext_fnc_fetchArmory" : "sog_client_ext_fnc_fetchGarage";
                DllEntry.callback("ArmaSOGClient", functionName, json);

                return unlocks;
            }
            catch (Exception ex)
            {
                DllEntry.ASC_Debuglog($"Error fetching {typeCol} unlocks: {ex.Message} {ex.StackTrace}");
                return new List<List<string>>();
            }
        });

        public List<(string DataType, object DataValue)> Fetch(string dataType = null)
        {
            IEnumerable<PlayerData> items;
            if (!string.IsNullOrEmpty(dataType))
            {
                items = PlayerCollection.Find(x => x.DataType == dataType);
            }
            else
            {
                items = PlayerCollection.FindAll();
            }

            var result = new List<(string DataType, object DataValue)>();
            foreach (var item in items)
            {
                object dataValue = TryParseJson(item.DataValue);
                result.Add((item.DataType, dataValue));
            }

            DllEntry.ASC_Debuglog($"{result}");
            return result;
        }

        public void Update(string dataType, object newValue)
        {
            string serializedNewValue = newValue is string str ? str : JsonConvert.SerializeObject(newValue);

            var dataToUpdate = PlayerCollection.Find(x => x.DataType == dataType).FirstOrDefault();

            if (dataToUpdate != null)
            {
                dataToUpdate.DataValue = serializedNewValue;
                PlayerCollection.Update(dataToUpdate);
            }
        }

        public void Append(string dataType, object additionalValue)
        {
            var dataToUpdate = PlayerCollection.FindOne(x => x.DataType == dataType);

            if (dataToUpdate != null)
            {
                var currentValue = JsonConvert.DeserializeObject<List<object>>(dataToUpdate.DataValue) ?? throw new InvalidOperationException("Current item value is not a list and cannot be appended to.");
                
                if (additionalValue is Dictionary<string, object> dictValue)
                {
                    foreach (var kvp in dictValue)
                    {
                        currentValue.Add(new List<object> { kvp.Key, kvp.Value });
                    }
                }
                else if (additionalValue is List<object> listValue)
                {
                    if (listValue.All(item => item is Dictionary<string, object>))
                    {
                        foreach (var item in listValue)
                        {
                            var dictItem = (Dictionary<string, object>)item;
                            foreach (var kvp in dictItem)
                            {
                                currentValue.Add(new List<object> { kvp.Key, kvp.Value });
                            }
                        }
                    }
                    else
                    {
                        currentValue.AddRange(listValue);
                    }
                }
                else
                {
                    currentValue.Add(additionalValue);
                }

                dataToUpdate.DataValue = JsonConvert.SerializeObject(currentValue);
                PlayerCollection.Update(dataToUpdate);
            }
        }

        public void New(string dataType, object dataValue)
        {
            string serializedDataValue = dataValue is string str ? str : JsonConvert.SerializeObject(dataValue);

            var newData = new PlayerData
            {
                DataType = dataType,
                DataValue = serializedDataValue
            };

            PlayerCollection.Insert(newData);
        }

        private object TryParseJson(string input)
        {
            if (input.StartsWith("[") || input.StartsWith("{"))
            {
                try
                {
                    return JsonConvert.DeserializeObject(input);
                }
                catch (JsonException)
                {
                    return input;
                }
            }
            return input;
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}