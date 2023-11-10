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

namespace BrainSimulator.Modules
{
    public class ModuleAttentionLook : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAttentionLook()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }
        ModulePodInterface thePod = null;

        public ModulePodInterface Pod()
        {
            if (thePod == null)
            {
                thePod = (ModulePodInterface)FindModule("PodInterface");
            }
            return thePod;
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            GetUKS();
            if (UKS == null) return;
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModel is null) return;
            ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theMentalModel == null) return;
            if (mentalModel.Children.Count == 0) return;
            Thing Attention = UKS.GetOrAddThing("Attention", "Thing");
            if (Attention is null || (Attention.Relationships.Count < 1)) return;

            Thing objectOfAttentioin = Attention.HasRelationshipWithParent(mentalModel);
            if (objectOfAttentioin == null) return;
            var test = objectOfAttentioin.GetRelationshipsAsDictionary();
            if (test.ContainsKey("cen"))
            {
                var p = test["cen"];
                if (p is Point3DPlus p1)
                {
                    if (theMentalModel.PhysObjectIsVisible(objectOfAttentioin))
                    {
                        Pod()?.CommandTilt(p1.Phi, false);
                        Pod()?.CommandPan(p1.Theta, false);
                    }
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

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
        }
    }
}
