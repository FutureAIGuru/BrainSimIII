//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BrainSimulator.Modules;

public class ModuleLearnSequence : ModuleBase
{
    //any public variable you create here will automatically be saved and restored  with the network
    //unless you precede it with the [XmlIgnore] directive
    //[XlmIgnore] 
    //public theStatus = 1;

    [XmlIgnore]
    public NeuronSequence neuronSequence = new();
    [XmlIgnore]
    public NeuronSequence lastSequence = new();
    [XmlIgnore]
    public List<NeuronSequence> storedSequences = new();
    [XmlIgnore]
    public int pauseLimit = 10; // the min generation/cycle count to denote a seperate new sequence
    [XmlIgnore]
    public int maxSequenceLength = 7;// the max neurons to be considered a sequence to add to storedSequences
    [XmlIgnore]
    public int minSequenceLength = 2; // the min neurons to be considered a sequence to add to storedSequences 
    [XmlIgnore]
    public int maxNumberOfSequences = 10; // the max number of sequences the storedSequences can hold
    [XmlIgnore]
    public List<TimedNeuron> sequenceFound = new(); // tracks what was fired
    [XmlIgnore]
    public int playPointer = 0; // the index of the neuron to fire when there is a partial match
    [XmlIgnore]
    public int initialPlayPointer = 0;
    [XmlIgnore]
    public long lastGenerationFired;
    [XmlIgnore]
    DateTime lastTimeFired = DateTime.Now;
    [XmlIgnore]
    DateTime lastPointerFiredTime = DateTime.Now;
    //                 day, hour, min, seconds, milliseconds
    [XmlIgnore]
    TimeSpan pauseTime = new(0, 0, 0, 2, 0); // 2 second default
    [XmlIgnore]
    public double playbackSpeed = 1.0; // multiply by speed of neuron completion fires. 2 plays twice as fast
    [XmlIgnore]
    public int matchIndex = 1;


    public TimeSpan GetPauseTime()
    {
        return pauseTime;
    }
    public int GetPauseTimeInSeconds()
    {
        return (int)pauseTime.TotalSeconds;
    }

    public void SetPauseTime(int seconds)
    {
        if (seconds < 0)
        {
            seconds = 0;
        }

        pauseTime = new(0, 0, 0, seconds, 0);
    }
    public void SetPauseTime(int days, int hours, int minutes, int seconds, int millseconds)
    {
        if (days < 0)
        {
            days = 0;
        }
        if (hours < 0)
        {
            hours = 0;
        }
        if (minutes < 0)
        {
            minutes = 0;
        }
        if (seconds < 0)
        {
            seconds = 0;
        }
        if (millseconds < 0)
        {
            millseconds = 0;
        }

        pauseTime = new(days, hours, minutes, seconds, millseconds);
    }

    public int GetPauseLimit()
    {
        return pauseLimit;
    }
    public void SetPauseLimit(int length)
    {
        if (length < 1)
        {
            length = 1;
        }

        pauseLimit = length;
    }

    public int GetMaxNumberOfSequences()
    {
        return maxNumberOfSequences;
    }
    public void SetMaxNumberOfSequences(int length)
    {
        if (length < 3)
        {
            length = 3;
        }

        maxNumberOfSequences = length;
    }

    public int GetMinSequenceLength()
    {
        return minSequenceLength;
    }
    public void SetMinSequenceLength(int length)
    {
        // hard min of 2
        if (length < 2)
        {
            length = 2;
        }

        minSequenceLength = length;
    }
    public int GetMaxSequenceLength()
    {
        return maxSequenceLength;
    }
    public void SetMaxSequenceLength(int length)
    {
        if (length <= minSequenceLength)
        {
            length = minSequenceLength + 1;
        }

        maxSequenceLength = length;
    }

    //set size parameters as needed in the constructor
    //set max to be -1 if unlimited
    public ModuleLearnSequence()
    {
        minHeight = 2;
        maxHeight = 500;
        minWidth = 2;
        maxWidth = 500;
    }

    public class TimedNeuron
    {
        public string NeuronID { get; set; }
        public DateTime TimeFired { get; set; }

        public TimedNeuron(string newNeuronID, DateTime newTimeFired)
        {
            NeuronID = newNeuronID;
            TimeFired = newTimeFired;
        }

        public TimedNeuron(string newNeuronID)
        {
            SetTimedNeuron(newNeuronID);
        }

        public void SetTimedNeuron(string newNeuronID)
        {
            NeuronID = newNeuronID;
            TimeFired = DateTime.Now;
        }
    }

    public class NeuronSequence
    {
        public List<TimedNeuron> sequence;
        public int useCount;
        public double lastPlaybackSpeed;

        public NeuronSequence()
        {
            sequence = new();
            useCount = 0;
            lastPlaybackSpeed = 1;
        }
        public NeuronSequence(List<TimedNeuron> sequence)
        {
            SetSequence(sequence);
            lastPlaybackSpeed = 1;
        }
        public NeuronSequence(List<TimedNeuron> sequence, int useCount)
        {
            // force initial to state
            SetSequence(sequence);
            SetUseCount(useCount);
            lastPlaybackSpeed = 1;
        }

        // Sequence
        public List<TimedNeuron> GetSequence()
        {
            return sequence;
        }
        public string GetSequenceAsString()
        {
            string result = "";
            if (GetSequence() is not null)
            {
                foreach (var timedNeuron in GetSequence())
                {
                    result += timedNeuron.NeuronID + " ";
                }
            }

            return result;
        }
        public void SetSequence(List<TimedNeuron> setSequence)
        {
            //For adding a full list, not one neuron
            sequence = setSequence;
            SetUseCount(1);
        }
        public void AddToSequence(string sequenceId)
        {
            TimedNeuron nueron = new(sequenceId);

            sequence.Add(nueron);

            if (useCount == 0)
            {
                IncrementUseCount();
            }
        }
        public void ClearSequence()
        {
            sequence = new();
            SetUseCount(0);
        }
        public int SequenceCount()
        {
            return sequence?.Count ?? 0;
        }
        public void RemoveIndexSequence(int id)
        {
            GetSequence().RemoveAt(id);
        }

        // UseCount
        public int GetUseCount()
        {
            return useCount;
        }
        public void SetUseCount(int count)
        {
            if (count >= 0)
            {
                useCount = count;
            }
        }
        public void IncrementUseCount()
        {
            useCount++;
        }
        public void DecrementUseCount()
        {
            useCount--;
        }
    }

    public static void SortStoredSequencesByUseCount(List<NeuronSequence> storedSequences)
    {
        storedSequences.Sort((q, p) => p.GetUseCount().CompareTo(q.GetUseCount()));
    }

    public static void PruneStoredSequencesLowestCount(List<NeuronSequence> storedSequences)
    {
        // delete the second from the end to not delete the newest, already sorted
        // called when trying to add but already past max number of sequences 
        if (storedSequences.Count >= 2)
        {
            storedSequences.RemoveAt(storedSequences.Count - 2);
        }
    }
    public static void PruneNeurnonSequencesToMaxSequenceLength(NeuronSequence neuronSequence, int maxSequenceLength)
    {
        if (neuronSequence.SequenceCount() >= 1 && neuronSequence.SequenceCount() > maxSequenceLength)
        {
            int countToDelete = neuronSequence.SequenceCount() - maxSequenceLength;
            while (countToDelete > 0)
            {
                neuronSequence.RemoveIndexSequence(neuronSequence.SequenceCount() - 1);
                countToDelete--;
            }
        }
    }

    public string NeuronListToString()
    {
        // to print to dialog
        StringBuilder sb = new();
        sb.AppendLine("Learned   Uses   Sequence");

        for (int i = 0; i < storedSequences.Count; i++)
        {
            if (storedSequences[i].GetUseCount() >= 1)
            {
                sb.AppendLine($"{i}\t{storedSequences[i].GetUseCount()}\t{storedSequences[i].GetSequenceAsString()}");
            }
        }

        return sb.ToString();
    }

    public bool IsPausedTime()
    {
        if (DateTime.Now > lastTimeFired.Add(pauseTime))
        {
            return false;
        }

        return true;
    }


    public void LoadNeuronSequenceWithFiredNeurons()
    {
        foreach (var neuron in mv.Neurons)
        {
            if (neuron.Fired())
            {
                neuronSequence.AddToSequence(neuron.Id.ToString());
                lastSequence = neuronSequence;
                lastTimeFired = DateTime.Now;
                lastGenerationFired = MainWindow.theNeuronArray.Generation;
                break;
            }
        }
    }

    public void IncrementStoredSequenceCountWhenMatchIsFound()
    {
        for (int i = 0; i < storedSequences.Count; i++)
        {
            bool matches = true;

            for (int j = 0; j < storedSequences[i].SequenceCount()
                && j < neuronSequence.SequenceCount() &&
                storedSequences[i].SequenceCount() != 0; j++)
            {

                if (storedSequences[i].GetSequence()[j].NeuronID != neuronSequence.GetSequence()[j].NeuronID)
                {
                    matches = false;
                    break;
                }
            }

            if (storedSequences[i].SequenceCount() == 0)
            {
                matches = false;
            }

            if (matches)
            {
                sequenceFound = storedSequences[i].sequence;
                matchIndex = i;
                storedSequences[i].IncrementUseCount();
                SortStoredSequencesByUseCount(storedSequences);
                playPointer = neuronSequence.SequenceCount();
                initialPlayPointer = playPointer;
                playbackSpeed = 1;
                break;
            }
        }
    }

    public void AddNewSequence()
    {
        if (neuronSequence.SequenceCount() > maxSequenceLength)
        {
            PruneNeurnonSequencesToMaxSequenceLength(neuronSequence, maxSequenceLength);
        }

        storedSequences.Add(neuronSequence);

        if (storedSequences.Count > maxNumberOfSequences)
        {
            PruneStoredSequencesLowestCount(storedSequences);
        }
    }

    public void BlinkNeuronAndIncrementPlayPointer()
    {
        if (IsTimeBetweenFiresTimeReached())
        {
            string sequenceFoundID = sequenceFound[playPointer].NeuronID;
            MainWindow.theNeuronArray.GetNeuron(sequenceFoundID).SetValue(1);

            playPointer++;
            lastPointerFiredTime = DateTime.Now;
        }
    }

    public double AverageLastSequenceTime()
    {
        DateTime lastFireTime = lastSequence.sequence[0].TimeFired;
        DateTime currentFireTime = lastSequence.sequence[^1].TimeFired;
        TimeSpan fireDifference = currentFireTime.Subtract(lastFireTime);

        double output = ((double)fireDifference.Ticks) / (lastSequence.sequence.Count);
        return output;
    }

    public double AverageStoredSequenceTime()
    {
        DateTime lastStoredTime = storedSequences[matchIndex].sequence[0].TimeFired;
        DateTime currentStoredTime = storedSequences[matchIndex].sequence[^1].TimeFired;
        TimeSpan storedDifference = currentStoredTime.Subtract(lastStoredTime);

        double output = ((double)storedDifference.Ticks) / (storedSequences[matchIndex].sequence.Count);
        return output;
    }

    public void UpdatePlaybackSpeed()
    {
        // set playbackSpeed based on ratio between stored and fired rates 
        if (lastSequence.SequenceCount() == 1)
        {
            //if sequence length is only one, use last playback speed of the stored sequence
            playbackSpeed = storedSequences[matchIndex].lastPlaybackSpeed;
            return;
        }

        playbackSpeed = AverageStoredSequenceTime() / AverageLastSequenceTime();
        storedSequences[matchIndex].lastPlaybackSpeed = playbackSpeed;
    }

    public bool IsTimeBetweenFiresTimeReached()
    {
        // Checks if enough time has passed to fire the next neuron
        // Wait time matches difference between saved fire times
        bool isDone = false;

        if (initialPlayPointer == playPointer)
        {
            UpdatePlaybackSpeed();
        }

        DateTime lastFireTime = sequenceFound[playPointer - 1].TimeFired;
        DateTime currentFireTime = sequenceFound[playPointer].TimeFired;
        TimeSpan fireDifference = currentFireTime.Subtract(lastFireTime);

        DateTime now = DateTime.Now;
        TimeSpan nowDifference = now.Subtract(lastPointerFiredTime);

        if (((double)nowDifference.Ticks * playbackSpeed) > (double)fireDifference.Ticks)
        {
            isDone = true;
        }

        return isDone;
    }

    public void ProcessSequence()
    {
        if (sequenceFound is not null)
        {
            if (playPointer >= sequenceFound.Count)
            {
                sequenceFound = null;
                return;
            }
            else
            {
                if (IsTimeBetweenFiresTimeReached())
                {
                    BlinkNeuronAndIncrementPlayPointer();
                }

                return;
            }
        }

        if (mv != null)
        {
            LoadNeuronSequenceWithFiredNeurons();
        }

        if (!IsPausedTime() && neuronSequence.SequenceCount() > 0)
        {
            IncrementStoredSequenceCountWhenMatchIsFound();

            if (sequenceFound is null || sequenceFound.Count == 0)
            {
                AddNewSequence();
            }
            if (neuronSequence.sequence.Count > 1)
            {
                lastSequence = neuronSequence;
            }

            neuronSequence = new();
        }
    }

    //fill this method in with code which will execute once for each cycle of the engine
    public override void Fire()
    {
        Init();  //be sure to leave this here

        //if you want the dlg to update, use the following code whenever any parameter changes
        UpdateDialog();

        ProcessSequence();
    }

    public void SetNeronLablesToId()
    {
        Neuron[] neurons = mv.Neurons.ToArray();

        for (int i = 0; i < neurons.Length; i++)
        {
            neurons[i].Label = neurons[i].id.ToString();
        }
    }


    //fill this method in with code which will execute once
    //when the module is added, when "initialize" is selected from the context menu,
    //or when the engine restart button is pressed
    public override void Initialize()
    {
        storedSequences = new();
        neuronSequence = new();

        SetNeronLablesToId();
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

        SetNeronLablesToId();
    }
}
