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

namespace BrainSimulator.Modules
{
    public class ModuleAssociation : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        private int hitsThreshold = 7; // number of hits before we try solidifying associations.


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAssociation()
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
            ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
            if (naSource == null) return;

            UKS = (ModuleUKS)naSource.TheModule;

            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            Thing wordParent = UKS.GetOrAddThing("Word", "Audible");
            Thing attn = UKS.GetOrAddThing("Attention", "Thing");
            if (attn == null) return;
            Thing attnVisualTarget = attn.GetRelationshipWithAncestor(mentalModel);
            Thing associate = UKS.GetOrAddThing("Associate", "Attention");
            Thing attnAudibleTarget = null;
            if (associate.Relationships.Count == 0) return;

            List<Thing> associationWords = new();
            while ( associate.Relationships.Count > 0)
            {
                associationWords.Add((associate.Relationships[0].T as Thing));
                associate.RelationshipsWriteable.RemoveAt(0);
            }

            AssociateWordsWithVisuals(associationWords);
            AssociateWordsWithSituation(attnAudibleTarget);

            // disabling UpdateDialog() and adding a refresh button
            //if you want the dlg to update, use the following code whenever any parameter changes
            //if (dlg != null && !((ModuleAssociationDlg)dlg).busy)
            //    UpdateDialog();
        }


        private void AssociateWordsWithSituation(Thing attnAudibleTarget)
        {
            if (attnAudibleTarget == null) return;
            Thing situationRoot = UKS.GetOrAddThing("Situation", "Behavior");
            IList<Thing> recentlyFiredSituations = situationRoot.DescendentsWhichFired();
            foreach ( Thing situation in recentlyFiredSituations)
            {
                attnAudibleTarget.AdjustRelationship(situation);
            }
        }

        
        public void AssociateWordsWithVisuals(List<Thing> recentlyFiredWords)
        {
            if (recentlyFiredWords == null || recentlyFiredWords.Count == 0) return;

            //IList<Thing> recentlyFiredWords = uks.Labeled("Word").DescendentsWhichFired;
            //IList<Thing> recentlyFiredRelationships = uks.Labeled("Relationship").DescendentsWhichFired;
            Thing propertyRoot = UKS.GetOrAddThing("Property","Thing");
            Thing colRoot = UKS.GetOrAddThing("col", "Property");
            Thing shpRoot = UKS.GetOrAddThing("shp", "Property");
            IList<Thing> recentlyFiredVisuals = colRoot.DescendentsWhichFired(2);
            recentlyFiredVisuals = recentlyFiredVisuals.Concat(shpRoot.DescendentsWhichFired(2)).ToList();


            //set the hits
            foreach (Thing word in recentlyFiredWords)
            {
                List<Relationship> references = word.GetRelationshipsWithAncestor(UKS.GetOrAddThing("col", "Property"));
                references.AddRange(word.GetRelationshipsWithAncestor(UKS.GetOrAddThing("shp", "Property")));
                references = references.OrderByDescending(l => l.hits).ToList();

                // Check if word is "known"
                if ( references.Count == 1 && references[0].hits > hitsThreshold) continue;

                foreach (Thing visual in recentlyFiredVisuals)
                {
                    if (!visual.HasAncestor(UKS.GetOrAddThing("MentalModel", "Thing")))
                    {

                        List<Relationship> referencedByColShp = visual.RelationshipsFrom
                            .FindAll(l => (l.T as Thing).HasAncestor(UKS.GetOrAddThing("Word", "Audible")));
                        // Check if the property is already linked to word.
                        if (referencedByColShp.Count == 1 && referencedByColShp[0].hits > hitsThreshold) continue;
                        word.AdjustRelationship(visual);
                    }
                       
                }

                if (references.Count <= 1) continue;
                
                
                if ( references[0].hits > hitsThreshold && references[0].hits > references[1].hits )
                {

                    Thing propertyMatch = (references[0].T as Thing);
                    foreach (Relationship l in references)
                        if (!(l.T == propertyMatch))
                            word.RemoveRelationship((l.T as Thing));
                    List<Thing> wordsToDeReference = new();
                    foreach (Relationship l in propertyMatch.RelationshipsFrom)
                        if ((l.source as Thing).HasAncestor(UKS.GetOrAddThing("Word", "Audible")))
                            if (l.source != word)
                                wordsToDeReference.Add((l.T as Thing));
                    foreach (Thing t in wordsToDeReference)
                        t.RemoveRelationship(propertyMatch);
                }
                
            }

            //set the misses for properties
            foreach (Thing visual in recentlyFiredVisuals)
            {
                foreach (Relationship l in visual.RelationshipsFrom)
                {
                    Thing word = (l.source as Thing);
                    if (word.HasAncestor(UKS.Labeled("Word")))
                    {
                        if (!recentlyFiredWords.Contains(word))
                        {
                            word.AdjustRelationship(visual, -1);
                        }
                    }
                }
            }
        }

        public Thing GetBestAssociation(Thing t)
        {
            if (UKS == null)
            {
                ModuleView naSource = theNeuronArray.FindModuleByLabel("UKS");
                if (naSource == null) return null;
                UKS = (ModuleUKS)naSource.TheModule;
            }
            IList<Thing> words = UKS.Labeled("Word").Children.Reverse().ToList();
            List<Thing> properties = UKS.Labeled("Color").Children.ToList();
            properties.AddRange(UKS.Labeled("Area").Children.ToList());
            properties = properties.OrderBy(x => x.Label).ToList();
            IList<Thing> relationships = UKS.Labeled("Relationship").Children;
            relationships = relationships.OrderBy(x => x.Label.Substring(1)).ToList();

            float[,] values = GetAssociations();
            int row = -1;
            row = properties.IndexOf(t);
            if (row == -1)
            {
                row = relationships.IndexOf(t);
                if (row != -1) row += properties.Count;
            }
            if (row == -1) return null;

            float max = 0;
            int bestCol = -1;
            Thing best = null;
            for (int i = 0; i < values.GetLength(1); i++)
            {
                if (values[row, i] > max)
                {
                    max = values[row, i];
                    best = words[i];
                    bestCol = i;
                }
            }
            if (bestCol != -1)
            {
                for (int i = 0; i < values.GetLength(0); i++)
                {
                    if (values[i, bestCol] > max)
                    {
                        return null;
                    }
                }
            }
            return best;
        }

        public float[,] GetAssociations()
        {
            IList<Thing> words = UKS.Labeled("Word").Children.Reverse().ToList();
            List<Thing> properties = UKS.Labeled("col").Children.ToList();
            properties.AddRange(UKS.Labeled("shp").Children.ToList());
            properties = properties.OrderBy(x => x.Label).ToList();
            IList<Thing> relationships = UKS.Labeled("Relationship").Children;
            relationships = relationships.OrderBy(x => x.Label.Substring(1)).ToList();


            //collect all the values in a single spot
            float[,] values = new float[properties.Count + relationships.Count, words.Count];

            int row = 0;
            foreach (Thing property in properties)
            {
                for (int i = 0; i < words.Count; i++)
                {
                    Relationship l = words[i].HasRelationship(property);
                    if (l != null)
                    {
                        values[row, i] = l.Value1;
                    }
                }
                row++;
            }
            foreach (Thing relationship in relationships)
            {
                for (int i = 0; i < words.Count; i++)
                {
                    Relationship l = words[i].HasRelationship(relationship);
                    if (l != null)
                    {
                        values[row, i] = l.Value1;
                    }
                }
                row++;
            }
            return values;
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
