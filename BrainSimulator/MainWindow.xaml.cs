using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Threading;
using System.Xml.Serialization;
using UKS;

namespace BrainSimulator
{
    public class BrainSim3Data
    {
        public List<string> pythonModules = new();
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static BrainSim3Data BrainSim3Data = new BrainSim3Data();

        public List<ModuleBase> activeModules = new List<ModuleBase>();

        //the name of the currently-loaded network file
        public static string currentFileName = "";
        public static string pythonPath = "";
        public static UKS.UKS theUKS = new UKS.UKS();
        public static MainWindow theWindow = null;

        public MainWindow()
        {
            InitializeComponent();

            SetTitleBar();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            theWindow = this;
            string fileName = Directory.GetCurrentDirectory() + @"\networks\UKS\demo.xml";  
            string savedFile = (string)Properties.Settings.Default["CurrentFile"];
            if (savedFile != "")
                fileName = savedFile;

            //setup the python path
            pythonPath = (string)Properties.Settings.Default["PythonPath"];
            if (string.IsNullOrEmpty(pythonPath) && pythonPath != "No Python")
            {
                string likeliPath = (string)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                likeliPath += @"\Programs\Python";
                System.Windows.Forms.OpenFileDialog openFileDialog = new()
                {
                    Filter = Utils.FilterXMLs,
                    Title = "SELECT path to Python .dll (or cancel for no Python support)",
                    InitialDirectory = likeliPath,
                };

                // Show the file Dialog.  
                DialogResult result = openFileDialog.ShowDialog();
                // If the user clicked OK in the dialog and  
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    pythonPath = openFileDialog.FileName;
                    Properties.Settings.Default["PythonPath"] = pythonPath;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Properties.Settings.Default["PythonPath"] = "No Python";
                    Properties.Settings.Default.Save();
                }
                openFileDialog.Dispose();
            }

            LoadModuleTypeMenu();

            try
            {
                if (fileName != "")
                {
                    LoadFile(fileName);
                }
                else //force a new file creation on startup if no file name set
                {
                    CreateEmptyNetwork();
                }
            }
            catch (Exception ex) { }

            InitializeModulePane();

            ShowAllModuleDialogs();

            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            dt.Tick += Dt_Tick;
            dt.Start();
        }

        public void InitializeModulePane()
        {
            loadedModulesSP = LoadedModuleSP;
            LoadedModuleSP.Children.Clear();

            for (int i = 0; i < activeModules.Count; i++)
            {
                ModuleBase mod = activeModules[i];
                if (mod != null)
                {
                    mod.SetUpAfterLoad();
                }
            }

            ReloadLoadedModules();
        }

        public void ShowAllModuleDialogs()
        {
            foreach (ModuleBase mb in activeModules)
            {
                if (mb != null && mb.dlgIsOpen)
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
            theUKS.GetOrAddThing("UKS", "ActiveModule");
            theUKS.GetOrAddThing("AddStatement", "ActiveModule");

            activeModules.Clear();
            activeModules.Add("UKS");
            activeModules.Add(CreateNewUniqueModule("UKSStatement"));
        }


        public void CloseAllModuleDialogs()
        {
            lock (activeModules)
            {
                foreach (ModuleBase md in activeModules)
                {
                    if (md != null)
                    {
                        md.CloseDlg();
                    }
                }
            }
        }

        public void CloseAllModules()
        {
            lock (activeModules)
            {
                foreach (ModuleBase mb in activeModules)
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
            foreach (ModuleBase mb1 in activeModules)
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
        }

        public static void ResumeEngine()
        {
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            foreach (Thing module in theUKS.Labeled("ActiveModule").Children)
//            foreach (ModuleBase mb in MainWindow.BrainSim3Data.modules)
            {
                ModuleBase mb = activeModules.FindFirst(x => x.Label == module.Label); 
                if (mb != null && mb.dlgIsOpen)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.Fire();
                    });
                }
            }
            foreach (string pythonModule in BrainSim3Data.pythonModules)
            {
                string module = pythonModule.Replace(".py", "");
                RunScript(module);
            }
        }

        private void LoadModuleTypeMenu()
        {
            var moduleTypes = Utils.GetArrayOfModuleTypes();

            theUKS.GetOrAddThing("BrainSim", null);
            theUKS.GetOrAddThing("AvailableModule", "BrainSim");
            theUKS.GetOrAddThing("ActiveModule", "BrainSim");

            foreach (var moduleType in moduleTypes)
            {
                string moduleName = moduleType.Name;
                moduleName = moduleName.Replace("Module", "");
                theUKS.GetOrAddThing(moduleName, "AvailableModule");
            }

            var pythonModules = GetPythonModules();
            foreach (var moduleType in pythonModules)
            {
                theUKS.GetOrAddThing(moduleType, "AvailableModule");
            }

            foreach (Thing t in theUKS.Labeled("AvailableModule").Children)
            {
                ModuleListComboBox.Items.Add(new System.Windows.Controls.Label { Content = t.Label, Margin = new Thickness(0), Padding = new Thickness(0) });
            }
        }
    }
}
