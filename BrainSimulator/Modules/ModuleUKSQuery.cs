//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using Pluralize.NET;
using static BrainSimulator.Modules.ModuleUKS;

namespace BrainSimulator.Modules
{
    public class ModuleUKSQuery : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive

        public ModuleUKSQuery()
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
            if (UKS == null) return true;

            UKS.GetOrAddThing(newThing, parent);
            return true;
        }

        public List<ThingWithQueryParams> QueryUKS(string source)
        {
            GetUKS();
            if (UKS == null) return null;
            IPluralize pluralizer = new Pluralizer();


            source = source.Trim();
            string[] tempStringArray = source.Split(' ');
            List<Thing> sourceModifiers = new();
            source = pluralizer.Singularize(tempStringArray[tempStringArray.Length - 1]);
            Thing tSource = ThingLabels.GetThing(source);
            if (tSource == null) return null;

            for (int i = 0; i < tempStringArray.Length - 1; i++)
            {
                Thing t = ThingLabels.GetThing(pluralizer.Singularize(tempStringArray[i]));
                if (t == null) return null;
                sourceModifiers.Add(t);
            }

            Thing t1 = UKS.SubclassExists(ThingLabels.GetThing(source), sourceModifiers);
            if (t1 == null)
            {
                sourceModifiers.Add(tSource);
                var x = UKS.FindThingsWithAttributes(sourceModifiers);
            }

            List<ThingWithQueryParams> retVal = UKS.Query(source);

            return retVal;
        }
        public IList<Thing> QueryAncestors(string source)
        {
            GetUKS();
            if (UKS == null) return null;
            IPluralize pluralizer = new Pluralizer();

            source = source.Trim();
            source = pluralizer.Singularize(source);

            Thing t = ThingLabels.GetThing(source);

            List<Thing> retVal = new();
            if (t != null)
                retVal = t.AncestorList().ToList();

            return retVal;
        }
        public IList<Thing> QuerySequence(string source)
        {
            GetUKS();
            if (UKS == null) return null;
            IPluralize pluralizer = new Pluralizer();
            List<Thing> retVal = new();

            source = source.Trim();
            string[] tempStringArray = source.Split(' ');
            List<Thing> sourceModifiers = new();
            for (int i = 0; i < tempStringArray.Length; i++)
            {
                if (tempStringArray[i] == "") continue;
                Thing t = ThingLabels.GetThing(pluralizer.Singularize(tempStringArray[i]));
                if (t == null) return null;
                sourceModifiers.Add(t);
            }

            retVal = UKS.HasSequence(sourceModifiers);

            return retVal;
        }
        public List<Relationship> QueryRelationships(List<ThingWithQueryParams> thingsToExamine, Relationship.Part p)
        {
            GetUKS();
            if (UKS == null) return null;
            if (thingsToExamine == null) return null;

            List<Relationship> retVal = UKS.GetAllRelationships(thingsToExamine, p);

            return retVal;
        }
        public IList<Thing> QueryChildren(string label)
        {
            GetUKS();
            if (UKS == null) return null;
            if (label == "") return null;
            Thing t = ThingLabels.GetThing(label);
            if (t == null) return null;

            IList<Thing> retVal = t.Children;

            return retVal;
        }
    }
}
