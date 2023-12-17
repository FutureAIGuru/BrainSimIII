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

    public class ClauseType
    {
        public ClsType a;
        public Relationship clause;
        public ClauseType() { }
        public ClauseType(ClsType a1, Relationship clause1)
        {
            a = a1;
            clause = clause1;
        }

    };
    public enum ClsType { all, source, type, target, condition };

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
            set {s = value;}
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
            get=>-1;
            set{}
        }

        public Relationship AddClause(string clauseType, Relationship r2)
        {
            Relationship r = null;
            ClauseType theClause = null;
            switch (clauseType.ToLower())
            {
                case "condition":
                    theClause = new ClauseType { a = ClsType.condition};
                    break;
                case "source":
                    theClause = new ClauseType { a = ClsType.source};
                    break;
                case "target":
                    theClause = new ClauseType { a = ClsType.target};
                    break;
                case "type":
                    theClause = new ClauseType { a = ClsType.type};
                    break;
                case "all":
                    theClause = new ClauseType { a = ClsType.all};
                    break;
            }
            if (theClause != null)
            {
                theClause.clause = r2;
                if (clauses.FindFirst(x => x.a == theClause.a && x.clause == r2) == null)
                {
                    clauses.Add(theClause);
                    r2.clausesFrom.Add(this);
                }
            }

            return r;
        }
            

        public override string ToString()
        {
            string retVal = "";
            string sourceModifierString = "";
            string typeModifierString = "";
            string targetModifierString = "";
            string allModifierString = "";
            foreach (ClauseType c in clauses)
            {
                switch (c.a)
                {
                    case ClsType.source:
                        {
                            sourceModifierString += " AND " + c.clause.source;
                            break;
                        }
                    case ClsType.type:
                        {
                            typeModifierString += " AND " + c.clause.relType;
                            break;
                        }
                    case ClsType.target:
                        {
                            targetModifierString += " AND  " + c.clause.target;
                            break;
                        }
                    case ClsType.all:
                        {
                            targetModifierString += " AND " + c.clause;
                            break;
                        }
                    case ClsType.condition:
                        {
                            targetModifierString += " IF " + c.clause;
                            break;
                        }
                }
            }

            if (!string.IsNullOrEmpty(source?.Label))
                retVal += TrimDigits(source?.Label) + /*ThingProperties(source) +*/ sourceModifierString;
            if (!string.IsNullOrEmpty(relType?.Label))
                retVal += ((retVal == "") ? "" : "->") + relType?.Label + ThingProperties(relType) + typeModifierString;
            if (!string.IsNullOrEmpty(target?.Label))
                //                retVal += ((retVal == "") ? "" : "->") + TrimDigits(T?.Label) + ThingProperties(target) + string.Join(", ", targetModifierString);
                retVal += ((retVal == "") ? "" : "->") + target?.Label + ThingProperties(target) + string.Join(", ", targetModifierString);
            else if (targetModifierString.Length > 0)
                retVal += targetModifierString;
            if (allModifierString != "")
            {
                retVal += " IF " + allModifierString;
            }
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
                if (t.Label.Contains("." + r.target?.Label))continue;
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

        public static bool operator ==(Relationship a, Relationship b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.T == b.T && a.source == b.source && a.relType == b.relType)
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
        public ClsType clauseType;
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