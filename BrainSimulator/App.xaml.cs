/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
using BrainSimulator;
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
}
