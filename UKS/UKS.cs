/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
namespace UKS;

using Pluralize.NET;
using System.Runtime.CompilerServices;


/// <summary>
/// Contains a collection of Thoughts linked by Links to implement Common Sense and general knowledge.
/// </summary>
public partial class UKS
{
    //This is the actual internal Universal Knowledge Store
    static private List<Thought> uKSList = new();// { Capacity = 1000000, };


    //This is a reformatted temporary copy of the UKS which used internally during the save and restore process to 
    //break circular links by storing index values instead of actual links Note the use of SThought instead of Thought
    private List<sThought> UKSTemp = new();

    /// <summary>
    /// Occasionally a list of all the Thoughts in the UKS is needed. This is READ ONLY.
    /// There is only one (shared) list for the App.
    /// </summary>
    public List<Thought> AllThoughts { get => uKSList; }

    //TimeToLive processing for links
    static public List<Thought> transientLinks = new List<Thought>();
    static Timer stateTimer;

    public static UKS theUKS = new UKS();

    /// <summary>
    /// Creates a new reference to the UKS and initializes it if it is the first reference. 
    /// </summary>
    public UKS(bool clear = false)
    {
        if (AllThoughts.Count == 0 || clear)
        {
            AllThoughts.Clear();
            ThoughtLabels.ClearLabelList();
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
                    r.To.RemoveLink(r);
                    //if this leaves an orphan thought, make it unknown
                    if (r.LinkType.Label == "is-a" && r.From?.Parents.Count == 0)
                    {
                        r.From.AddParent("Unknown");
                    }
                    transientLinks.Remove(r);
                }
            }
        }
        finally
        {
            isRunning = false;
        }
    }


    /// <summary>
    /// This is a primitive method needed only to create ROOT Thoughts which have no parents
    /// </summary>
    /// <param name="label"></param>
    /// <param name="parent">May be null</param>
    /// <returns></returns>
    public virtual Thought AddThought(string label, Thought? parent)
    {
        Thought newThought = new();
        newThought.Label = label;
        if (parent is not null)
        {
            newThought.AddParent(parent);
        }
        lock (AllThoughts)
        {
            AllThoughts.Add(newThought);
        }

        return newThought;
    }

    /// <summary>
    /// This is a primitive method to Delete a Thought...the Thought must not have any children
    /// </summary>
    /// <param name="t">The Thought to delete</param>
    public virtual void DeleteThought(Thought t)
    {
        if (t is null) return;

        foreach (Thought r in t.LinksTo.Where(x => IsSequenceFirstElement(x.To)))
            DeleteSequence(r.To);

        foreach (Thought r in t.LinksTo)
            t.RemoveLink(r);
        foreach (Thought r in t.LinksFrom)
            r.From.RemoveLink(r);
        ThoughtLabels.RemoveThoughtLabel(t.Label);
        lock (AllThoughts)
            AllThoughts.Remove(t);
    }

    /// <summary>
    /// Uses a hash table to return the Thought with the given label or null if it does not exist
    /// </summary>
    /// <param name="label"></param>
    /// <returns>The Thought or null</returns>
    public Thought Labeled(string label)
    {
        Thought retVal = ThoughtLabels.GetThought(label);
        return retVal;
    }

    public bool ThoughtInTree(Thought t1, Thought t2)
    {
        if (t2 is null) return false;
        if (t1 is null) return false;
        if (t1 == t2) return true;
        if (t1.AncestorList().Contains(t2)) return true;
        if (t2.AncestorList().Contains(t1)) return true;
        return false;
    }


    //TODO: This method has gotten out of hand and needs a rewrite
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

            IReadOnlyList<Thought> r1LinkiProps = r1.LinkType.GetAttributes();
            IReadOnlyList<Thought> r2LinkProps = r2.LinkType.GetAttributes();
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
                foreach (Thought t1 in r1LinkiProps)
                    foreach (Thought t2 in r2LinkProps)
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
            bool hasNumber1 = (r1LinkiProps.FindFirst(x => x.HasAncestor("number")) is not null);
            bool hasNumber2 = (r2LinkProps.FindFirst(x => x.HasAncestor("number")) is not null);
            if (r1.To == r2.To &&
                (hasNumber1 || hasNumber2))
                return true;

            //if one of the linkypes contains negation and not the other
            Thought r1Not = r1LinkiProps.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thought r2Not = r2LinkProps.FindFirst(x => x.Label == "not" || x.Label == "no");
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


    private bool LinksAreEqual(Thought r1, Thought r2, bool ignoreSource = true)
    {
        if (
            r1.Label == r2.Label &&
            (r1.From == r2.From || ignoreSource) &&
            (r1.To is null && r2.To is null || r1.To == r2.To) &&
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

    public Thought GetLink(Thought source, Thought linkType, Thought target)
    {
        if (source is null) return null;
        //create a temporary link
        Thought r = new() { From = source, LinkType = linkType, To = target };
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
    public List<Thought> GetLinks(Thought r)
    {
        List<Thought> retVal = new();
        foreach (Thought r1 in r.From?.LinksTo)
        {
            if (r.LinkType == r1.LinkType && r.To == r1.To)
                retVal.Add(r1);
        }
        return retVal;
    }

    private Thought ThoughtFromString(string label, string defaultParent, Thought source = null)
    {
        GetOrAddThought("Thought"); //safety
        GetOrAddThought("Unknown", "Thought"); //safety
        if (string.IsNullOrEmpty(label)) return null;
        if (label == "") return null;
        Thought t = Labeled(label);

        if (t is null)
        {
            if (Labeled(defaultParent) is null)
            {
                GetOrAddThought(defaultParent, Labeled("Object"), source);
            }
            t = GetOrAddThought(label, defaultParent, source);
        }
        return t;
    }

    //temporarily public for testing
    private Thought ThoughtFromObject(object o, string parentLabel = "", Thought source = null)
    {
        if (parentLabel == "")
            parentLabel = "Unknown";
        if (o is string s3)
            return ThoughtFromString(s3.Trim(), parentLabel, source);
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
                    DeleteThought(theChild);
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
    public Thought GetOrAddThought(string label, object parent = null, Thought source = null)
    {
        Thought thoughtToReturn = null;

        if (string.IsNullOrEmpty(label)) return thoughtToReturn;

        thoughtToReturn = ThoughtLabels.GetThought(label);
        if (thoughtToReturn is not null) return thoughtToReturn;

        //. are used to indicate attributes to be added
        if (label.Contains(".") && label != "." && !label.Contains(".py"))
        {
            string[] attribs = label.Split(".");
            Thought baseThought = Labeled(attribs[0]);
            if (baseThought is null) baseThought = AddThought(attribs[0], "Unknown");
            Thought instanceThought = Labeled(label);
            if (instanceThought is null)
            {
                instanceThought = AddThought(label, baseThought);
            }
            for (int i = 1; i < attribs.Length; i++)
            {
                Thought attrib = Labeled(attribs[i]);
                if (attrib is null)
                    attrib = AddThought(attribs[i], "Unknown");
                instanceThought.AddLink(attrib, "is");
            }
            return instanceThought;
        }


        Thought correctParent = null;
        if (parent is string s)
            correctParent = ThoughtLabels.GetThought(s);
        if (parent is Thought t)
            correctParent = t;
        if (correctParent is null)
            correctParent = ThoughtLabels.GetThought("Unknown");

        if (correctParent is null) return null;
//            throw new ArgumentException("GetOrAddThought: could not find parent");

        if (label.EndsWith("*"))
        {
            string baseLabel = label.Substring(0, label.Length - 1);
            Thought newParent = ThoughtLabels.GetThought(baseLabel);
            //instead of creating a new label, see if the next label for this item already exists and can be reused
            if (source is not null)
            {
                int digit = 0;
                while (source.LinksTo.FindFirst(x => x.LinkType.Label == baseLabel + digit) is not null) digit++;
                Thought labeled = ThoughtLabels.GetThought(baseLabel + digit);
                if (labeled is not null)
                    return labeled;
            }
            //if (newParent is null)
            //    newParent = AddThought(baseLabel, correctParent);
            //correctParent = newParent;
        }

        thoughtToReturn = AddThought(label, correctParent);
        return thoughtToReturn;
    }


    /// <summary>
    /// Finds or creates a subclass.  "Has 4" becomes Thought{has.4} and has.4 is 4.
    /// </summary>
    /// <param name="label">The string to process</param>
    /// <param name="attributesFollow">Attributes follow or precede the main</param>
    /// <param name="singularize"></param>
    /// <returns></returns>
    public Thought CreateThoughtFromMultipleAttributes(string label, bool attributesFollow, bool singularize = true)
    {
        IPluralize pluralizer = new Pluralizer();
        label = label.Trim();
        string[] tempStringArray = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tempStringArray.Length == 0 || tempStringArray[0].Length == 0) return null;

        for (int i = 0; i < tempStringArray.Length; i++)
            if (!char.IsUpper(tempStringArray[i][0]) && singularize)
                tempStringArray[i] = pluralizer.Singularize(tempStringArray[i]);

        string thoughtLabel;
        if (attributesFollow)
        {
            thoughtLabel = tempStringArray[0];
            for (int i = 1; i < tempStringArray.Length; i++)
                if (!string.IsNullOrEmpty(tempStringArray[i]))
                    thoughtLabel += "." + tempStringArray[i];
        }
        else
        {
            int last = tempStringArray.Length - 1;
            thoughtLabel = tempStringArray[last];
            for (int i = 0; i < last; i++)
                if (!string.IsNullOrEmpty(tempStringArray[i]))
                    thoughtLabel += "." + tempStringArray[i];
        }

        Thought t = GetOrAddThought(thoughtLabel);
        return t;
    }
}
