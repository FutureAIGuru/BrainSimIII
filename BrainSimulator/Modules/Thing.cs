//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//


using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainSimulator.Modules
{
    /// <summary>
    /// In the lexicon of graphs, a Thing is a "node".  A Thing can represent anything, physical object, attribute, word, action, etc.
    /// </summary>
    public partial class Thing
    {
        private List<Relationship> relationships = new List<Relationship>(); //synapses to "has", "is", others
        private List<Relationship> relationshipsFrom = new List<Relationship>(); //synapses from
        private List<Relationship> relationshipsAsType = new List<Relationship>(); //nodes which use this as a relationshipType
        /// <summary>
        /// Only used by the tree control
        /// </summary>
        public IList<Relationship> RelationshipsNoCount { get { lock (relationships) { return new List<Relationship>(relationships.AsReadOnly()); } } }
        /// <summary>
        /// Get an "unsafe" writeable list of a Thing's Relationships.
        /// This list may change while it is in use and so should not be used as a foreach iterator
        /// </summary>
        public List<Relationship> RelationshipsWriteable { get => relationships; }
        /// <summary>
        /// Get a "safe" list of relationships which target this Thing
        /// </summary>
        public IList<Relationship> RelationshipsFrom { get { lock (relationshipsFrom) { return new List<Relationship>(relationshipsFrom.AsReadOnly()); } } }
        /// <summary>
        /// Get an "unsafe" writeable list of Relationships which target this Thing
        /// </summary>
        public List<Relationship> RelationshipsFromWriteable { get => relationshipsFrom; }
        /// <summary>
        /// Get an "unsafe" writeable list of Relationships for which this Thing is the relationship type
        /// </summary>
        public List<Relationship> RelationshipsAsTypeWriteable { get => relationshipsAsType; }

        private string label = "";
        object value;
        public int useCount = 0;
        public DateTime lastFiredTime = new();
        
        /// <summary>
        /// Any serializable object can be attached to a Thing
        /// </summary>
        public object V
        {
            get => value;
            set
            {
                if (value is not null && !value.GetType().IsSerializable)
                    throw new ArgumentException("Cannot set nonserializable value");
                this.value = value;
            }
        }


        /// <summary>
        /// Returns a Thing's label
        /// don't delete this ToString because the debugger uses it when mousing over a Thing
        /// </summary>
        /// <returns>the Thing's label</returns>
        public override string ToString()
        {
            string retVal = label;
            return retVal;
        }
        /// <summary>
        /// Formats a displayable Thing as a string
        /// </summary>
        /// <param name="showProperties">Appends a parenthetical list of relationships to the label</param>
        /// <returns>The string to display</returns>
        public string ToString(bool showProperties = false)
        {
            string retVal = label;// + ": " + useCount;
            if (Relationships.Count > 0 && showProperties)
            {
                retVal += " {";
                foreach (Relationship l in Relationships)
                    retVal += l.target?.label + ",";
                retVal += "}";
            }
            return retVal;
        }

        /// <summary>
        /// Manages a Thing's label and maintais a hash table
        /// </summary>
        public string Label
        {
            get => label;
            set
            {
                if (value == label) return; //label is unchanged
                label = ThingLabels.AddThingLabel(value, this);
            }
        }

        private IList<Thing> RelationshipsOfType(Thing relType, bool useRelationshipFrom = false)
        {
            IList<Thing> retVal = new List<Thing>();
            if (!useRelationshipFrom)
            {
                lock (relationshipsFrom)
                {
                    foreach (Relationship r in relationships)
                        if (r.relType != null && r.relType == relType && r.source == this)
                            retVal.Add(r.target);
                }
            }
            else
            {
                lock (relationshipsFrom)
                {
                    foreach (Relationship r in relationshipsFrom)
                        if (r.relType != null && r.relType == relType && r.target == this)
                            retVal.Add(r.source);
                }
            }
            return retVal;
        }
        private bool IsKindOf(Thing thingType)
        {
            if (this == thingType) return true;
            foreach (Thing t in this.Parents)
                if (t.IsKindOf(thingType)) return true;
            return false;
        }


        /// <summary>
        /// "Safe" list of direct ancestors
        /// </summary>
        public IList<Thing> Parents { get => RelationshipsOfType(HasChild, true); }

        /// <summary>
        /// "Safe" list of direct descendants
        /// </summary>
        public IList<Thing> Children { get => RelationshipsOfType(HasChild, false); }

        /// <summary>
        /// Full "Safe" list or relationships
        /// </summary>
        public IList<Relationship> Relationships
        {
            get
            {
                lock (relationships)
                {
                    foreach (Relationship r in relationships)
                        r.Misses++;
                    return new List<Relationship>(relationships.AsReadOnly());
                }
            }
        }

        /// ////////////////////////////////////////////////////////////////////////////
        //Handle the ancestors and descendents of a Thing
        //////////////////////////////////////////////////////////////
        public IList<Thing> AncestorList()
        {
            return FollowTransitiveRelationships(hasChildType, true);
        }

        /// <summary>
        /// Recursively gets all the ancestors of a Thing
        /// </summary>
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
            Thing t = ThingLabels.GetThing(label);
            if (t == null) return false;
            return HasAncestor(t);
        }
        public bool HasAncestor(Thing t)
        {
            var x = FollowTransitiveRelationships(hasChildType, true, t);
            return x.Count != 0;
        }

        public int GetDescendentsCount()
        {
            return DescendentsList().Count;
        }
        public IList<Thing> DescendentsList()
        {
            return FollowTransitiveRelationships(hasChildType, false);
        }

        /// <summary>
        /// Rrecursively gets all descendents of a Thing. Use with caution as this might be a large list
        /// </summary>
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

        //Follow chain of relationships with relType
        private IList<Thing> FollowTransitiveRelationships(Thing relType, bool followUpwards = true, Thing searchTarget = null)
        {
            List<Thing> retVal = new();
            retVal.Add(this);
            if (this == searchTarget) return retVal;

            for (int i = 0; i < retVal.Count; i++)
            {
                Thing t = retVal[i];
                IList<Relationship> relationshipsToFollow = followUpwards ? t.RelationshipsFrom : t.Relationships;
                foreach (Relationship r in relationshipsToFollow)
                {
                    Thing thingToAdd = followUpwards ? r.source : r.target;
                    if (r.relType == relType)
                    {
                        if (!retVal.Contains(thingToAdd))
                            retVal.Add(thingToAdd);
                    }
                    if (searchTarget == thingToAdd)
                        return retVal;
                }
            }
            if (searchTarget != null) retVal.Clear();
            return retVal;
        }


        public void SetFired(Thing t = null)
        {
            if (t != null)
            {
                t.lastFiredTime = DateTime.Now;
                t.useCount++;
            }
            else
            {
                lastFiredTime = DateTime.Now;
                useCount++;
            }
        }


        //RELATIONSHIPS

        //add a relationship from this thing to the specified thing
        public Relationship AddRelationship(Thing target, Thing relationshipType)
        {
            if (relationshipType == null)
                return null;

            //does the relationship already exist?
            Relationship r = HasRelationship(target, relationshipType);
            if (r != null)
            {
                //AdjustRelationship(r.T);
                return r;
            }
            r = new Relationship()
            {
                relType = relationshipType,
                source = this,
                target = target,
            };
            if (target != null)
            {
                lock (relationships)
                    lock (target.relationshipsFrom)
                        lock (relationshipType.relationshipsAsType)
                        {
                            RelationshipsWriteable.Add(r);
                            target.RelationshipsFromWriteable.Add(r);
                            if (!relationshipType.RelationshipsAsTypeWriteable.Contains(r))
                                relationshipType.RelationshipsAsTypeWriteable.Add(r);
                        }
            }
            else
            {
                lock (relationships)
                    lock (relationshipType.relationshipsAsType)
                    {
                        RelationshipsWriteable.Add(r);
                        if (!relationshipType.RelationshipsAsTypeWriteable.Contains(r))
                            relationshipType.RelationshipsAsTypeWriteable.Add(r);
                    }
            }
            return r;
        }
        private Relationship HasRelationship(Thing target, Thing relationshipType)
        {
            foreach (Relationship r in relationships)
            {
                if (r.source == this && r.target == target && r.relType == relationshipType)
                    return r;
            }
            return null;
        }
        public Relationship HasRelationship(Thing t)
        {
            foreach (Relationship r in Relationships)
                if (r.target == t) return r;
            return null;
        }

        public void RemoveRelationship(Thing t2, Thing relationshipType)
        {
            Relationship r = new() { source = this, relType = relationshipType, target = t2 };
            RemoveRelationship(r);
        }

        public void RemoveRelationship(Relationship r)
        {
            if (r == null) return;
            if (r.relType == null) return;
            if (r.source == null)
            {
                lock (r.relType.RelationshipsFromWriteable)
                {
                    lock (r.target.RelationshipsFromWriteable)
                    {
                        r.relType.RelationshipsFromWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                        r.target.RelationshipsFromWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                    }
                }
            }
            else if (r.target == null)
            {
                lock (r.source.RelationshipsWriteable)
                {
                    lock (r.relType.RelationshipsFromWriteable)
                    {
                        r.source.RelationshipsWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                        r.relType.RelationshipsFromWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
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
                            r.source.RelationshipsWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                            r.relType.RelationshipsFromWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                            r.target.RelationshipsFromWriteable.RemoveAll(x => x.source == r.source && x.relType == r.relType && x.target == r.target);
                        }
                    }
                }
            }
            foreach (Clause c in r.Clauses)
                RemoveRelationship(c.clause);
        }

        public Thing HasRelationshipWithParent(Thing t)
        {
            foreach (Relationship L in Relationships)
                if (L.target.Parents.Contains(t)) return L.target;
            return null;
        }

        public Thing HasRelationshipWithAncestorLabeled(string s)
        {
            foreach (Relationship L in Relationships)
            {
                if (L.target != null)
                {
                    Thing t = L.target.AncestorList().FindFirst(x => x.Label == s);
                    if (t != null) return L.target;
                }
            }
            return null;
        }

        public void AddParent(Thing newParent)
        {
            if (newParent == null) return;
            if (!Parents.Contains(newParent))
            {
                newParent.AddRelationship(this, HasChild);
            }
        }
        public Relationship AddChild(Thing t)
        {
            return AddRelationship(t, HasChild);
        }


        public void RemoveParent(Thing t)
        {
            Relationship r = new() { source = t,relType = hasChildType, target = this };
            t.RemoveRelationship(r);
        }

        public void RemoveChild(Thing t)
        {
            Relationship r = new() { source = this, relType = hasChildType, target = t };
            RemoveRelationship(r);
        }

        public bool HasPropertyLabeled(string label)
        {
            foreach (Relationship r in relationships)
                if (r.relType.Label.ToLower() == "hasproperty" && r.target.Label.ToLower() == label.ToLower())
                    return true;
            foreach (Thing t in Parents)
            {
                return t.HasPropertyLabeled(label);
            }
            return false;
        }
    }
}
