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
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{

    public partial class Module3DSimViewDlg : ModuleBaseDlg
    {
        bool threeDInitialized;

        public Module3DSimViewDlg()
        {
            InitializeComponent();
            threeDInitialized = false;
        }

        bool updateView;
        bool gridChecked = true;

        public ModulePodInterface thePod;

        public ModulePodInterface Pod()
        {
            if (thePod == null)
            {
                Module3DSimView theModule = (Module3DSimView)base.ParentModule;
                if (theModule == null) return null;
                thePod = (ModulePodInterface)theModule.FindModule("PodInterface");
            }
            return thePod;
        }

        public void AddEnvironmentObjects()
        {
            Module3DSimView theModule = (Module3DSimView)base.ParentModule;
            if (!threeDInitialized || theModule.recreateWorld || updateView)
            {
                updateView = false;
                theModule.recreateWorld = false;
                ourObjects.Children.Clear();
                Constructor3D construct = new();

                if (gridChecked)
                {
                    // Floor temporarily deactived for vision experiment testing... 
                    // ourObjects.Children.Add(construct.Floor());
                    // ourObjects.Children.Add(construct.Grid());
                }

                if (theModule == null) return;
                Module3DSimModel theEnvironment = (Module3DSimModel)theModule.FindModule("3DSimModel");
                if (theEnvironment == null) return;

                foreach (EnvironmentObject shape in theEnvironment.ourThings)
                {
                    if (shape == null) continue;
                    if (shape.shape == "Cube")
                        ourObjects.Children.Add(construct.Cube(shape as Cube));
                    else if (shape.shape == "Sphere")
                        ourObjects.Children.Add(construct.Sphere(shape as Sphere));
                    else if (shape.shape == "Cylinder")
                        ourObjects.Children.Add(construct.Cylinder(shape as Cylinder));
                    else if (shape.shape == "Cone")
                        ourObjects.Children.Add(construct.Cone(shape as Cone));
                    else if (shape.shape == "Wall")
                        ourObjects.Children.Add(construct.Wall(shape as Wall));
                    else if (shape.shape == "Triangle2D")
                        ourObjects.Children.Add(construct.Triangle2D(shape as Triangle2D));
                }
            }

            threeDInitialized = true;
        }

        DateTime frameTime = DateTime.Now;
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null) return false;
            ProduceOutputCheckBox.IsChecked = parent.ProduceOutput;

            //if (DateTime.Now < frameTime + TimeSpan.FromMilliseconds(750)) return false;
            frameTime = DateTime.Now;
            AddEnvironmentObjects();

            if (Pod() != null)
            {
                Point3DPlus cameraPos = Pod().HeadPosition.Clone();
                cameraPos.Z = 4f;
                HelixPort.SetView(cameraPos, Pod().HeadDirection, Pod().BodyUpDirection);
            }

            if (parent.ProduceOutput && parent.IsEnabled() && parent.triggerSave)
            {
                // if needed, save the source image to the parent module...
                parent.sourceImage = null;
                PrepareSourceBitmap();
                parent.triggerSave = false;
            }
            return true;
        }

        public void PrepareSourceBitmap()
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent.ProduceOutput && parent.IsEnabled() && parent.triggerSave)
            {
                try
                {
                    string filename = parent.BuildFileName();
                    SaveImageFromViewPortInQueue(filename);
                    if (parent.SaveImagesWithMovement && Utils.ImageHasMovement(filename))
                    {
                        SaveImageFromViewPortInFile(filename);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void SaveImageFromViewPortInFile(string filename)
        {
            //return;
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null || filename == null || filename.Length == 0 || !parent.ProduceOutput) return;
            try
            {
                // for now use 512x384, should be OK for debugging outputs
                RenderTargetBitmap bmp = new RenderTargetBitmap(512, 384, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(HelixPort.Viewport);
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using Stream stm = File.Create(filename);
                encoder.Save(stm);
            }
            catch (System.AccessViolationException ex)
            {
                Debug.WriteLine("Crash in Saving JpgImage for virtual environment: " + ex.Message);
                try
                {
                    File.Delete(filename); // delete if possible...
                }
                catch (Exception)
                {
                }
            }
        }

        public void SaveImageFromViewPortInQueue(string filename)
        {
            //return;
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null || filename == null || filename.Length == 0 || !parent.ProduceOutput) return;
            try
            {
                BitmapSource bmpsource = Viewport3DHelper.RenderBitmap(HelixPort.Viewport, Background);
                Bitmap bmp = ImgUtils.BitmapSource2Bitmap(bmpsource);
                Mat mat = ImgUtils.Bitmap2Mat(bmp).Clone();
                System.Drawing.Size resize = ImgUtils.ProcessingSize;
                CvInvoke.Resize(mat, mat, resize, 0, 0, Emgu.CV.CvEnum.Inter.Cubic);
                Sallie.VideoQueue.Enqueue(new CameraFeedEntry(filename, mat));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Crash in Saving JpgImage for virtual environment: " + ex.Message);
            }
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            parent.triggerSave = true;
            Draw(false);
        }

        private void ProduceOutputCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null) return;
            parent.ProduceOutput = true;
            Sallie.VideoQueue.Clear(); 
        }

        private void ProduceOutputCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null) return;
            parent.ProduceOutput = false;
            Sallie.VideoQueue.Clear();
        }

        private void SaveImagesWithMovementCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null) return;
            parent.SaveImagesWithMovement = true;
        }
        private void SaveImagesWithMovementCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Module3DSimView parent = (Module3DSimView)base.ParentModule;
            if (parent == null) return;
            parent.SaveImagesWithMovement = false;
        }
    }
}