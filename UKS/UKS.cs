namespace UKS;

using Pluralize.NET;


/// <summary>
/// Contains a collection of Things linked by Links to implement Common Sense and general knowledge.
/// </summary>
public partial class UKS
{

    //This is the actual internal Universal Knowledge Store
    static private List<Thought> uKSList = new() { Capacity = 1000000, };


    //This is a temporary copy of the UKS which used internally during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThing instead of Thought
    private List<sCogneme> UKSTemp = new();

    /// <summary>
    /// Occasionally a list of all the Things in the UKS is needed. This is READ ONLY.
    /// There is only one (shared) list for the App.
    /// </summary>
    public List<Thought> AllThings { get => uKSList; }

    //TimeToLive processing for links
    static public List<Thought> transientLinks = new List<Thought>();
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
            ThoughtLabels.ClearLabelList();
            CreateInitialStructure();
        }
        UKSTemp.Clear();

        var autoEvent = new AutoResetEvent(false);
        stateTimer = new Timer(RemoveExpiredLinks, autoEvent, 0, 1000);
    }

    static bool isRunning = false;
    private void RemoveExpiredLinks(Object stateInfo)
    {
        if (isRunning) return;
        isRunning = true;
        try
        {
            for (int i = transientLinks.Count - 1; i >= 0; i--)
            {
                Thought r = transientLinks[i];
                //check to see if the link has expired
                if (r.TimeToLive != TimeSpan.MaxValue && r.LastFiredTime + r.TimeToLive < DateTime.Now)
                {
                    r.From.RemoveLink(r);
                    //if this leaves an orphan thought, delete the thought
                    if (r.LinkType.Label == "has-child" && r.To?.Parents.Count == 0)
                    {
                        r.To.AddParent(ThoughtLabels.GetThing("Unknown"));
                    }
                    transientLinks.Remove(r);
                    //HACK
                    if (r.LinkType.Label == "has-child")
                    {
                        DeleteAllChildren(r.To);
                        DeleteThing(r.To);
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
    public virtual Thought AddThing(string label, Thought? parent)
    {
        Thought newThing = new();
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
    /// This is a primitive method to Delete a Thought...the Thought must not have any children
    /// </summary>
    /// <param name="t">The Thought to delete</param>
    public virtual void DeleteThing(Thought t)
    {
        if (t is null) return;
        //if (t.Children.Count != 0)
        //    return; //can't delete something with children...must delete all children first.
        foreach (Thought r in t.LinksTo)
            t.RemoveLink(r);
        foreach (Thought r in t.LinksFrom)
            r.From.RemoveLink(r);
        ThoughtLabels.RemoveThingLabel(t.Label);
        lock (AllThings)
            AllThings.Remove(t);
    }

    /// <summary>
    /// Uses a hash table to return the Thought with the given label or null if it does not exist
    /// </summary>
    /// <param name="label"></param>
    /// <returns>The Thought or null</returns>
    public Thought Labeled(string label)
    {
        Thought retVal = ThoughtLabels.GetThing(label);
        return retVal;
    }

    public bool ThingInTree(Thought t1, Thought t2)
    {
        if (t2 is null) return false;
        if (t1 is null) return false;
        if (t1 == t2) return true;
        if (t1.AncestorList().Contains(t2)) return true;
        if (t2.AncestorList().Contains(t1)) return true;
        return false;
    }
    List<Thought> GetTransitiveTargetChain(Thought t, Thought relType, List<Thought> results = null)
    {
        if (results is null) results = new();
        List<Thought> targets = LinkTree(t, relType);
        foreach (Thought r in targets)
            if (r.LinkType == relType)
            {
                if (!results.Contains(r.To))
                {
                    results.Add(r.To);
                    results.AddRange(r.To.Descendents);
                    GetTransitiveTargetChain(r.To, r.LinkType, results);
                }
            }
        return results;
    }
    List<Thought> LinkTree(Thought t, Thought relType)
    {
        List<Thought> results = new();
        results.AddRange(t.LinksTo.FindAll(x => x.LinkType == relType));
        foreach (Thought t1 in t.Ancestors)
            results.AddRange(t1.LinksTo.FindAll(x => x.LinkType == relType));
        foreach (Thought t1 in t.Descendents)
            results.AddRange(t1.LinksTo.FindAll(x => x.LinkType == relType));
        return results;
    }
    List<Thought> GetTransitiveSourceChain(Thought t, Thought relType, List<Thought> results = null)
    {
        if (results is null) results = new();
        List<Thought> targets = LinksByTree(t, relType);
        foreach (Thought r in targets)
            if (r.LinkType == relType)
            {
                if (!results.Contains(r.From))
                {
                    results.Add(r.From);
                    //results.AddRange(r.source.Ancestors);
                    GetTransitiveSourceChain(r.From, r.LinkType, results);
                }
            }
        return results;
    }
    List<Thought> LinksByTree(Thought t, Thought relType)
    {
        List<Thought> results = new();
        if (t is null) return results;
        results.AddRange(t.LinksFrom.FindAll(x => x.LinkType == relType));
        foreach (Thought t1 in t.Ancestors)
            results.AddRange(t1.LinksFrom.FindAll(x => x.LinkType == relType));
        foreach (Thought t1 in t.Descendents)
            results.AddRange(t1.LinksFrom.FindAll(x => x.LinkType == relType));
        return results;
    }

    private bool LinksAreExclusive(Thought r1, Thought r2)
    {
        //are two links mutually exclusive?
        //yes if they differ by a single component property
        //   which is exclusive on a property
        //      which source and target are the ancestor of one another

        //TODO:  expand this to handle
        //  is lessthan is greaterthan
        //  several other cases

        if (r1.To != r2.To && (r1.To is null || r2.To is null)) return false;
        if (r1.To == r2.To && r1.LinkType == r2.LinkType) return false;
        //TODO Verify this:
        if (r1.HasProperty("isResult")) return false;
        if (r1.HasProperty("isCondition")) return false;
        if (r2.HasProperty("isResult")) return false;
        if (r2.HasProperty("isCondition")) return false;

        if (r1.From == r2.From ||
            r1.From.AncestorList().Contains(r2.From) ||
            r2.From.AncestorList().Contains(r1.From) ||
            FindCommonParents(r1.From, r1.From).Count() > 0)
        {

            IReadOnlyList<Thought> r1RelProps = r1.LinkType.GetAttributes();
            IReadOnlyList<Thought> r2RelProps = r2.LinkType.GetAttributes();
            //handle case with properties of the target
            if (r1.To is not null && r1.To == r2.To &&
                (r1.To.AncestorList().Contains(r2.To) ||
                r2.To.AncestorList().Contains(r1.To) ||
                FindCommonParents(r1.To, r1.To).Count() > 0))
            {
                IReadOnlyList<Thought> r1TargetProps = r1.To.GetAttributes();
                IReadOnlyList<Thought> r2TargetProps = r2.To.GetAttributes();
                foreach (Thought t1 in r1TargetProps)
                    foreach (Thought t2 in r2TargetProps)
                    {
                        List<Thought> commonParents = FindCommonParents(t1, t2);
                        foreach (Thought t3 in commonParents)
                        {
                            if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                                return true;
                        }
                    }
            }
            //handle case with conflicting targets
            if (r1.To is not null && r2.To is not null)
            {
                List<Thought> commonParents = FindCommonParents(r1.To, r2.To);
                foreach (Thought t3 in commonParents)
                {
                    if (HasProperty(t3, "isexclusive") || HasProperty(t3, "allowMultiple"))
                        return true;
                }
            }
            if (r1.To == r2.To)
            {
                foreach (Thought t1 in r1RelProps)
                    foreach (Thought t2 in r2RelProps)
                    {
                        if (t1 == t2) continue;
                        List<Thought> commonParents = FindCommonParents(t1, t2);
                        foreach (Thought t3 in commonParents)
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
            if (r1.To == r2.To &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the reltypes contains negation and not the other
            Thought r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thought r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            if ((r1.From.Ancestors.Contains(r2.From) ||
                r2.From.Ancestors.Contains(r1.From)) &&
                r1.To == r2.To &&
                (r1Not is null && r2Not is not null || r1Not is not null && r2Not is null))
                return true;
        }
        else
        {
            //this appears to duplicate code at line 226
            List<Thought> commonParents = FindCommonParents(r1.To, r2.To);
            foreach (Thought t3 in commonParents)
            {
                if (HasProperty(t3, "isexclusive"))
                    return true;
                if (HasProperty(t3, "allowMultiple") && r1.From != r2.From)
                    return true;
            }

        }
        return false;
    }

    private bool LinkTypesAreExclusive(Thought r1, Thought r2)
    {
        IReadOnlyList<Thought> r1RelProps = r1.LinkType.GetAttributes();
        IReadOnlyList<Thought> r2RelProps = r2.LinkType.GetAttributes();
        Thought r1Not = r1RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        Thought r2Not = r2RelProps.FindFirst(x => x.Label == "not" || x.Label == "no");
        if (r1.To == r2.To &&
            (r1Not is null && r2Not is not null || r1Not is not null && r2Not is null))
            return true;
        return false;
    }

    private bool HasAttribute(Thought t, string name)
    {
        if (t is null) return false;
        foreach (Thought r in t.LinksTo)
        {
            if (r.LinkType is not null && r.LinkType.Label == "is" && r.To.Label == name)
                return true;
        }
        return false;
    }

    bool HasProperty(Thought t, string propertyName)
    {
        if (t is null) return false;
        var v = t.LinksTo;
        if (v.FindFirst(x => x.To?.Label.ToLower() == propertyName.ToLower() && x.LinkType.Label == "hasProperty") is not null) return true;
        return false;
    }

    bool LinksAreEqualIgnoringLabels(Thought r1, Thought r2, bool ignoreSource = true)
    {
        if (
            (r1.From == r2.From || ignoreSource) &&
            r1.To == r2.To &&
            r1.LinkType == r2.LinkType
          ) return true;
        //special case if these contain other links
        if (r1.From is Thought rt1 && r2.From is Thought rt2)
        {
            if (!LinksAreEqual(rt1, rt2)) return false;
            if (r1.To is Thought rt3 && r2.To is Thought rt4)
                if (!LinksAreEqual(rt3, rt4)) return false;
            if (r1.LinkType != r2.LinkType) return false;
            return true;
        }
        return false;
    }


    bool LinksAreEqual(Thought r1, Thought r2, bool ignoreSource = true)
    {
        if (
            r1.Label == r2.Label &&
            (r1.From == r2.From || ignoreSource) &&
            r1.To == r2.To &&
            r1.LinkType == r2.LinkType
          ) return true;
        //special case if these contain other links
        if (r1.From is Thought rt1 && r2.From is Thought rt2)
        {
            if (!LinksAreEqual(rt1, rt2)) return false;
            if (r1.To is Thought rt3 && r2.To is Thought rt4)
                if (!LinksAreEqual(rt3, rt4)) return false;
            if (r1.LinkType != r2.LinkType) return false;
            return true;
        }
        return false;
    }

    public Thought GetLink(Thought source, Thought relType, Thought target)
    {
        if (source is null) return null;
        //create a temporary link
        Thought r = new() { From = source, LinkType = relType, To = target };
        //see if it already exists
        return GetLink(r);
    }
    public Thought GetLink(Thought r)
    {
        foreach (Thought r1 in r.From?.LinksTo)
        {
            if (LinksAreEqual(r, r1)) return r1;
        }
        return null;
    }

    private Thought ThingFromString(string label, string defaultParent, Thought source = null)
    {
        if (string.IsNullOrEmpty(label)) return null;
        if (label == "") return null;
        Thought t = Labeled(label);

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
    private Thought ThingFromObject(object o, string parentLabel = "", Thought source = null)
    {
        if (parentLabel == "")
            parentLabel = "Unknown";
        if (o is string s3)
            return ThingFromString(s3.Trim(), parentLabel, source);
        else if (o is Thought t3)
            return t3;
        else if (o is null)
            return null;
        else
            return null;
    }

    /// <summary>
    /// Recursively removes all the descendants of a Thought. If these descendants have no other parents, they will be deleted as well
    /// </summary>
    /// <param name="t">The Thought to remove the children from</param>
    public void DeleteAllChildren(Thought t)
    {
        if (t is not null)
        {
            while (t.Children.Count > 0)
            {
                Thought theChild = t.Children[0];
                if (theChild.Parents.Count == 1)
                {
                    DeleteAllChildren(theChild);
                    if (t.Label == "Thought" && t.Children.Count == 0) return;
                    DeleteThing(theChild);
                }
                else
                {//this thought has multiple parents.
                    t.RemoveChild(theChild);
                }
            }
        }

    }

    // If a thought exists, return it.  If not, create it.
    // If it is currently an unknown, defining the parent can make it known
    /// <summary>
    /// Creates a new Thought in the UKS OR returns an existing Thought, based on the label
    /// </summary>
    /// <param name="label">The new label OR if it ends in an asterisk, the astrisk will be replaced by digits to create a new Thought with a unique label.</param>
    /// <param name="parent"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Thought GetOrAddThing(string label, object parent = null, Thought source = null)
    {
        Thought thingToReturn = null;

        if (string.IsNullOrEmpty(label)) return thingToReturn;

        thingToReturn = ThoughtLabels.GetThing(label);
        if (thingToReturn is not null) return thingToReturn;

        //. are used to indicate attributes to be added
        if (label.Contains(".") && label != "." && !label.Contains(".py"))
        {
            string[] attribs = label.Split(".");
            Thought baseThing = Labeled(attribs[0]);
            if (baseThing is null) baseThing = AddThing(attribs[0], "Unknown");
            Thought instanceThing = Labeled(label);
            if (instanceThing is null)
            {
                instanceThing = AddThing(label, baseThing);
            }
            for (int i = 1; i < attribs.Length; i++)
            {
                Thought attrib = Labeled(attribs[i]);
                if (attrib is null)
                    attrib = AddThing(attribs[i], "Unknown");
                instanceThing.AddLink(attrib, "is");
            }
            return instanceThing;
        }


        Thought correctParent = null;
        if (parent is string s)
            correctParent = ThoughtLabels.GetThing(s);
        if (parent is Thought t)
            correctParent = t;
        if (correctParent is null)
            correctParent = ThoughtLabels.GetThing("Unknown");

        if (correctParent is null) throw new ArgumentException("GetOrAddThing: could not find parent");

        if (label.EndsWith("*"))
        {
            string baseLabel = label.Substring(0, label.Length - 1);
            Thought newParent = ThoughtLabels.GetThing(baseLabel);
            //instead of creating a new label, see if the next label for this item already exists and can be reused
            if (source is not null)
            {
                int digit = 0;
                while (source.LinksTo.FindFirst(x => x.LinkType.Label == baseLabel + digit) is not null) digit++;
                Thought labeled = ThoughtLabels.GetThing(baseLabel + digit);
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
    /// Finds or creates a subclass.  "Has 4" becomes Thought{has.4} and has.4 is 4.
    /// </summary>
    /// <param name="label">The string to process</param>
    /// <param name="attributesFollow">Attributes follow or precede the main</param>
    /// <param name="singularize"></param>
    /// <returns></returns>
    public Thought CreateThingFromMultipleAttributes(string label, bool attributesFollow, bool singularize = true)
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

        Thought t = GetOrAddThing(thingLabel);
        return t;
    }
}
