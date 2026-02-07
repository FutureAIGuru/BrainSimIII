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
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleSound : ModuleBase
{

    DateTime lastFiredTime = DateTime.Now;
    DateTime? lastNotePressed = null;
    DateTime lastCadenceTime = DateTime.Now;
    List<Thought> tuneToSearch = null;

    IEnumerator<Thought> enumerator = null;  //we'll needc multiple enumerators soon

    // Add these properties to the ModuleSound class
    public int Cadence { get; set; } = 100;
    public int PitchOffset { get; set; } = 0;



    public override void Fire()
    {
        Init();
        if (lastNotePressed is not null && DateTime.Now > lastNotePressed + TimeSpan.FromMilliseconds(2000))
        {
            if (tuneToSearch.Count > 1)
            {
                var tunesFound = theUKS.HasSequence(tuneToSearch, null);
                if (tunesFound.Count > 0)
                    tunesFound[0].r.From.Fire();
            }
            tuneToSearch = null;
            lastNotePressed = null;
        }

        var muscialPhrase = ((Thought)"MusicalPhrase");
        if (muscialPhrase is not null)
            foreach (Thought phrase in muscialPhrase.Children)
            {
                if (phrase is not null && phrase.LastFiredTime >= lastFiredTime)
                {
                    var seqStart = phrase;
                    if (!theUKS.IsSequenceElement(seqStart))
                        seqStart = phrase.LinksTo.FindFirst(x => theUKS.IsSequenceElement(x.To))?.To;

                    enumerator = theUKS.EnumerateSequenceElements(seqStart).GetEnumerator();
                }
            }

        if (DateTime.Now > lastCadenceTime + TimeSpan.FromMilliseconds(Cadence) && enumerator is not null)
        {
            lastCadenceTime = DateTime.Now;
            if (enumerator.MoveNext())
               theUKS.GetElementValue(enumerator.Current).Fire();
            else
                enumerator = null;
        }


        Thought musicalNote = "MusicalNote";
        if (musicalNote is not null)
            foreach (var note1 in ((Thought)"MusicalNote")?.Children)
            {
                if (note1.LastFiredTime <= lastFiredTime) continue;
                if (note1.Label == "+") continue;
                string pitch = note1.Label[5..];
                int note = pitch switch
                {
                    "C" => 60,
                    "D" => 62,
                    "E" => 64,
                    "F" => 65,
                    "G" => 67,
                    "A" => 69,
                    "B" => 71,
                    "C+" => 72,
                    _ => 60
                };
                note += PitchOffset;
                PlayNote(note);
            }

        lastFiredTime = DateTime.Now;
        UpdateDialog();
    }

    // Fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
    }

    // called whenever the UKS performs an Initialize()
    public override void UKSInitializedNotification()
    {
        GetUKS();
        theUKS.GetOrAddThought("MusicalPhrase");
        theUKS.GetOrAddThought("MusicalNote", "MusicalPhrase");
        theUKS.GetOrAddThought("pitchC", "MusicalNote");
        theUKS.GetOrAddThought("pitchD", "MusicalNote");
        theUKS.GetOrAddThought("pitchE", "MusicalNote");
        theUKS.GetOrAddThought("pitchF", "MusicalNote");
        theUKS.GetOrAddThought("pitchG", "MusicalNote");
        theUKS.GetOrAddThought("pitchA", "MusicalNote");
        theUKS.GetOrAddThought("pitchB", "MusicalNote");
        theUKS.GetOrAddThought("pitchC+", "MusicalNote");
        theUKS.GetOrAddThought("+", "MusicalNote");

        var s = theUKS.GetOrAddThought("soundAs", "Action");

        theUKS.AddSequence(theUKS.GetOrAddThought("Triad", "MusicalPhrase"), s, new List<Thought> { "pitchC", "+", "pitchE", "+", "pitchG", "+" });
        theUKS.AddSequence(theUKS.GetOrAddThought("ThreeBlindMice", "MusicalPhrase"), s, new List<Thought> { "pitchE", "+", "+", "pitchD", "+", "+", "pitchC", "+", "+", "+", "+", "+" });
        theUKS.AddSequence(theUKS.GetOrAddThought("SeeHowTheyRun", "MusicalPhrase"), s, new List<Thought> { "pitchG", "+", "+", "pitchF", "+", "pitchF", "pitchE", "+", "+", "+", "+", "+" });
        theUKS.AddSequence(theUKS.GetOrAddThought("TheyAllRanAfter", "MusicalPhrase"), s,
            new List<Thought> { "pitchG", "pitchC+", "+", "pitchC+", "pitchB", "pitchA", "pitchB", "pitchC+", "+", "pitchG", "pitchG", "+" });
        theUKS.AddSequence(theUKS.GetOrAddThought("TBL2", "MusicalPhrase"), s, new List<Thought> { "ThreeBlindMice-seq0", "ThreeBlindMice-seq0" });
        theUKS.AddSequence(theUKS.GetOrAddThought("SHTR2", "MusicalPhrase"), s, new List<Thought> { "SeeHowTheyRun-seq0", "SeeHowTheyRun-seq0" });
        theUKS.AddSequence(theUKS.GetOrAddThought("TARA3", "MusicalPhrase"), s, new List<Thought> { "TheyAllRanafter-seq0", "TheyAllRanafter-seq0", "TheyAllRanafter-seq0" });
        theUKS.AddSequence(theUKS.GetOrAddThought("TBLSong", "MusicalPhrase"), s,
            new List<Thought> { "TBL2-seq0", "SHTR2-seq0", "TARA3-seq0", "+", "pitchF", "ThreeBlindMice-seq0" });

        lastFiredTime = DateTime.Now;
    }


    public void FireNote(string noteLabel)
    {
        Thought note = "pitch" + noteLabel;
        if (tuneToSearch == null) tuneToSearch = new();
        tuneToSearch.Add(note);
        lastNotePressed = DateTime.Now;
        if (note is not null)
            note.Fire();
    }

    static MidiOut? midi;

    static MidiOut Midi => midi ??= new MidiOut(0);
    public async void PlayCMajorTriad(int durationMs = 300)
    {
        int channel = 1;
        int velocity = 100;

        // C major triad: C4, E4, G4
        int c = 60; // C4
        int e = 64; // E4
        int g = 67; // G4

        // Optional: choose instrument (piano)
        Midi.Send(MidiMessage.ChangePatch(0, channel).RawData);

        // Start notes
        Midi.Send(MidiMessage.StartNote(c, velocity, channel).RawData);
        Midi.Send(MidiMessage.StartNote(e, velocity, channel).RawData);
        Midi.Send(MidiMessage.StartNote(g, velocity, channel).RawData);

        await Task.Delay(durationMs);

        // Stop notes
        Midi.Send(MidiMessage.StopNote(c, 0, channel).RawData);
        Midi.Send(MidiMessage.StopNote(e, 0, channel).RawData);
        Midi.Send(MidiMessage.StopNote(g, 0, channel).RawData);
    }

    public void PlayNote(int midiNote, int durationMs = 500)
    {
        int channel = 1;
        int velocity = 100;
        // Optional: choose instrument (piano)
        Midi.Send(MidiMessage.ChangePatch(0, channel).RawData);
        // Start note
        Midi.Send(MidiMessage.StartNote(midiNote, velocity, channel).RawData);
        _ = Task.Delay(durationMs).ContinueWith(_ =>
        {
            // Stop note
            Midi.Send(MidiMessage.StopNote(midiNote, 0, channel).RawData);
        });
    }
}
