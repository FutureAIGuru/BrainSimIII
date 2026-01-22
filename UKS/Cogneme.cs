//
// From the Future AI Society and Charles Simon
// Available for use under an MIT license.
//  



namespace UKS;

/// <summary>
/// A cogneme is an atomic unit of thought. In the lexicon of graphs, a cogneme is both a "node" and an Edge.  
/// A cogneme can represent anything, physical object, attribute, word, action, feeling, etc.
/// </summary>
/// Cognemes may have labels which are any string. Like comments or variable names, these are typically used for programmer convenience and are not usually 
/// used for functionality but are necessary to save and restore the structure.
/// Labels are case-insensitive although the initial case is preserved within the UKS.
/// Methods which return a Thing may return null in the event no Thing matches the result of the method. Methods which return lists of Things will
/// return a list of zero elements if no Things match the result of the method.
/// A Thing may be referenced by its Label. You can write AddParent("color") [where a Thing is a required parameter.] The system sill automatically retreive a Thing
/// with the given label or throw an exception if none exists.

public partial class Cogneme
{
    /// <summary>
    /// This is the magic which allows for strings to be put in place of Things for any method Paramter
    /// </summary>
    /// <param name="label"></param>
    /// Throse 
    public static implicit operator Cogneme(string label)
    {
        Cogneme t = CognemeLabels.GetThing(label);
        if (t is null)
        { }
        //            throw new ArgumentNullException($"No Thing found with label: {label}");
        return t;
    }
    //    public static Thing HasChild { get => ThingLabels.GetThing("has-child"); }
    public static Cogneme IsA { get => CognemeLabels.GetThing("is-a"); }

    private List<Cogneme> _relationships = new List<Cogneme>(); //synapses to "has", "is", others
    private List<Cogneme> _relationshipsFrom = new List<Cogneme>(); //synapses from
    private List<Cogneme> relationshipsAsType = new List<Cogneme>(); //nodes which use this as a relationshipType

    /// <summary>
    /// Get an "unsafe" writeable list of a Thing's Relationships.
    /// This list may change while it is in use and so should not be used as a foreach iterator
    /// </summary>
    public List<Cogneme> RelationshipsWriteable { get => _relationships; }
    /// <summary>
    /// Full "Safe" list or relationships
    /// </summary>
    public IReadOnlyList<Cogneme> Relationships {get{lock (_relationships){return new List<Cogneme>(_relationships.AsReadOnly());}}}
    /// <summary>
    /// Get a "safe" list of relationships which target this Thing
    /// </summary>
    public IReadOnlyList<Cogneme> RelationshipsFrom { get { lock (_relationshipsFrom) { return new List<Cogneme>(_relationshipsFrom.AsReadOnly()); } } }
    /// <summary>
    /// Get an "unsafe" writeable list of Relationships which target this Thing
    /// </summary>
    public List<Cogneme> RelationshipsFromWriteable { get => _relationshipsFrom; }
    /// <summary>
    /// Get an "unsafe" writeable list of Relationships for which this Thing is the relationship type
    /// </summary>
    public List<Cogneme> RelationshipsAsTypeWriteable { get => relationshipsAsType; }

    private string _label = "";
    object _value;
    //public int useCount = 0;
    public DateTime LastFiredTime = new();


    //NEEDED for Relationships
    public Cogneme _source;
    /// <summary>
    /// the Thing Source
    /// </summary>
    public Cogneme Source
    {
        get => _source;
        set { _source = value; }
    }
    private Cogneme _relType;
    /// <summary>
    /// The Thing Type
    /// </summary>
    public Cogneme RelType
    {
        get { return _relType; }
        set
        {
            _relType = value;
        }
    }
    private Cogneme _target;
    public Cogneme Target
    {
        get { /*Hits++; lastUsed = DateTime.Now;*/ return _target; }
        set
        {
            _target = value;
        }
    }


    /// <summary>
    /// Any serializable object can be attached to a Thing
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
            //if this is a commutative relationship, also set the weight on the reverse
            if (RelType?.HasProperty("IsCommutative") == true)
            {
                Cogneme rReverse = Target.Relationships.FindFirst(x => x.RelType == RelType && x.Target == Source);
                if (rReverse is not null)
                {
                    rReverse._weight = _weight;
                }
            }
        }
    }

    private TimeSpan _timeToLive = TimeSpan.MaxValue;
    /// <summary>
    /// When set, makes a Thing transient
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


    public Cogneme()
    {
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="r"></param>
    public Cogneme(Cogneme r)
    {
        RelType = r.RelType;
        Source = r.Source;
        Target = r.Target;
        Weight = r.Weight;
    }

    /// <summary>
    /// Returns a Thing's label.
    /// Even though it shows zero references, don't delete this ToString() because the debugger uses it when mousing over a Thing
    /// </summary>
    /// <returns>the Thing's label</returns>
    public override string ToString()
    {

        string retVal = _label;
        if (V is not null)
            retVal += " V: " + V.ToString();

        if (Source is not null || RelType is not null || Target is not null)
            retVal += SingleToString();

        return retVal;
    }
    /// <summary>
    /// Manages a Thing's label and maintais a hash table
    /// </summary>
    public string Label
    {
        get => _label;
        set
        {
            if (value == _label) return; //label is unchanged
            CognemeLabels.RemoveThingLabel(_label);
            _label = CognemeLabels.AddThingLabel(value, this);
        }
    }

    public Cogneme AddToUKS()
    {
        if (this.RelType is null) return this;
        if (string.IsNullOrEmpty(this.Label))
            Label = "R*";
        this.AddParent("Relationship");
        //this.Source.AddRelationship(this.Target, this.RelType);
        lock (UKS.theUKS.AllThings)
        {
            if (!UKS.theUKS.AllThings.Contains(this))
                UKS.theUKS.AllThings.Add(this);
        }
        return this;
    }


    public string SingleToString()
    {
        string retVal = "";// Label;
        retVal += "[";
        if (!string.IsNullOrEmpty(Source?.ToString()))
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
        retVal += "]";
        return retVal;
    }

    public static bool operator ==(Cogneme? a, Cogneme? b)
    {
        //if (a is null && b is null)
        //    return true;
        if (a is null || b is null)
            return false;
        if (a.Label != "" && a.Label == b.Label) return true;
        if (a.Target is not null || a.RelType is not null || a.Target is not null)
            if (a.Target == b.Target && a.Source == b.Source && a.RelType == b.RelType)
                return true;
        return false;
    }
    //The following is needed to suppress a warning
    public override bool Equals(Object obj)
    {
        if (obj is Cogneme t && Label != t.Label) return false;
        if (obj is Cogneme a && (a.Source is not null || a.RelType is not null || a.Target is not null))
        {
            if (a.Target == Target &&
                a.Source == Source &&
                a.RelType == RelType &&
                a.Relationships.SequenceEqual(Relationships))
                return true;
        }
        if (obj is Cogneme b && (b.Source is null && b.RelType is null && b.Target is null))
            return true;
        return false;
    }

    public static bool operator !=(Cogneme? a, Cogneme? b)
    {
        if (a is null && b is null)
            return false;
        if (a is null || b is null)
            return true;
        if (a.Target == b.Target && a.Source == b.Source && a.RelType == b.RelType) return false;
        return true;
    }


    private IReadOnlyList<Cogneme> RelationshipsOfType(Cogneme relType, bool useRelationshipFrom = false)
    {
        List<Cogneme> retVal = new List<Cogneme>();
        if (!useRelationshipFrom)
        {
            lock (_relationships)
            {
                foreach (Cogneme r in _relationships)
                    if (r?.RelType is not null && r?.RelType == relType && r?.Source == this)
                        retVal.Add(r.Target);
            }
        }
        else
        {
            lock (_relationshipsFrom)
            {
                foreach (Cogneme r in _relationshipsFrom)
                    if (r.RelType is not null && r.RelType == relType && r.Target == this)
                        retVal.Add(r.Source);
            }
        }
        return retVal;
    }



    /// <summary>
    /// "Safe" list of direct ancestors
    /// </summary>
    public IReadOnlyList<Cogneme> Parents { get => RelationshipsOfType(IsA, false); }

    /// <summary>
    /// "Safe" list of direct descendants
    /// </summary>
    public IReadOnlyList<Cogneme> Children { get => RelationshipsOfType(IsA, true); }
    public IReadOnlyList<Cogneme> ChildrenWithSubclasses
    {
        get
        {
            List<Cogneme> retVal = (List<Cogneme>)RelationshipsOfType(IsA, true);

            for (int i = 0; i < retVal.Count; i++)
            {
                Cogneme t = retVal[i];
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
    //Handle the ancestors and descendents of a Thing
    //////////////////////////////////////////////////////////////
    public IReadOnlyList<Cogneme> AncestorList()
    {
        return FollowTransitiveRelationships(IsA, true);
    }

    /// <summary>
    /// Recursively gets all the ancestors of a Thing
    /// </summary>
    public IEnumerable<Cogneme> Ancestors
    {
        get
        {
            IReadOnlyList<Cogneme> ancestors = AncestorList();
            for (int i = 0; i < ancestors.Count; i++)
            {
                Cogneme child = ancestors[i];
                yield return child;
            }
        }
    }

    public IEnumerable<Cogneme> Descendants
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
    public IEnumerable<Cogneme> RecursiveRelationships
    {
        get
        {
            foreach (var r in this.Relationships)
            {
                yield return r;

                foreach (var r1 in r.RecursiveRelationships)
                    yield return r1;
            }
        }
    }

    public IEnumerable<Cogneme> SequenceNodes()
    {
        var current = this;

        while (current is not null)
        {
            yield return current;  // Return this node, pause, wait for next request

            Cogneme nextRel = null;
            if (current.RelType?.Label == "NXT") nextRel = current.Target;

            if (nextRel is null) yield break;  // No more nodes, stop iteration

            if (nextRel.Target == this) yield break;  // Reached source, sequence complete

            current = nextRel;
        }
    }

    /// <summary>
    /// Determines whether a Thing has a specific ancestor
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool HasAncestorLabeled(string label)
    {
        Cogneme t = CognemeLabels.GetThing(label);
        if (t is null) return false;
        return HasAncestor(label);
    }

    /// <summary>
    /// Determines whether a Thing has a specific ancestor
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool HasAncestor(Cogneme t)
    {
        var x = FollowTransitiveRelationships(IsA, true, t);
        return x.Count != 0;
    }

    /// <summary>
    /// Determines how many descendants a Thing has
    /// </summary>
    /// <returns>the count</returns>
    public int GetDescendentsCount()
    {
        return DescendentsList().Count;
    }

    /// <summary>
    /// Returns a list of all of a thing's descendandants.
    /// CAUTION: this may be large and time-consuming
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<Cogneme> DescendentsList()
    {
        return FollowTransitiveRelationships(IsA, false);
    }

    /// <summary>
    /// Recursively gets all descendents of a Thing. Use with caution as this might be a large list
    /// </summary>
    public IEnumerable<Cogneme> Descendents
    {
        get
        {
            IReadOnlyList<Cogneme> descendents = DescendentsList();
            for (int i = 0; i < descendents.Count; i++)
            {
                Cogneme child = descendents[i];
                yield return child;
            }
        }
    }

    //Follow chain of relationships with relType
    private IReadOnlyList<Cogneme> FollowTransitiveRelationships(Cogneme relType, bool followUpwards = true, Cogneme searchTarget = null)
    {
        List<Cogneme> retVal = new();
        retVal.Add(this);
        if (this == searchTarget) return retVal;

        for (int i = 0; i < retVal.Count; i++)
        {
            Cogneme t = retVal[i];
            //IReadOnlyList<Thing> relationshipsToFollow = followUpwards ? t.RelationshipsFrom : t.Relationships;
            IReadOnlyList<Cogneme> relationshipsToFollow = followUpwards ? t.Relationships : t.RelationshipsFrom;
            foreach (Cogneme r in relationshipsToFollow)
            {
                //Thing thingToAdd = followUpwards ? r.source : r.target;
                Cogneme thingToAdd = followUpwards ? r?.Target : r?.Source;
                if (r?.RelType == relType)
                {
                    if (!retVal.Contains(thingToAdd))
                        retVal.Add(thingToAdd);
                }
                if (searchTarget == thingToAdd)
                    return retVal;
            }
        }
        if (searchTarget is not null) retVal.Clear();
        return retVal;
    }

    /// <summary>
    /// Updates the last-fired time on a Thing
    /// </summary>
    public void Fire()
    {
        LastFiredTime = DateTime.Now;
        //useCount++;
    }


    //RELATIONSHIPS
    //TODO reverse the parameters so it's type,target
    /// <summary>
    /// Adds a relationship to a Thing if it does not already exist.  The Thing is the source of the relationship.
    /// </summary>
    /// <param name="target">Target Thing</param>
    /// <param name="relationshipType">RelatinoshipType Thing</param>
    /// <returns>the new or existing Thing</returns>
    public Cogneme AddRelationship(Cogneme target, Cogneme relationshipType)
    {
        if (relationshipType is null)  //NULL relationship types could be allowed in search Thingys Parameter?
        {
            return null;
        }

        //does the relationship already exist?
        Cogneme r = HasRelationship(target, relationshipType);
        if (r is not null)
        {
            //AdjustRelationship(r.T);
            return r;
        }
        r = new Cogneme()
        {
            RelType = relationshipType,
            Source = this,
            Target = target,
        };
        if (target is not null && relationshipType is not null)
        {
            lock (_relationships)
                lock (target._relationshipsFrom)
                    lock (relationshipType.relationshipsAsType)
                    {
                        RelationshipsWriteable.Add(r);
                        target.RelationshipsFromWriteable.Add(r);
                        if (!relationshipType.RelationshipsAsTypeWriteable.Contains(r))
                            relationshipType.RelationshipsAsTypeWriteable.Add(r);
                    }
        }
        else if (relationshipType is null)
        {
            lock (_relationships)
            {
                RelationshipsWriteable.Add(r);
            }
        }
        else if (relationshipType is not null)
        {
            lock (_relationships)
                lock (relationshipType.relationshipsAsType)
                {
                    RelationshipsWriteable.Add(r);
                    if (!relationshipType.RelationshipsAsTypeWriteable.Contains(r))
                        relationshipType.RelationshipsAsTypeWriteable.Add(r);
                }
        }
        return r;
    }

    public void RemoveRelationships(Cogneme relationshipType)
    {
        for (int i = 0; i < _relationships.Count; i++)
        {
            Cogneme r = _relationships[i];
            if (r.Source == this && r.RelType == relationshipType)
            {
                RemoveRelationship(r);
                i--;
            }
        }
    }

    //TODO reverse the parameters so it's type,target
    private Cogneme HasRelationship(Cogneme target, Cogneme relationshipType)
    {
        foreach (Cogneme r in _relationships)
        {
            if (r.Source == this && r.Target == target && r.RelType == relationshipType)
                return r;
        }
        return null;
    }

    /// <summary>
    /// Removes a relationship. 
    /// </summary>
    /// <param name="r">The Thing's source neede not be this Thing</param>
    public void RemoveRelationship(Cogneme r)
    {
        if (r is null) return;
        if (r.RelType is null) return;
        if (r.Source is null)
        {
            lock (r.RelType.RelationshipsFromWriteable)
            {
                lock (r.Target.RelationshipsFromWriteable)
                {
                    r.RelType.RelationshipsFromWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                    r.Target.RelationshipsFromWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                }
            }
        }
        else if (r.Target is null)
        {
            lock (r.Source.RelationshipsWriteable)
            {
                lock (r.RelType.RelationshipsFromWriteable)
                {
                    r.Source.RelationshipsWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target); ;
                    r.RelType.RelationshipsFromWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                }
            }
        }
        else
        {
            lock (r.Source.RelationshipsWriteable)
            {
                lock (r.RelType.RelationshipsFromWriteable)
                {
                    lock (r.Target.RelationshipsFromWriteable)
                    {
                        r.Source.RelationshipsWriteable.Remove(r);
                        r.RelType.RelationshipsFromWriteable.Remove(r);
                        r.Target.RelationshipsFromWriteable.Remove(r);
                        //r.Source.RelationshipsWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                        //r.RelType.RelationshipsFromWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                        //r.Target.RelationshipsFromWriteable.RemoveAll(x => x.Source == r.Source && x.RelType == r.RelType && x.Target == r.Target);
                    }
                }
            }
        }
    }

    public Cogneme HasRelationship(Cogneme source, Cogneme relType, Cogneme targett)
    {
        if (source is null && relType is null && targett is null) return null;
        foreach (Cogneme r in Relationships)
            if ((source is null || r.Source == source) &&
                (relType is null || r.RelType == relType) &&
                (targett is null || r.Target == targett)) return r;
        return null;
    }


    public Cogneme RemoveRelationship(Cogneme t2, Cogneme relationshipType)
    {
        Cogneme r = new() { Source = this, RelType = relationshipType, Target = t2 };
        RemoveRelationship(r);
        return r;
    }

    /// <summary>
    /// Addsa a parent to a Thing
    /// </summary>
    /// <param name="newParent"></param>
    public Cogneme AddParent(Cogneme newParent)
    {
        if (newParent is null) return null;
        if (!Parents.Contains(newParent))
        {
            //newParent.AddRelationship(this, IsA);
            return AddRelationship(newParent, IsA);
        }
        return Relationships.FindFirst(x => x.Target == newParent && x.RelType == IsA);
    }

    /// <summary>
    /// Remove a parent from a Thing
    /// </summary>
    /// <param name="t">If the Thing is not a parent, the function does nothing</param>
    public void RemoveParent(Cogneme t)
    {
        Cogneme r = new() { Source = this, RelType = IsA, Target = t };
        t.RemoveRelationship(r);
    }


    public void RemoveChild(Cogneme t)
    {
        Cogneme r = new() { Source = t, RelType = IsA, Target = this };
        RemoveRelationship(r);
    }


    public List<Cogneme> GetAttributes()
    {
        List<Cogneme> retVal = new();
        foreach (Cogneme r in Relationships)
        {
            if (r.RelType.Label != "hasAttribute" && r.RelType.Label != "is") continue;
            retVal.Add(r.Target);
        }
        return retVal;
    }


    public bool HasProperty(Cogneme t)  //with inheritance
    {
        foreach (Cogneme r in Relationships)
            if (r?.RelType.Label == "hasProperty" && r.Target == t)
                return true;
        foreach (Cogneme t1 in Parents) //handle inheritance 
        {
            return t1.HasProperty(t);
        }
        return false;
    }


    private void AddToTransientList()
    {
        if (!UKS.transientRelationships.Contains(this))
            UKS.transientRelationships.Add(this);
    }

}
