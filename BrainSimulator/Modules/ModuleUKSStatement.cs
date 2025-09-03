//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using UKS;
using Pluralize.NET;

namespace BrainSimulator.Modules
{
    public class ModuleUKSStatement : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive

        public ModuleUKSStatement()
        {
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            // if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
        }

        // fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        // the following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
        }

        // called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {

        }

        // return true if thing was added
        public bool AddChildButton(string newThing, string parent)
        {
            GetUKS();
            if (theUKS == null) return true;

            theUKS.GetOrAddThing(newThing, parent);
            return true;
        }

        public Relationship AddRelationship(string source, string target, string relationshipType)
        {
            GetUKS();
            if (theUKS == null) return null;

            Thing tSource = theUKS.CreateThingFromDottedAttributes(source, false);
            Thing tRelType = theUKS.CreateThingFromDottedAttributes(relationshipType, true);
            Thing tTarget = theUKS.CreateThingFromDottedAttributes(target, false);

            if (target == "" && relationshipType == "is-a")
            {
                if (target == "" && source != "")
                    theUKS.AddThing(source, null);
                return null;
            }

            Relationship r = theUKS.AddStatement(tSource, tRelType, tTarget, true);
            return r;
        }

        public static List<Thing> ThingListFromString(string source)
        {
            List<Thing> retVal = new();
            IPluralize pluralizer = new Pluralizer();
            source = source.Trim();
            string[] tempStringArray = source.Split(' ');
            //first, build a list of all the Things in the list
            for (int i = 0; i < tempStringArray.Length; i++)
            {
                if (tempStringArray[i] == "") continue;
                Thing t = ThingLabels.GetThing(pluralizer.Singularize(tempStringArray[i]));
                if (t == null) return retVal;
                retVal.Add(t);
            }
            ////is this a sequence?
            //List<Thing> tSequence = MainWindow.theUKS.HasSequence(retVal);
            //if (tSequence != null && tSequence.Count > 0)
            //{
            //    retVal = tSequence;
            //}
            //else if (retVal.Count > 1) //do things represent a list of attributes
            //{
            //    //retVal = MainWindow.theUKS.FindThingsWithAttributes(retVal);
            //}

            return retVal;
        }

    }
}
