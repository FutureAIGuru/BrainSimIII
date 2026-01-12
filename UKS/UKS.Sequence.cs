using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Adds a sequence of Things as ordered relationships from a source Thing
    /// Can handle nested sequences - targets can be letters OR sequence start nodes
    /// </summary>
    /// <param name="source">The 'owner' of the sequence</param>
    /// <param name="relType">The type of relationship</param>
    /// <param name="targets">The target Things (can be letters or sequence start nodes)</param>
    /// <param name="baseWeight">The base weight of the relationships</param>
    /// <returns></returns>
    public Relationship AddSequence(Thing source, Thing relType, List<Thing> targets, float baseWeight = 1.0f)
    {
        if (targets.Count == 0) return null;

        // Create first node with FRST pointer
        Relationship firstNode = new Relationship() { Label = source.Label + "-seq0" };
        firstNode.AddRelationship(firstNode, "FRST"); // Points to itself as first node
        firstNode.AddRelationship(targets[0], "VLU");

        Relationship retVal = source.AddRelationship(firstNode, relType);

        var prevElement = firstNode;

        // Build chain of sequence nodes
        for (int i = 1; i < targets.Count; i++)
        {
            Relationship newNode = new Relationship() { Label = source.Label + "-seq" + i };
            newNode.AddRelationship(firstNode, "FRST");
            //does this target have a similar relationship?
            var target = targets[i].Relationships?.FindFirst(x => x.RelType == relType)?.Target;
            if (target == null) target = targets[i];
            newNode.AddRelationship(target, "VLU");
            prevElement.Source = prevElement;
            prevElement.RelType = (Thing)"NXT";
            prevElement.Target = newNode;
            prevElement = newNode;
        }
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
        //the sstructure of a sequence is a series of Relationships of RelType "NXT" with a target of the next element in the sequence
        //each of these elements also has a relationship of RelType "VLU" to the actual Thing in the sequence
        //the "owner" of the sequences had a relationship of RelType relType to the first element in the sequence
        //the last element in the sequence has a relationship of RelType "NXT" back to the owner Thing
        //Example, to represent the spelling of "CAT":
        // [cat -> spelled -> seq0]
        // seq0 --NXT--> seq1 --NXT--> seq2 --NXT--> cat
        // seq0 --VLU--> C
        // seq1 --VLU--> A
        // seq2 --VLU--> T
        // NOTE: the elements seq* need not have labels at all, they are just used here for clarity
        // each seq* element must also have a FRST releationship back to the owner Thing
        // seq0 -> FRST -> cat
        // seq1 -> FRST -> cat
        // seq2 -> FRST -> cat

        // A few Special cases can be detected by comparing targets
        // Start of sequence:  seq->NXT = seq->FRST
        // End of sequence:    seq->NXT = null 

        //If circularSearch is true, then the search will consider sequences that wrap around from end to start Thing.
        //If firstLastPriority is true, then matches that have the first and last elements matching will be given higher confidence
        ///AND the order of the elements will be lower priority if all intervening elements are found.  
        ///Example: given the stored sequance S E A T , S A E T would match with higher confidence than E S A T
        ///

        // Returns a list of tuples of (Relationship to the start of the matching sequence, confidence value between 0 and 1)

        //CASE 0:  elements must be in exact order, no circular search, no first/last priority
        //Ignoring, for now, the circularSearch and firstLastPriority options
        //start by finding all sequences that contain the first target.  This is a superset of the actual matches.
        // it is target[0].RelationshipsFrom.Where(r => r.RelType.Label == "VLU") 
        // this builds an initial list of candidate sequence entry points
        // then we can walk each sequence to see if it matches the full pattern and remove from the candidate list if it does not match
        // that is: for targets[1] create a new list of candidates that have targets[1] as the VLU of the NXT relationship from the first candidate list
        // then match the two lists: if an entry in the new list does not have a RelationshipFrom an enement in list 1 with RelType NXT, it is removed from the candidate list
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

        // Step 1: Find all sequence nodes that have targets[0] as their VLU
        // These are potential starting points for matching sequences
        var candidateNodes = targets[0].RelationshipsFrom
            .Where(r => r.RelType?.Label == "VLU")
            .Select(r => (seqNode: r.Source, matchedCount: 1))
            .ToList();

        if (candidateNodes.Count == 0) return retVal;

        // Step 2: For each subsequent target, filter candidates by following NXT relationships
        for (int i = 1; i < targets.Count; i++)
        {
            Thing currentTarget = targets[i];
            if (currentTarget == null) break; // Stop if we hit a null target

            // Find all sequence nodes that have currentTarget as their VLU
            var nodesWithCurrentValue = currentTarget.RelationshipsFrom
                .Where(r => r.RelType?.Label == "VLU")
                .Select(r => r.Source)
                .ToList();

            // Create new candidate list by checking if any existing candidate has NXT to these nodes
            var newCandidates = new List<(Thing seqNode, int matchedCount)>();

            foreach (var candidate in candidateNodes)
            {
                // Case A: Check if this candidate has a NXT relationship to any node with currentTarget as VLU
                var nextRelationships = (((Relationship)candidate.Item1)?.RelType?.Label == "NXT") ?
                    new List<Thing>() { ((Relationship)candidate.Item1).Target } : null;

                if (nextRelationships != null && nextRelationships.Count > 0)
                {
                    foreach (var nextRel in nextRelationships)
                    {
                        if (nodesWithCurrentValue.Contains(nextRel))
                        {
                            // Found a match! This NXT node has the correct VLU
                            newCandidates.Add((nextRel, candidate.matchedCount + 1));
                        }
                    }
                }
                else
                {
                    // Case B: No NXT relationship - need to navigate through parent sequences
                    // Get the FRST (first node of this sequence)
                    var sourceRel = candidate.seqNode.Relationships
                        ?.FirstOrDefault(r => r.RelType?.Label == "FRST");

                    if (sourceRel?.Target != null)
                    {
                        Thing firstNode = sourceRel.Target;

                        // Find all sequence nodes that have this firstNode as their VLU
                        var parentSequenceNodes = firstNode.RelationshipsFrom
                            ?.Where(r => r.RelType?.Label == "VLU")
                            .Select(r => r.Source)
                            .ToList();

                        if (parentSequenceNodes != null)
                        {
                            foreach (var parentNode in parentSequenceNodes)
                            {
                                // Follow NXT from these parent nodes
                                var parentNextRels = parentNode.Relationships
                                    ?.Where(r => r.RelType?.Label == "NXT")
                                    .ToList();

                                if (parentNextRels != null)
                                {
                                    foreach (var parentNextRel in parentNextRels)
                                    {
                                        if (nodesWithCurrentValue.Contains(parentNextRel.Target))
                                        {
                                            newCandidates.Add((parentNextRel.Target, candidate.matchedCount + 1));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            candidateNodes = newCandidates;
            if (candidateNodes.Count == 0) break; // No more candidates
        }

        // Step 3: Calculate confidence and find the Things that reference these sequences
        foreach (var candidate in candidateNodes)
        {
            // Find FRST relationship from the candidate node to get the sequence's first node
            Relationship firstSeqNode = (Relationship)candidate.seqNode.Relationships
                .FindFirst(r => r.RelType?.Label == "FRST")?.Target;
            if (firstSeqNode == null) continue;


            // Find all Things that reference this sequence (have relationships pointing to firstSeqNode)
            var referencingThings = firstSeqNode.RelationshipsFrom
                ?.Where(r => ((relType == null || r.RelType == relType) && r.RelType?.Label != "FRST"))
                .ToList();

            if (referencingThings != null)
            {
                foreach (var refRel in referencingThings)
                {
                    if (refRel.Source != null)
                    {
                        // Calculate confidence
                        float confidence;
                        if (candidate.matchedCount == targets.Count)
                        {
                            // Full match - check if it's a complete sequence
                            int actualSequenceLength = CountSequenceLength(firstSeqNode, null);

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

                        // Add the relationship from the referencing Thing to the sequence
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

    /// <summary>
    /// Helper method to count the actual length of a sequence
    /// </summary>
    private int CountSequenceLength(Relationship firstNode, Thing sourceThing)
    {
        if (firstNode == null) return 0;

        int count = 0;
        var visited = new HashSet<Thing>(); //prevent circular refernce problems
        var current = firstNode;

        while (current != null && !visited.Contains(current))
        {
            count++;

            visited.Add(current);

            // Follow NXT relationship
            if (current.RelType?.Label != "NXT") break;
            Relationship nextRel = (Relationship)current.Target;

            if (nextRel == null) break;

            // Stop if we've reached back to the source (completed the circle)
            if (sourceThing != null && nextRel.Target == sourceThing) break;

            current = nextRel;
        }

        return count;
    }

    /// <summary>
    /// Helper method to find the first node in a sequence by following FRST to owner, then finding owner's relationship to sequence start
    /// </summary>
    private Thing FindFirstSequenceNode(Thing currentNode, Thing sourceThing)
    {
        if (currentNode == null || sourceThing == null) return null;

        // The first sequence node is the one that the source Thing has a direct relationship to
        // and which has FRST pointing back to sourceThing
        var potentialFirstNodes = sourceThing.Relationships
            ?.Where(r => r.Target != null)
            .Select(r => r.Target)
            .ToList();

        if (potentialFirstNodes == null) return null;

        // Check each potential first node to see if it's part of the same sequence
        foreach (var node in potentialFirstNodes)
        {
            // Verify this node has FRST back to sourceThing
            var hasSourceBack = node.Relationships
                ?.Any(r => r.RelType?.Label == "FRST" && r.Target == sourceThing) ?? false;

            if (hasSourceBack)
            {
                // Check if we can reach currentNode by following NXT relationships
                if (CanReachNode(node, currentNode, sourceThing))
                {
                    return node;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Helper method to check if we can reach targetNode from startNode by following NXT relationships
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

            // Follow NXT relationship
            var nextRel = current.Relationships
                ?.FirstOrDefault(r => r.RelType?.Label == "NXT");

            if (nextRel == null) break;

            // Stop if we've reached back to the source (completed the circle)
            if (sourceThing != null && nextRel.Target == sourceThing) break;

            current = nextRel.Target;
        }

        return false;
    }

    private Relationship NextNode(Relationship currentNode)
    {
        //cases:  This is end of the sequence
        //        This is a reference to another sequence
        //        This is the end of a subsequence
        if (currentNode.RelType?.Label != "NXT") return null;
        return (Relationship)currentNode.Target;
    }

    /// <summary>
    /// Flatten a sequence into a list of leaf Things (letters)
    /// Handles nested sequences automatically by following VLU pointers
    /// </summary>
    public List<Thing> FlattenSequence(Thing sequenceStart)
    {
        List<Thing> result = new();
        if (sequenceStart.RelType?.Label != "NXT") return result;  //this is not a sequence 
        Thing current = sequenceStart;

        while (current != null)
        {
            // Get the VLU relationship
            var valueRel = current.Relationships
                ?.FirstOrDefault(r => r.RelType?.Label == "VLU");

            if (valueRel != null)
            {
                Thing valueTarget = valueRel.Target;

                // Is this VLU pointing to another sequence?
                var isSequenceStart = valueTarget.RelType?.Label == "NXT";

                if (isSequenceStart)
                {
                    // It's a nested sequence - flatten it recursively
                    // (This is conceptual recursion, not call-stack recursion)
                    result.AddRange(FlattenSequence(valueTarget));
                }
                else
                {
                    // It's a leaf node (letter) - add it
                    result.Add(valueTarget);
                }
            }
            if (current.RelType?.Label == "NXT")
                current = current.Target;
            else
                current = null;
        }

        return result;
    }
}
