//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    //these are used so that Children can be readOnly. This prevents programmers from accidentally doing a (e.g.) Child.Add() which will not handle reverse links properly
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
        public AppliesTo a;
        public Relationship clause;
        public ClauseType() { }
        public ClauseType(AppliesTo a1, Relationship clause1)
        {
            a = a1;
            clause = clause1;
        }

    };
    public enum AppliesTo { all, source, type, target, condition };

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
            //sentencetype = r.sentencetype;
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
            get { return s; }
            set
            {
                s = value;
            }
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

        //private SentenceType st = null;
        //public SentenceType sentencetype { get { return st; } set { this.st = value; } }

        public string label = "";
        public float weight = 1;
        public int hits = 0;
        public int misses = 0;
        public DateTime lastUsed = DateTime.Now;

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

        public Relationship(Relationship r)
        {
            count = r.count;
            misses = r.misses;
            relType = r.relType;
            source = r.source;
            weight = r.weight;
            hits = r.hits++;
            //sentencetype = r.sentencetype;
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
            get
            {
                int retVal = -1;
                return retVal;
            }
            set
            {
            }
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
                    case AppliesTo.source:
                        {
                            sourceModifierString += " " + c.clause;
                            break;
                        }
                    case AppliesTo.type:
                        {
                            typeModifierString += " " + c.clause;
                            break;
                        }
                    case AppliesTo.target:
                        {
                            targetModifierString += " " + c.clause;
                            break;
                        }
                    case AppliesTo.all:
                        {
                            targetModifierString += " " + c.clause;
                            break;
                        }
                    case AppliesTo.condition:
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
            foreach (Relationship r in t.RelationshipsWithoutChildren)
            {
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


        //This is the (temporary) algorithm calculating the weight based on hits or misses
        public float Value()
        {
            float retVal = weight;
            if (hits != 0 && misses != 0)
            {
                float denom = misses;
                if (denom == 0) denom = .1f;
                retVal = hits / denom;
                TimeSpan age = lastUsed - DateTime.Now;
                retVal = retVal / (float)age.TotalMilliseconds;
                //bloating the score so its more readable
                retVal = retVal * -100000;
            }
            return retVal;
        }

        public float Value1
        {
            get
            {
                float val = hits;
                if (float.IsNaN(val))
                    val = -1;
                return val;
            }
        }

        public float Confidence()
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

        public void UpdateRelationship(Relationship r)
        {
            count = r.count;
            misses = r.misses;
            relType = r.relType;
            source = r.source;
            weight = r.weight;
            hits = r.hits++;
            //sentencetype = r.sentencetype;
            target = r.target;
            inferred = r.inferred;
            clauses = r.clauses;
            clausesFrom = r.clausesFrom;
        }

        public Relationship AddClause(AppliesTo a, Relationship tClause)
        {
            ClauseType c = new ClauseType() { clause = tClause, a = a, };
            if (!clauses.Contains(c)) clauses.Add(c);
            if (!tClause.clausesFrom.Contains(this)) tClause.clausesFrom.Add(this);
            return this;
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
                    //hack to force the MM dialog to update to make things disappear
                    if (r.target?.Label.StartsWith("io") == true)
                    {
                        // ModuleMentalModel mm = (ModuleMentalModel)MainWindow.theNeuronArray.modules.Find(x => x.Label == "MentalModel")?.TheModule;
                        // if (mm != null)
                        //     mm.MentalModelChanged = true;
                    }
                    r.source.RemoveRelationship(r);
                    /*
                    //if this leaves an orphan thing, delete the thing
                    if (r.reltype.Label == "has-child" && r.target?.Parents.Count == 0)
                    {
                        ModuleBase uks = MainWindow.GetUKS();
                        if (uks != null)
                        {
                            uks.DeleteAllChildren(r.target);
                            uks.DeleteThing(r.target);
                        }
                    }
                    transientRelationships.Remove(r);
                    */
                }
            }
        }


    }


    //this is a non-pointer representation of a relationship needed for XML storage
    public class SClauseType
    {
        public AppliesTo a;
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
//        public object sentencetype;
        public List<SClauseType> clauses = new();
    }

}