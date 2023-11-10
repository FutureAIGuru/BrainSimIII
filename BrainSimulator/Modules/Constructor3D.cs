//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace BrainSimulator.Modules
{
    public class Constructor3D
    {
        public RectangleVisual3D Floor()
        {
            RectangleVisual3D Floor = new RectangleVisual3D()
            {
                LengthDirection = new Vector3D(1, 0, 0),
                Normal = new Vector3D(0, 0, 1),
                Origin = new Point3D(0, 0, -0.01),
                Length = 1000,
                Width = 1000,
                Fill = Brushes.Gray,
                Material = MaterialHelper.CreateMaterial(Brushes.Gray, Brushes.Gray)
            };
            return Floor;
        }

        public GridLinesVisual3D Grid()
        {
            GridLinesVisual3D GridLines = new()
            {
                LengthDirection = new Vector3D(1, 0, 0),
                Normal = new Vector3D(0, 0, 1),
                Center = new Point3D(0, 0, 0),
                Length = 1000,
                Width = 1000,
            };
            return GridLines;
        }

        public ModelUIElement3D Cube(Cube envCube)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envCube.position.X, envCube.position.Y, envCube.position.Z);
            meshBuilder.AddBox(posPoint, envCube.size, envCube.size, envCube.size);
            geometry.Geometry = meshBuilder.ToMesh();
            if (envCube.color == null) envCube.color = "Red";
            geometry.Material = new DiffuseMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(envCube.color)));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                               Convert.ToDouble(envCube.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public ModelUIElement3D Sphere(Sphere envSphere)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envSphere.position.X, envSphere.position.Y, envSphere.position.Z);
            meshBuilder.AddSphere(posPoint, envSphere.size / 2.0, 100, 100);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush(envSphere.color));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                             Convert.ToDouble(envSphere.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }
        public ModelUIElement3D Cylinder(Cylinder envCylinder)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envCylinder.position.X, envCylinder.position.Y, envCylinder.position.Z - (envCylinder.size / 2));
            Point3D apexPoint = new Point3D(envCylinder.position.X, envCylinder.position.Y, envCylinder.position.Z + (envCylinder.size / 2));
            meshBuilder.AddCylinder(posPoint, apexPoint, envCylinder.size / 2.0);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush(envCylinder.color));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                             Convert.ToDouble(envCylinder.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public ModelUIElement3D Cone(Cone envCone)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envCone.position.X, envCone.position.Y, envCone.position.Z - (envCone.size / 2));
            Point3D apexPoint = new Point3D(envCone.position.X, envCone.position.Y, envCone.position.Z + (envCone.size / 2));
            meshBuilder.AddCone(posPoint, apexPoint, envCone.size / 2.0, true, 100);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush(envCone.color));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                             Convert.ToDouble(envCone.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public ModelUIElement3D Wall(Wall envWall)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envWall.position.X, envWall.position.Y, 0);
            meshBuilder.AddBox(posPoint, 1, envWall.size, envWall.position.Z);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush(envWall.color));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                               Convert.ToDouble(envWall.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public ModelUIElement3D Triangle2D(Triangle2D envTriangle2D)
        {
            var element = new ModelUIElement3D();
            Point3D posPoint = new Point3D(envTriangle2D.position.X, envTriangle2D.position.Y, 0);
            Color theColor = Utils.ColorFromName(envTriangle2D.color);
            float size = envTriangle2D.size;
            var p0 = new Point3D(0, 0, size);
            var p1 = new Point3D(size/2, 0, 0);
            var p2 = new Point3D(size/-2, 0, 0);
            element = Triangle(p0, p1, p2, theColor);
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                               Convert.ToDouble(envTriangle2D.rotation)));
            TranslateTransform3D myTranslate = new TranslateTransform3D(envTriangle2D.position.X, 
                                                                        envTriangle2D.position.Y,
                                                                        envTriangle2D.position.Z - size/2);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public ModelUIElement3D Triangle(Point3DPlus p1, Point3DPlus p2, Point3DPlus p3, Color c)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            meshBuilder.AddTriangle(p1.P, p2.P, p3.P);
            meshBuilder.AddTriangle(p1.P, p3.P, p2.P);
            geometry.Geometry = meshBuilder.ToMesh();
            //geometry.Material = new SpecularMaterial(new SolidColorBrush(c), 0.0);
            geometry.Material = new DiffuseMaterial(new SolidColorBrush(c));
            element.Model = geometry;

            return element;
        }

        public ModelUIElement3D MM_Wall(Wall envWall)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D posPoint = new Point3D(envWall.position.X, envWall.position.Y, 0);
            meshBuilder.AddBox(posPoint, 1, 8, 4);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush(envWall.color));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                               Convert.ToDouble(envWall.rotation)), posPoint);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

        public void PopulateShapeBox(ComboBox ShapeBox)
        {
            // construct the shapes to choose from
            ShapeBox.Items.Clear();
            ComboBoxItem cboxitem1 = new ComboBoxItem(); cboxitem1.Content = "Cylinder"; ShapeBox.Items.Add(cboxitem1);
            ComboBoxItem cboxitem2 = new ComboBoxItem(); cboxitem2.Content = "Cone"; ShapeBox.Items.Add(cboxitem2);
            ComboBoxItem cboxitem3 = new ComboBoxItem(); cboxitem3.Content = "Cube";   ShapeBox.Items.Add(cboxitem3);
            ComboBoxItem cboxitem4 = new ComboBoxItem(); cboxitem4.Content = "Sphere"; ShapeBox.Items.Add(cboxitem4);
            ComboBoxItem cboxitem5 = new ComboBoxItem(); cboxitem5.Content = "Wall";   ShapeBox.Items.Add(cboxitem5);
            ComboBoxItem cboxitem6 = new ComboBoxItem(); cboxitem6.Content = "Triangle2D"; ShapeBox.Items.Add(cboxitem6);
        }

        public void PopulateColorBox(ComboBox ColorBox)
        {
            // construct the colors to choose from, non recognized colors commented out... 
            ColorBox.Items.Clear();
            ComboBoxItem cboxitema = new ComboBoxItem(); cboxitema.Content = "Black"; ColorBox.Items.Add(cboxitema);
            ComboBoxItem cboxitemb = new ComboBoxItem(); cboxitemb.Content = "White"; ColorBox.Items.Add(cboxitemb);
            ComboBoxItem cboxitemc = new ComboBoxItem(); cboxitemc.Content = "Red"; ColorBox.Items.Add(cboxitemc);
            ComboBoxItem cboxitemd = new ComboBoxItem(); cboxitemd.Content = "Lime"; ColorBox.Items.Add(cboxitemd);
            ComboBoxItem cboxiteme = new ComboBoxItem(); cboxiteme.Content = "Blue"; ColorBox.Items.Add(cboxiteme);
            ComboBoxItem cboxitemf = new ComboBoxItem(); cboxitemf.Content = "Yellow"; ColorBox.Items.Add(cboxitemf);
            ComboBoxItem cboxitemg = new ComboBoxItem(); cboxitemg.Content = "Cyan"; ColorBox.Items.Add(cboxitemg);
            ComboBoxItem cboxitemh = new ComboBoxItem(); cboxitemh.Content = "Magenta"; ColorBox.Items.Add(cboxitemh);
            ComboBoxItem cboxitemi = new ComboBoxItem(); cboxitemi.Content = "Orange"; ColorBox.Items.Add(cboxitemi);
            //ComboBoxItem cboxitemj = new ComboBoxItem(); cboxitemj.Content = "Silver"; ColorBox.Items.Add(cboxitemj);
            //ComboBoxItem cboxitemk = new ComboBoxItem(); cboxitemk.Content = "Gray"; ColorBox.Items.Add(cboxitemk);
            //ComboBoxItem cboxiteml = new ComboBoxItem(); cboxiteml.Content = "Maroon"; ColorBox.Items.Add(cboxiteml);
            //ComboBoxItem cboxitemm = new ComboBoxItem(); cboxitemm.Content = "Olive"; ColorBox.Items.Add(cboxitemm);
            ComboBoxItem cboxitemn = new ComboBoxItem(); cboxitemn.Content = "Green"; ColorBox.Items.Add(cboxitemn);
            ComboBoxItem cboxitemo = new ComboBoxItem(); cboxitemo.Content = "Purple"; ColorBox.Items.Add(cboxitemo);
            //ComboBoxItem cboxitemp = new ComboBoxItem(); cboxitemp.Content = "Teal"; ColorBox.Items.Add(cboxitemp);
            //ComboBoxItem cboxitemq = new ComboBoxItem(); cboxitemq.Content = "Navy"; ColorBox.Items.Add(cboxitemq);
        }

        public ModelUIElement3D SallieArm(double sallieAngle, Point3D salliePosition, double armLength, double pincerDistance)
        {
            var element = new ModelUIElement3D();
            var geometry = new GeometryModel3D();
            var meshBuilder = new MeshBuilder();
            Point3D leftPincerPoint = new Point3D(pincerDistance / -2.0, armLength, 5.0);
            meshBuilder.AddBox(leftPincerPoint, 0.25, 1, 1);
            Point3D rightPincerPoint = new Point3D(pincerDistance / 2.0, armLength, 5.0);
            meshBuilder.AddBox(rightPincerPoint, 0.25, 1, 1);
            geometry.Geometry = meshBuilder.ToMesh();
            geometry.Material = new DiffuseMaterial(Utils.GetBrush("Silver"));
            element.Model = geometry;
            Transform3DGroup myTransformer = new();
            RotateTransform3D myRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1),
                                                             Convert.ToDouble(sallieAngle)), (Point3D)salliePosition);
            TranslateTransform3D myTranslate = new TranslateTransform3D(0, 0, 0);
            myTransformer.Children.Add(myRotate);
            myTransformer.Children.Add(myTranslate);
            element.Transform = myTransformer;
            return element;
        }

    }
}
