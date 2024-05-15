//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BrainSimulator.Modules;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

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
                //Reload_network.IsEnabled = true;
                //ReloadNetwork.IsEnabled = true;
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
            CloseAllModuleDialogs();
            CloseAllModules();
            this.Close();
        }


        public ModuleBase CreateNewModule(string moduleTypeLabel, string moduleLabel = "")
        {
            Type t = Type.GetType("BrainSimulator.Modules.Module" + moduleTypeLabel);
            ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(t);

            theModule.Label = moduleLabel;
            if (moduleLabel == "")
                theModule.Label = moduleTypeLabel;
            theModule.GetUKS();
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

            ReloadActiveModulesSP();
        }
        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;

            SuspendEngine();
            CloseAllModuleDialogs();
            CloseAllModules();
            UnloadActiveModules();

            CreateEmptyUKS(); // to avoid keeping too many bytes occupied...

            currentFileName = "";
            SetCurrentFileNameToProperties();

            LoadModuleTypeMenu();

            InitializeActiveModules();

            LoadMRUMenu();


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
                currentFileName = Path.GetFullPath("./UKSContent/" + fileName + ".xml");
                LoadCurrentFile();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PromptToSaveChanges();
            CloseAllModuleDialogs();
            CloseAllModules();
        }
    }

}
