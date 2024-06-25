using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace UKS;
public partial class UKS
{
    List<Relationship> failedConditions = new();
    List<Relationship> succeededConditions = new();

    /// <summary>
    /// Gets all relationships to a gropu of Things includeing inherited relationships
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="reverse">if true, the first parameter is a list of targets rather than sources</param>
    /// <returns>List of matching relationships</returns>
    public List<Thing> GetAllAttributes(Thing t) //with inheritance, conflicts, etc
    {
        //TODO This is hard-codedd to "is" relationships
        List<Thing> retVal = new();

        var temp1 = BuildSearchList(new List<Thing>() { t });
        var temp2 = GetAllRelationshipsInt(temp1);
        foreach (Relationship r in temp2)
            if (r.relType.Label == "is")
                retVal.Add(r.target);
        return retVal;
    }
    public List<Relationship> GetAllRelationships(List<Thing> sources, bool reverse) //with inheritance, conflicts, etc
    {
        var result1 = BuildSearchList(sources, reverse);
        List<Relationship> result2 = GetAllRelationshipsInt(result1);
        if (result2.Count < 200)
            RemoveConflictingResults(result2);
        RemoveFalseConditionals(result2);
        AlphabetizeRelationships(ref result2);
        return result2;
    }

    private void AlphabetizeRelationships(ref List<Relationship> result2)
    {
        result2 = result2.OrderBy(x => x.ToString()).ToList();
    }


    //This is used to store temporary content during queries
    private class ThingWithQueryParams
    {
        public Thing thing;
        public int hopCount;
        public int haveCount = 1;
        public int hitCount = 1;
        public float weight;
        public Thing reachedWith = null;
        public bool corner = false;
        public override string ToString()
        {
            return (thing.Label + "  : " + hopCount + " : " + weight + "  Count: " +
                haveCount + " Hits: " + hitCount + " Corner: " + corner);
        }
    }

    //TODO This is hardcoded to only follow "has" and "has-child" transitive relationships
    private List<ThingWithQueryParams> BuildSearchList(List<Thing> q, bool reverse = false)
    {

        List<ThingWithQueryParams> thingsToExamine = new();
        int maxHops = 8;
        int hopCount = 0;
        foreach (Thing t in q)
            thingsToExamine.Add(new ThingWithQueryParams
            {
                thing = t,
                hopCount = hopCount,
                weight = 1,
                reachedWith = null
            }); ;
        hopCount++;
        int currentEnd = thingsToExamine.Count;
        for (int i = 0; i < thingsToExamine.Count; i++)
        {
            Thing t = thingsToExamine[i].thing;
            float curWeight = thingsToExamine[i].weight;
            int curCount = thingsToExamine[i].haveCount;
            Thing reachedWith = thingsToExamine[i].reachedWith;

            foreach (Relationship r in t.RelationshipsFrom)  //has-child et al
                if ((r.relType.HasAncestorLabeled("has-child") && !reverse) ||
                    (r.relType.HasAncestorLabeled("has") && reverse))
                {
                    //if there are several relationship, ignore the is-a, it is likely wrong
                    var existingRelationships = GetRelationshipsBetween(r.source, r.target);
                    if (existingRelationships.Count > 1) continue;

                    if (thingsToExamine.FindFirst(x => x.thing == r.source) is ThingWithQueryParams twgp)
                        twgp.hitCount++;//thing is in the list, increment its count
                    else
                    {//thing is not in the list, add it
                        bool corner = !ThingInTree(r.relType, thingsToExamine[i].reachedWith) &&
                            thingsToExamine[i].reachedWith != null;
                        if (corner)
                        { } //corners are the reasons in a logic progression
                        thingsToExamine[i].corner |= corner;
                        ThingWithQueryParams thingToAdd = new ThingWithQueryParams
                        {
                            thing = r.source,
                            hopCount = hopCount,
                            weight = curWeight * r.Weight,
                            reachedWith = r.relType,
                        };
                        thingsToExamine.Add(thingToAdd);
                        //if things have counts, they are multiplied
                        int val = GetCount(r.reltype);
                        thingToAdd.haveCount = curCount * val;
                    }
                }

            foreach (Relationship r in t.Relationships) //has-a et al
                if ((r.relType.HasAncestorLabeled("has-child") && reverse) ||
                    (r.relType.HasAncestorLabeled("has") && !reverse))
                {
                    if (thingsToExamine.FindFirst(x => x.thing == r.target) is ThingWithQueryParams twqp)
                    {
                        twqp.hitCount++;
                    }
                    else
                    {
                        bool corner = !ThingInTree(r.relType, thingsToExamine[i].reachedWith) &&
                            thingsToExamine[i].reachedWith != null;
                        if (corner)
                        { }
                        thingsToExamine[i].corner |= corner;
                        ThingWithQueryParams thingToAdd = new ThingWithQueryParams
                        {
                            thing = r.target,
                            hopCount = hopCount,
                            weight = curWeight * r.Weight,
                            reachedWith = r.relType,
                        };
                        thingsToExamine.Add(thingToAdd);
                        //if things have counts, they are multiplied
                        int val = GetCount(r.reltype);
                        thingToAdd.haveCount = curCount * val;
                    }
                }
            if (i == currentEnd - 1)
            {
                hopCount++;
                currentEnd = thingsToExamine.Count;
                if (hopCount > maxHops) break;
            }
        }
        return thingsToExamine;
    }
    private List<Relationship> GetRelationshipsBetween(Thing t1, Thing t2)
    {
        List<Relationship> retVal = new();
        foreach (Relationship r in t1.Relationships)
            if (r.target == t2) retVal.Add(r);
        foreach (Relationship r in t1.RelationshipsFrom)
            if (r.target == t2) retVal.Add(r);
        foreach (Relationship r in t2.Relationships)
            if (r.target == t1) retVal.Add(r);
        foreach (Relationship r in t2.RelationshipsFrom)
            if (r.target == t1) retVal.Add(r);
        return retVal;
    }
    private List<Relationship> GetAllRelationshipsInt(List<ThingWithQueryParams> thingsToExamine)
    {
        List<Relationship> result = new();
        for (int i = 0; i < thingsToExamine.Count; i++)
        {
            Thing t = thingsToExamine[i].thing;
            if (t == null) continue; //safety
            int haveCount = thingsToExamine[i].haveCount;
            List<Relationship> relationshipsToAdd = null;
            relationshipsToAdd = new();
            relationshipsToAdd.AddRange(t.Relationships);
            foreach (Relationship r in relationshipsToAdd)
            {
                if (r.reltype == Thing.HasChild) continue;
                //only add the new relatinoship to the list if it is not already in the list
                bool ignoreSource = thingsToExamine[i].hopCount > 1;
                Relationship existing = result.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
                if (existing != null) continue;

                if (haveCount > 1 && r.relType?.HasAncestorLabeled("has") != null)
                {
                    //this creates a temporary relationship so suzie has 2 arm, arm has 5 fingers, return suzie has 10 fingers
                    //this (transient) relationshiop doesn't exist in the UKS
                    Relationship r1 = new Relationship(r);
                    Thing newCountType = GetOrAddThing((GetCount(r.reltype) * haveCount).ToString(), "number");

                    //hack for numeric labels
                    Thing rootThing = r1.reltype;
                    if (r.relType.Label.Contains("."))
                        rootThing = GetOrAddThing(r.relType.Label.Substring(0, r.relType.Label.IndexOf(".")));
                    Thing bestMatch = r.relType;
                    List<Thing> missingAttributes = new();
                    Thing newRelType = SubclassExists(rootThing, new List<Thing> { newCountType }, ref bestMatch, ref missingAttributes);
                    if (newRelType == null)
                        newRelType = CreateSubclass(rootThing, new List<Thing> { newCountType });
                    r1.reltype = newRelType;
                    result.Add(r1);
                }
                else
                    result.Add(r);
            }
        }
        return result;
    }

    private void RemoveConflictingResults(List<Relationship> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Relationship r1 = result[i];
            for (int j = i + 1; j < result.Count; j++)
            {
                Relationship r2 = result[j];
                if (RelationshipsAreExclusive(r1, r2))
                {
                    result.RemoveAt(j);
                    j--;
                }

            }
        }
    }
    private void RemoveFalseConditionals(List<Relationship> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Relationship r1 = result[i];
            if (!ConditionsAreMet(r1.Clauses, r1))
            {
                failedConditions.Add(r1);
                result.RemoveAt(i);
                i--;
            }
            else
            {
                succeededConditions.Add(r1);
            }
        }
    }

    /// <summary>
    /// Filters a list of Relationships returning only those with at least one component which  has an ancestor in the list of Ancestors
    /// </summary>
    /// <param name="result">List of Relationships from a previous Query</param>
    /// <param name="ancestors">Filter</param>
    /// <returns></returns>
    public IList<Relationship> FilterResults(List<Relationship> result, List<Thing> ancestors)
    {
        List<Relationship> retVal = new();
        if (ancestors == null || ancestors.Count == 0)
            return result;
        foreach (Relationship r in result)
            if (RelationshipHasAncestor(r, ancestors))
                retVal.Add(r);
        return retVal;
    }

    private bool RelationshipHasAncestor(Relationship r, List<Thing> ancestors)
    {
        foreach (Thing ancestor in ancestors)
        {
            if (r.source.HasAncestor(ancestor)) return true;
            if (r.relType.HasAncestor(ancestor)) return true;
            if (r.target.HasAncestor(ancestor)) return true;
        }
        return false;
    }

    int GetCount(Thing t)
    {
        int retVal = 1;
        foreach (Relationship r in t.Relationships)
            if (r.relType.Label == "is")
                if (int.TryParse(r.target.Label, out int val))
                    return val;
        return retVal;
    }

    //TODO add concept of "near" Things
    //TODO add option to require first and/or last entries to match
    /// <summary>
    /// This determines how well two Things match in terms of the order of their ordered attributes.
    /// </summary>
    /// <param name="pattern">This is the pattern we are searching for</param>
    /// <param name="candidate">This is the item suggested as a possible sequence match< (the stored pattern)</param>
    /// <param name="bestOffset">Return value of offset into the candidate where the pattern match begins.</param>
    /// <param name="relType">If specified, specifees the relationship type to follow, otherwise all sequential relTypes are matched</param>
    /// <param name="circularSearch">If true, circularizes the search of the candidate (for visuals)</param>
    /// <returns>Confidence that the pattern exists in the candidate</returns>
    public float HasSequence(Thing pattern, Thing candidate, out int bestOffset, bool circularSearch = false, Thing relType = null)
    {
        bestOffset = -1;
        if (candidate == null) return -1;
        if (pattern == null) return -1;
        if (candidate.Relationships.Count == 0) return -1;
        if (pattern.Relationships.Count == 0) return -1;

        //get the needed relationships and put them in the order specified by the relationshipType digits
        float bestScore = -1;
        List<Relationship> patternRelationships = new(pattern.Relationships);
        patternRelationships = patternRelationships.FindAll(
            x => x.relType == null || Regex.IsMatch(x.reltype.Label, @"\d+") && (relType == null ||  x.relType.Parents.Contains(relType)));
        patternRelationships = patternRelationships.OrderBy(s => (s.relType == null) ? 0 : int.Parse(Regex.Match(s.reltype.Label, @"\d+").Value)).ToList();
        List<Relationship> candidateRelationships = new(candidate.Relationships);
        candidateRelationships = candidateRelationships.FindAll(
            x => x.relType == null || Regex.IsMatch(x.reltype.Label, @"\d+") && (relType == null || x.relType.Parents.Contains(relType)));
        candidateRelationships = candidateRelationships.OrderBy(s => (s.relType == null) ? 0 : int.Parse(Regex.Match(s.reltype.Label, @"\d+").Value)).ToList();

        //offset is the number of rels to skip at the beginning of the stored pattern
        for (int offset = 0; offset < candidateRelationships.Count; offset++)
        {
            float score = 0;
            for (int i = 0; i < patternRelationships.Count; i++)
            {
                //if circular search is requested and the offset is off the end of the candidate, loop back
                if (!circularSearch && offset  + i >= candidateRelationships.Count) break;
                int index = (offset + i) % candidateRelationships.Count;
                if (patternRelationships[i].target == candidateRelationships[index].target)
                {
                    score += candidateRelationships[index].Weight;
                }
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestOffset = offset;
            }
        }

        bestScore /= pattern.Relationships.Count;
        return bestScore;
    }

    //TODO add parameter to allow some number of misses
    private bool HasAllAttributes(Thing t, List<Thing> targetAttributes)
    {
        List<Thing> thingAttributes = GetAllAttributes(t);
        foreach (Thing t2 in targetAttributes)
            if (!thingAttributes.Contains(t2) && !t.AncestorList().Contains(t2)) return false;
        return true;
    }
    /// <summary>
    /// Returns a list of Things which have ALL the given attributes (IS Relationships)
    /// </summary>
    /// <param name="attributes">List of Things</param>
    /// <returns></returns>
    public List<Thing> FindThingsWithAttributes(List<Thing> attributes)
    {
        List<Thing> retVal = new();
        List<Thing> attribOwners = new List<Thing>();
        foreach (Thing t in attributes)
        {
            foreach (Relationship r in t.RelationshipsFrom)
                if (r.reltype.Label == "is")
                {
                    attribOwners.Add(r.source);
                }
        }
        List<ThingWithQueryParams> results = BuildSearchList(attribOwners);
        foreach (var t in results)
        {
            if (HasAllAttributes(t.thing, attributes))
                retVal.Add(t.thing);
        }
        return retVal;
    }


    bool ConditionsAreMet(List<Clause> clauses, Relationship query)
    {
        if (clauses.Count == 0) return true;
        //if (StackContains("ConditionsAreMet",1)) return true;
        foreach (Clause c in clauses)
        {
            if (c.clauseType.Label.ToLower() != "if") continue;
            Relationship r = c.clause;
            QueryRelationship q = new(r);
            if (query.source != null && query.source.AncestorList().Contains(q.source))
                q.source = query.source;
            if (query.source != null && query.source.AncestorList().Contains(q.target))
                q.target = query.target;
            if (query.target != null && query.target.AncestorList().Contains(q.source))
                q.source = query.target;
            var qResult = GetRelationship(q);
            if (qResult != null && qResult.Weight < 0.8)
                return false;
            if (qResult == null)
            {
                failedConditions.Add(q);
                return false;
            }
            else
                succeededConditions.Add(q);
        }
        return true;
    }
    /// <summary>
    /// Returns a list of Relationships which were false in the previous query
    /// </summary>
    /// <returns></returns>

    public List<Relationship> WhyNot()
    {
        return failedConditions;
    }
    /// <summary>
    /// Returns a list of Relatioships which were true in the previous query
    /// </summary>
    /// <returns></returns>
    public List<Relationship> Why()
    {
        return succeededConditions;
    }

    Dictionary<Thing, float> searchCandidates;
    /// <summary>
    /// Given that you have performed a search with SearchForClosestMatch, this returns the next-best result
    /// given the previous best.
    /// </summary>
    /// <param name="confidence">value representin the quality of the match</param>
    /// <returns></returns>
    public Thing GetNextClosestMatch(ref float confidence)
    {
        Thing bestThing = null;
        confidence = -1;
        if (searchCandidates == null) return bestThing;

        //find the best match with a value LESS THAN the previous best
        foreach (var key in searchCandidates)
            if (key.Value > confidence)
            {
                confidence = key.Value;
                bestThing = key.Key;
            }

        //remove the item from the dictionary
        if (bestThing != null)
            searchCandidates.Remove(bestThing);
        return bestThing;
    }

    /// <summary>
    /// Search for the Thing which most closely resembles the target Thing based on the attributes of the target
    /// </summary>
    /// <param name="target">The Relationships of this Thing are the attributes to search on</param>
    /// <param name="root">All searching is done within the descendents of this Thing</param>
    /// <param name="confidence">value representing the quality of the match. </param>
    /// <returns></returns>
    public Thing SearchForClosestMatch(Thing target, Thing root, ref float confidence)
    {
        searchCandidates = new();
        foreach (Relationship r in target.Relationships)
        {
            foreach (Relationship r1 in r.target.RelationshipsFrom)
            {
                if (r1.reltype.Label == "has-child") continue;//this is likely unnecessary
                //TODO: make this handle order-independency (go0,go1) vs relType params (has.2, has.4)
                //note: order-descriptors DO NOT have attributes while params DO
                if (r1.source.HasAncestor(root) && (r.reltype == null || r1.relType == r.relType))
                {
                    if (!searchCandidates.ContainsKey(r1.source))
                        searchCandidates[r1.source] = 0; //initialize a new dictionary entry if needed
                    searchCandidates[r1.source] += r1.Weight;
                }
            }
        }
        Thing bestThing = null;
        confidence = -1;

        //normalize the confidences
        foreach (var key in searchCandidates)
            searchCandidates[key.Key] /= target.Relationships.Count;

        ////handle inheritance
        ////TODO: check if this messes up on multiple inheritance and/or problems with the order of processing
        foreach (Thing t in searchCandidates.Keys)
            foreach (Thing t1 in t.Descendents)
                if (t1 != t && searchCandidates.ContainsKey(t1))
                    searchCandidates[t1] += searchCandidates[t];

        //find the best value
        foreach (var key in searchCandidates)
            if (key.Value > confidence)
            {
                confidence = key.Value;
                bestThing = key.Key;
            }
        //remove the item from the dictionary
        if (bestThing != null)
            searchCandidates.Remove(bestThing);
        return bestThing;
    }
}
