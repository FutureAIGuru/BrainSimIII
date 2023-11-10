//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
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
using System;
using System.IO.Ports;
using System.Management;


    

namespace BrainSimulator.Modules
{
    public partial class ModulePodDialogControlsDlg : ModuleBaseDlg
    {
        public ModulePodDialogControlsDlg()
        {
            InitializeComponent();
            FindComPorts();
        }        
        public void FindComPorts()
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            var portnames = SerialPort.GetPortNames();
            var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

            var portList = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();

            ComPort.ItemsSource = portList;
        }
        public void PutToTerminal()
        {
            ModulePodDialogControls parent = (ModulePodDialogControls)base.ParentModule;
            string theOutput = "";
            List<string> input = parent.PullPodOutput();
            if (input.Count > 0 && input != null)
            {
                for (int i = 0; i<input.Count;i++)
                {
                    theOutput += (input[i]+'\n');                    
                }
                TerminalOutput.Text = (theOutput);
            }
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
            PutToTerminal();
            
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }
    }
}