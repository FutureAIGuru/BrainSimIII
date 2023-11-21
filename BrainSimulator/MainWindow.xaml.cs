using BrainSimulator;
using BrainSimulator.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<ModuleBase> modules = new List<ModuleBase>();

        public MainWindow()
        {
            Debug.WriteLine("MainWindow entered");
            InitializeComponent();
            CreateEmptyNetwork();

            Debug.WriteLine("Opening ModuleViews");

            foreach (ModuleBase mb in MainWindow.modules)
            {
                if (mb != null && !mb.dlgIsOpen)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.ShowDialog();
                    });
                }
            }
            
            Debug.WriteLine("Starting Tick Timer");
            DispatcherTimer dt = new();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            dt.Tick += Dt_Tick;
            dt.Start();
            // this.Close();
        }

        public void CreateEmptyNetwork()
        {
            Debug.WriteLine("CreateEmptyNetwork entered");

            InsertModules();

            Update();
        }

        public void InsertModules()
        {
            Debug.WriteLine("InsertModules entered");
            modules.Add(new BrainSimulator.Modules.ModuleUKS());
            modules.Add(new BrainSimulator.Modules.ModuleUKSInteract());
        }

        public static void CloseAllModuleDialogs()
        {
            lock (modules)
            {
                foreach (ModuleBase md in MainWindow.modules)
                {
                    if (md != null)
                    {
                        md.CloseDlg();
                    }
                }
            }
        }

        public static void CloseAllModules()
        {
            lock (MainWindow.modules)
            {
                foreach (ModuleBase mb in MainWindow.modules)
                {
                    if (mb != null)
                    {
                        mb.Closing();
                    }
                }
            }
        }

        public static void Update()
        {
            Debug.WriteLine("Update entered");
        }

        [XmlIgnore]
        public ModuleUKS UKS = null;
        public void GetUKS()
        {
            if (UKS is null)
            {
                UKS = (ModuleUKS)FindModule(typeof(ModuleUKS));
            }
        }

        public ModuleBase FindModule(Type t, bool suppressWarning = true)
        {
            foreach (ModuleBase mb1 in modules)
            {
                if (mb1 != null && mb1.GetType() == t)
                {
                    return mb1;
                }
            }
            return null;
        }

        // stack to make sure we supend and resume the engine properly
        static Stack<int> engineSpeedStack = new Stack<int>();

        public bool IsEngineSuspended()
        {
            return engineSpeedStack.Count > 0;
        }

        public static void SuspendEngine()
        {
            /*
            // just pushing an int here, we won't restore it later
            engineSpeedStack.Push(engineDelay);
            if (theNeuronArray == null)
                return;

            string currentThreadName = Thread.CurrentThread.Name;
            // wait for the engine to actually stop before returning
            while (theNeuronArray != null && !engineIsPaused && currentThreadName != "EngineThread")
            {
                Thread.Sleep(100);
                System.Windows.Forms.Application.DoEvents();
            }
            */
        }

        public static void ResumeEngine()
        {
            /*
            // first pop the top to make sure we balance the suspends and resumes
            if (engineSpeedStack.Count > 0)
                engineDelay = engineSpeedStack.Pop();
            if (theNeuronArray == null)
                return;

            // resume the engine
            // on shutdown, the current application may be gone when this is requested
            if (theNeuronArray != null && Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow.thisWindow.SetSliderPosition(engineDelay);
                });
            }
            */
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            // Debug.WriteLine("Dt_tick entered");

            foreach (ModuleBase mb in MainWindow.modules)
            {
                if (mb != null && mb.dlgIsOpen)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        mb.Fire();
                    });
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Debug.WriteLine("Button_Click entered");
            CloseAllModuleDialogs();
            CloseAllModules();
            this.Close();
        }

    }
}
