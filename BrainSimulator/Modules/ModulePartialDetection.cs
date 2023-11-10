//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModulePartialDetection : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePartialDetection()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        Angle PrevTheta = new Angle();
        Angle PrevPhi = new Angle();
        List<Thing> PartialList = new List<Thing>();
        double prevGeneration = new();
        int update = 0;//update is to stop commands from comming to frequently, as each move must be undone afterwards 

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            Thing Generation = UKS.GetOrAddThing("Generation Number", "Attention");
            //add busy check
            if (podInterface.IsPodBusy()) { return; }
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (update == 1)
            {
                //move camera to original position
                podInterface.CommandTilt(0);
                podInterface.CommandPan(0);
                update = 0;
                return;
            }
            while (update == 0 && PartialList.Count > 0)
            {
                //move camera to all known partials
                var shape = PartialList[0].GetRelationshipsAsDictionary();
                var c = shape["cen"];
                var partial = shape["shp"];
                if (c is Point3DPlus point)
                {
                    if (point.Theta >= Abs(Angle.FromDegrees(60)) && point.Phi >= Abs(Angle.FromDegrees(90)))
                    {
                        PartialList.Remove(PartialList[0]);
                        return;
                    }
                    if (partial.ToString() == "Partial")
                    {
                        PrevTheta = -point.Theta;
                        PrevPhi = -point.Phi;
                        podInterface.CommandTilt(point.Phi);
                        podInterface.CommandPan(point.Theta);
                        update = 1;
                        PartialList.Remove(PartialList[0]);
                        return;
                    }
                    else PartialList.Remove(PartialList[0]);
                }
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
