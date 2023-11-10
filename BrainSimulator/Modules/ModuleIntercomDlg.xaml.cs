//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleIntercomDlg : ModuleBaseDlg
    {
        public ModuleIntercomDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            ModuleIntercom parent = (ModuleIntercom)base.ParentModule;
            if (parent.Speak ) Button_Record.Content = "Stop Speak";
            else Button_Record.Content = "Start Speak";
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void StartStop_Listen(object sender, EventArgs e)
        {
            ModuleIntercom parent = (ModuleIntercom)base.ParentModule;
            
            Button b = sender as Button;
            
            if ( b.Content == "Start Listen")
            {
                parent.SetListenToPod(true);
                b.Content = "Stop Listen";
            }
            else
            {
                parent.SetListenToPod(false);
                b.Content = "Start Listen";
            }
        }

        private void StartStop_Speak(object sender, MouseEventArgs e)
        {
            ModuleIntercom parent = (ModuleIntercom)base.ParentModule;
            Button b = sender as Button;

            if (b.Content == "Start Speak"  && e.RoutedEvent != Mouse.MouseLeaveEvent)
            {
                b.Content = "Stop Speak";
                parent.SendMicrophoneToPod(true);
                e.Handled = true;
            }
            else
            {
                b.Content = "Start Speak";
                parent.SendMicrophoneToPod(false);
                e.Handled = true;
            }
        }
    }
}