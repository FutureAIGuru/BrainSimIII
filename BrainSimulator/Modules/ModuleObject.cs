//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BrainSimulator.Modules
{
    public class ModuleObject : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;

        public  bool canBubble = false;
        public bool canParent = true;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleObject()
        {
        }

        Relationship pendingRelationship = null;


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();
            //IList<Thing> allObjects;
            ////ModuleUKS parent = (ModuleUKS)FindModule("UKS");
            //int refCount = 0;
            //int tCount = 0;
            //Thing Object = UKS.GetOrAddThing("Object", "Thing");
            //allObjects = Object.DescendentsList();


            //foreach (Thing obj in allObjects)
            //{
            //    refCount += obj.RelationshipsNoCount.Count;
            //}

            //tCount = allObjects.Count;


            //if (refCount > 500)
            //{
            //    ForgetCall();
            //}
            //if (tCount > 1000)
            //{
            //    ForgetCall2();
            //}

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }



        //return true if "Initialize" in in the call stack
        private bool StackContains(string target, int count = 0)
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

            // do not bubble if called from Initialization
            foreach (StackFrame stackFrame in stackFrames)
            {
                string name = stackFrame.GetMethod().Name;
                if (name.Contains(target))
                {
                    if (count == 0)
                        return true;
                    count--;
                }
            }
            return false;
        }

        public Relationship DoPendingRelationship(bool doIt = true)
        {
            if (!doIt) pendingRelationship = null;
            if (pendingRelationship == null) return null;
            Relationship r2 = UKS.AddStatement(pendingRelationship.source, pendingRelationship.reltype, pendingRelationship.target);
            if (r2 != null)
            {
                r2.weight = pendingRelationship.weight;
                r2.inferred = pendingRelationship.inferred;
            }
            pendingRelationship = null;
            return r2;
        }
        public Relationship GetPendingRelationship() { return pendingRelationship; }

        public class ParentCount { public Thing parent; public int count; public float weight; };
        public class RelationshipCount : Relationship
        {
            public float score = 0;
            public int count = 0;
            public RelationshipCount() { }
            public RelationshipCount(Relationship r)
            {
                source = r.source;
                reltype = r.reltype;
                target = r.target;
                //sentencetype = r.sentencetype;
                weight = r.weight;
                foreach (ClauseType c in r.clauses)
                    clauses.Add(c);
            }
        }

        public void PredictParents(Relationship r)
        {
            if (!canParent) return;
            if (r.source == null) return;
            if (StackContains("Initialize")) return;
            if (StackContains("PredictParents", 1)) return;
            if (!r.source.HasAncestorLabeled("Object")) return;
            GetUKS();

            NoteChildrenWithNegativeParents(r.source);
            //possible parents of the source
            if (r.source?.Parents.FindFirst(x => x.Label == "Object") != null && r.target?.Parents.FindFirst(x => x.Label == "Relationship") == null)
                PredictParentsOfSource(r.source);
            //possible parents of the target  TODO: doesn't work if the target is an instance because it has parameters
            if (r.target?.Parents.FindFirst(x => x.Label == "Object" || x.Label.StartsWith("unknownObject")) != null)
                PredictParentsOfTarget(r.target);
            // PredictParentsWithRelationshipTypes(r.target);
            //possible parents of the type?
        }

        private void NoteChildrenWithNegativeParents(Thing child)
        {
            // if "fido is not a dog" then we may want to ask what fido *is*
            foreach (Relationship r in child.Relationships)
            {
                if (Relationship.TrimDigits(r.reltype.Label) == "has-child")
                {
                    if (r.reltype.Relationships.FindFirst(x => x.target.Label == "not") != null)
                    {
                        pendingRelationship = new()
                        {
                            source = null,
                            target = r.target,
                            reltype = UKS.Labeled("has-child"),
                            inferred = true,
                            weight = 0.9f,
                        };
                    }
                }
            }
        }

        private void PredictParentsOfSource(Thing child)
        {
            List<Relationship> targetProperties = child.RelationshipsWithoutChildren.ToList();
            List<RelationshipCount> allMatches = new();

            //Find ALL the related relationships
            foreach (Relationship r in targetProperties)
            {
                var x = UKS.Query(null, r.reltype, r.target).ToList();
                if (x.Count < 2)
                    x = UKS.Query(null, r.reltype, null).ToList();
                foreach (Relationship r3 in x)
                    allMatches.Add(new RelationshipCount(r3));
            }

            //don't consider self as a target
            allMatches.RemoveAll(x => x.source == child);

            //don't consider specific instances as potential parents (use the generic)
            //allMatches.RemoveAll(x => char.IsDigit(x.source.Label.Last()));

            FindLikelyParent(child, allMatches, "source");
        }
        private void PredictParentsOfTarget(Thing child)
        {

            List<Relationship> targetProperties = child.RelationshipsFrom.ToList();
            List<RelationshipCount> allMatches = new();

            //Find ALL the related relationships
            foreach (Relationship r in targetProperties)
            {
                if (r.reltype.Label == "has-child") continue;
                var x = UKS.Query(null, r.reltype, r.target).ToList();
                x.RemoveAll(x => x.target == child);
                x.RemoveAll(x => child.Parents.Contains(x.target));
                if (x.Count < 2)
                {
                    x = UKS.Query(null, r.reltype, null).ToList();
                    if (x.Count > 20) x.RemoveAll(x => x == x);
                    x.RemoveAll(x => x.target == child);
                    x.RemoveAll(x => child.Parents.Contains(x.target));
                }
                foreach (Relationship r3 in x)
                    allMatches.Add(new RelationshipCount(r3));
            }

            //don't consider self as a target
            allMatches.RemoveAll(x => x.target == child);

            FindLikelyParent(child, allMatches, "T");
        }

        private void FindLikelyParent(Thing child, List<RelationshipCount> allMatches, string propName = "")
        {
            if (allMatches.Count > 30) return;
            if (allMatches.Count == 0) return;
            List<ParentCount> parentCounts = new();
            var prop = typeof(Relationship).GetProperty(propName);
            foreach (Relationship r in allMatches)
            {
                var val = (Thing)prop.GetValue(r, null); //this is getting a property of Relationship (source or target) by name
                if (val == null) continue;
                List<Relationship> parents = new()                    ;
                if (val.Children.Count == 0)
                    parents = val.RelationshipsFrom.FindAll(x => x.reltype.Label == "has-child");
                else
                    parentCounts.Add(new ParentCount { parent = val, count = 1, weight = r.weight });
                foreach (Relationship r1 in parents)
                {
                    if (r1.weight < .8f) continue;
                    Thing t;
                    //if (propName == "T")
                    t = r1.source;
                    //else
                    //    t = r1.target;
                    ParentCount pc = parentCounts.FindFirst(x => x.parent == t);
                    if (pc == null)
                        parentCounts.Add(new ParentCount { parent = t, count = 1, weight = r1.weight });
                    else
                        pc.count++;
                }
            }

            //if we have a definitive parent, set it
            //if ambiguous, put it in a pendingRelationship for verification
            parentCounts.RemoveAll(x => x.parent.Label == "Object");
            if (parentCounts.Count > 0)
            {
                int highestCount = parentCounts.Max(x => x.count);
                var candidates = parentCounts.FindAll(x => x.count == highestCount);
                //in case of a tie, check for highest weight
                candidates = candidates.FindAll(x => x.weight == candidates.Max(x => x.weight));
                Thing theNewParent = candidates[0].parent;
                var theBestCandidate = candidates[0];
                foreach (var c in candidates)
                {
                    if (c.count > theBestCandidate.count ||
                        (c.count == theBestCandidate.count && c.weight > theBestCandidate.weight)
                        )
                    {
                        theNewParent = c.parent;
                        theBestCandidate = c;
                    }
                }
                if (theNewParent != null && !UKS.ThingInTree(child, theNewParent))
                {
                    pendingRelationship = new()
                    {
                        source = child,
                        target = theNewParent,
                        reltype = UKS.Labeled("is-a"),
                        inferred = true,
                        weight = theBestCandidate.weight * 0.9f,
                    };
                    if (parentCounts.Count < 3) DoPendingRelationship();
                }
            }
        }

        Thing GetInstanceParent(Thing t)
        {
            Thing retVal = t;
            if (char.IsDigit(t.Label.Last()) && t.Parents.Count == 1)
            {
                retVal = t.Parents[0];
            }
            return retVal;
        }
        private void RemoveLeapfroggingRelationships(Thing child)
        {
            foreach (Thing t in child.Parents)
            {
                foreach (Thing t1 in child.Parents)
                {
                    if (t == t1) { continue; }
                    if (t.HasAncestor(t1))
                    {
                        child.RemoveParent(t1);
                        if (!t.HasAncestorLabeled("Object"))
                            child.AddParent(t1);
                        break;
                    }
                }
            }
        }

        private void PredictParentsWithRelationshipTypes(Thing child)
        {
            //here we try to find common parents of a target...places, body parts., etc.
            List<RelationshipCount> allMatches = new();
            var x = UKS.Query(null, null, child).ToList();
            x.RemoveAll(x => x.reltype.Label == "has-child");
            foreach (Relationship r in x)
            {
                var x1 = UKS.Query(null, Relationship.TrimDigits(r.reltype.Label), null);
                foreach (Relationship r3 in x1)
                    allMatches.Add(new RelationshipCount(r3));
            }
            allMatches.RemoveAll(x => x.target == child);

            List<ParentCount> parentCounts = new();
            foreach (Relationship r in allMatches)
            {
                if (r.target == null) continue;
                foreach (Thing t in r.target.Parents)
                {
                    ParentCount pc = parentCounts.FindFirst(x => x.parent == t);
                    if (pc == null)
                        parentCounts.Add(new ParentCount { parent = t, count = 1, });
                    else
                        pc.count++;
                }
            }
            if (parentCounts.Count > 0)
            {
                var x1 = parentCounts.MaxBy(x => x.count);
                Thing theNewParent = x1.parent;
                if (theNewParent.Label != "Object" && !theNewParent.Label.Contains("unknown") && !child.Ancestors.Contains(theNewParent))
                {
                    pendingRelationship = new()
                    {
                        source = child,
                        target = theNewParent,
                        reltype = UKS.Labeled("is-a"),
                        inferred = true,
                        weight = 0.9f,
                    };
                    if (x1.count > 2) DoPendingRelationship(); //if the concensus is great, add the relationship without asking
                }
            }
        }

        public void BubbleProperties1(Thing child)
        {
            if (!canBubble) return;
            //TODO: prevent bubbling of things which have a lot of blank parameters
            //TODO: special casse of a thing with a single parent
            //TODO: generalize to count better
            //TODO: take a relationship as input
            GetUKS();
            if (StackContains("Initialize")) return;
            if (StackContains("CreateInstance")) return;
            if (child == null) return;
            RemoveLeapfroggingRelationships(child);

            foreach (Thing parent in child.Parents)
                UnBubbleProperties(parent);
            foreach (Thing Parent in child.Parents)
            {
                if (Parent.Label == "Object") continue;
                if (Parent.Label == "MentalModel") continue;
                //TODO handle instances by getting the relavant parent
                List<Relationship> relList2 = new();
                List<Relationship> relList = new();
                foreach (Thing child1 in Parent.Children)
                {
                    foreach (Relationship r in child1.RelationshipsWithoutChildren)
                    {
                        if (r.T == null || r.T.HasAncestorLabeled("TransientProperty")) continue;
                        relList.Add(r);
                        if (!relList2.Contains(r))
                        {
                            relList2.Add(r);
                        }
                    }
                }

                //bubble the remaining majority relationships
                foreach (Relationship r1 in relList2)
                {
                    float valCount = relList.Count(x => x.T == r1.T && x.reltype == r1.reltype);
                    if (r1.T.Parents.Count == 0) continue;
                    float numWithParent = relList.Count(x => x.T.Parents.Count > 0 && x.T.Parents[0] == r1.T.Parents[0]);
                    float likelihood = valCount / numWithParent;
                    if (likelihood > 0.1f &&
                        Parent.Relationships.FindFirst(x => x.T == r1.T && x.reltype == r1.reltype) == null)
                    {
                        Relationship r2 = new Relationship
                        {
                            source = Parent,
                            relType = r1.reltype,
                            target = r1.target,
                            inferred = true,
                            weight = r1.weight * likelihood * .9f,
                        };
                        foreach (Relationship r3 in Parent.Relationships)
                        {
                            if (r3.inferred) continue;
                            if (UKS.RelationshipsAreExclusive(r3, r2))
                            {
                                goto dontWrite;
                            }
                        }
                        ModuleUKS.WriteTheRelationship(r2);
                    dontWrite:
                        continue;
                    }
                }
            }
            return;
        }

        public void BubbleProperties(Thing Parent)
        {
            if (StackContains("Initialize")) return;

            UnBubbleProperties(Parent);
            List<Relationship> relList2 = new();
            List<Relationship> relList = new();
            foreach (Thing child in Parent.Children)
            {
                foreach (Relationship r in child.RelationshipsWithoutChildren)
                {
                    if (r.T.HasAncestorLabeled("TransientProperty")) continue;
                    relList.Add(r);
                    if (!relList2.Contains(r))
                    {
                        relList2.Add(r);
                    }
                }
            }
            foreach (Relationship r1 in relList2)
            {
                float valCount = relList.Count(x => x.T == r1.T);
                float numWithParent = relList.Count(x => x.T.Parents[0] == r1.T.Parents[0]);
                float likelihood = valCount / numWithParent;
                if (likelihood > 0.5f)
                {
                    Relationship r2 = Parent.AddRelationship(r1.target);
                    r2.reltype = r1.reltype;
                    r2.weight = likelihood * .9f;
                    r2.inferred = true;
                }
            }

            return;
        }

        public void UnBubbleProperties(Thing Parent)
        {
            if (Parent.Children.Count == 0)
                return;
            foreach (Relationship r in Parent.Relationships)
            {
                if (r.inferred && r.reltype?.Label != "has-child")
                    Parent.RemoveRelationship(r.T);
            }

        }

        public void UnBubbleAll()
        {
            GetUKS();
            Thing Object = UKS.GetOrAddThing("Object", "Thing");
            if (Object.Descendents.Count() > 0)
            {
                foreach (Thing child in Object.Descendents)
                {
                    UnBubbleProperties(child);
                }
            }

        }
        //Test code, only called by a dialog button
        public void BubbleAll()
        {
            GetUKS();
            Thing Object = UKS.GetOrAddThing("Object", "Thing");

            if (Object.Descendents.Count() > 0)
            {
                foreach (Thing descendent in Object.Descendents)
                {
                    //find leaf node
                    if (descendent.Children.Count == 0)
                    {
                        foreach (Thing ancestor in descendent.Ancestors)
                        {//bubble all properties
                            if (ancestor != Object) BubbleProperties(ancestor);
                        }
                    }
                    //continue until all leafs are finished
                }
            }
        }

        public void UnBubbleProperty(Thing Parent)
        {
            GetUKS();
            Thing MentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            List<Thing> pThings = Parent.RelationshipsAsThings;
            //Thing Object = UKS.GetOrAddThing("Object", "Thing");
            if (Parent.Descendents.Count() == 0 && Parent.GetRelationshipByWithAncestor(MentalModel).Count == 0)
            {
                Parent.RemoveAllRelationships();
            }
        }

        public void ForgetCall()
        {
            GetUKS();
            Thing Object = UKS.GetOrAddThing("Object", "Thing");

            Forget(Object);
        }

        public int NegetiveEvidenceHelper(Thing Parent, Relationship r)
        {
            int result = 0;
            foreach (Thing Child in Parent.Children)
            {
                foreach (Relationship cr in Child.Relationships)
                {
                    if (cr.relType == r.relType)
                        if (cr.T == r.T)
                            if (cr.count != r.count)
                            {
                                result++;
                            }
                }
            }
            return result;
        }

        public void ConnectedRelationshipCheck()
        {
            GetUKS();
            Thing R = UKS.GetOrAddThing("Relationship", "Thing");
            for (int i = 0; i < R.Children.Count(); i++)
            {
                Thing Con = R.Children[i];
                if (Con.Label == "" || Con.Label == "Can" || Con.Label == "Is" || Con.Label == "Has" || Con.Label == "has-child") continue;
                Thing Object = UKS.GetOrAddThing("Object", "Thing");
                Thing temp1 = new();
                Thing temp2 = new();

                foreach (Thing t in Object.Descendents)
                {
                    foreach (Relationship re in Con.RelationshipsFrom)
                    {
                        if (t == re.source)
                        {
                            temp2 = re.target;
                            foreach (Relationship r in t.RelationshipsFrom)
                            {
                                if (r.relType != null)
                                    temp1 = r.source;
                                if (r.target == re.target)
                                {
                                    r.count = 1;
                                    return;
                                }
                                if (temp1.Label == "" || r.relType == null) continue;
                                if (r.count > 0)
                                    temp1.AddRelationship(temp2, r.relType, r.count);
                                else
                                    temp1.AddRelationship(temp2, r.relType);
                            }
                        }
                        else if (t == re.target)
                        {
                            temp2 = re.source;
                            foreach (Relationship r in t.RelationshipsFrom)
                            {
                                if (r.relType != null)
                                    temp1 = r.source;
                                if (r.target == re.source)
                                {
                                    r.count = 1;
                                    return;
                                }
                                if (temp1.Label == "" || r.relType == null) continue;
                                if (r.count > 0)
                                    temp1.AddRelationship(temp2, r.relType, r.count);
                                else
                                    temp1.AddRelationship(temp2, r.relType);
                            }
                        }
                    }
                }
            }
        }

        public void Forget(Thing Parent)
        {
            float min = float.MaxValue;
            Thing delParent = null;
            Relationship deleteCandidate = null;
            if (Parent.Descendents.Count() > 0)
            {
                foreach (Thing desc in Parent.Descendents)
                {
                    foreach (Relationship rel in desc.Relationships)
                    {
                        if (min > rel.Value())
                        {
                            min = rel.Value();
                            deleteCandidate = rel;
                            delParent = desc;
                        }

                    }
                }
            }
            //check if a canidate has been selected for deletion
            if (min < float.MaxValue)
            {
                //delParent.RemoveRelationship(deleteCandidate.target);

            }

        }

        public void ForgetCall2()
        {
            GetUKS();
            Thing Object = UKS.GetOrAddThing("Object", "Thing");

            Forget2(Object);
        }

        public void Forget2(Thing Parent)
        {
            /*
            ModuleUKS uks = (ModuleUKS)MainWindow.FindModule(typeof(ModuleUKS));
            float min = float.MaxValue;
            Thing delParent = null;
            if (Parent.Descendents.Count() > 0)
            {
                foreach (Thing desc in Parent.Descendents)
                {
                    if (desc.Children.Count == 0)
                    {
                        if (min > desc.Value())
                        {
                            min = desc.Value();
                            delParent = desc;
                        }
                    }
                }
            }
            //check if a canidate has been selected for deletion
            if (min < float.MaxValue)
            {
                uks.DeleteThing(delParent);
            }
            */
        }

        public void AddNewParentRelationship(Thing Parent, Thing Target)
        {
            GetUKS();
            //should only run when new thing is added to object in UKS for the sake of efficiency
            Thing Object = UKS.GetOrAddThing("Object", "Thing");
            if (Parent != null)
            {
                if (Object.Descendents.Contains(Target))
                {
                    Target.AddParent(Parent);
                    Target.RemoveParent(Object);
                    if (Target.Ancestors.Contains(Object))
                    {
                        return;
                    }
                    else
                        Target.AddParent(Object);
                    return;
                }
            }
        }

        public void AddAllMatches(Thing Parent)
        {
            GetUKS();
            Thing Object = UKS.GetOrAddThing("Object", "Thing");
            Thing MentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            List<Thing> DescendentList = Object.Descendents.ToList();
            List<Thing> pThings = Parent.RelationshipsAsThings;
            if (Parent != null)
            {
                //bubble here
                for (int i = 0; i < DescendentList.Count; i++)
                {
                    List<Thing> tThings = DescendentList[i].RelationshipsAsThings;
                    int j = 0;
                    if (DescendentList[i] != Parent)
                    {

                        for (int k = 0; k < tThings.Count; k++)
                        {
                            if (Parent.RelationshipsAsThings.Contains(tThings[k]))
                            {
                                j++;
                            }
                            if (j == Parent.Relationships.Count && j != 0)
                            {
                                //ensure no duplicates or self references
                                if (!Parent.Ancestors.Contains(DescendentList[i]))
                                {
                                    Parent.AddChild(DescendentList[i]);
                                    // DescendentList[i]

                                    DescendentList[i].RemoveParent(Object);
                                    if (!DescendentList[i].Ancestors.Contains(Object))
                                        DescendentList[i].AddParent(Object);
                                }

                            }

                        }
                    }
                    foreach (Thing t in MentalModel.Children)
                    {
                        for (int k = 0; k < pThings.Count; k++)
                        {
                            if (t.RelationshipsAsThings.Contains(pThings[k]))
                            {
                                if (!t.RelationshipsAsThings.Contains(Parent) && k == pThings.Count - 1)
                                    t.AddRelationship(Parent);
                            }
                            else
                                break;

                        }
                    }
                }
            }
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            //hack a hierarchy into the UKS
            GetUKS();

            if (UKS != null)
            {
                //AddNewUndefined();
                //this should also be called in the Mental Model whenever a new object is added to the list
            }
        }

        public void AddNewUndefined()
        {
            //may need a new method for single new object, as this will re-add all the objects to undefined
            Thing Objects = UKS.GetOrAddThing("Object", "Thing");
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            IEnumerable<Thing> objectDescendents = Objects.Descendents;
            foreach (Thing t in mentalModel.Children)
            {
                if (!objectDescendents.Contains(t))
                    Objects.AddRelationship(t);
            }
        }

        public void AddParentObject(Thing t, string s)
        {
            //if parent is just now being created, change parent to new parent and return
            if (t.Label == "") { return; }
            if (s == "") { return; }
            GetUKS();
            Thing thing = UKS.Labeled("Object");
            Thing newParent = UKS.Labeled(s, thing.Descendents.ToList());
            if (newParent == null)
            {
                newParent = UKS.GetOrAddThing(s, "Object");
                if (t != newParent && newParent.Children.Count == 0)
                {
                    t.ChangeParent(UKS.Labeled("Object"), newParent);
                    return;
                }
            }
            //if connection is non circular and the first connection
            if (t != newParent && newParent.Children.Count == 0)
            {
                t.ChangeParent(UKS.Labeled("Object"), newParent);
                if (t.Ancestors.Contains(UKS.Labeled("Object")))
                    return;
                else
                    t.AddParent(UKS.Labeled("Object"));
            }
            else
            {
                UnBubbleProperties(newParent);
                AddNewParentRelationship(newParent, t);
                BubbleProperties(newParent);
            }

        }

        public void AddParentReference(Thing t, string parentLabel)
        {
            //List<Thing> parent = t.Parents;
            GetUKS();
            Thing thing = UKS.Labeled("Object");
            Thing newParent = UKS.Labeled(parentLabel, thing.Descendents.ToList());
            if (newParent == null)
            {
                newParent = UKS.GetOrAddThing(parentLabel, "Object");
                newParent.V = 0;

                t.ChangeReleationship(UKS.Labeled("Object"), newParent);
                foreach (Thing prop in t.RelationshipsAsThings)
                {
                    if (prop.HasAncestor(UKS.Labeled("Property")) &&
                        !prop.HasAncestor(UKS.Labeled("TransientProperty")))
                    {
                        newParent.AddRelationship(prop, UKS.GetOrAddThing("HasProperty", "Relationship"));
                    }
                }
            }
            else
            {
                Thing MentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
                if (newParent.Children.Count == 0 &&
                    (newParent.GetRelationshipByWithAncestor(MentalModel).Count == 0 ||
                    newParent.GetRelationshipByWithAncestor(MentalModel) == null))
                {
                    foreach (Thing x in t.RelationshipsAsThings)
                    {
                        Thing trans = UKS.Labeled("TransientProperty");
                        // if (!x.HasAncestor(trans))
                        // newParent.AddReference(x);
                    }
                }
                AddNewParentRelationship(newParent, t);
                t.AddRelationWithoutDuplicate(newParent);
            }

            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            for (int i = 0; i < mentalModel.Children.Count; i++)
            {
                var test = mentalModel.Children[i].RelationshipsAsThings;
                var refs = newParent.RelationshipsAsThings;
                // var obj = t.GetReferencesAsDictionary();
                int x = 0;
                //for (int k = 0; k < obj.Count; k++)
                //{
                //    var key = obj.ElementAt(k).Key;

                for (int j = 0; j < refs.Count; j++)
                {
                    var kvp = refs.ElementAt(j);
                    var value = kvp.Label;

                    if (test.Contains(kvp))
                    {
                        x++;
                        if (x == refs.Count)
                        {
                            string a = mentalModel.Children[i].ToString();
                            string[] b = a.Split(":");
                            //extract only object name
                            if (b[0] == t.Label)
                            {
                                //reference already exist
                                break;
                            }
                            var r = mentalModel.Children[i].GetRelationshipsAsDictionary(true);
                            if (!r.ContainsKey(newParent.Label))
                                mentalModel.Children[i].AddRelationWithoutDuplicate(newParent);
                        }
                        continue;
                    }
                    else { break; }
                    // }
                }
            }
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
            // if (mv == null) return; //this is called the first time before the module actually exists
        }
        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();

            //each demo object must have transient properties to prevent a crash

        }
    }
}