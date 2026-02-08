/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using BrainSimulator.Modules;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UKS;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static StackPanel loadedModulesSP;
        private bool LoadFile(string fileName)
        {
            SuspendEngine();
            CloseAllModuleDialogs();
            UnloadActiveModules();

            if (!theUKS.LoadUKSfromXMLFile(fileName))
            {
                theUKS = new UKS.UKS();
                return false;
            }
            currentFileName = fileName;

            if (theUKS.Labeled("BrainSim") is null)
                CreateEmptyUKS();

            SetCurrentFileNameToProperties();

            UpdateModuleListsInUKS();
            LoadActiveModules();
            ReloadActiveModulesSP();
            ShowAllModuleDialogs();
            SetTitleBar();
            ResumeEngine();
            AddFileToMRUList(fileName); 
            return true;
        }

        public void ReloadActiveModulesSP()
        {
            ActiveModuleSP.Children.Clear();

            Thought activeModuleParent = theUKS.Labeled("ActiveModule");
            //TODO: Remove
            activeModuleParent.AddParent("BrainSim");

            if (activeModuleParent is null) { return; }
            var activeModules1 = activeModuleParent.Children;
            activeModules1 = activeModules1.OrderBy(x => x.Label).ToList();

            foreach (Thought t in activeModules1)
            {
                //what kind of module is this?
                Thought t1 = t.Parents.FindFirst(x => x.HasAncestor("AvailableModule"));
                if (t1 is null) continue;
                string moduleType = t1.Label;

                TextBlock tb = new TextBlock();
                tb.Text = t.Label;
                tb.Margin = new Thickness(5, 2, 5, 2);
                tb.Padding = new Thickness(10, 3, 10, 3);
                tb.ContextMenu = new ContextMenu();
                if (moduleType.Contains(".py"))
                { }
                else
                {
                    ModuleBase mod = activeModules.FindFirst(x => x.Label == t.Label);
                    CreateContextMenu(mod, tb, tb.ContextMenu);
                }
                tb.Background = new SolidColorBrush(Colors.LightGreen);
                ActiveModuleSP.Children.Add(tb);
            }
        }
        void UnloadActiveModules()
        {
            Thought activeModulesParent = theUKS.Labeled("ActiveModule");
            if (activeModulesParent is null) return;
            var activeModules1 = activeModulesParent.Children;

            foreach (Thought t in activeModules1)
            {
                for (int i = 0; i < t.LinksTo.Count; i++)
                {
                    Thought r = t.LinksTo[i];
                    theUKS.DeleteThought(r.To);
                    t.RemoveLink(r);
                }
                theUKS.DeleteThought(t);
            }
        }

        void LoadActiveModules()
        {
            activeModules.Clear();
            pythonModules.Clear();
            moduleHandler.pythonModules.Clear();
            moduleHandler.activePythonModules.Clear();


            var activeModules1 = theUKS.Labeled("ActiveModule").Children;
            activeModules1 = activeModules1.OrderBy(x => x.Label).ToList();

            foreach (Thought t in activeModules1)
            {
                //what kind of module is this?
                Thought tModuleType = t.Parents.FindFirst(x => x.HasAncestor("AvailableModule"));
                if (tModuleType is null) continue;
                string moduleType = tModuleType.Label;

                if (moduleType.Contains(".py"))
                {
                    pythonModules.Add(t.Label);
                }
                else
                {
                    ModuleBase mod = CreateNewModule(moduleType, t.Label);
                    if (mod is not null) 
                        activeModules.Add(mod);
                    else
                    {
                        theUKS.Labeled("ActiveModule").RemoveChild(t);
                    }
                }
            }
        }
        private bool SaveFile(string fileName)
        {
            Save();
            AddFileToMRUList(fileName);
            return true;
        }

        private void AddFileToMRUList(string filePath)
        {
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList is null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            MRUList.Insert(0, filePath); //add it to the top of the list
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
            LoadMRUMenu();
        }
        public static void RemoveFileFromMRUList(string filePath)
        {
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList is null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
        }
        private void LoadMRUMenu()
        {
            MRUListMenu.Items.Clear();
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList is null)
                MRUList = new StringCollection();
            foreach (string fileItem in MRUList)
            {
                if (fileItem is null) continue;
                string shortName = Path.GetFileNameWithoutExtension(fileItem);
                MenuItem mi = new MenuItem() { Header = shortName };
                mi.Click += buttonLoad_Click;
                mi.ToolTip = fileItem;
                MRUListMenu.Items.Add(mi);
            }
        }

        private void LoadCurrentFile()
        {
            LoadFile(currentFileName);
        }

        private static void SetCurrentFileNameToProperties()
        {
            Properties.Settings.Default["CurrentFile"] = currentFileName;
            Properties.Settings.Default.Save();
        }

        private bool PromptToSaveChanges()
        {
            var result = MessageBox.Show("Save current UKS content first?", "Save?", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
                return false;
            if (result == MessageBoxResult.Yes)
                Save();
            return true;
        }

        private bool Save()
        {
            SetupBeforeSave();
            return theUKS.SaveUKStoXMLFile(currentFileName);
        }
        private bool SaveAs()
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleUKSFileSave,
                InitialDirectory = Utils.GetOrAddLocalSubFolder(Directory.GetCurrentDirectory() + "\\" + Utils.UKSContentFolder),
            };

            // Show the file Dialog.  
            // If the user clicked OK in the dialog and  
            System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                MainWindow.SuspendEngine();
                currentFileName = saveFileDialog.FileName;
                theUKS.SaveUKStoXMLFile(currentFileName);
                AddFileToMRUList(currentFileName);
                SetCurrentFileNameToProperties();

                SetTitleBar();
                ResumeEngine();
            }
            else
            {
                saveFileDialog.Dispose();
                return false;
            }
            saveFileDialog.Dispose();
            ResumeEngine();
            return true;
        }
    }
}
