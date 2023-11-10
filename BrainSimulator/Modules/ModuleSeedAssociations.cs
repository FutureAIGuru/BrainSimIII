//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleSeedAssociations : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleSeedAssociations()
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

            GetUKS();
            if (UKS == null) return;

            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            IList<Thing> recentlyFiredVisuals = mentalModelRoot.DescendentsWhichFired(1);
            if (recentlyFiredVisuals.Count <= 0) return; 
            Thing colorRoot = UKS.GetOrAddThing("col", "Property");
            Thing shapeRoot = UKS.GetOrAddThing("shp", "Property");

            foreach (Thing fired in recentlyFiredVisuals)
            {
                if (fired == null) return;
                
                Thing color = fired.GetRelationshipWithAncestor(colorRoot);
                Thing shape = fired.GetRelationshipWithAncestor(shapeRoot);

                Thing colorWord = UKS.GetOrAddThing("w" + color.V, "Word");
                Thing shapeWord = UKS.GetOrAddThing("w" + shape.V, "Word");

                ModuleAssociation modAssociation = (ModuleAssociation)FindModule(typeof(ModuleAssociation));

                List<Thing> words = new();
                words.Add(colorWord);
                words.Add(shapeWord);
                modAssociation.AssociateWordsWithVisuals(words);
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