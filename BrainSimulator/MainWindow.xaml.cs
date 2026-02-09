/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
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
            if (theUKS.Labeled("BrainSim") is null)
                CreateEmptyUKS();

            UpdateModuleListsInUKS();

            LoadModuleTypeMenu();

            InitializeActiveModules();

            LoadMRUMenu();

            //start the module engine
            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.001);
            dt.Tick += Dt_Tick;
            dt.Start();
        }


        public void InitializeActiveModules()
        {
            for (int i = 0; i < activeModules.Count; i++)
            {
                ModuleBase mod = activeModules[i];
                if (mod is not null)
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
                if (mod is not null)
                {
                    mod.SetUpBeforeSave();
                }
            }
        }


        public void ShowAllModuleDialogs()
        {
            foreach (ModuleBase mb in activeModules)
            {
                if (mb is not null)
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
            theUKS.AllThoughts.Clear();
            theUKS = new UKS.UKS();
            theUKS.CreateInitialStructure();  //creates the "thought" substructure

            if (theUKS.Labeled("BrainSim") is null)
                theUKS.AddThought("BrainSim", null);
            theUKS.GetOrAddThought("AvailableModule", "BrainSim");
            theUKS.GetOrAddThought("ActiveModule", "BrainSim");

            InsertMandatoryModules();
            InitializeActiveModules();
        }

        public void UpdateModuleListsInUKS()
        {
            theUKS.GetOrAddThought("BrainSim", null);
            theUKS.GetOrAddThought("AvailableModule", "BrainSim");
            theUKS.GetOrAddThought("ActiveModule", "BrainSim");
            var availableListInUKS = theUKS.Labeled("AvailableModule").Children;

            //add any missing modules
            var CSharpModules = Utils.GetListOfExistingCSharpModuleTypes();
            foreach (var module in CSharpModules)
            {
                string name = module.Name;
                Thought availableModule = availableListInUKS.FindFirst(x => x.Label == name);
                if (availableModule is null)
                    theUKS.GetOrAddThought(name, "AvailableModule");
            }
            var PythonModules = moduleHandler.GetListOfExistingPythonModuleTypes();
            foreach (var name in PythonModules)
            {
                Thought availableModule = availableListInUKS.FindFirst(x => x.Label == name);
                if (availableModule is null)
                    theUKS.GetOrAddThought(name, "AvailableModule");
            }
            //delete any non-existant modules
            availableListInUKS = theUKS.Labeled("AvailableModule").Children;
            foreach (Thought t in availableListInUKS)
            {
                string name = t.Label;
                if (CSharpModules.FindFirst(x=>x.Name == name) is not null) continue;
                if (PythonModules.FindFirst(x => x == name) is not null) continue;
                theUKS.DeleteAllChildren(t);
                theUKS.DeleteThought(t);
            }

            //reconnect/delete any active modules
            var activeListInUKS = theUKS.Labeled("ActiveModule").Children;
            foreach(Thought t in activeListInUKS)
            {
                Thought parent = availableListInUKS.FindFirst(x => x.Label == t.Label.Substring(0, t.Label.Length - 1));
                if (parent is not null)
                    t.AddParent(parent);
                else
                    theUKS.DeleteThought(t);
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
            Thought t = theUKS.GetOrAddThought(moduleType, "AvailableModule");
            t = theUKS.CreateInstanceOf(theUKS.Labeled(moduleType));
            t.AddParent(theUKS.Labeled("ActiveModule"));

            if (!moduleType.Contains(".py"))
            {
                ModuleBase newModule = CreateNewModule(moduleType);
                if (newModule is null) return "";
                newModule.Label = t.Label;
                activeModules.Add(newModule);
                newModule.OpenDlg();
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
                    if (md is not null)
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
                    if (mb is not null)
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

        //THIS IS THE MAIN ENGINE LOOP
        private void Dt_Tick(object? sender, EventArgs e)
        {
            Thought activeModuleParent = theUKS.Labeled("ActiveModule");
            if (activeModuleParent is null) return;
            foreach (Thought module in activeModuleParent.Children)
            {
                ModuleBase mb = activeModules.FindFirst(x => x.Label == module.Label);
                if (mb is not null)
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
                Thought t = theUKS.GetOrAddThought(moduleName, "AvailableModule");
                //TODO: delete the following
                t.AddParent("AvailableModule");
            }

            var pythonModules = moduleHandler.GetListOfExistingPythonModuleTypes();
            foreach (var moduleType in pythonModules)
            {
                theUKS.GetOrAddThought(moduleType, "AvailableModule");
            }

            ModuleListComboBox.Items.Clear();
            ModuleListComboBox.FontSize = 18;
            foreach (Thought t in theUKS.Labeled("AvailableModule").Children)
            {
                //ModuleListComboBox.Items.Add(new System.Windows.Controls.Label { Content = t.Label, Margin = new Thickness(0), Padding = new Thickness(0) });
                ModuleListComboBox.Items.Add(t.Label);
            }
            ModuleListComboBox.Items.SortDescriptions.Add(
                new System.ComponentModel.SortDescription(
                    "", // empty = sort by the item itself (e.g., string)
                    System.ComponentModel.ListSortDirection.Ascending));
        }
    }
}
