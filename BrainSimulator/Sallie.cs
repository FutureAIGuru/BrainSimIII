//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Emgu.CV;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    // The Sallie class has been implemented to keep all information about Sallie's whereabouts 
    // and her various positions and angles together, so we need only pass her to the various 
    // Modules instead of a whole bunch of more primitive variables of different types. 
    // There can be multiple instances of Sallie, but they all share the same set of static variables.
    // That way there can be no discrepancies between the various instances.
    public static class Sallie
    {
        // Queue of images for image recognition
        public static Queue<CameraFeedEntry> VideoQueue = new();
        public static Mat TestImage { get; set; }
        public static string TestImageFilename { get; set; }
        public static string TestFolderName { get; set; } = "";
        public static string[] TestFolderFilenames { get; set; }
        public static int TestFolderIndex { get; set; }

        // These same default values are also set in ResetPosition

        // These attributes are the leading values
        private static Point3DPlus bodyPosition = new Point3DPlus(0.0f, 0.0f, 0.0f);
        private static Angle bodyAngle = 0;
        private static Angle cameraPan = 0;
        private static Angle cameraTilt = 0;

        // These attributes, while needed, are calculated from the above.
        private static Point3DPlus headPosition = new Point3DPlus(0.0f, 0.0f, 2f);
        private static Point3DPlus armPosition = new Point3DPlus(0.0f, 0.0f, 4f);
        private static Vector3D bodyUpDirection = new Vector3D(0, 0, 1);
        private static Vector3D bodyDirection = new Vector3D(0, 1, 0);
        private static Vector3D headDirection = new Vector3D(0, 1, 0);

        // this sets the time Sallie last moved anything...
        public static double lastMoved = Utils.GetPreciseTime();

        [XmlIgnore]
        public static Vector3D BodyUpDirection { get => bodyUpDirection; set { bodyUpDirection = value; Check(); } }
        [XmlIgnore]
        public static Vector3D BodyDirection { get => bodyDirection; set { bodyDirection = value; Check(); } }
        [XmlIgnore]
        public static Vector3D HeadDirection { get => headDirection; set { headDirection = value; Check(); } }
        [XmlIgnore]
        public static Point3DPlus BodyPosition { get => bodyPosition; set { bodyPosition = value; Check(); } }
        [XmlIgnore]
        public static Point3DPlus HeadPosition {
            get => headPosition;
            set { headPosition = value; Check(); } }
        [XmlIgnore]
        public static Point3DPlus ArmPosition { get => armPosition; set { armPosition = value; Check(); } }
        [XmlIgnore]
        public static Angle BodyAngle { get => bodyAngle; set { bodyAngle = value; Check(); } }
        [XmlIgnore]
        public static Angle CameraPan { get => cameraPan; set { cameraPan = value; Check(); } }
        [XmlIgnore]
        public static Angle CameraTilt { get => cameraTilt; set { cameraTilt = value; Check(); } }

        private static Stopwatch centralStopWatch = new Stopwatch();

        public static void StartCentralStopWatch()
        {
            centralStopWatch.Start();
        }

        public static double StopCentralStopWatch()
        {
            centralStopWatch.Stop();
            double elapsed = centralStopWatch.ElapsedMilliseconds;
            centralStopWatch.Reset();
            return elapsed;
        }

        // This Check method makes sure none of the variables have invalid values. 
        public static void Check()
        {
            // body angle should be +/- 360 degrees
            if (bodyAngle < Angle.FromDegrees(-360)) bodyAngle += Angle.FromDegrees(360);
            if (bodyAngle > Angle.FromDegrees(360)) bodyAngle -= Angle.FromDegrees(360);

            // camera pan restricted to +/- 90 degrees
            if (cameraPan < Angle.FromDegrees(-90)) cameraPan = -Angle.FromDegrees(90);
            if (cameraPan > Angle.FromDegrees(90)) cameraPan = Angle.FromDegrees(90);

            // camera tilt restricted to +/- 90  degrees
            if (cameraTilt < Angle.FromDegrees(-90)) cameraTilt = -Angle.FromDegrees(90);
            if (cameraTilt > Angle.FromDegrees(90)) cameraTilt = Angle.FromDegrees(90);

            lastMoved = Utils.GetPreciseTime();
            CalculateDependentAttributes();
        }

        public static void CalculateDependentAttributes()
        {
            // This actually never changes, is needed for the View dialog 3D viewport camera
            bodyUpDirection = new Vector3D(0, 0, 1);

            // headposition always body with Z = 2.0
            if (BodyPosition == null) return;

            headPosition = bodyPosition.Clone();
            headPosition.Z = 2.0f;

            // update Sallie's Direction...
            Point3DPlus bodyVector = new Point3DPlus(1, (Angle)(bodyAngle), (Angle)0);
            bodyDirection = new Vector3D(bodyVector.Y, bodyVector.X, 0);
            bodyDirection.Normalize();

            // update Sallie's Head Direction...
            Point3DPlus headVector = new Point3DPlus(1, (Angle)(bodyAngle - cameraPan), (Angle)cameraTilt);
            headDirection = new Vector3D(headVector.Y, headVector.X, headVector.Z);
            headDirection.Normalize();

            lastMoved = Utils.GetPreciseTime();
        }

        public static Transform3DGroup GetBodyTransform()
        {
            Transform3DGroup myBodyTransformer = new();
            TranslateTransform3D myBodyTranslate = new TranslateTransform3D((Vector3D)(Point3D)BodyPosition);
            RotateTransform3D myBodyRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                                   Convert.ToDouble(-BodyAngle.Degrees)),
                                                                   (Point3D)BodyPosition);
            myBodyTransformer.Children.Add(myBodyTranslate);
            myBodyTransformer.Children.Add(myBodyRotate);
            return myBodyTransformer;
        }

        public static Transform3DGroup GetHeadTransform()
        {
            Transform3DGroup myHeadTransformer = new();
            TranslateTransform3D myHeadTranslate = new TranslateTransform3D((Vector3D)(Point3D)BodyPosition);
            RotateTransform3D myHeadPan = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                               Convert.ToDouble(-BodyAngle.Degrees + CameraPan.Degrees)),
                                                               (Point3D)HeadPosition);
            RotateTransform3D myHeadTilt = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0),
                                                               Convert.ToDouble(CameraTilt.Degrees)), (Point3D)HeadPosition);
            myHeadTransformer.Children.Add(myHeadTranslate);
            myHeadTransformer.Children.Add(myHeadTilt);
            myHeadTransformer.Children.Add(myHeadPan);
            return myHeadTransformer;
        }

        public static ModelUIElement3D ConstructBody()
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(0, 0, 2.5);
            Point3D posPoint1 = new Point3D(0, 0, 0);
            Point3D centerScewTop = new Point3D(0, 0, 8.3);
            Point3D eyeCylinderStart = new Point3D(1, 3.8, 4.5);
            Point3D eyeCylinderEnd = new Point3D(-1, 3.8, 4.5);

            // Eye Cylinder - Needs to be sorted with head.
            meshBuilder.AddCylinder(eyeCylinderStart, eyeCylinderEnd, 2);
            // Screw Cylinder
            meshBuilder.AddCylinder(posPoint, centerScewTop, .4);
            meshBuilder.AddCylinder(posPoint, posPoint1, 6.1);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush("Black"));
            element.Model = geometry;
            element.Transform = GetBodyTransform();
            return element;
        }

        public static ModelUIElement3D ConstructHead()
        {

            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            meshBuilder.AddSphere(headPosition, 5.95, 100, 100);
            //meshBuilder.AddSphere(headPosition, 2, 100, 100);
            // draw the lines of sight
            Point3D viewLineLeft = headPosition.Clone();
            Point3D viewLineRight = headPosition.Clone();
            viewLineLeft.X -= 1.5;
            viewLineRight.X += 1.5;
            viewLineLeft.Y += 1.45;
            viewLineRight.Y += 1.45;
            viewLineLeft.Z = 4;
            viewLineRight.Z = 4;
            for (int i = 0; i < 20; i++)
            {
                viewLineLeft.X -= .5;
                viewLineRight.X += .5;
                viewLineLeft.Y += 1.0;
                viewLineRight.Y += 1.0;
                meshBuilder.AddSphere(viewLineLeft, 0.4, 100, 100);
                meshBuilder.AddSphere(viewLineRight, 0.4, 100, 100);
            }

            //Point3D nosePoint = headPosition;
            //nosePoint.Y = 2.25;
            //meshBuilder.AddSphere(nosePoint, 0.5, 100, 100); // Nose
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush("Magenta"));
            element.Model = geometry;
            element.Transform = GetHeadTransform();
            return element;
        }

        public static void ResetPosition()
        {
            // These attributes are the leading values
            bodyPosition = new Point3DPlus(0.0f, 0.0f, 0.0f);
            bodyAngle = 0;
            cameraPan = 0;
            cameraTilt = 0;
            CalculateDependentAttributes();
        }

        public static void ResetCamera()
        {
            cameraPan = 0;
            cameraTilt = 0;
            CalculateDependentAttributes();
        }

        public static void Move(double distance)
        {
            // BodyDirection is a vector of length 1
            // bodyPosition is only for 3D space, 
            // in SallieSpace bodyposition is always (0, 0, 0)
            BodyPosition += (Point3DPlus)(Point3D)(BodyDirection * distance);
        }
    }

    public class CameraFeedEntry
    {
        public string filename { get; set; }
        public Mat image { get; set; }

        public CameraFeedEntry(string filename, Mat image)
        {
            this.filename = filename;
            this.image = image;
        }
    }
}