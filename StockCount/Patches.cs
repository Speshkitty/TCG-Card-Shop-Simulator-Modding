using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockCount
{
    [HarmonyPatch(typeof(RestockItemPanelUI), nameof(RestockItemPanelUI.Init))]
    class Patches
    {
        public static void Postfix(RestockItemPanelUI __instance, int index)
        {
            Plugin.Logger.LogInfo($"Postfix for {__instance.m_ItemNameText.text} ran!");

            RestockData restockData = InventoryBase.GetRestockData(index);

            if (Plugin.StockAmounts.TryGetValue(restockData.itemType, out var amount))
            {
                __instance.m_AmountText.text = $"Stock: {amount}";
            }
            else
            {
                __instance.m_AmountText.text = $"Stock: 0";
            }
        }
    }
}
