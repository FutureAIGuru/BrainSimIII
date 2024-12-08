
namespace UKS;

/// <summary>
/// Contains a collection of Things linked by Relationships to implement Common Sense and general knowledge.
/// </summary>
public partial class UKS
{
    //This is the actual internal Universal Knowledge Store
    static private List<Thing> uKSList = new() { Capacity = 1000000, };


    //This is a temporary copy of the UKS which used internally during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
    private  List<SThing> UKSTemp = new();

    /// <summary>
    /// Occasionally a list of all the Things in the UKS is needed. This is READ ONLY.
    /// There is only one (shared) list for the App.
    /// </summary>
    public IList<Thing> UKSList { get => uKSList;}

    //TimeToLive processing for relationships
    static public  List<Relationship> transientRelationships = new List<Relationship>();
    static Timer stateTimer;
    /// <summary>
    /// Creates a new reference to the UKS and initializes it if it is the first reference. 
    /// </summary>
    public UKS()
    {
        //UKSList.Clear();
        //ThingLabels.ClearLabelList();

        if (UKSList.Count == 0)
        {
            UKSList.Clear();
            ThingLabels.ClearLabelList();
            CreateInitialStructure();
        }
        UKSTemp.Clear();

        var autoEvent = new AutoResetEvent(false);
        stateTimer = new Timer(RemoveExpiredRelationships,autoEvent,0, 10000);
    }

    static bool isRunning = false;
    private void RemoveExpiredRelationships(Object stateInfo)
    {
        if (isRunning) return;
        isRunning = true;
        try
        {
            for (int i = transientRelationships.Count - 1; i >= 0; i--)
            {
                Relationship r = transientRelationships[i];
                //check to see if the relationship has expired
                if (r.TimeToLive != TimeSpan.MaxValue && r.LastUsed + r.TimeToLive < DateTime.Now)
                {
                    r.source.RemoveRelationship(r);
                    //if this leaves an orphan thing, delete the thing
                    if (r.reltype.Label == "has-child" && r.target?.Parents.Count == 0)
                    {
                        r.target.AddParent(ThingLabels.GetThing("unknownObject"));
                    }
                    transientRelationships.Remove(r);
                    //HACK
                    if (r.reltype.Label == "has-child")
                    {
                        DeleteAllChildren(r.target);
                        DeleteThing(r.target);
                    }
                }
            }
        }
        finally
        {
            isRunning = false;
        }
    }

    /// <summary>
    /// This is a primitive method needed only to create ROOT Things which have no parents
    /// </summary>
    /// <param name="label"></param>
    /// <param name="parent">May be null</param>
    /// <returns></returns>
    public virtual Thing AddThing(string label, Thing parent)
    {
        Thing newThing = new();
        newThing.Label = label;
        if (parent is not null)
        {
            newThing.AddParent(parent);
        }
        lock (UKSList)
        {
            UKSList.Add(newThing);
        }
        return newThing;
    }

    /// <summary>
    /// This is a primitive method to Delete a Thing...the Thing must not have any children
    /// </summary>
    /// <param name="t">The Thing to delete</param>
    public virtual void DeleteThing(Thing t)
    {
        if (t == null) return;
        //if (t.Children.Count != 0)
        //    return; //can't delete something with children...must delete all children first.
        foreach (Relationship r in t.Relationships)
            t.RemoveRelationship(r);
        foreach (Relationship r in t.RelationshipsFrom)
            r.source.RemoveRelationship(r);
        ThingLabels.RemoveThingLabel(t.Label);
        lock (UKSList)
            UKSList.Remove(t);

    }

    /// <summary>
    /// Uses a hash table to return the Thing with the given label or null if it does not exist
    /// </summary>
    /// <param name="label"></param>
    /// <returns>The Thing or null</returns>
    public Thing Labeled(string label)
    {
        Thing retVal = ThingLabels.GetThing(label);
        return retVal;
    }

    public bool ThingInTree(Thing t1, Thing t2)
    {
        if (t2 == null) return false;
        if (t1 == null) return false;
        if (t1 == t2) return true;
        if (t1.AncestorList().Contains(t2)) return true;
        if (t2.AncestorList().Contains(t1)) return true;
        return false;
    }
    List<Thing> GetTransitiveTargetChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipTree(t, relType);
        foreach (Relationship r in targets)
            if (r.reltype == relType)
            {
                if (!results.Contains(r.target))
                {
                    results.Add(r.target);
                    results.AddRange(r.target.Descendents);
                    GetTransitiveTargetChain(r.target, r.reltype, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        results.AddRange(t.Relationships.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));
        return results;
    }
    List<Thing> GetTransitiveSourceChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipsByTree(t, relType);
        foreach (Relationship r in targets)
            if (r.reltype == relType)
            {
                if (!results.Contains(r.source))
                {
                    results.Add(r.source);
                    //results.AddRange(r.source.Ancestors);
                    GetTransitiveSourceChain(r.source, r.reltype, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipsByTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        if (t == null) return results;
        results.AddRange(t.RelationshipsFrom.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.reltype == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.reltype == relType));
        return results;
    }

    private  bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
    {
        //are two relationships mutually exclusive?
        //yes if they differ by a single component property
        //   which is exclusive on a property
        //      which source and target are the ancestor of one another

        //TODO:  expand this to handle
        //  is lessthan is greaterthan
        //  several other cases

        if (r1.target != r2.target && (r1.target == null || r2.target == null)) return false;
        //if (r1.target == r2.target && r1.source != r2.source) return false;

        if (r1.source == r2.source ||
            r1.source.AncestorList().Contains(r2.source) ||
            r2.source.AncestorList().Contains(r1.source) ||
            FindCommonParents(r1.source, r1.source).Count() > 0)
        {

            IList<Thing> r1RelProps = r1.reltype.GetAttributes();
            IList<Thing> r2RelProps = r2.reltype.GetAttributes();
            //handle case with properties of the target
            if (r1.target != null && r1.target == r2.target &&
                (r1.target.AncestorList().Contains(r2.target) ||
                r2.target.AncestorList().Contains(r1.target) ||
                FindCommonParents(r1.target, r1.target).Count() > 0))
            {
                IList<Thing> r1TargetProps = r1.target.GetAttributes();
                IList<Thing> r2TargetProps = r2.target.GetAttributes();
                foreach (Thing t1 in r1TargetProps)
                    foreach (Thing t2 in r2TargetProps)
                    {
                        List<Thing> commonParents = FindCommonParents(t1, t2);
                        foreach (Thing t3 in commonParents)
                        {
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //handle case with conflicting targets
            if (r1.target != null && r2.target != null)
            {
                List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
                foreach (Thing t3 in commonParents)
                {
                    if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                        return true;
                }
            }
            if (r1.target == r2.target)
            {
                foreach (Thing t1 in r1RelProps)
                    foreach (Thing t2 in r2RelProps)
                    {
                        if (t1 == t2) continue;
                        List<Thing> commonParents = FindCommonParents(t1, t2);
                        foreach (Thing t3 in commonParents)
                        {
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //if source and target are the same and one contains a number, assume that the other contains "1"
            // fido has leg -> fido has 1 leg  
            bool hasNumber1 = (r1RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            bool hasNumber2 = (r2RelProps.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            if (r1.target == r2.target &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the reltypes contains negation and not the other
            Thing r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thing r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            if ((r1.source.Ancestors.Contains(r2.source) ||
                r2.source.Ancestors.Contains(r1.source)) &&
                r1.target == r2.target &&
                (r1Not == null && r2Not != null || r1Not != null && r2Not == null))
                return true;
        }
        else
        {
            //this appears to duplicate code at line 226
            List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
            foreach (Thing t3 in commonParents)
            {
                if (HasProperty(t3, "isexclusive"))
                    return true;
                if (HasProperty(t3, "allowMultiple") && r1.source != r2.source)
                    return true;
            }

        }
        return false;
    }


    private bool HasAttribute(Thing t, string name)
    {
        if (t == null) return false;
        foreach (Relationship r in t.Relationships)
        {
            if (r.reltype != null && r.reltype.Label == "is" && r.target.Label == name)
                return true;
        }
        return false;
    }

    bool HasProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        var v = t.Relationships;
        if (v.FindFirst(x => x.target?.Label.ToLower() == propertyName.ToLower() && x.reltype.Label == "hasProperty") != null) return true;
        return false;
    }

    bool RelationshipsAreEqual(Relationship r1, Relationship r2, bool ignoreSource = true)
    {
        if (
            (r1.source == r2.source || ignoreSource) &&
            r1.target == r2.target &&
            r1.relType == r2.relType
          ) return true;
        return false;
    }

    public Relationship GetRelationship(Thing source, Thing relType, Thing target)
    {
        if (source == null) return null;
        //create a temporary relationship
        Relationship r = new() { source = source, relType = relType, target = target };
        //see if it already exists
        return GetRelationship(r);
    }
    public Relationship GetRelationship(Relationship r)
    {
        foreach (Relationship r1 in r.source.Relationships)
        {
            if (RelationshipsAreEqual (r,r1)) return r1;
        }
        return null;
    }


    private List<Thing> ThingListFromStringList(List<string> modifiers, string defaultParent)
    {
        if (modifiers == null) return null;
        List<Thing> retVal = new();
        foreach (string s in modifiers)
        {
            Thing t = ThingFromString(s, defaultParent);
            if (t != null) retVal.Add(t);
        }
        return retVal;
    }

    private Thing ThingFromString(string label, string defaultParent, Thing source = null)
    {
        if (string.IsNullOrEmpty(label)) return null;
        if (label == "") return null;
        Thing t = Labeled(label);

        if (t == null)
        {
            if (Labeled(defaultParent) == null)
            {
                GetOrAddThing(defaultParent, Labeled("Object"), source);
            }
            t = GetOrAddThing(label, defaultParent, source);
        }
        return t;
    }

    //temporarily public for testing
    private Thing ThingFromObject(object o, string parentLabel = "", Thing source = null)
    {
        if (parentLabel == "")
            parentLabel = "unknownObject";
        if (o is string s3)
            return ThingFromString(s3.Trim(), parentLabel, source);
        else if (o is Thing t3)
            return t3;
        else if (o is null)
            return null;
        else
            return null;
    }
    private List<Thing> ThingListFromString(string s, string parentLabel = "unknownObject")
    {
        List<Thing> retVal = new List<Thing>();
        string[] multiString = s.Split("|");
        foreach (string s1 in multiString)
        {
            Thing t = ThingFromString(s1.Trim(), parentLabel);
            if (t != null)
                retVal.Add(t);
        }
        return retVal;
    }
    //temporarily public for testing
    public List<Thing> ThingListFromObject(object o, string parentLabel = "unknownObject")
    {
        if (o is List<string> sl)
            return ThingListFromStringList(sl, parentLabel);
        else if (o is string s)
            return ThingListFromString(s, parentLabel);
        else if (o is Thing t)
            return new() { t };
        else if (o is List<Thing> tl)
            return tl;
        else if (o is null)
            return new();
        else
            throw new ArgumentException("invalid arg type in AddStatement: " + o.GetType());
    }




    /// <summary>
    /// Recursively removes all the descendants of a Thing. If these descendants have no other parents, they will be deleted as well
    /// </summary>
    /// <param name="t">The Thing to remove the children from</param>
    public void DeleteAllChildren(Thing t)
    {
        if (t is not null)
        {
            while (t.Children.Count > 0)
            {
                Thing theChild = t.Children[0];
                if (theChild.Parents.Count == 1)
                {
                    DeleteAllChildren(theChild);
                    if (t.Label == "Thing" && t.Children.Count == 0) return;
                    DeleteThing(theChild);
                }
                else
                {//this thing has multiple parents.
                    t.RemoveChild(theChild);
                }
            }
        }

    }

    // If a thing exists, return it.  If not, create it.
    // If it is currently an unknown, defining the parent can make it known
    /// <summary>
    /// Creates a new Thing in the UKS OR returns an existing Thing, based on the label
    /// </summary>
    /// <param name="label">The new label OR if it ends in an asterisk, the astrisk will be replaced by digits to create a new Thing with a unique label.</param>
    /// <param name="parent"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Thing GetOrAddThing(string label, object parent = null, Thing source = null)
    {
        Thing thingToReturn = null;

        if (string.IsNullOrEmpty(label)) return thingToReturn;

        thingToReturn = ThingLabels.GetThing(label);
        if (thingToReturn != null) return thingToReturn;

        Thing correctParent = null;
        if (parent is string s)
            correctParent = ThingLabels.GetThing(s);
        if (parent is Thing t)
            correctParent = t;
        if (correctParent == null)
            correctParent = ThingLabels.GetThing("unknownObject");

        if (correctParent is null) throw new ArgumentException("GetOrAddThing: could not find parent");

        if (label.EndsWith("*"))
        {
            string baseLabel = label.Substring(0, label.Length - 1);
            Thing newParent = ThingLabels.GetThing(baseLabel);
            //instead of creating a new label, see if the next label for this item already exists and can be reused
            if (source != null)
            {
                int digit = 0;
                while (source.Relationships.FindFirst(x => x.reltype.Label == baseLabel + digit) != null) digit++;
                Thing labeled = ThingLabels.GetThing(baseLabel + digit);
                if (labeled != null)
                    return labeled;
            }
            if (newParent == null)
                newParent = AddThing(baseLabel, correctParent);
            correctParent = newParent;
        }

        thingToReturn = AddThing(label, correctParent);
        return thingToReturn;
    }

}
