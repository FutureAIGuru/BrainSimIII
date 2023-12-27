//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
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

    public class ClauseType
    {
        public Thing clauseType;
        public Relationship clause;
        public ClauseType() { }
        public ClauseType(Thing theType, Relationship clause1)
        {
            clauseType = theType;
            clause = clause1;
        }

    };

    public class QueryRelationship : Relationship
    {
        public List<Thing> typeProperties = new();
        public List<Thing> sourceProperties = new();
        public List<Thing> targetProperties = new();
        public QueryRelationship() { }
        public QueryRelationship(Relationship r)
        {
            source = r.source;
            reltype = r.reltype;
            target = r.target;
            foreach (ClauseType c in r.clauses)
                clauses.Add(c);
        }
    }

    //a relationship is a weighted link to a thing and has a type
    public class Relationship
    {
        public Thing s = null;
        public Thing source
        {
            get => s;
            set { s = value; }
        }
        public Thing reltype = null;
        public Thing relType
        {
            get { return reltype; }
            set
            {
                reltype = value;
            }
        }
        public Thing target = null;
        public Thing T
        {
            get { hits++; lastUsed = DateTime.Now; return target; }
            set
            {
                target = value;
            }
        }

        public bool inferred = false;
        public List<ClauseType> clauses = new();
        public List<Relationship> clausesFrom = new();

        public float weight = 1;
        public int hits = 0;
        public int misses = 0;
        public DateTime lastUsed = DateTime.Now;
        public DateTime created = DateTime.Now;

        //TimeToLive processing for relationships
        static private List<Relationship> transientRelationships = new List<Relationship>();
        static private DispatcherTimer transientTimer;
        private TimeSpan timeToLive = TimeSpan.MaxValue;
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

        public void Fire()
        {
            lastUsed = DateTime.Now;
        }
        public Relationship(Relationship r)
        {
            count = r.count;
            misses = r.misses;
            relType = r.relType;
            source = r.source;
            weight = r.weight;
            hits = r.hits++;
            target = r.target;
            inferred = r.inferred;
            if (r.clauses == null) clauses = new();
            else clauses = new(r.clauses);
            if (r.clausesFrom == null) clausesFrom = new();
            else clausesFrom = new(r.clausesFrom);
        }

        public void ClearHits()
        {
            hits = 0;
        }
        public void ClearAccessCount()
        {
            misses = 0;
        }

        public int count
        {
            get => -1;
            set { }
        }

        public Relationship AddClause(Thing clauseType, Relationship r2)
        {
            ClauseType theClause = new();

            theClause.clause = r2;
            theClause.clauseType = clauseType;
            if (clauses.FindFirst(x => x.clauseType == theClause.clauseType && x.clause == r2) == null)
            {
                clauses.Add(theClause);
                r2.clausesFrom.Add(this);
            }

            return this;
        }

        public  string ToString(List<Relationship> stack)
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
            foreach (ClauseType c in clauses)
                    allModifierString += c.clauseType.Label + " " + c.clause.ToString(stack);

            if (allModifierString != "")
                retVal += " " + allModifierString;

            return retVal;
        }

        public override string ToString()
        {
            string retVal = this.ToString(new List<Relationship>());
            return retVal;
        }

        private string BasicRelationshipToString(string retVal, string sourceModifierString, string typeModifierString, string targetModifierString)
        {
            if (!string.IsNullOrEmpty(source?.Label))
                retVal += TrimDigits(source?.Label) + sourceModifierString;
            if (!string.IsNullOrEmpty(relType?.Label))
                retVal += ((retVal == "") ? "" : "->") + relType?.Label + ThingProperties(relType) + typeModifierString;
            if (!string.IsNullOrEmpty(target?.Label))
                retVal += ((retVal == "") ? "" : "->") + target?.Label + ThingProperties(target) + string.Join(", ", targetModifierString);
            else if (targetModifierString.Length > 0)
                retVal += targetModifierString;
            return retVal;
        }

        public static string TrimDigits(string s)
        {
            if (s is null) return null;
            while (s.Length > 0 && char.IsDigit(s[s.Length - 1]))
                s = s.Substring(0, s.Length - 1);
            return s;
        }
        string ThingProperties(Thing t)
        {
            string retVal = null;
            foreach (Relationship r in t.Relationships)
            {
                if (r.reltype == Thing.HasChild) continue;
                if (t.Label.Contains("." + r.target?.Label)) continue;
                if (r.relType?.Label == "is")
                {
                    if (retVal == null) retVal += "(";
                    else retVal += ", ";
                    retVal += r.T?.Label;
                }
            }
            if (retVal != null)
                retVal += ")";
            return retVal;
        }

        public override bool Equals(Object o)
        {
            if (o is Relationship r)     
                return r == this;
            return false;
        }
        public static bool operator ==(Relationship a, Relationship b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.target == b.target && a.source == b.source && a.relType == b.relType)
                return true;
            return false;
        }
        public static bool operator !=(Relationship a, Relationship b)
        {
            if (a is null && b is null)
                return false;
            if (a is null || b is null)
                return true;
            if (a.T == b.T && a.source == b.source && a.relType == b.relType) return false;
            return true;
        }


        public float Value
        {
            get
            {
                //need a way to track how confident we should be
                float retVal = weight;
                if (hits != 0 && misses != 0)
                {
                    //replace with more robust algorithm
                    float denom = misses;
                    if (denom == 0) denom = .1f;
                    retVal = hits / denom;
                }
                return retVal;
            }
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
                if (r.timeToLive != TimeSpan.MaxValue && r.lastUsed + r.timeToLive < DateTime.Now)
                {
                    r.source.RemoveRelationship(r);
                    //if this leaves an orphan thing, delete the thing
                    if (r.reltype.Label == "has-child" && r.target?.Parents.Count == 0)
                    {
                        r.target.AddParent(ThingLabels.GetThing("unknownObject"));
                    }
                    transientRelationships.Remove(r);
                }
            }
        }


    }

    //this is a non-pointer representation of a relationship needed for XML storage
    public class SClauseType
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
        public List<SClauseType> clauses = new();
    }

}