//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

namespace UKS;

//these are used so that relationship lists can be readOnly.
//This prevents programmers from accidentally doing a (e.g.) relationships.Add() which will not handle reverse links properly
//Why the IList doesn't have FindFirst, FindAll, and FindIndex is a ???
public static class IListExtensions
{
    public static T? FindFirst<T>(this IList<T> source, Func<T, bool> condition)
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
//public class Clause
//{
//    /// <summary>
//    /// The type of dependency between two clauses
//    /// </summary>
//    public Thing clauseType;
//    /// <summary>
//    /// The target Relationship. The "Source" is the owner of the list of clauses
//    /// </summary>
//    public Relationship clause;
//    public Clause(Thing theType, Relationship clause1)
//    {
//        clauseType = theType;
//        clause = clause1;
//    }
//};

///// <summary>
///// This is used internally during query processing
///// </summary>

//public class QueryRelationship : Relationship
//{
//    public List<Thing> typeProperties = new();
//    public List<Thing> sourceProperties = new();
//    public List<Thing> targetProperties = new();
//    public QueryRelationship() { }
//    public QueryRelationship(Relationship r)
//    {
//        source = r.source;
//        reltype = r.reltype;
//        target = r.target;
//        foreach (Clause c in r.Clauses)
//            Clauses.Add(c);
//    }
//}

/// <summary>
/// In the lexicon of graphs, a Relationship is an "edge".
/// A Relationship is a weighted link between two Things, the "source" and the "target". The Relationship has a type which is also a Thing. Various other
/// properties are used to track the usage of a Relationship which is used to help determine the most likely result of a query.
/// Each relationship also maintains a list of "Clause"s which are relationships to other Relationships. 
/// </summary>
public class Relationship : Thing
{

    //private List<Clause> clauses = new();
    /// <summary>
    /// List of Clauses for which this is the Source Relationship
    /// </summary>
    //public List<Clause> Clauses { get => clauses; set => clauses = value; }
    /// <summary>
    /// The list of Clauses for which this is the Target Relationship
    /// </summary>
    //public List<Relationship> clausesFrom = new();

    private float _weight = 1;
    public float Weight
    {
        get
        {
            return _weight;
        }
        set
        {
            _weight = value;
            //if this is a commutative relationship, also set the weight on the reverse
            if (RelType?.HasProperty("IsCommutative") == true)
            {
                Relationship rReverse = Target.Relationships.FindFirst(x => x.RelType == RelType && x.Target == Source);
                if (rReverse != null)
                {
                    rReverse._weight = _weight;
                }
            }
        }
    }

    //private int hits = 0;
    //private int misses = 0;
    ///// <summary>
    ///// Used internally to calculate the Weight
    ///// </summary>
    //public int Hits { get => hits; set => hits = value; }
    ///// <summary>
    ///// Used internally to calculate the Weight
    ///// </summary>
    //public int Misses { get => misses; set => misses = value; }


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
    //public bool GPTVerified = false;
    //public bool isStatement = true;

    public Relationship()
    {
    }

    public Relationship AddToUKS()
    {
        if (string.IsNullOrEmpty(this.Label))
            Label = "R*";
        this.AddParent("Relationship");
        this.Source.AddRelationship(this.Target, this.RelType);
        lock (UKS.theUKS.AllThings)
        {
            UKS.theUKS.AllThings.Add(this);
        }
        return this;
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="r"></param>
    public Relationship(Relationship r)
    {
        RelType = r.RelType;
        Source = r.Source;
        Target = r.Target;
        Weight = r.Weight;
        //if (r.Clauses == null) Clauses = new();
        //else Clauses = new(r.Clauses);
        //if (r.clausesFrom == null) clausesFrom = new();
        //else clausesFrom = new(r.clausesFrom);
    }

    //public void ClearHits()
    //{
    //    Hits = 0;
    //}
    //public void ClearAccessCount()
    //{
    //    Misses = 0;
    //}

    //public int count
    //{
    //    get => -1;
    //    set { }
    //}
    /// <summary>
    /// Add a clusse to this Relationship
    /// </summary>
    /// <param name="clauseType"></param>
    /// <param name="r2"></param>
    /// <returns></returns>
    //public Relationship AddClause(Thing clauseType, Relationship r2)
    //{
    //    Clause theClause = new(clauseType,r2);

    //    if (Clauses.FindFirst(x => x.clauseType == theClause.clauseType && x.clause == r2) == null)
    //    {
    //        Clauses.Add(theClause);
    //        r2.clausesFrom.Add(this);
    //    }

    //    return this;
    //}

    public string ToString(List<Relationship> stack)
    {
        if (stack.Contains(this))  //looping block protect from circular references
            return "";
        stack.Add(this);
        string retVal = "";

        retVal = BasicRelationshipToString(retVal);
        return retVal;
    }

    public override string ToString()
    {
        string retVal = this.ToString(new List<Relationship>());
        return retVal;
    }

    private string BasicRelationshipToString(string retVal)
    {
        //for convience show any value in singls quotes
        bool showBrackets = true;
        //var value = this.Relationships.FindFirst(x => x.RelType.Label == "VLU")?.Target;
        //if (value != null)
        //{
        //    retVal += "'" + value.ToString() + "'";
        //    showBrackets = false;
        //}
        //else
            retVal += Label;
        if (showBrackets) retVal += "[";
        if (Source != this && !string.IsNullOrEmpty(Source?.ToString()))
        {
            retVal += Source?.ToString();
        }
        if (!string.IsNullOrEmpty(RelType?.ToString()))
            retVal += ((retVal == "") ? "" : "->") + RelType?.ToString();
        if (!string.IsNullOrEmpty(Target?.ToString()))
        {
            retVal += ((retVal == "") ? "" : "->");
            retVal += Target?.ToString();
        }
        if (showBrackets) retVal += "]";
        return retVal;
    }


    public static bool operator ==(Relationship? a, Relationship? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        if (a.Target == b.Target && a.Source == b.Source && a.RelType == b.RelType)
            return true;
        return false;
    }
    //The following is needed to suppress a warning
    public override bool Equals(Object obj)
    {
        if (obj is Relationship a)
        {
            if (a.Target == Target &&
                a.Source == Source &&
                a.RelType == RelType &&
                a.Relationships.Count == Relationships.Count)
                return true;
        }
        return false;
    }

    public static bool operator !=(Relationship? a, Relationship? b)
    {
        if (a is null && b is null)
            return false;
        if (a is null || b is null)
            return true;
        if (a.Target == b.Target && a.Source == b.Source && a.RelType == b.RelType) return false;
        return true;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }


    private void AddToTransientList()
    {
        if (!UKS.transientRelationships.Contains(this))
            UKS.transientRelationships.Add(this);
    }


}