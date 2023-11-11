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

        private void Dt_Tick(object? sender, EventArgs e)
        {
            Debug.WriteLine("Dt_tick entered");

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
            Debug.WriteLine("Button_Click entered");
            CloseAllModuleDialogs();
            CloseAllModules();
            this.Close();
        }

    }
}
