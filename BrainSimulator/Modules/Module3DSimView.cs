//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class Module3DSimView : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public bool triggerSave = false;
        [XmlIgnore]
        public Bitmap sourceImage = null;
        [XmlIgnore]
        public Bitmap processedImage = null;

        private DateTime lastSave = DateTime.MinValue;
        [XmlIgnore]
        public int FPSDelay = 100;
        public bool ProduceOutput = false;
        public bool SaveImagesWithMovement = false;
        [XmlIgnore]
        public string Filename = "";

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public Module3DSimView()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        // stuff on Sallie's whereabouts... Gets pushed from Module3DSimControl.
        [XmlIgnore]
        public bool recreateWorld = false;

        public void updateViewCamera()
        {
            UpdateDialog();
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            ModulePodCamera podCam = (ModulePodCamera)FindModule("PodCamera");
            if (ProduceOutput &&            // does 3DSimView want output?
                triggerSave == false &&     // has save image been triggered? 
                DateTime.Now - lastSave > TimeSpan.FromMilliseconds(FPSDelay))   // has frame time expired?
            {
                //Filename = BuildFileName();
                lastSave = DateTime.Now;
                triggerSave = true;
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }
        public string BuildFileName()
        {
            string saveFolder = Utils.GetOrAddDocumentsSubFolder("VirtualCameraOutput");
            Angle deltaTurn = new Angle();
            double deltaMove = 0;
            Angle cameraPan = new Angle();
            Angle cameraTilt = new Angle();

            ModuleUKS UKS = (ModuleUKS)FindModule("UKS");
            if (UKS != null)
            {
                Thing selfRoot = UKS.Labeled("Self");
                if (selfRoot != null)
                {
                    Thing t = UKS.Labeled("CameraPan", selfRoot.Children);
                    if (t != null && t.V != null)
                    {
                        cameraPan = (Angle)t.V;
                    }
                    t = UKS.Labeled("CameraTilt", selfRoot.Children);
                    if (t != null && t.V != null)
                    {
                        cameraTilt = (Angle)t.V;
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
                        t.V = 0f;
                    }
                }
            }

            return  Utils.BuildAnnotatedImageFileName(saveFolder, deltaTurn, deltaMove, cameraPan, cameraTilt, "jpg");
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