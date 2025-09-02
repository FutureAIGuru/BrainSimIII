//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pluralize.NET;
using UKS;

namespace BrainSimulator.Modules
{

    public partial class ModuleGPTInfoDlg : ModuleBaseDlg
    {
        // Error count and relationship count, used for debugging (static for now, working on fix).
        public static int errorCount;
        public static int relationshipCount;

        // Word max set to 10 by default. *Modify/Increase Value at your own risk!*
        int wordMax = 100;
        List<string> words = new List<string>();  // List to hold all words

        // UKS call
        // Needed when accessing all items in the UKS.
        public static ModuleHandler moduleHandler = new();
        public static UKS.UKS theUKS = moduleHandler.theUKS;


        public ModuleGPTInfoDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            StatusLabel.Content = $"{GPT.totalTokensUsed} tokens used.  {mf.theUKS.Labeled("unknownObject")?.Children.Count} unknown Things.  ";
            return base.Draw(checkDrawTimer);
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SetOutputText(string theText)
        {
            txtOutput.Text = theText;
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            StatusLabel.Content = $"{GPT.totalTokensUsed} tokens used.  {mf.theUKS.Labeled("unknownObject")?.Children.Count} unknown Things.  ";
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset to 0 each time for error and relationship count
                errorCount = 0;
                relationshipCount = 0;
                MainWindow.SuspendEngine();
                words.Clear();
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Filter files by extension

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    try
                    {
                        var pluralizer = new Pluralizer();

                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Break if we are over word max (set to 50 by default) for testing.
                                if (words.Count >= wordMax)
                                {
                                    break;
                                }
                                // Split the line into words, removing empty entries
                                List<string> lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                // Singularize the words.
                                for (int i = 0; i < lineWords.Count; i++)
                                {
                                    if (lineWords[i].Trim() == "")
                                    {
                                        lineWords.RemoveAt(i);
                                        i--;
                                    }
                                    lineWords[i] = pluralizer.Singularize(lineWords[i]);
                                }
                                // Add the words to the list from the line
                                words.AddRange(lineWords);
                            }
                        }
                        txtOutput.Text = "File Successfully Read!";
                    }
                    catch (IOException error)
                    {
                        Debug.WriteLine("An error occurred while reading the file:");
                        SetOutputText("An error occurred while reading the file:\n" + error.Message);
                        Debug.WriteLine(error.Message);
                    }
                }
                else
                {
                    txtOutput.Text = "No file selected.";
                    Debug.WriteLine("No file selected.");
                }
            }
            await ProcessWordsAsync(words);
        }

        private async void LoadAmbiguity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset to 0 each time for error and relationship count
                errorCount = 0;
                relationshipCount = 0;
                MainWindow.SuspendEngine();
                words.Clear();
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Title = "Select a file";
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Filter files by extension

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    try
                    {
                        var pluralizer = new Pluralizer();

                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Break if we are over word max (set to 50 by default) for testing.
                                if (words.Count >= wordMax)
                                {
                                    break;
                                }
                                // Split the line into words, removing empty entries
                                List<string> lineWords = line.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                // Singularize the words.
                                for (int i = 0; i < lineWords.Count; i++)
                                {
                                    if (lineWords[i].Trim() == "")
                                    {
                                        lineWords.RemoveAt(i);
                                        i--;
                                    }
                                    lineWords[i] = pluralizer.Singularize(lineWords[i]);
                                }
                                // Add the words to the list from the line
                                words.AddRange(lineWords);
                            }
                        }
                        txtOutput.Text = "File Successfully Read!";
                    }
                    catch (IOException error)
                    {
                        Debug.WriteLine("An error occurred while reading the file:");
                        SetOutputText("An error occurred while reading the file:\n" + error.Message);
                        Debug.WriteLine(error.Message);
                    }
                }
                else
                {
                    txtOutput.Text = "No file selected.";
                    Debug.WriteLine("No file selected.");
                }
            }
            await ProcessAmbiguityAsync(words);

        }


        // Task to process the words.
        public async Task ProcessWordsAsync(List<string> words)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;

            SetOutputText("Running through words... Word count is: " + words.Count + ".");
            Debug.WriteLine("Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.GetChatGPTData(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.GetChatGPTData(word.Trim());
            }

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }

        // Task to process the words AND remove ambiguity.
        public async Task ProcessAmbiguityAsync(List<string> words)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;

            SetOutputText("Running through words... Word count is: " + words.Count + ".");
            Debug.WriteLine("Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.DisambiguateTermsFile(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.DisambiguateTermsFile(word.Trim());
            }

            txtOutput.Text = $"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.";
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }


        // Task to process the words.
        public async Task ProcessParentsAsync(List<string> words)
        {
            txtOutput.Text = "Getting Parents: Running through words... Word count is: " + words.Count + ".";
            Debug.WriteLine("Getting Parents: Running through words... Word count is: " + words.Count + ".");

            foreach (string word in words)
            {
                if (word == words.Last())
                    await ModuleGPTInfo.GetChatGPTParents(word.Trim());
                if (word.Trim() != "")
                    ModuleGPTInfo.GetChatGPTParents(word.Trim());
            }

            SetOutputText($"Done processing unknowns! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
            Debug.WriteLine($"Done running! Total word count: {words.Count}. Total relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }

        static int count = 0;
        public async Task verifyAllAsync()
        {
            count = 0;
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            SetOutputText("Verifying all is-a relationships");
            foreach (Thing t in mf.theUKS.UKSList)
            {
                if (t.Parents.FindFirst(x => x.Label == "unknownObject") != null) continue;
                if (!t.Label.StartsWith('.')) continue;
                if (t.Label == ".") continue;
                if (t == mf.theUKS.UKSList.Last())
                    await VerifyAsync(t.Label);
                else
                    VerifyAsync(t.Label);
            }
            SetOutputText($"Done verifying is-a relationships for reasonableness. Checked {count} relationships.");

        }
        public async Task VerifyAsync(string label)
        {
            ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
            if (!label.StartsWith(".")) label = "." + label;
            UKS.Thing t = mf.theUKS.Labeled(label);
            if (t == null) return;
            foreach (Relationship r in t.Relationships)
            {
                if (r.GPTVerified) continue;
                if (r.reltype.Label != "has-child") continue;

                count++;
                ModuleGPTInfo.GetChatGPTVerifyParentChild(r.target.Label, t.Label);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SetOutputText(txtOutput.Text = "");
        }

        private async void textInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetOutputText("Working...");
                string txt = textInput.Text;
                await ModuleGPTInfo.GetChatGPTData(txt);
                SetOutputText(ModuleGPTInfo.Output);
                txtOutput.Text += $"\n\rTotal relationship count: {relationshipCount}. Total error count (not accepted): {errorCount}.";
            }
            if (e.Key == Key.Up)
            {
                SetOutputText("Working...");
                string txt = textInput.Text;
                await ModuleGPTInfo.GetChatGPTParents(txt);
                SetOutputText(ModuleGPTInfo.Output);

            }
        }

        private async void GetGPTClausesAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.GetChatGPTClauses();
            SetOutputText($"\n\rTotal clause count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }

        private async void SolveAmbiguityGPTAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.DisambiguateTerms();
            SetOutputText($"\n\rTotal disambiguity success count: {relationshipCount}. Total error count (not accepted): {errorCount}.");
        }

        private async void SolveDuplicatesAsync()
        {
            SetOutputText("Working...");
            await ModuleGPTInfo.SolveDuplicates();
            SetOutputText($"\n\rDone! Duplicates resolved: {relationshipCount}.");
        }

        // Task to read NLP sentences and convert to UKS.

        /// <summary>
        /// Function that takes natural language sentences and sends them to ModuleGPTToUKS to handle.
        /// </summary>
        private async void ProcessNLPAsync()
        {
            try
            {
                MainWindow.SuspendEngine();
                txtOutput.Text = "Select a .txt file with natural-language sentences…";

                // 1) Let user choose a file
                using var openFileDialog = new System.Windows.Forms.OpenFileDialog
                {
                    Title = "Select a text file",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    txtOutput.Text = "No file selected.";
                    return;
                }

                string filePath = openFileDialog.FileName;

                // 2) Read entire file as text
                string text;
                try
                {
                    text = await File.ReadAllTextAsync(filePath);
                }
                catch (IOException ioex)
                {
                    SetOutputText("An error occurred while reading the file:\n" + ioex.Message);
                    return;
                }

                // 3) Split into sentences for counting purposes only for now. (simple heuristic; adjust if one has a better splitter)
                //    This splits on ., !, ? followed by whitespace. Keeps punctuation attached to the sentence.
                var sentences = System.Text.RegularExpressions.Regex
                    .Split(text, @"(?<=[\.!\?])\s+")
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (sentences.Count == 0)
                {
                    txtOutput.Text = "No sentences found in the file.";
                    return;
                }

                // Optional: cap the number of sentences if one wants a quick test cycle for a larger file.
                // int sentenceMax = 50;
                // if (sentences.Count > sentenceMax) sentences = sentences.Take(sentenceMax).ToList();

                txtOutput.Text = $"Read (an estimated count of) {sentences.Count} sentence(s). Sending to GPT→UKS module…";

                // 4) Hand off to the LLM→UKS module (adjust signature/types as needed)
                string report;
                try
                {
                    await ModuleGPTInfo.NaturalToUKS(text);
                    //Debug.WriteLine("Report is " + report);
                }
                catch (Exception ex)
                {
                    // If the module throws, surface a friendly message
                    SetOutputText("An error occurred in ModuleGPTInfo:\n" + ex.Message);
                    return;
                }

                // 5) Show result/summary in the UI
                txtOutput.Text = $"Processed (an estimated count of) {sentences.Count} sentence(s) via GPT→UKS.";
            }
            finally
            {
                // Make sure the engine is resumed even if something fails
                try { MainWindow.ResumeEngine(); } catch { /* ignore */ }
            }
        }





        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                ModuleGPTInfo mf = (ModuleGPTInfo)base.ParentModule;
                // Reset to 0 each time for error and relationship count
                errorCount = 0;
                relationshipCount = 0;
                if (b.Content.ToString().StartsWith("Re-Parse"))
                {
                    ModuleGPTInfo.ParseGPTOutput(textInput.Text, txtOutput.Text);
                }
                else if (b.Content.ToString().StartsWith("Verify All"))
                {
                    verifyAllAsync();
                }
                else if (b.Content.ToString().StartsWith("Add Clauses To All"))
                {
                    GetGPTClausesAsync();
                }
                else if (b.Content.ToString().StartsWith("Solve Ambiguity"))
                {
                    SolveAmbiguityGPTAsync();
                }
                else if (b.Content.ToString().StartsWith("Remove Duplicates"))
                {
                    SolveDuplicatesAsync();
                }
                else if (b.Content.ToString().StartsWith("NLP File"))
                {
                    ProcessNLPAsync();
                }
                else //process unknowns
                {
                    List<string> words = new List<string>();
                    var thingList = mf.theUKS.Labeled("unknownObject").Children;

                    SetOutputText($"Getting parents for {thingList.Count} Things");
                    foreach (var thing in thingList)
                    {
                        if (words.Count >= wordMax) break;
                        words.Add(thing.Label);
                    }

                    ProcessParentsAsync(words);
                }
            }
        }
    }
}