//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System.Windows;
using System.Windows.Controls;

namespace BrainSimulator.Modules
{
    public partial class ModuleObjectDlg : ModuleBaseDlg
    {
        public ModuleObjectDlg()
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
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            ModuleObject Object = (ModuleObject)ParentModule;
            cbBubble.IsChecked= Object.canBubble;
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Bubble_Click(object sender, RoutedEventArgs e)
        {
            ModuleObject Object = (ModuleObject)ParentModule;
            Object.BubbleAll();
        }

        private void Unbubble_Click(object sender, RoutedEventArgs e)
        {
            ModuleObject Object = (ModuleObject)ParentModule;
            Object.UnBubbleAll();
        }

        private void Forget_Click(object sender, RoutedEventArgs e)
        {
            ModuleObject Object = (ModuleObject)ParentModule;
            Object.ForgetCall();
        }

        private void CheckBox_CheckedOrUnchecked(object sender, RoutedEventArgs e)
        {
            ModuleObject Object = (ModuleObject)ParentModule;
            if (sender is CheckBox cb)
            {
                bool isChecked = cb.IsChecked ?? false;

                if (isChecked)
                {
                    if (cb.Name == "cbBubble")
                        Object.canBubble = true;
                }
                else
                {
                    if (cb.Name == "cbBubble")
                        Object.canBubble = false;
                }
            }
        }

    }
}
