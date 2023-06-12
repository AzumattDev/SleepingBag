using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using PieceManager;
using ServerSync;
using UnityEngine;
using CraftingTable = PieceManager.CraftingTable;

namespace SleepingBag
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SleepingBagPlugin : BaseUnityPlugin
    {
        internal const string ModName = "SleepingBag";
        internal const string ModVersion = "1.0.4";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource SleepingBagLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        
        public void Awake()
        {
            ConfigSync.IsLocked = true;

            Item sleepingBagItem = new("sleepingbag", "sleepingbag_item");
            sleepingBagItem.Name.English("Sleeping bag (Packed)");
            sleepingBagItem.Description.English(
                "Allows you to set your spawn point with less requirements than a bed, and to sleep under the stars if you are lucky with the weather.");
            sleepingBagItem.Crafting.Add(ItemManager.CraftingTable.Workbench, 1);
            sleepingBagItem.RequiredItems.Add("DeerHide", 3);
            sleepingBagItem.RequiredItems.Add("LeatherScraps", 2);


            BuildPiece sleepingBag = new("sleepingbag", "sleepingbag_piece");
            sleepingBag.Name.English("Sleeping bag");
            sleepingBag.Description.English(
                "Allows you to set your spawn point with less requirements than a bed, and to sleep under the stars if you are lucky with the weather.");
            sleepingBag.RequiredItems.Add("sleepingbag_item", 1, true);
            sleepingBag.Category.Add(BuildPieceCategory.Furniture);
            sleepingBag.Crafting.Set(CraftingTable.None);
            DestroyImmediate(sleepingBag.Prefab.GetComponent<WearNTear>());
            MaterialReplacer.RegisterGameObjectForShaderSwap(sleepingBag.Prefab,
                MaterialReplacer.ShaderType.PieceShader);

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                SleepingBagLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                SleepingBagLogger.LogError($"There was an issue loading your {ConfigFileName}");
                SleepingBagLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
        static class Bed_Interact_Patch
        {
            static bool Prefix(Bed __instance, Humanoid human, bool repeat, bool alt)
            {
                if (Player.m_localPlayer != null)
                {
                    // this function overrides the Bed.Interact method to bypass roof check if the GameObject is a sleeping bag.
                    if (repeat)
                        return false;
                    long my__instance = Game.instance.GetPlayerProfile().GetPlayerID();
                    long belongsTo = __instance.GetOwner();
                    Player player1 = human as Player;

                    // if it doesn't belong to anybody
                    if (belongsTo == 0L)
                    {
                        //then check if it is a sleeping bag, if so, bypass Roof Check
                        if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") &&
                            !(__instance.gameObject.name == "sleepingbag_piece"))
                        {
                            if (!__instance.CheckExposure(player1))
                                return false;
                        }

                        // now, it's mine
                        __instance.SetOwner(my__instance, Game.instance.GetPlayerProfile().GetName());
                        Game.instance.GetPlayerProfile().SetCustomSpawnPoint(__instance.GetSpawnPoint());
                        human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
                    }
                    //if it is mine
                    else if (__instance.IsMine())
                    {
                        //if it's my current spawnpoint
                        if (__instance.IsCurrent())
                        {
                            //is it time to sleep ? else prevent sleeping
                            if (!EnvMan.instance.IsAfternoon() && !EnvMan.instance.IsNight())
                            {
                                human.Message(MessageHud.MessageType.Center, "$msg_cantsleep");
                                return false;
                            }

                            //all clear ? warm ? dry ? else prevent sleeping 
                            if (!__instance.CheckEnemies(player1) ||
                                (!__instance.CheckFire(player1) || !__instance.CheckWet(player1)))
                            {
                                //heck if it is a sleeping bag, if so, bypass Roof Check and go to sleep !
                                if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") &&
                                    !(__instance.gameObject.name == "sleepingbag_piece"))
                                {
                                    if (!__instance.CheckExposure(player1))
                                        return false;
                                }
                            }

                            human.AttachStart(__instance.m_spawnPoint, __instance.gameObject, true, true, false,
                                "attach_bed",
                                new Vector3(0.0f, 0.5f, 0.0f));
                            return false;
                        }

                        //then check if it is a sleeping bag, if so, bypass Roof Check
                        if (!(__instance.gameObject.name == "sleepingbag_piece(Clone)") &&
                            !(__instance.gameObject.name == "sleepingbag_piece"))
                        {
                            if (!__instance.CheckExposure(player1))
                                return false;
                        }

                        //else, define as current
                        Game.instance.GetPlayerProfile().SetCustomSpawnPoint(__instance.GetSpawnPoint());
                        human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
                    }

                    return false;
                }

                return true;
            }
        }


        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}