namespace UKS;

using Pluralize.NET;


/// <summary>
/// Contains a collection of Things linked by Relationships to implement Common Sense and general knowledge.
/// </summary>
public partial class UKS
{

    //This is the actual internal Universal Knowledge Store
    static private List<Thing> uKSList = new() { Capacity = 1000000, };


    //This is a temporary copy of the UKS which used internally during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
    private List<SThing> UKSTemp = new();

    /// <summary>
    /// Occasionally a list of all the Things in the UKS is needed. This is READ ONLY.
    /// There is only one (shared) list for the App.
    /// </summary>
    public IList<Thing> AllThings { get => uKSList; }

    //TimeToLive processing for relationships
    static public List<Relationship> transientRelationships = new List<Relationship>();
    static Timer stateTimer;

    public static UKS theUKS = new UKS();

    /// <summary>
    /// Creates a new reference to the UKS and initializes it if it is the first reference. 
    /// </summary>
    public UKS(bool clear = false)
    {
        if (AllThings.Count == 0 || clear)
        {
            AllThings.Clear();
            ThingLabels.ClearLabelList();
            CreateInitialStructure();
        }
        UKSTemp.Clear();

        var autoEvent = new AutoResetEvent(false);
        stateTimer = new Timer(RemoveExpiredRelationships, autoEvent, 0, 1000);
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
                if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now)
                {
                    r.Source.RemoveRelationship(r);
                    //if this leaves an orphan thing, delete the thing
                    if (r.RelType.Label == "has-child" && r.Target?.Parents.Count == 0)
                    {
                        r.Target.AddParent(ThingLabels.GetThing("unknownObject"));
                    }
                    transientRelationships.Remove(r);
                    //HACK
                    if (r.RelType.Label == "has-child")
                    {
                        DeleteAllChildren(r.Target);
                        DeleteThing(r.Target);
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
    public virtual Thing AddThing(string label, Thing? parent)
    {
        Thing newThing = new();
        newThing.Label = label;
        if (parent is not null)
        {
            newThing.AddParent(parent);
        }
        lock (AllThings)
        {
            AllThings.Add(newThing);
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
            r.Source.RemoveRelationship(r);
        ThingLabels.RemoveThingLabel(t.Label);
        lock (AllThings)
            AllThings.Remove(t);

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
            if (r.RelType == relType)
            {
                if (!results.Contains(r.Target))
                {
                    results.Add(r.Target);
                    results.AddRange(r.Target.Descendents);
                    GetTransitiveTargetChain(r.Target, r.RelType, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        results.AddRange(t.Relationships.FindAll(x => x.RelType == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.Relationships.FindAll(x => x.RelType == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.Relationships.FindAll(x => x.RelType == relType));
        return results;
    }
    List<Thing> GetTransitiveSourceChain(Thing t, Thing relType, List<Thing> results = null)
    {
        if (results == null) results = new();
        List<Relationship> targets = RelationshipsByTree(t, relType);
        foreach (Relationship r in targets)
            if (r.RelType == relType)
            {
                if (!results.Contains(r.Source))
                {
                    results.Add(r.Source);
                    //results.AddRange(r.source.Ancestors);
                    GetTransitiveSourceChain(r.Source, r.RelType, results);
                }
            }
        return results;
    }
    List<Relationship> RelationshipsByTree(Thing t, Thing relType)
    {
        List<Relationship> results = new();
        if (t == null) return results;
        results.AddRange(t.RelationshipsFrom.FindAll(x => x.RelType == relType));
        foreach (Thing t1 in t.Ancestors)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.RelType == relType));
        foreach (Thing t1 in t.Descendents)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.RelType == relType));
        return results;
    }

    private bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
    {
        //are two relationships mutually exclusive?
        //yes if they differ by a single component property
        //   which is exclusive on a property
        //      which source and target are the ancestor of one another

        //TODO:  expand this to handle
        //  is lessthan is greaterthan
        //  several other cases

        if (r1.Target != r2.Target && (r1.Target == null || r2.Target == null)) return false;
        if (r1.Target == r2.Target && r1.RelType == r2.RelType) return false;
        //TODO Verify this:
        if (r1.HasProperty("isResult")) return false;
        if (r1.HasProperty("isCondition")) return false;
        if (r2.HasProperty("isResult")) return false;
        if (r2.HasProperty("isCondition")) return false;

        if (r1.Source == r2.Source ||
            r1.Source.AncestorList().Contains(r2.Source) ||
            r2.Source.AncestorList().Contains(r1.Source) ||
            FindCommonParents(r1.Source, r1.Source).Count() > 0)
        {

            IList<Thing> r1RelProps = r1.RelType.GetAttributes();
            IList<Thing> r2RelProps = r2.RelType.GetAttributes();
            //handle case with properties of the target
            if (r1.Target != null && r1.Target == r2.Target &&
                (r1.Target.AncestorList().Contains(r2.Target) ||
                r2.Target.AncestorList().Contains(r1.Target) ||
                FindCommonParents(r1.Target, r1.Target).Count() > 0))
            {
                IList<Thing> r1TargetProps = r1.Target.GetAttributes();
                IList<Thing> r2TargetProps = r2.Target.GetAttributes();
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
            if (r1.Target != null && r2.Target != null)
            {
                List<Thing> commonParents = FindCommonParents(r1.Target, r2.Target);
                foreach (Thing t3 in commonParents)
                {
                    if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                        return true;
                }
            }
            if (r1.Target == r2.Target)
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
            if (r1.Target == r2.Target &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the reltypes contains negation and not the other
            Thing r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thing r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            if ((r1.Source.Ancestors.Contains(r2.Source) ||
                r2.Source.Ancestors.Contains(r1.Source)) &&
                r1.Target == r2.Target &&
                (r1Not == null && r2Not != null || r1Not != null && r2Not == null))
                return true;
        }
        else
        {
            //this appears to duplicate code at line 226
            List<Thing> commonParents = FindCommonParents(r1.Target, r2.Target);
            foreach (Thing t3 in commonParents)
            {
                if (HasProperty(t3, "isexclusive"))
                    return true;
                if (HasProperty(t3, "allowMultiple") && r1.Source != r2.Source)
                    return true;
            }

        }
        return false;
    }

    private bool RelationshipTypesAreExclusive(Relationship r1, Relationship r2)
    {
        IList<Thing> r1RelProps = r1.RelType.GetAttributes();
        IList<Thing> r2RelProps = r2.RelType.GetAttributes();
        Thing r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        Thing r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        if (r1.Target == r2.Target &&
            (r1Not == null && r2Not != null || r1Not != null && r2Not == null))
            return true;
        return false;
    }

    private bool HasAttribute(Thing t, string name)
    {
        if (t == null) return false;
        foreach (Relationship r in t.Relationships)
        {
            if (r.RelType != null && r.RelType.Label == "is" && r.Target.Label == name)
                return true;
        }
        return false;
    }

    bool HasProperty(Thing t, string propertyName)
    {
        if (t == null) return false;
        var v = t.Relationships;
        if (v.FindFirst(x => x.Target?.Label.ToLower() == propertyName.ToLower() && x.RelType.Label == "hasProperty") != null) return true;
        return false;
    }

    bool RelationshipsAreEqual(Relationship r1, Relationship r2, bool ignoreSource = true)
    {
        //special case if these contain other relationships
        if (r1.Source is Relationship rt1 && r2.Source is Relationship rt2)
        {
            if (!RelationshipsAreEqual(rt1, rt2)) return false;
            if (r1.Target is Relationship rt3 && r2.Target is Relationship rt4)
                if (!RelationshipsAreEqual(rt3, rt4)) return false;
            if (r1.RelType != r2.RelType) return false;
            return true;
        }
        if (
            (r1.Source == r2.Source || ignoreSource) &&
            r1.Target == r2.Target &&
            r1.RelType == r2.RelType
          ) return true;
        return false;
    }

    public Relationship GetRelationship(Thing source, Thing relType, Thing target)
    {
        if (source == null) return null;
        //create a temporary relationship
        Relationship r = new() { Source = source, RelType = relType, Target = target };
        //see if it already exists
        return GetRelationship(r);
    }
    public Relationship GetRelationship(Relationship r)
    {
        foreach (Relationship r1 in r.Source.Relationships)
        {
            if (RelationshipsAreEqual(r, r1)) return r1;
        }
        return null;
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

        //. are used to indicate attributes to be added
        if (label.Contains(".") && label != "." && !label.Contains(".py"))
        {
            string[] attribs = label.Split(".");
            Thing baseThing = Labeled(attribs[0]);
            if (baseThing == null) baseThing = AddThing(attribs[0], "unknownObject");
            Thing instanceThing = Labeled(label);
            if (instanceThing == null)
            {
                instanceThing = AddThing(label, baseThing);
            }
            for (int i = 1; i < attribs.Length; i++)
            {
                Thing attrib = Labeled(attribs[i]);
                if (attrib == null)
                    attrib = AddThing(attribs[i], "unknownObject");
                instanceThing.AddRelationship(attrib, "is");
            }
            return instanceThing;
        }


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
                while (source.Relationships.FindFirst(x => x.RelType.Label == baseLabel + digit) != null) digit++;
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


    /*Restrictions on Node Names:
     * must be unique
     * cannot be empty
     * cannot include ' ' (use a - instead)
     * cannot include '.' this is the flag for creating a subclass with following attributes
     * cannot include '*' this is the flag for auto-increment the label
     * case insensitive but initial input case is preserved for display
     * capitalized labels are never signularized even if "singularize=true"
    */


    /// <summary>
    /// Finds or creates a subclass.  "Has 4" becomes Thing{has.4} and has.4 is 4.
    /// </summary>
    /// <param name="label">The string to process</param>
    /// <param name="attributesFollow">Attributes follow or precede the main</param>
    /// <param name="singularize"></param>
    /// <returns></returns>
    public Thing CreateThingFromMultipleAttributes(string label, bool attributesFollow, bool singularize = true)
    {
        IPluralize pluralizer = new Pluralizer();
        label = label.Trim();
        string[] tempStringArray = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tempStringArray.Length == 0 || tempStringArray[0].Length == 0) return null;

        for (int i = 0; i < tempStringArray.Length; i++)
            if (!char.IsUpper(tempStringArray[i][0]) && singularize)
                tempStringArray[i] = pluralizer.Singularize(tempStringArray[i]);

        string thingLabel;
        if (attributesFollow)
        {
            thingLabel = tempStringArray[0];
            for (int i = 1; i < tempStringArray.Length; i++)
                if (!string.IsNullOrEmpty(tempStringArray[i]))
                    thingLabel += "." + tempStringArray[i];
        }
        else
        {
            int last = tempStringArray.Length - 1;
            thingLabel = tempStringArray[last];
            for (int i = 0; i < last; i++)
                if (!string.IsNullOrEmpty(tempStringArray[i]))
                    thingLabel += "." + tempStringArray[i];
        }

        Thing t = GetOrAddThing(thingLabel);
        return t;
    }



    public Relationship AddClause(Relationship rBase, Thing clauseType, Relationship rClause)
    {
        //rNew is an orhpan...not a real linked-up relationship, yet
        Relationship rNew = new() { Source = rBase, RelType = clauseType, Target = rClause, Weight = .9f };

        //does this relation/clause already exist?
        var r = GetRelationship(rNew);
        if (r != null) return r;

        rBase.AddRelationship(rNew.Target, rNew.RelType);

        if (clauseType.Label == "IF")
        {
            rBase.AddRelationship("isResult", "hasProperty");
            rClause.AddRelationship("isCondition", "hasProperty");

            //THIS needs to be fixed
            if (rBase.Label == "") rBase.AddToUKS();
            if (rClause.Label == "") rClause.AddToUKS();
            if (rNew.Label == "") rNew.AddToUKS();
        }
        return rNew;
    }
}
