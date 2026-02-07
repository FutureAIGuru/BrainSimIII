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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleText : ModuleBase
{

    // Fill this method in with code which will execute
    // once for each cycle of the engine
    public override void Fire()
    {
        Init();

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

    }

    public static string AddPhrase(string phrase)
    {

        var theUKS = MainWindow.theUKS;
        char[] trimChars = { '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}' };

        int attempted = 0;
        int ingested = 0;
        try
        {
            List<Thought> wordsInPhrase = new();
            foreach (string token in phrase.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string clean = token.Trim(trimChars).ToLowerInvariant();
                if (string.IsNullOrEmpty(clean)) continue;

                attempted++;
                var wordThought = ModuleWord.AddWordSpelling(clean);
                wordsInPhrase.Add(wordThought);
                ingested++;
            }

            theUKS.GetOrAddThought("Phrase");
            theUKS.GetOrAddThought("hasWords", "LinkType");
            Thought thePhrase = theUKS.GetOrAddThought("p*", "Phrase");
            theUKS.AddSequence(thePhrase, "hasWords", wordsInPhrase);

            //create bigrams
            theUKS.GetOrAddThought("bigram");
            theUKS.GetOrAddThought("followedBy", "LinkType");
            for (int i = 0; i < wordsInPhrase.Count - 1; i++)
            {
                var bigram = wordsInPhrase[i].LinksTo.FindFirst(x => x.LinkType == "followedBy" && x.To == wordsInPhrase[i + 1]);
                if (bigram is null)
                {  //does not exist, create a new pair.
                    {
                        bigram = theUKS.AddStatement(wordsInPhrase[i], "followedBy", wordsInPhrase[i + 1]);
                        bigram.Weight = .1f;
                        theUKS.AddStatement(bigram, "is-a", "bigram");
                    }
                    int MAX_FOLLOWEDBY = 10;
                    if (wordsInPhrase[i].LinksTo.Count > MAX_FOLLOWEDBY)
                    {
                        var outgoing = wordsInPhrase[1].LinksTo
                            .Where(l => l.LinkType == "followedBy")
                            .OrderByDescending(l => l.Weight /* **Recency(l.LastFiredTime*/)
                            .ToList();

                        if (outgoing.Count > MAX_FOLLOWEDBY)
                        {
                            var losers = outgoing.Skip(MAX_FOLLOWEDBY);
                            foreach (var l in losers)
                                l.From.RemoveLink(l);   // or hard-decay
                        }
                    }
                }
                else
                {
                    bigram.LastFiredTime = DateTime.Now;
                    bigram.Weight = MathF.Min(1f, bigram.Weight + 0.05f * (1f - bigram.Weight));
                }
            }

            return $"Processed {attempted} tokens; ingested {ingested} words.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }

    }

    public static string AddText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Null input";

        string[] sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");
        foreach (string sentence in sentences)
        {
            string trimmed = sentence.Trim();
            if (trimmed.Length == 0) continue;

            AddPhrase(trimmed);
        }
        return "OK";
    }

    public int LoadTextFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;

        int count = 0;
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            //Parallel.ForEach (lines, line=>
            {
                string word = line.Trim();
                var splits = word.Split("\t");
                word = splits[0];
                if (!string.IsNullOrWhiteSpace(word))
                {
                    AddText(word);
                    count++;
                }
                //    });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading words from file: {ex.Message}");
        }

        return count;
    }

    public static int CreateTrigrams()
    {
        int retVal = 0;
        var theUKS = MainWindow.theUKS;
        // Implementation for creating trigrams
        theUKS.GetOrAddThought("trigram");
        foreach (Thought t in ((Thought)"bigram").Children)
        {
            foreach (Thought l in t.To.LinksTo.Where(x=>x.LinkType.Label == "followedBy"))
            {
                if (l.LinkType.Label != "followedBy") continue;
                float wSum = t.Weight + l.Weight;
            }
         
        }
        return retVal;
    }

}