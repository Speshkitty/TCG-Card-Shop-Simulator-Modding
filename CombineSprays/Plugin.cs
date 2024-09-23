using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CombineSprays
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private static Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            harmony = new Harmony(MyPluginInfo.PLUGIN_NAME);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch]
    class HarmonyPatches
    {
        public static MethodBase TargetMethod()
        {
            // use normal reflection or helper methods in <AccessTools> to find the method/constructor
            // you want to patch and return its MethodInfo/ConstructorInfo
            //
            var type = typeof(InteractableAutoCleanser);
            return AccessTools.FirstMethod(type, method => method.Name.Contains("Spray"));
        }

        static void Postfix(InteractableAutoCleanser __instance)
        {
            var items = __instance.GetStoredItemList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("I sprayed!");
            sb.AppendLine($"  I have {__instance.GetItemCount()} bottles.");
            float total = 0F;
            for (int i = 0; i < __instance.GetItemCount(); i++)
            {
                total += items[i].GetContentFill();
                sb.AppendLine($"    Bottle {i + 1} has {items[i].GetContentFill() * 100}% remaining. ");
            }
            sb.AppendLine($"  I have a total of {total * 100}% spray.");
            sb.AppendLine($"  Consolidating...");

            int cansFilled = 0;

            List<Item> itemsToRemove = new List<Item>();
            for (int i = 0; i < items.Count; i++)
            {
                var nextCan = Math.Min(1F, total);
                if (nextCan > 0F)
                {
                    items[i].SetContentFill(nextCan);
                    total -= nextCan;
                    cansFilled++;
                    sb.AppendLine($"    {cansFilled} can{(cansFilled==1 ? "":"s")} filled. I have {total * 100}% spray remaining.");
                }
                else
                {
                    itemsToRemove.Add(items[i]);
                }
            }

            sb.AppendLine($"    {itemsToRemove.Count} can{(itemsToRemove.Count == 1 ? "" : "s")} to remove.");

            itemsToRemove.ForEach(item =>
            {
                __instance.RemoveItem(item);
            });

            Plugin.Logger.LogDebug(sb.ToString());
        }
    }
}
