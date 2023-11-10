//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModuleMentalModelDlg : ModuleBaseDlg
    {
        bool threeDInitialized = false;

        public ModulePodInterface thePod = null;

        public Dictionary<ModelUIElement3D, string> modelProperties = new(); //the properties to be displayed with an object

        ToolTip objectToolTip = new();

        public ModulePodInterface Pod()
        {
            if (thePod == null)
            {
                ModuleMentalModel theModule = (ModuleMentalModel)base.ParentModule;
                if (theModule == null) return null;
                thePod = (ModulePodInterface)theModule.FindModule("PodInterface");
            }
            return thePod;
        }

        public ModuleMentalModelDlg()
        {
            InitializeComponent();
            threeDInitialized = false;

            PerspectiveCamera cam = new PerspectiveCamera();
            cam.FieldOfView = 52;
            HelixPort.Viewport.Camera = cam;

            HelixPort.SetView(new Point3D(0, -25, 50), new Vector3D(0, 50, -50), new Vector3D(0, 1, 0));
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;
            if (parent.SalliesView)
            {
                SalliesViewCheckBox.IsChecked = true;
            }

            ModuleUKS UKS = (ModuleUKS)parent.FindModule("UKS");
            if (UKS == null) return false;
            Thing mentalModelRoot = UKS.Labeled("MentalModel");
            if (mentalModelRoot == null) return false;

            if (parent.MentalModelChanged || !threeDInitialized)
            {
                parent.MentalModelChanged = false;
                threeDInitialized = true;
                parent.UpdateMentalObjects();
            }

            if (parent.SalliesView && Pod() != null)
            {
                Point3DPlus cameraPosition = new Point3DPlus(0f, 3f, 4f);
                Vector3D angleCorrection = new Vector3D(0, 1, 0);
                Vector3D cameraDirection = Pod().HeadDirection - Pod().BodyDirection + angleCorrection;
                Vector3D cameraUpDirection = new Vector3D(0, 0, 1);
                HelixPort.SetView(cameraPosition, cameraDirection, cameraUpDirection);
            }
            return true;
        }

        public void DrawObjects(List<ModelUIElement3D> theObjects)
        {
            Constructor3D construct = new();
            ourObjects.Children.Clear();
            ourObjects.Children.Add(construct.Grid());
            foreach (ModelUIElement3D anObject in theObjects)
            {
                ourObjects.Children.Add(anObject);
            }
        }
        public void ClearObjects()
        {
            Constructor3D construct = new();
            ourObjects.Children.Clear();
            ourObjects.Children.Add(construct.Grid());
        }
        public void AddObjects(List<ModelUIElement3D> theObjects)
        {
            foreach (ModelUIElement3D anObject in theObjects)
            {
                ourObjects.Children.Add(anObject);
            }
        }
        public void DeleteObjects(List<ModelUIElement3D> theObjects)
        {
            foreach (ModelUIElement3D anObject in theObjects)
            {
                ourObjects.Children.Remove(anObject);
            }
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;
            Draw(false);
        }

        private void SalliesViewCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;
            parent.SalliesView = true;
        }
        private void SalliesViewCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;
            parent.SalliesView = false;
            HelixPort.SetView(new Point3D(0, 0, 150), new Vector3D(0, 0, -150), new Vector3D(0, 1, 0));
        }

        //if mouse is over an object, display properties of the object
        private void HelixPort_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.GetPosition(HelixPort);
            RayHitTestResult rayHitTestResult = VisualTreeHelper.HitTest(HelixPort, mousePoint) as RayHitTestResult;

            if (rayHitTestResult == null || rayHitTestResult.VisualHit as ModelUIElement3D == null)
            {
                objectToolTip.IsOpen = false;
                return;
            }
            try
            {
                if (!objectToolTip.IsOpen)
                {
                    objectToolTip.Content = modelProperties[rayHitTestResult.VisualHit as ModelUIElement3D];
                    objectToolTip.IsOpen = true;
                }
            }
            catch { }
        }

        //the mouse may leave the viewport too quickly for MouseMove to be called
        private void HelixPort_MouseLeave(object sender, MouseEventArgs e)
        {
            objectToolTip.IsOpen = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;
            parent.ResetMentalModel();
        }
    }
}