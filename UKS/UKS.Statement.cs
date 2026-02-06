using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Xml;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Creates a Thought. <br/>
    /// Parameters are strings. If the Thoughts with those labels
    /// do not exist, they will be created. <br/>
    /// If the LinkType has an inverse, the inverse will be used and the Thought will be reversed so that 
    /// Fido Is-a Dog become Dog Has-child Fido.<br/>
    /// </summary>
    /// <param name="sSource">string or Thought</param>
    /// <param name="sLinkType">string or Thought</param>
    /// <param name="sTarget">string or Thought (or null)</param>
    /// <param name="isStatement">Boolean indicating if this is a true statement or part of a conditional</param>
    /// <returns>The primary link which was created (others may be created for given attributes</returns>
    public Thought AddStatement(string sSource, string sLinkType, string sTarget, string label = "")
    {
        Thought source = ThoughtFromObject(sSource);
        Thought linkType = ThoughtFromObject(sLinkType, "LinkType", source);
        Thought target = ThoughtFromObject(sTarget);

        Thought theLink = AddStatement(source, linkType, target, label);
        return theLink;
    }
    /// <summary>
    /// Adds a statement relating the specified source, link type, and target. No new Thoughts are created.
    /// </summary>
    /// <remarks>If a link with the same source, link type, and target already exists, the existing
    /// link  is returned after being activated. Otherwise, a new link is created and added. If the
    /// link type  has the "isCommutative" property, a reverse link is also created. Additionally, any
    /// extraneous parent  links for the source, target, or link type are cleared.</remarks>
    /// <param name="source">The source <see cref="Thought"/> of the link. Cannot be <see langword="null"/>.</param>
    /// <param name="linkType">The link type <see cref="Thought"/>. Cannot be <see langword="null"/>.</param>
    /// <param name="target">The target <see cref="Thought"/> of the link.</param>
    /// <returns>The created or existing <see cref="Thought"/> object that represents the link.  Returns <see
    /// langword="null"/> if <paramref name="source"/> or <paramref name="linkType"/> is <see langword="null"/>.</returns>
    public Thought AddStatement(Thought source, Thought linkType, Thought target, string label = "")
    {
        if (source is null || linkType is null) return null;

        Thought existing = Labeled(label);
        Thought r = null; 

        if (existing is null)
        {        //create the link but don't add it to the UKS
            r = CreateTheLink(source, linkType, target);
            existing = GetLink(r);
        }
        else
        {
            existing.LinkType = linkType;
            existing.To = target;
            existing.From = source;
        }

        //does this link already exist (without conditions)?
        if (existing is not null)
        {
            WeakenConflictingLinks(source, existing);
            existing.Fire();
            return existing;
        }
        if (r?.From?.Label == "") r.From.AddToUKS();
        if (r?.To?.Label == "") r.To.AddToUKS();
        if (!string.IsNullOrEmpty(label))
            r.Label = label.Trim();

        WeakenConflictingLinks(source, r);

        WriteTheLink(r);
        if (r.LinkType is not null && HasProperty(r.LinkType, "isCommutative"))
        {
            Thought rReverse = new Thought(r);
            (rReverse.From, rReverse.To) = (rReverse.To, rReverse.From);
            //rReverse.Clauses.Clear();
            WriteTheLink(rReverse);
        }

        //if this is adding a child link, remove any Unknown parent
        ClearExtraneousParents(r.From);
        ClearExtraneousParents(r.To);
        ClearExtraneousParents(r.LinkType);

        return r;
    }


    //these are used by the subclass searching system to report back the closest match and what attributes are missing
    public Thought CreateTheLink(
      object oSource, object oLinkType, object oTarget)
    {
        //Debug.WriteLine(oSource.ToString()+" "+oLinkType.ToString()+" "+oTarget.ToString());
        Thought source = ThoughtFromObject(oSource);
        Thought linkType = ThoughtFromObject(oLinkType, "LinkType", source);
        Thought target = ThoughtFromObject(oTarget);


        Thought theLink = CreateTheLink(ref source, ref linkType, ref target);
        return theLink;
    }

    public Thought CreateTheLink(ref Thought source, ref Thought linkType, ref Thought target)
    {
        Thought inverseType1 = CheckForInverse(linkType);
        //if this link has an inverse, switcheroo so we are storing consistently in one direction
        if (inverseType1 is not null)
        {
            (source, target) = (target, source);
            linkType = inverseType1;
        }

        //CREATE new subclasses if needed

        Thought r = new Thought()
        { From = source, LinkType = linkType, To = target };

        r.From?.Fire();
        r.To?.Fire();
        r.LinkType?.Fire();

        return r;
    }

    private void WeakenConflictingLinks(Thought newSource, Thought newLink)
    {
        //does this new link conflict with an existing link)?
        for (int i = 0; i < newSource?.LinksTo.Count; i++)
        {
            Thought existingLink = newSource.LinksTo[i];
            if (existingLink == newLink)
            {
                //strengthen this link
                newLink.Weight += (1 - newLink.Weight) / 2.0f;
                newLink.Fire();
            }
            else if (LinksAreExclusive(newLink, existingLink))
            {
                //special cases for "not" so we delete rather than weakening
                if (newLink.LinkType.Children.Contains(existingLink.LinkType) && HasAttribute(existingLink.LinkType, "not"))
                {
                    AddStatement(newLink, "AFTER", existingLink);
                    existingLink.From.RemoveLink(existingLink);
                }
                else if (existingLink.LinkType.Children.Contains(newLink.LinkType) && HasAttribute(newLink.LinkType, "not"))
                {
                    AddStatement(newLink, "AFTER", existingLink);
                    existingLink.From.RemoveLink(existingLink);
                }
                else
                {
                    if (newLink.Weight == 1 && existingLink.Weight == 1)
                        existingLink.Weight = .5f;
                    else
                        existingLink.Weight = Math.Clamp(existingLink.Weight - .2f, -1, 1);
                    if (existingLink.Weight <= 0)
                    {
                        newSource.RemoveLink(existingLink);
                        i--;
                    }
                }
            }
        }
    }

    void ClearExtraneousParents(Thought t)
    {
        if (t is null) return;

        bool reconnectNeeded = t.HasAncestorLabeled("Thought");
        //if a thought has more than one parent and one of them is unkonwn, 
        //then the Unknown link is unnecessary
        if (t.Parents.Count > 1)
            t.RemoveParent(ThoughtLabels.GetThought("Unknown"));
        //if this disconnects the Thought from the tree, reconnect it as a Unknown
        //this may happen in the case of a circular reference.
        if (reconnectNeeded && !t.HasAncestor("Thought"))
            t.AddParent(ThoughtLabels.GetThought("Unknown"));
    }

    public Thought SubclassExists(Thought t, List<Thought> thoughtAttributes, ref Thought bestMatch, ref List<Thought> missingAttributes)
    {
        //TODO this doesn't work as needed if some attributes are inherited from an ancestor
        if (t is null) return null;

        bestMatch = t;
        missingAttributes = thoughtAttributes;
        //there are no attributes specified
        if (thoughtAttributes.Count == 0) return t;

        List<Thought> attrs = new List<Thought>(thoughtAttributes);

        //get the attributes of t
        //var existingLinks = GetAllLinks(new List<Thought> { t }, false);
        var existingLinks = t.LinksTo;
        foreach (Thought r in existingLinks)
        {
            if (attrs.Contains(r.To)) attrs.Remove(r.To);
            if (attrs.Contains(r.LinkType)) attrs.Remove(r.LinkType);
        }

        //t already has these attributes
        if (attrs.Count == 0)
            return t;

        //attrs now contains the remaing attributes we need to find in a descendent
        //bestMatch = null;
        //missingAttributes = new List<Thought>();
        return ChildHasAllAttributes(t, attrs, ref bestMatch, ref missingAttributes);
    }

    List<Thought> GetDirectAttributes(Thought t)
    {
        List<Thought> retVal = new();
        foreach (Thought r in t.LinksTo)
        {
            if (r.LinkType.Label == "is")
                retVal.Add(r.To);
        }
        return retVal;
    }
    private Thought ChildHasAllAttributes(Thought t, List<Thought> attrs, ref Thought bestMatch, ref List<Thought> missingAttributes, List<Thought> alreadyVisited = null)
    {
        //circular reference protection
        if (alreadyVisited is null) alreadyVisited = new List<Thought>();
        if (alreadyVisited.Contains(t)) return null;
        alreadyVisited.Add(t);

        //Localattrs lets us remove attrs from the required list without clobbering the parent list
        List<Thought> localAttrs = new List<Thought>(attrs);
        foreach (Thought child in t.Children)
        {
            List<Thought> childAttrs = GetDirectAttributes(child);
            foreach (Thought t3 in childAttrs)
                localAttrs.Remove(t3);

            if (localAttrs.Count == 0) //have all the attributes been found?
                return child;

            if (localAttrs.Count < missingAttributes.Count)
            {
                missingAttributes = new List<Thought>(localAttrs);
                bestMatch = child;
            }
            //search any children with the remaining needed attributes
            Thought retVal = ChildHasAllAttributes(child, localAttrs, ref bestMatch, ref missingAttributes, alreadyVisited);
            if (retVal is not null)
                return retVal;
            localAttrs = new List<Thought>(attrs);
        }
        return null;
    }

    public Thought CreateInstanceOf(Thought t)
    {
        return CreateSubclass(t, new List<Thought>());
    }
    Thought CreateSubclass(Thought t, List<Thought> attributes)
    {
        if (t is null) return null;
        //Thought t2 = SubclassExists(t, attributes);
        //if (t2 is not null && attributes.Count != 0) return t2;

        string newLabel = t.Label;
        foreach (Thought t1 in attributes)
        {
            newLabel += ((t1.Label.StartsWith(".")) ? "" : ".") + t1.Label;
        }
        //create the new thought which is child of the original
        Thought retVal = AddThought(newLabel, t);
        //add the attributes
        foreach (Thought t1 in attributes)
        {
            Thought r1 = new Thought()
            { From = retVal, LinkType = ThoughtLabels.GetThought("is"), To = t1 };
            WriteTheLink(r1);
        }
        return retVal;
    }

    private Thought CheckForInverse(Thought linkType)
    {
        if (linkType is null) return null;
        Thought inverse = linkType.LinksTo.FindFirst(x => x.LinkType.Label == "inverseOf");
        if (inverse is not null) return inverse.To;
        //use the below if inverses are 2-way.  Without this, there is a one-way translation
        //inverse = linkType.LinksBy.FindFirst(x => x.linktype.Label == "inverseOf");
        //if (inverse is not null) return inverse.source;
        return null;
    }
    private static List<Thought> FindCommonParents(Thought t, Thought t1)
    {
        List<Thought> commonParents = new List<Thought>();
        foreach (Thought p in t.Parents)
            if (t1.Parents.Contains(p))
                commonParents.Add(p);
        return commonParents;
    }
    public static void WriteTheLink(Thought r)
    {
        if (r.From is null && r.To is null) return;
        if (r.LinkType is null) return;
        if (r.To is null)
        {
            lock (r.From.LinksWriteable)
                lock (r.LinkType.LinksFromWriteable)
                {
                    if (!r.From.LinksWriteable.Contains(r))
                        r.From.LinksWriteable.Add(r);
                    if (!r.LinkType.LinksAsTypeWriteable.Contains(r))
                        r.LinkType.LinksAsTypeWriteable.Add(r);
                }
        }
        else if (r.From is null)
        {
            lock (r.To.LinksWriteable)
                lock (r.LinkType.LinksFromWriteable)
                {
                    if (!r.To.LinksWriteable.Contains(r))
                        r.To.LinksFromWriteable.Add(r);
                    if (!r.LinkType.LinksAsTypeWriteable.Contains(r))
                        r.LinkType.LinksAsTypeWriteable.Add(r);
                }
        }
        else
        {
            lock (r.From.LinksWriteable)
                lock (r.To.LinksFromWriteable)
                    lock (r.LinkType.LinksFromWriteable)
                    {
                        if (!r.From.LinksWriteable.Contains(r))
                            r.From.LinksWriteable.Add(r);
                        if (!r.To.LinksWriteable.Contains(r))
                            r.To.LinksFromWriteable.Add(r);
                        if (!r.LinkType.LinksAsTypeWriteable.Contains(r))
                            r.LinkType.LinksAsTypeWriteable.Add(r);

                    }
        }
    }
}
