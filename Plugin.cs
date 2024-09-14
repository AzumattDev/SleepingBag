using System.IO;
using System.Linq;
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
    [BepInDependency(BackpacksGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class SleepingBagPlugin : BaseUnityPlugin
    {
        internal const string ModName = "SleepingBag";
        internal const string ModVersion = "1.0.6";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        internal const string BackpacksGUID = "org.bepinex.plugins.backpacks";

        public static readonly ManualLogSource SleepingBagLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            ConfigSync.IsLocked = true;

            Item sleepingBagItem = new("sleepingbag", "sleepingbag_item");
            sleepingBagItem.Name.English("Sleeping bag (Packed)");
            sleepingBagItem.Description.English("Allows you to set your spawn point with less requirements than a bed, and to sleep under the stars if you are lucky with the weather.");
            sleepingBagItem.Crafting.Add(ItemManager.CraftingTable.Workbench, 1);
            sleepingBagItem.RequiredItems.Add("DeerHide", 3);
            sleepingBagItem.RequiredItems.Add("LeatherScraps", 2);
            //sleepingBagItem.Snapshot();

            BuildPiece sleepingBag = new("sleepingbag", "sleepingbag_piece");
            sleepingBag.Name.English("Sleeping bag");
            sleepingBag.Description.English("Allows you to set your spawn point with less requirements than a bed, and to sleep under the stars if you are lucky with the weather.");
            sleepingBag.RequiredItems.Add("sleepingbag_item", 1, true);
            sleepingBag.Category.Set(BuildPieceCategory.Furniture);
            sleepingBag.Crafting.Set(CraftingTable.None);
            //sleepingBag.Snapshot();
            DestroyImmediate(sleepingBag.Prefab.GetComponent<WearNTear>());

            MaterialReplacer.RegisterGameObjectForShaderSwap(sleepingBagItem.Prefab, MaterialReplacer.ShaderType.PieceShader);
            MaterialReplacer.RegisterGameObjectForShaderSwap(sleepingBag.Prefab, MaterialReplacer.ShaderType.PieceShader);

            PiecePrefabManager.RegisterPrefab("sleepingbag", "sfx_wrap_unwrap");

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


        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
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

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
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