using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using UKS;

namespace BrainSimulator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //TODO move these to ModuleHandler
        public List<ModuleBase> activeModules = new();
        public List<string> pythonModules = new();

        //the name of the currently-loaded network file
        public static string currentFileName = "";
        public static string pythonPath = "";
        public static ModuleHandler moduleHandler = new();
        public static UKS.UKS theUKS = moduleHandler.theUKS;
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

            //setup the python support
            pythonPath = (string)Environment.GetEnvironmentVariable("PythonPath", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(pythonPath))
            {
                var result1 = MessageBox.Show("Do you want to use Python Modules?", "Python?", MessageBoxButton.YesNo);
                if (result1 == MessageBoxResult.Yes)
                {
                    string likeliPath = (string)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    likeliPath += @"\Programs\Python";
                    System.Windows.Forms.OpenFileDialog openFileDialog = new()
                    {
                        Title = "SELECT path to Python .dll (or cancel for no Python support)",
                        InitialDirectory = likeliPath,
                    };

                    // Show the file Dialog.  
                    System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
                    // If the user clicked OK in the dialog and  
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        pythonPath = openFileDialog.FileName;
                        Environment.SetEnvironmentVariable("PythonPath", pythonPath, EnvironmentVariableTarget.User);
                    }
                    else
                    {
                        Environment.SetEnvironmentVariable("PythonPath", "", EnvironmentVariableTarget.User);
                    }
                    openFileDialog.Dispose();
                }
                else
                {
                    pythonPath = "no";
                    Environment.SetEnvironmentVariable("PythonPath", pythonPath, EnvironmentVariableTarget.User);
                }
            }
            moduleHandler.PythonPath = pythonPath;
            if (pythonPath != "no")
            {
                moduleHandler.InitPythonEngine();
            }

            //setup the input file
            string fileName = "";
            string savedFile = (string)Properties.Settings.Default["CurrentFile"];
            if (savedFile != "")
                fileName = savedFile;

            try
            {
                if (fileName != "")
                {
                    if (!LoadFile(fileName))
                    {
                        MessageBox.Show("Previous UKS File could not be opened, empty UKS initialized", "File not read", MessageBoxButton.OK);
                        CreateEmptyUKS();
                    }
                }
                else //force a new file creation on startup if no file name set
                {
                    CreateEmptyUKS();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("UKS Content not loaded");
            }

            //safety check
            if (theUKS.Labeled("BrainSim") == null)
                CreateEmptyUKS();

            UpdateModuleListsInUKS();

            LoadModuleTypeMenu();

            InitializeActiveModules();

            LoadMRUMenu();

            //start the module engine
            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            dt.Tick += Dt_Tick;
            dt.Start();
        }


        public void InitializeActiveModules()
        {
            for (int i = 0; i < activeModules.Count; i++)
            {
                ModuleBase mod = activeModules[i];
                if (mod != null)
                {
                    mod.SetUpAfterLoad();
                }
            }
        }
        public void SetupBeforeSave()
        {
            for (int i = 0; i < activeModules.Count; i++)
            {
                ModuleBase mod = activeModules[i];
                if (mod != null)
                {
                    mod.SetUpBeforeSave();
                }
            }
        }


        public void ShowAllModuleDialogs()
        {
            foreach (ModuleBase mb in activeModules)
            {
                if (mb != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.ShowDialog();
                    });
                }
            }
        }

        public void CreateEmptyUKS()
        {
            theUKS.UKSList.Clear();
            theUKS = new UKS.UKS();
            theUKS.AddThing("BrainSim", null);
            theUKS.GetOrAddThing("AvailableModule", "BrainSim");
            theUKS.GetOrAddThing("ActiveModule", "BrainSim");

            InsertMandatoryModules();
            InitializeActiveModules();
        }

        public void UpdateModuleListsInUKS()
        {
            theUKS.GetOrAddThing("BrainSim", null);
            theUKS.GetOrAddThing("AvailableModule", "BrainSim");
            theUKS.GetOrAddThing("ActiveModule", "BrainSim");
            var availableListInUKS = theUKS.Labeled("AvailableModule").Children;

            //add any missing modules
            var CSharpModules = Utils.GetListOfExistingCSharpModuleTypes();
            foreach (var module in CSharpModules)
            {
                string name = module.Name;
                //name = name.Replace("Module", "");
                Thing availableModule = availableListInUKS.FindFirst(x => x.Label == name);
                if (availableModule == null)
                    theUKS.GetOrAddThing(name, "AvailableModule");
            }
            var PythonModules = moduleHandler.GetListOfExistingPythonModuleTypes();
            foreach (var name in PythonModules)
            {
                Thing availableModule = availableListInUKS.FindFirst(x => x.Label == name);
                if (availableModule == null)
                    theUKS.GetOrAddThing(name, "AvailableModule");
            }
            //delete any non-existant modules
            availableListInUKS = theUKS.Labeled("AvailableModule").Children;
            foreach (Thing t in availableListInUKS)
            {
                string name = t.Label;
                if (CSharpModules.FindFirst(x=>x.Name == name) != null) continue;
                if (PythonModules.FindFirst(x => x == name) != null) continue;
                theUKS.DeleteAllChildren(t);
                theUKS.DeleteThing(t);
            }

            //reconnect/delete any active modules
            var activeListInUKS = theUKS.Labeled("ActiveModule").Children;
            foreach(Thing t in activeListInUKS)
            {
                Thing parent = availableListInUKS.FindFirst(x => x.Label == t.Label.Substring(0, t.Label.Length - 1));
                if (parent != null)
                    t.AddParent(parent);
                else
                    theUKS.DeleteThing(t);
            }
        }

        public void InsertMandatoryModules()
        {

            Debug.WriteLine("InsertMandatoryModules entered");
            ActivateModule("ModuleUKS");
            ActivateModule("ModuleUKSStatement");
        }

        public string ActivateModule(string moduleType)
        {
            Thing t = theUKS.GetOrAddThing(moduleType, "AvailableModule");
            t = theUKS.CreateInstanceOf(theUKS.Labeled(moduleType));
            t.AddParent(theUKS.Labeled("ActiveModule"));

            if (!moduleType.Contains(".py"))
            {
                ModuleBase newModule = CreateNewModule(moduleType);
                if (newModule == null) return "";
                newModule.Label = t.Label;
                activeModules.Add(newModule);
            }
            else
            {
                pythonModules.Add(t.Label);
            }

            ReloadActiveModulesSP();
            return t.Label;
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
            foreach (string pythonModule in pythonModules)
            {
                moduleHandler.Close(pythonModule);
            }
            pythonModules.Clear();
        }

        private void SetTitleBar()
        {
            Title = "Brain Simulator III " + System.IO.Path.GetFileNameWithoutExtension(currentFileName);
        }

        public static void SuspendEngine()
        {
        }

        public static void ResumeEngine()
        {
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            Thing activeModuleParent = theUKS.Labeled("ActiveModule");
            if (activeModuleParent == null) return;
            foreach (Thing module in activeModuleParent.Children)
            {
                ModuleBase mb = activeModules.FindFirst(x => x.Label == module.Label);
                if (mb != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.Fire();
                    });
                }
            }
            foreach (string pythonModule in pythonModules)
            {
                moduleHandler.RunScript(pythonModule);
            }
        }

        private void LoadModuleTypeMenu()
        {
            var moduleTypes = Utils.GetListOfExistingCSharpModuleTypes();

            foreach (var moduleType in moduleTypes)
            {
                string moduleName = moduleType.Name;
                theUKS.GetOrAddThing(moduleName, "AvailableModule");
            }

            var pythonModules = moduleHandler.GetListOfExistingPythonModuleTypes();
            foreach (var moduleType in pythonModules)
            {
                theUKS.GetOrAddThing(moduleType, "AvailableModule");
            }

            ModuleListComboBox.Items.Clear();
            foreach (Thing t in theUKS.Labeled("AvailableModule").Children)
            {
                ModuleListComboBox.Items.Add(new System.Windows.Controls.Label { Content = t.Label, Margin = new Thickness(0), Padding = new Thickness(0) });
            }
        }
    }
}
