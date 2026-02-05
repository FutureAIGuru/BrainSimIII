//
// From the Future AI Society and Charles Simon
// Available for use under an MIT license.
//  



using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static UKS.UKS;

namespace UKS;

/// <summary>
/// A Thought is an atomic unit of thought. In the lexicon of graphs, a Thought is both a "node" and an Edge.  
/// A Thought can represent anythought, physical object, attribute, word, action, feeling, etc.
/// </summary>
/// Cognemes may have labels which are any string. Like comments or variable names, these are typically used for programmer convenience and are not usually 
/// used for functionality but are necessary to save and restore the structure.
/// Labels are case-insensitive although the initial case is preserved within the UKS.
/// Methods which return a Thought may return null in the event no Thought matches the result of the method. Methods which return lists of Thoughts will
/// return a list of zero elements if no Thoughts match the result of the method.
/// A Thought may be referenced by its Label. You can write AddParent("color") [where a Thought is a required parameter.] The system sill automatically retreive a Thought
/// with the given label or throw an exception if none exists.

public partial class Thought
{
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
    public static Thought IsA { get => ThoughtLabels.GetThought("is-a"); }
    private List<Thought> _linksTo = new List<Thought>(); //synapses to "has", "is", others
    private List<Thought> _linksFrom = new List<Thought>(); //synapses from
    private List<Thought> linksAsType = new List<Thought>(); //nodes which use this as a linkType

    /// <summary>
    /// Get an "unsafe" writeable list of a Thought's Links.
    /// This list may change while it is in use and so should not be used as a foreach iterator
    /// </summary>
    public List<Thought> LinksWriteable { get => _linksTo; }
    /// <summary>
    /// Full "Safe" list or links
    /// </summary>
    public IReadOnlyList<Thought> LinksTo { get { lock (_linksTo) { return new List<Thought>(_linksTo.AsReadOnly()); } } }
    /// <summary>
    /// Get a "safe" list of links which target this Thought
    /// </summary>
    public IReadOnlyList<Thought> LinksFrom { get { lock (_linksFrom) { return new List<Thought>(_linksFrom.AsReadOnly()); } } }
    /// <summary>
    /// Get an "unsafe" writeable list of Links which target this Thought
    /// </summary>
    public List<Thought> LinksFromWriteable { get => _linksFrom; }
    /// <summary>
    /// Get an "unsafe" writeable list of Links for which this Thought is the link type
    /// </summary>
    public List<Thought> LinksAsTypeWriteable { get => linksAsType; }

    private string _label = "";
    /// <summary>
    /// Manages a Thought's label and maintais a hash table
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

    public DateTime LastFiredTime = new();


    //NEEDED for Links
    public Thought _from;
    /// <summary>
    /// the Thought Source
    /// </summary>
    public Thought? From
    {
        get => _from;
        set { _from = value; }
    }
    private Thought _linkType;
    /// <summary>
    /// The Link Type
    /// </summary>
    public Thought? LinkType
    {
        get { return _linkType; }
        set
        {
            _linkType = value;
        }
    }
    private Thought _to;
    public Thought? To
    {
        get { /*Hits++; lastUsed = DateTime.Now;*/ return _to; }
        set
        {
            _to = value;
        }
    }


    object _value;
    /// <summary>
    /// Any serializable object can be attached to a Thought
    /// </summary>
    public object V
    {
        get => _value;
        set
        {
            this._value = value;
        }
    }

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

    //The following is needed to suppress a warning
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
            if (a.To == b.To && a.From == b.From && a.LinkType == b.LinkType)
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

    public Thought AddToUKS()
    {
        if (this.LinkType is null) return this;
        if (string.IsNullOrEmpty(this.Label))
            Label = "R*";
        this.AddParent("Link");
        lock (theUKS.AllThoughts)
        {
            if (!theUKS.AllThoughts.Contains(this))
                theUKS.AllThoughts.Add(this);
        }
        return this;
    }



    private IReadOnlyList<Thought> LinksOfType(Thought linkType, bool useLinkFrom = false)
    {
        List<Thought> retVal = new List<Thought>();
        if (!useLinkFrom)
        {
            lock (_linksTo)
            {
                foreach (Thought r in _linksTo)
                    if (r?.LinkType is not null && r?.LinkType == linkType && r?.From == this)
                        retVal.Add(r.To);
            }
        }
        else
        {
            lock (_linksFrom)
            {
                foreach (Thought r in _linksFrom)
                    if (r.LinkType is not null && r.LinkType == linkType && r.To == this)
                        retVal.Add(r.From);
            }
        }
        return retVal;
    }



    /// <summary>
    /// "Safe" list of direct ancestors
    /// </summary>
    public IReadOnlyList<Thought> Parents { get => LinksOfType(IsA, false); }

    /// <summary>
    /// "Safe" list of direct descendants
    /// </summary>
    public IReadOnlyList<Thought> Children { get => LinksOfType(IsA, true); }
    public IReadOnlyList<Thought> ChildrenWithSubclasses
    {
        get
        {
            List<Thought> retVal = (List<Thought>)LinksOfType(IsA, true);

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
        return FollowTransitiveLinks(IsA, true);
    }

    /// <summary>
    /// Recursively gets all the ancestors of a Thought
    /// </summary>
    public IEnumerable<Thought> Ancestors
    {
        get
        {
            IReadOnlyList<Thought> ancestors = AncestorList();
            for (int i = 0; i < ancestors.Count; i++)
            {
                Thought child = ancestors[i];
                yield return child;
            }
        }
    }

    public IEnumerable<Thought> Descendants
    {
        get
        {
            foreach (var child in this.Children)
            {
                yield return child;

                foreach (var descendant in child.Descendants)
                    yield return descendant;
            }
        }
    }
    public IEnumerable<Thought> RecursiveLinks
    {
        get
        {
            foreach (var r in this.LinksTo)
            {
                yield return r;

                foreach (var r1 in r.RecursiveLinks)
                    yield return r1;
            }
        }
    }

    public IEnumerable<Thought> SequenceNodes()
    {
        var current = this;

        while (current is not null)
        {
            yield return current;  // Return this node, pause, wait for next request

            Thought nextRel = null;
            if (current.LinkType?.Label == "NXT") nextRel = current.To;

            if (nextRel is null) yield break;  // No more nodes, stop iteration

            if (nextRel.To == this) yield break;  // Reached source, sequence complete

            current = nextRel;
        }
    }

    /// <summary>
    /// Determines whether a Thought has a specific ancestor
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool HasAncestorLabeled(string label)
    {
        Thought t = ThoughtLabels.GetThought(label);
        if (t is null) return false;
        return HasAncestor(label);
    }

    /// <summary>
    /// Determines whether a Thought has a specific ancestor
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool HasAncestor(Thought t)
    {
        var x = FollowTransitiveLinks(IsA, true, t);
        return x.Count != 0;
    }

    /// <summary>
    /// Determines how many descendants a Thought has
    /// </summary>
    /// <returns>the count</returns>
    public int GetDescendentsCount()
    {
        return DescendentsList().Count;
    }

    /// <summary>
    /// Returns a list of all of a thought's descendandants.
    /// CAUTION: this may be large and time-consuming
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<Thought> DescendentsList()
    {
        return FollowTransitiveLinks(IsA, false);
    }

    /// <summary>
    /// Recursively gets all descendents of a Thought. Use with caution as this might be a large list
    /// </summary>
    public IEnumerable<Thought> Descendents
    {
        get
        {
            IReadOnlyList<Thought> descendents = DescendentsList();
            for (int i = 0; i < descendents.Count; i++)
            {
                Thought child = descendents[i];
                yield return child;
            }
        }
    }

    //Follow chain of links with linkType
    private IReadOnlyList<Thought> FollowTransitiveLinks(Thought linkType, bool followUpwards = true, Thought searchTarget = null)
    {
        List<Thought> retVal = new();
        retVal.Add(this);
        if (this == searchTarget) return retVal;

        for (int i = 0; i < retVal.Count; i++)
        {
            Thought t = retVal[i];
            //IReadOnlyList<Thought> linksToFollow = followUpwards ? t.LinksFrom : t.Links;
            IReadOnlyList<Thought> linksToFollow = followUpwards ? t.LinksTo : t.LinksFrom;
            foreach (Thought r in linksToFollow)
            {
                //Thought thoughtToAdd = followUpwards ? r.source : r.target;
                Thought thoughtToAdd = followUpwards ? r?.To : r?.From;
                if (r?.LinkType == linkType)
                {
                    if (!retVal.Contains(thoughtToAdd))
                        retVal.Add(thoughtToAdd);
                }
                if (searchTarget == thoughtToAdd)
                    return retVal;
            }
        }
        if (searchTarget is not null) retVal.Clear();
        return retVal;
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
                    lock (linkType.linksAsType)
                    {
                        LinksWriteable.Add(r);
                        target.LinksFromWriteable.Add(r);
                        if (!linkType.LinksAsTypeWriteable.Contains(r))
                            linkType.LinksAsTypeWriteable.Add(r);
                    }
        }
        else if (linkType is null)
        {
            lock (_linksTo)
            {
                LinksWriteable.Add(r);
            }
        }
        else if (linkType is not null)
        {
            lock (_linksTo)
                lock (linkType.linksAsType)
                {
                    LinksWriteable.Add(r);
                    if (!linkType.LinksAsTypeWriteable.Contains(r))
                        linkType.LinksAsTypeWriteable.Add(r);
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
            lock (r.From.LinksWriteable)
            {
                lock (r.LinkType.LinksFromWriteable)
                {
                    r.From.LinksWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To is null);
                    r.LinkType.LinksFromWriteable.RemoveAll(x => x.From == r.From && x.LinkType == r.LinkType && x.To is null);
                }
            }
        }
        else
        {
            lock (r.From.LinksWriteable)
            {
                lock (r.LinkType.LinksFromWriteable)
                {
                    lock (r.To.LinksFromWriteable)
                    {
                        r.From.LinksWriteable.Remove(r);
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
            return AddLink(newParent, IsA);
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

    List<Thought> stack = null;
    public bool HasProperty(Thought t)  //with inheritance
    {
        if (stack is null) stack = new();
        if (stack.Contains(t)) return false;
        stack.Add(t);
        foreach (Thought r in LinksTo)
            if (r?.LinkType.Label == "hasProperty" && r.To == t)
                return true;
        foreach (Thought t1 in Parents) //handle inheritance 
        {
            return t1.HasProperty(t);
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
    /// Additionally: if any encountered Thought is unlabeled, it is assigned a GUID label.
    /// </summary>
    public IEnumerable<Thought> EnumerateClosure()
    {
        var visited = new HashSet<Thought>();
        var q = new Queue<Thought>();

        void EnsureLabel(Thought t)
        {
            if (string.IsNullOrWhiteSpace(t.Label))
                // Put the GUID into the label only when it's unlabeled
                t.Label = $"unl_{Guid.NewGuid().ToString("N")[..8]}";
        }

        void EnqueueIfNew(Thought? t)
        {
            if (t is null) return;
            //if (!t.To?.HasAncestor(this)) return;
            EnsureLabel(t);
            if (visited.Add(t))
                q.Enqueue(t);
        }

        // start
        EnqueueIfNew(this);
        foreach (var isaLink in this.LinksTo.Where(x => x.LinkType?.Label == "is-a"))
        {
            EnsureLabel(isaLink);  //hack to include the parents of the root.
            yield return isaLink;
        }

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (t is null) continue;
            if (t.From is not null || t.To is not null || t.LinkType is not null)
                yield return t;

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
