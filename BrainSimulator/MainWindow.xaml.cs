using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class BrainSim3Data
    {
        public List<ModuleBase> modules = new List<ModuleBase>();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static BrainSim3Data BrainSim3Data = new BrainSim3Data();

        //the name of the currently-loaded network file
        public static string currentFileName = "";

        public MainWindow()
        {
            InitializeComponent();

            SetTitleBar();
            CreateEmptyNetwork();
            LoadModuleTypeMenu();
            InitializeModulePane();
            Loaded += MainWindow_Loaded;

            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            dt.Tick += Dt_Tick;
            dt.Start();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowAllModuleDialogs();
        }

        public void InitializeModulePane()
        {
            loadedModulesSP = LoadedModuleSP;
            LoadedModuleSP.Children.Clear();

            for (int i = 0; i < MainWindow.BrainSim3Data.modules.Count; i++)
            {
                ModuleBase mod = MainWindow.BrainSim3Data.modules[i];
                if (mod != null)
                {
                    mod.SetUpAfterLoad();
                }
            }

            ReloadLoadedModules();
        }

        public void ShowAllModuleDialogs()
        {
            foreach (ModuleBase mb in MainWindow.BrainSim3Data.modules)
            {
                if (mb != null && !mb.dlgIsOpen)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.ShowDialog();
                    });
                }
            }
        }

        public void CreateEmptyNetwork()
        {
            InsertMandatoryModules();
            InitializeModulePane();
            Update();
        }

        public void InsertMandatoryModules()
        {
            Debug.WriteLine("InsertMandatoryModules entered");
            BrainSim3Data.modules.Clear();
            BrainSim3Data.modules.Add(CreateNewUniqueModule("UKS"));
            BrainSim3Data.modules.Add(CreateNewUniqueModule("UKSInteract"));
        }

        public ModuleBase CreateNewUniqueModule(string ModuleName)
        {
            ModuleBase newModule = CreateNewModule(ModuleName);
            newModule.Label = MainWindow.GetUniqueModuleLabel(ModuleName);
            return newModule;
        }

        public static void CloseAllModuleDialogs()
        {
            lock (BrainSim3Data.modules)
            {
                foreach (ModuleBase md in MainWindow.BrainSim3Data.modules)
                {
                    if (md != null)
                    {
                        md.CloseDlg();
                    }
                }
            }
        }

        public static void CloseAllModules()
        {
            lock (MainWindow.BrainSim3Data.modules)
            {
                foreach (ModuleBase mb in MainWindow.BrainSim3Data.modules)
                {
                    if (mb != null)
                    {
                        mb.Closing();
                    }
                }
            }
        }

        private void SetTitleBar()
        {
            Title = "Brain Simulator III " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }
        
        public static void Update()
        {
            Debug.WriteLine("Update entered");
        }

        [XmlIgnore]
        public ModuleUKS UKS = null;
        public void GetUKS()
        {
            if (UKS is null)
            {
                UKS = (ModuleUKS)FindModule(typeof(ModuleUKS));
            }
        }

        public ModuleBase FindModule(Type t, bool suppressWarning = true)
        {
            foreach (ModuleBase mb1 in BrainSim3Data.modules)
            {
                if (mb1 != null && mb1.GetType() == t)
                {
                    return mb1;
                }
            }
            return null;
        }

        // stack to make sure we supend and resume the engine properly
        static Stack<int> engineSpeedStack = new Stack<int>();

        public bool IsEngineSuspended()
        {
            return engineSpeedStack.Count > 0;
        }

        public static void SuspendEngine()
        {
            /*
            // just pushing an int here, we won't restore it later
            engineSpeedStack.Push(engineDelay);
            if (theNeuronArray == null)
                return;

            string currentThreadName = Thread.CurrentThread.Name;
            // wait for the engine to actually stop before returning
            while (theNeuronArray != null && !engineIsPaused && currentThreadName != "EngineThread")
            {
                Thread.Sleep(100);
                System.Windows.Forms.Application.DoEvents();
            }
            */
        }

        public static void ResumeEngine()
        {
            /*
            // first pop the top to make sure we balance the suspends and resumes
            if (engineSpeedStack.Count > 0)
                engineDelay = engineSpeedStack.Pop();
            if (theNeuronArray == null)
                return;

            // resume the engine
            // on shutdown, the current application may be gone when this is requested
            if (theNeuronArray != null && Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow.thisWindow.SetSliderPosition(engineDelay);
                });
            }
            */
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            // Debug.WriteLine("Dt_tick entered");

            foreach (ModuleBase mb in MainWindow.BrainSim3Data.modules)
            {
                if (mb != null && mb.dlgIsOpen)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.Fire();
                    });
                }
            }
        }

        private void LoadModuleTypeMenu()
        {
            var moduleTypes = Utils.GetArrayOfModuleTypes();

            foreach (var moduleType in moduleTypes)
            {
                string moduleName = moduleType.Name;
                moduleName = moduleName.Replace("Module", "");
                Modules.ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(moduleType);
                string toolTip = ModuleDescriptionFile.GetToolTip(moduleType.Name);

                ModuleListComboBox.Items.Add(new System.Windows.Controls.Label { Content = moduleName, ToolTip = toolTip, Margin = new Thickness(0), Padding = new Thickness(0) });
            }
        }

    }
}
