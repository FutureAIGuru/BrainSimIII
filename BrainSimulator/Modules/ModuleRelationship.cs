//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;

namespace BrainSimulator.Modules
{
    public class ModuleRelationship : ModuleBase
    {
        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRelationship()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //ModuleUKS UKS = null;
        Thing prevAttnTarget = null;
        Thing attnVisualTarget = null;


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            bool visualTargetChanged = false;
            Init();  //be sure to leave this here
            GetUKS();
            if (UKS == null) return;

            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModel is null) return;
            if (mentalModel.Children.Count == 0) return;

            //DeleteUnusedRelationships();

            Thing attn = UKS.GetOrAddThing("Attention", "Thing");
            if (attn.Relationships.Count == 0) return;


            if (attn is null || attn.Relationships.Count < 1) return;

            Thing objectOfAttentioin = (attn.Relationships[0].T as Thing);
            if (objectOfAttentioin != attnVisualTarget)
            {
                visualTargetChanged = true;
            }
            if (visualTargetChanged)
            {
                prevAttnTarget = attnVisualTarget;
                attnVisualTarget = objectOfAttentioin;
            }

            if (prevAttnTarget != null && attnVisualTarget != null)
            {
                if (visualTargetChanged && mentalModel.Children.Count > 1)
                {
                    //add all the relationships to the UKS
                    AddRelationshipsToUKS(prevAttnTarget, attnVisualTarget);
                }
            }

            UpdateDialog();
            return;
        }

        private void DeletePreviousRelationships(Thing t1, Thing t2)
        {
            t1.RemoveRelationship(t2);
            t2.RemoveRelationship(t1);
        }
        private void AddRelationshipsToUKS(Thing t1, Thing t2)
        {
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModel is null) return;
            if (mentalModel.Children.Count == 0) return;
            if (t1 == null || t2 == null) return;
            //DeletePreviousRelationships(t1, t2);
            var dict1 = t1.GetRelationshipsAsDictionary();
            var dict2 = t2.GetRelationshipsAsDictionary();
            foreach (KeyValuePair<string, object> pair1 in dict1)
            {
                foreach (KeyValuePair<string, object> pair2 in dict2)
                {
                    if (pair1.Key == pair2.Key)
                    {
                        if (pair1.Value is Double)
                        {
                            double p1 = Convert.ToDouble(pair1.Value.ToString());
                            double p2 = Convert.ToDouble(pair2.Value.ToString());
                            if (p1 == p2)
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("==" + pair2.Key, "Relationship"));
                            }
                            else if (p1 > p2)
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing(">" + pair2.Key, "Relationship"));
                            }
                            else
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("<" + pair2.Key, "Relationship"));
                            }
                        }
                        else if (pair1.Value is Point3DPlus)
                        {
                            Point3DPlus p1 = pair1.Value as Point3DPlus;
                            Point3DPlus p2 = pair2.Value as Point3DPlus;
                            if (p1.Theta > p2.Theta)
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing(">" + pair2.Key, "Relationship"));
                            }
                            else
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("<" + pair2.Key, "Relationship"));
                            }
                            if (p1.Phi > p2.Phi)
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("^" + pair2.Key, "Relationship"));
                            }
                            else
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("v" + pair2.Key, "Relationship"));
                            }
                        }
                        else
                        {
                            if (pair1.Value == pair2.Value)
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("==" + pair2.Key, "Relationship"));
                            }
                            else
                            {
                                t1.AddRelationship(t2, UKS.GetOrAddThing("!" + pair2.Key, "Relationship"));
                            }
                        }
                    }
                    
                }
            }
        }

        void DeleteUnusedRelationships()
        {
            Thing relationshipParent = UKS.GetOrAddThing("Relationship", "Thing");
            for (int i = 0; i < relationshipParent.Children.Count; i++)
            {
                Thing relationshipType = relationshipParent.Children[i];
                for (int j = 0; j < relationshipType.Children.Count; j++)
                {
                    Thing child = relationshipType.Children[j];
                    if (child.Relationships.Count != 1 || child.RelationshipsFrom.Count != 1)
                    {
                        UKS.DeleteThing(child);
                        j--;
                    }
                }
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            prevAttnTarget = null;
            attnVisualTarget = null;

           
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
            //UKS.GetOrAddThing("action", "Object");
            //UKS.GetOrAddThing("count", "Object");
            //UKS.GetOrAddThing("one", "count");
            //UKS.GetOrAddThing("two", "count");
            //UKS.GetOrAddThing("three", "count");
            //UKS.GetOrAddThing("four", "count");

            //UKS.GetOrAddThing("speed", "Object");
            //UKS.GetOrAddThing("slow", "speed");
            //UKS.GetOrAddThing("fast", "speed");

            //UKS.GetOrAddThing("food", "Object");
            //UKS.GetOrAddThing("dogfood", "food");
            //UKS.GetOrAddThing("color", "Object");
            //UKS.GetOrAddThing("orange", "color");
            //UKS.GetOrAddThing("swim", "action");
            //UKS.GetOrAddThing("eat", "action");
            //UKS.GetOrAddThing("ride", "action");
            //UKS.GetOrAddThing("animal", "Object");
            //UKS.GetOrAddThing("body-part", "Object");
            //UKS.GetOrAddThing("furniture", "Object");
            //UKS.GetOrAddThing("table", "furniture");
            //UKS.GetOrAddThing("chair", "furniture");
            //UKS.GetOrAddThing("toy", "Object");
            //UKS.GetOrAddThing("bike", "toy");
            //UKS.GetOrAddThing("cat", "animal");
            //UKS.GetOrAddThing("dog", "animal");
            //UKS.GetOrAddThing("fish", "animal");
            //UKS.GetOrAddThing("Kevin", "animal");
            //UKS.GetOrAddThing("horse", "animal");
            //UKS.GetOrAddThing("tail", "body-part");
            //UKS.GetOrAddThing("leg", "body-part");
            //UKS.GetOrAddThing("fin", "body-part");
            //UKS.Labeled("cat").AddRelationship(UKS.Labeled("tail"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("one") });
            //UKS.Labeled("dog").AddRelationship(UKS.Labeled("tail"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("one") });
            //UKS.Labeled("fish").AddRelationship(UKS.Labeled("tail"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("one") });
            //UKS.Labeled("horse").AddRelationship(UKS.Labeled("tail"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("one") });
            //UKS.Labeled("dog").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"),new List<Thing>() { UKS.Labeled("four")});
            //UKS.Labeled("cat").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("four") });
            //UKS.Labeled("horse").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("four") });
            //UKS.Labeled("Kevin").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("two") });
            //UKS.Labeled("table").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("four") });
            //UKS.Labeled("chair").AddRelationship(UKS.Labeled("leg"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("four") });
            //UKS.Labeled("Kevin").AddRelationship(UKS.Labeled("dog"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("two") });
            //UKS.Labeled("Kevin").AddRelationship(UKS.Labeled("ride"), UKS.GetOrAddThing("can", "Relationship"), new List<Thing>() { UKS.Labeled("horse") });
            //UKS.Labeled("fish").AddRelationship(UKS.Labeled("fin"), UKS.GetOrAddThing("has", "Relationship"), new List<Thing>() { UKS.Labeled("five") });
            //UKS.Labeled("dog").AddRelationship(UKS.Labeled("swim"), UKS.GetOrAddThing("can", "Relationship"), new List<Thing>() { UKS.Labeled("slow") });
            //UKS.Labeled("fish").AddRelationship(UKS.Labeled("swim"), UKS.GetOrAddThing("can", "Relationship"), new List<Thing>() { UKS.Labeled("fast") });
            //UKS.Labeled("cat").AddRelationship(UKS.Labeled("orange"), UKS.GetOrAddThing("is", "Relationship"));
            //UKS.Labeled("cat").AddRelationship(UKS.Labeled("eat"), UKS.GetOrAddThing("can", "Relationship"), new List<Thing>() { UKS.Labeled("fish") });
            //UKS.Labeled("dog").AddRelationship(UKS.Labeled("eat"), UKS.GetOrAddThing("can", "Relationship"), new List<Thing>() { UKS.Labeled("cat"), UKS.Labeled("dogfood") });
            MainWindow.ResumeEngine();
        }
    }
}
