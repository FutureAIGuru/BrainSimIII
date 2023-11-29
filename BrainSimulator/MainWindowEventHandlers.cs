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
using System.Windows.Input;
using System.Collections.Generic;
using BrainSimulator.Modules;
using System.Linq;

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
                // SaveFile(currentFileName);
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
                // Modules.Sallie.VideoQueue.Clear();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            //first check to see if help is already open
            Process[] theProcesses1 = Process.GetProcesses();
            Process latestProcess = null;
            for (int i = 1; i < theProcesses1.Length; i++)
            {
                try
                {
                    if (theProcesses1[i].MainWindowTitle != "")
                    {
                        if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                            latestProcess = theProcesses1[i];
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Opening Help Item Failed, Message: " + e1.Message);
                }
            }


            if (latestProcess == null)
            {
                OpenApp("https://futureai.guru/BrainSimHelp/gettingstarted.html");

                //gotta wait for the page to render before it shows in the processes list
                DateTime starttTime = DateTime.Now;

                while (latestProcess == null && DateTime.Now < starttTime + new TimeSpan(0, 0, 3))
                {
                    theProcesses1 = Process.GetProcesses();
                    for (int i = 1; i < theProcesses1.Length; i++)
                    {
                        try
                        {
                            if (theProcesses1[i].MainWindowTitle != "")
                            {
                                if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                                    latestProcess = theProcesses1[i];
                            }
                        }
                        catch (Exception e1)
                        {
                            MessageBox.Show("Opening Help Item Failed, Message: " + e1.Message);
                        }
                    }
                }
            }

            try
            {
                if (latestProcess != null)
                {
                    IntPtr id = latestProcess.MainWindowHandle;

                    Rect theRect = new Rect();
                    GetWindowRect(id, ref theRect);

                    bool retVal = MoveWindow(id, 300, 100, 1200, 700, true);
                    this.Activate();
                    SetForegroundWindow(id);
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("Opening Help Failed, Message: " + e1.Message);
            }
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

            //ensure no name collistions
            theModule.Label = MainWindow.GetUniqueModuleLabel(moduleLabel);
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
                    ModuleBase newModule = CreateNewModule(moduleName);
                    MainWindow.modules.Add(newModule);
                }
            }
            loadedModulesSP = LoadedModuleSP;
            LoadedModuleSP.Children.Clear();

            for (int i = 0; i < MainWindow.modules.Count; i++)
            {
                ModuleBase mod = MainWindow.modules[i];
                if (mod != null)
                {
                    mod.SetUpAfterLoad();
                }
            }

            ReloadLoadedModules();
        }
        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (PromptToSaveChanges())
                return;

            SuspendEngine();
            CreateEmptyNetwork(); // to avoid keeping too many bytes occupied...

            ReloadNetwork.IsEnabled = false;
            Reload_network.IsEnabled = false;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();

            currentFileName = "";
            SetCurrentFileNameToProperties();
            SetTitleBar();
                
            Update();
            Modules.Sallie.VideoQueue.Clear();
            ResumeEngine();
        }

        internal static string GetUniqueModuleLabel(string searchString)
        {
            string number = (modules.Count(module => module.Label.StartsWith(searchString + "_", StringComparison.OrdinalIgnoreCase)) + 1).ToString();            return searchString + "_" + number;
        }
    }
}
