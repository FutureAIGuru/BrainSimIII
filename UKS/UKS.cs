namespace UKS;

using Pluralize.NET;


/// <summary>
/// Contains a collection of Things linked by Relationships to implement Common Sense and general knowledge.
/// </summary>
public partial class UKS
{

    //This is the actual internal Universal Knowledge Store
    static private List<Cogneme> uKSList = new() { Capacity = 1000000, };


    //This is a temporary copy of the UKS which used internally during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thing
    private List<sCogneme> UKSTemp = new();

    /// <summary>
    /// Occasionally a list of all the Things in the UKS is needed. This is READ ONLY.
    /// There is only one (shared) list for the App.
    /// </summary>
    public List<Cogneme> AllThings { get => uKSList; }

    //TimeToLive processing for relationships
    static public List<Cogneme> transientRelationships = new List<Cogneme>();
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
            CognemeLabels.ClearLabelList();
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
                Cogneme r = transientRelationships[i];
                //check to see if the relationship has expired
                if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now)
                {
                    r.Source.RemoveRelationship(r);
                    //if this leaves an orphan thing, delete the thing
                    if (r.RelType.Label == "has-child" && r.Target?.Parents.Count == 0)
                    {
                        r.Target.AddParent(CognemeLabels.GetThing("Unknown"));
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
    public virtual Cogneme AddThing(string label, Cogneme? parent)
    {
        Cogneme newThing = new();
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
    public virtual void DeleteThing(Cogneme t)
    {
        if (t is null) return;
        //if (t.Children.Count != 0)
        //    return; //can't delete something with children...must delete all children first.
        foreach (Cogneme r in t.Relationships)
            t.RemoveRelationship(r);
        foreach (Cogneme r in t.RelationshipsFrom)
            r.Source.RemoveRelationship(r);
        CognemeLabels.RemoveThingLabel(t.Label);
        lock (AllThings)
            AllThings.Remove(t);
    }

    /// <summary>
    /// Uses a hash table to return the Thing with the given label or null if it does not exist
    /// </summary>
    /// <param name="label"></param>
    /// <returns>The Thing or null</returns>
    public Cogneme Labeled(string label)
    {
        Cogneme retVal = CognemeLabels.GetThing(label);
        return retVal;
    }

    public bool ThingInTree(Cogneme t1, Cogneme t2)
    {
        if (t2 is null) return false;
        if (t1 is null) return false;
        if (t1 == t2) return true;
        if (t1.AncestorList().Contains(t2)) return true;
        if (t2.AncestorList().Contains(t1)) return true;
        return false;
    }
    List<Cogneme> GetTransitiveTargetChain(Cogneme t, Cogneme relType, List<Cogneme> results = null)
    {
        if (results is null) results = new();
        List<Cogneme> targets = RelationshipTree(t, relType);
        foreach (Cogneme r in targets)
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
    List<Cogneme> RelationshipTree(Cogneme t, Cogneme relType)
    {
        List<Cogneme> results = new();
        results.AddRange(t.Relationships.FindAll(x => x.RelType == relType));
        foreach (Cogneme t1 in t.Ancestors)
            results.AddRange(t1.Relationships.FindAll(x => x.RelType == relType));
        foreach (Cogneme t1 in t.Descendents)
            results.AddRange(t1.Relationships.FindAll(x => x.RelType == relType));
        return results;
    }
    List<Cogneme> GetTransitiveSourceChain(Cogneme t, Cogneme relType, List<Cogneme> results = null)
    {
        if (results is null) results = new();
        List<Cogneme> targets = RelationshipsByTree(t, relType);
        foreach (Cogneme r in targets)
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
    List<Cogneme> RelationshipsByTree(Cogneme t, Cogneme relType)
    {
        List<Cogneme> results = new();
        if (t is null) return results;
        results.AddRange(t.RelationshipsFrom.FindAll(x => x.RelType == relType));
        foreach (Cogneme t1 in t.Ancestors)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.RelType == relType));
        foreach (Cogneme t1 in t.Descendents)
            results.AddRange(t1.RelationshipsFrom.FindAll(x => x.RelType == relType));
        return results;
    }

    private bool RelationshipsAreExclusive(Cogneme r1, Cogneme r2)
    {
        //are two relationships mutually exclusive?
        //yes if they differ by a single component property
        //   which is exclusive on a property
        //      which source and target are the ancestor of one another

        //TODO:  expand this to handle
        //  is lessthan is greaterthan
        //  several other cases

        if (r1.Target != r2.Target && (r1.Target is null || r2.Target is null)) return false;
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

            IReadOnlyList<Cogneme> r1RelProps = r1.RelType.GetAttributes();
            IReadOnlyList<Cogneme> r2RelProps = r2.RelType.GetAttributes();
            //handle case with properties of the target
            if (r1.Target is not null && r1.Target == r2.Target &&
                (r1.Target.AncestorList().Contains(r2.Target) ||
                r2.Target.AncestorList().Contains(r1.Target) ||
                FindCommonParents(r1.Target, r1.Target).Count() > 0))
            {
                IReadOnlyList<Cogneme> r1TargetProps = r1.Target.GetAttributes();
                IReadOnlyList<Cogneme> r2TargetProps = r2.Target.GetAttributes();
                foreach (Cogneme t1 in r1TargetProps)
                    foreach (Cogneme t2 in r2TargetProps)
                    {
                        List<Cogneme> commonParents = FindCommonParents(t1, t2);
                        foreach (Cogneme t3 in commonParents)
                        {
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //handle case with conflicting targets
            if (r1.Target is not null && r2.Target is not null)
            {
                List<Cogneme> commonParents = FindCommonParents(r1.Target, r2.Target);
                foreach (Cogneme t3 in commonParents)
                {
                    if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                        return true;
                }
            }
            if (r1.Target == r2.Target)
            {
                foreach (Cogneme t1 in r1RelProps)
                    foreach (Cogneme t2 in r2RelProps)
                    {
                        if (t1 == t2) continue;
                        List<Cogneme> commonParents = FindCommonParents(t1, t2);
                        foreach (Cogneme t3 in commonParents)
                        {
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //if source and target are the same and one contains a number, assume that the other contains "1"
            // fido has leg -> fido has 1 leg  
            bool hasNumber1 = (r1RelProps.FindFirst(x => x.HasAncestorLabeled("number")) is not null);
            bool hasNumber2 = (r2RelProps.FindFirst(x => x.HasAncestorLabeled("number")) is not null);
            if (r1.Target == r2.Target &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the reltypes contains negation and not the other
            Cogneme r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Cogneme r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            if ((r1.Source.Ancestors.Contains(r2.Source) ||
                r2.Source.Ancestors.Contains(r1.Source)) &&
                r1.Target == r2.Target &&
                (r1Not is null && r2Not is not null || r1Not is not null && r2Not is null))
                return true;
        }
        else
        {
            //this appears to duplicate code at line 226
            List<Cogneme> commonParents = FindCommonParents(r1.Target, r2.Target);
            foreach (Cogneme t3 in commonParents)
            {
                if (HasProperty(t3, "isexclusive"))
                    return true;
                if (HasProperty(t3, "allowMultiple") && r1.Source != r2.Source)
                    return true;
            }

        }
        return false;
    }

    private bool RelationshipTypesAreExclusive(Cogneme r1, Cogneme r2)
    {
        IReadOnlyList<Cogneme> r1RelProps = r1.RelType.GetAttributes();
        IReadOnlyList<Cogneme> r2RelProps = r2.RelType.GetAttributes();
        Cogneme r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        Cogneme r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        if (r1.Target == r2.Target &&
            (r1Not is null && r2Not is not null || r1Not is not null && r2Not is null))
            return true;
        return false;
    }

    private bool HasAttribute(Cogneme t, string name)
    {
        if (t is null) return false;
        foreach (Cogneme r in t.Relationships)
        {
            if (r.RelType is not null && r.RelType.Label == "is" && r.Target.Label == name)
                return true;
        }
        return false;
    }

    bool HasProperty(Cogneme t, string propertyName)
    {
        if (t is null) return false;
        var v = t.Relationships;
        if (v.FindFirst(x => x.Target?.Label.ToLower() == propertyName.ToLower() && x.RelType.Label == "hasProperty") is not null) return true;
        return false;
    }

    bool RelationshipsAreEqualIgnoringLabels(Cogneme r1, Cogneme r2, bool ignoreSource = true)
    {
        if (
            (r1.Source == r2.Source || ignoreSource) &&
            r1.Target == r2.Target &&
            r1.RelType == r2.RelType
          ) return true;
        //special case if these contain other relationships
        if (r1.Source is Cogneme rt1 && r2.Source is Cogneme rt2)
        {
            if (!RelationshipsAreEqual(rt1, rt2)) return false;
            if (r1.Target is Cogneme rt3 && r2.Target is Cogneme rt4)
                if (!RelationshipsAreEqual(rt3, rt4)) return false;
            if (r1.RelType != r2.RelType) return false;
            return true;
        }
        return false;
    }


    bool RelationshipsAreEqual(Cogneme r1, Cogneme r2, bool ignoreSource = true)
    {
        if (
            r1.Label == r2.Label &&
            (r1.Source == r2.Source || ignoreSource) &&
            r1.Target == r2.Target &&
            r1.RelType == r2.RelType
          ) return true;
        //special case if these contain other relationships
        if (r1.Source is Cogneme rt1 && r2.Source is Cogneme rt2)
        {
            if (!RelationshipsAreEqual(rt1, rt2)) return false;
            if (r1.Target is Cogneme rt3 && r2.Target is Cogneme rt4)
                if (!RelationshipsAreEqual(rt3, rt4)) return false;
            if (r1.RelType != r2.RelType) return false;
            return true;
        }
        return false;
    }

    public Cogneme GetRelationship(Cogneme source, Cogneme relType, Cogneme target)
    {
        if (source is null) return null;
        //create a temporary relationship
        Cogneme r = new() { Source = source, RelType = relType, Target = target };
        //see if it already exists
        return GetRelationship(r);
    }
    public Cogneme GetRelationship(Cogneme r)
    {
        foreach (Cogneme r1 in r.Source?.Relationships)
        {
            if (RelationshipsAreEqual(r, r1)) return r1;
        }
        return null;
    }

    private Cogneme ThingFromString(string label, string defaultParent, Cogneme source = null)
    {
        if (string.IsNullOrEmpty(label)) return null;
        if (label == "") return null;
        Cogneme t = Labeled(label);

        if (t is null)
        {
            if (Labeled(defaultParent) is null)
            {
                GetOrAddThing(defaultParent, Labeled("Object"), source);
            }
            t = GetOrAddThing(label, defaultParent, source);
        }
        return t;
    }

    //temporarily public for testing
    private Cogneme ThingFromObject(object o, string parentLabel = "", Cogneme source = null)
    {
        if (parentLabel == "")
            parentLabel = "Unknown";
        if (o is string s3)
            return ThingFromString(s3.Trim(), parentLabel, source);
        else if (o is Cogneme t3)
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
    public void DeleteAllChildren(Cogneme t)
    {
        if (t is not null)
        {
            while (t.Children.Count > 0)
            {
                Cogneme theChild = t.Children[0];
                if (theChild.Parents.Count == 1)
                {
                    DeleteAllChildren(theChild);
                    if (t.Label == "Cogneme" && t.Children.Count == 0) return;
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
    public Cogneme GetOrAddThing(string label, object parent = null, Cogneme source = null)
    {
        Cogneme thingToReturn = null;

        if (string.IsNullOrEmpty(label)) return thingToReturn;

        thingToReturn = CognemeLabels.GetThing(label);
        if (thingToReturn is not null) return thingToReturn;

        //. are used to indicate attributes to be added
        if (label.Contains(".") && label != "." && !label.Contains(".py"))
        {
            string[] attribs = label.Split(".");
            Cogneme baseThing = Labeled(attribs[0]);
            if (baseThing is null) baseThing = AddThing(attribs[0], "Unknown");
            Cogneme instanceThing = Labeled(label);
            if (instanceThing is null)
            {
                instanceThing = AddThing(label, baseThing);
            }
            for (int i = 1; i < attribs.Length; i++)
            {
                Cogneme attrib = Labeled(attribs[i]);
                if (attrib is null)
                    attrib = AddThing(attribs[i], "Unknown");
                instanceThing.AddRelationship(attrib, "is");
            }
            return instanceThing;
        }


        Cogneme correctParent = null;
        if (parent is string s)
            correctParent = CognemeLabels.GetThing(s);
        if (parent is Cogneme t)
            correctParent = t;
        if (correctParent is null)
            correctParent = CognemeLabels.GetThing("Unknown");

        if (correctParent is null) throw new ArgumentException("GetOrAddThing: could not find parent");

        if (label.EndsWith("*"))
        {
            string baseLabel = label.Substring(0, label.Length - 1);
            Cogneme newParent = CognemeLabels.GetThing(baseLabel);
            //instead of creating a new label, see if the next label for this item already exists and can be reused
            if (source is not null)
            {
                int digit = 0;
                while (source.Relationships.FindFirst(x => x.RelType.Label == baseLabel + digit) is not null) digit++;
                Cogneme labeled = CognemeLabels.GetThing(baseLabel + digit);
                if (labeled is not null)
                    return labeled;
            }
            if (newParent is null)
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
    public Cogneme CreateThingFromMultipleAttributes(string label, bool attributesFollow, bool singularize = true)
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

        Cogneme t = GetOrAddThing(thingLabel);
        return t;
    }
}
