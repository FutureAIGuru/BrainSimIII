//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

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
    public partial class ModuleUI_HelpDlg : ModuleBaseDlg
    {
        public ModuleUI_HelpDlg()
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

        private void CommandsLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ShowAvailableCommands();
        }

        public static Process OpenApp(string fileName)
        {
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.UseShellExecute = true;
            p.Start();
            return p;
        }

        private void GetStartedLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenApp("https://futureai.guru/brain-simulator-help/");
        }

        private void HelpGuideLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenApp("https://futureai.guru/brain-simulator-help/");
        }

        private void ForumLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenApp("https://futureai.guru/brain-simulator-help/");
        }
    }
}