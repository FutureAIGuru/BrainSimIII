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
    public partial class ModuleInputControlDlg : ModuleBaseDlg
    {
        public ModuleInputControlDlg()
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

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            ModuleInputControl parent = (ModuleInputControl)base.ParentModule;
            
            switch (e.Key )
            {
                case Key.Up:
                    if (!e.IsRepeat) parent.MoveForwardBackward(true);
                    break;
                case Key.Down:
                    if (!e.IsRepeat) parent.MoveForwardBackward(false);
                    break;
                case Key.Left:
                    parent.turnLeft = true;
                    break;
                case Key.Right:
                    parent.turnRight = true;
                    break;
                case Key.Space:
                    parent.Stop();
                    break;
            }
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            ModuleInputControl parent = (ModuleInputControl)base.ParentModule;
            //parent.Stop();
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            ModuleInputControl parent = (ModuleInputControl)base.ParentModule;

            switch (e.Key)
            {
                case Key.Up:
                    parent.StopForwardBack();
                    break;
                case Key.Down:
                    parent.StopForwardBack();
                    break;
                case Key.Left:
                    parent.turnLeft = false;
                    parent.StopTurn();
                    break;
                case Key.Right:
                    parent.turnRight = false;
                    parent.StopTurn();
                    break;
            }
        }
    }
}