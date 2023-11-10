//
// PROPRIETARY AN
// CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

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
        internal List<ModuleView> modules = new List<ModuleView>();
        public DisplayParams displayParams;

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

        public List<ModuleView> Modules
        {
            get { return modules; }
        }

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

            if(MainWindow.pauseTheNeuronArray)
                EngineIsPaused = true;
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
            lock (modules)
            {
                // First make sure all modules that need it have the proper camera image...
                ImgUtils.DistributeNextFrame(modules);
                List<int> firstNeurons = new List<int>();
                for (int i = 0; i < modules.Count; i++)
                    firstNeurons.Add(modules[i].FirstNeuron);
                firstNeurons.Sort();

                long startModules = Utils.GetPreciseTime();
                for (int i = 0; i < modules.Count; i++)
                {
                    ModuleView na = modules.Find(x => x.FirstNeuron == firstNeurons[i]);
                    if (na != null && na.TheModule != null)
                    {
                        int kk = 0;
                        //if (na.TheModule.UKS.)
                        //if not in debug mode, trap exceptions and offer to delete module
#if !DEBUG
                        try
                        {
                            if (na.TheModule.isEnabled)
                                na.TheModule.Fire();
                        }
                        catch (Exception e)
                        {
                            // Get stack trace for the exception with source file information
                            var st = new StackTrace(e, true);
                            // Get the top stack frame
                            var frame = st.GetFrame(0);
                            // Get the line number from the stack frame
                            var line = frame.GetFileLineNumber();

                            message = "Module " + na.Label + " threw an unhandled exception with the message:\n" + e.Message;
                            message += "\nat line " + line;
                            message += "\n\n Would you like to remove it from this network?";
                            badModule = i;
                        }
#else
                        if (na.TheModule.isEnabled)
                        {
                            long start = Utils.GetPreciseTime();
                            na.TheModule.Fire();
                            long duration = Utils.GetPreciseTime() - start;
                            if (duration > 1000000)
                            {
                                // Debug.WriteLine("Fire() runs " + duration + " for " + na.TheModule.ToString());
                            }
                        }
 #endif
                    }
                }
                // long TotalDuration = Utils.GetPreciseTime() - startModules;
                // Debug.WriteLine("Fire() runs " + TotalDuration + " for all modules");
            }
            if (message != "")
            {
                MessageBoxResult mbr = MessageBox.Show(message, "Remove Module?", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.Yes)
                {
                    ModuleView.DeleteModule(badModule);
                    MainWindow.Update();
                }
            }
        }

        public ModuleView FindModuleByLabel(string label)
        {
            ModuleView moduleView = modules.Find(na => na.Label.Trim() == label);
            if (moduleView == null)
            {
                if (label.StartsWith("Module"))
                {
                    label = label.Replace("Module", "");
                    moduleView = modules.Find(na => na.Label.Trim() == label);
                }
            }
            return moduleView;
        }
    }
}
