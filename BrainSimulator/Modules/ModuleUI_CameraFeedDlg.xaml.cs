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

namespace BrainSimulator.Modules
{
    public partial class ModuleUI_CameraFeedDlg : ModuleBaseDlg
    {
        public ModuleUI_CameraFeedDlg()
        {
            InitializeComponent();
        }

        ModuleUserInterface mainWindow;

        public void Initialize(float minPan, float maxPan, float minTilt, float maxTilt)
        {
            PanSlider.Minimum = minPan;
            PanSlider.Maximum = maxPan;
            TiltSlider.Minimum = minTilt;
            TiltSlider.Maximum = maxTilt;

            mainWindow = (ModuleUserInterface)(((ModuleUI_CameraFeed)ParentModule).FindModule(typeof(ModuleUserInterface)));
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

            ModuleUI_CameraFeed parent = (ModuleUI_CameraFeed)ParentModule;
            if (parent == null) return true;

            ModulePodCamera modulePodCamera = (ModulePodCamera)parent.FindModule(typeof(ModulePodCamera));
            if (modulePodCamera == null) return true;

            try
            {
                if(modulePodCamera.theBitmap != null)
                    CameraFeedImage.ImageSource = modulePodCamera.theBitmap.Clone();
            }
            catch { }

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

        private void Dlg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModuleUI_CameraFeed parent = (ModuleUI_CameraFeed)ParentModule;
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
            ModuleUI_CameraFeed parent = (ModuleUI_CameraFeed)ParentModule;
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
            ModuleUI_CameraFeed parent = (ModuleUI_CameraFeed)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        private void TiltSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWindow != null)
                mainWindow.TiltCamera(Angle.FromDegrees((float)TiltSlider.Value));
        }

        private void PanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWindow != null)
                mainWindow.PanCamera(Angle.FromDegrees((float)PanSlider.Value));
        }

        private void CenterViewButton_Click(object sender, RoutedEventArgs e)
        {
            CenterViewButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Center-Cam-Enabled.png", System.UriKind.Relative));

            if (mainWindow != null)
            {
                mainWindow.CenterCamera();
                TiltSlider.Value = 0;
                PanSlider.Value = 0;
            }
        }

        private void CenterViewButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            CenterViewButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Center-Cam-Disabled.png", System.UriKind.Relative));
        }

        private void CenterViewButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            CenterViewButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Center-Cam-Disabled.png", System.UriKind.Relative));
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }

        private void TakePictureButton_Click(object sender, RoutedEventArgs e)
        {
            TakePictureButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Snap-Shot-Enabled.png", System.UriKind.Relative));

            ModuleUserInterfaceDlg UI = (ModuleUserInterfaceDlg)Owner;
            UI.saveImage = true;
        }

        private void TakePictureButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TakePictureButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Snap-Shot-Disabled.png", System.UriKind.Relative));
        }

        private void TakePictureButton_MouseLeave(object sender, MouseEventArgs e)
        {
            TakePictureButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Camera/ICON-Snap-Shot-Disabled.png", System.UriKind.Relative));
        }
    }
}