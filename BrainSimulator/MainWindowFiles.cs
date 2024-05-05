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
            if (!theUKS.LoadUKSfromXMLFile(fileName))
            {
                theUKS = new UKS.UKS();
                return false;
            }
            currentFileName = fileName;
            SetCurrentFileNameToProperties();
            ReloadActiveModulesSP();
            ShowAllModuleDialogs();
            SetTitleBar();
            return true;
        }

        public void ReloadActiveModulesSP()
        {
            ActiveModuleSP.Children.Clear();

            var activeModules1 = theUKS.Labeled("ActiveModule").Children;
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
                ModuleBase mod = activeModules.FindFirst(x => x.Label == t.Label);
                if ( mod == null)
                    mod = CreateNewModule(moduleType);
                activeModules.Add(mod);
                CreateContextMenu(mod, tb, tb.ContextMenu);
                //                    if (mod.isEnabled)
                tb.Background = new SolidColorBrush(Colors.LightGreen);
                //                  else tb.Background = new SolidColorBrush(Colors.Pink);
                ActiveModuleSP.Children.Add(tb);
            }
        }

        private bool SaveFile(string fileName)
        {
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

        public int undoCountAtLastSave = 0;
        private bool PromptToSaveChanges()
        {
            return false;
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
            }
            else
            {
                saveFileDialog.Dispose();
                ResumeEngine();
                return false;
            }
            saveFileDialog.Dispose();
            ResumeEngine();
            return true;
        }
    }
}
