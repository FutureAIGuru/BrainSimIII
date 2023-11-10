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
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public partial class Module3DSimControlDlg : ModuleBaseDlg
    {
        bool threeDInitialized = false;
        double sallieLastMoved = Sallie.lastMoved - 100; // make sure it triggers first time...
        ModulePodInterface thePod = null;

        public Module3DSimControlDlg()
        {
            InitializeComponent();
            threeDInitialized = false;
        }

        public ModulePodInterface Pod()
        {
            if (thePod == null)
            {
                Module3DSimControl theModule = (Module3DSimControl)base.ParentModule;
                if (theModule == null) return null;
                thePod = (ModulePodInterface)theModule.FindModule("PodInterface");
            }
            if (thePod == null)
            {
                Debug.WriteLine("ERROR: Module3DSimView needs ModulePodInterface to function fully.");
            }
            return thePod;
        }

        ModelUIElement3D SallieBody = null;
        ModelUIElement3D SallieHead = null;

        public void AddEnvironmentObjects(bool immediateRedrawRequested = false)
        {
            Module3DSimControl theModule = (Module3DSimControl)base.ParentModule;
            if (!threeDInitialized || theModule.recreateWorld)
            {
                Pod()?.ResetPosition();
                theModule.recreateWorld = false;
                ourObjects.Children.Clear();
                Constructor3D construct = new();
                ourObjects.Children.Add(construct.Floor());
                ourObjects.Children.Add(construct.Grid());

                if (theModule == null) return;
                Module3DSimModel theEnvironment = (Module3DSimModel)theModule.FindModule("3DSimModel");
                if (theEnvironment == null) return;

                foreach (EnvironmentObject shape in theEnvironment.ourThings)
                {
                    if (shape == null) continue;
                    if (shape.shape == "Cube")
                    {
                        ourObjects.Children.Add(construct.Cube(shape as Cube));
                    }
                    else if (shape.shape == "Sphere")
                    {
                        ourObjects.Children.Add(construct.Sphere(shape as Sphere));
                    }
                    else if (shape.shape == "Cylinder")
                    {
                        ourObjects.Children.Add(construct.Cylinder(shape as Cylinder));
                    }
                    else if (shape.shape == "Cone")
                    {
                        ourObjects.Children.Add(construct.Cone(shape as Cone));
                    }
                    else if (shape.shape == "Wall")
                    {
                        ourObjects.Children.Add(construct.Wall(shape as Wall));
                    }
                    else if (shape.shape == "Triangle2D")
                    {
                        ourObjects.Children.Add(construct.Triangle2D(shape as Triangle2D));
                    }
                }
                SallieBody = Sallie.ConstructBody();
                ourObjects.Children.Add(SallieBody);
                SallieHead = Sallie.ConstructHead();
                ourObjects.Children.Add(SallieHead);
            }

            threeDInitialized = true;
        }

        public override bool Draw(bool checkDrawTimer)
        {
            AddEnvironmentObjects();
            Module3DSimControl parent = (Module3DSimControl)base.ParentModule;
            if (!base.Draw(checkDrawTimer)) return false;

            if (SallieBody != null && Sallie.lastMoved > sallieLastMoved)
            {
                SallieBody.Transform = Sallie.GetBodyTransform();
                SallieHead.Transform = Sallie.GetHeadTransform();
                sallieLastMoved = Sallie.lastMoved;
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!threeDInitialized)
                HelixPort.SetView(new Point3D(0, 0, 150), new Vector3D(0, 0, -150), new Vector3D(0, 1, 0));
            Draw(false);
        }
    }
}