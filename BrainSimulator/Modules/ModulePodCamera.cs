//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections;
using Emgu.CV;
using Emgu.CV.IntensityTransform;

namespace BrainSimulator.Modules
{
    public class ModulePodCamera : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePodCamera()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        bool newImageAvailable;
        bool drawEmptyImage;

        [XmlIgnore]
        public BitmapImage theBitmap;
        [XmlIgnore]
        public bool CameraOrientation { get; set; }

        DateTime lastSaveTime;
        DateTime lastConnectTime;
        [XmlIgnore]
        public bool saveImagesToDisk;
        [XmlIgnore]
        public bool saveOneImage;
        [XmlIgnore]
        public int failureCounter = 0;
        [XmlIgnore]
        public bool failureFlag = false;
        [XmlIgnore]
        public bool cameraSelected = false;
        [XmlIgnore]
        public double fullDelta;

        int debugMsgCount;

        Task UDPListenerTask;
        CancellationTokenSource tokenSource2 = new();
        private bool sendDevicePoll = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here
            ModulePodConnection mpc = (ModulePodConnection)FindModule("PodConnection");
            if (mpc == null || mpc.PodSelected?.CameraIP == null)
            {
                cameraSelected = false;
                    UpdateDialog();
                if (drawEmptyImage)
                {
                    drawEmptyImage = false;
                }
                return;
            }

            try
            {
                GetCameraImage(); //only issues request if not currently busy
                //if you want the dlg to update, use the following code whenever any parameter changes
                if (newImageAvailable)
                {
                    UpdateDialog();
                    newImageAvailable = false;                    
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("PodCameraModule encountered an exception: " + e.Message);                
            }            
        }

        public void ClearBitmap()
        {
            theBitmap = new();
            drawEmptyImage = true;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in mv.Neurons)
                n.Model = Neuron.modelType.Color;            

            initialized = true;
        }

        async void GetCameraImage()
        {
            ModulePodConnection mpc = (ModulePodConnection)FindModule("PodConnection");
            if (mpc == null) return;
            if (mpc.PodSelected.PodIP == null)
            {
                cameraSelected = false;
                UpdateDialog();
                return;
            }
            else
            {
                cameraSelected = true;
            }
            if (Network.httpClientBusy)
                return;

            try
            {
                Network.httpClientBusy = true;                
                var response = await Network.theHttpClient.GetAsync("http://" + mpc.PodSelected.CameraIP);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        string saveFolder = Utils.GetOrAddDocumentsSubFolder("CameraOutput");
                        var theStream = await response.Content.ReadAsByteArrayAsync();
                        using (var mem = new MemoryStream(theStream))
                        using (Bitmap bitmap1 = (Bitmap)System.Drawing.Image.FromStream(mem))
                        {
                            if (!CameraOrientation)
                                bitmap1.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            else
                                bitmap1.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            //LoadImage(bitmap1);
                            if (saveOneImage || saveImagesToDisk)// && DateTime.Now - lastSaveTime > TimeSpan.FromSeconds(5))
                            {
                                lastSaveTime = DateTime.Now;
                                Angle deltaTurn = new();
                                double deltaMove = 0;
                                Angle cameraPan = new();
                                Angle cameraTilt = new();

                                GetUKS();
                                if (UKS != null)
                                {
                                    Thing selfRoot = UKS.Labeled("Self");
                                    if (selfRoot != null)
                                    {
                                        Thing t = UKS.Labeled("CameraPan", selfRoot.Children);
                                        if (t != null && t.V != null)
                                        {
                                            cameraPan = (Angle)t.V;
                                            //t.V = new Angle(0);
                                        }
                                        t = UKS.Labeled("CameraTilt", selfRoot.Children);
                                        if (t != null && t.V != null)
                                        {
                                            cameraTilt = (Angle)t.V;
                                            //t.V = new Angle(0);
                                        }
                                        t = UKS.Labeled("PodDeltaTurn", selfRoot.Children);
                                        if (t != null && t.V != null)
                                        {
                                            deltaTurn = (Angle)t.V;
                                            t.V = new Angle(0);
                                        }
                                        t = UKS.Labeled("PodDeltaMove", selfRoot.Children);
                                        if (t != null && t.V != null)
                                        {
                                            deltaMove = (float)t.V;
                                            fullDelta += deltaMove;
                                            t.V = 0f;
                                        }
                                    }
                                }

                                DateTime now = DateTime.Now;

                                string filename = Utils.BuildAnnotatedImageFileName(saveFolder, deltaTurn, deltaMove, cameraPan, cameraTilt, "jpg   ");
                                int original_width = bitmap1.Width;
                                int original_height = bitmap1.Height;
                                Bitmap output = new Bitmap(bitmap1);
                                double ratio = (double)original_width / (double)original_height;
                                if (CameraOrientation && ratio < 0.77)
                                {
                                    // portait image, so crop to landscape and enlarge to original size
                                    int x = 0;
                                    int y = (int)(original_width * ratio / 2);
                                    int w = original_width;
                                    int h = (int)(original_width * ratio);
                                    Rectangle croparea = new Rectangle(x, y, w, h);
                                    Bitmap cropped = new Bitmap(croparea.Width,croparea.Height);
                                    using (Graphics g = Graphics.FromImage(cropped))
                                    {
                                        g.DrawImage(bitmap1, -croparea.X, -croparea.Y);
                                    };
                                    cropped = new Bitmap(cropped, new System.Drawing.Size(original_height, original_width));
                                    output = cropped;
                                }
                                if (saveOneImage)
                                {
                                    output.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                                }
                                else
                                {
                                    Mat unscaled = BitmapExtension.ToMat(output);
                                    Mat scaled = new Mat();
                                    System.Drawing.Size resize = ImgUtils.ProcessingSize;
                                    CvInvoke.Resize(unscaled, scaled, resize, 0, 0, Emgu.CV.CvEnum.Inter.Cubic);

                                    // Since the pod camera delivers low contrast images, we up the contrast to correct...
                                    IntensityTransformInvoke.ContrastStretching(scaled, unscaled, 45, 0, 185, 255);

                                    Sallie.VideoQueue.Enqueue(new CameraFeedEntry(filename, unscaled));
                                }

                                saveOneImage = false;
                            }

                            ModuleUserInterface moduleUserInterface = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
                            if(moduleUserInterface != null && moduleUserInterface.GetSaveImage())
                            {
                                bitmap1.Save(moduleUserInterface.ImageSaveLocation + "\\" + DateTime.Now.ToString(@"MM-dd-yyyy hh.mm.sstt") + ".jpg",
                                    System.Drawing.Imaging.ImageFormat.Jpeg);
                                moduleUserInterface.SetSaveImage(false);
                            }

                            //for the dialog box display
                            mem.Position = 0;
                            theBitmap = new BitmapImage();
                            theBitmap.BeginInit();
                            if (!CameraOrientation)
                                theBitmap.Rotation = Rotation.Rotate180;
                            else
                                theBitmap.Rotation = Rotation.Rotate90;
                            theBitmap.StreamSource = mem;
                            theBitmap.CacheOption = BitmapCacheOption.OnLoad;
                            theBitmap.EndInit();
                            theBitmap.Freeze();
                            newImageAvailable = true;
                        }
                        debugMsgCount = 0;
                        lastConnectTime = DateTime.Now;
                        failureCounter = 0;
                        if (failureFlag)
                        {
                            failureFlag = false;
                            UpdateDialog();
                        }
                    }
                    catch
                    { }
                }
                else
                {
                    Debug.WriteLine("ModulePodCamera:GetCameraImage status code error =  " + response.ReasonPhrase);
                    FailureCheckWrapper(mpc);
                }
            }
            catch (Exception e)
            {
                FailureCheckWrapper(mpc);
                if (debugMsgCount++ < 5)
                    Debug.WriteLine("ModulePodCamera:GetCameraImage encountered exception: " + e.Message);
                mv.GetNeuronAt(0, 0).SetValueInt(0xff0000);
                Network.theHttpClient.CancelPendingRequests();
            }
            Network.httpClientBusy = false;
        }

        void FailureCheckWrapper(ModulePodConnection mpc)
        {
            failureCounter++;
            if (failureCounter>=4 && !failureFlag)
            {
                if (mpc == null) return;
                mpc.SendUDP("Reset", mpc.CameraIP);
                Debug.WriteLine("CameraReset command out");
                failureFlag = true;
                UpdateDialog();
            }
        }


        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            Init();
            Initialize();
        }
        
        public override MenuItem CustomContextMenuItems()
        {
            StackPanel sp = new() { Orientation = Orientation.Vertical };
            CheckBox cb = new() { IsChecked = saveImagesToDisk, Content = "Save Images to Disk" };
            cb.Checked += Cb_Checked;
            cb.Unchecked += Cb_Checked;

            //Button saveOne = new() { Click = saveImagesToDisk, Content = "Save Images to Disk" };
            Button saveOne = new() { Content = "Save One Image", };
            saveOne.Click += Tb_Clicked;

            sp.Children.Add(cb);
            sp.Children.Add(saveOne);

            return new MenuItem { Header = sp, StaysOpenOnClick = true };
        }

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                saveImagesToDisk = (bool)cb.IsChecked;
            }
        }

        private void Tb_Clicked(object sender, RoutedEventArgs e)
        {
            saveOneImage = true;
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
            foreach (Neuron n in mv.Neurons)
            {
                n.Model = Neuron.modelType.Color;
            }
        }
        public void CameraSwapMode(string modeNum)
        {
            ModulePodConnection connectHandle = (ModulePodConnection)FindModule(typeof(ModulePodConnection));
            if (connectHandle == null || connectHandle.CameraIP == null) return;
            connectHandle.SendUDP("CamSwap:" + modeNum, connectHandle.CameraIP);
            failureFlag = false;
            UpdateDialog();
        }
        public void ForceTestCam()
        {
            ModulePodConnection connectHandle = (ModulePodConnection)FindModule(typeof(ModulePodConnection));
            if (connectHandle == null) return;            
            connectHandle.CameraIP =  IPAddress.Parse("192.168.0.59");
        }
    }
}
