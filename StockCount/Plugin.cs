using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace StockCount
{
    [BepInProcess("Card Shop Simulator.exe")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Dictionary<EItemType, int> StockAmounts = [];

        private Thread workerThread;


        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");


            workerThread = new Thread(new ThreadStart(StockWorker));
            workerThread.IsBackground = true;
            workerThread.Start();
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            
        }

        private void StockWorker()
        {
            while (true)
            {
                Thread.Sleep(1000);

                var boxes = CGameManager.FindObjectsOfType<InteractablePackagingBox_Item>(); //find all item boxes
                if (boxes.Length == 0)
                {
                    //Logger.LogInfo("No boxes found");
                    Thread.Sleep(10000);
                    continue;
                }

                StringBuilder sb = new();
                try
                {
                    sb.AppendLine($"{boxes.Length} item boxes found!");
                    StockAmounts.Clear();
                    Dictionary<EItemType, int> temp = [];
                    foreach (var box in boxes)
                    {
                        if (box.GetItemType() == EItemType.None) { continue; }
                        if(!temp.TryAdd(box.GetItemType(), box.m_ItemCompartment.GetItemCount()))
                        {
                            temp[box.GetItemType()] += box.m_ItemCompartment.GetItemCount();
                        }
                    }

                    foreach (var box in temp)
                    {
                        sb.AppendLine($"  {box.Key} : {box.Value}");
                    }

                    StockAmounts = temp;
                    //Logger.LogInfo(sb.ToString().Trim());
                }
                catch (Exception ex)
                {

                    //Logger.LogInfo(sb.ToString().Trim());
                    Logger.LogInfo($"error: {ex.Message}");
                }
            }
        }

    }
}

