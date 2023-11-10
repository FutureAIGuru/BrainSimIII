//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

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
    public partial class ModuleConversationDlg : ModuleBaseDlg
    {
        public ModuleConversationDlg()
        {
            InitializeComponent();
        }
        int i;
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

            while (theConversation.Children.Count > 30) theConversation.Children.RemoveAt(0);
            Label theLabel = new Label { Content = "message" + i++ };
            Border theBorder = new() { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(10) };
            if (i % 2 == 0)
            {
                theBorder.HorizontalAlignment = HorizontalAlignment.Right;
                theLabel.HorizontalAlignment = HorizontalAlignment.Right;
                theBorder.Background = new SolidColorBrush(Colors.LightBlue);
            }
            else
            {
                theBorder.HorizontalAlignment = HorizontalAlignment.Left;
                theLabel.HorizontalAlignment = HorizontalAlignment.Left;
                theBorder.Background = new SolidColorBrush(Colors.LightCoral);
            }
            theBorder.Child = theLabel;
            theConversation.Children.Add(theBorder);
            if (theScroller.VerticalOffset == theScroller.ScrollableHeight)
            {
                theScroller.ScrollToEnd();
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

    }
}