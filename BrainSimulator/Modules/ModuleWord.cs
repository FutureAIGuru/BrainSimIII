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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleWord : ModuleBase
{
    public ModuleWord()
    {
        Label = "Word";
    }

    public override void Fire()
    {
        // Called periodically by the module engine
    }
    public override void Initialize()
    {
    }

    public override void SetUpAfterLoad()
    {
        base.SetUpAfterLoad();
    }

    public override void ShowDialog()
    {
        if (dlg == null)
        {
            dlg = new ModuleWordDlg();
            dlg.Owner = MainWindow.theWindow;
        }
        base.ShowDialog();
    }

    public string GetWordSuggestion(string word)
    {
        List<Thought> letters = new List<Thought>();
        foreach (char c in word.ToUpper())
        {
            string letterLabel = c.ToString();
            Thought letter = theUKS.GetOrAddThought(letterLabel, "letter");
            letters.Add(letter);
        }
        string retVal = word;
        var suggestions = theUKS.HasSequence(letters,"spelled",true,true);
        if (suggestions.Count > 0)
            retVal = suggestions[0].r.From?.Label;
        return retVal;
    }

    public static Thought AddWordSpelling(string word)
    {
        var theUKS = MainWindow.theUKS;
        if (string.IsNullOrWhiteSpace(word)) return null;

        word = word.Trim();
        theUKS.GetOrAddThought("Word", "Thought");
        theUKS.GetOrAddThought("letter", "Object");

        // Get or create the word thought
        Thought wordThought = theUKS.GetOrAddThought(word, "Word");

        // Create list of letter cognemes
        List<Thought> letters = new List<Thought>();
        foreach (char c in word.ToUpper())
        {
            string letterLabel = c.ToString();
            Thought letter = theUKS.GetOrAddThought(letterLabel, "letter");
            letters.Add(letter);
        }

        // Get or create the "spelled" Linktype
        Thought spelledLinkType = theUKS.GetOrAddThought("spelled", "LinkType");

        // Add the sequence
        theUKS.AddSequence(wordThought, spelledLinkType, letters);

        return wordThought;
    }

    public int LoadWordsFromFile(string filePath)
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
                    AddWordSpelling(word);
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
}