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

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void DisplayUpdate_TimerTick(object sender, EventArgs e)
        {
            updateDisplay = true;

            //Debug.WriteLine("Display Update " + DateTime.Now);

            //this hack is here so that the shift key can be trapped before the window is activated
            //which is important for debugging so the zoom/pan will work on the first try
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && !shiftPressed && mouseInWindow)
            {
                Debug.WriteLine("Left Shift Pressed in display timer");
                shiftPressed = true;
                // theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
                //Activate();
            }
            else if ((Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) && shiftPressed && mouseInWindow)
            {
                Debug.WriteLine("Left Shift released in display timer");
                shiftPressed = false;
            }
        }


        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //Debug.WriteLine("Window_KeyUp");
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrlPressed = false;
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftPressed = false;
                // if (Mouse.LeftButton != MouseButtonState.Pressed)
                //     theNeuronArrayView.theCanvas.Cursor = Cursors.Cross;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                ctrlPressed = true;
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                shiftPressed = true;
                // theNeuronArrayView.theCanvas.Cursor = Cursors.Hand;
            }
            if (e.Key == Key.F1)
            {
                MenuItemHelp_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.O)
            {
                buttonLoad_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.N)
            {
                button_FileNew_Click(null, null);
            }
            if (ctrlPressed && e.Key == Key.S)
            {
                buttonSave_Click(null, null);
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
        }


        private void buttonSaveAs_Click(object sender, RoutedEventArgs e)
        {
        }

        private void buttonReloadNetwork_click(object sender, RoutedEventArgs e)
        {
        }

        private void NeuronMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                string id = mi.Header.ToString();
                //is it a neuron label?
                id = id.Substring(id.LastIndexOf('(') + 1);
                id = id.Substring(0, id.Length - 1);
                if (int.TryParse(id, out int nID))
                {
                    // theNeuronArrayView.targetNeuronIndex = nID;
                    // theNeuronArrayView.PanToNeuron(nID);
                }
            }
        }
        private void ModuleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                string id = mi.Header.ToString();
                for (int i = 0; i < theNeuronArray.modules.Count; i++)
                {
                    if (theNeuronArray.modules[i].Label == id)
                    {
                        // theNeuronArrayView.PanToNeuron(theNeuronArray.modules[i].FirstNeuron);
                        break;
                    }
                }
            }
        }

        private void ModuleMenu_RightClick(object sender, RoutedEventArgs e)
        {
            if ( sender is MenuItem mi )
            {
                string id = mi.Header.ToString();
                foreach (ModuleView mv in theNeuronArray.modules)
                {
                    if ( mv.Label == id )
                    {
                        mv.TheModule.ShowDialog();
                        break;
                    }
                }
            }
        }

        private void MRUListItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button_ClipboardSave_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button_FileNew_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            MainWindow.WasClosed = true;
            this.Close();
        }

        //Set the engine speed
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = sender as Slider;
            int value = (int)s.Value;
            int interval = 0;
            if (value == 0) interval = 1000;
            if (value == 1) interval = 1000;
            if (value == 2) interval = 500;
            if (value == 3) interval = 250;
            if (value == 4) interval = 100;
            if (value == 5) interval = 50;
            if (value == 6) interval = 10;
            if (value == 7) interval = 5;
            if (value == 8) interval = 2;
            if (value == 9) interval = 1;
            if (value > 9)
                interval = 0;
            engineDelay = interval;
            if (theNeuronArray != null)
                theNeuronArray.EngineSpeed = interval;
            if (!engineThread.IsAlive)
                engineThread.Start();
            displayUpdateTimer.Start();
            if (engineSpeedStack.Count > 0)
            {//if there is a stack entry, the engine is paused...put the new value on the stack
                engineSpeedStack.Pop();
                engineSpeedStack.Push(engineDelay);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //close any help window which was open.
            Process[] theProcesses1 = Process.GetProcesses();
            for (int i = 1; i < theProcesses1.Length; i++)
            {
                try
                {
                    if (theProcesses1[i].MainWindowTitle != "")
                    {
                        if (theProcesses1[i].MainWindowTitle.Contains("GettingStarted"))
                        {
                            theProcesses1[i].CloseMainWindow(); ;
                        }
                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Window Clopsing Failed, Message: " + e1.Message);
                }
            }


            if (theNeuronArray != null)
            {
                {
                    SuspendEngine();

                    engineIsCancelled = true;
                    CloseAllModuleDialogs();
                }
            }
            else
            {
                engineIsCancelled = true;
            }
        }
        private void MenuItem_MoveNeurons(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.MoveNeurons();
        }
        private void MenuItem_Undo(object sender, RoutedEventArgs e)
        {
            // theNeuronArray.Undo();
            // theNeuronArrayView.Update();
        }

        private void MenuItem_CutNeurons(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.CutNeurons();
        }
        private void MenuItem_CopyNeurons(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.CopyNeurons();
        }
        private void MenuItem_PasteNeurons(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.PasteNeurons();
        }
        private void MenuItem_DeleteNeurons(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.DeleteSelection();
            Update();
        }
        private void MenuItem_ClearSelection(object sender, RoutedEventArgs e)
        {
            // theNeuronArrayView.ClearSelection();
            Update();
        }
        private void Button_HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            HelpAbout dlg = new HelpAbout
            {
                Owner = this
            };
            dlg.Show();
        }

        //this reloads the file which was being used on the previous run of the program
        //or creates a new one
        private void Window_ContentRendered(object sender, EventArgs e)
        {
#if DEBUG
            //if the left shift key is pressed, don't load the file
            if (Keyboard.IsKeyUp(Key.LeftShift))
            {
                try
                {
                    string fileName = ""; //if the load is successful, currentfile will be set by the load process
                    if (App.StartupString != "")
                        fileName = App.StartupString;
                    if (fileName == "")
                        fileName = (string)Properties.Settings.Default["CurrentFile"];
                    if (fileName != "")
                    {
                        // LoadFile(fileName);
                        // NeuronView.OpenHistoryWindow();
                    }
                    else //force a new file creation on startup if no file name set
                    {
                        CreateEmptyNetwork();
                    }
                }
                //various errors might have happened so we'll just ignore them all and start with a fresh file 
                catch (Exception e1)
                {
                    e1.GetType();
                    MessageBox.Show("Error encountered in file load: " + e1.Message);
                    CreateEmptyNetwork();
                }
            }

            //if control is pressed, dont start the engine
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                SuspendEngine();
                // theNeuronArrayView.UpdateNeuronColors();
                engineIsPaused = true;
                //pause the neuron array when it is initialized
                pauseTheNeuronArray = true;
            }
#else
            LoadFile(Path.GetFullPath("." + "\\Networks\\StandardNetworkUI.xml"));
            NeuronView.OpenHistoryWindow();
#endif
            LoadModuleTypeMenu();
        }
        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            if (IsArrayEmpty()) return;
            SuspendEngine();
            lock (theNeuronArray.Modules)
            {
                foreach (ModuleView na in theNeuronArray.Modules)
                {
                    if (na.TheModule != null)
                        na.TheModule.Initialize();
                }
            }
            //TODO: doing this messes up because LastFired is not reset
            //            theNeuronArray.Generation = 0;
            //            theNeuronArray.SetGeneration(0);
            // theNeuronArrayView.Update();
            ResumeEngine();
        }


        //This is here so we can capture the shift key to do a pan whenever the mouse in in the window
        bool mouseInWindow = false;
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("MainWindow MouseEnter");
            //Keyboard.ClearFocus();
            //Keyboard.Focus(this);
            //this.Focus();
            mouseInWindow = true;
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("MainWindow MouseLeave");
            mouseInWindow = false;
        }

        private void MenuItemProperties_Click(object sender, RoutedEventArgs e)
        {
            /*
            PropertiesDlg p = new PropertiesDlg();
            try
            {
                p.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Properties could not be displayed");
            }
            */
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
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

        private void MenuItemOnlineHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://futureai.guru/BrainSimHelp/ui.html");
        }

        private void MenuItemOnlineBugs_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://github.com/FutureAIGuru/BrainSimII/issues");
        }

        private void MenuItemRegister_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://futureai.guru/BrainSimRegister.aspx");
        }

        private void MenuItemOnlineDiscussions_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://facebook.com/groups/BrainSim");
        }

        private void MenuItemYouTube_Click(object sender, RoutedEventArgs e)
        {
            OpenApp("https://www.youtube.com/c/futureai");
        }

        private void ThreadCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (int.TryParse(tb.Text, out int newThreadCount))
                {
                    // if (newThreadCount > 0 && newThreadCount < 512)
                    //     theNeuronArray.SetThreadCount(newThreadCount);
                }
            }
        }

        private void MenuItem_SelectAll(object sender, RoutedEventArgs e)
        {
            // arrayView.ClearSelection();
            // SelectionRectangle rr = new SelectionRectangle(0, theNeuronArray.Cols, theNeuronArray.rows);
            // arrayView.theSelection.selectedRectangles.Add(rr);
            Update();
        }

        private void cbShowHelpAtStartup_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["ShowHelp"] = cbShowHelpAtStartup.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void InsertModule_Click(object sender, RoutedEventArgs e)
        {
            // if (sender is MenuItem mi)
            //    NeuronArrayView.StartInsertingModule(mi.Header.ToString());
        }
        private void ModuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                if (cb.SelectedItem != null)
                {
                    string moduleName = ((Label)cb.SelectedItem).Content.ToString();
                    cb.SelectedIndex = -1;
                    // NeuronArrayView.StartInsertingModule(moduleName);
                }
            }
        }
        private void MenuCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            //CheckForVersionUpdate(true);
        }
        private void MenuItemModuleInfo_Click(object sender, RoutedEventArgs e)
        {
            ModuleDescriptionDlg md = new ModuleDescriptionDlg("");
            md.ShowDialog();
        }

    }
}
