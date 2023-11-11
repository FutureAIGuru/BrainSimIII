//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//


using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using BrainSimulator.Modules;

namespace BrainSimulator
{
    //a thing is anything, physical object, attribute, word, action, etc.
    public class Thing
    {
        private List<Relationship> relationships = new List<Relationship>(); //synapses to "has", "is", others
        private List<Relationship> relationshipsFrom = new List<Relationship>(); //synapses from
        public IList<Relationship> RelationshipsNoCount { get { lock (relationships) { return new List<Relationship>(relationships.AsReadOnly()); } } }
        public List<Relationship> RelationshipsWriteable { get => relationships; }
        public IList<Relationship> RelationshipsFrom { get { lock (relationshipsFrom) { return new List<Relationship>(relationshipsFrom.AsReadOnly()); } } }
        public List<Relationship> RelationshipsFromWriteable { get => relationshipsFrom; }

        private string label = ""; //this is just for convenience in debugging 
        object value;
        public int useCount = 0;
        public long lastFired = 0;
        public DateTime lastFiredTime = new();

        public object V
        {
            get => value;
            set
            {
                if (value is Thing t)
                    throw new ArgumentException("Cannot add a Thing to a Thing's Value");
                this.value = value;
            }
        }

        public string Label
        {
            get
            {
                return label;
            }

            set
            { //This code allows you to put a * at the end of a label and it will auto-increment
                string newLabel = value;
                if (newLabel.EndsWith("*"))
                {
                    int greatestValue = -1;
                    string baseString = newLabel.Substring(0, newLabel.Length - 1);
                    foreach (Thing parent in Parents)
                    {
                        lock (parent)
                        {
                            foreach (Thing t in parent.Children)
                            {
                                if (t.Label.StartsWith(baseString))
                                {
                                    if (int.TryParse(t.Label.Substring(baseString.Length), out int theVal))
                                    {
                                        if (theVal > greatestValue)
                                            greatestValue = theVal;
                                    }
                                }
                            }
                        }
                    }
                    greatestValue++;
                    newLabel = baseString + greatestValue.ToString();
                }
                label = newLabel;
            }
        }

        public IList<Thing> Parents
        {
            get
            {
                List<Thing> list = new List<Thing>();
                lock (relationshipsFrom)
                {
                    foreach (Relationship r in relationshipsFrom)
                        if (r.relType != null && Relationship.TrimDigits(r.relType.Label) == "has-child" && r.target == this)
                            list.Add(r.source);
                }
                return list;
            }
        }

        public IList<Thing> Children
        {
            get
            {
                List<Thing> list = new List<Thing>();
                lock (relationships)
                {
                    foreach (Relationship r in relationships)
                        if (r.relType != null && Relationship.TrimDigits(r.relType.Label) == "has-child" && r.target != null)
                            list.Add(r.target);
                }
                return list;
            }
        }

        public IList<Relationship> Relationships
        {
            get
            {
                lock (relationships)
                {
                    foreach (Relationship r in relationships)
                        r.misses++;
                    return new List<Relationship>(relationships.AsReadOnly());
                }
            }
        }

        public IList<Relationship> RelationshipsWithoutChildren
        {
            get
            {
                List<Relationship> retVal = new();
                foreach (Relationship r in Relationships)
                    if (r.reltype == null || Relationship.TrimDigits(r.relType.Label) != "has-child") retVal.Add(r);
                return retVal;
            }
        }

        public float Value()
        {
            float retVal = 0;
            float denom = .1f;
            retVal = useCount / denom;
            TimeSpan age = lastFiredTime - DateTime.Now;
            retVal = retVal / (float)age.TotalMilliseconds;
            //bloating the score so its more readable
            retVal = retVal * -10000;
            return retVal;
        }


        /// ////////////////////////////////////////////////////////////////////////////
        //Handle the descendents of a Thing
        public int GetDescendentsCount()
        {
            return DescendentsList().Count;
        }
        public IList<Thing> DescendentsList(List<Thing> descendents = null)
        {
            if (descendents == null)
            {
                descendents = new List<Thing>();
            }
            if (descendents.Count < 5000)
            {
                foreach (Thing t2 in this.Children)
                {
                    if (t2 == null) continue;
                    if (!descendents.Contains(t2))
                    {
                        descendents.Add(t2);
                        t2.DescendentsList(descendents);
                    }
                }
            }
            return descendents;
        }

        //recursively gets all descendents
        public IEnumerable<Thing> Descendents
        {
            get
            {
                IList<Thing> descendents = DescendentsList();
                for (int i = 0; i < descendents.Count; i++)
                {
                    Thing child = descendents[i];
                    yield return child;
                }
            }
        }

        //Get the ancestors of a thing with recursion

        public IList<Thing> AncestorList(List<Thing> ancestors = null, int depth = 0)
        {
            depth++;
            if (depth > 10)
                return ancestors;
            if (ancestors == null)
            {
                ancestors = new List<Thing>();
            }
            foreach (Thing t2 in this.Parents)
            {
                if (!ancestors.Contains(t2))// && ancestors.Count < 100)
                {
                    ancestors.Add(t2);
                    t2.AncestorList(ancestors, depth);
                }
                else
                { }  //track circular reference?
            }
            return ancestors;
        }

        public IList<Relationship> GetAttributesWithInheritance(string relationshipToTrace,bool followFroms)
        {
            List<Relationship> resultList = new();
            var ancestors = ExpandTransitiveRelationship(relationshipToTrace,followFroms);
            ancestors.Insert(0, this);
            foreach (Thing t in ancestors)
                foreach (Relationship r in t.relationships)
                {
                    //type match
                    if (r.reltype.Label == relationshipToTrace) 
                        goto DontAdd;
                    //already in list
                    if (resultList.FindFirst(x => x.reltype == r.reltype && x.target == r.target) != null) 
                        goto DontAdd ;
                    //conflicts 
                    foreach (Relationship r1 in resultList)
                        if (Exclusive(r1, r)==0) 
                            goto DontAdd;
                    resultList.Add(r);
                DontAdd: continue;
                }
             return resultList;
        }
        float Exclusive(Relationship r1,Relationship r2)
        {
            //todo extend to handle instances of targets
            if (r1.target == r2.target)
            {
                var commonParents = ModuleUKS.FindCommonParents(r1.reltype,r2.reltype);
                if (commonParents.Count > 0)
                {
                    //IList<Thing> r1RelProps = GetProperties(r1.reltype);
                    //IList<Thing> r2RelProps = GetProperties(r2.reltype);

                }
            }
            return 1;
        }

        public IList<Thing> ExpandTransitiveRelationship(string relationshipToTrace,bool followFroms)
        {
            //for has-child relationships, followFroms=true  gets ancestors
            //get the full list of ancestors including duplicates and depths
            List<(Thing,int)> resultList = FollowTransitivieRelationshipOneLevel(this, 1, relationshipToTrace, followFroms);
            //sort by depth and remove duplicates
            resultList = resultList.OrderBy(x => x.Item2).ToList();
            List<Thing> ancestors = new();
            foreach (var t1 in resultList)
                if (ancestors.Count == 0 || t1.Item1 != ancestors.Last()) ancestors.Add(t1.Item1);
            return ancestors;
        }
        public IList<(Thing t,int depth)> ExpandTransitiveRelationshipWithDepth(string relationshipToTrace,bool followFroms)
        {
            //get the full list of ancestors including duplicates and depths
            List<(Thing t,int depth)> resultList = FollowTransitivieRelationshipOneLevel(this, 1, relationshipToTrace,followFroms);
            //sort by depth and remove duplicates
            resultList = resultList.OrderBy(x => x.Item2).ToList();
            List<(Thing t,int depth)> ancestors = new();
            foreach (var t1 in resultList)
                if (ancestors.Count == 0 || t1 != ancestors.Last()) ancestors.Add(t1);
            return ancestors;
        }
        private List<(Thing t,int depth)> FollowTransitivieRelationshipOneLevel(Thing t, int depth, string relationshipToTrace, bool followFroms)
        {
            List<(Thing t, int depth)> resultList = new();
            if (followFroms)
            {
                foreach (Relationship r in t.RelationshipsFrom)
                {
                    if (r.relType == null) continue;
                    if (r.relType.Label != relationshipToTrace && !r.relType.HasAncestorLabeled(relationshipToTrace)) continue;
                    if (resultList.FindFirst(x => x.t == r.s) == default)
                        resultList.AddRange(FollowTransitivieRelationshipOneLevel(r.s, depth + 1, relationshipToTrace, followFroms));
                    resultList.Add((r.s, depth));
                }
            }
            else
            {
                foreach (Relationship r in t.Relationships)
                {
                    if (r.relType == null) continue;
                    if (r.relType.Label != relationshipToTrace && !r.relType.HasAncestorLabeled(relationshipToTrace)) continue;
                    if (resultList.FindFirst(x => x.t == r.target) == default)
                        resultList.AddRange(FollowTransitivieRelationshipOneLevel(r.target, depth + 1, relationshipToTrace, followFroms));
                    resultList.Add((r.target, depth));
                }
            }
            return resultList;
        }

        public IEnumerable<Thing> Ancestors
        {
            get
            {
                IList<Thing> ancestors = AncestorList();
                for (int i = 0; i < ancestors.Count; i++)
                {
                    Thing child = ancestors[i];
                    yield return child;
                }
            }
        }

        public bool HasAncestorLabeled(string label)
        {
            IList<Thing> ancestors = AncestorList();
            for (int i = 0; i < ancestors.Count; i++)
            {
                Thing parent = ancestors[i];
                if (parent.label == label)
                    return true;
            }
            return false;
        }
        public bool HasAncestor(Thing t)
        {
            IList<Thing> ancestors = AncestorList();
            for (int i = 0; i < ancestors.Count; i++)
            {
                Thing parent = ancestors[i];
                if (parent == t)
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            string retVal = label + ": " + useCount;
            if (Relationships.Count > 0)
            {
                retVal += " {";
                foreach (Relationship l in Relationships)
                    retVal += l.T?.label + ",";
                retVal += "}";
            }
            return retVal;
        }

        public void SetFired(Thing t = null)
        {
            if (t != null)
            {
                // t.lastFired = MainWindow.theNeuronArray.Generation;
                t.lastFiredTime = DateTime.Now;
                t.useCount++;
            }
            else
            {
                // lastFired = MainWindow.theNeuronArray.Generation;
                lastFiredTime = DateTime.Now;
                useCount++;
            }
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////
        /// </summary>

        //RELATIONSHIPS

        public List<Thing> RelationshipsAsThings
        {
            get
            {
                List<Thing> retVal = new List<Thing>();
                foreach (Relationship l in Relationships)
                    retVal.Add(l.target);
                return retVal;
            }
        }

        //add a relationship from this thing to the specified thing
        public Relationship AddRelationship(Thing t, float weight = 1)//, SentenceType sentencetype = null)
        {
            if (t == null) return null; //do not add null relationship or duplicates
                                        // Commented Below Code because Phrases need duplicates
                                        // ReferencesWriteable.RemoveAll(v => v.T == t);
                                        // t.relationshipdByWriteable.RemoveAll(v => v.T == this);
            Relationship newLink;
            //if (sentencetype == null)
            //{
            //    sentencetype = new SentenceType();
            //}
            newLink = new Relationship { source = this, T = t, weight = weight/*, sentencetype = sentencetype */};
            lock (relationships)
            {
                relationships.Add(newLink);
            }
            lock (t.relationshipsFrom)
            {
                t.relationshipsFrom.Add(newLink);
            }
            //SetFired(t);

            return newLink;
        }

        public Relationship AddRelationWithoutDuplicate(Thing t, float weight = 1)
        {
            if (t == null) return null; //do not add null relationships or duplicates

            Relationship newLink = new Relationship { source = this, T = t, weight = weight };
            Relationship l = relationships.Find(l => l.source == this && l.T == t);
            if (l != null) // Link already exists, set new weight.
            {
                l.weight = weight;
            }
            else // Link doesn't exist, create it.
            {
                lock (relationships)
                {
                    relationships.Add(newLink);
                }
                lock (t.relationshipsFrom)
                {
                    t.relationshipsFrom.Add(newLink);
                }
            }
            return newLink;
        }

        public void InsertRelationshipAt(int index, Thing t, float weight = 1)
        {
            if (t == null) return; //do not add null relationships
            Relationship r = new Relationship { source = this, T = t, weight = weight };
            lock (relationships)
            {
                if (index > relationships.Count) return;
                Relationships.Insert(index, r);
            }
            lock (t.relationshipsFrom)
            {
                t.RelationshipsFrom.Add(r);
            }
        }

        public void RemoveRelationship(Thing t)
        {
            if (t == null) return;
            //t.sentencetype = new SentenceType { belief = new SentenceType.Belief() { TRUTH = null, Tense = null } };
            bool wasRelationship = false; ////TODO take this out when relationshipBY is reversed
            foreach (Relationship l in Relationships)
            {
                if (l.relType is not null && l.reltype.Label != "has-child") //hack for performance
                {
                    lock (l.relType.relationshipsFrom)
                        l.relType.relationshipsFrom.RemoveAll(v => v.target == t && v.source == this);
                }
            }
            lock (relationships)
            {
                relationships.RemoveAll(v => v.target == t);
            }
            lock (t.relationshipsFrom)
            {
                t.relationshipsFrom.RemoveAll(v => v.source == this);
            }
        }

        public void RemoveRelationship(Relationship r)
        {
            if (r == null) return;
            if (r.reltype == null) return;
            if (r.source == null)
            {
                lock (r.relType.RelationshipsFromWriteable)
                {
                    lock (r.target.RelationshipsFromWriteable)
                    {
                        r.relType.RelationshipsFromWriteable.Remove(r);
                        r.target.RelationshipsFromWriteable.Remove(r);
                    }
                }
            }
            else if (r.target == null)
            {
                lock (r.source.RelationshipsWriteable)
                {
                    lock (r.relType.RelationshipsFromWriteable)
                    {
                        r.source.RelationshipsWriteable.Remove(r);
                        r.relType.RelationshipsFromWriteable.Remove(r);
                    }
                }
            }
            else
            {
                lock (r.source.RelationshipsWriteable)
                {
                    lock (r.relType.RelationshipsFromWriteable)
                    {
                        lock (r.target.RelationshipsFromWriteable)
                        {
                            r.source.RelationshipsWriteable.Remove(r);
                            r.relType.RelationshipsFromWriteable.Remove(r);
                            r.target.RelationshipsFromWriteable.Remove(r);
                        }
                    }
                }
            }
            foreach (ClauseType c in r.clauses)
                RemoveRelationship(c.clause);
        }

        public void RemoveAllRelationships()
        {
            lock (relationships)
            {
                while (relationships.Count > 0)
                {
                    RemoveRelationshipAt(0);
                }
            }
        }
        //TODO this only works for simple relationships
        public void RemoveRelationshipAt(int i)
        {
            lock (relationships)
            {
                if (i < relationships.Count)
                {
                    Relationship r = relationships[i];
                    if (r.target != null)
                    {
                        lock (r.target.relationshipsFrom)
                        {
                            for (int j = 0; j < r.target.relationshipsFrom.Count; j++)
                            {
                                Relationship l1 = r.target.relationshipsFrom[j];
                                if (l1.source == this && l1.reltype == r.reltype)
                                {
                                    r.target.relationshipsFrom.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                        relationships.RemoveAt(i);
                    }
                }
            }
        }

        public Relationship HasRelationship(Thing t)
        {
            foreach (Relationship L in Relationships)
                if (L.T == t) return L;
            return null;
        }

        public Thing HasRelationshipWithParent(Thing t)
        {
            foreach (Relationship L in Relationships)
                if (L.T.Parents.Contains(t)) return L.T;
            return null;
        }

        public Thing HasRelationshipWithAncestorLabeled(string s)
        {
            foreach (Relationship L in Relationships)
            {
                if (L.T != null)
                {
                    Thing t = L.T.AncestorList().FindFirst(x => x.Label == s);
                    if (t != null) return L.T;
                }
            }
            return null;
        }

        //(send a negative value to decrease a relationship weight)
        public float AdjustRelationship(Thing t, float incr = 1)
        {
            //change any exisiting link or add a new one
            Relationship existingLink = Relationships.FindFirst(v => v.T == t);
            if (existingLink == null && incr > 0)
            {
                existingLink = AddRelationship(t, incr);
            }
            if (existingLink != null)
            {
                if (existingLink.relType is not null)
                {
                    //TODO adjust the weight of relationshipType revers link
                }
                else
                {
                    Relationship reverseLink = existingLink.T.relationshipsFrom.Find(v => v.T == this);
                    existingLink.weight += incr;
                    if (incr > 0) existingLink.hits++;
                    if (incr < 0) existingLink.misses++;
                    reverseLink.weight = existingLink.weight;
                    reverseLink.hits = existingLink.hits;
                    reverseLink.misses = existingLink.misses;
                }
                if (existingLink.weight < 0)
                {
                    return -1;
                }
                return existingLink.weight;
            }
            return 0;
        }

        public Relationship AddRelationship(Thing t2, Thing relationshipType, List<Thing> modifiers)
        {
            Relationship rel = AddRelationship(t2, relationshipType);

            //foreach (Thing mod in modifiers)
            //{
            //    rel.targetProperties.Add(mod);
            //}
            return rel;
        }
        public Relationship AddRelationship(Thing t2, Thing relationshipType, int count)
        {
            Relationship rel = AddRelationship(t2, relationshipType);
            rel.count = count;
            //rel.AsThing.Label = this.Label + " " + relationshipType.Label + " " + t2.Label;
            //rel.AsThing.AddParent(this.AncestorLabeled("Object"));
            return rel;
        }
        //public Relationship AddRelationship(Thing t2, Thing relationshipType, int count, SentenceType sentencetype)
        //{
        //    Relationship rel = AddRelationship(t2, relationshipType);
        //    if (rel == null) return null;
        //    rel.count = count;
        //    //rel.sentencetype = sentencetype;
        //    return rel;
        //}

        public Relationship AddRelationship(Thing t2, Thing relationshipType)//, SentenceType sentencetype = null)
        {
            if (relationshipType == null)
                return null;

            relationshipType.SetFired();
            //Relationship r = HasRelationship(t2, relationshipType);
            //if (r != null)
            //{
            //    AdjustRelationship(r.T);
            //    return r;
            //}
            Relationship r = new Relationship()
            {
                relType = relationshipType,
                source = this,
                T = t2,
                //sentencetype = (sentencetype != null ? sentencetype : new SentenceType())
            };
            if (t2 != null)
            {
                lock (relationships)
                    lock (t2.relationshipsFrom)
                        lock (relationshipType.relationshipsFrom)
                        {
                            RelationshipsWriteable.Add(r);
                            t2.RelationshipsFromWriteable.Add(r);
                            relationshipType.RelationshipsFromWriteable.Add(r);
                        }
            }
            else
            {
                lock (relationships)
                    lock (relationshipType.relationshipsFrom)
                    {
                        RelationshipsWriteable.Add(r);
                        relationshipType.RelationshipsFromWriteable.Add(r);
                    }
            }
            return r;
        }

        public Relationship HasRelationship(Thing t2, Thing relationshipType)
        {
            Relationship retVal = null;
            foreach (Relationship r in Relationships)
            {
                if ((r.relType == relationshipType || relationshipType == null) && r.target == t2)
                {
                    retVal = r;
                    break;
                }
            }
            return retVal;
        }


        public void RemoveRelationship(Thing t2, Thing relationshipType)
        {
            RemoveRelationship(t2);
        }


        public Dictionary<string, object> GetRelationshipsAsDictionary(bool optionalbool = false)
        {
            if (!optionalbool)
            {
                Dictionary<string, object> retVal = new Dictionary<string, object>();
                foreach (Relationship l in Relationships)
                {
                    IList<Thing> thingParents = l.T.Parents;
                    if (thingParents.Count == 0)
                        continue;
                    if (thingParents[0].Label != "MentalModel")
                    {
                        IList<Thing> par = l.T.Parents;
                        if (par.Count > 0)
                        {
                            string propLabel = par[0].Label;
                            if (propLabel == "TransientProperty")
                                propLabel = l.T.Label;
                            if (!retVal.ContainsKey(propLabel))
                            {
                                if (l.T.HasAncestorLabeled("TransientProperty"))
                                {
                                    retVal.Add(propLabel, l.T.V);
                                }
                                else
                                {
                                    if (l.T.Children.Count > 0)
                                        retVal.Add(propLabel, l.T.Children[0].V);
                                }
                            }
                        }
                    }
                }
                return retVal;
            }
            else
            {
                Dictionary<string, object> retVal = new Dictionary<string, object>();
                foreach (Relationship l in Relationships)
                {
                    if (l.T.Label != "MentalModel")
                        try
                        {
                            if (!retVal.ContainsKey(l.T.Label))

                            {
                                retVal.Add(l.T.Label, l.T.V);
                            }

                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.StackTrace);
                        }
                }
                return retVal;
            }
        }

        //returns all the matching refrences
        public List<Relationship> GetRelationshipsWithAncestor(Thing t)
        {
            List<Relationship> retVal = new List<Relationship>();
            lock (relationships)
            {
                for (int i = 0; i < Relationships.Count; i++)
                {
                    if (Relationships[i].T.HasAncestor(t))
                    {
                        retVal.Add(Relationships[i]);
                    }
                }
                return retVal.OrderBy(x => -x.Value1).ToList();
            }
        }

        public void RemoveRelationshpsWithAncestor(Thing t)
        {
            if (t == null) return;
            lock (relationships)
            {
                for (int i = 0; i < relationships.Count; i++)
                {
                    if (relationships[i].T.HasAncestor(t))
                    {
                        RemoveRelationship(relationships[i].T);
                        i--;
                    }
                }
            }
        }

        //returns the best matching relationship
        public Thing GetRelationshipWithAncestor(Thing t)
        {
            List<Relationship> refs = GetRelationshipsWithAncestor(t);
            if (refs.Count > 0)
                return refs[0].T;
            return null;
        }

        public List<Relationship> GetRelationshipByWithAncestor(Thing t)
        {
            List<Relationship> retVal = new List<Relationship>();
            for (int i = 0; i < relationshipsFrom.Count; i++)
            {
                if (relationshipsFrom[i].source.HasAncestor(t))
                {
                    retVal.Add(relationshipsFrom[i]);
                }
            }
            return retVal.OrderBy(x => -x.Value1).ToList();
        }


        public void ChangeParent(Thing oldParent, Thing newParent)
        {
            if (oldParent == null || newParent == null && !newParent.Descendents.Contains(newParent)) return;

            RemoveParent(oldParent);
            AddParent(newParent);

        }

        public void ChangeReleationship(Thing oldref, Thing newref)
        {
            if (oldref == null || newref == null) return;
            RemoveRelationship(oldref);
            AddRelationship(newref);

        }

        Thing hasChildType;
        Thing HasChild
        {
            get
            {
                if (hasChildType == null)
                {
                    /*
                    if (MainWindow.theNeuronArray == null) return null;
                    if (MainWindow.theNeuronArray.modules == null) return null;

                    // var uks = MainWindow.modules.Find(x => x. == "UKS");
                    uks.GetUKS();
                    hasChildType = uks.UKS.Labeled("has-child");
                    if (hasChildType == null)
                    {
                        Thing relParent = uks.UKS.Labeled("Relationship");
                        if (relParent == null)
                        {
                            relParent = uks.UKS.AddThing("Relationship", null);
                        }
                        Debug.Assert(relParent != null);
                        hasChildType = uks.UKS.AddThing("has-child", null);
                        relParent.AddRelationship(hasChildType, hasChildType);
                        uks.UKS.Labeled("Thing").AddRelationship(relParent, hasChildType);
                    }
                    */
                }
                return hasChildType;
            }

        }
        public void AddParent(Thing t)//, SentenceType sentencetype = null)
        {
            if (t == null) return;
            if (!Parents.Contains(t))
            {
                t.AddRelationship(this, HasChild);//, sentencetype);
            }
        }

        public void RemoveParent(Thing t)
        {
            t.RemoveRelationship(this);
        }

        public Relationship AddChild(Thing t)
        {
            return AddRelationship(t, HasChild);
        }

        public void RemoveChild(Thing t)
        {
            RemoveRelationship(t, HasChild);
        }

        public bool HasChildWithLabel(string labelToFind)
        {
            foreach (Thing t in Children)
            {
                if (t?.Label == labelToFind)
                {
                    return true;
                }
            }
            return false;
        }
        //TODO:  move this to the UKS
        internal Thing ChildWithProperties(List<string> properties)
        {
            foreach (Thing t in Children)
            {
                List<Thing> list = t.RelationshipsAsThings;
                foreach (string s in properties)
                    if (list.FindFirst(x => x.Label == s) == null) goto notFound;
                return t;
            notFound: continue;
            }
            return null;
        }
    }

    //this is a modification of Thing which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thing references with int indexed into an array and makes things much more compact
    public class SThing
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<SRelationship> relationships = new();
        object value;
        public object V { get => value; set => this.value = value; }
        public int useCount;
    }
}
