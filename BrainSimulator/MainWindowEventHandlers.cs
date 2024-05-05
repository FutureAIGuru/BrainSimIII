//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using BrainSimulator.Modules;
using System.Linq;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool WasClosed = false;

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentFileName.Length == 0)
            {
                buttonSaveAs_Click(null, null);
            }
            else
            {
                SaveFile(currentFileName);
            }
        }

        private void buttonSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAs())
            {
                SaveButton.IsEnabled = true;
                Reload_network.IsEnabled = true;
                ReloadNetwork.IsEnabled = true;
            }
        }

        private void buttonReloadNetwork_click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;
            
            if (currentFileName != "")
            {
                LoadCurrentFile();
                ShowAllModuleDialogs();
                // Modules.Sallie.VideoQueue.Clear();
            }
        }

        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            MainWindow.WasClosed = true;
            CloseAllModuleDialogs();
            CloseAllModules();
            this.Close();
        }


        //This opens an app depending on the assignments of the file extensions in Windows
        public static Process OpenApp(string fileName)
        {
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            return p;
        }

        private ModuleBase CreateNewModule(string moduleLabel)
        {
            Type t = Type.GetType("BrainSimulator.Modules.Module" + moduleLabel);
            ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(t);

            theModule.Label = moduleLabel;
            return theModule;
        }
        private void ModuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                if (cb.SelectedItem != null)
                {
                    string moduleName = ((Label)cb.SelectedItem).Content.ToString();
                    cb.SelectedIndex = -1;
                    ActivateModule(moduleName);
                }
            }

            //for (int i = 0; i < MainWindow.BrainSim3Data.modules.Count; i++)
            //{
            //    ModuleBase mod = MainWindow.BrainSim3Data.modules[i];
            //    if (mod != null)
            //    {
            //        mod.SetUpAfterLoad();
            //    }
            //}

            ReloadActiveModulesSP();
        }
        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;

            SuspendEngine();
            CreateEmptyUKS(); // to avoid keeping too many bytes occupied...

            ReloadNetwork.IsEnabled = false;
            Reload_network.IsEnabled = false;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();

            currentFileName = "";
            SetCurrentFileNameToProperties();
            SetTitleBar();
                
            ResumeEngine();
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;
            string fileName = "_Open";
            if (sender is MenuItem mainMenu)
                fileName = (string)mainMenu.Header;

            if (fileName == "_Open")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Filter = Utils.FilterXMLs,
                    Title = Utils.TitleUKSFileLoad,
                };
                // Show the Dialog.  
                // If the user clicked OK in the dialog and  
                Nullable<bool> result = openFileDialog1.ShowDialog();
                if (result ?? false)
                {
                    currentFileName = openFileDialog1.FileName;
                    LoadCurrentFile();
                }
            }
            else
            {
                //this is a file name from the File menu
                currentFileName = Path.GetFullPath("./Networks/" + fileName + ".xml");
                LoadCurrentFile();
            }
        }
    }
}
