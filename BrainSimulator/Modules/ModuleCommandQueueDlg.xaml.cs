//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
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
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public partial class ModuleCommandQueueDlg : ModuleBaseDlg
    {

        bool toolong;

        public ModuleCommandQueueDlg()
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
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            
            // FillComboBox();
            try
            {
                if (CQ.RecList.Count < 25)
                {
                    toolong = false;
                    CommandList.Clear();
                    foreach (Thing Command in CQ.RecList)
                    {

                        if (Command.V != null)
                            CommandList.Text += Command.Label + " " + Command.V.ToString() + "\n";
                        else
                            CommandList.Text += Command.Label + "\n";
                    }
                }
                else if(!toolong)
                {
                    CommandList.Text += "...\n";
                    toolong = true;
                }
            }
            catch
            {
                Debug.WriteLine("ERROR: Collection Modified");
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectText.Text == "")
                return;
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            if (!CQ.FindObject(ObjectText.Text))
            {
                System.Windows.MessageBox.Show($"Object does not exist.");
                //write error message to screen
            }
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            if (CQ.Recording)
            {
                Thing temp = new();
                if (SequenceSelection.Text == SequenceName.Text)
                    return;
                temp.Label = SequenceSelection.Text;
                CQ.RecList.Add(temp);
                return;
            }
            CQ.executeCommandQueue(SequenceSelection.Text);
        }

        private void FillComboBox()
        {
            List<string> list = new();
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            list = CQ.FillSequenceList();
            if (list == null)
                return;
            SequenceSelection.Items.Clear();
            foreach (string s in list)
                SequenceSelection.Items.Add(s);
        }

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    FillComboBox();
        //}

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            if (!CQ.Recording)
            {
                CQ.Recording = true;
                //RecordBtn.IsChecked = true;
                //SequenceButton.IsEnabled = false;
                return;
            }
            else
            {
                if (CQ.RecList.Count == 0)
                {
                    if (CQ.Recording)
                    {
                        CQ.Recording = false;
                        //RecordBtn.IsChecked = false;
                        // SequenceButton.IsEnabled = true;
                        return;
                    }
                }
                else if (SequenceName.Text == "")
                {
                    MessageBox.Show("Please enter sequence name");
                    RecordBtn.IsChecked = true;
                    return;
                }
                if (SequenceSelection.Text != SequenceName.Text)
                {
                    CQ.SaveRecording(SequenceName.Text);
                }
                CQ.RecList.Clear();
            }
            if (CQ.Recording)
            {
                CQ.Recording = false;
                //RecordBtn.IsChecked = false;
                // SequenceButton.IsEnabled = true;
            }
        }



        private void SequenceSelection_DropDownOpened(object sender, System.EventArgs e)
        {
            FillComboBox();
        }

        private void LiveBtn_Click(object sender, RoutedEventArgs e)
        {
            ModuleCommandQueue CQ = (ModuleCommandQueue)ParentModule;
            if (CQ.Live == false)
            {
                CQ.Live = true;
            }
            else
            {
                CQ.Live = false;
            }
        }
    }
}
