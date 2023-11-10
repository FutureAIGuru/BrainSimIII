//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using SharpDX;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrainSimulator.Modules
{
    public partial class ModulePodInterfaceDlg : ModuleBaseDlg
    {
        // Method to easily access the ModulePod module if it exists...
        private ModulePod theLivePod = null;
        private ModulePodInterface thePod;

        public ModulePodInterfaceDlg()
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

            // update the position textboxes
            //double roundedAngle = Math.Round(Sallie.BodyAngle, 2);
            //AngleBox.Text = roundedAngle.ToString();
            string newPosition = "("
                + Math.Round(Sallie.BodyPosition.X, 2) + ", "
                + Math.Round(Sallie.BodyPosition.Y, 2) + ", "
                + Math.Round(Sallie.BodyPosition.Z, 2) + ")";
            PositionBox.Text = newPosition;
            PanBox.Text = Math.Round(-Sallie.CameraPan.Degrees, 0).ToString();
            TiltBox.Text = Math.Round(Sallie.CameraTilt.Degrees, 0).ToString();
            if (theLivePod != null)
            {
                SetAngleBox.Text = theLivePod.GetDesiredAngle().ToString();
                AngleBox.Text = theLivePod.getrawYaw().ToString();
            }
            else
            {
                AngleBox.Text = Math.Round(Sallie.BodyAngle.Degrees, 0).ToString();
                SetAngleBox.Text = Math.Round(Sallie.BodyAngle.Degrees, 0).ToString();
            }

            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            LiveCheckbox.IsChecked = parent.isLive;
            UpdateLiveCheckbox();

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        // method to easily access the parent ModulePodInterface which always should exist.
        public ModulePodInterface Pod()
        {
            if (thePod == null)
            {
                thePod = (ModulePodInterface)ParentModule;
            }
            return thePod;
        }

        public ModulePod LivePod()
        {
            if (theLivePod == null)
            {
                ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
                theLivePod = (ModulePod)parent.FindModule("Pod", true);
            }
            return theLivePod;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            //parent.IsPodActive();
            parent?.CommandStop();
        }

        private void CenterViewButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            parent.IsPodActive();
            Pod()?.ResetCamera();
            Pod()?.UpdateSallieSelf();
        }
       
        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DistanceBox.Text, out int distance) && int.TryParse(TurnAngle.Text, out int turnAngle))
            {
                ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
                if (sender is Button b)
                {
                    switch (b.Content)
                    {
                        case "Forward":
                            parent.CommandMove(distance, !queueMoves);
                            break;
                        case "Backward":                            
                            parent.CommandMove(-distance, !queueMoves);
                            break;
                        case "Left":
                            parent.CommandTurn(Angle.FromDegrees(-turnAngle), !queueMoves);
                            break;
                        case "Right":
                            parent.CommandTurn(+Angle.FromDegrees(turnAngle), !queueMoves);
                            break;
                    }
                }
            }
        }

        private void MoveButton_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (int.TryParse(DistanceBox.Text, out int distance) && int.TryParse(TurnAngle.Text, out int turnAngle))
            {
                ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
                if (sender is Button b)
                {
                    switch (b.Content)
                    {
                        case "Forward":
                            parent.CommandMove(distance / 10, !queueMoves);
                            break;
                        case "Backward":
                            parent.CommandMove(-distance / 10, !queueMoves);
                            break;
                        case "Left":
                            parent.CommandTurn(+Angle.FromDegrees(-turnAngle / 10), !queueMoves);
                            break;
                        case "Right":
                            parent.CommandTurn(+Angle.FromDegrees(turnAngle / 10), !queueMoves);
                            break;
                    }
                }
            }
        }
        private void CamButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CameraMoveBox.Text, out int degrees))
            {
                ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
                if (sender is Button b)
                {
                    switch (b.Content)
                    {
                        case "Up":
                            parent.CommandTilt(+Angle.FromDegrees(degrees),true, !queueMoves);
                            break;
                        case "Down":
                            parent.CommandTilt(+Angle.FromDegrees(-degrees),true, !queueMoves);
                            break;
                        case "Left":
                            parent.CommandPan(+Angle.FromDegrees(degrees),true, !queueMoves);
                            break;
                        case "Right":
                            parent.CommandPan(+Angle.FromDegrees(-degrees), true,!queueMoves);
                            break;
                    }
                }
            }
        }
        private void CamButton_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (int.TryParse(CameraMoveBox.Text, out int degrees))
            {
                ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
                if (sender is Button b)
                {
                    switch (b.Content)
                    {
                        case "Up":
                            parent.CommandTilt(+Angle.FromDegrees(degrees / 10),true, !queueMoves);
                            break;
                        case "Down":
                            parent.CommandTilt(+Angle.FromDegrees(-degrees / 10),true, !queueMoves);
                            break;
                        case "Left":
                            parent.CommandPan(+Angle.FromDegrees(degrees / 10), true,!queueMoves);
                            break;
                        case "Right":
                            parent.CommandPan(+Angle.FromDegrees(-degrees / 10),true, !queueMoves);
                            break;
                    }
                }
            }
        }

        private void LiveCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateLiveCheckbox();

            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            if (parent != null)
            {
                ModulePodCamera mpc = (ModulePodCamera)parent.FindModule("PodCamera");
                Module3DSimView msv = (Module3DSimView)parent.FindModule("3DSimView");
                if (mpc != null) mpc.saveImagesToDisk = true;
                if (msv != null) msv.ProduceOutput = false;
            }

            // Update the parent module so it knows...
            Pod().isLive = (bool)LiveCheckbox.IsChecked;
        }

        private void LiveCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateLiveCheckbox();

            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            if (parent != null)
            {
                ModulePodCamera mpc = (ModulePodCamera)parent.FindModule("PodCamera");
                Module3DSimView msv = (Module3DSimView)parent.FindModule("3DSimView");
                if (mpc != null) mpc.saveImagesToDisk = false;
                if (msv != null) msv.ProduceOutput = true;
            }

            // Update the parent module so it knows...
            Pod().isLive = (bool)LiveCheckbox.IsChecked;
        }

        public void UpdateLiveCheckbox()
        {
            if (LivePod() == null)
            {
                LiveCheckbox.Foreground = Brushes.DarkGray;
                LiveCheckbox.IsChecked = false;
                LiveCheckbox.IsEnabled = false;
            }
            else
            {
                LiveCheckbox.IsEnabled = true;
                if (LiveCheckbox.IsChecked == true)
                {
                    LiveCheckbox.Foreground = Brushes.Yellow;
                }
                else
                {
                    LiveCheckbox.Foreground = Brushes.Black;
                }
            }
        }

        private void ResetSalliePostionButton_Click(object sender, RoutedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;

            parent.ResetSallie();
        }
        bool queueMoves = false;

        private void checkBox_Clicked(object sender, RoutedEventArgs e)
        {
            queueMoves = (bool)QueueActive.IsChecked;
        }

        private void SpeedBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            if (parent != null && int.TryParse(SpeedBox.Text, out int value)  && value <= parent.MaxSpeed && value >= parent.MinSpeed)
            {
                parent.CommandSpeed((float)value,!queueMoves);
            }
        }

        private void SpeedBox_Loaded(object sender, RoutedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            if (parent != null && int.TryParse(SpeedBox.Text, out int value)  && value <= 90 && value >= 20)
            {
                parent.CommandSpeed((float)value, !queueMoves);
            }
        }

        private void invertTiltCheck_Checked(object sender, RoutedEventArgs e)
        {
            ModulePodInterface parent = (ModulePodInterface)base.ParentModule;
            if (parent == null) return;
            if (sender is CheckBox cb)
                parent.tiltInverted = (bool)cb.IsChecked;
        }

    }
}
