using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public List<ModuleBase> activeModules = new();
        public List<string> pythonModules = new();

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
            string fileName = "";  
            string savedFile = (string)Properties.Settings.Default["CurrentFile"];
            if (savedFile != "")
                fileName = savedFile;

            //setup the python path
            pythonPath = (string)Properties.Settings.Default["PythonPath"];
            if (string.IsNullOrEmpty(pythonPath) && pythonPath != "No Python")
            {
                MessageBox.Show("Do you want Python Modules?");
                string likeliPath = (string)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                likeliPath += @"\Programs\Python";
                System.Windows.Forms.OpenFileDialog openFileDialog = new()
                {
                    Filter = Utils.FilterXMLs,
                    Title = "SELECT path to Python .dll (or cancel for no Python support)",
                    InitialDirectory = likeliPath,
                };

                // Show the file Dialog.  
                System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
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


            try
            {
                if (fileName != "")
                {
                    if (!LoadFile(fileName))
                        CreateEmptyUKS();
                }
                else //force a new file creation on startup if no file name set
                {
                    CreateEmptyUKS();
                }
            }
            catch (Exception ex) { 
                System.Windows.MessageBox.Show("UKS Content not loaded"); }

            //safety check
            if (theUKS.Labeled("BrainSim") == null)
                CreateEmptyUKS();

            LoadModuleTypeMenu();

            InitializeActiveModules();

            LoadMRUMenu();

            //start the module engine
            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            dt.Tick += Dt_Tick;
            dt.Start();
        }

        void LoadActiveModuleSP()
        {
            ActiveModuleSP.Children.Clear();
            var activeModules = theUKS.GetOrAddThing("ActiveModule").Children;
            foreach (Thing t in activeModules)
                ActiveModuleSP.Children.Add(new System.Windows.Controls.Label { Content = t.Label});
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
            theUKS = new UKS.UKS();
            theUKS.AddThing("BrainSim", null);
            theUKS.GetOrAddThing("AvailableModule", "BrainSim");
            theUKS.GetOrAddThing("ActiveModule", "BrainSim");

            InsertMandatoryModules();
            InitializeActiveModules();
        }

        public void InsertMandatoryModules()
        {

            Debug.WriteLine("InsertMandatoryModules entered");
            ActivateModule("UKS");
            ActivateModule("UKSStatement");
        }

        public string ActivateModule(string moduleName)
        {
            Thing t = theUKS.GetOrAddThing(moduleName, "AvailableModule");
            t = theUKS.CreateInstanceOf(theUKS.Labeled(moduleName));
            t.AddParent(theUKS.Labeled("ActiveModule"));

            if (!moduleName.Contains(".py"))
            {
                ModuleBase newModule = CreateNewModule(moduleName);
                newModule.Label = t.Label;
                activeModules.Add(newModule);
            }
            else
            {
                theUKS.AddStatement(t.Label, "is-a", "ActiveModule");
                pythonModules.Add(t.Label);
            }

            LoadActiveModuleSP();
            return t.Label;
        }

        public ModuleBase CreateNewUniqueModule(string ModuleName)
        {
            ModuleBase newModule = CreateNewModule(ModuleName);
            newModule.Label = GetUniqueModuleLabel(ModuleName);
            return newModule;
        }
        internal string GetUniqueModuleLabel(string searchString)
        {
            var existing = activeModules.FindAll(module => module.Label.StartsWith(searchString));
            if (existing == null || existing.Count == 0) return searchString;
            return searchString + "_" + existing.Count;
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
            foreach (string pythonModule in pythonModules)
            {
                string module = pythonModule.Replace(".py", "");
                RunScript(module);
            }
        }

        private void LoadModuleTypeMenu()
        {
            var moduleTypes = Utils.GetArrayOfModuleTypes();

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
