using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArmaSOGClient
{
    internal class DBDefaults_Garage
    {
        private static async Task AddUnlockAsync(PlayerUnlock playerGarage)
        {
            await Task.Run(() => 
            {
                var record = DBHandler.GarageCollection.FindOne(x => x.ClassName == playerGarage.ClassName);
                if (record != null)
                {
                    DllEntry.ASC_Debuglog($"{playerGarage.ClassName} already exists, aborting add garage unlock.");
                    return;
                }

                DBHandler.GarageCollection.Insert(playerGarage);
                DBHandler.GarageCollection.EnsureIndex(x => x.ClassName, unique: true);
            });
        }

        public static async Task AddGarageUnlocksAsync()
        {
            #region Unlocks
            List<PlayerUnlock> Wheeled = new List<PlayerUnlock>
            {
                new PlayerUnlock { ClassName = "B_Quadbike_01_F", TypeInt = 0 },
            };

            var allUnlocks = new List<PlayerUnlock>();
            allUnlocks.AddRange(Wheeled);

            foreach (var playerGarageUnlock in allUnlocks)
            {
                await AddUnlockAsync(playerGarageUnlock);
            }
            #endregion
        }
    }
}
