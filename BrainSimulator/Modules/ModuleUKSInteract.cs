//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleUKSInteract : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive

        public ModuleUKSInteract()
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

        public void AddReference(string source, string target, string relationshipType)
        {
            GetUKS();
            if (UKS == null) return;
            UKS.AddStatement(source, relationshipType, target);
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

        public List<string> RelationshipTypes()
        {
            GetUKS();
            if (UKS == null) return null;

            Thing relParent = UKS.GetOrAddThing("RelationshipType", "Thing");
            List<string> relTypes = new();

            foreach (Thing relationshipType in relParent.Children)
            {
                relTypes.Add(relationshipType.Label);
            }

            return relTypes;
        }
    }
}
