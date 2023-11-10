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
    public partial class ModuleUI_MotionDlg : ModuleBaseDlg
    {
        public ModuleUI_MotionDlg()
        {
            InitializeComponent();
        }

        const float smallMove = 2, largeMove = 10;
        const float smallTurn = 5, mediumTurn = 45, largeTurn = 90;

        public void Initialize(float minSpeed, float maxSpeed, out float setSpeed)
        {
            SpeedSlider.Minimum = minSpeed;
            SpeedSlider.Maximum = maxSpeed;

            setSpeed = (float)SpeedSlider.Value;

            StopButton.ToolTip = new ToolTip() { Content = "Stop" };
            Forward1Button.ToolTip = new ToolTip() { Content = "Forward " + smallMove };
            Forward2Button.ToolTip = new ToolTip() { Content = "Forward " + largeMove };
            Forward3Button.ToolTip = new ToolTip() { Content = "Forward Continuous" };
            Back1Button.ToolTip = new ToolTip() { Content = "Back " + smallMove };
            Back2Button.ToolTip = new ToolTip() { Content = "Back " + largeMove };
            Back3Button.ToolTip = new ToolTip() { Content = "Back Continuous" };
            Left1Button.ToolTip = new ToolTip() { Content = "Left " + smallTurn };
            Left2Button.ToolTip = new ToolTip() { Content = "Left " + mediumTurn };
            Left3Button.ToolTip = new ToolTip() { Content = "Left " + largeTurn };
            Right1Button.ToolTip = new ToolTip() { Content = "Right " + smallTurn };
            Right2Button.ToolTip = new ToolTip() { Content = "Right " + mediumTurn };
            Right3Button.ToolTip = new ToolTip() { Content = "Right " + largeTurn };
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

        bool expanded = false;
        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            expanded = ((ModuleUserInterfaceDlg)Owner).ExpandCollapseWindow(this.GetType());
            if (expanded)
            {
                theChrome.ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
                theChrome.CaptionHeight = 40;
                ExpandButton.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else
            {
                WindowState = WindowState.Normal;
                ((ModuleUserInterfaceDlg)Owner).MoveChildren();
                theChrome.ResizeBorderThickness = new Thickness(0);
                theChrome.CaptionHeight = 0;
                CollapseButton.Visibility = Visibility.Collapsed;
                ExpandButton.Visibility = Visibility.Visible;
            }
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }

        private void Dlg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box on other pages
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(true);
                    //prevent arrow keys from doing anything else on the home page
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(false);
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = true;
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = true;
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Down:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_Deactivated(object sender, System.EventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.SetPodSpeed((float)SpeedSlider.Value);
        }

        private void Forward1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Drive(smallMove);

            Forward1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Small.png", UriKind.Relative));
        }

        private void Forward1Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Forward1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Small-Disabled.png", UriKind.Relative));
        }

        private void Forward2Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Drive(largeMove);

            Forward2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Med.png", UriKind.Relative));
        }

        private void Forward2Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Forward2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Med-Disabled.png", UriKind.Relative));
        }

        private void Forward3Button_Click(object sender, RoutedEventArgs e)
        {
            Forward3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Big.png", UriKind.Relative));

            ModuleUI_Motion parent = (ModuleUI_Motion)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            moduleInputControl.MoveForwardBackward(true);
        }

        private void Forward3Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Forward3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Big-Disabled.png", UriKind.Relative));
        }

        private void Back1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Drive(-smallMove);

            Back1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Small.png", UriKind.Relative));
        }

        private void Back1Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Back1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Small-Disabled.png", UriKind.Relative));
        }

        private void Back2Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Drive(-largeMove);

            Back2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Med.png", UriKind.Relative));
        }

        private void Back2Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Back2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Med-Disabled.png", UriKind.Relative));
        }

        private void Back3Button_Click(object sender, RoutedEventArgs e)
        {
            Back3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Big.png", UriKind.Relative));

            ModuleUI_Motion parent = (ModuleUI_Motion)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            moduleInputControl.MoveForwardBackward(false);
        }

        private void Back3Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Back3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Down-Big-Disabled.png", UriKind.Relative));
        }

        private void Left1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(-smallTurn));

            Left1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Small.png", UriKind.Relative));
        }

        private void Left1Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Left1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Small-Disabled.png", UriKind.Relative));
        }

        private void Left2Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(-mediumTurn));

            Left2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Med.png", UriKind.Relative));
        }

        private void Left2Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Left2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Med-Disabled.png", UriKind.Relative));
        }

        private void Left3Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(-largeTurn));

            Left3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Big.png", UriKind.Relative));
        }

        private void Left3Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Left3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Left-Big-Disabled.png", UriKind.Relative));
        }

        private void Right1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(smallTurn));

            Right1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Small.png", UriKind.Relative));
        }

        private void Right1Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Right1Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Small-Disabled.png", UriKind.Relative));
        }

        private void Right2Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(mediumTurn));

            Right2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Med.png", UriKind.Relative));
        }

        private void Right2Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Right2Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Med-Disabled.png", UriKind.Relative));
        }

        private void Right3Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.Turn(Angle.FromDegrees(largeTurn));

            Right3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Big.png", UriKind.Relative));
        }

        private void Right3Button_MouseUp(object sender, RoutedEventArgs e)
        {
            Right3Button.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Right-Big-Disabled.png", UriKind.Relative));
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Motion parent = (ModuleUI_Motion)base.ParentModule;
            if (parent != null)
                parent.StopMovement();

            StopButton.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Stop.png", UriKind.Relative));
        }

        private void StopButton_MouseUp(object sender, RoutedEventArgs e)
        {
            StopButton.Source = new BitmapImage(new Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Stop-Disabled.png", UriKind.Relative));
        }
    }
}