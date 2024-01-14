//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace BrainSimulator.Modules
{
    public class ModuleBaseDlg : Window
    {
        public ModuleBase ParentModule;
        private DateTime dt;
        private DispatcherTimer timer;
        public int UpdateMS = 100;

        public ModuleBaseDlg()
        {
            this.Loaded += ModuleBaseDlg_Loaded;
        }

        private void ModuleBaseDlg_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a button and add it to the panel
            Button button = new Button
            {
                Content = "?",
                FontSize = 24,
                Width = 20,
                Height = 25,
                Margin = new Thickness(5),
                Padding = new Thickness(0,-6,0,0),
                Name = "helpButton",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            button.Click += HelpButton_Click;

            // Add the button to the dialog
            ((Panel)this.Content).Children.Add(button);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string theModuleType = this.GetType().Name.ToString();
            theModuleType = theModuleType.Replace("Dlg", "");
            ModuleDescriptionDlg md = new ModuleDescriptionDlg(theModuleType);
            md.Show();
        }

        virtual public bool Draw(bool checkDrawTimer)
        {
            if (!checkDrawTimer) return true;
            return true;
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            if (Application.Current == null) return;
            if (this != null)
                Draw(false);

        }

        //this picks up a final draw after 1/4 second 
        public void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            if (Application.Current == null) return;
            if (this != null)
                Draw(false);
        }

    }
}
