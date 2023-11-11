//
// PROPRIETARY AN
// CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public partial class NeuronArray
    {
        public string networkNotes = "";
        public bool hideNotes = false;
        public long Generation = 0;
        public int EngineSpeed = 250;
        public bool EngineIsPaused = false;
        public int arraySize;
        public int rows;

        public int lastFireCount = 0;

        //these have nothing to do with the NeuronArray but are here so it will be saved and restored with the network
        private bool showSynapses = false;
        public bool ShowSynapses
        {
            get => showSynapses;
            set => showSynapses = value;
        }
        public int Cols { get => arraySize / rows; }
        private bool loadComplete = false;
        [XmlIgnore]
        public bool LoadComplete { get => loadComplete; set => loadComplete = value; }

        private Dictionary<int, string> labelCache = new Dictionary<int, string>();
        public void AddLabelToCache(int nID, string label)
        {
            if (labelCache.ContainsKey(nID))
            {
                labelCache[nID] = label;
            }
            else
            {
                labelCache.Add(nID, label);
            }
        }
        public void RemoveLabelFromCache(int nID)
        {
            try
            {
                labelCache.Remove(nID);
            }
            catch { };
        }
        public string GetLabelFromCache(int nID)
        {
            if (labelCache.ContainsKey(nID))
                return labelCache[nID];
            else
                return "";
        }

        public void ClearLabelCache()
        {
            labelCache.Clear();
        }

        public List<string> GetValuesFromLabelCache()
        {
            return labelCache.Values.ToList();
        }
        public List<int> GetKeysFromLabelCache()
        {
            return labelCache.Keys.ToList();
        }


        private int refractoryDelay = 0;
        public int RefractoryDelay
        { get => refractoryDelay; set { refractoryDelay = value; 
                // SetRefractoryDelay(refractoryDelay); 
            }}

        public void Initialize(int count, int inRows, bool clipBoard = false)
        {
            rows = inRows;
            arraySize = count;
            ClearLabelCache();

            // if(MainWindow.pauseTheNeuronArray)
            //     EngineIsPaused = true;
        }

        public NeuronArray()
        {
        }

        public void GetCounts(out long synapseCount, out int useCount)
        {
            synapseCount = 0; //  GetTotalSynapses();
            useCount = 0; //  GetTotalNeuronsInUse();
        }

        public new void Fire()
        {
            Generation = 0; // GetGeneration();
            lastFireCount = 0; // GetFiredCount();
        }

        //fires all the modules
        private void HandleProgrammedActions()
        {
            int badModule = -1;
            string message = "";
            lock (MainWindow.modules)
            {
                // First make sure all modules that need it have the proper camera image...
                ImgUtils.DistributeNextFrame(MainWindow.modules);
                List<int> firstNeurons = new List<int>();

                long startModules = Utils.GetPreciseTime();
                for (int i = 0; i < MainWindow.modules.Count; i++)
                {
                    ModuleBase ma = MainWindow.modules[i];
                    if (ma != null)
                    {
                        if (ma.isEnabled)
                        {
                            long start = Utils.GetPreciseTime();
                            ma.Fire();
                            long duration = Utils.GetPreciseTime() - start;
                            if (duration > 1000000)
                            {
                                // Debug.WriteLine("Fire() runs " + duration + " for " + na.TheModule.ToString());
                            }
                        }
                    }
                }
                // long TotalDuration = Utils.GetPreciseTime() - startModules;
                // Debug.WriteLine("Fire() runs " + TotalDuration + " for all modules");
            }
        }
    }
}
