using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    //The structure of a sequence is a series of Links of LinkType "NXT" with a To of the next element in the sequence
    //Each of these elements also has a link of LinkType "VLU" to the actual Thought in the sequence
    //the "owner" of the sequences had a link of LinkType relType to the first element in the sequence
    //the last element in the sequence has a link of LinkType "NXT" with a To of null
    //Example, to represent the spelling of "CAT":
    // [cat -> spelled -> seq0]
    // seq0 --NXT--> seq1 --NXT--> seq2 --NXT--> null
    // seq0 --VLU--> C
    // seq1 --VLU--> A
    // seq2 --VLU--> T
    // NOTE: the elements seq* need not have labels at all, they are just used here for clarity
    // each seq* element must also have a FRST releationship back to the owner Thought
    // seq0 -> FRST -> cat
    // seq1 -> FRST -> cat
    // seq2 -> FRST -> cat

    // A few Special cases can be detected by comparing targets
    // Start of sequence:  seq->NXT = seq->FRST
    // End of sequence:    seq->NXT = null 


    public bool IsSequenceElement(Thought t)
    {
        return (t?.LinkType?.Label == "NXT");
    }
    public bool IsSequenceFirstElement(Thought t)
    {
        if (t?.LinkType?.Label != "NXT") return false;
        Thought t1 = GetFirstElement(t);
        return object.ReferenceEquals(t1, t); //use this to check for same object becaue == is be overloaded
    }
    public Thought GetNextElement(Thought t)
    {
        if (t?.LinkType?.Label != "NXT") return null;
        return t?.To;
    }
    public Thought GetFirstElement(Thought t)
    {
        if (t?.LinkType?.Label != "NXT") return null;
        return t?.LinksTo.FindFirst(x => x.LinkType.Label == "FRST")?.To;
    }
    public List<Thought> GetValueElement(Thought t)
    {
        if (t?.LinkType?.Label != "NXT") return null;
        return t?.LinksTo.Where(x => x.LinkType.Label == "VLU").Select(x => x.To).ToList();
    }
    private int GetSequenceLength(Thought firstNode)
    {
        if (firstNode is null) return 0;
        return FlattenSequence(firstNode).Count;
    }

    public Thought AddElement(Thought prevElement, Thought value)
    {
        Thought newNode = new Thought() { Label = prevElement.Label + "*", LinkType = (Thought)"NXT", To = null };  //the label will auto-increment.
        newNode.From = newNode;
        newNode.AddLink(GetFirstElement(prevElement), "FRST");
        newNode.AddLink(value, "VLU");
        prevElement.From = prevElement;
        prevElement.LinkType = (Thought)"NXT";
        prevElement.To = newNode;
        return newNode;
    }
    public Thought CreateFirstElement(Thought source, Thought value)
    {
        Thought firstNode = new Thought() { Label = source.Label.ToLower() + "-seq0" };
        firstNode.AddLink(firstNode, "FRST"); // Points to source as first node
        firstNode.AddLink(value, "VLU");
        firstNode.From = firstNode;
        firstNode.LinkType = (Thought)"NXT";
        firstNode.To = null;
        return firstNode;
    }

    /// <summary>
    /// Adds a sequence of Things as ordered links from a source Thought
    /// Can handle nested sequences - targets can be letters OR sequence start nodes
    /// </summary>
    /// <param name="source">The 'owner' of the sequence</param>
    /// <param name="relType">The type of link</param>
    /// <param name="targets">The target Things (can be letters or sequence start nodes)</param>
    /// <param name="baseWeight">The base weight of the links</param>
    /// <returns></returns>
    public Thought AddSequence(Thought source, Thought relType, List<Thought> targets, float baseWeight = 1.0f)
    {
        if (targets.Count < 2) return null;  //a sequence must have at least 2 elements

        //clear out any existing sequence links of this type
        source.RemoveLinks(relType);

        //check for any existing sequences in the targets and incorporate them into the new sequence instead of the given targets
        (Thought seqStart, int length) FindExistingSubsequence(int startIndex)
        {
            int remaining = targets.Count - startIndex;
            for (int len = remaining; len >= 2; len--)
            {
                List<Thought> slice = targets.GetRange(startIndex, len);
                var matches = HasSequence(slice, relType);
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

        // Build a resolved target list that reuses any existing subsequences
        List<Thought> resolvedTargets = new();
        for (int i = 0; i < targets.Count;)
        {
            (Thought seqStart, int length) = FindExistingSubsequence(i);
            if (seqStart is not null)
            {
                resolvedTargets.Add(seqStart);
                i += length;
                continue;
            }

            Thought candidate = targets[i];
            Thought existingSeq = candidate?.LinksTo.FindFirst(x => x.LinkType == relType && IsSequenceElement(x.To))?.To;
            resolvedTargets.Add(existingSeq ?? candidate);
            i++;
        }
        //does this target point to a LEAF or a subsequence?
        Thought newTarget = resolvedTargets[0];
        Thought seqTarget = newTarget.LinksTo.FindFirst(x => x.LinkType == relType)?.To;
        if (IsSequenceElement(seqTarget))
            newTarget = seqTarget;

        Thought firstElement = CreateFirstElement(source, newTarget);
        source.AddLink(firstElement, relType);
        Thought prevElement = firstElement;

        for (int i = 1; i < resolvedTargets.Count; i++)
        {
            newTarget = resolvedTargets[i];
            if (newTarget is null) continue;
            seqTarget = newTarget.LinksTo.FindFirst(x=>x.LinkType == relType)?.To;
            if (IsSequenceElement(seqTarget))
                newTarget = seqTarget;
            
            Thought newElement = AddElement(prevElement, newTarget);
            prevElement = newElement;
        }
        return firstElement;
    }

    //TODO add concept of "near" Things
    //TODO: make mustMatchLast, circularSearch & allowOutOfOrder work
    /// <summary>
    /// This determines how well two Things match in terms of the order of their ordered attributes.
    /// </summary>
    /// <param name="targets">This is the pattern we are searching for</param>
    /// <param name="relType">If specified, specifies the link type to follow, otherwise all sequential relTypes are matched</param>
    /// <param name="circularSearch">If true, circularizes the search of the candidate (for visuals)</param>
    /// <param name="firstLastPriority">If true, prioritizes matches with first and last elements matching</param>
    /// <returns>Confidence that the pattern exists in the candidate</returns>
    public List<(Thought r, float confidence)> HasSequence(List<Thought> targets, Thought relType, 
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
        // it is target[0].LinksFrom.Where(r => r.RelType.Label == "VLU") 
        // this builds an initial list of candidate sequence entry points
        // then we can walk each sequence to see if it matches the full pattern and remove from the candidate list if it does not match
        // that is: for targets[1] create a new list of candidates that have targets[1] as the VLU of the NXT link from the first candidate list
        // then match the two lists: if an entry in the new list does not have a LinkFrom an enement in list 1 with RelType NXT, it is removed from the candidate list
        // if it does match, we put this new element in the initial ist as the new candidate for the next iteration.
        // repeat for all targets
        // at the end, the candidate list contains only sequences that match all targets in order and includes partial matches
        // we can then calculate confidence based on how many targets were matched in order
        // a perfect match will have a Final Next link back to the source Thought and the source thought will have a link of relType to the first element in the sequence
        // return the list of source things and their confidence values

        List<(Thought r, float confidence)> retVal = new();

        // Handle edge cases
        if (targets is null || targets.Count == 0) return retVal;
        if (targets[0] is null) return retVal;

        // Step 1: Find all sequence nodes that have targets[0] as their VLU
        // These are potential starting points for matching sequences
        var candidateNodes = targets[0].LinksFrom
            .Where(r => r.LinkType?.Label == "VLU")
            .Select(r => (seqNode: r.From, matchedCount: 1))
            .ToList();
        if (mustMatchFirst && candidateNodes.Count > 0)
        {
            candidateNodes = candidateNodes
                .Where(c => IsSequenceFirstElement(c.seqNode))
                .ToList();
        }

        if (candidateNodes.Count == 0) return retVal;

        //get/initialize enuerators for each candidate sequence
        List<(Thought seqNode, IEnumerator<Thought>? curPos, int matchCount)> searchCandidates = new();
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
                } while (nextThought.Label == "+");
                // Check if the next thought matches the current target
                if (nextThought != currentTarget)
                {
                    searchCandidates.RemoveAt(j);
                    j--; // Adjust index after removal
                }
                else
                {
                    int temp = searchCandidates[j].matchCount;
                    temp++;
                    searchCandidates[j] = (searchCandidates[j].seqNode,searchCandidates[j].curPos, temp);
                }
            }
        }
        if (mustMatchLast)
        {
            for (int j = 0; j < searchCandidates.Count; j++)
                if (searchCandidates[j].curPos.MoveNext())
                {
                    searchCandidates.RemoveAt(j);
                    j--;
                }

        }


        candidateNodes = new();
        foreach (var entry in searchCandidates)
            candidateNodes.Add(new(entry.seqNode, entry.matchCount));

        // Step 3: Calculate confidence and find the Things that reference these sequences
        foreach (var candidate in candidateNodes)
        {
            // Find FRST link from the candidate node to get the sequence's first node
            Thought firstSeqNode = (Thought)candidate.seqNode.LinksTo.FindFirst(r => r.LinkType?.Label == "FRST")?.To;

            if (firstSeqNode is null) continue;

            // Find all Thoughts that reference this sequence (have links pointing to firstSeqNode)
            var referencingThings = firstSeqNode.LinksFrom
                ?.Where(r => ((relType is null || r.LinkType == relType) && r.LinkType?.Label != "FRST"))
                .ToList();

            // Recursively add sequences that reference these
            if (referencingThings is not null)
            {
                var allReferencingThings = new List<Thought>(referencingThings);
                AddReferencingSequences(referencingThings, relType, allReferencingThings);
                referencingThings = allReferencingThings;
            }

            if (referencingThings is not null)
            {
                foreach (var refRel in referencingThings)
                {
                    if (refRel.From is not null)
                    {
                        firstSeqNode = refRel.To;
                        // Calculate confidence
                        float confidence;
                        if (candidate.matchedCount == targets.Count)
                        {
                            // Full match - check if it's a complete sequence
                            int actualSequenceLength = GetSequenceLength(firstSeqNode);

                            if (actualSequenceLength == targets.Count)
                            {
                                confidence = 1.0f; // Perfect complete match
                            }
                            else
                            {
                                // Matched all targets but sequence is longer than pattern
                                confidence = (float)candidate.matchedCount / actualSequenceLength;
                            }
                        }
                        else
                        {
                            // Partial match
                            confidence = (float)candidate.matchedCount / targets.Count;
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
    /// Helper method to count the actual length of a sequence
    /// </summary>

    /// <summary>
    /// Flatten a sequence into a list of leaf Things (letters)
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
            result.Add(e.Current);
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
            // Get the VLU relationship to find what this sequence node points to
            var valueRel = GetValueElement(current)?[0];

            if (IsSequenceElement(valueRel))
            {
                //Fire the owner
                var owner = valueRel?.LinksFrom.FindFirst(x => x.LinkType.Label != "VLU"&& x.LinkType.Label != "FRST")?.From;
                owner?.Fire();
                // Recursively enumerate the subsequence, passing the shared visitedSequences set
                foreach (var subElement in EnumerateSequenceElements(valueRel, visitedSequences))
                {
                    yield return subElement;
                }
            }
            else
            {
                // It's a leaf element, return it
                yield return valueRel;
            }

            // Move to next node via NXT relationship
            current = current.To;

            if (current is null) break;

            // Stop if we've circled back to the start
            if (current == sequenceStart) break;  //BROKEN
        }
        visitedSequences.Pop();
    }
    /// <summary>
    /// Recursively finds all sequences that reference the given sequences
    /// </summary>
    private void AddReferencingSequences(List<Thought> currentReferences, Thought relType, List<Thought> accumulator)
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
                ?.Where(r => ((relType is null || r.LinkType == relType) &&
                              r.LinkType?.Label != "FRST" &&
                              !accumulator.Contains(r)))
                .ToList();

            if (parentReferences is not null && parentReferences.Count > 0)
            {
                accumulator.AddRange(parentReferences);
                accumulator.Remove(link);
                // Recurse to find sequences that reference these
                AddReferencingSequences(parentReferences, relType, accumulator);
            }
        }
    }

}
