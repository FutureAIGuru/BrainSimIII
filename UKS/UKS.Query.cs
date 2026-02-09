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
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    //keeps track of the conditions of the previous query in order to answer "Why?" or "Why not?"
    List<Thought> failedConditions = new();
    List<Thought> succeededConditions = new();

    /// <summary>
    /// Gets all links to a group of Thoughts including inherited links
    /// </summary>
    /// <param name="sources"></param>
    /// <returns>List of matching links</returns>
    public List<Thought> GetAllLinks(List<Thought> sources) //with inheritance, conflicts, etc
    {
        List<Thought> result2 = new();
        if (sources.Count == 0) return result2;
            //expand search list to include instances of given objects  WHY??
        for (int i = 0; i < sources.Count; i++)
        {
            Thought t = sources[i];
            foreach (Thought child in t.Children)
                if (child.HasProperty("isInstance"))
                    sources.Add(child);
        }

        var result1 = BuildSearchList(sources);
        result2 = GetAllLinksInternal(result1);
        if (result2.Count < 200)  //the conflict-remover is really slow on large numbers
            RemoveConflictingResults(result2);
        RemoveFalseConditionals(result2);
        SortLinks(ref result2);
        return result2;
    }

    private void SortLinks(ref List<Thought> result2)
    {
        result2 = result2.OrderByDescending(x => x.Weight).ToList();
    }

    //This is used to store temporary content during queries
    private class ThoughtWithQueryParams
    {
        public Thought thought;
        public int hopCount;
        public int haveCount = 1;
        public int hitCount = 1;
        public float weight;
        public Thought reachedWith = null;
        public bool corner = false;
        public override string ToString()
        {
            return (thought.Label + "  : " + hopCount + " : " + weight + "  Count: " +
                haveCount + " Hits: " + hitCount + " Corner: " + corner);
        }
    }

    //this follows "inheritable" links...should it follow transitive too?
    private List<ThoughtWithQueryParams> BuildSearchList(List<Thought> q)
    {
        List<ThoughtWithQueryParams> thoughtsToExamine = new();
        int maxHops = 8;
        int hopCount = 0;
        foreach (Thought t in q)
            thoughtsToExamine.Add(new ThoughtWithQueryParams
            {
                thought = t,
                hopCount = hopCount,
                weight = 1,
                reachedWith = null
            });
        hopCount++;
        int currentEnd = thoughtsToExamine.Count;
        for (int i = 0; i < thoughtsToExamine.Count; i++)
        {
            Thought t = thoughtsToExamine[i].thought;
            float curWeight = thoughtsToExamine[i].weight;
            int curCount = thoughtsToExamine[i].haveCount;
            Thought reachedWith = thoughtsToExamine[i].reachedWith;

            foreach (Thought r in t.LinksTo)  //has-child et al
            {
                if (r.LinkType.HasProperty("inheritable"))
                {
                     if (thoughtsToExamine.FindFirst(x => x.thought == r.To) is ThoughtWithQueryParams twgp)
                        twgp.hitCount++;//thought is in the list, increment its count
                    else
                    {//thought is not in the list, add it
                        bool corner = !ThoughtInTree(r.LinkType, thoughtsToExamine[i].reachedWith) &&
                            thoughtsToExamine[i].reachedWith is not null;
                        if (corner)
                        { } //TODO: corners are the reasons in a logic progression
                        thoughtsToExamine[i].corner |= corner;
                        ThoughtWithQueryParams thoughtToAdd = new ThoughtWithQueryParams
                        {
                            thought = r.To,
                            hopCount = hopCount,
                            weight = curWeight * r.Weight,
                            reachedWith = r.LinkType,
                        };
                        thoughtsToExamine.Add(thoughtToAdd);
                        //JUST FOR FUN: if thoughts have counts, the counts are multiplied...  2hands * 5 fingers/hand = 10 fingers
                        int val = GetCount(r.LinkType);
                        thoughtToAdd.haveCount = curCount * val;
                    }
                }
            }
        }
        return thoughtsToExamine;
    }
    private List<Thought> GetLinksBetween(Thought t1, Thought t2)
    {
        List<Thought> retVal = new();
        foreach (Thought r in t1.LinksTo)
            if (r.To == t2) retVal.Add(r);
        foreach (Thought r in t1.LinksFrom)
            if (r.To == t2) retVal.Add(r);
        foreach (Thought r in t2.LinksTo)
            if (r.To == t1) retVal.Add(r);
        foreach (Thought r in t2.LinksFrom)
            if (r.To == t1) retVal.Add(r);
        return retVal;
    }
    private List<Thought> GetAllLinksInternal(List<ThoughtWithQueryParams> thoughtsToExamine)
    {
        List<Thought> result = new();
        for (int i = 0; i < thoughtsToExamine.Count; i++)
        {
            Thought t = thoughtsToExamine[i].thought;
            if (t is null) continue; //safety
            int haveCount = thoughtsToExamine[i].haveCount;
            foreach (Thought r in t.LinksTo)
            {
                if (r.LinkType == Thought.IsA) continue;
                //only add the new relatinoship to the list if it is not already in the list
                bool ignoreSource = thoughtsToExamine[i].hopCount > 1;
                Thought existing = result.FindFirst(x => LinksAreEqual(x, r, ignoreSource));
                if (existing is not null) continue;

                if (haveCount > 1 && r.LinkType?.HasAncestor("has") is not null)
                {
                    //this HACK creates a temporary link so suzie has 2 arm, arm has 5 fingers, return suzie has 10 fingers
                    //this (transient) relationshiop doesn't exist in the UKS
                    Thought r1 = new Thought(r);
                    r1.Weight *= thoughtsToExamine[i].weight;
                    Thought newCountType = GetOrAddThought((GetCount(r.LinkType) * haveCount).ToString(), "number");

                    //hack for numeric labels
                    Thought rootThought = r1.LinkType;
                    if (r.LinkType.Label.Contains("."))
                        rootThought = GetOrAddThought(r.LinkType.Label.Substring(0, r.LinkType.Label.IndexOf(".")));
                    Thought bestMatch = r.LinkType;
                    List<Thought> missingAttributes = new();
                    Thought newLinkType = SubclassExists(rootThought, new List<Thought> { newCountType }, ref bestMatch, ref missingAttributes);
                    if (newLinkType is null)
                        newLinkType = CreateSubclass(rootThought, new List<Thought> { newCountType });
                    r1.LinkType = newLinkType;
                    result.Add(r1);
                }
                else
                {
                    Thought r1 = new Thought(r);
                    foreach (Thought r3 in r.LinksTo.Where(x=>x.LinkType.Label != "is-a"))
                        r1.AddLink(r3.To, r3.LinkType);
                    r1.Weight *= thoughtsToExamine[i].weight;
                    result.Add(r1);
                }
            }
        }
        return result;
    }


    private void RemoveConflictingResults(List<Thought> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Thought r1 = result[i];

            //remove properties from the results list (they are internal)
            if (r1.LinkType.Label == "hasProperty")
            {
                result.RemoveAt(i);
                continue;
            }
            for (int j = i + 1; j < result.Count; j++)
            {
                Thought r2 = result[j];
                //are the results the same?
                if (r1.LinkType == r2.LinkType && r1.To == r2.To)
                {
                    result.RemoveAt(j);
                    j--;
                }
                if (r1.LinkType.Label.Contains(".") && r2.LinkType.Label.Contains("."))
                    if (LinksAreExclusive(r1, r2))
                    {
                        //if two links are in conflict, delete the 2nd one (First takes priority)
                        result.RemoveAt(j);
                        break;
                    }
            }
        }
    }
    private void RemoveFalseConditionals(List<Thought> result)
    {
        for (int i = 0; i < result.Count; i++)
        {
            Thought r1 = result[i];
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
    /// Filters a list of Links returning only those with at least one component which  has an ancestor in the list of Ancestors
    /// </summary>
    /// <param name="result">List of Links from a previous Query</param>
    /// <param name="ancestors">Filter</param>
    /// <returns></returns>
    public IReadOnlyList<Thought> FilterResults(List<Thought> result, List<Thought> ancestors)
    {
        List<Thought> retVal = new();
        if (ancestors is null || ancestors.Count == 0)
            return result;
        foreach (Thought r in result)
            if (LinkHasAncestor(r, ancestors))
                retVal.Add(r);
        return retVal;
    }

    private bool LinkHasAncestor(Thought r, List<Thought> ancestors)
    {
        foreach (Thought ancestor in ancestors)
        {
            if (r.From.HasAncestor(ancestor)) return true;
            if (r.LinkType.HasAncestor(ancestor)) return true;
            if (r.To.HasAncestor(ancestor)) return true;
        }
        return false;
    }

    int GetCount(Thought t)
    {
        int retVal = 1;
        foreach (Thought r in t.LinksTo)
            if (r.LinkType.Label == "is")
                if (int.TryParse(r.To.Label, out int val))
                    return val;
        return retVal;
    }


  

    bool ConditionsAreMet(Thought r)
    {
        foreach (Thought r1 in r.LinksTo)
        {
            if (!r1.From.HasProperty("isResult")) continue;
            if (!r1.To.HasProperty("isCondition")) continue;

            Thought r2 = (Thought)r1.To;
            //is r1 true?
            if (GetUnconditionalLink(r2) is null)
                return false;
        }
        return true;
    }
    Thought GetUnconditionalLink(Thought r)
    {
        foreach (Thought r1 in r.From.LinksTo)
        {
            if (Equals(r, r1))
            {
                if (!r1.HasProperty("isCondition"))
                   return r1;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a list of Links which were false in the previous query
    /// </summary>
    /// <returns></returns>

    public List<Thought> WhyNot()
    {
        return failedConditions;
    }
    /// <summary>
    /// Returns a list of Links which were true in the previous query
    /// </summary>
    /// <returns></returns>
    public List<Thought> Why()
    {
        return succeededConditions;
    }

    Dictionary<Thought, float> searchCandidates;
    /// <summary>
    /// Given that you have performed a search with SearchForClosestMatch, this returns the next-best result
    /// given the previous best.
    /// </summary>
    /// <param name="confidence">value representin the quality of the match</param>
    /// <returns></returns>
    public Thought GetNextClosestMatch(ref float confidence)
    {
        Thought bestThought = null;
        confidence = -1;
        if (searchCandidates is null) return bestThought;

        //find the best match with a value LESS THAN the previous best
        foreach (var key in searchCandidates)
            if (key.Value > confidence)
            {
                confidence = key.Value;
                bestThought = key.Key;
            }

        //remove the item from the dictionary
        if (bestThought is not null)
            searchCandidates.Remove(bestThought);
        return bestThought;
    }


    /// <summary>
    /// Search for the Thought which most closely resembles the target Thought based on the attributes of the target
    /// </summary>
    /// <param name="target">The Links of this Thought are the attributes to search on</param>
    /// <param name="root">All searching is done within the descendents of this Thought</param>
    /// <param name="confidence">value representing the quality of the match. </param>
    /// <returns></returns>
    public List<(Thought t, float conf)> SearchForClosestMatch(Thought target, Thought root)
    {
        List<(Thought t, float conf)> retVal = new();
        if (target.LinksTo.Count == 0) return retVal;
        //initialize the search queues
        List<Thought> thoughtsToSearch = new();
        List<Thought> alreadySearched = new();
        searchCandidates = new();

        //seed the search queue with the given parameters.
        foreach (Thought r in target.LinksTo)
        {
            foreach (Thought r1 in r.To.LinksFrom)
            {
                if (r1.From == target) continue;
                var existing = thoughtsToSearch.FindFirst(x => x == r1.From);
                if ((r1.LinkType == r.LinkType || r1.LinkType.HasAncestor(r.LinkType)) && r1.To == r.To && existing is null)
                {
                    thoughtsToSearch.Add(r1.From);
                    if (!searchCandidates.ContainsKey(r1.From))
                        searchCandidates[r1.From] = 0; //initialize a new dictionary entry if needed
                    searchCandidates[r1.From] += r1.Weight * r.Weight;
                }
                else if (existing is not null)
                {
                    searchCandidates[r1.From] += r1.Weight * r.Weight;
                }
            }
        }
        //fan out from these seeds following all "inheritable" reverse connections.
        while (thoughtsToSearch.Count > 0)
        {
            var t = thoughtsToSearch[0];
            thoughtsToSearch.RemoveAt(0);
            alreadySearched.Add(t);
            foreach (Thought r in t.LinksFrom)
            {
                if (!r.LinkType.HasProperty("inheritable")) continue;
                if (r.From == target) continue;
                AddToQueues(t, r.From);
                //TODO fix this to handle isSimilarTo  (and transitive...?)
                //var similarThoughts = GetListOfSimilarThoughts(r.source);
                //foreach (Thought t1 in similarThoughts)
                //    AddToQueues(t, t1);
            }
        }

        foreach (var key in searchCandidates.ToList())
        {
            if (!ThoughtsHaveConflictingLink(key.Key, target)) continue;
            //searchCandidates.Remove(key.Key);
            searchCandidates[key.Key] = searchCandidates[key.Key] - .5f;
        }
        if (searchCandidates.Count == 0)
            return retVal;

        // delete items which have ancestor in list too
        for (int i = 0; i < searchCandidates.Keys.Count; i++)
        {
            Thought t = (Thought)searchCandidates.Keys.ToList()[i];
            foreach (Thought t1 in t.Ancestors)
            {
                if (t1 != t && searchCandidates.ContainsKey(t1) && searchCandidates[t1] < 0)
                    searchCandidates.Remove(t);
            }
        }

        //create the output list
        var ordered = searchCandidates.OrderByDescending(kv => kv.Value);
        foreach (var kv in ordered)
            retVal.Add((kv.Key, kv.Value));

        return retVal;

        bool AddToQueues(Thought tPrev, Thought tNew)
        {
            if (!tNew.HasAncestor(root)) return false;
            if (!searchCandidates.ContainsKey(tNew))
                searchCandidates[tNew] = 0; //initialize a new dictionary entry if needed
            searchCandidates[tNew] += searchCandidates[tPrev] * GetLinkWeight(tNew, tPrev);
            if (alreadySearched.FindFirst(x => x == tNew) is not null) return false;
            if (thoughtsToSearch.FindFirst(x => x == tNew) is not null) return false;
            thoughtsToSearch.Add(tNew);
            return true;
        }
    }
    public float GetLinkWeight(Thought t1, Thought t2)
    {
        foreach (var r in t1.LinksTo)
            if (r.To == t2) return r.Weight;
        foreach (var r in t1.LinksFrom)
            if (r.To == t2) return r.Weight;
        return 0;
    }
    public void SetLinkWeight(Thought t1, Thought t2, float newWeight)
    {
        foreach (var r in t1.LinksTo)
            if (r.To == t2) r.Weight = newWeight;
        foreach (var r in t1.LinksFrom)
            if (r.To == t2) r.Weight = newWeight;
        foreach (var r in t2.LinksTo)
            if (r.To == t1) r.Weight = newWeight;
        foreach (var r in t2.LinksFrom)
            if (r.To == t1) r.Weight = newWeight;
    }

    public bool ThoughtsHaveConflictingLink(Thought source, Thought target)
    {
        foreach (Thought r1 in source.LinksTo)
            foreach (Thought r2 in target.LinksTo)
                if (LinksAreExclusive(r1, r2))
                    return true;
        return false;
    }
    private bool LinksAreSimilar(Thought r1, Thought r2)
    {
        if (r1.LinkType != r2.LinkType) return false;
        if (FindCommonParents(r1.To, r2.To).Count == 0) return false;
        return true;
    }
    public bool ThoughtsHaveSimilarLink(Thought source, Thought target)
    {
        foreach (Thought r1 in source.LinksTo)
            foreach (Thought r2 in target.LinksTo)
                if (LinksAreSimilar(r1, r2))
                    return true;
        return false;
    }
}
