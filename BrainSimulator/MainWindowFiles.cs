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
using System.Threading.Tasks;
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
            //CloseAllModuleDialogs();
            //CloseAllModules();
            //SuspendEngine();

            //bool success = false;
            //await Task.Run(delegate { success = XmlFile.Load(fileName); });
            //if (!success)
            //{
            //    CreateEmptyNetwork();
            //    Properties.Settings.Default["CurrentFile"] = currentFileName;
            //    Properties.Settings.Default.Save();
            //    ResumeEngine();
            //    return;
            //}
            //currentFileName = fileName;
            //Properties.Settings.Default["CurrentFile"] = currentFileName;
            //Properties.Settings.Default.Save();

            //ReloadNetwork.IsEnabled = true;
            //Reload_network.IsEnabled = true;
            ////if (XmlFile.CanWriteTo(currentFileName))
            ////    SaveButton.IsEnabled = true;
            ////else
            ////    SaveButton.IsEnabled = false;
            //SetTitleBar();
            //// await Task.Delay(1000).ContinueWith(t => ShowDialogs());
            //loadedModulesSP = LoadedModuleSP;
            //LoadedModuleSP.Children.Clear();

            //for (int i = 0; i < MainWindow.BrainSim3Data.modules.Count; i++)
            //{
            //    ModuleBase mod = MainWindow.BrainSim3Data.modules[i];
            //    if (mod != null)
            //    {
            //        mod.SetUpAfterLoad();
            //    }
            //}

            if (!theUKS.LoadUKSfromXMLFile(fileName))
            {
                theUKS = new UKS.UKS();
                return false;
            }
            Properties.Settings.Default["CurrentFile"] = currentFileName;
            Properties.Settings.Default.Save();
            ReloadActiveModulesSP();
            ShowAllModuleDialogs();
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
                activeModules.Add(mod);
                CreateContextMenu(mod, tb, tb.ContextMenu);
                //                    if (mod.isEnabled)
                tb.Background = new SolidColorBrush(Colors.LightGreen);
                //                  else tb.Background = new SolidColorBrush(Colors.Pink);
                ActiveModuleSP.Children.Add(tb);
            }
        }

        private void AddModuleToLoadedModules(int i, ModuleBase mod)
        {
            TextBlock tb = new TextBlock();
            tb.Text = mod.Label;
            tb.Margin = new Thickness(5, 2, 5, 2);
            tb.Padding = new Thickness(10, 3, 10, 3);
            tb.ContextMenu = new ContextMenu();
            CreateContextMenu(mod, tb, tb.ContextMenu);
            if (mod.isEnabled) tb.Background = new SolidColorBrush(Colors.LightGreen);
            else tb.Background = new SolidColorBrush(Colors.Pink);
            loadedModulesSP.Children.Add(tb);
        }

        private bool SaveFile(string fileName)
        {
            return true;
            //    SuspendEngine();
            //    //If the path contains "bin\64\debug" change the path to the actual development location instead
            //    //because file in bin..debug can be clobbered on every rebuild.
            //    if (fileName.ToLower().Contains("bin\\debug\\net6.0-windows"))
            //    {
            //        MessageBoxResult mbResult = System.Windows.MessageBox.Show(this, "Save to source folder instead?", "Save", MessageBoxButton.YesNoCancel,
            //        MessageBoxImage.Asterisk, MessageBoxResult.No);
            //        if (mbResult == MessageBoxResult.Yes)
            //            fileName = fileName.ToLower().Replace("bin\\debug\\net6.0-windows\\", "");
            //        if (mbResult == MessageBoxResult.Cancel)
            //            return false;
            //    }

            //    foreach (ModuleBase mod in BrainSim3Data.modules)
            //    {
            //        try
            //        {
            //            mod.SetUpBeforeSave();
            //        }
            //        catch (Exception e)
            //        {
            //            MessageBox.Show("SetupBeforeSave failed on module " + mod.Label + ".   Message: " + e.Message);
            //        }
            //    }

            //    if (XmlFile.Save(fileName))
            //    {
            //        currentFileName = fileName;
            //        SetCurrentFileNameToProperties();
            //        ResumeEngine();
            //        return true;
            //    }
            //    else
            //    {
            //        ResumeEngine();
            //        return false;
            //    }
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
            return true;
            //            bool canWrite = XmlFile.CanWriteTo(currentFileName, out string message);

            //SuspendEngine();

            //bool retVal = false;
            //MessageBoxResult mbResult = System.Windows.MessageBox.Show(this, "Do you want to save changes?", "Save", MessageBoxButton.YesNoCancel,
            //MessageBoxImage.Asterisk, MessageBoxResult.No);
            //if (mbResult == MessageBoxResult.Yes)
            //{
            //    if (currentFileName.Length != 0 && canWrite)
            //    {
            //        SaveFile(currentFileName);
            //    }
            //    else
            //    {
            //        if (!SaveAs())
            //        {
            //            retVal = true;
            //        }
            //    }
            //}
            //if (mbResult == MessageBoxResult.Cancel)
            //{
            //    retVal = true;
            //}
            //ResumeEngine();
            //return retVal;
        }

        private bool SaveAs()
        {
            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            defaultPath += "\\BrainSim";
            try
            {
                if (Directory.Exists(defaultPath)) defaultPath = "";
                else Directory.CreateDirectory(defaultPath);
            }
            catch
            {
                //maybe myDocuments is readonly of offline? let the user do whatever they want
                defaultPath = "";
            }
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleUKSFileSave,
                InitialDirectory = defaultPath
            };

            // Show the Dialog.  
            // If the user clicked OK in the dialog and  
            Nullable<bool> result = saveFileDialog1.ShowDialog();
            if (result ?? false)// System.Windows.Forms.DialogResult.OK)
            {
                if (SaveFile(saveFileDialog1.FileName))
                {
                    AddFileToMRUList(currentFileName);
                    SetTitleBar();
                    return true;
                }
            }
            return false;
        }
    }
}
