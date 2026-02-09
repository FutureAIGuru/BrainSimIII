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
using static UKS.UKS;

namespace UKS;

/// <summary>
/// A Thought is an atomic unit of thought. In the lexicon of graphs, a Thought is both a "node" and an Edge.  
/// A Thought can represent anything, physical object, attribute, word, action, feeling, etc.
/// </summary>
/// Thoughs may have labels which are any string...no special characters except '.'. Like comments or variable names, these are typically used for programmer convenience and are not usually 
/// used for functionality but are necessary to save and restore the structure.
/// Labels are case-insensitive although the initial case is preserved within the UKS.
/// Methods which return a Thought may return null in the event no Thought matches the result of the method. Methods which return lists of Thoughts will
/// return a list of zero elements if no Thoughts match the result of the method.
/// A Thought may be referenced by its Label. You can write AddParent("color") [where a Thought is a required parameter.] The system sill automatically retreive a Thought
/// with the given label.

public partial class Thought
{
    public static Thought IsA { get => ThoughtLabels.GetThought("is-a"); }  //this is a cache value shortcut for (Thought)"is-a"
    private List<Thought> _linksTo = new List<Thought>(); //links to "has", "is", is-a, many others
    private List<Thought> _linksFrom = new List<Thought>(); //links from

    /// <summary>
    /// Get an "unsafe" writeable list of a Thought's Links.
    /// This list may change while it is in use and so should not be used as a foreach iterator
    /// </summary>
    public List<Thought> LinksToWriteable { get => _linksTo; }
    /// <summary>
    /// Get an "unsafe" writeable list of Links which target this Thought
    /// </summary>
    public List<Thought> LinksFromWriteable { get => _linksFrom; }
    /// <summary>
    /// Full "Safe" list or links
    /// </summary>
    public IReadOnlyList<Thought> LinksTo { get { lock (_linksTo) { return new List<Thought>(_linksTo.AsReadOnly()); } } }
    /// <summary>
    /// Get a "safe" list of links which target this Thought
    /// </summary>
    public IReadOnlyList<Thought> LinksFrom { get { lock (_linksFrom) { return new List<Thought>(_linksFrom.AsReadOnly()); } } }
    /// <summary>
    /// "Safe" list of direct ancestors atomic thoughts (not links)
    /// </summary>
    public IReadOnlyList<Thought> Parents { get { lock (_linksTo) { return new List<Thought>(_linksTo.Where(x => x.LinkType?.Label == "is-a").Select(x => x.To).ToList().AsReadOnly()); } } }
    /// <summary>
    /// "Safe" list of direct descendants
    /// </summary>
    public IReadOnlyList<Thought> Children {get {lock (_linksFrom) { return new List<Thought>(_linksFrom.Where(x => x.LinkType?.Label == "is-a").Select(x => x.From).ToList().AsReadOnly()); }}}

    private string _label = "";
    /// <summary>
    /// Manages a Thought's label and maintais a hash table
    //*Restrictions on Thought LabelsNames:
    // * must be unique
    // * cannot include ' ' (use a - instead)
    // * cannot include '.' this is the flag for creating a subclass with following attributes
    // * cannot include '*' this is the flag for auto-increment the label
    // * case insensitive but initial input case is preserved for display
    // * capitalized labels are never signularized even if "singularize=true"
    /// </summary>
    public string Label
    {
        get => _label;
        set
        {
            if (value == _label) return; //label is unchanged
            ThoughtLabels.RemoveThoughtLabel(_label);
            _label = ThoughtLabels.AddThoughtLabel(value, this);
        }
    }

    //TODO: make this useful
    public DateTime LastFiredTime = DateTime.Now;
    private TimeSpan _timeToLive = TimeSpan.MaxValue;
    /// <summary>
    /// When set, makes a Thought transient
    /// </summary>
    public TimeSpan TimeToLive
    {
        get { return _timeToLive; }
        set
        {
            _timeToLive = value;
            if (_timeToLive != TimeSpan.MaxValue)
                AddToTransientList();
        }
    }


    //////NEEDED for Link functionality 
    public Thought? _from;
    /// <summary>
    /// the Thought Source
    /// </summary>
    public Thought? From
    {
        get => _from;
        set { _from = value; }
    }
    private Thought? _linkType;
    /// <summary>
    /// The Link Type
    /// </summary>
    public Thought? LinkType
    {
        get { return _linkType; }
        set { _linkType = value; }
    }
    private Thought? _to;
    public Thought? To
    {
        get { return _to; }
        set { _to = value; }
    }



    object _value;
    /// <summary>
    /// Any serializable object can be attached to a Thought
    /// ONLY STRINGS are supported for save/restor to disk file
    /// </summary>
    public object V
    {
        get => _value;
        set{this._value = value;}
    }

    private float _weight = 1;
    public float Weight
    {
        get { return _weight; }
        set
        {
            _weight = value;
            //if this is a commutative link, also set the weight on the reverse
            if (LinkType?.HasProperty("IsCommutative") == true)
            {
                Thought rReverse = To.LinksTo.FindFirst(x => x.LinkType == LinkType && x.To == From);
                if (rReverse is not null)
                {
                    rReverse._weight = _weight;
                }
            }
        }
    }


    //The constructores
    public Thought()
    {
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="r"></param>
    public Thought(Thought r)
    {
        LinkType = r.LinkType;
        From = r.From;
        To = r.To;
        Weight = r.Weight;
        //COPY other properties as needed
    }

    /// <summary>
    /// Returns a Thought's label.
    /// Even though it shows zero references, don't delete this ToString() because the debugger uses it when mousing over a Thought
    /// </summary>
    /// <returns>the Thought's label</returns>
    public override string ToString()
    {
        if (LinkType?.Label == "spelled")
        { }
        string retVal = Label;

        if (From is not null || LinkType is not null || To is not null)
        {
            if (theUKS.IsSequenceElement(this))
            {
                var valuList = theUKS.FlattenSequence(this);
                retVal = "^" + string.Join("", valuList);
                return retVal;
            }

            retVal += "[";
            if (From is not null)
            {
                retVal += From?.ToString();
            }
            if (LinkType is not null)
                retVal += ((retVal == "") ? "" : "->") + LinkType?.ToString();
            if (To is not null)
            {
                retVal += ((retVal == "") ? "" : "->") + To?.ToString();
            }
            retVal += "]";
        }
        if (V is not null)  //if there is a string value, add it to the end of the line
            retVal += "_V:" + V.ToString();

        return retVal;
    }


    /// <summary>
    /// This is the magic which allows for strings to be put in place of Thoughts for any method Paramter
    /// </summary>
    /// <param name="label"></param>
    /// Throse 
    public static implicit operator Thought(string label)
    {
        Thought t = ThoughtLabels.GetThought(label);
        if (t is null)
        { }
        //            throw new ArgumentNullException($"No Thought found with label: {label}");
        return t;
    }

    //The following is used by several list operations
    public override bool Equals(Object obj)
    {
        if (obj is Thought t)
        {
            if (Label != t.Label) return false;
            //are the links the same?
            if (LinksTo.Count != t.LinksTo.Count) return false;
            for (int i = 0; i < LinksTo.Count; i++)
                if (LinksTo[i] != t.LinksTo[i]) return false;

            if (t.From is null && t.LinkType is null && t.To is null)
            {//must be atomic
                return true;
            }
            if ((t.From is not null || t.LinkType is not null || t.To is not null))
            {
                //this must be a link
                if ((To is null || t.To == To) &&  //
                    (From is null || t.From == From) &&
                    (t.LinkType is not null && t.LinkType == LinkType))
                    return true;
            }
        }
        return false;
    }

    public static bool operator ==(Thought? a, Thought? b)
    {
        //if (a is null && b is null)
        //    return true;
        if (a is null || b is null)
            return false;
        if (a.Label != "" && a.Label == b.Label) return true;
        if (a.To is not null || a.LinkType is not null || a.To is not null)
            if ((a.To is null && b.To is null) || a.To == b.To && a.From == b.From && a.LinkType == b.LinkType)
                return true;
        return false;
    }
    public static bool operator !=(Thought? a, Thought? b)
    {
        if (a is null && b is null)
            return false;
        if (a is null || b is null)
            return true;
        if (a.Label != b.Label) return true;
        if ((a.To is null || a.To == b.To) &&
            (a.From is null || a.From == b.From) &&
            (a.LinkType is null || a.LinkType == b.LinkType))
            return false;
        return true;
    }

    public Thought AddDefaultLabel()
    {
        if (this.LinkType is null) return this;
        if (string.IsNullOrEmpty(this.Label))
            Label = "R*";
        return this;
    }

    //This is only used in certain Agent modules...  Refactor out
    public IReadOnlyList<Thought> ChildrenWithSubclasses
    {
        get
        {
            List<Thought> retVal = (List<Thought>)Children;// (List<Thought>)LinksOfType(IsA, true);

            for (int i = 0; i < retVal.Count; i++)
            {
                Thought t = retVal[i];
                if (t.Label.StartsWith(this._label))
                {
                    retVal.AddRange(t.Children);
                    retVal.RemoveAt(i);
                    i--;
                }
            }
            return retVal;
        }
    }


    /// ////////////////////////////////////////////////////////////////////////////
    //Handle the ancestors and descendents of a Thought
    //////////////////////////////////////////////////////////////
    public IReadOnlyList<Thought> AncestorList()
    {
        return Ancestors.ToList();
    }

    public IEnumerable<Thought> Ancestors
    {
        get
        {
            //TODO: examine ramifications of adding "this" to beginning of list
            var queue = new Queue<Thought>();
            queue.Enqueue(this);
            foreach (var parent in Parents)
                queue.Enqueue(parent);
            var seen = new HashSet<Thought>();

            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                if (parent is null || !seen.Add(parent)) continue;

                yield return parent;

                foreach (var gp in parent.Parents)
                    queue.Enqueue(gp);
            }
        }
    }

    public IEnumerable<Thought> Descendants
    {
        get
        {
            var queue = new Queue<Thought>(Children);
            var seen = new HashSet<Thought>();

            while (queue.Count > 0)
            {
                var child = queue.Dequeue();
                if (child is null || !seen.Add(child)) continue;

                yield return child;

                foreach (var gc in child.Children)
                    queue.Enqueue(gc);
            }
        }
    }


    /// <summary>
    /// Determines whether a Thought has a specific ancestor
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool HasAncestor(Thought t)
    {
        foreach (var ancestor in Ancestors)
            if (ancestor == t) return true;
        return false;
    }

    /// <summary>
    /// Returns a list of all of a thought's descendandants.
    /// CAUTION: this may be large and time-consuming
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<Thought> DescendantsList()
    {
        return Descendants.ToList();
    }


    /// <summary>
    /// Updates the last-fired time on a Thought
    /// </summary>
    public void Fire()
    {
        LastFiredTime = DateTime.Now;
        //useCount++;
    }


    //LINKS
    //TODO reverse the parameters so it's type,target
    /// <summary>
    /// Adds a link to a Thought if it does not already exist.  The Thought is the source of the link.
    /// </summary>
    /// <param name="target">Target Thought</param>
    /// <param name="linkType">RelatinoshipType Thought</param>
    /// <returns>the new or existing Thought</returns>
    public Thought AddLink(Thought target, Thought linkType)
    {
        if (linkType is null)  //NULL link types could be allowed in search Thoughtys Parameter?
        {
            return null;
        }

        //does the link already exist?
        Thought r = HasLink(target, linkType);
        if (r is not null)
        {
            //AdjustLink(r.T);
            return r;
        }
        r = new Thought()
        {
            LinkType = linkType,
            From = this,
            To = target,
        };
        if (target is not null && linkType is not null)
        {
            lock (_linksTo)
                lock (target._linksFrom)
                {
                    LinksToWriteable.Add(r);
                    target.LinksFromWriteable.Add(r);
                }
        }
        else
        {
            lock (_linksTo)
            {
                LinksToWriteable.Add(r);
            }
        }
        return r;
    }

    public void RemoveLinks(Thought linkType)
    {
        for (int i = 0; i < _linksTo.Count; i++)
        {
            Thought r = _linksTo[i];
            if (r.From == this && r.LinkType == linkType)
            {
                RemoveLink(r);
                i--;
            }
        }
    }

    //TODO reverse the parameters so it's type,target
    private Thought HasLink(Thought target, Thought linkType)
    {
        foreach (Thought r in _linksTo)
        {
            if (r.From == this && r.To == target && r.LinkType == linkType)
                return r;
        }
        return null;
    }

    /// <summary>
    /// Removes a link. 
    /// </summary>
    /// <param name="r">The Thought's source neede not be this Thought</param>
    public void RemoveLink(Thought r)
    {
        if (r is null) return;
        if (r.LinkType is null) return;
        if (r.From is null)
        {
            lock (r.LinkType.LinksFromWriteable)
            {
                lock (r.To.LinksFromWriteable)
                {
                    r.LinkType.LinksFromWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To == r.To);
                    r.To.LinksFromWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To == r.To);
                }
            }
        }
        else if (r.To is null)
        {
            lock (r.From.LinksToWriteable)
            {
                lock (r.LinkType.LinksFromWriteable)
                {
                    r.From.LinksToWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To is null);
                    r.LinkType.LinksFromWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To is null);
                }
            }
        }
        else
        {
            lock (r.From.LinksToWriteable)
            {
                lock (r.LinkType.LinksFromWriteable)
                {
                    lock (r.To.LinksFromWriteable)
                    {
                        r.From.LinksToWriteable.Remove(r);
                        r.LinkType.LinksFromWriteable.Remove(r);
                        r.To.LinksFromWriteable.Remove(r);
                    }
                }
            }
        }
    }

    public Thought HasLink(Thought source, Thought linkType, Thought targett)
    {
        if (source is null && linkType is null && targett is null) return null;
        foreach (Thought r in LinksTo)
            if ((source is null || r.From == source) &&
                (linkType is null || r.LinkType == linkType) &&
                (targett is null || r.To == targett)) return r;
        return null;
    }


    public Thought RemoveLink(Thought t2, Thought linkType)
    {
        Thought r = new() { From = this, LinkType = linkType, To = t2 };
        RemoveLink(r);
        return r;
    }

    /// <summary>
    /// Addsa a parent to a Thought
    /// </summary>
    /// <param name="newParent"></param>
    public Thought AddParent(Thought newParent)
    {
        if (newParent is null) return null;
        if (!Parents.Contains(newParent))
        {
            //newParent.AddLink(this, IsA);
            return AddLink(newParent, "is-a");
        }
        return LinksTo.FindFirst(x => x.To == newParent && x.LinkType == IsA);
    }

    /// <summary>
    /// Remove a parent from a Thought
    /// </summary>
    /// <param name="t">If the Thought is not a parent, the function does nothought</param>
    public void RemoveParent(Thought t)
    {
        Thought r = new() { From = this, LinkType = IsA, To = t };
        t.RemoveLink(r);
    }


    public void RemoveChild(Thought t)
    {
        Thought r = new() { From = t, LinkType = IsA, To = this };
        RemoveLink(r);
    }


    public List<Thought> GetAttributes()
    {
        List<Thought> retVal = new();
        foreach (Thought r in LinksTo)
        {
            if (r.LinkType.Label != "hasAttribute" && r.LinkType.Label != "is") continue;
            retVal.Add(r.To);
        }
        return retVal;
    }

    public bool HasProperty(Thought t)  //with inheritance
    {
        //NOT thread safe
        if (t is null) return false;
        if (LinksTo.FindFirst(x => x.LinkType.Label == "hasProperty" && x.To == t) is not null) return true;

        foreach (Thought t1 in Ancestors) //handle inheritance 
        {
            if (t1.LinksTo.FindFirst(x => x.LinkType.Label == "hasProperty" && x.To == t) is not null) return true;
        }
        return false;
    }


    private void AddToTransientList()
    {
        if (!UKS.transientLinks.Contains(this))
            UKS.transientLinks.Add(this);
    }

    /// <summary>
    /// Enumerate the closure starting from this Thought (root) using a queue (BFS):
    /// - yields root
    /// - follows all outgoing LinksTo (includes the link-thought, its LinkType, its To)
    /// - also follows incoming "is-a" LinksFrom (includes the link-thought, its LinkType, its From)
    /// Cycle-safe via reference-identity visited set (not labels).
    ///
    /// </summary>
    public IEnumerable<Thought> EnumerateSubThoughts()
    {
        var visited = new HashSet<Thought>();
        var q = new Queue<Thought>();

        void EnqueueIfNew(Thought? t)
        {
            if (t is null) return;
            if (visited.Add(t))
                q.Enqueue(t);
        }

        // start
        EnqueueIfNew(this);
        foreach (var isaLink in this.LinksTo.Where(x => x.LinkType?.Label == "is-a"))
        {
            yield return isaLink;
        }

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (t is null) continue;
            if (t.From is not null || t.To is not null || t.LinkType is not null)
                yield return t;
            else
            {}

            EnqueueIfNew(t.LinkType);
            EnqueueIfNew(t.To);
            foreach (var isaLink in t.LinksFrom.Where(x => x.LinkType?.Label == "is-a"))  //get all the children of this Thought
            {
                EnqueueIfNew(isaLink);
                EnqueueIfNew(isaLink.From);
            }
            foreach (var link in t.LinksTo.Where(x => x.LinkType?.Label != "is-a")) //don't get the parents again
            {
                EnqueueIfNew(link);
                EnqueueIfNew(link.To);
            }

            //is this a seq?
            EnqueueIfNew(t.LinkType);
            EnqueueIfNew(t.To);
        }
    }
}
