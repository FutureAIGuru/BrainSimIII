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
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System;

namespace BrainSimulator.Modules
{
    public partial class ModuleUI_EnvironmentDlg : ModuleBaseDlg
    {
        public ModuleUI_EnvironmentDlg()
        {
            InitializeComponent();
        }

        bool salliesView = false;

        public void Initialize()
        {
            HelixPort.SetView(new Point3D(0, 0, 150), new Vector3D(0, 0, -150), new Vector3D(0, 1, 0));

            Constructor3D construct = new();
            environmentModel.Children.Add(construct.Grid());
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

            if (salliesView)
            {
                ModuleUI_Environment parent = (ModuleUI_Environment)ParentModule;
                if (parent == null) return true;
                ModulePodInterface modulePodInterface = (ModulePodInterface)parent.FindModule(typeof(ModulePodInterface));
                if (modulePodInterface == null) return true;

                Point3DPlus cameraPosition = new Point3DPlus(0f, 3f, 4f);
                Vector3D angleCorrection = new Vector3D(0, 1, 0);
                Vector3D cameraDirection = modulePodInterface.HeadDirection - modulePodInterface.BodyDirection + angleCorrection;
                Vector3D cameraUpDirection = new Vector3D(0, 0, 1);
                HelixPort.SetView(cameraPosition, cameraDirection, cameraUpDirection);
            }

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
            ModuleUI_Environment parent = (ModuleUI_Environment)ParentModule;
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
            ModuleUI_Environment parent = (ModuleUI_Environment)ParentModule;
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
            ModuleUI_Environment parent = (ModuleUI_Environment)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        public void DrawObjects(List<ModelUIElement3D> theObjects)
        {
            Constructor3D construct = new();
            environmentModel.Children.Clear();
            environmentModel.Children.Add(construct.Grid());
            foreach (ModelUIElement3D anObject in theObjects)
            {
                environmentModel.Children.Add(anObject);
            }
        }

        private void ViewSwitchOff_Click(object sender, RoutedEventArgs e)
        {
            salliesView = true;

            ViewSwitchOff.Visibility = Visibility.Collapsed;
            ViewSwitchOn.Visibility = Visibility.Visible;
        }

        private void ViewSwitchOn_Click(object sender, RoutedEventArgs e)
        {
            salliesView = false;
            HelixPort.SetView(new Point3D(0, 0, 150), new Vector3D(0, 0, -150), new Vector3D(0, 1, 0));

            ViewSwitchOn.Visibility = Visibility.Collapsed;
            ViewSwitchOff.Visibility = Visibility.Visible;
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }
    }
}