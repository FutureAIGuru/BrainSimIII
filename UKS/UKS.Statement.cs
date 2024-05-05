using System;
using System.Collections.Generic;

namespace UKS;

public partial class UKS
{
    //this overload lets you pass in
    //a string or a thing for the first three parameters
    //and a string, Thing, or list of string or Thing for the last 3
    public Relationship AddStatement(
        object oSource, object oRelationshipType, object oTarget,
        object oSourceProperties = null,
        object oTypeProperties = null,
        object oTargetProperties = null
                    )
    {
//        try
        {
            Thing source = ThingFromObject(oSource);
            Thing relationshipType = ThingFromObject(oRelationshipType, "RelationshipType", source);
            Thing target = ThingFromObject(oTarget);

            List<Thing> sourceModifiers = ThingListFromObject(oSourceProperties);
            List<Thing> relationshipTypeModifiers = ThingListFromObject(oTypeProperties, "Action");
            List<Thing> targetModifiers = ThingListFromObject(oTargetProperties);

            Relationship theRelationship = AddStatement(source, relationshipType, target, sourceModifiers, relationshipTypeModifiers, targetModifiers);
            return theRelationship;
        }
//        catch (Exception ex)
//        {
//            return null;
//        }
    }

    public Relationship AddStatement(
                    Thing source, Thing relType, Thing target,
                    List<Thing> sourceProperties,
                    List<Thing> typeProperties,
                    List<Thing> targetProperties
            )
    {
        if (source == null || relType == null) return null;

        //this replaces pronouns with antecedents
        ////if (HandlePronouns(r)) return r;

        Relationship r = CreateTheRelationship(ref source, ref relType, ref target, ref sourceProperties, typeProperties, ref targetProperties);

        //does this relationship already exist (without conditions)?
        Relationship existing = Relationship.GetRelationship(r);
        if (existing != null)
        {
            WeakenConflictingRelationships(source, existing);
            existing.Fire();
            return existing;
        }

        WeakenConflictingRelationships(source, r);

        WriteTheRelationship(r);
        if (r.relType != null && HasProperty(r.relType, "isCommutative"))
        {
            Relationship rReverse = new Relationship(r);
            (rReverse.source, rReverse.target) = (rReverse.target, rReverse.source);
            rReverse.Clauses.Clear();
            WriteTheRelationship(rReverse);
        }

        //if this is adding a child relationship, remove any unknownObject parent
        ClearExtraneousParents(r.source);
        ClearExtraneousParents(r.T);
        ClearExtraneousParents(r.relType);
        ClearRedundancyInAncestry(r.target);

        return r;
    }

    void ClearRedundancyInAncestry(Thing t)
    {
        if (t == null) return;
        //if a direct parent has an ancestor which is also another direct parent, remove that 2nd direct parent
        var parents = t.Parents;
        foreach (Thing parent in parents)
        {
            var p = parent.AncestorList();
            p.RemoveAt(0); //don't check yourself
            foreach (Thing ancestor in p)
            {
                if (parents.Contains(ancestor))
                    t.RemoveParent(ancestor);
            }
        }
    }

    //these are used by the subclass searching system to report back the closest match and what attributes are missing
    private Thing bestMatch = null;
    private List<Thing> missingAttributes;

    public Relationship CreateTheRelationship(ref Thing source, ref Thing relType, ref Thing target,
        ref List<Thing> sourceProperties, List<Thing> typeProperties, ref List<Thing> targetProperties)
    {
        Thing inverseType1 = CheckForInverse(relType);
        //if this relationship has an inverse, switcheroo so we are storing consistently in one direction
        if (inverseType1 != null)
        {
            (source, target) = (target, source);
            (sourceProperties, targetProperties) = (targetProperties, sourceProperties);
            relType = inverseType1;
        }

        //CAUTION: this code is not multithreadable
        //CREATE new subclasses if needed
        Thing source1 = SubclassExists(source, sourceProperties);
        if (source1 == null)
        {
            if (bestMatch == null) bestMatch = source;
            if (missingAttributes.Count == 0) missingAttributes = new List<Thing>(sourceProperties);
            source1 = CreateSubclass(bestMatch, missingAttributes);
        }
        source = source1;

        Thing target1 = SubclassExists(target, targetProperties);
        if (target1 == null)
        {
            if (bestMatch == null) bestMatch = target;
            if (missingAttributes.Count == 0) missingAttributes = new List<Thing>(targetProperties);
            target1 = CreateSubclass(bestMatch, missingAttributes);
        }
        target = target1;

        Thing relType1 = SubclassExists(relType, typeProperties);
        if (relType1 == null)
        {
            if (bestMatch == null) bestMatch = relType;
            if (missingAttributes.Count == 0) missingAttributes = new List<Thing>(typeProperties);
            relType1 = CreateSubclass(bestMatch, missingAttributes);
        }
        relType = relType1;

        Relationship r = new Relationship()
        { source = source, reltype = relType, target = target };

        r.source?.SetFired();
        r.target?.SetFired();
        r.relType?.SetFired();

        return r;
    }

    private void WeakenConflictingRelationships(Thing newSource, Relationship existingRelationship)
    {
        //does this new relationship conflict with an existing relationship)?
        for (int i = 0; i < newSource?.Relationships.Count; i++)
        {
            Relationship sourceRel = newSource.Relationships[i];
            if (sourceRel == existingRelationship)
            {
                //strengthen this relationship
                existingRelationship.Weight += (1 - existingRelationship.Weight) / 2.0f;
                existingRelationship.Fire();
            }
            else if (RelationshipsAreExclusive(existingRelationship, sourceRel))
            {
                //special cases for "not" so we delete rather than weakening
                if (existingRelationship.reltype.Children.Contains(sourceRel.relType) && HasAttribute(sourceRel.relType, "not"))
                {
                    newSource.RemoveRelationship(sourceRel);
                    i--;
                }
                if (sourceRel.reltype.Children.Contains(existingRelationship.relType) && HasAttribute(existingRelationship.relType, "not"))
                {
                    newSource.RemoveRelationship(sourceRel);
                    i--;
                }
                else
                {
                    if (existingRelationship.Weight == 1 && sourceRel.Weight == 1)
                        sourceRel.Weight = .5f;
                    else
                        sourceRel.Weight = Math.Clamp(sourceRel.Weight - .2f, -1, 1);
                    if (sourceRel.Weight <= 0)
                    {
                        newSource.RemoveRelationship(sourceRel);
                        i--;
                    }
                }
            }
        }
    }

    void ClearExtraneousParents(Thing t)
    {
        if (t == null) return;

        bool reconnectNeeded = t.HasAncestorLabeled("Thing");
        //if a thing has more than one parent and one of them is unkonwnObject, 
        //then the unknownObject relationship is unnecessary
        if (t.Parents.Count > 1)
            t.RemoveParent(ThingLabels.GetThing("unknownObject"));
        //if this disconnects the Thing from the tree, reconnect it as a Unknown
        //this may happen in the case of a circular reference.
        if (reconnectNeeded && !t.HasAncestor(ThingLabels.GetThing("Thing")))
            t.AddParent(ThingLabels.GetThing("unknownObject"));
    }

    public Thing SubclassExists(Thing t, List<Thing> thingAttributes)
    {
        //TODO this doesn't work as needed if some attributes are inherited from an ancestor
        if (t == null) return null;

        bestMatch = t;
        missingAttributes = thingAttributes;
        //there are no attributes specified
        if (thingAttributes.Count == 0) return t;

        List<Thing> attrs = new List<Thing>(thingAttributes);

        //get the attributes of t
        var existingRelationships = GetAllRelationships(new List<Thing> { t }, false);
        foreach (Relationship r in existingRelationships)
        {
            if (attrs.Contains(r.target)) attrs.Remove(r.target);
            if (attrs.Contains(r.relType)) attrs.Remove(r.relType);
        }

        //t already has these attributes
        if (attrs.Count == 0)
            return t;

        //attrs now contains the remaing attributes we need to find in a descendent
        bestMatch = null;
        missingAttributes = new List<Thing>();
        return ChildContainsAttrs(t, attrs);
    }

    private Thing ChildContainsAttrs(Thing t, List<Thing> attrs, List<Thing> alreadyVisited = null)
    {
        //circular reference protection
        if (alreadyVisited == null) alreadyVisited = new List<Thing>();
        if (alreadyVisited.Contains(t)) return null;

        //Localattrs lets us remove attrs from the required list without clobbering the parent list
        List<Thing> localAttrs = new List<Thing>(attrs);
        foreach (Thing child in t.Children)
        {
            List<Thing> childAttrs = GetAllAttributes(child);
            foreach (Thing t3 in childAttrs)
                localAttrs.Remove(t3);

            if (localAttrs.Count == 0) //have all the attributes been found?
                return child;

            if (localAttrs.Count < missingAttributes.Count)
            {
                missingAttributes = new List<Thing>(localAttrs);
                bestMatch = child;
            }
            //search any children with the remaining needed attributes
            Thing retVal = ChildContainsAttrs(child, localAttrs);
            if (retVal != null)
                return retVal;
            localAttrs = new List<Thing>(attrs);
        }
        alreadyVisited.Add(t);
        return null;
    }

    public Thing CreateInstanceOf(Thing t)
    {
        return CreateSubclass(t, new List<Thing>());
    }
    Thing CreateSubclass(Thing t, List<Thing> attributes)
    {
        if (t == null) return null;
        Thing t2 = SubclassExists(t, attributes);
        if (t2 != null) return t2;

        string newLabel = t.Label;
        foreach (Thing t1 in attributes)
        {
            newLabel += "." + t1.Label;
        }
        //create the new thing which is child of the original
        Thing retVal = GetOrAddThing(newLabel, t);
        //add the attributes
        foreach (Thing t1 in attributes)
        {
            Relationship r1 = new Relationship()
            { source = retVal, reltype = ThingLabels.GetThing("is"), target = t1 };
            WriteTheRelationship(r1);
        }
        return retVal;
    }

    private Thing CheckForInverse(Thing relationshipType)
    {
        if (relationshipType == null) return null;
        Relationship inverse = relationshipType.Relationships.FindFirst(x => x.reltype.Label == "inverseOf");
        if (inverse != null) return inverse.target;
        //use the bwlow if inverses are 2-way.  Without this, there is a one-way translation
        //inverse = relationshipType.RelationshipsBy.FindFirst(x => x.reltype.Label == "inverseOf");
        //if (inverse != null) return inverse.source;
        return null;
    }
    public static List<Thing> FindCommonParents(Thing t, Thing t1)
    {
        List<Thing> commonParents = new List<Thing>();
        foreach (Thing p in t.Parents)
            if (t1.Parents.Contains(p))
                commonParents.Add(p);
        return commonParents;
    }
    public static void WriteTheRelationship(Relationship r)
    {
        if (r.source == null && r.target == null) return;
        if (r.reltype == null) return;
        if (r.target == null)
        {
            lock (r.source.RelationshipsWriteable)
                lock (r.relType.RelationshipsFromWriteable)
                {
                    if (!r.source.RelationshipsWriteable.Contains(r))
                        r.source.RelationshipsWriteable.Add(r);
                    if (!r.reltype.RelationshipsAsTypeWriteable.Contains(r))
                        r.reltype.RelationshipsAsTypeWriteable.Add(r);
                }
        }
        else if (r.source == null)
        {
            lock (r.target.RelationshipsWriteable)
                lock (r.relType.RelationshipsFromWriteable)
                {
                    if (!r.target.RelationshipsWriteable.Contains(r))
                        r.target.RelationshipsFromWriteable.Add(r);
                    if (!r.reltype.RelationshipsAsTypeWriteable.Contains(r))
                        r.reltype.RelationshipsAsTypeWriteable.Add(r);
                }
        }
        else
        {
            lock (r.source.RelationshipsWriteable)
                lock (r.target.RelationshipsFromWriteable)
                    lock (r.relType.RelationshipsFromWriteable)
                    {
                        if (!r.source.RelationshipsWriteable.Contains(r))
                            r.source.RelationshipsWriteable.Add(r);
                        if (!r.target.RelationshipsWriteable.Contains(r))
                            r.target.RelationshipsFromWriteable.Add(r);
                        if (!r.reltype.RelationshipsAsTypeWriteable.Contains(r))
                            r.reltype.RelationshipsAsTypeWriteable.Add(r);
                    }
        }
    }


}
