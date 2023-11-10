//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;

namespace BrainSimulator.Modules
{
    public partial class ModulePodCameraDlg : ModuleBaseDlg
    {
        public ModulePodCameraDlg()
        {
            InitializeComponent();
            for (int i = 0; i<4; i++)
            {
                ModeSelectionBox.Items.Add(i);
            }
        }
        String CameraRunning = "Camera Running";
        String CameraResetting = "Camera Resetting";
        String CameraDisconnected = "Camera Disconnected";
        public override bool Draw(bool checkDrawTimer)
        {
            //if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            saveImagesCheckBox.IsChecked = parent.saveImagesToDisk;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;            
            if (!parent.cameraSelected)
            {
                CameraStatus.Content = CameraDisconnected;
                CameraStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            }
            else if (!parent.failureFlag && parent.failureCounter<4)
            {
                CameraStatus.Content = CameraRunning;
                CameraStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0,255,0));
            }
            else
            {
                CameraStatus.Content = CameraResetting;
                CameraStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                ModeLabel.Content="240x176";
                ModeSelectionBox.SelectedIndex = 0;
            }
            try
            {
                theImage.Source = parent.theBitmap;
            }
            catch { }

            VertLine.Points.Clear();
            HorizLine.Points.Clear();

            PointPlus imageCenter = new PointPlus((float)(theImage.ActualWidth / 2f), (float)(theImage.ActualHeight/2f));

            HorizLine.Points.Add(new Point(imageCenter.X-20, imageCenter.Y));
            HorizLine.Points.Add(new Point(imageCenter.X+20, imageCenter.Y));

            VertLine.Points.Add(new Point(imageCenter.X, imageCenter.Y-20));
            VertLine.Points.Add(new Point(imageCenter.X, imageCenter.Y+20));

            //Debug.WriteLine(imageCenter);
            //Debug.WriteLine(HorizLine);

            return true;
        }

       

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        

        private void saveOneButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            parent.saveOneImage = true;
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            if (sender is System.Windows.Controls.CheckBox cb)
            {
                parent.saveImagesToDisk = (bool)cb.IsChecked;
                Sallie.VideoQueue.Clear();
            }
        }

        private void Orientation_Checked(object sender, RoutedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            parent.CameraOrientation = true;
            Sallie.VideoQueue.Clear();
        }

        private void Orientation_Unchecked(object sender, RoutedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            parent.CameraOrientation = false;
            Sallie.VideoQueue.Clear();
        }

        private void ModeSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            string modeSelectString = ModeSelectionBox.SelectedValue.ToString();
            switch ((int)ModeSelectionBox.SelectedItem)
            {
                case 0:
                    ModeLabel.Content = "240x176";
                    break;
                case 1:
                    ModeLabel.Content = "640x480";
                    break;
                case 2:
                    ModeLabel.Content = "800x600";
                    break;
                case 3:
                    ModeLabel.Content = "1024x768";
                    break;
            }
            parent.failureCounter = 0;
            parent.failureFlag = false;
            parent.CameraSwapMode(modeSelectString);
        }

        private void forceTestCam_Click(object sender, RoutedEventArgs e)
        {
            ModulePodCamera parent = (ModulePodCamera)base.ParentModule;
            parent.ForceTestCam();
        }
    }
}
