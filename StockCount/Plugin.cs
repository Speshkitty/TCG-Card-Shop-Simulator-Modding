using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace StockCount
{
    [BepInProcess("Card Shop Simulator.exe")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static new ManualLogSource Logger;
        public static ConfigEntry<bool> showAmountInShelves;
        public static ConfigEntry<bool> showAmountofBoxes;


        private void Awake()
        {
            // Plugin startup logic
            showAmountInShelves = Config.Bind("General", "Show amount in Shelves", false, "Show the amount of items currently displayed in shelves.");
            showAmountofBoxes = Config.Bind("General", "Show amount of Boxes", false, "Show in how many different boxes your stock is kept.");
            
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            
        }

        internal static void Log(object TextToLog)
        {
#if DEBUG
            Logger.LogInfo(TextToLog.ToString());
#endif
        }

    }
}

