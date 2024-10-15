using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace StockCount
{
    [HarmonyPatch]
    class Patches
    {
        public class StockDataFont 
        { 
            // Find all loaded TMP_FontAsset objects in the game and return FredokaOne
            public static TMP_FontAsset fredokaFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(font => font.name == "FredokaOne-Regular SDF border");
        }

        class StockData
        {
            
            public int ItemsInShelves;
            public int ItemsInBoxes;
            public int AmountOfBoxes;

            public StockData(int itemsInShelves, int itemsInBoxes, int amountOfBoxes)
            {
                ItemsInShelves = itemsInShelves;
                ItemsInBoxes = itemsInBoxes;
                AmountOfBoxes = amountOfBoxes;
            }
        }

        private static Dictionary<EItemType, StockData> StockAmounts = new Dictionary<EItemType, StockData>();

        [HarmonyPatch(typeof(RestockItemPanelUI), nameof(RestockItemPanelUI.Init))]
        [HarmonyPostfix]
        public static void PatchItemStock(RestockItemPanelUI __instance, int index)
        {
            
            if(index == -1) { return; } 

            Plugin.Log($"Postfix for {__instance.m_ItemNameText.text} ran!");

            // Find the parent object of the text object
            Transform UIGroupGO = __instance.transform.Find("TopUIGrp/UIGrp");
            GameObject textObject;
            TextMeshProUGUI stockText;

            TMP_FontAsset fredokaFont = StockDataFont.fredokaFont;

            // Only create new Text gameobject if it doesn't exist yet and if License has been unlocked
            if (!UIGroupGO.Find("StockText"))
            {
                textObject = new GameObject("StockText");
                stockText = textObject.AddComponent<TextMeshProUGUI>();
                stockText.font = fredokaFont;
                stockText.fontSizeMin = 20;
                stockText.fontSizeMax = 24;
                stockText.enableAutoSizing = true;
                stockText.color = Color.white;
                stockText.outlineWidth = 0.237f;
                stockText.alignment = TextAlignmentOptions.Center;

                // Move the total cost box down a bit so we have more space for the stock text
                Transform costBox = UIGroupGO.Find("TotalPriceBG");
                costBox.transform.localPosition += new Vector3(0, -20, 0);


                // place stock text above total cost box
                textObject.transform.SetParent(UIGroupGO.transform);
                RectTransform rectTransform = stockText.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(600, 100);  // Set the size of the text box
                rectTransform.transform.localScale = new Vector3(1, 1, 1);
                rectTransform.transform.localPosition = costBox.transform.localPosition + new Vector3(0, 55, 0);
            } else
            {
                textObject = UIGroupGO.Find("StockText").gameObject;
                stockText = textObject.GetComponent<TextMeshProUGUI>();
            }    

            try
            {
                RestockData restockData = InventoryBase.GetRestockData(index);
                //only update text if the license has been bought
                if (restockData != null && __instance.m_UIGrp.activeSelf) { 
                    if (StockAmounts.TryGetValue(restockData.itemType, out var stockData))
                    {
                    
                        if (Plugin.showAmountofBoxes.Value && Plugin.showAmountInShelves.Value)
                        {
                            // setting font size directly doesnt work so we have to set min size
                            stockText.text = $"On Display: {stockData.ItemsInShelves}  Stock: {stockData.ItemsInBoxes} ({stockData.AmountOfBoxes})";

                        }
                        else if (Plugin.showAmountInShelves.Value)
                        {
                            // setting font size directly doesnt work so we have to set min size
                            stockText.text = $"On Display: {stockData.ItemsInShelves}  Stock: {stockData.ItemsInBoxes}";
                        }
                        else if (Plugin.showAmountofBoxes.Value)
                        {
                            stockText.text = $"Stock: {stockData.ItemsInBoxes} ({stockData.AmountOfBoxes})";
                        }
                        else
                        {
                            stockText.text = $"Stock: {stockData.ItemsInBoxes}";
                        }
                    }
                    else
                    {
                        if (Plugin.showAmountofBoxes.Value && Plugin.showAmountInShelves.Value)
                        {
                            // setting font size directly doesnt work so we have to set min size
                            stockText.text = $"On Display: 0  Stock: 0 (0)";
                        }
                        else if (Plugin.showAmountInShelves.Value)
                        {
                            // setting font size directly doesnt work so we have to set min size
                            stockText.text = $"On Display: 0  Stock: 0";
                        }
                        else if (Plugin.showAmountofBoxes.Value)
                        {
                            stockText.text = $"Stock: 0 (0)";
                        }
                        else
                        {
                            stockText.text = $"Stock: 0";
                        }
                    }
                }

                // add stockText to the restock panel

            }
            catch(Exception ex)
            {
                Plugin.Log(ex.Message);
                Plugin.Log(ex.StackTrace);
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
                if(!Plugin.showAmountInShelves.Value)
                {
                    // only skip the rest if we don't need to show the amount in shelves
                    return;
                }
            } else { }
            StringBuilder sb = new();
            try
            {
                sb.AppendLine($"{boxes.Length} item boxes found!");
                StockAmounts.Clear();

                foreach (var box in boxes)
                {
                    if (box.GetItemType() == EItemType.None) { continue; }
                    if (!StockAmounts.TryAdd(box.GetItemType(), new StockData(0, box.m_ItemCompartment.GetItemCount(), 1)))
                    {
                        StockAmounts[box.GetItemType()].ItemsInBoxes += box.m_ItemCompartment.GetItemCount();
                        StockAmounts[box.GetItemType()].AmountOfBoxes += 1;
                    }
                }
#if DEBUG
                foreach (var box in StockAmounts)
                {
                    sb.AppendLine($"  {box.Key} : {box.Value}");
                }
#endif
                // only run if option is enabled for performance reasons
                if (Plugin.showAmountInShelves.Value) { 
                    var shelfList = ShelfManager.GetShelfList(); //find all shelves tags
                    foreach (var shelf in shelfList)
                    {   
                        var itemCompartmentList = shelf.GetItemCompartmentList();
                        foreach (var item in itemCompartmentList)
                        {
                            if (item.GetItemType() == EItemType.None) { continue; }
                            if (!StockAmounts.TryAdd(item.GetItemType(), new StockData(item.GetItemCount(), 0, 0)))
                            {
                                StockAmounts[item.GetItemType()].ItemsInShelves += item.GetItemCount();
                            }
                        }
                    }
                }

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
