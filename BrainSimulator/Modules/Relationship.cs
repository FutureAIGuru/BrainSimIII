//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace BrainSimulator.Modules
{
    //these are used so that relatinoship lists can be readOnly.
    //This prevents programmers from accidentally doing a (e.g.) relationships.Add() which will not handle reverse links properly
    //Why the IList doesn't have FindAll and FindFirst is a ???
    public static class IListExtensions
    {
        public static T FindFirst<T>(this IList<T> source, Func<T, bool> condition)
        {
            foreach (T item in source)
                if (condition(item))
                    return item;
            return default(T);
        }
        public static List<T> FindAll<T>(this IList<T> source, Func<T, bool> condition)
        {
            List<T> theList = new List<T>();
            if (source == null) return theList;
            foreach (T item in source)
                if (condition(item))
                    theList.Add(item);
            return theList;
        }
    }

    /// <summary>
    /// In the same way a Relationship relates 2 Things (the "source" and the "target") with a relationship type, a Clause relates two Relationsips 
    /// with a clauseType. Every Relationship has a list of clauses with the Relationship representing source and the Clause containing its type and target.
    /// </summary>
    public class Clause
    {
        public Thing clauseType;
        public Relationship clause;
        public Clause() { }
        public Clause(Thing theType, Relationship clause1)
        {
            clauseType = theType;
            clause = clause1;
        }

    };

    /// <summary>
    /// This is used internally during query processing
    /// </summary>
    public class QueryRelationship : Relationship
    {
        public List<Thing> typeProperties = new();
        public List<Thing> sourceProperties = new();
        public List<Thing> targetProperties = new();
        public QueryRelationship() { }
        public QueryRelationship(Relationship r)
        {
            source = r.source;
            relType = r.relType;
            target = r.target;
            foreach (Clause c in r.Clauses)
                Clauses.Add(c);
        }
    }

    /// <summary>
    /// In the lexicon of graphs, a Relationship is an "edge".
    /// A Relationship is a weighted link between two Things, the "source" and the "target". The Relationship has a type which is also a Thing. Various other
    /// properties are used to track the usage of a Relationship which is used to help determine the most likely result of a query.
    /// Each relationship also maintains a list of "Clause"s which are relationships to other Relationships. 
    /// </summary>
    public class Relationship
    {
        private Thing s = null;
        public Thing source
        {
            get => s;
            set { s = value; }
        }
        private Thing reltype = null;
        public Thing relType
        {
            get { return reltype; }
            set
            {
                reltype = value;
            }
        }
        private Thing targ = null;
        public Thing target
        {
            get { return targ; }
            set
            {
                targ = value;
            }
        }

        private bool inferred = false;
        private List<Clause> clauses = new();
        private List<Relationship> clausesFrom = new();

        private float weight = 1;
        private int hits = 0;
        private int misses = 0;
        private DateTime lastUsed = DateTime.Now;
        private DateTime created = DateTime.Now;

        //TimeToLive processing for relationships
        static private List<Relationship> transientRelationships = new List<Relationship>();
        static private DispatcherTimer transientTimer;
        private TimeSpan timeToLive = TimeSpan.MaxValue;

        /// <summary>
        /// When set, makes a Relationship transient
        /// </summary>
        public TimeSpan TimeToLive
        {
            get { return timeToLive; }
            set
            {
                timeToLive = value;
                if (timeToLive != TimeSpan.MaxValue)
                    AddToTransientList();
            }
        }

        public float Weight { get => weight; set => weight = value; }
        public int Hits { get => hits; set => hits = value; }
        public int Misses { get => misses; set => misses = value; }
        public DateTime LastUsed { get => lastUsed; set => lastUsed = value; }
        public DateTime Created { get => created; set => created = value; }
        public List<Clause> Clauses { get => clauses; set => clauses = value; }
        public List<Relationship> ClausesFrom { get => clausesFrom; set => clausesFrom = value; }

        public Relationship()
        { }

        public static Relationship GetRelationship(Relationship r)
        {
            foreach (Relationship r1 in r.source.Relationships)
            {
                if (r1 == r) return r1;
            }
            return null;
        }

        /// <summary>
        /// Updates the lastUsed time to the current time
        /// </summary>
        public void Fire()
        {
            LastUsed = DateTime.Now;
        }
        public Relationship(Relationship r)
        {
            Misses = r.Misses;
            relType = r.relType;
            source = r.source;
            Weight = r.Weight;
            Hits = r.Hits++;
            targ = r.targ;
            inferred = r.inferred;
            if (r.Clauses == null) Clauses = new();
            else Clauses = new(r.Clauses);
            if (r.ClausesFrom == null) ClausesFrom = new();
            else ClausesFrom = new(r.ClausesFrom);
        }

        public void ClearHits()
        {
            Hits = 0;
        }
        public void ClearAccessCount()
        {
            Misses = 0;
        }


        /// <summary>
        /// Adds a Clause to the Relationship
        /// </summary>
        /// <param name="clauseType">Thing e.g. "if", "because"... </param>
        /// <param name="r2">The dependent clause</param>
        /// <returns>The Relationship</returns>
        public Relationship AddClause(Thing clauseType, Relationship r2)
        {
            Clause theClause = new();

            theClause.clause = r2;
            theClause.clauseType = clauseType;
            if (Clauses.FindFirst(x => x.clauseType == theClause.clauseType && x.clause == r2) == null)
            {
                Clauses.Add(theClause);
                r2.ClausesFrom.Add(this);
            }

            return this;
        }

        private string ToString(List<Relationship> stack)
        {
            if (stack.Contains(this))
                return "";
            stack.Add(this);
            string retVal = "";
            string sourceModifierString = "";
            string typeModifierString = "";
            string targetModifierString = "";
            string allModifierString = "";

            retVal = BasicRelationshipToString(retVal, sourceModifierString, typeModifierString, targetModifierString);

            //handle Clauses
            //TODO prevent general circular reference stack overflow
            foreach (Clause c in Clauses)
                    allModifierString += c.clauseType.Label + " " + c.clause.ToString(stack)+" ";

            if (allModifierString != "")
                retVal += " " + allModifierString;

            return retVal;
        }


        /// <summary>
        /// Formats a string representing the Relationship (and its clauses)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string retVal = this.ToString(new List<Relationship>());
            return retVal;
        }

        private string BasicRelationshipToString(string retVal, string sourceModifierString, string typeModifierString, string targetModifierString)
        {
            if (!string.IsNullOrEmpty(source?.Label))
                retVal += source?.Label + sourceModifierString;
            if (!string.IsNullOrEmpty(relType?.Label))
                retVal += ((retVal == "") ? "" : "->") + relType?.Label + ThingProperties(relType) + typeModifierString;
            if (!string.IsNullOrEmpty(targ?.Label))
                retVal += ((retVal == "") ? "" : "->") + targ?.Label + ThingProperties(targ) + string.Join(", ", targetModifierString);
            else if (targetModifierString.Length > 0)
                retVal += targetModifierString;
            return retVal;
        }

        string ThingProperties(Thing t)
        {
            string retVal = null;
            foreach (Relationship r in t.Relationships)
            {
                if (r.reltype == Thing.HasChild) continue;
                if (t.Label.Contains("." + r.targ?.Label)) continue;
                if (r.relType?.Label == "is")
                {
                    if (retVal == null) retVal += "(";
                    else retVal += ", ";
                    retVal += r.targ?.Label;
                }
            }
            if (retVal != null)
                retVal += ")";
            return retVal;
        }

        public static bool operator ==(Relationship a, Relationship b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.targ == b.targ && a.source == b.source && a.relType == b.relType)
                return true;
            return false;
        }
        public static bool operator !=(Relationship a, Relationship b)
        {
            if (a is null && b is null)
                return false;
            if (a is null || b is null)
                return true;
            if (a.target == b.target && a.source == b.source && a.relType == b.relType) return false;
            return true;
        }


        private void AddToTransientList()
        {
            if (!transientRelationships.Contains(this)) transientRelationships.Add(this);
            if (transientTimer == null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    transientTimer = new();

                    transientTimer.Interval = TimeSpan.FromSeconds(1);
                    transientTimer.Tick += ForgetTransientRelationships;
                    transientTimer.Start();
                }));
            }
        }

        private void ForgetTransientRelationships(object sender, EventArgs e)
        {

            for (int i = transientRelationships.Count - 1; i >= 0; i--)
            {
                Relationship r = transientRelationships[i];
                //check to see if the relationship has expired
                if (r.timeToLive != TimeSpan.MaxValue && r.LastUsed + r.timeToLive < DateTime.Now)
                {
                    r.source.RemoveRelationship(r);
                    //if this leaves an orphan thing, delete the thing
                    if (r.reltype.Label == "has-child" && r.targ?.Parents.Count == 0)
                    {
                        r.targ.AddParent(ThingLabels.GetThing("unknownObject"));
                    }
                    transientRelationships.Remove(r);
                }
            }
        }


    }

    //this is a non-pointer representation of a relationship needed for XML storage
    public class SClause
    {
        public int clauseType = -1;
        public SRelationship r;
    }

    public class SRelationship
    {
        public int source = -1;
        public int target = -1;
        public int hits = 0;
        public int misses = 0;
        public float weight = 0;
        public int relationshipType = -1;
        public int count = -1;
        public List<SClause> clauses = new();
    }

}