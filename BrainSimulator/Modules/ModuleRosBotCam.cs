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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;
using System.Threading;
using System.Windows.Media.Imaging;

namespace BrainSimulator.Modules
{
    public class ModuleRosBotCam : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;


        // Set size parameters as needed in the constructor
        // Set max to be -1 if unlimited
        public ModuleRosBotCam()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        // Fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            UpdateDialog();
        }
        [XmlIgnore]
        public BitmapImage bitmapHandle = new BitmapImage();
        bool drawEmptyImage = false;
        bool newImageAvailable = false;
        string botip = "";
        public void ClearBitmap()
        {
            bitmapHandle = new();
            drawEmptyImage = true;
        }

        void bitmapToImage(MemoryStream mem)
        {
            mem.Position = 0;
            bitmapHandle = new BitmapImage();
            bitmapHandle.BeginInit();            
            bitmapHandle.StreamSource = mem;
            bitmapHandle.CacheOption = BitmapCacheOption.OnLoad;
            bitmapHandle.EndInit();
            bitmapHandle.Freeze();
            newImageAvailable = true;
        }

        byte[] GetShrunkArray(byte[] inputBuffer, int i)
        {
            var handle2 = new byte[i+2];
            for (int j = 0; j<=i+1; j++)
            {
                handle2[j] = inputBuffer[j];
            }
            using (var mem = new MemoryStream(handle2))
            {
                Bitmap bitmap1 = (Bitmap)System.Drawing.Image.FromStream(mem);
                bitmapToImage(mem);
                //bitmap1.Save("testbit2.jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);                
            }
            return handle2;
        }
        byte GetByteFromStream(Stream inStream)
        {
            int x = inStream.ReadByte();
            if (x == -1)
            {
                return 0;
            }
            if (copying)
            {
                jpegHandle[indexSize++] = (byte)x;
            }
            return (byte)x;
        }
        byte[] jpegHandle = new byte[4096*4096*3];
        bool copying = false;
        int indexSize = 0;//position of index in the jpeg

        void GetSingleFrame(Stream response)
        {
            copying = false;
            byte byte1 = 0;
            byte byte2 = GetByteFromStream(response);
            while (!(byte1 == 0xff && byte2 == 0xd8))
            {
                byte1 = byte2;
                byte2 = GetByteFromStream(response);
            }
            jpegHandle[0] = 0xff;
            jpegHandle[1] = 0xd8;
            indexSize=2;
            copying = true;

            while (!(byte1 == 0xff && byte2 == 0xd9))
            {
                byte1 = byte2;
                byte2 = GetByteFromStream(response);
            }
            GetShrunkArray(jpegHandle, --indexSize);
            copying = false;
        }       
        async void GetStreamImage()
        {            
            byte exampleByte = new byte();
            byte highByte = 0x00;
            byte lowByte = 0x00;
            //Network.theHttpClient.Timeout = TimeSpan.FromSeconds(20);
            var response = await Network.theHttpClient.GetStreamAsync("http://"+botip+":6500/video_feed");            
            bool endFound = false;
            bool ff_Found = false;
            bool onHighLowBytes = false;
            bool highByteRead = false;
            int markerCount = 0;//length of marker in stream

            for (; ; )
            {
                GetSingleFrame(response);
                UpdateDialog();
            }
            return;

        }
        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        Task CameraStreamTask;
        CancellationTokenSource cts = new();
        public override void Initialize()
        {
            DialogLockSpan = new System.TimeSpan(0, 0, 0, 0, 10);
            ModuleRosBot rosbotHandle = (ModuleRosBot)FindModule(typeof(ModuleRosBot));
            if (rosbotHandle != null)
            {
                botip = rosbotHandle.botToUse.botIP.ToString();
            }
            CancellationToken cancelStream = cts.Token;
            if (CameraStreamTask == null)
            {
                CameraStreamTask = Task.Run(() => {
                    Thread.CurrentThread.Name = "RosBotCamera";
                    GetStreamImage();
                }, cts.Token);
            }
        }
        public override void Closing()
        {
            if (CameraStreamTask != null)
            {
                cts.Cancel();//may not close properly should really come back and change it
            }
            base.Closing();
        }
        // The following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        // Called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        // called whenever the UKS performed an Initialize()
        public override void UKSInitializedNotification()
        {

        }
    }
}