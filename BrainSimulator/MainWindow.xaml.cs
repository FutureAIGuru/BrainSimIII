//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //if (e.Args.Length == 1)
            //    StartupString = e.Args[0];

            MainWindow mainWin = new();
#if !DEBUG
            mainWin.WindowState = WindowState.Minimized;
#endif
            mainWin.Show();
#if !DEBUG
            mainWin.Hide();
#endif        
        }
        private static string startupString = "";

        public static string StartupString { get => startupString; set => startupString = value; }
    }
    public partial class MainWindow : Window
    {
        //Globals
        // public static NeuronArrayView arrayView = null;
        public static NeuronArray theNeuronArray = null;
        //for cut-copy-paste
        public static NeuronArray myClipBoard = null; //refactor back to private

        public static FiringHistoryWindow fwWindow = null;
        public static NotesDialog notesWindow = null;

        // Flag to check when program was closed
        public static bool WasClosed;

        readonly Thread engineThread;

        public static bool useServers = false;

        private static int engineDelay = 500; //wait after each cycle of the engine, 0-1000

        //timer to update the neuron values 
        readonly private DispatcherTimer displayUpdateTimer = new DispatcherTimer();

        // if the control key is pressed...used for adding multiple selection areas
        public static bool ctrlPressed = false;
        public static bool shiftPressed = false;

        public static bool pauseTheNeuronArray;

        //the name of the currently-loaded network file
        public static string currentFileName = "";

        public static MainWindow thisWindow;
        readonly Window splashScreen = new SplashScreeen();

        public MainWindow()
        {
            //this puts up a dialog on unhandled exceptions
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    string message = "Brain Simulator encountered an error--";
                    Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
                    message += "\r\nVersion: " + $"{version.Major}.{version.Minor}.{version.Build}   ({buildDate})";
                    message += "\r\n.exe location: " + System.Reflection.Assembly.GetExecutingAssembly().Location;
                    message += "\r\nPROGRAM WILL EXIT  (this message copied to clipboard)";

                    message += "\r\n\r\n"+ eventArgs.ExceptionObject.ToString();
                 
                    System.Windows.Forms.Clipboard.SetText(message);
                    MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
                    Application.Current.Shutdown(255);
                };
#endif
            // First of all remove all left over camera images that were not processed yet.
            // Utils.CleanAndRecreateDocumentsSubFolder("CameraOutput");
            // Utils.CleanAndRecreateDocumentsSubFolder("VirtualCameraOutput");
            Utils.CleanAndRecreateDocumentsSubFolder("ImageProcessingOutput");
            Utils.CleanAndRecreateDocumentsSubFolder("VisionDetails");

            engineThread = new Thread(new ThreadStart(EngineLoop)) { Name = "EngineThread" };

            //testing of crash message...
            //throw new FileNotFoundException();

            InitializeComponent();

            engineThread.Priority = ThreadPriority.Lowest;

            displayUpdateTimer.Tick += DisplayUpdate_TimerTick;
            displayUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            displayUpdateTimer.Start();

            thisWindow = this;

            splashScreen.Left = 300;
            splashScreen.Top = 300;
            splashScreen.Show();
            DispatcherTimer splashHide = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 3),
            };
            splashHide.Tick += SplashHide_Tick;
            splashHide.Start();
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
            //CheckForVersionUpdate();
        }

        private void SplashHide_Tick(object sender, EventArgs e)
        {
            Application.Current.MainWindow = this;
            splashScreen.Close();
            ((DispatcherTimer)sender).Stop();

            bool showHelp = (bool)Properties.Settings.Default["ShowHelp"];
            cbShowHelpAtStartup.IsChecked = showHelp;
            if (showHelp)
            {
                MenuItemHelp_Click(null, null);
            }
            
            //this is here because the file can be loaded before the mainwindow displays so
            //module dialogs may open before their owner so this happens a few seconds later
            /*
            if (theNeuronArray != null)
            {
                lock (theNeuronArray.Modules)
                {
                    foreach (ModuleView na in theNeuronArray.Modules)
                    {
                        if (na.TheModule != null && na.TheModule.GetType() != typeof(Modules.ModuleUserInterface))
                        {
                            na.TheModule.SetDlgOwner(this);
                        }
                    }
                }
            }
            */
            prevTop = Top;
            prevLeft = Left;

            // NeuronView.OpenHistoryWindow();
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
                mi.Click += MRUListItem_Click;
                mi.ToolTip = fileItem;
                MRUListMenu.Items.Add(mi);
            }
        }

        private void LoadModuleTypeMenu()
        {
            var moduleTypes = Utils.GetArrayOfModuleTypes();

            foreach (var moduleType in moduleTypes)
            {
                string moduleName = moduleType.Name;
                moduleName = moduleName.Replace("Module", "");
                Modules.ModuleBase theModule = (Modules.ModuleBase)Activator.CreateInstance(moduleType);
                string toolTip = ModuleDescriptionFile.GetToolTip(moduleType.Name);

                MenuItem mi = new MenuItem { Header = moduleName, ToolTip = toolTip, };
                mi.Click += InsertModule_Click;
            }
        }

        private void LoadFindMenus()
        {
            if (IsArrayEmpty()) return;
        }

        //Enable/disable menu item specified by "Entry"...pass in the Menu.Items as the root to search
        private void EnableMenuItem(ItemCollection mm, string Entry, bool enabled)
        {
            foreach (Object m1 in mm)
            {
                if (m1.GetType() == typeof(MenuItem))
                {
                    MenuItem m = (MenuItem)m1;
                    if (m.Header.ToString() == Entry)
                    {
                        m.IsEnabled = enabled;
                        return;
                    }
                    else
                        EnableMenuItem(m.Items, Entry, enabled);
                }
            }

            return;
        }

        //this enables and disables various menu entries based on circumstances
        static bool fullUpdateNeeded = false;
        public static void Update()
        {
            if (thisWindow.IsEngineSuspended())
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    // arrayView.Update();
                });
            }
            else
            {
                fullUpdateNeeded = true;
            }
        }

        public static void CloseAllModuleDialogs()
        {
            if (theNeuronArray != null)
            {
                lock (theNeuronArray.Modules)
                {
                    foreach (ModuleView na in theNeuronArray.Modules)
                    {
                        if (na.TheModule != null)
                        {
                            na.TheModule.CloseDlg();
                        }
                    }
                }
            }
        }

        public static bool IsArrayEmpty()
        {
            if (MainWindow.theNeuronArray == null) return true;
            if (MainWindow.theNeuronArray.arraySize == 0) return true;
            if (MainWindow.theNeuronArray.rows == 0) return true;
            if (MainWindow.theNeuronArray.Cols == 0) return true;
            if (!MainWindow.theNeuronArray.LoadComplete) return true;
            return false;
        }

        public void CreateEmptyNetwork()
        {
            theNeuronArray = new NeuronArray();
            // arrayView.Dp.NeuronDisplaySize = 62;
            // arrayView.Dp.DisplayOffset = new Point(0, 0);
            theNeuronArray.Initialize(450, 15);
            theNeuronArray.LoadComplete = true;
            Update();
        }

        double prevTop = double.MaxValue;
        double prevLeft = double.MaxValue;
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (prevTop != double.MaxValue && prevTop != double.MaxValue)
            { 
                double dy= Top - prevTop;
                double dx = Left - prevLeft;
                foreach (ModuleView m in theNeuronArray.modules)
                {
                    m.TheModule.MoveDialog(dx, dy);
                }
            } 
            prevTop = Top;
            prevLeft = Left;
        }
    }
}
