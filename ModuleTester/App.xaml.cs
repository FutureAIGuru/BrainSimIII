using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ModuleTester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //if (e.Args.Length == 1)
            //    StartupString = e.Args[0];

            BrainSimulator.MainWindow mainWin = new();
            mainWin.WindowState = WindowState.Minimized;
            mainWin.Show();
            mainWin.Hide();
        }
    }
}
