//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleAvoidanceScanner : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public double Distance = 0;
        [XmlIgnore]
        public double Direction = 0;
        [XmlIgnore]
        public Mat ImageToRecognize { get; set; }
        [XmlIgnore]
        public string FileToRecognize { get; set; }
        [XmlIgnore]
        public bool SaveDebugImages { get; set; }
        private long lastFrameAt { get; set; } = Utils.GetPreciseTime();    

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAvoidanceScanner()
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

            if (Utils.GetPreciseTime() - lastFrameAt > 5000000)
            {
                lastFrameAt = Utils.GetPreciseTime();
                CalculateAvoidanceValues();
                // Debug.WriteLine("AVOIDANCE TIMING: " + (Utils.GetPreciseTime() - lastFrameAt));
                // Debug.WriteLine(FileToRecognize);   
            }

            UpdateDialog();
        }

        public override void SetInputImage(Mat inputImage, string inputFilename)
        {
            ImageToRecognize = inputImage;
            FileToRecognize = inputFilename;
        }

        public void CalculateAvoidanceValues()
        {
            if (ImageToRecognize == null) return;
            ModuleIntegratedVision iVision = (ModuleIntegratedVision)base.FindModule("IntegratedVision");
            Mat matInput = iVision.MaskedOriginal;
            if (matInput == null) return;

            // this assumes an image that is a single plane black and white image, preferably a mask
            Mat imgOutput = ImgUtils.DetermineAvoidanceWarning(matInput, out Distance, out Direction);
            ImgUtils.SaveImageIf(Utils.GetOrAddDocumentsSubFolder("ImageProcessingOutput"), 
                                 "6.Avoidanceoutput.jpg", 
                                 imgOutput, 
                                 false);

            SetAvoidanceValuesInUKS();
            ImageToRecognize = null;
            FileToRecognize = "";
        }
        
        private void SetAvoidanceValuesInUKS()
        {
            GetUKS();
            Thing SenseRoot = UKS.GetOrAddThing("Sense", "Thing");
            if (SenseRoot == null) return;
            Thing VisualRoot = UKS.GetOrAddThing("Visual", SenseRoot);
            if (VisualRoot == null) return;
            if (Distance > 0.00001)
            {
                UKS.GetOrAddThing("AvoidanceDistance", VisualRoot, Distance * 100);
                UKS.GetOrAddThing("AvoidanceDirection", VisualRoot, Direction * 100);
            }
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
