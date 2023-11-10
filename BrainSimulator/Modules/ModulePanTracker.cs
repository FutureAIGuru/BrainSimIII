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
using System.Xml.Serialization;
using static System.Math;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModulePanTracker : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;


        // Set size parameters as needed in the constructor
        // Set max to be -1 if unlimited
        public ModulePanTracker()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        // Fill this method in with code which will execute
        // once for each cycle of the engine

        object thingPositionRef = null;
        DateTime lastMove = DateTime.Now;
        TimeSpan offset = TimeSpan.FromMilliseconds(400);
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();
            if (UKS == null) return;
            List<Thing> xdref = UKS.GetTheUKS();
            if (theLivePod == null) return;
            if (theInterface == null) return;
            Point3DPlus t = null;
            try 
            {
                t = (Point3DPlus)xdref[15].Children[0].Children[0].V;                
            }
            catch
            {
                return;
            }
            
            if (t != null && ((t.Theta.Degrees >= (0+1)) || (t.Theta.Degrees <= (0-1))))
            {
                thingPositionRef = t;
                //Debug.WriteLine("Angle to pan" + -(t.Theta.Degrees));
                if ((lastMove + (offset)) < (DateTime.Now))
                {
                    lastMove = DateTime.Now;
                    Angle turnAmt = t.Theta;
                    int panBind = 1;
                    if (t.Theta.Degrees > (0+panBind)) turnAmt.Degrees = panBind;
                    if (t.Theta.Degrees < (0-panBind)) turnAmt.Degrees = -panBind;
                    //turnAmt.Degrees = 90.0f + turnAmt.Degrees;
                    theLivePod.pan(t.Theta.Degrees);
                }
            }

            UpdateDialog();
        }


        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        private ModulePod theLivePod = null;

        public ModulePod LivePod()
        {
            if (theLivePod == null)
            {
                ModulePod parent = (ModulePod)base.FindModule("Pod");
                theLivePod = (ModulePod)parent.FindModule("Pod", true);
            }
            return theLivePod;
        }

        private ModuleUKS theUKS = null;

        public ModuleUKS UKSRef()
        {
            if (theUKS == null)
            {
                ModuleUKS parent = (ModuleUKS)base.FindModule("UKS");
                theUKS = (ModuleUKS)parent.FindModule("UKS", true);
            }
            return theUKS;
        }

        private ModulePodInterface theInterface = null;

        public ModulePodInterface InterfaceRef()
        {
            if (theInterface == null)
            {
                ModulePodInterface parent = (ModulePodInterface)base.FindModule("PodInterface");
                theInterface = (ModulePodInterface)parent.FindModule("PodInterface", true);
            }
            return theInterface;
        }

        public override void Initialize()
        {
            theLivePod = LivePod();
            theUKS = UKSRef();
            theInterface = InterfaceRef();
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