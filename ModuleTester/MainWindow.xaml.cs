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

namespace ModuleTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static NeuronArrayView arrayView = null;
        public static NeuronArray theNeuronArray = null;

        public MainWindow()
        {
            Debug.WriteLine("MainWindow entered");
            InitializeComponent();
            CreateEmptyNetwork();

            Debug.WriteLine("Opening ModuleViews");

            if (MainWindow.theNeuronArray != null)
            {
                foreach (ModuleView na in MainWindow.theNeuronArray.Modules)
                {
                    if (na.TheModule != null && !na.TheModule.dlgIsOpen)
                    {
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Debug.WriteLine("Showing "+na.Label);
                            na.TheModule.ShowDialog();
                        });
                    }
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

            theNeuronArray = BrainSimulator.MainWindow.theNeuronArray;
            if (theNeuronArray == null) {
                theNeuronArray = new NeuronArray();
                // BrainSimulator.MainWindow.theNeuronArray = theNeuronArray;
            }

            theNeuronArray.Initialize(450, 15);
            InsertModules();
            theNeuronArray.LoadComplete = true;
            // MainWindow.theNeuronArray = theNeuronArray;
            Update();
        }

        public void InsertModules()
        {
            Debug.WriteLine("InsertModules entered");
            CreateModule("ModuleUKS", "ModuleUKS");
            CreateModule("ModuleUKSInteract", "ModuleUKSInteract");
        }

        public static void CreateModule(string label, string commandLine)
        {
            Debug.WriteLine("CreateModule entered");
            ModuleView mv = new ModuleView(0, 1, 1, label, commandLine, Utils.ColorToInt(new Color() { A = 64, R = 192, G = 192, B = 192 }));
            if (mv.Width < mv.theModule.MinWidth) mv.Width = mv.theModule.MinWidth;
            if (mv.Height < mv.theModule.MinHeight) mv.Height = mv.theModule.MinHeight;

            MainWindow.theNeuronArray.Modules.Add(mv);
            string[] Params = commandLine.Split(' ');
            Type t1x = Type.GetType("BrainSimulator.Modules." + Params[0]);
            if (t1x != null && (mv.TheModule == null || mv.TheModule.GetType() != t1x))
            {
                mv.TheModule = (ModuleBase)Activator.CreateInstance(t1x);
                // MainWindow.theNeuronArray.areas[i].TheModule.Initialize();
            }
        }

        public static void CloseAllModuleDialogs()
        {
            if (MainWindow.theNeuronArray != null)
            {
                lock (MainWindow.theNeuronArray.Modules)
                {
                    foreach (ModuleView na in MainWindow.theNeuronArray.Modules)
                    {
                        if (na.TheModule != null)
                        {
                            na.TheModule.CloseDlg();
                        }
                    }
                }
            }
        }

        public static void CloseAllModules()
        {
            if (MainWindow.theNeuronArray != null)
            {
                lock (MainWindow.theNeuronArray.Modules)
                {
                    foreach (ModuleView na in MainWindow.theNeuronArray.Modules)
                    {
                        if (na.TheModule != null)
                        {
                            na.TheModule.Closing();
                        }
                    }
                }
            }
        }

        public static void Update()
        {
            Debug.WriteLine("Update entered");
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            Debug.WriteLine("Dt_tick entered");
            if (MainWindow.theNeuronArray == null) return;
            Debug.WriteLine("TheNeuronArray not null");

            foreach (ModuleView na in MainWindow.theNeuronArray.Modules)
            {
                if (na.TheModule != null && na.TheModule.dlgIsOpen)
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Debug.WriteLine("Firing "+na.Label);
                        na.TheModule.Fire();
                    });
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button_Click entered");
            CloseAllModuleDialogs();
            CloseAllModules();
            this.Close();
        }

    }
}
