using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    //keeps track of the conditions of the previous query in order to answer "Why?" or "Why not?"
    List<Cogneme> failedConditions = new();
    List<Cogneme> succeededConditions = new();

    /// <summary>
    /// Gets all relationships to a group of Things including inherited relationships
    /// </summary>
    /// <param name="sources"></param>
    /// <returns>List of matching relationships</returns>
    public List<Cogneme> GetAllRelationships(List<Cogneme> sources) //with inheritance, conflicts, etc
    {
        List<Cogneme> result2 = new();
        if (sources.Count == 0) return result2;
            //expand search list to include instances of given objects  WHY??
        for (int i = 0; i < sources.Count; i++)
        {
            Cogneme t = sources[i];
            foreach (Cogneme child in t.Children)
                if (child.HasProperty("isInstance"))
                    sources.Add(child);
        }

        var result1 = BuildSearchList(sources);
        result2 = GetAllRelationshipsInternal(result1);
        if (result2.Count < 200)  //the conflict-remover is really slow on large numbers
            RemoveConflictingResults(result2);
        RemoveFalseConditionals(result2);
        SortRelationships(ref result2);
        return result2;
    }

    private void SortRelationships(ref List<Cogneme> result2)
    {
        result2 = result2.OrderByDescending(x => x.Weight).ToList();
    }

    //This is used to store temporary content during queries
    private class ThingWithQueryParams
    {
        public Cogneme thing;
        public int hopCount;
        public int haveCount = 1;
        public int hitCount = 1;
        public float weight;
        public Cogneme reachedWith = null;
        public bool corner = false;
        public override string ToString()
        {
            return (thing.Label + "  : " + hopCount + " : " + weight + "  Count: " +
                haveCount + " Hits: " + hitCount + " Corner: " + corner);
        }
    }

    //this follows "inheritable" relationships...should it follow transitive too?
    private List<ThingWithQueryParams> BuildSearchList(List<Cogneme> q)
    {
        List<ThingWithQueryParams> thingsToExamine = new();
        int maxHops = 8;
        int hopCount = 0;
        foreach (Cogneme t in q)
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
            Cogneme t = thingsToExamine[i].thing;
            float curWeight = thingsToExamine[i].weight;
            int curCount = thingsToExamine[i].haveCount;
            Cogneme reachedWith = thingsToExamine[i].reachedWith;

            foreach (Cogneme r in t.Relationships)  //has-child et al
            {
                if (r.RelType.HasProperty("inheritable"))
                {
                    //if there are several relationships, ignore the is-a, it is likely wrong
                    //var existingRelationships = GetRelationshipsBetween(r.source, r.target);
                    //if (existingRelationships.Count > 1) continue;

                    if (thingsToExamine.FindFirst(x => x.thing == r.Target) is ThingWithQueryParams twgp)
                        twgp.hitCount++;//thing is in the list, increment its count
                    else
                    {//thing is not in the list, add it
                        bool corner = !ThingInTree(r.RelType, thingsToExamine[i].reachedWith) &&
                            thingsToExamine[i].reachedWith is not null;
                        if (corner)
                        { } //TODO: corners are the reasons in a logic progression
                        thingsToExamine[i].corner |= corner;
                        ThingWithQueryParams thingToAdd = new ThingWithQueryParams
                        {
                            thing = r.Target,
                            hopCount = hopCount,
                            weight = curWeight * r.Weight,
                            reachedWith = r.RelType,
                        };
                        thingsToExamine.Add(thingToAdd);
                        //JUST FOR FUN: if things have counts, the counts are multiplied...  2hands * 5 fingers/hand = 10 fingers
                        int val = GetCount(r.RelType);
                        thingToAdd.haveCount = curCount * val;
                    }
                }
            }
        }
        return thingsToExamine;
    }
    private List<Cogneme> GetRelationshipsBetween(Cogneme t1, Cogneme t2)
    {
        List<Cogneme> retVal = new();
        foreach (Cogneme r in t1.Relationships)
            if (r.Target == t2) retVal.Add(r);
        foreach (Cogneme r in t1.RelationshipsFrom)
            if (r.Target == t2) retVal.Add(r);
        foreach (Cogneme r in t2.Relationships)
            if (r.Target == t1) retVal.Add(r);
        foreach (Cogneme r in t2.RelationshipsFrom)
            if (r.Target == t1) retVal.Add(r);
        return retVal;
    }
    private List<Cogneme> GetAllRelationshipsInternal(List<ThingWithQueryParams> thingsToExamine)
    {
        List<Cogneme> result = new();
        for (int i = 0; i < thingsToExamine.Count; i++)
        {
            Cogneme t = thingsToExamine[i].thing;
            if (t is null) continue; //safety
            int haveCount = thingsToExamine[i].haveCount;
            foreach (Cogneme r in t.Relationships)
            {
                if (r.RelType == Cogneme.IsA) continue;
                //only add the new relatinoship to the list if it is not already in the list
                bool ignoreSource = thingsToExamine[i].hopCount > 1;
                Cogneme existing = result.FindFirst(x => RelationshipsAreEqual(x, r, ignoreSource));
                if (existing is not null) continue;

                if (haveCount > 1 && r.RelType?.HasAncestorLabeled("has") is not null)
                {
                    //this HACK creates a temporary relationship so suzie has 2 arm, arm has 5 fingers, return suzie has 10 fingers
                    //this (transient) relationshiop doesn't exist in the UKS
                    Cogneme r1 = new Cogneme(r);
                    r1.Weight *= thingsToExamine[i].weight;
                    Cogneme newCountType = GetOrAddThing((GetCount(r.RelType) * haveCount).ToString(), "number");

                    //hack for numeric labels
                    Cogneme rootThing = r1.RelType;
                    if (r.RelType.Label.Contains("."))
                        rootThing = GetOrAddThing(r.RelType.Label.Substring(0, r.RelType.Label.IndexOf(".")));
                    Cogneme bestMatch = r.RelType;
                    List<Cogneme> missingAttributes = new();
                    Cogneme newRelType = SubclassExists(rootThing, new List<Cogneme> { newCountType }, ref bestMatch, ref missingAttributes);
                    if (newRelType is null)
                        newRelType = CreateSubclass(rootThing, new List<Cogneme> { newCountType });
                    r1.RelType = newRelType;
                    result.Add(r1);
                }
                else
                {
                    Cogneme r1 = new Cogneme(r);
                    foreach (Cogneme r3 in r.Relationships.Where(x=>x.RelType.Label != "is-a"))
                        r1.AddRelationship(r3.Target, r3.RelType);
                    r1.Weight *= thingsToExamine[i].weight;
                    result.Add(r1);
                }
            }
        }
        return result;
    }


    private void RemoveConflictingResults(List<Cogneme> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Cogneme r1 = result[i];

            //remove properties from the results list (they are internal)
            if (r1.RelType.Label == "hasProperty")
            {
                result.RemoveAt(i);
                continue;
            }
            for (int j = i + 1; j < result.Count; j++)
            {
                Cogneme r2 = result[j];
                //are the results the same?
                if (r1.RelType == r2.RelType && r1.Target == r2.Target)
                {
                    result.RemoveAt(j);
                    j--;
                }
                if (r1.RelType.Label.Contains(".") && r2.RelType.Label.Contains("."))
                    if (RelationshipsAreExclusive(r1, r2))
                    {
                        //if two relationships are in conflict, delete the 2nd one (First takes priority)
                        result.RemoveAt(j);
                        break;
                    }
            }
        }
    }
    private void RemoveFalseConditionals(List<Cogneme> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Cogneme r1 = result[i];
            if (!r1.HasProperty("isResult")) continue;
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
    public IReadOnlyList<Cogneme> FilterResults(List<Cogneme> result, List<Cogneme> ancestors)
    {
        List<Cogneme> retVal = new();
        if (ancestors is null || ancestors.Count == 0)
            return result;
        foreach (Cogneme r in result)
            if (RelationshipHasAncestor(r, ancestors))
                retVal.Add(r);
        return retVal;
    }

    private bool RelationshipHasAncestor(Cogneme r, List<Cogneme> ancestors)
    {
        foreach (Cogneme ancestor in ancestors)
        {
            if (r.Source.HasAncestor(ancestor)) return true;
            if (r.RelType.HasAncestor(ancestor)) return true;
            if (r.Target.HasAncestor(ancestor)) return true;
        }
        return false;
    }

    int GetCount(Cogneme t)
    {
        int retVal = 1;
        foreach (Cogneme r in t.Relationships)
            if (r.RelType.Label == "is")
                if (int.TryParse(r.Target.Label, out int val))
                    return val;
        return retVal;
    }


  

    bool ConditionsAreMet(Cogneme r)
    {
        foreach (Cogneme r1 in r.Relationships)
        {
            if (!r1.Source.HasProperty("isResult")) continue;
            if (!r1.Target.HasProperty("isCondition")) continue;

            Cogneme r2 = (Cogneme)r1.Target;
            //is r1 true?
            if (GetUnconditionalRelationship(r2) is null)
                return false;
        }
        return true;
    }
    Cogneme GetUnconditionalRelationship(Cogneme r)
    {
        foreach (Cogneme r1 in r.Source.Relationships)
        {
            if (RelationshipsAreEqualIgnoringLabels(r, r1))
            {
                if (!r1.HasProperty("isCondition"))
                   return r1;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a list of Relationships which were false in the previous query
    /// </summary>
    /// <returns></returns>

    public List<Cogneme> WhyNot()
    {
        return failedConditions;
    }
    /// <summary>
    /// Returns a list of Relationships which were true in the previous query
    /// </summary>
    /// <returns></returns>
    public List<Cogneme> Why()
    {
        return succeededConditions;
    }

    Dictionary<Cogneme, float> searchCandidates;
    /// <summary>
    /// Given that you have performed a search with SearchForClosestMatch, this returns the next-best result
    /// given the previous best.
    /// </summary>
    /// <param name="confidence">value representin the quality of the match</param>
    /// <returns></returns>
    public Cogneme GetNextClosestMatch(ref float confidence)
    {
        Cogneme bestThing = null;
        confidence = -1;
        if (searchCandidates is null) return bestThing;

        //find the best match with a value LESS THAN the previous best
        foreach (var key in searchCandidates)
            if (key.Value > confidence)
            {
                confidence = key.Value;
                bestThing = key.Key;
            }

        //remove the item from the dictionary
        if (bestThing is not null)
            searchCandidates.Remove(bestThing);
        return bestThing;
    }

    //this will be expanded to transitive...
    private List<Cogneme> GetListOfSimilarThings(Cogneme t)
    {
        List<Cogneme> retVal = new();
        foreach (Cogneme r in t.Relationships)
            if (r.RelType.Label == "isSimilarTo")
                retVal.Add(r.Target);
        foreach (Cogneme r in t.RelationshipsFrom)
            if (r.RelType.Label == "isSimilarTo" && !retVal.Contains(r.Source))
                retVal.Add(r.Source);
        return retVal;
    }

    /// <summary>
    /// Search for the Thing which most closely resembles the target Thing based on the attributes of the target
    /// </summary>
    /// <param name="target">The Relationships of this Thing are the attributes to search on</param>
    /// <param name="root">All searching is done within the descendents of this Thing</param>
    /// <param name="confidence">value representing the quality of the match. </param>
    /// <returns></returns>
    public List<(Cogneme t, float conf)> SearchForClosestMatch(Cogneme target, Cogneme root)
    {
        List<(Cogneme t, float conf)> retVal = new();
        if (target.Relationships.Count == 0) return retVal;
        //initialize the search queues
        List<Cogneme> thingsToSearch = new();
        List<Cogneme> alreadySearched = new();
        searchCandidates = new();

        //seed the search queue with the given parameters.
        foreach (Cogneme r in target.Relationships)
        {
            foreach (Cogneme r1 in r.Target.RelationshipsFrom)
            {
                if (r1.Source == target) continue;
                var existing = thingsToSearch.FindFirst(x => x == r1.Source);
                if (r1.RelType.HasAncestor(r.RelType) && r1.Target == r.Target && existing is null)
                {
                    thingsToSearch.Add(r1.Source);
                    if (!searchCandidates.ContainsKey(r1.Source))
                        searchCandidates[r1.Source] = 0; //initialize a new dictionary entry if needed
                    searchCandidates[r1.Source] += r1.Weight * r.Weight;
                }
                else if (existing is not null)
                {
                    searchCandidates[r1.Source] += r1.Weight * r.Weight;
                }
            }
        }
        //fan out from these seeds following all "inheritable" reverse connections.
        while (thingsToSearch.Count > 0)
        {
            var t = thingsToSearch[0];
            thingsToSearch.RemoveAt(0);
            alreadySearched.Add(t);
            foreach (Cogneme r in t.RelationshipsFrom)
            {
                if (!r.RelType.HasProperty("inheritable")) continue;
                if (r.Source == target) continue;
                AddToQueues(t, r.Source);
                //TODO fix this to handle isSimilarTo  (and transitive...?)
                //var similarThings = GetListOfSimilarThings(r.source);
                //foreach (Thing t1 in similarThings)
                //    AddToQueues(t, t1);
            }
        }

        foreach (var key in searchCandidates.ToList())
        {
            if (!ThingsHaveConflictingRelationship(key.Key, target)) continue;
            //searchCandidates.Remove(key.Key);
            searchCandidates[key.Key] = searchCandidates[key.Key] - .5f;
        }
        if (searchCandidates.Count == 0)
            return retVal;

        // delete items which have ancestor in list too
        for (int i = 0; i < searchCandidates.Keys.Count; i++)
        {
            Cogneme t = (Cogneme)searchCandidates.Keys.ToList()[i];
            foreach (Cogneme t1 in t.Ancestors)
            {
                if (t1 != t && searchCandidates.ContainsKey(t1) && searchCandidates[t1] < 0)
                    searchCandidates.Remove(t);
            }
        }

        ////normalize the confidences
        //float max = searchCandidates.Max(x => x.Value);
        //if (max < target.Relationships.Count) max = target.Relationships.Count;
        //foreach (var v in searchCandidates)
        //{
        //    searchCandidates[v.Key] /= max;
        //}

        //create the output list
        var ordered = searchCandidates.OrderByDescending(kv => kv.Value);
        foreach (var kv in ordered)
            retVal.Add((kv.Key, kv.Value));

        return retVal;

        bool AddToQueues(Cogneme tPrev, Cogneme tNew)
        {
            if (!tNew.HasAncestor(root)) return false;
            if (!searchCandidates.ContainsKey(tNew))
                searchCandidates[tNew] = 0; //initialize a new dictionary entry if needed
            searchCandidates[tNew] += searchCandidates[tPrev] * GetRelationshipWeight(tNew, tPrev);
            if (alreadySearched.FindFirst(x => x == tNew) is not null) return false;
            if (thingsToSearch.FindFirst(x => x == tNew) is not null) return false;
            thingsToSearch.Add(tNew);
            return true;
        }
    }
    public float GetRelationshipWeight(Cogneme t1, Cogneme t2)
    {
        foreach (var r in t1.Relationships)
            if (r.Target == t2) return r.Weight;
        foreach (var r in t1.RelationshipsFrom)
            if (r.Target == t2) return r.Weight;
        return 0;
    }
    public void SetRelationshipWeight(Cogneme t1, Cogneme t2, float newWeight)
    {
        foreach (var r in t1.Relationships)
            if (r.Target == t2) r.Weight = newWeight;
        foreach (var r in t1.RelationshipsFrom)
            if (r.Target == t2) r.Weight = newWeight;
        foreach (var r in t2.Relationships)
            if (r.Target == t1) r.Weight = newWeight;
        foreach (var r in t2.RelationshipsFrom)
            if (r.Target == t1) r.Weight = newWeight;
    }

    public bool ThingsHaveConflictingRelationship(Cogneme source, Cogneme target)
    {
        foreach (Cogneme r1 in source.Relationships)
            foreach (Cogneme r2 in target.Relationships)
                if (RelationshipsAreExclusive(r1, r2))
                    return true;
        return false;
    }
    private bool RelationshipsAreSimilar(Cogneme r1, Cogneme r2)
    {
        if (r1.RelType != r2.RelType) return false;
        if (FindCommonParents(r1.Target, r2.Target).Count == 0) return false;
        return true;
    }
    public bool ThingsHaveSimilarRelationship(Cogneme source, Cogneme target)
    {
        foreach (Cogneme r1 in source.Relationships)
            foreach (Cogneme r2 in target.Relationships)
                if (RelationshipsAreSimilar(r1, r2))
                    return true;
        return false;
    }
}
