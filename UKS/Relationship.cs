//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;

namespace UKS;

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
    public static int FindIndex<T>(this IList<T> source, Func<T, bool> condition)
    {
        for (int i = 0; i < source.Count; i++)
        {
            T item = source[i];
            if (condition(item))
                return i;
        }
        return -1;
    }
}

/// <summary>
/// In the same way a Relationship relates 2 Things (the "source" and the "target") with a relationship type, a Clause relates two Relationsips 
/// with a clauseType. Every Relationship has a list of clauses with the Relationship representing source and the Clause containing its type and target.
/// </summary>
public class Clause
{
    /// <summary>
    /// The type of dependency between two clauses
    /// </summary>
    public Thing clauseType;
    /// <summary>
    /// The target Relationship. The "Source" is the owner of the list of clauses
    /// </summary>
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
        reltype = r.reltype;
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
    public Thing s = null;
    /// <summary>
    /// the Relationship Source
    /// </summary>
    public Thing source
    {
        get => s;
        set { s = value; }
    }
    public Thing reltype = null;
    /// <summary>
    /// The Relationship Type
    /// </summary>
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
        get { /*Hits++; lastUsed = DateTime.Now;*/ return targ; }
        set
        {
            targ = value;
        }
    }

    private List<Clause> clauses = new();
    /// <summary>
    /// List of Clauses for which this is the Source Relationship
    /// </summary>
    public List<Clause> Clauses { get => clauses; set => clauses = value; }
    /// <summary>
    /// The list of Clauses for which this is the Target Relatiosnship
    /// </summary>
    public List<Relationship> clausesFrom = new();

    private float weight = 1;
    public float Weight
    {
        get
        {
            return weight;
        }
        set
        {
            weight = value;
            //if this is a commutative relationship, also set the weight on the reverse
            if (relType.HasProperty("IsCommutative"))
            {
                Relationship rReverse = target.Relationships.FindFirst(x => x.reltype == relType && x.target == source);
                if (rReverse != null)
                {
                    rReverse.weight = weight;
                }
            }
        }
    }

    private int hits = 0;
    private int misses = 0;
    /// <summary>
    /// Used internally to calculate the Weight
    /// </summary>
    public int Hits { get => hits; set => hits = value; }
    /// <summary>
    /// Used internally to calculate the Weight
    /// </summary>
    public int Misses { get => misses; set => misses = value; }

    private DateTime lastUsed = DateTime.Now;
    /// <summary>
    /// Time when this Relationship was last accessed at the result of a query. This is used
    /// to help determine the importance of this relationship
    /// </summary>
    public DateTime LastUsed { get => lastUsed; set => lastUsed = value; }
    /// <summary>
    /// Time when this relationship was created.
    /// </summary>
    public DateTime created = DateTime.Now;

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
    public bool GPTVerified = false;

    public Relationship()
    { }

    public void Fire()
    {
        lastUsed = DateTime.Now;
    }
    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="r"></param>
    public Relationship(Relationship r)
    {
        count = r.count;
        Misses = r.Misses;
        relType = r.relType;
        source = r.source;
        Hits = r.Hits++;
        targ = r.targ;
        Weight = r.Weight;
        if (r.Clauses == null) Clauses = new();
        else Clauses = new(r.Clauses);
        if (r.clausesFrom == null) clausesFrom = new();
        else clausesFrom = new(r.clausesFrom);
    }

    public void ClearHits()
    {
        Hits = 0;
    }
    public void ClearAccessCount()
    {
        Misses = 0;
    }

    public int count
    {
        get => -1;
        set { }
    }
    /// <summary>
    /// Add a clusse to this Relationship
    /// </summary>
    /// <param name="clauseType"></param>
    /// <param name="r2"></param>
    /// <returns></returns>
    public Relationship AddClause(Thing clauseType, Relationship r2)
    {
        Clause theClause = new();

        theClause.clause = r2;
        theClause.clauseType = clauseType;
        if (Clauses.FindFirst(x => x.clauseType == theClause.clauseType && x.clause == r2) == null)
        {
            Clauses.Add(theClause);
            r2.clausesFrom.Add(this);
        }

        return this;
    }

    public string ToString(List<Relationship> stack)
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
            allModifierString += c.clauseType.Label + " " + c.clause.ToString(stack) + " ";

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


    public float Value
    {
        get
        {
            //need a way to track how confident we should be
            float retVal = Weight;
            if (Hits != 0 && Misses != 0)
            {
                //replace with more robust algorithm
                float denom = Misses;
                if (denom == 0) denom = .1f;
                retVal = Hits / denom;
            }
            return retVal;
        }
    }

    private void AddToTransientList()
    {
        if (!UKS.transientRelationships.Contains(this))
            UKS.transientRelationships.Add(this);
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
    public bool GPTVerified = false;
    public List<SClauseType> clauses = new();
}
