using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace UKS;
public partial class UKS
{
    //keeps track of the conditions of the previous query in order to answer "Why?" or "Why not?"
    List<Relationship> failedConditions = new();
    List<Relationship> succeededConditions = new();

    /// <summary>
    /// Gets all relationships to a group of Things including inherited relationships
    /// </summary>
    /// <param name="sources"></param>
    /// <returns>List of matching relationships</returns>
    public List<Relationship> GetAllRelationships(List<Thing> sources) //with inheritance, conflicts, etc
    {
        //expand search list to include instances of given objects  WHY??
        for (int i = 0; i < sources.Count; i++)
        {
            Thing t = sources[i];
            foreach (Thing child in t.Children)
                if (child.HasProperty("isInstance"))
                    sources.Add(child);
        }

        var result1 = BuildSearchList(sources);
        List<Relationship> result2 = GetAllRelationshipsInt(result1);
        if (result2.Count < 200)  //the conflict-remover is really slow on large numbers
            RemoveConflictingResults(result2);
        RemoveFalseConditionals(result2);
        SortRelationships(ref result2);
        return result2;
    }

    private void SortRelationships(ref List<Relationship> result2)
    {
        result2 = result2.OrderByDescending(x => x.Weight).ToList();
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

    //this follows "inheritable" relationships...should it follow transitive too?
    private List<ThingWithQueryParams> BuildSearchList(List<Thing> q)
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
            });
        hopCount++;
        int currentEnd = thingsToExamine.Count;
        for (int i = 0; i < thingsToExamine.Count; i++)
        {
            Thing t = thingsToExamine[i].thing;
            float curWeight = thingsToExamine[i].weight;
            int curCount = thingsToExamine[i].haveCount;
            Thing reachedWith = thingsToExamine[i].reachedWith;

            foreach (Relationship r in t.Relationships)  //has-child et al
            {
                if (r.relType.HasProperty("inheritable"))
                {
                    //if there are several relationships, ignore the is-a, it is likely wrong
                    //var existingRelationships = GetRelationshipsBetween(r.source, r.target);
                    //if (existingRelationships.Count > 1) continue;

                    if (thingsToExamine.FindFirst(x => x.thing == r.target) is ThingWithQueryParams twgp)
                        twgp.hitCount++;//thing is in the list, increment its count
                    else
                    {//thing is not in the list, add it
                        bool corner = !ThingInTree(r.relType, thingsToExamine[i].reachedWith) &&
                            thingsToExamine[i].reachedWith != null;
                        if (corner)
                        { } //TODO: corners are the reasons in a logic progression
                        thingsToExamine[i].corner |= corner;
                        ThingWithQueryParams thingToAdd = new ThingWithQueryParams
                        {
                            thing = r.target,
                            hopCount = hopCount,
                            weight = curWeight * r.Weight,
                            reachedWith = r.relType,
                        };
                        thingsToExamine.Add(thingToAdd);
                        //JUST FOR FUN: if things have counts, the counts are multiplied...  2hands * 5 fingers/hand = 10 fingers
                        int val = GetCount(r.reltype);
                        thingToAdd.haveCount = curCount * val;
                    }
                }
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
                if (r.reltype == Thing.IsA) continue;
                //only add the new relatinoship to the list if it is not already in the list
                bool ignoreSource = thingsToExamine[i].hopCount > 1;
                Relationship existing = result.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
                if (existing != null) continue;

                if (haveCount > 1 && r.relType?.HasAncestorLabeled("has") != null)
                {
                    //this HACK creates a temporary relationship so suzie has 2 arm, arm has 5 fingers, return suzie has 10 fingers
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

            //remove properties from the results list (they are internal)
            if (r1.reltype.Label == "hasProperty")
            {
                result.RemoveAt(i);
                continue;
            }
            for (int j = i + 1; j < result.Count; j++)
            {
                Relationship r2 = result[j];
                //are the results the same?
                if (r1.reltype == r2.reltype && r1.target == r2.target)
                {
                    result.RemoveAt(j);
                    j--;
                }
                if (r1.reltype.Label.Contains(".") && r2.reltype.Label.Contains("."))
                    if (RelationshipsAreExclusive(r1, r2))
                    {
                        //if two relationships are in conflict, delete the 2nd one (First takes priority)
                        result.RemoveAt(j);
                        break;
                    }
            }
        }
    }
    private void RemoveFalseConditionals(List<Relationship> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Relationship r1 = result[i];
            if (!ConditionsAreMet(r1))
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
            x => x.relType == null || Regex.IsMatch(x.reltype.Label, @"\d+") && (relType == null || x.relType.Parents.Contains(relType)));
        patternRelationships = patternRelationships.OrderBy(s => (s.relType == null) ? 0 : int.Parse(Regex.Match(s.reltype.Label, @"\d+").Value)).ToList();
        List<Relationship> candidateRelationships = new(candidate.Relationships);
        candidateRelationships = candidateRelationships.FindAll(
            x => x.relType == null || Regex.IsMatch(x.reltype.Label, @"\d+") && (relType == null || x.relType.Parents.Contains(relType)));
        candidateRelationships = candidateRelationships.OrderBy(s => (s.relType == null) ? 0 : int.Parse(Regex.Match(s.reltype.Label, @"\d+").Value)).ToList();

        //offset is the number of rels to skip at the beginning of the stored pattern
        for (int offset = 0; offset < patternRelationships.Count; offset++)
        {
            float score = 0;
            for (int i = 0; i < candidateRelationships.Count; i++)
            {
                //if circular search is requested and the offset is off the end of the candidate, loop back
                if (!circularSearch && offset + i >= candidateRelationships.Count) break;
                int index = (offset + i) % patternRelationships.Count;
                if (candidateRelationships[i].target == patternRelationships[index].target)
                {
                    score += patternRelationships[index].Weight;
                }
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestOffset = offset;
            }
        }

        bestScore /= patternRelationships.Count;
        return bestScore;
    }


    bool ConditionsAreMet(Relationship r)
    {
        if (r.Clauses.Count == 0 && r.isStatement) return true;
        //if (StackContains("ConditionsAreMet",1)) return true;
        foreach (Clause c in r.Clauses)
        {
            if (c.clauseType.Label.ToLower() != "if") continue;
            Relationship r1 = c.clause;
            QueryRelationship q = new(r1);
            //if (query.source != null && query.source.AncestorList().Contains(q.source))
            //    q.source = query.source;
            //if (query.source != null && query.source.AncestorList().Contains(q.target))
            //    q.target = query.target;
            //if (query.target != null && query.target.AncestorList().Contains(q.source))
            //    q.source = query.target;
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
    /// Returns a list of Relationships which were true in the previous query
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

    //this will be expanded to transitive...
    private List<Thing> GetListOfSimilarThings(Thing t)
    {
        List<Thing> retVal = new();
        foreach (Relationship r in t.Relationships)
            if (r.relType.Label == "isSimilarTo")
                retVal.Add(r.target);
        foreach (Relationship r in t.RelationshipsFrom)
            if (r.relType.Label == "isSimilarTo" && !retVal.Contains(r.source))
                retVal.Add(r.source);
        return retVal;
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
        //initialize the search queues
        Queue<Thing> thingsToSearch = new();
        Queue<Thing> alreadySearched = new();

        //seed the search queue with the given parameters.
        foreach (Relationship r in target.Relationships)
        {
            foreach (Relationship r1 in r.target.RelationshipsFrom)
            {
                if (r1.source == target) continue;
                var newItem = (r1.source, r1.Weight);
                if (r1.reltype.HasAncestor(r.reltype) && r1.target == r.target && !thingsToSearch.Contains(r1.source))
                {
                    thingsToSearch.Enqueue(r1.source);
                    if (!searchCandidates.ContainsKey(r1.source))
                        searchCandidates[r1.source] = 0; //initialize a new dictionary entry if needed
                    searchCandidates[r1.source] += r1.Weight;
                }
            }
        }
        //fan out from these seeds following all "inheritable" reverse connections.
        while (thingsToSearch.Count > 0)
        {
            var t = thingsToSearch.Dequeue();
            alreadySearched.Enqueue(t);
            foreach (Relationship r in t.RelationshipsFrom)
            {
                if (!r.relType.HasProperty("inheritable")) continue;
                if (ThingsHaveConflictingRelationship(r.source, target)) continue;
                AddToQueues(t, r.source);
                //TODO fix this to handle isSimilarTo  (and transitive...?)
                //var similarThings = GetListOfSimilarThings(r.source);
                //foreach (Thing t1 in similarThings)
                //    AddToQueues(t, t1);
            }
        }
             
        Thing bestThing = null;
        confidence = -1;

        if (searchCandidates.Count == 0)
            return null;

        // delete items which have ancestor in list too
        for (int i = 0; i < searchCandidates.Keys.Count; i++)
        {
            Thing t = (Thing)searchCandidates.Keys.ToList()[i];
            foreach (Thing t1 in t.Ancestors)
            {
                if (t1 != t && searchCandidates.ContainsKey(t1) && searchCandidates[t1] < 0)
                    searchCandidates.Remove(t);
            }
        }

        //normalize the confidences
        float max = searchCandidates.Max(x => x.Value);
        foreach (var v in searchCandidates)
        {
            searchCandidates[v.Key] /= max;
        }
        //find the best value
        foreach (var key in searchCandidates)
            if (key.Value > confidence)
            {
                confidence = key.Value;
                bestThing = key.Key;
            }

        //remove the top item from the dictionary... so GetNextClosestMatch will work
        if (bestThing != null)
            searchCandidates.Remove(bestThing);
        return bestThing;

        bool AddToQueues(Thing tPrev, Thing tNew)
        {
            if (!tNew.HasAncestor(root)) return false;
            if (!searchCandidates.ContainsKey(tNew))
                searchCandidates[tNew] = 0; //initialize a new dictionary entry if needed
            searchCandidates[tNew] += searchCandidates[tPrev];
            if (alreadySearched.Contains(tNew)) return false;
            if (thingsToSearch.Contains(tNew)) return false;
            thingsToSearch.Enqueue(tNew);
            return true;
        }
    }

    private bool ThingsHaveConflictingRelationship(Thing source, Thing target)
    {
        foreach (Relationship r1 in source.Relationships)
            foreach (Relationship r2 in target.Relationships)
                if (RelationshipsAreExclusive(r1, r2)) 
                    return true;
        return false;
    }
}
