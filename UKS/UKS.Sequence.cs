using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// adds a sequence of Things as ordered relationships from a source Thing
    /// </summary>
    /// <param name="source">The 'owner' of the sequence</param>
    /// <param name="relType">The type of relationship</param>
    /// <param name="targets">The target Things</param>
    /// <param name="baseWeight">The base weight of the relationships</param>
    /// <returns></returns>
    public Relationship AddSequence(Thing source, Thing relType, List<Thing> targets, float baseWeight = 1.0f)
    {
        if (targets.Count == 0) return null;
        //TODO check for already existing sequence
        Relationship newNode = new Relationship() { Label = "SEQ*" }; //creates a "floating" relationship...no explicity UKS entry
        Relationship retVal = source.AddRelationship(newNode, relType);
        newNode.AddRelationship(targets[0], "VALUE");
        newNode.AddRelationship(source, "SOURCE");
        var curElement = newNode;

        
        for (int i = 1; i < targets.Count; i++)
        {
            newNode = new Relationship() { Label = "SEQ*" };
            newNode.AddRelationship(targets[i], "VALUE");
            newNode.AddRelationship(source, "SOURCE");
            curElement.AddRelationship(newNode, "NEXT");
            curElement = newNode;
        }
        curElement.AddRelationship(source, "NEXT");
        return retVal;
    }

    //TODO add concept of "near" Things
    //TODO add option to require first and/or last entries to match
    /// <summary>
    /// This determines how well two Things match in terms of the order of their ordered attributes.
    /// </summary>
    /// <param name="targets">This is the pattern we are searching for</param>
    /// <param name="relType">If specified, specifies the relationship type to follow, otherwise all sequential relTypes are matched</param>
    /// <param name="circularSearch">If true, circularizes the search of the candidate (for visuals)</param>
    /// <param name="firstLastPriority">If true, prioritizes matches with first and last elements matching</param>
    /// <returns>Confidence that the pattern exists in the candidate</returns>
    public List<(Relationship r, float confidence)> HasSequence(List<Thing> targets, Thing relType, bool circularSearch = false, bool firstLastPriority = false)
    {
        //this function searches the UKS for sequences matching the specified pattern in targets. 
        //the sstructure of a sequence is a series of Relationships of RelType "NEXT" with a target of the next element in the sequence
        //each of these elements also has a relationship of RelType "VALUE" to the actual Thing in the sequence
        //the "owner" of the sequences had a relationship of RelType relType to the first element in the sequence
        //the last element in the sequence has a relationship of RelType "NEXT" back to the owner Thing
        //Example, to represent the spelling of "CAT":
        // [cat -> spelled -> seq0]
        // seq0 --NEXT--> seq1 --NEXT--> seq2 --NEXT--> cat
        // seq0 --VALUE--> C
        // seq1 --VALUE--> A
        // seq2 --VALUE--> T
        // NOTE: the elements seq* need not have labels at all, they are just used here for clarity
        // each seq* element must also have a SOURCE releationship back to the owner Thing
        // seq0 -> SOURCE -> cat
        // seq1 -> SOURCE -> cat
        // seq2 -> SOURCE -> cat

        //if circularSearch is true, then the search will consider sequences that wrap around from end to start
        //ningif firstLastPriority is true, then matches that have the first and last elements matching will be given higher confidence
        ///AND the order of the elements will be lower priority if all intervening elements are found.  
        ///Example: given the stored sequance S E A T , S A E T would match with higher confidence than E S A T
        ///

        // Returns a list of tuples of (Relationship to the start of the matching sequence, confidence value between 0 and 1)

        //CASE 0:  elements must be in exact order, no circular search, no first/last priority
        //Ignoring, for now, the circularSearch and firstLastPriority options
        //start by finding all sequences that contain the first target.  This is a superset of the actual matches.
        // it is target[0].RelationshipsFrom.Where(r => r.RelType.Label == "VALUE") 
        // this builds an initial list of candidate sequence entry points
        // then we can walk each sequence to see if it matches the full pattern and remove from the candidate list if it does not match
        // that is: for targets[1] create a new list of candidates that have targets[1] as the VALUE of the NEXT relationship from the first candidate list
        // then match the two lists: if an entry in the new list does not have a RelationshipFrom an enement in list 1 with RelType NEXT, it is removed from the candidate list
        // if it does match, we put this new element in the initial ist as the new candidate for the next iteration.
        // repeat for all targets
        // at the end, the candidate list contains only sequences that match all targets in order and includes partial matches
        // we can then calculate confidence based on how many targets were matched in order
        // a perfect match will have a Final Next relationship back to the source Thing and the source thing will have a relationship of relType to the first element in the sequence
        // return the list of source things and their confidence values

        List<(Relationship r, float confidence)> retVal = new List<(Relationship r, float confidence)>();

        // Handle edge cases
        if (targets == null || targets.Count == 0) return retVal;
        if (targets[0] == null) return retVal;

        // Step 1: Find all sequence nodes that have targets[0] as their VALUE
        // These are potential starting points for matching sequences
        var candidateNodes = targets[0].RelationshipsFrom
            .Where(r => r.RelType?.Label == "VALUE")
            .Select(r => (seqNode: r.Source, matchedCount: 1))
            .ToList();

        if (candidateNodes.Count == 0) return retVal;

        // Step 2: For each subsequent target, filter candidates by following NEXT relationships
        for (int i = 1; i < targets.Count; i++)
        {
            Thing currentTarget = targets[i];
            if (currentTarget == null) break; // Stop if we hit a null target

            // Find all sequence nodes that have currentTarget as their VALUE
            var nodesWithCurrentValue = currentTarget.RelationshipsFrom
                .Where(r => r.RelType?.Label == "VALUE")
                .Select(r => r.Source)
                .ToList();

            // Create new candidate list by checking if any existing candidate has NEXT to these nodes
            var newCandidates = new List<(Thing seqNode, int matchedCount)>();

            foreach (var candidate in candidateNodes)
            {
                // Check if this candidate has a NEXT relationship to any node with currentTarget as VALUE
                var nextRelationships = candidate.seqNode.Relationships
                    .Where(r => r.RelType?.Label == "NEXT");

                foreach (var nextRel in nextRelationships)
                {
                    if (nodesWithCurrentValue.Contains(nextRel.Target))
                    {
                        // Found a match! This NEXT node has the correct VALUE
                        newCandidates.Add((nextRel.Target, candidate.matchedCount + 1));
                    }
                }
            }

            candidateNodes = newCandidates;
            if (candidateNodes.Count == 0) break; // No more candidates
        }

        // Step 3: Calculate confidence and find the source Thing for each candidate
        foreach (var candidate in candidateNodes)
        {
            // Try to find the source Thing by following the NEXT relationship from the last matched node
            // and checking for SOURCE relationships
            Thing sourceThing = null;
            Relationship sequenceStartRelationship = null;

            // Find SOURCE relationship from the candidate node
            var sourceRelationships = candidate.seqNode.Relationships
                .Where(r => r.RelType?.Label == "SOURCE")
                .ToList();

            if (sourceRelationships.Count > 0)
            {
                sourceThing = sourceRelationships[0].Target;

                // Find the relationship from source to the first sequence node
                // We need to trace back to find the first node in this sequence
                Thing firstSeqNode = FindFirstSequenceNode(candidate.seqNode, sourceThing);

                if (firstSeqNode != null && sourceThing != null)
                {
                    // Find the relationship from source to first sequence node with the specified relType
                    sequenceStartRelationship = sourceThing.Relationships
                        .FirstOrDefault(r => r.Target == firstSeqNode && 
                                           (relType == null || r.RelType == relType));
                }

                // Calculate confidence: only 1.0 if we have a perfect complete match
                float confidence;
                if (candidate.matchedCount == targets.Count)
                {
                    // Check if the last node has NEXT back to source (complete circular sequence)
                    var nextToSource = candidate.seqNode.Relationships
                        .FirstOrDefault(r => r.RelType?.Label == "NEXT" && r.Target == sourceThing);

                        // Also verify we found the first node and it matches where we should have started
                    if (nextToSource != null && firstSeqNode != null && sequenceStartRelationship != null)
                    {
                        // Verify the complete sequence length matches
                        // Count the actual sequence length by walking from first to last
                        int actualSequenceLength = CountSequenceLength(firstSeqNode, sourceThing);
                        
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
                        // Matched all targets but didn't complete the full sequence
                        confidence = (float)candidate.matchedCount / targets.Count * 0.9f;
                    }
                }
                else
                {
                    // Partial match
                    confidence = (float)candidate.matchedCount / targets.Count;
                }

                if (sequenceStartRelationship != null)
                {
                    retVal.Add((sequenceStartRelationship, confidence));
                }
            }
        }

        // Sort by confidence descending
        retVal = retVal.OrderByDescending(x => x.confidence).ToList();

        return retVal;
    }

    /// <summary>
    /// Helper method to count the actual length of a sequence
    /// </summary>
    private int CountSequenceLength(Thing firstNode, Thing sourceThing)
    {
        if (firstNode == null || sourceThing == null) return 0;

        int count = 0;
        var visited = new HashSet<Thing>();
        var current = firstNode;

        while (current != null && !visited.Contains(current))
        {
            // Count this node if it has a VALUE relationship
            var hasValue = current.Relationships
                .Any(r => r.RelType?.Label == "VALUE");
            
            if (hasValue)
            {
                count++;
            }

            visited.Add(current);

            // Follow NEXT relationship
            var nextRel = current.Relationships
                .FirstOrDefault(r => r.RelType?.Label == "NEXT");

            if (nextRel == null) break;

            // Stop if we've reached back to the source (completed the circle)
            if (nextRel.Target == sourceThing) break;

            current = nextRel.Target;
        }

        return count;
    }

    /// <summary>
    /// Helper method to find the first node in a sequence by following SOURCE to owner, then finding owner's relationship to sequence start
    /// </summary>
    private Thing FindFirstSequenceNode(Thing currentNode, Thing sourceThing)
    {
        if (currentNode == null || sourceThing == null) return null;

        // The first sequence node is the one that the source Thing has a direct relationship to
        // and which has SOURCE pointing back to sourceThing
        var potentialFirstNodes = sourceThing.Relationships
            .Where(r => r.Target != null)
            .Select(r => r.Target)
            .ToList();

        // Check each potential first node to see if it's part of the same sequence
        foreach (var node in potentialFirstNodes)
        {
            // Verify this node has SOURCE back to sourceThing
            var hasSourceBack = node.Relationships
                .Any(r => r.RelType?.Label == "SOURCE" && r.Target == sourceThing);

            if (hasSourceBack)
            {
                // Check if we can reach currentNode by following NEXT relationships
                if (CanReachNode(node, currentNode, sourceThing))
                {
                    return node;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Helper method to check if we can reach targetNode from startNode by following NEXT relationships
    /// </summary>
    private bool CanReachNode(Thing startNode, Thing targetNode, Thing sourceThing)
    {
        if (startNode == targetNode) return true;

        var visited = new HashSet<Thing>();
        var current = startNode;

        while (current != null && !visited.Contains(current))
        {
            if (current == targetNode) return true;

            visited.Add(current);

            // Follow NEXT relationship
            var nextRel = current.Relationships
                .FirstOrDefault(r => r.RelType?.Label == "NEXT");

            if (nextRel == null) break;

            // Stop if we've reached back to the source (completed the circle)
            if (nextRel.Target == sourceThing) break;

            current = nextRel.Target;
        }

        return false;
    }

}
