//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleUnitTests : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleUnitTests()
        {
            // Debug.WriteLine("ModuleUnitTests:ModuleUnitTests()");
            minHeight = 2;
            maxHeight = 500;
            minWidth = 2;
            maxWidth = 500;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            // Debug.WriteLine("ModuleUnitTests:Fire()");
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            // Debug.WriteLine("ModuleUnitTests:Initialize()");

            // Only the Initialize method is used here, to fire off the various unit test methods.
            TestPoint3DPlus();
        }

        private static int TestPoint3DPlus()
        {
            int errorCount = 0;
            try
            {
                Point3DPlus point0 = new Point3DPlus();
                Point3DPlus point0a = point0.Clone();
                Point3DPlus point1 = new Point3DPlus((float)1.0, (float)1.0, (float)1.0);
                Point3DPlus point2 = new Point3DPlus((float)1.0, (float)1.0, (float)1.0);
                Point3DPlus pointX = new Point3DPlus((float)1.0, (float)0.0, (float)0.0);
                Point3DPlus pointY = new Point3DPlus((float)0.0, (float)1.0, (float)0.0);
                Point3DPlus pointZ = new Point3DPlus((float)0.0, (float)0.0, (float)1.0);
                Point3DPlus pointR = new Point3DPlus((float)1.0, (Angle)0.0, (Angle)0.0);
                Point3DPlus pointT = new Point3DPlus((float)1.0, (Angle)Math.PI, (Angle)0.0);
                Point3DPlus pointP = new Point3DPlus((float)1.0, (Angle)0.0, (Angle)Math.PI/2.0);
                Point3DPlus pointQ1 = new Point3DPlus((float)1.0, (float)1.0, (float)1.0);
                Point3DPlus pointQ2 = new Point3DPlus((float)-1.0, (float)1.0, (float)1.0);
                Point3DPlus pointQ3 = new Point3DPlus((float)1.0, (float)-1.0, (float)1.0);
                Point3DPlus pointQ4 = new Point3DPlus((float)-1.0, (float)-1.0, (float)1.0);
                Point3DPlus pointQ5 = new Point3DPlus((float)1.0, (float)1.0, (float)-1.0);
                Point3DPlus pointQ6 = new Point3DPlus((float)-1.0, (float)1.0, (float)-1.0);
                Point3DPlus pointQ7 = new Point3DPlus((float)1.0, (float)-1.0, (float)-1.0);
                Point3DPlus pointQ8 = new Point3DPlus((float)-1.0, (float)-1.0, (float)-1.0);

                Debug.Assert(point0 != point1);
                Debug.Assert(point0 == point0a);
                Debug.Assert(pointX.R == pointY.R);
                Debug.Assert(pointZ.R == pointY.R);

                Debug.Assert(point0.ToString() == "R: 0.000, Theta: 0.000°, Phi: 0.000° (0.00,0.00,0.00) Conf:0.000");
                Debug.Assert(point0.X == 0);
                Debug.Assert(point0.Y == 0);
                Debug.Assert(point0.Z == 0);
                Debug.Assert(point0.R == 0);
                Debug.Assert(point0.Theta == 0);
                Debug.Assert(point0.Phi == 0);

                Debug.Assert(point0a.ToString() == "R: 0.000, Theta: 0.000°, Phi: 0.000° (0.00,0.00,0.00) Conf:0.000");
                Debug.Assert(point0a.X == 0);
                Debug.Assert(point0a.Y == 0);
                Debug.Assert(point0a.Z == 0);
                Debug.Assert(point0a.R == 0);
                Debug.Assert(point0a.Theta == 0);
                Debug.Assert(point0a.Phi == 0);

                Debug.Assert(point1.ToString() == "R: 1.732, Theta: 45.000°, Phi: 35.264° (1.00,1.00,1.00) Conf:0.000");
                Debug.Assert(point1.X == 1.0);
                Debug.Assert(point1.Y == 1.0);
                Debug.Assert(point1.Z == 1.0);
                Debug.Assert(point1.R - 1.73205078 < 0.00000001);
                Debug.Assert(point1.Theta - (Math.PI / 4.0) < 0.00000001);
                Debug.Assert(point1.Phi - 0.6154797 < 0.00000001);

                Debug.Assert(point2.ToString() == "R: 1.732, Theta: 45.000°, Phi: 35.264° (1.00,1.00,1.00) Conf:0.000");
                Debug.Assert(point2.X == 1.0);
                Debug.Assert(point2.Y == 1.0);
                Debug.Assert(point2.Z == 1.0);
                Debug.Assert(point2.R - 1.73205078 < 0.00000001);
                Debug.Assert(point2.Theta - (Math.PI / 2.0) < 0.00000001);
                Debug.Assert(point2.Phi - 0.6154797 < 0.00000001);

                Debug.Assert(pointX.ToString() == "R: 1.000, Theta: 0.000°, Phi: 0.000° (1.00,0.00,0.00) Conf:0.000");
                Debug.Assert(pointX.X == 1.0);
                Debug.Assert(pointX.Y == 0.0);
                Debug.Assert(pointX.Z == 0.0);
                Debug.Assert(pointX.R - 1.0 < 0.00000001);
                Debug.Assert(pointX.Theta - (Math.PI / 2.0) < 0.00000001);
                Debug.Assert(pointX.Phi - 0.6154797 < 0.00000001);

                Debug.Assert(pointY.ToString() == "R: 1.000, Theta: 90.000°, Phi: 0.000° (0.00,1.00,0.00) Conf:0.000");
                Debug.Assert(pointY.X == 0.0);
                Debug.Assert(pointY.Y == 1.0);
                Debug.Assert(pointY.Z == 0.0);
                Debug.Assert(pointY.R - 1.0 < 0.00000001);
                Debug.Assert(pointY.Theta - (Math.PI / 2.0) < 0.00000001);
                Debug.Assert(pointY.Phi - 0.6154797 < 0.00000001);

                Debug.Assert(pointZ.ToString() == "R: 1.000, Theta: 0.000°, Phi: 90.000° (0.00,0.00,1.00) Conf:0.000");
                Debug.Assert(pointZ.X == 0.0);
                Debug.Assert(pointZ.Y == 0.0);
                Debug.Assert(pointZ.Z == 1.0);
                Debug.Assert(pointZ.R - 1.0 < 0.00000001);
                Debug.Assert(pointZ.Theta - (Math.PI / 2.0) < 0.00000001);
                Debug.Assert(pointZ.Phi - 1.57079637 < 0.00000001);

                Debug.Assert(pointR.ToString() == "R: 1.000, Theta: 0.000°, Phi: 0.000° (1.00,0.00,0.00) Conf:0.000");
                Debug.Assert(pointR.X - 1.0 < 0.00000001);
                Debug.Assert(pointR.Y < 0.00000001);
                Debug.Assert(pointR.Z - 0.84 < 0.00000001);
                Debug.Assert(pointR.R - 1.0 < 0.00000001);
                Debug.Assert(pointR.Theta < 0.00000001);
                Debug.Assert(pointR.Phi - (Math.PI / 4.0) < 0.00000001);

                Debug.Assert(pointT.ToString() == "R: 1.000, Theta: 180.000°, Phi: 0.000° (-1.00,-0.00,0.00) Conf:0.000");
                Debug.Assert(pointT.X == -1.0);
                Debug.Assert(pointT.Y < 0.00000001);
                Debug.Assert(pointT.Z < 0.00000001);
                Debug.Assert(pointT.R - 1.0 < 0.00000001);
                Debug.Assert(pointT.Theta - Math.PI < 0.00000001);
                Debug.Assert(pointT.Phi - (Math.PI / 2.0) < 0.00000001);

                Debug.Assert(pointP.ToString() == "R: 1.000, Theta: 0.000°, Phi: 90.000° (-0.00,-0.00,1.00) Conf:0.000");
                Debug.Assert(pointP.X < 0.00000001);
                Debug.Assert(pointP.Y < 0.00000001);
                Debug.Assert(pointP.Z == 1.0);
                Debug.Assert(pointP.R - 1.0 < 0.00000001);
                Debug.Assert(pointP.Theta < 0.00000001);
                Debug.Assert(pointP.Phi - (Math.PI / 2.0) < 0.00000001);

                Debug.Assert(pointQ1.ToString() == "R: 1.732, Theta: 45.000°, Phi: 35.264° (1.00,1.00,1.00) Conf:0.000");
                Debug.Assert(pointQ2.ToString() == "R: 1.732, Theta: 135.000°, Phi: 35.264° (-1.00,1.00,1.00) Conf:0.000");
                Debug.Assert(pointQ3.ToString() == "R: 1.732, Theta: -45.000°, Phi: 35.264° (1.00,-1.00,1.00) Conf:0.000");
                Debug.Assert(pointQ4.ToString() == "R: 1.732, Theta: -135.000°, Phi: 35.264° (-1.00,-1.00,1.00) Conf:0.000");
                Debug.Assert(pointQ5.ToString() == "R: 1.732, Theta: 45.000°, Phi: -35.264° (1.00,1.00,-1.00) Conf:0.000");
                Debug.Assert(pointQ6.ToString() == "R: 1.732, Theta: 135.000°, Phi: -35.264° (-1.00,1.00,-1.00) Conf:0.000");
                Debug.Assert(pointQ7.ToString() == "R: 1.732, Theta: -45.000°, Phi: -35.264° (1.00,-1.00,-1.00) Conf:0.000");
                Debug.Assert(pointQ8.ToString() == "R: 1.732, Theta: -135.000°, Phi: -35.264° (-1.00,-1.00,-1.00) Conf:0.000");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                errorCount++;
            }
            if (errorCount > 0)
            {
                Debug.WriteLine(errorCount +" tests failed");
            }
            return errorCount;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
            // Debug.WriteLine("ModuleUnitTests:SetUpBeforeSave()");
        }
        public override void SetUpAfterLoad()
        {
            // Debug.WriteLine("ModuleUnitTests:SetUpAfterLoad()");
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            // Debug.WriteLine("ModuleUnitTests:SizeChanged()");
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
