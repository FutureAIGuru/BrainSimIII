//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using Pluralize.NET;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleUKSClause: ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive

        public ModuleUKSClause()
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
        public Thing GetClauseType(string newThing)
        {
            GetUKS();
            if (theUKS == null) return null;

            return theUKS.GetOrAddThing(newThing, "ClauseType");
        }

        public Relationship AddRelationship(string source, string target, string relationshipType)
        {
            GetUKS();
            if (theUKS == null) return null;
            IPluralize pluralizer = new Pluralizer();


            source = source.Trim();
            target = target.Trim();
            relationshipType = relationshipType.Trim();

            string[] tempStringArray = source.Split(' ');
            List<string> sourceModifiers = new();
            source = pluralizer.Singularize(tempStringArray[tempStringArray.Length-1]);
            for (int i = 0; i < tempStringArray.Length-1; i++) sourceModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            tempStringArray = target.Split(' ');
            List<string> targetModifiers = new();
            target = pluralizer.Singularize(tempStringArray[tempStringArray.Length - 1]);
            for (int i = 0; i < tempStringArray.Length - 1; i++) targetModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            tempStringArray = relationshipType.Split(' ');
            List<string> typeModifiers = new();
            relationshipType= pluralizer.Singularize(tempStringArray[0]);
            for (int i = 1; i < tempStringArray.Length; i++) typeModifiers.Add(pluralizer.Singularize(tempStringArray[i]));

            Relationship r = theUKS.AddStatement(source, relationshipType, target,sourceModifiers, typeModifiers, targetModifiers, false);
            
            return r;
        }

        public Thing GetUKSThing(string thing, string parent)
        {
            GetUKS();
            if (theUKS == null) return null;

            return theUKS.GetOrAddThing(thing, parent);
        }

        public Thing SearchLabelUKS(string label)
        {
            GetUKS();
            if (theUKS == null) return null;

            return theUKS.Labeled(label);
        }

        public List<string> RelationshipTypes()
        {
            GetUKS();
            if (theUKS == null) return null;

            Thing relParent = theUKS.GetOrAddThing("RelationshipType", "Thing");
            List<string> relTypes = new();

            foreach (Thing relationshipType in relParent.Children)
            {
                relTypes.Add(relationshipType.Label);
            }

            return relTypes;
        }
    }
}
