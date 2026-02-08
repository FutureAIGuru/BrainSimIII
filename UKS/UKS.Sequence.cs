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
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    //The structure of a sequence is a series of elements, each with 3 links.
    //"NXT" with a To of the next element in the sequence
    //"VLU" to the actual Thought in the sequence
    //"FRST" which points to the first element of the list
    //the "owner" of the sequences had a link of LinkType (spelled e.g.) to the first element in the sequence
    //the last element in the sequence has a link of LinkType "NXT" with a To of null
    //Example, to represent the spelling of "CAT":
    // [cat -> spelled -> seq0]
    // seq0 ->NXT--> seq1
    // seq0 ->FRST-> seq0
    // seq0 ->VLU--> C
    // seq1 ->NXT -> seq2
    // seq1 ->FRST-> seq0
    // seq1 ->VLU--> A
    // seq2 ->NXT -> null  (or has no NXT entry)
    // seq2 ->FRST-> seq0
    // seq2 ->VLU--> T
    // NOTE: the elements seq* need not have labels at all, they are just used here for clarity
    // each seq* element must also have a FRST releationship back to the owner Thought
    // The Thought ToString() method will automatically follow the sequences and return cat->spelled->^cat
    // VLU targets may be other sequences

    // A few Special cases can be detected by comparing targets
    // Is a sequence element:  Has a FRST link
    // Start of sequence:  seq->NXT = seq->FRST
    // End of sequence:    seq->NXT = null 

    //TODO:
    // add circular sequences (search can start at any location in the sequence)
    // search with errors and scoring: First/Last are correct, Elements out of order, Elements near others

    public bool IsSequenceElement(Thought t)
    {
        return (t?.LinksTo.FindFirst(x=>x.LinkType?.Label == "FRST") != null);
    }
    public bool IsSequenceFirstElement(Thought t)
    {
        if (!IsSequenceElement(t)) return false;
        Thought t1 = GetFirstElement(t);
        return object.ReferenceEquals(t1, t); //use this to check for same object becaue == is be overloaded
    }
    public bool IsSequenceLastElement(Thought t)
    {
        if (!IsSequenceElement(t)) return false;
        return (t.To is null); 
    }
    public Thought GetNextElement(Thought t)
    {
        Thought retVal = null;
        retVal = t?.LinksTo.FindFirst(x => x.LinkType.Label == "NXT")?.To;
        return retVal;
    }
    public Thought GetFirstElement(Thought t)
    {
        if (!IsSequenceElement(t)) return null;
        return t?.LinksTo.FindFirst(x => x.LinkType.Label == "FRST")?.To;
    }
    public Thought GetLastlement(Thought t)
    {
        if (!IsSequenceElement(t)) return null;
        Thought retVal = t;
        Thought next = t;
        while (next is not null)
        {
            next = GetNextElement(retVal);
            if (next is not null) retVal = next;
        }
        return retVal;
    }
    public Thought GetElementValue(Thought t)  //assuming there is only one
    {
        if (!IsSequenceElement(t)) return null;
        Thought retVal = t?.LinksTo.FindFirst(x => x.LinkType.Label == "VLU")?.To;
        return retVal;

    }
    public List<Thought> GetElementValues(Thought t)  //there could be more than one
    {
        if (!IsSequenceElement(t)) return null;
        var retVal = t?.LinksTo.Where(x => x.LinkType.Label == "VLU").Select(x=>x.To)?.ToList();
        return retVal;

    }
    private int GetSequenceLength(Thought firstNode)
    {
        if (firstNode is null) return 0;
        return FlattenSequence(firstNode).Count;
    }

    //puts a new wlement at the beginning of the sequence
    public Thought InsertElement(Thought prevElementIn, Thought value)
    {
        Thought first = prevElementIn;
        if (IsSequenceFirstElement(prevElementIn))
        {
            //this is a bit tricky...
            //it actually adds a 2nd element but then copies old 1st element values to the 2nd element and puts the new value on the old first
            //why? all the FRST links will still be correct without modification
            Thought newNode = new Thought() { Label = prevElementIn.Label + "*" };  //the label will auto-increment.
            newNode.AddLink(first, "FRST");
            newNode.AddLink(first.LinksTo.FindFirst(x => x.LinkType.Label == "VLU").To, "VLU");
            newNode.AddLink(first.LinksTo.FindFirst(x => x.LinkType.Label == "NXT")?.To, "NXT");
            first.RemoveLinks("VLU");
            first.RemoveLinks("NXT");
            first.AddLink(value, "VLU");
            first.AddLink(newNode, "NXT");
        }
        else
        {
            throw new NotImplementedException();
        }
        return first;
    }

    //Adds a new element to the end of the sequence
    public Thought AddElement(Thought prevElementIn, Thought value)
    {
        Thought prevElement = GetLastlement(prevElementIn);
        Thought newNode = new Thought() { Label = prevElement.Label + "*" };  //the label will auto-increment.
        newNode.AddLink(GetFirstElement(prevElement), "FRST");
        newNode.AddLink(value, "VLU");
        prevElement.AddLink(newNode, "NXT");
        return newNode;
    }
    //starts a sequence
    public Thought CreateFirstElement(Thought source, Thought value)
    {
        Thought firstNode = new Thought() { Label = source.Label.ToLower() + "-seq0" };
        firstNode.AddLink(firstNode, "FRST"); // Points to source as first node
        firstNode.AddLink(value, "VLU");
        return firstNode;
    }
    public void DeleteSequence(Thought t)
    {
        //make sure there's only one reference to this sequence
        if (!IsSequenceFirstElement(t)) return;
        if (t.LinksFrom.Count(x=>x.LinkType.Label != "FRST") > 1) return;
        //follow the chain and delete the elements.
        Thought current = t;
        Thought next = t.To;
        while (current is not null)
        {  //This replicates DeleteThought but eliminates problems of re-entrance
            next = current.To;  
            foreach (Thought r in current.LinksTo)
                t.RemoveLink(r);
            foreach (Thought r in current.LinksFrom)
                r.From.RemoveLink(r);
            ThoughtLabels.RemoveThoughtLabel(current.Label);
            lock (AllThoughts)
                AllThoughts.Remove(current);
            current = next;
        }
    }
    public Thought CreateRawSequence(List<Thought> targets, string baseLabel = "seq*")
    {
        if (targets.Count < 2) return null;
        Thought newTarget = targets[0];

        Thought firstElement = CreateFirstElement(baseLabel, newTarget);
        Thought prevElement = firstElement;

        for (int i = 1; i < targets.Count; i++)
        {
            newTarget = targets[i];
            Thought newElement = AddElement(prevElement, newTarget);
            prevElement = newElement;
        }
        return firstElement;
    }

    /// <summary>
    /// Adds a sequence of Thoughts as ordered links from a source Thought
    /// Can handle nested sequences - targets can be letters OR sequence start nodes
    /// </summary>
    /// <param name="source">The 'owner' of the sequence</param>
    /// <param name="linkType">The type of link</param>
    /// <param name="targets">The target Thoughts (can be any Thoughts including sequence start of other sequences)</param>
    /// <param name="baseWeight">The base weight of the links</param>
    /// <returns></returns>
    public Thought AddSequence(Thought source, Thought linkType, List<Thought>  targets, float baseWeight = 1.0f)
    {
        if (targets.Count < 2) return null;  //a sequence must have at least 2 elements

        //clear out any existing sequence links of this type
        source.RemoveLinks(linkType);  //TODO delete the sequence

        List<Thought> resolvedTargets = new();
        //do any of the targets point to thoughts which have sequences of the same LinkType
        foreach (Thought target in targets)
        {
            Thought t1 = target.LinksTo.FindFirst(x => x.LinkType == linkType)?.To;
            if (!IsSequenceElement(target) && IsSequenceElement(t1))
                    resolvedTargets.Add(t1);
            else
                resolvedTargets.Add(target);
        }

        //check for any existing sequences which begins with the targets[stargIndes]
        (Thought seqStart, int length) FindExistingSubsequence(int startIndex)
        {
            int remaining = targets.Count - startIndex;
            for (int len = remaining; len >= 2; len--)
            {
                List<Thought> slice = targets.GetRange(startIndex, len);
                var matches = HasSequence(slice, linkType);
                foreach (var match in matches)
                {
                    Thought seqStartNode = match.r?.To;
                    if (seqStartNode is null || !IsSequenceElement(seqStartNode)) continue;
                    if (match.confidence < 1.0f) continue;
                    if (GetSequenceLength(seqStartNode) != len) continue; // exact-length match
                    return (seqStartNode, len);
                }
            }
            return (null, 0);
        }

        //Are there any existing seqnences in the target list?
        // edit the resolved target list that reuses any existing subsequences
        for (int i = 0; i < resolvedTargets.Count;i++)
        {
            (Thought seqStart, int length) = FindExistingSubsequence(i);
            if (seqStart is not null)
            {
                resolvedTargets.RemoveRange(i, length);
                resolvedTargets.Insert(i,seqStart);
                continue;
            }

        }

        // does this one already exist?
        var existingSequences = RawSearch(resolvedTargets);
        foreach (var t in existingSequences)
        {
            if (IsSequenceFirstElement(t.seqNode) && IsSequenceLastElement(t.curPos.Current))
            {
                source.AddLink(t.seqNode, linkType);
                return t.seqNode;
            }
        }

        //Finally, create the sequence and link to it
        Thought rawSequence = CreateRawSequence(resolvedTargets,source.Label);
        source.AddLink(rawSequence, linkType);
        return rawSequence;
    }

    //TODO: make mustMatchLast, circularSearch & allowOutOfOrder work
    /// <summary>
    /// This determines how well two Thoughts match in terms of the order of their ordered attributes.
    /// </summary>
    /// <param name="targets">This is the pattern we are searching for</param>
    /// <param name="linkType">If specified, specifies the link type to follow, otherwise all sequential linkTypes are matched</param>
    /// <param name="circularSearch">If true, circularizes the search of the candidate (for visuals)</param>
    /// <param name="firstLastPriority">If true, prioritizes matches with first and last elements matching</param>
    /// <returns>Confidence that the pattern exists in the candidate</returns>
    public List<(Thought r, float confidence)> HasSequence(List<Thought> targets, Thought linkType, 
        bool mustMatchFirst = false, bool mustMatchLast = false, bool circularSearch = false, bool allowOutOfOrder = false)
    {
        //this function searches the UKS for sequences matching the specified pattern in targets. 

        //If circularSearch is true, then the search will consider sequences that wrap around from end to start Thought.
        //If firstLastPriority is true, then matches that have the first and last elements matching will be given higher confidence
        ///AND the order of the elements will be lower priority if all intervening elements are found.  
        ///Example: given the stored sequance S E A T , S A E T would match with higher confidence than E S A T
        ///

        // Returns a list of tuples of (Thought to the start of the matching sequence, confidence value between 0 and 1)

        //CASE 0:  elements must be in exact order, no circular search, no first/last priority
        //Ignoring, for now, the circularSearch and firstLastPriority options
        //start by finding all sequences that contain the first target.  This is a superset of the actual matches.
        // it is target[0].LinksFrom.Where(r => r.LinkType.Label == "VLU") 
        // this builds an initial list of candidate sequence entry points
        // then we can walk each sequence to see if it matches the full pattern and remove from the candidate list if it does not match
        // that is: for targets[1] create a new list of candidates that have targets[1] as the VLU of the NXT link from the first candidate list
        // then match the two lists: if an entry in the new list does not have a LinkFrom an enement in list 1 with LinkType NXT, it is removed from the candidate list
        // if it does match, we put this new element in the initial ist as the new candidate for the next iteration.
        // repeat for all targets
        // at the end, the candidate list contains only sequences that match all targets in order and includes partial matches
        // we can then calculate confidence based on how many targets were matched in order
        // a perfect match will have a Final Next link back to the source Thought and the source thought will have a link of linkType to the first element in the sequence
        // return the list of source thoughts and their confidence values

        List<(Thought r, float confidence)> retVal = new();

        // Handle edge cases
        if (targets is null || targets.Count == 0) return retVal;
        if (targets[0] is null) return retVal;

        // Step 1: Find all sequence nodes that have targets[0] as their VLU
        // These are potential starting points for matching sequences

        // When this returns, seqNode is the first matching node.  curPos.Current is the last
        List<(Thought seqNode, IEnumerator<Thought>? curPos, int matchCount)> searchCandidates = RawSearch(targets);
        if (searchCandidates.Count == 0) return retVal;

        if (mustMatchFirst)
        {
            for (int j = 0; j < searchCandidates.Count; j++)
                if (!IsSequenceFirstElement(searchCandidates[j].seqNode))
                {
                    searchCandidates.RemoveAt(j);
                    j--;
                }
            searchCandidates = searchCandidates
                .Where(c => IsSequenceFirstElement(c.seqNode))
                .ToList();
        }


        if (mustMatchLast)
        {
            for (int j = 0; j < searchCandidates.Count; j++)
                if (!IsSequenceLastElement(searchCandidates[j].curPos.Current))
                {
                    searchCandidates.RemoveAt(j);
                    j--;
                }
        }


        // Step 3: Calculate confidence and find the Thoughts that reference these sequences
        foreach (var candidate in searchCandidates)
        {
            // Find FRST link from the candidate node to get the sequence's first node
            Thought firstSeqNode = (Thought)candidate.seqNode.LinksTo.FindFirst(r => r.LinkType?.Label == "FRST")?.To;

            if (firstSeqNode is null) continue;

            // Find all Thoughts that reference this sequence (have links pointing to firstSeqNode)
            var referencingThoughts = firstSeqNode.LinksFrom
                ?.Where(r => ((linkType is null || r.LinkType == linkType) && r.LinkType?.Label != "FRST"))
                .ToList();

            // Recursively add sequences that reference these
            if (referencingThoughts is not null)
            {
                var allReferencingThoughts = new List<Thought>(referencingThoughts);
                AddReferencingSequences(referencingThoughts, linkType, allReferencingThoughts);
                referencingThoughts = allReferencingThoughts;
            }

            if (referencingThoughts is not null)
            {
                foreach (var refRel in referencingThoughts)
                {
                    if (refRel.From is not null)
                    {
                        firstSeqNode = refRel.To;
                        // Calculate confidence
                        float confidence;
                        if (candidate.matchCount == targets.Count)
                        {
                            // Full match - check if it's a complete sequence
                            int actualSequenceLength = GetSequenceLength(firstSeqNode);

                            if (actualSequenceLength == targets.Count)
                                confidence = 1.0f; // Perfect complete match
                            else
                                // Matched all targets but sequence is longer than pattern
                                confidence = (float)candidate.matchCount / actualSequenceLength;
                        }
                        else
                        {
                            // Partial match
                            confidence = (float)candidate.matchCount / targets.Count;
                        }

                        // Add the link from the referencing Thought to the sequence
                        retVal.Add((refRel, confidence));
                    }
                }
            }
        }

        // Sort by confidence descending and remove duplicates
        retVal = retVal
            .GroupBy(x => x.r)
            .Select(g => g.OrderByDescending(x => x.confidence).First())
            .OrderByDescending(x => x.confidence)
            .ToList();

        return retVal;
    }

    // searches for a sequence given a list of targets
    private List<(Thought seqNode, IEnumerator<Thought>? curPos, int matchCount)> RawSearch(List<Thought> targets)
    {
        List<(Thought seqNode, IEnumerator<Thought>? curPos, int matchCount)> searchCandidates = new();
        if (targets is null || targets.Count < 2) return searchCandidates;

        //Step 1: initialize enuerators for each candidate sequence
        var candidateNodes = targets[0].LinksFrom
            .Where(r => r.LinkType?.Label == "VLU")
            .Select(r => (seqNode: r.From, matchedCount: 1))
            .ToList();
        foreach (var candidate in candidateNodes)
        {
            var enumerator = EnumerateSequenceElements(candidate.seqNode).GetEnumerator();
            searchCandidates.Add(new(candidate.seqNode, enumerator, 1));
            searchCandidates.Last().curPos.MoveNext();
        }

        // Step 2: For each subsequent target, filter candidates by following NXT links
        for (int i = 1; i < targets.Count; i++)
        {
            Thought currentTarget = targets[i];
            if (currentTarget is null) break; // Stop if we hit a null target

            for (int j = 0; j < searchCandidates.Count; j++)
            {
                Thought? nextThought = null;
                Thought theValue = null;
                do //skip over "+" placeholders
                {
                    //have we reached the end of the current subsequence?
                    if (!searchCandidates[j].curPos.MoveNext())
                    {
                        var referrers = GetAllFollowingNodes(searchCandidates[j].seqNode);
                        foreach (var referrer in referrers)
                        {
                            var x = searchCandidates.FindFirst(x => x.seqNode == referrer);
                            if (x.seqNode is null)
                                searchCandidates.Add(new(referrer, EnumerateSequenceElements(referrer).GetEnumerator(), searchCandidates[j].matchCount));
                        }
                        break;
                    }
                    nextThought = searchCandidates[j].curPos.Current;
                } while (GetElementValue(nextThought).Label == "+");
                // Check if the next thought matches the current target
                if( GetElementValue(nextThought) != currentTarget)
                {
                    searchCandidates.RemoveAt(j);
                    j--; // Adjust index after removal
                }
                else
                {
                    int temp = searchCandidates[j].matchCount; //hack to increment a value within a tuple
                    temp++;  
                    searchCandidates[j] = (searchCandidates[j].seqNode, searchCandidates[j].curPos, temp);
                }
            }
        }

        return searchCandidates;
    }

    List<Thought> GetAllFollowingNodes(Thought node)
    {
        List<Thought> retVal = new();
        //if this is a subsequence, get the caller(s)
        Thought startOfSequence = GetFirstElement(node);
        List<Thought> referrers = startOfSequence.LinksFrom.Where(x => x.LinkType.Label == "VLU").ToList();
        foreach (var referrer in referrers)
        {
            Thought nextLocation = referrer.From.To;
            if (nextLocation is not null)
                retVal.Add(nextLocation);
            else
                retVal.AddRange(GetAllFollowingNodes(referrer.From));
        }
        return retVal;
    }

    /// <summary>
    /// Flatten a sequence into a list of leaf Thoughts (letters)
    /// Handles nested sequences automatically by following VLU pointers
    /// </summary>
    public List<Thought> FlattenSequence(Thought sequenceStart)
    {
        if (sequenceStart.Label.StartsWith("abstr"))
        { }
        //experimentating with an enumartor for sequences
        List<Thought> result = new();
        var e = EnumerateSequenceElements(sequenceStart).GetEnumerator();
        while (e.MoveNext())
            result.Add(GetElementValue(e.Current));
        //foreach (Thought t in EnumerateSequenceElements(sequenceStart))
        //    result.Add(t);
        return result;
    }

    /// <summary>
    /// Enumerates all leaf elements in a sequence, recursively traversing into subsequences.
    /// This allows matching patterns that span across subsequence boundaries.
    /// Protected against circular subsequence references (A contains B contains A).
    /// </summary>
    /// <param name="sequenceStart">The first node of the sequence</param>
    /// <param name="visitedSequences">Optional set to track visited sequences across recursive calls</param>
    /// <returns>Enumerable of all leaf elements in order</returns>
    public IEnumerable<Thought> EnumerateSequenceElements(Thought sequenceStart, Stack<Thought> visitedSequences = null)
    {
        if (sequenceStart is null) yield break;

        // Initialize visited sequences tracker if this is the top-level call
        if (visitedSequences is null) visitedSequences = new();

        // Check if we've already processed this sequence (circular reference protection)
        if (visitedSequences.Contains(sequenceStart)) yield break; // Already visited this sequence, stop to prevent infinite recursion

        visitedSequences.Push(sequenceStart);

        var current = sequenceStart;
        //        var visitedNodes = new HashSet<Thought>(); // Track nodes within this sequence

        while (current is not null)
        {
            // Get the VLU Linkto find what this sequence node points to
            var valueRel = GetElementValue(current);

            if (IsSequenceElement(valueRel))
            {
                //Fire the owner
                var owner = valueRel?.LinksFrom.FindFirst(x => x.LinkType.Label != "VLU"&& x.LinkType.Label != "FRST")?.From;
                //owner?.Fire();
                // Recursively enumerate the subsequence, passing the shared visitedSequences set
                foreach (var subElement in EnumerateSequenceElements(valueRel, visitedSequences))
                {
                    yield return subElement;
                }
            }
            else
            {
                // It's a leaf element, return it
                yield return current;
            }

            // Move to next node via NXT Link
            current = GetNextElement(current);

            if (current is null) break;

            // Stop if we've circled back to the start
            if (current == sequenceStart) break;  //BROKEN
        }
        visitedSequences.Pop();
    }
    /// <summary>
    /// Recursively finds all sequences that reference the given sequence
    /// </summary>
    private void AddReferencingSequences(List<Thought> currentReferences, Thought linkType, List<Thought> accumulator)
    {
        if (currentReferences is null || currentReferences.Count == 0) return;

        var visited = new HashSet<Thought>();

        foreach (var link in currentReferences)
        {
            if (link.From is null || visited.Contains(link.From)) continue;
            visited.Add(link.From);

            // Check if this Thought is part of a sequence (has FRST link)
            var frstLink = link.From.LinksTo?.FirstOrDefault(r => r.LinkType?.Label == "FRST");
            if (frstLink?.To is null) continue;

            // Find what references this sequence
            var parentReferences = frstLink.To.LinksFrom
                ?.Where(r => ((linkType is null || r.LinkType == linkType) &&
                              r.LinkType?.Label != "FRST" &&
                              !accumulator.Contains(r)))
                .ToList();

            if (parentReferences is not null && parentReferences.Count > 0)
            {
                accumulator.AddRange(parentReferences);
                accumulator.Remove(link);
                // Recurse to find sequences that reference these
                AddReferencingSequences(parentReferences, linkType, accumulator);
            }
        }
    }
}
