using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace StockCount
{
    [HarmonyPatch]
    class Patches
    {
        private static Dictionary<EItemType, int> StockAmounts = [];

        [HarmonyPatch(typeof(RestockItemPanelUI), nameof(RestockItemPanelUI.Init))]
        [HarmonyPostfix]
        public static void PatchQuantity(RestockItemPanelUI __instance, int index)
        {

            Plugin.Log($"Postfix for {__instance.m_ItemNameText.text} ran!");
            try
            {
                RestockData restockData = InventoryBase.GetRestockData(index);

                if (StockAmounts.TryGetValue(restockData.itemType, out var amount))
                {
                    __instance.m_AmountText.text = $"Stock{Environment.NewLine}{amount}";
                }
                else
                {
                    __instance.m_AmountText.text = $"Stock: 0";
                }
            }
            catch
            {
                return;
            }
        }

        [HarmonyPatch(typeof(RestockItemScreen), "Init")]
        [HarmonyPrefix]
        public static void RunOnRestockLoad(RestockItemScreen __instance)
        {
            Plugin.Log($"Prefix for RestockItemScreen ran!");

            var boxes = CGameManager.FindObjectsOfType<InteractablePackagingBox_Item>(); //find all item boxes
            if (boxes.Length == 0)
            {
                Plugin.Log("No boxes found");
                return;
            }
            StringBuilder sb = new();
            try
            {
                sb.AppendLine($"{boxes.Length} item boxes found!");
                StockAmounts.Clear();

                foreach (var box in boxes)
                {
                    if (box.GetItemType() == EItemType.None) { continue; }
                    if (!StockAmounts.TryAdd(box.GetItemType(), box.m_ItemCompartment.GetItemCount()))
                    {
                        StockAmounts[box.GetItemType()] += box.m_ItemCompartment.GetItemCount();
                    }
                }
#if DEBUG
                foreach (var box in StockAmounts)
                {
                    sb.AppendLine($"  {box.Key} : {box.Value}");
                }
#endif 

                //StockAmounts = temp;
                Plugin.Log(sb.ToString().Trim());
            }
            catch (Exception ex)
            {

                Plugin.Log(sb.ToString().Trim());
                Plugin.Log($"error: {ex.Message}");
            }
        }
    }
}
