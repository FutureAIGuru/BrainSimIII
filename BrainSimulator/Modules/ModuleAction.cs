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
    public class ModuleAction : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAction()
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

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            GetUKS();
            UKSInitializedNotification();
        }


        public Thing SetAction(Thing evnt, Thing Action)
        {
            //Thing TurnRight = UKS.GetOrAddThing("TurnRight", "Action");
            //Thing TurnLeft = UKS.GetOrAddThing("TurnLeft", "Action");
            //Thing Move = UKS.GetOrAddThing("Move", "Action");
            //if (rslt > 0)
            //{
                Action.AddRelationship(evnt);
                //TurnRight.AddReference(TurnRight);
                return Action;
          
            //return null;
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
            Thing ActionRoot = UKS.GetOrAddThing("Action", "Behavior");
            UKS.GetOrAddThing("Turn Right", ActionRoot);
            UKS.GetOrAddThing("Turn Left", ActionRoot);
            UKS.GetOrAddThing("Move", ActionRoot);

            MainWindow.ResumeEngine();
        }
    }
}