using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArmaSOGClient
{
    internal class DBDefaults_Armory
    {
        private static async Task AddUnlockAsync(PlayerUnlock playerArmoryUnlock)
        {
            await Task.Run(() =>
            {
                var record = DBHandler.ArmoryCollection.FindOne(x => x.ClassName == playerArmoryUnlock.ClassName);
                if (record != null)
                {
                    DllEntry.ASC_Debuglog($"{playerArmoryUnlock.ClassName} already exists, aborting add armory unlock.");
                    return;
                }

                DBHandler.ArmoryCollection.Insert(playerArmoryUnlock);
                DBHandler.ArmoryCollection.EnsureIndex(x => x.ClassName, unique: true);
            });
        }

        public static async Task AddArmoryUnlocksAsync()
        {
            #region Unlocks
            List<PlayerUnlock> Items = new List<PlayerUnlock>
            {
                new PlayerUnlock { ClassName = "SOG_Phone", TypeInt = 0 },
                new PlayerUnlock { ClassName = "ItemCompass", TypeInt = 0 },
                new PlayerUnlock { ClassName = "ItemGPS", TypeInt = 0 },
                new PlayerUnlock { ClassName = "ItemMap", TypeInt = 0 },
                new PlayerUnlock { ClassName = "ItemRadio", TypeInt = 0 },
                new PlayerUnlock { ClassName = "ItemWatch", TypeInt = 0 },
                new PlayerUnlock { ClassName = "U_BG_Guerrilla_6_1", TypeInt = 0 },
                new PlayerUnlock { ClassName = "V_Rangemaster_belt", TypeInt = 0 },
            };

            List<PlayerUnlock> Weapons = new List<PlayerUnlock>
            {
                new PlayerUnlock { ClassName = "hgun_P07_F", TypeInt = 1 },
            };

            List<PlayerUnlock> Magazines = new List<PlayerUnlock>
            {
                new PlayerUnlock { ClassName = "16Rnd_9x21_Mag", TypeInt = 2 },
            };

            List<PlayerUnlock> Backpacks = new List<PlayerUnlock>
            {
                new PlayerUnlock { ClassName = "B_AssaultPack_rgr", TypeInt = 3},
            };

            var allUnlocks = new List<PlayerUnlock>();
            allUnlocks.AddRange(Items);
            allUnlocks.AddRange(Weapons);
            allUnlocks.AddRange(Magazines);
            allUnlocks.AddRange(Backpacks);

            foreach (var playerArmoryUnlock in allUnlocks)
            {
                await AddUnlockAsync(playerArmoryUnlock);
            }
            #endregion
        }
    }
}
