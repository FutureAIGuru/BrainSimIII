using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Xml;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Creates a Relationship. <br/>
    /// Parameters are strings. If the Things with those labels
    /// do not exist, they will be created. <br/>
    /// If the RelationshipType has an inverse, the inverse will be used and the Relationship will be reversed so that 
    /// Fido Is-a Dog become Dog Has-child Fido.<br/>
    /// </summary>
    /// <param name="sSource">string or Thing</param>
    /// <param name="sRelationshipType">string or Thing</param>
    /// <param name="sTarget">string or Thing (or null)</param>
    /// <param name="isStatement">Boolean indicating if this is a true statement or part of a conditional</param>
    /// <returns>The primary relationship which was created (others may be created for given attributes</returns>
    public Relationship AddStatement(string sSource, string sRelationshipType, string sTarget)
    {
        Thing source = ThingFromObject(sSource);
        Thing relationshipType = ThingFromObject(sRelationshipType, "RelationshipType", source);
        Thing target = ThingFromObject(sTarget);

        Relationship theRelationship = AddStatement(source, relationshipType, target);
        return theRelationship;
    }
    /// <summary>
    /// Adds a relationship between the specified source, relationship type, and target. No new Things are created.
    /// </summary>
    /// <remarks>If a relationship with the same source, relationship type, and target already exists, the existing
    /// relationship  is returned after being activated. Otherwise, a new relationship is created and added. If the
    /// relationship type  has the "isCommutative" property, a reverse relationship is also created. Additionally, any
    /// extraneous parent  relationships for the source, target, or relationship type are cleared.</remarks>
    /// <param name="source">The source <see cref="Thing"/> of the relationship. Cannot be <see langword="null"/>.</param>
    /// <param name="relType">The relationship type <see cref="Thing"/>. Cannot be <see langword="null"/>.</param>
    /// <param name="target">The target <see cref="Thing"/> of the relationship.</param>
    /// <returns>The created or existing <see cref="Relationship"/> object that represents the relationship.  Returns <see
    /// langword="null"/> if <paramref name="source"/> or <paramref name="relType"/> is <see langword="null"/>.</returns>
    public Relationship AddStatement(Thing source, Thing relType, Thing target)
    {
        if (source == null || relType == null) return null;

        //create the relationship but don't add it to the UKS
        Relationship r = CreateTheRelationship(source, relType, target);

        //does this relationship already exist (without conditions)?
        Relationship existing = GetRelationship(r);
        if (existing != null && existing.Relationships.Count == 0)
        {
            WeakenConflictingRelationships(source, existing);
            existing.Fire();
            return existing;
        }

        WeakenConflictingRelationships(source, r);

        WriteTheRelationship(r);
        if (r.RelType != null && HasProperty(r.RelType, "isCommutative"))
        {
            Relationship rReverse = new Relationship(r);
            (rReverse.Source, rReverse.Target) = (rReverse.Target, rReverse.Source);
            //rReverse.Clauses.Clear();
            WriteTheRelationship(rReverse);
        }

        //if this is adding a child relationship, remove any unknownObject parent
        ClearExtraneousParents(r.Source);
        ClearExtraneousParents(r.Target);
        ClearExtraneousParents(r.RelType);

        return r;
    }


    //these are used by the subclass searching system to report back the closest match and what attributes are missing
    public Relationship CreateTheRelationship(
      object oSource, object oRelationshipType, object oTarget)
    {
        //Debug.WriteLine(oSource.ToString()+" "+oRelationshipType.ToString()+" "+oTarget.ToString());
        Thing source = ThingFromObject(oSource);
        Thing relationshipType = ThingFromObject(oRelationshipType, "RelationshipType", source);
        Thing target = ThingFromObject(oTarget);


        Relationship theRelationship = CreateTheRelationship(ref source, ref relationshipType, ref target);
        return theRelationship;
    }

    public Relationship CreateTheRelationship(ref Thing source, ref Thing relType, ref Thing target)
    {
        Thing inverseType1 = CheckForInverse(relType);
        //if this relationship has an inverse, switcheroo so we are storing consistently in one direction
        if (inverseType1 != null)
        {
            (source, target) = (target, source);
            relType = inverseType1;
        }

        //CREATE new subclasses if needed

        Relationship r = new Relationship()
        { Source = source, RelType = relType, Target = target };

        r.Source?.Fire();
        r.Target?.Fire();
        r.RelType?.Fire();

        return r;
    }

    private void WeakenConflictingRelationships(Thing newSource, Relationship newRelationship)
    {
        //does this new relationship conflict with an existing relationship)?
        for (int i = 0; i < newSource?.Relationships.Count; i++)
        {
            Relationship existingRelationship = newSource.Relationships[i];
            if (existingRelationship == newRelationship)
            {
                //strengthen this relationship
                newRelationship.Weight += (1 - newRelationship.Weight) / 2.0f;
                newRelationship.Fire();
            }
            else if (RelationshipsAreExclusive(newRelationship, existingRelationship))
            {
                //special cases for "not" so we delete rather than weakening
                if (newRelationship.RelType.Children.Contains(existingRelationship.RelType) && HasAttribute(existingRelationship.RelType, "not"))
                {
                    Thing after = GetOrAddThing("AFTER", "ClauseType");
                    AddClause(newRelationship, "AFTER", existingRelationship);
                    existingRelationship.Source.RemoveRelationship(existingRelationship);
                    //newRelationship.AddClause(after, existingRelationship);
                    //                    newSource.RemoveRelationship(existingRelationship);
                    //                    i--;
                }
                else if (existingRelationship.RelType.Children.Contains(newRelationship.RelType) && HasAttribute(newRelationship.RelType, "not"))
                {
                    Thing after = GetOrAddThing("AFTER", "ClauseType");
                    AddClause(newRelationship, "AFTER", existingRelationship);
                    existingRelationship.Source.RemoveRelationship(existingRelationship);

                    //newRelationship.AddClause(after, existingRelationship);
                    //                    newSource.RemoveRelationship(existingRelationship);
                    //                    i--;
                }
                else
                {
                    if (newRelationship.Weight == 1 && existingRelationship.Weight == 1)
                        existingRelationship.Weight = .5f;
                    else
                        existingRelationship.Weight = Math.Clamp(existingRelationship.Weight - .2f, -1, 1);
                    if (existingRelationship.Weight <= 0)
                    {
                        newSource.RemoveRelationship(existingRelationship);
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
        if (reconnectNeeded && !t.HasAncestor("Thing"))
            t.AddParent(ThingLabels.GetThing("unknownObject"));
    }

    public Thing SubclassExists(Thing t, List<Thing> thingAttributes, ref Thing bestMatch, ref List<Thing> missingAttributes)
    {
        //TODO this doesn't work as needed if some attributes are inherited from an ancestor
        if (t == null) return null;

        bestMatch = t;
        missingAttributes = thingAttributes;
        //there are no attributes specified
        if (thingAttributes.Count == 0) return t;

        List<Thing> attrs = new List<Thing>(thingAttributes);

        //get the attributes of t
        //var existingRelationships = GetAllRelationships(new List<Thing> { t }, false);
        var existingRelationships = t.Relationships;
        foreach (Relationship r in existingRelationships)
        {
            if (attrs.Contains(r.Target)) attrs.Remove(r.Target);
            if (attrs.Contains(r.RelType)) attrs.Remove(r.RelType);
        }

        //t already has these attributes
        if (attrs.Count == 0)
            return t;

        //attrs now contains the remaing attributes we need to find in a descendent
        //bestMatch = null;
        //missingAttributes = new List<Thing>();
        return ChildHasAllAttributes(t, attrs, ref bestMatch, ref missingAttributes);
    }

    List<Thing> GetDirectAttributes(Thing t)
    {
        List<Thing> retVal = new();
        foreach (Relationship r in t.Relationships)
        {
            if (r.RelType.Label == "is")
                retVal.Add(r.Target);
        }
        return retVal;
    }
    private Thing ChildHasAllAttributes(Thing t, List<Thing> attrs, ref Thing bestMatch, ref List<Thing> missingAttributes, List<Thing> alreadyVisited = null)
    {
        //circular reference protection
        if (alreadyVisited == null) alreadyVisited = new List<Thing>();
        if (alreadyVisited.Contains(t)) return null;
        alreadyVisited.Add(t);

        //Localattrs lets us remove attrs from the required list without clobbering the parent list
        List<Thing> localAttrs = new List<Thing>(attrs);
        foreach (Thing child in t.Children)
        {
            List<Thing> childAttrs = GetDirectAttributes(child);
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
            Thing retVal = ChildHasAllAttributes(child, localAttrs, ref bestMatch, ref missingAttributes, alreadyVisited);
            if (retVal != null)
                return retVal;
            localAttrs = new List<Thing>(attrs);
        }
        return null;
    }

    public Thing CreateInstanceOf(Thing t)
    {
        return CreateSubclass(t, new List<Thing>());
    }
    Thing CreateSubclass(Thing t, List<Thing> attributes)
    {
        if (t == null) return null;
        //Thing t2 = SubclassExists(t, attributes);
        //if (t2 != null && attributes.Count != 0) return t2;

        string newLabel = t.Label;
        foreach (Thing t1 in attributes)
        {
            newLabel += ((t1.Label.StartsWith(".")) ? "" : ".") + t1.Label;
        }
        //create the new thing which is child of the original
        Thing retVal = AddThing(newLabel, t);
        //add the attributes
        foreach (Thing t1 in attributes)
        {
            Relationship r1 = new Relationship()
            { Source = retVal, RelType = ThingLabels.GetThing("is"), Target = t1 };
            WriteTheRelationship(r1);
        }
        return retVal;
    }

    private Thing CheckForInverse(Thing relationshipType)
    {
        if (relationshipType == null) return null;
        Relationship inverse = relationshipType.Relationships.FindFirst(x => x.RelType.Label == "inverseOf");
        if (inverse != null) return inverse.Target;
        //use the below if inverses are 2-way.  Without this, there is a one-way translation
        //inverse = relationshipType.RelationshipsBy.FindFirst(x => x.reltype.Label == "inverseOf");
        //if (inverse != null) return inverse.source;
        return null;
    }
    private static List<Thing> FindCommonParents(Thing t, Thing t1)
    {
        List<Thing> commonParents = new List<Thing>();
        foreach (Thing p in t.Parents)
            if (t1.Parents.Contains(p))
                commonParents.Add(p);
        return commonParents;
    }
    public static void WriteTheRelationship(Relationship r)
    {
        if (r.Source == null && r.Target == null) return;
        if (r.RelType == null) return;
        if (r.Target == null)
        {
            lock (r.Source.RelationshipsWriteable)
                lock (r.RelType.RelationshipsFromWriteable)
                {
                    if (!r.Source.RelationshipsWriteable.Contains(r))
                        r.Source.RelationshipsWriteable.Add(r);
                    if (!r.RelType.RelationshipsAsTypeWriteable.Contains(r))
                        r.RelType.RelationshipsAsTypeWriteable.Add(r);
                }
        }
        else if (r.Source == null)
        {
            lock (r.Target.RelationshipsWriteable)
                lock (r.RelType.RelationshipsFromWriteable)
                {
                    if (!r.Target.RelationshipsWriteable.Contains(r))
                        r.Target.RelationshipsFromWriteable.Add(r);
                    if (!r.RelType.RelationshipsAsTypeWriteable.Contains(r))
                        r.RelType.RelationshipsAsTypeWriteable.Add(r);
                }
        }
        else
        {
            lock (r.Source.RelationshipsWriteable)
                lock (r.Target.RelationshipsFromWriteable)
                    lock (r.RelType.RelationshipsFromWriteable)
                    {
                        if (!r.Source.RelationshipsWriteable.Contains(r))
                            r.Source.RelationshipsWriteable.Add(r);
                        if (!r.Target.RelationshipsWriteable.Contains(r))
                            r.Target.RelationshipsFromWriteable.Add(r);
                        if (!r.RelType.RelationshipsAsTypeWriteable.Contains(r))
                            r.RelType.RelationshipsAsTypeWriteable.Add(r);

                    }
        }
    }
}
