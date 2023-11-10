//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public partial class ModulePodConnectionDlg : ModuleBaseDlg
    {
        public ModulePodConnectionDlg()
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
            ModulePodConnection mpc = (ModulePodConnection)base.ParentModule;

            UpdateList(mpc);
            AutoConnect.IsChecked = mpc.AutoConnection;
            if (IPPodListComboBox.IsEnabled || IPPodListComboBox.SelectedItem == null || mpc.CameraIP == null) UpdateList(mpc);
            if ( mpc.podConnected )
            {
                IPPodListComboBox.IsEnabled = false;
                PairButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
            }

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void IPListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            
            if (sender is System.Windows.Controls.ComboBox cb && cb.SelectedItem != null)
            {
                parent.PodSelected = parent.PodList[(string)cb.SelectedItem];
            }
        }

        public void UpdateList(ModulePodConnection parent)
        {
            if (!parent.listChanged || IPPodListComboBox.IsDropDownOpen) return;
            IPPodListComboBox.Items.Clear();
            foreach (ModulePodConnection.PodIPInfo podIPInfo in parent.PodList.Values)
            {
                IPAddress ip = podIPInfo.PodIP;
                int index = IPPodListComboBox.Items.Add(podIPInfo.PodName);
                if (parent.PodSelected?.PodIP.ToString() == ip.ToString() )
                {
                    IPPodListComboBox.SelectedIndex = index;
                }
            }
        }

        private void IPPodListComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            UpdateList(parent);
        }

        private void PairPod_Button_Click(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;

            if (MainWindow.theNeuronArray.EngineIsPaused)
            {
                System.Windows.MessageBox.Show("Engine is paused, pod pair command not sent.");
                return;
            }

            else if (MainWindow.theNeuronArray.EngineSpeed != 0)
            {
                System.Windows.MessageBox.Show("Engine speed is too slow, pod pair command not sent.");
                return;
            }
            if (IPPodListComboBox.SelectedIndex == -1)
            {
                System.Windows.MessageBox.Show("Please select a pod to pair to.");
                return;
            }

            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            renamePodTextBox.Text = (string)IPPodListComboBox.SelectedItem;
            IPPodListComboBox.IsEnabled = false;
            PairButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            parent.PairPod(parent.PodSelected);
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
        }

        public void DisconnectPod_Button_Click(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            parent.DisconnectPod();
            renamePodTextBox.Text = "";
            IPPodListComboBox.IsEnabled = true;
            IPPodListComboBox.SelectedIndex = -1;
            PairButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
        }

        public void renameButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            if (renamePodTextBox.Text == "")
            {
                System.Windows.MessageBox.Show("Please fill out the Name Field, before renaming pod");
                return;
            }
            parent.RenamePod(renamePodTextBox.Text);
        }

        private void SendPodOTACommand(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            parent.OTA_Mode(parent.PairedPodIP, "Pod");
        }

        private void SendCameraOTACommand(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            parent.OTA_Mode(parent.CameraIP, "Camera");
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            parent.AutoConnection = (bool) cb.IsChecked;
        }

        private void speakerEnabledToggle_Click(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            if (parent == null) return;
            if ((bool)speakerEnabledToggle.IsChecked)
            {
                Network.SendStringToPodTCP("s1");
            }
            else
            {
                Network.SendStringToPodTCP("s0");
            }
        }

        private void micEnabledToggle_Click(object sender, RoutedEventArgs e)
        {
            ModulePodConnection parent = (ModulePodConnection)base.ParentModule;
            if (parent == null) return;
            if ((bool)speakerEnabledToggle.IsChecked)
            {
                Network.SendStringToPodTCP("a1");
            }
            else
            {
                Network.SendStringToPodTCP("a0");
            }
        }
    }
}