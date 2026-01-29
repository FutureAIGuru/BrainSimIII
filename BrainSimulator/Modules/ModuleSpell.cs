using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleSpell : ModuleBase
{
    public ModuleSpell()
    {
        Label = "Spell";
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
            dlg = new ModuleSpellDlg();
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
            Thought letter = theUKS.GetOrAddThing(letterLabel, "letter");
            letters.Add(letter);
        }
        string retVal = word;
        var suggestions = theUKS.HasSequence(letters,"spelled",true,true);
        if (suggestions.Count > 0)
            retVal = suggestions[0].r.From?.Label;
        return retVal;
    }

    public void AddWordSpelling(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;

        word = word.Trim();
        theUKS.GetOrAddThing("Word", "Thought");
        theUKS.GetOrAddThing("letter", "Object");

        // Get or create the word thing
        Thought wordThing = theUKS.GetOrAddThing(word, "Word");

        // Create list of letter cognemes
        List<Thought> letters = new List<Thought>();
        foreach (char c in word.ToUpper())
        {
            string letterLabel = c.ToString();
            Thought letter = theUKS.GetOrAddThing(letterLabel, "letter");
            letters.Add(letter);
        }

        // Get or create the "spelled" relationship type
        Thought spelledRelType = theUKS.GetOrAddThing("spelled", "LinkType");

        // Add the sequence
        theUKS.AddSequence(wordThing, spelledRelType, letters);
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