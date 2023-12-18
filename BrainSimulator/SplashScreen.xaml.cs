//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 


using System;
using System.Windows;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for SplashScreeen.xaml
    /// </summary>
    public partial class SplashScreeen : Window
    {
        public SplashScreeen()
        {
            InitializeComponent();
            DispatcherTimer dt = new();
            dt.Tick += Dt_Tick;
            dt.Interval = TimeSpan.FromSeconds(4);
            dt.Start();
        }

        private void Dt_Tick(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}
