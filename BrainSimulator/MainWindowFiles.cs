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

            if (theUKS.Labeled("BrainSim") == null)
                CreateEmptyUKS();

            SetCurrentFileNameToProperties();
            LoadActiveModules();
            ReloadActiveModulesSP();
            ShowAllModuleDialogs();
            SetTitleBar();
            ResumeEngine();
            return true;
        }

        public void ReloadActiveModulesSP()
        {
            ActiveModuleSP.Children.Clear();

            Thing activeModuleParent = theUKS.Labeled("ActiveModule");
            if (activeModuleParent == null) { return; }
            var activeModules1 = activeModuleParent.Children;
            activeModules1 = activeModules1.OrderBy(x => x.Label).ToList();

            foreach (Thing t in activeModules1)
            {
                //what kind of module is this?
                Thing t1 = t.Parents.FindFirst(x => x.HasAncestorLabeled("AvailableModule"));
                if (t1 == null) continue;
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
            Thing activeModulesParent = theUKS.Labeled("ActiveModule");
            if (activeModulesParent == null) return;
            var activeModules1 = activeModulesParent.Children;

            foreach (Thing t in activeModules1)
            {
                for (int i = 0; i < t.Relationships.Count; i++)
                {
                    Relationship r = t.Relationships[i];
                    theUKS.DeleteThing(r.target);
                    t.RemoveRelationship(r);
                }
                theUKS.DeleteThing(t);
            }
        }

        void LoadActiveModules()
        {
            activeModules.Clear();
            pythonModules.Clear();

            var activeModules1 = theUKS.Labeled("ActiveModule").Children;
            activeModules1 = activeModules1.OrderBy(x => x.Label).ToList();

            foreach (Thing t in activeModules1)
            {
                //what kind of module is this?
                Thing tModuleType = t.Parents.FindFirst(x => x.HasAncestorLabeled("AvailableModule"));
                if (tModuleType == null) continue;
                string moduleType = tModuleType.Label;

                if (moduleType.Contains(".py"))
                {
                    pythonModules.Add(t.Label);
                }
                else
                {
                    ModuleBase mod = CreateNewModule(moduleType, t.Label);
                    activeModules.Add(mod);
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
            if (MRUList == null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            MRUList.Insert(0, filePath); //add it to the top of the list
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
        }
        public static void RemoveFileFromMRUList(string filePath)
        {
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList == null)
                MRUList = new StringCollection();
            MRUList.Remove(filePath); //remove it if it's already there
            Properties.Settings.Default["MRUList"] = MRUList;
            Properties.Settings.Default.Save();
        }
        private void LoadMRUMenu()
        {
            MRUListMenu.Items.Clear();
            StringCollection MRUList = (StringCollection)Properties.Settings.Default["MRUList"];
            if (MRUList == null)
                MRUList = new StringCollection();
            foreach (string fileItem in MRUList)
            {
                if (fileItem == null) continue;
                string shortName = Path.GetFileNameWithoutExtension(fileItem);
                MenuItem mi = new MenuItem() { Header = shortName };
                mi.Click += buttonLoad_Click;
                //mi.Click += MRUListItem_Click;
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
            var result = MessageBox.Show("Save before loading?", "Save", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                Save();
            }
            return false;
        }

        private bool Save()
        {
            return theUKS.SaveUKStoXMLFile(currentFileName);
        }
        private bool SaveAs()
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleUKSFileSave,
                InitialDirectory = Utils.GetOrAddLocalSubFolder(Directory.GetCurrentDirectory()+"\\"+Utils.UKSContentFolder),
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
