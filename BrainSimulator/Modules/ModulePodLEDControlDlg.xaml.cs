//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class ModulePodLEDControlDlg : ModuleBaseDlg
    {
        private int R = 0;
        private int G = 0;
        private int B = 0;
        public ModulePodLEDControlDlg()
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

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

       
        private void SetLED_ButtonClick(object sender, RoutedEventArgs e)
        {
            ModulePodLEDControl mplc = (ModulePodLEDControl)base.ParentModule;
            mplc.SetLED(R, G, B);
        }

        private void FlashLED_ButtonClick(object sender, RoutedEventArgs e)
        {
            ModulePodLEDControl mplc = (ModulePodLEDControl)base.ParentModule;
            ModulePodInterface mpi = (ModulePodInterface)mplc.FindModule("PodInterface");
            int flashDuration = 300;
            for ( int i = 0; i < 10; i++)
            {
                mpi.QueueLED(Tuple.Create(R, G, B));
                mpi.CommandPause(flashDuration);
                mpi.QueueLED(Tuple.Create(0, 0, 0));
                mpi.CommandPause(flashDuration);
            }

            mpi.QueueLED(Tuple.Create(R, G, B));
        }

        // This function is handling text changes on the RGB text boxes.
        private void TextBox_SelectionChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = e.Source as TextBox;
            if (tb.Name != "RedTB" && tb.Name != "GreenTB" && tb.Name != "BlueTB") return;
            int value;
            if ( int.TryParse(tb.Text, out value))
            {
                tb.Background = new SolidColorBrush(Colors.Green);
                value = Math.Clamp(value, 0, 255);
                switch ( tb.Name )
                {
                    case "RedTB":
                        R = value;
                        RedTB.Text = value.ToString();
                        break;
                    case "GreenTB":
                        G = value;
                        GreenTB.Text = value.ToString();
                        break;
                    default:
                        B = value;
                        BlueTB.Text = value.ToString();
                        break;
                }
                ColorCanvas.Background = new SolidColorBrush(Color.FromArgb(255, (byte)R, (byte)G, (byte)B));
            }
            else tb.Background = new SolidColorBrush(Colors.Yellow);

        }
    }
}