//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class SequenceComparer : IComparer<Thing>
    {
        public int Compare(Thing x, Thing y)
        {
            if (x.useCount > y.useCount) return -1;
            else if(y.useCount > x.useCount) return 1;
            else if(x.lastFiredTime < y.lastFiredTime) return 1;
            else return -1;
        }
    }

    public class ModuleWordSequencing : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;

        [XmlIgnore]
        public List<Thing> inputWords = new();

        const int maxSeqLength = 10;
        const int maxSequencesRemembered = 30;
        const int minWordsInSequence = 2;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleWordSequencing()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        private Thing FindSequence(List<Thing> toFind)
        {
            GetUKS();
            Thing sequenceParent = UKS.GetOrAddThing("Sequences", "Behavior");
            foreach (Thing toCompare in sequenceParent.Children)
            {
                List<Thing> toCompareRefs = toCompare.RelationshipsAsThings;
                if (toCompareRefs.Count != toFind.Count) continue;
                bool refsMatch = true;
                for (int i = 0; i < toCompareRefs.Count; i++)
                {
                    if (toCompareRefs[i].Label.ToLower() != toFind[i].Label.ToLower())
                    {
                        refsMatch = false;
                        break;
                    }
                }
                if (refsMatch)
                    return toCompare;
            }
            return null;
        }

        //break the list of intput words into subarrays to create sequences
        public void AddSequences()
        {
            int startingPoint = 0;
            while (startingPoint < inputWords.Count - 1)
            {
                startingPoint = CreateSequences(startingPoint);
            }

            inputWords.Clear();
        }

        //create sequences based on subarrays of the list of input words
        public int CreateSequences(int startingPoint)
        {
            int leftPtr = startingPoint, rightPtr = leftPtr;
            List<Thing> sequence = new();
            while (leftPtr < inputWords.Count - 1 && leftPtr < startingPoint + maxSeqLength - 1)
            {
                rightPtr = leftPtr;
                while (rightPtr < inputWords.Count && rightPtr < startingPoint + maxSeqLength)
                {
                    sequence.Add(inputWords[rightPtr]);
                    if (sequence.Count > minWordsInSequence-1)
                    {
                        Thing foundSequence = FindSequence(sequence);
                        if (foundSequence == null)
                        {
                            PruneSequence();
                            Thing sequenceParent = UKS.GetOrAddThing("Sequences", "Behavior");
                            Thing newSeq = UKS.GetOrAddThing("words*", sequenceParent);
                            foreach (Thing word in sequence)
                                newSeq.AddRelationship(word);
                            foundSequence = newSeq;
                        }
                        foundSequence.SetFired();
                    }
                    rightPtr++;
                }
                sequence = new();
                leftPtr++;
            }
            return rightPtr;
        }

        private List<Thing> GetWordSequences()
        {
            GetUKS();
            Thing sequenceParent = UKS.GetOrAddThing("Sequences", "Behavior");
            List<Thing> sequences = new();
            foreach (Thing sequence in sequenceParent.Children)
            {
                if (sequence.Label.StartsWith("words"))
                    sequences.Add(sequence);
            }
            return sequences;
        }

        //remove the least used sequence to make room for a new one
        private void PruneSequence()
        {
            List<Thing> sequences = GetWordSequences();

            if (sequences.Count >= maxSequencesRemembered)
            {
                SequenceComparer sequenceComparer = new();
                sequences.Sort((x, y) => sequenceComparer.Compare(x,y));
                UKS.DeleteThing(sequences.Last());
                sequences.Remove(sequences.Last());
            }
        }

        public List<Thing> PredictSequence(List<string> sequenceStart)
        {
            List<Thing> sequences = GetWordSequences();
            List<Thing> matches = new();
            foreach(Thing sequence in sequences)
            {
                List<Thing> sequenceWords = sequence.RelationshipsAsThings;
                bool match = true;
                for(int i = 0; i < sequenceStart.Count; i++)
                {
                    if (i >= sequenceWords.Count || "w" + sequenceStart[i].ToLower() != sequenceWords[i].Label.ToLower())
                    {
                        match = false;
                        break;
                    }
                }

                if (match == true)
                    matches.Add(sequence);
            }

            return matches;
        }
    }
}