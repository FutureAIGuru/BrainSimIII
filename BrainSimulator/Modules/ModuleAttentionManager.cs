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
    public class ModuleAttentionManager : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        ModuleWakeUp wakeUp;
        ModuleExplore explore;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAttentionManager()
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
            return;
            Init();  //be sure to leave this here

            GetUKS();


            int j = attentionChildren.Count;
            for (int i = 0; i < j; i++)
            {
                //do only one movement type action at a time
                //can add other types of actions that occur simultaneously
                if(attention.Children[i].Label == "TurnAround")
                {
                    if (wakeUp == null) break;
                    wakeUp.TurnOnce();
                    break;
                }
                if(attention.Children[i].Label == "Explore")
                {
                    if (explore == null) break;
                    explore.Explore(attention.Children[i]);
                    break;
                }
                if(attention.Children[i].Label == "GoTo")
                {
                    if (explore == null) break;
                    explore.GoTo(attention.Children[i]);
                    break;
                }

                j = attentionChildren.Count;
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            wakeUp = (ModuleWakeUp)FindModule(typeof(ModuleWakeUp));
            explore = (ModuleExplore)FindModule(typeof(ModuleExplore));
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
