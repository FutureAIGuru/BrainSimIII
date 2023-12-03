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
    public class ModuleUKSInteract : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleUKSInteract()
        {
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
            //test
            //ModuleUKS testModule = new();
            //Debug.WriteLine(testModule.AllLabeled("Object"));

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
            
        }

        //return true if thing was added
        public bool AddChildButton(string newThing, string parent)
        {
            GetUKS();
            if (UKS == null) return true;

            UKS.GetOrAddThing(newThing, parent);
            return true;
        }

        public void AddReference(string source, string parent, string target, string relationshipType, string targetParent, string[] modifiersString)
        {
            GetUKS();
            if (UKS == null) return;

            Thing firstThing = UKS.GetOrAddThing(source, parent);
            Thing referenceThing = UKS.GetOrAddThing(target, targetParent);
            List<Thing> modifiers = new();

            foreach (string s in modifiersString)
            {
                if (s != "")
                {
                    if (Int32.TryParse(s, out int count))
                    {
                        Thing relType = UKS.GetOrAddThing(relationshipType, "Relationship");
                        firstThing.AddRelationship(referenceThing, relType, count);
                        return;
                    }
                    Console.WriteLine($"Unable to parse '{s}'");
                    Thing t = UKS.Labeled(s);
                    if (t == null) t = UKS.GetOrAddThing(s, "Object");
                    modifiers.Add(t);
                }
            }
            if (relationshipType == "None")
                firstThing.AddRelationship(referenceThing);
            else
            {
                Thing relType = UKS.GetOrAddThing(relationshipType, "Relationship");
                firstThing.AddRelationship(referenceThing, relType, modifiers);
            }
        }

        public Thing GetUKSThing(string thing, string parent)
        {
            GetUKS();
            if (UKS == null) return null;

            return UKS.GetOrAddThing(thing, parent);
        }

        public Thing SearchLabelUKS(string label)
        {
            GetUKS();
            if (UKS == null) return null;

            return UKS.Labeled(label);
        }

        //returns true if thing was deleted
        public bool DeleteThing(string thing, string parent)
        {
            GetUKS();
            if (UKS == null) return true;

            Thing T = UKS.GetOrAddThing(thing, parent);
            if (T.Children.Count != 0)
                return false;

            UKS.DeleteThing(T);
            return true;
        }

        public List<Thing> RelationshipTypes()
        {
            GetUKS();
            if (UKS == null) return null;

            Thing relParent = UKS.GetOrAddThing("Relationship", "Thing");
            List<Thing> relTypes = new();

            foreach (Thing relationshipType in relParent.Children)
            {
                relTypes.Add(relationshipType);
            }

            return relTypes;
        }

        public List<Thing> ParentsOfLabel(string label)
        {
            GetUKS();
            if (UKS == null) return null;

            List<Thing> allLabels = UKS.AllLabeled(label);
            List<Thing> allParents = new();

            foreach (var labelThing in allLabels)
            {
                foreach (var parentThing in labelThing.Parents)
                {
                    allParents.Add(parentThing);
                }
            }

            return allParents;
        }

        public bool isRoot(string label)
        {
            GetUKS();
            if (UKS == null) return false;

            foreach (var labelThing in UKS.AllLabeled(label))
            {
                if (labelThing.Parents.Count != 0)
                    return false;
            }

            return true;
        }
    }
}
