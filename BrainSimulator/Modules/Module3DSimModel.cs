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
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class EnvironmentObject : DependencyObject
    {
        public EnvironmentObject()
        {
            shape = "Unknown";
        }

        public EnvironmentObject(PhysicalObject po)
        {
            size = 3f;
            position = po.center;
            color = po.color;
            rotation = 0;
           // handle = Utils.Random(11111, 99999); //handle is already set by the base
        }

        public string shape;
        public float size;
        public Point3DPlus position;//position in mental model dialogue, does not match position in UKS
        public string color;
        public double rotation;
        public string handle = Utils.Random(11111, 99999);
        public Point3DPlus center;//matches position in UKS
        public string visibility;
        public string height;
        public string width;

        override public string ToString()
        {
            string shapeString = shape + " " + color + " " + size;
            shapeString += " (" + position.X + ", " + position.Y + ", " + position.Z + ") ";
            shapeString += rotation;
            return shapeString;
        }
    }

    public class Cube : EnvironmentObject
    {
        public Cube()
        {
            shape = "Cube";
        }
        public Cube(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }

    public class Sphere : EnvironmentObject
    {
        public Sphere()
        {
            shape = "Sphere";
        }
        public Sphere(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }
    public class Cylinder : EnvironmentObject
    {
        public Cylinder()
        {
            shape = "Cylinder";
        }
        public Cylinder(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }
    public class Cone : EnvironmentObject
    {
        public Cone()
        {
            shape = "Cone";
        }
        public Cone(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }

    public class Wall : EnvironmentObject
    {
        public Wall()
        {
            shape = "Wall";
        }
        public Wall(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }

    public class Triangle2D : EnvironmentObject
    {
        public Triangle2D()
        {
            shape = "Triangle2D";
        }
        public Triangle2D(PhysicalObject po) : base(po)
        {
            shape = po.shape;
        }
    }

    public class Module3DSimModel : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        public List<EnvironmentObject> ourThings = new();

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public Module3DSimModel()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        public void SetObjectsInUKS()
        {
            MainWindow.SuspendEngine();

            // Reset Sallie's position if not an active Pod...
            ModulePodInterface thePod = (ModulePodInterface)FindModule("PodInterface");
            if (thePod != null && !thePod.isLive)
            {
                thePod.ResetPosition();
                thePod.UpdateSallieSelf();
            }

            // Remove all PhysicalObjects from the Mental Model...
            ModuleMentalModel theModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theModel == null) return;
            theModel.Clear();

            // Store all 3DSim Model objects as PhysicalObjects in Mental Model...
            foreach (EnvironmentObject block in ourThings)
            {
                Dictionary<string, object> properties = FillProperties(block);
                theModel.AddPhysicalObject(properties);
            }

            // Add colors to properties if needed...
            GetUKS();
            if (UKS.Labeled("col").Children.Count == 0)
            {
                MainWindow.ResumeEngine();
                return;
            }
            foreach (Thing color in UKS.Labeled("col").Children)
            {
                if (color.V != null)
                {
                    string colorName = color.V.ToString();
                    color.V = null;
                    UKS.GetOrAddThing("cv*", color, new HSLColor(Utils.ColorFromName(colorName)));
                }
            }

            MainWindow.ResumeEngine();
        }

        Dictionary<string, object> FillProperties(EnvironmentObject block)
        {
            Dictionary<string, object> properties = new();

            // Assume a start position for Sallie, at center of model space
            // We will calculate the Point3DPlus objects based on that.
            // First make them all the same...
            // Center the Center...
            Point3DPlus Center = block.position.Clone();
            Point3DPlus TopLeft = block.position.Clone();
            Point3DPlus TopRight = block.position.Clone();
            Point3DPlus BottomLeft = block.position.Clone();
            Point3DPlus BottomRight = block.position.Clone();
            Center.Conf = 1;

            // Then correct Z based on size...
            TopLeft.Z = (float)block.position.Z / 4.0f;
            TopRight.Z = (float)block.position.Z / 4.0f;
            BottomLeft.Z = 0;
            BottomRight.Z = 0;

            // Turn all angles 90 degrees...
            Center.Theta -= Angle.FromDegrees(90);
            TopLeft.Theta -= Angle.FromDegrees(90);
            TopRight.Theta -= Angle.FromDegrees(90);
            BottomLeft.Theta -= Angle.FromDegrees(90);
            BottomRight.Theta -= Angle.FromDegrees(90);

            // Next correct angular width based on distance...
            // We do this by calculating the Phi difference between 
            // Top and Bottom, and using this to determine Theta
            Angle sizeTheta = (TopLeft.Phi - BottomLeft.Phi) / ((float)block.position.Z / (float)block.size) * 2.0f;
            Angle sizePhi = sizeTheta * (float)block.size / (float)block.position.Z / 2.0f;

            // Now we need to rotate the extremities around the center 
            // based on the rotation parameter of the physical object.
            // (if it is a wall)
            TopLeft.Theta -= sizeTheta;  // temporarily set this...
            Point3DPlus extension = TopLeft - Center;
            extension.Theta -= Center.Theta;
            extension.Theta += Angle.FromDegrees((float)block.rotation);

            // and reset this again...
            TopLeft = block.position.Clone();
            TopLeft.Z = block.position.Z / 4.0f;
            TopLeft.Theta -= Angle.FromDegrees(90);

            extension.Theta += Angle.FromDegrees(270); // + block.position.Theta;
            Point3DPlus rotatedExtension = extension;

            rotatedExtension.Theta += sizeTheta;
            rotatedExtension.Phi += sizePhi;
            TopLeft += rotatedExtension;

            rotatedExtension = extension;
            rotatedExtension.Theta += sizeTheta;
            rotatedExtension.Phi -= sizePhi;
            BottomLeft += rotatedExtension;
            TopLeft.Y = BottomLeft.Y;

            extension.Theta += Angle.FromDegrees(180);
            rotatedExtension = extension;
            rotatedExtension.Theta -= sizeTheta;
            rotatedExtension.Phi += sizePhi;
            TopRight += rotatedExtension;

            rotatedExtension = extension;
            rotatedExtension.Theta -= sizeTheta;
            rotatedExtension.Phi -= sizePhi;
            BottomRight += rotatedExtension;
            TopRight.Y = BottomRight.Y;

            // Move all points up to account for head height...
            Center.Z += 4;
            TopLeft.Z += 4;
            TopRight.Z += 4;
            BottomLeft.Z += 4;
            BottomRight.Z += 4;

            // Center the Center...
            Center.Z = (TopLeft.Z + BottomLeft.Z) / 2.0f;

            // Add everything the Dictionary...
            properties.Add("cen", Center);
            properties.Add("tpl", TopLeft);
            properties.Add("tpr", TopRight);
            properties.Add("btl", BottomLeft);
            properties.Add("btr", BottomRight);
            properties.Add("wid", (double)sizeTheta.Degrees / Center.R * 111);
            properties.Add("hig", (double)sizeTheta.Degrees / Center.R * 111);
            properties.Add("shp", block.shape);
            properties.Add("col", block.color);
            properties.Add("vsb", "Visible");
            properties.Add("app", "New");
            properties.Add("siz", block.size);

            return properties;
        }

        public override MenuItem CustomContextMenuItems()
        {
            MenuItem mi = new MenuItem();
            mi.Header = "Set in UKS";
            mi.Click += Mi_Click;
            return mi;
        }

        private void Mi_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetObjectsInUKS();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
