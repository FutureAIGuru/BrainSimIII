using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pluralize.NET;
using UKS;
using static BrainSimulator.Modules.ModuleOnlineInfo;

namespace BrainSimulator.Modules
{
    /// <summary>
    /// Interaction logic for ModuleDemoQADlg.xaml
    /// </summary>
    public partial class ModuleDemoQADlg : ModuleBaseDlg
    {
        public ModuleDemoQADlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            return base.Draw(checkDrawTimer);
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                // Make sure the content is the same as what's here, or it won't run!
                if (b.Content.ToString().StartsWith("A) Ingest Text -> UKS"))
                {
                    ProcessNLPAsync();

                }
                else if (b.Content.ToString().StartsWith("B) Ingest File -> UKS"))
                {
                    ProcessNLPAsyncFile();
                }
                else if (b.Content.ToString().StartsWith("Query UKS"))
                {
                    QueryNLP();
                }

            }

        }

        private void txtOutput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        /// <summary>
        /// Query the UKS using NLP.
        /// Currently NOT using LLMs, but will probablu expand into that at a later time.
        /// </summary>
        private void QueryNLP()
        {


            // 1) Get the raw NL query. Make sure the textbox has the name "queryTextBox"
            //    Otherwise, call this method after setting a local 'raw' string.

            string raw = (this.FindName("queryTextBox") as System.Windows.Controls.TextBox)?.Text ?? string.Empty;
            raw = raw.Trim();

            // Guard: nothing to do
            if (string.IsNullOrWhiteSpace(raw))
            {
                queryOutcomeLbl.Content = "No query text.";
                return;
            }

            // Normalization
            string q = raw.ToLowerInvariant();
            q = Regex.Replace(q, @"[?]", " ? "); // pad question marks
            q = Regex.Replace(q, @"\s+", " ").Trim();

            // Helpers to set UI fields & run
            void Run(string src, string typ, string tgt, string filt = "")
            {
                queryTextBox.Text = "";
                //queryTextBox.Text = src?.Trim() ?? "";
                //typeText.Text = typ?.Trim() ?? "";
                //targetText.Text = tgt?.Trim() ?? "";
                //filterText.Text = filt?.Trim() ?? "";
                List<Thing> things;
                List<Relationship> relationships;
                ModuleUKSQuery.QueryUKSStatic(MainWindow.theUKS, src, typ, tgt, filt, out things, out relationships);

                Debug.WriteLine(src + " : " + typ + " : " + tgt);

                /*
                foreach (Thing thing in things)
                {
                    Debug.WriteLine(thing.Label);
                }

                foreach (Relationship rel in relationships)
                {
                    Debug.WriteLine(rel.ToString());
                }
                */

                if (things.Count > 0)
                    queryOutcomeLbl.Content = "Query Status: " + OutputResults(things);
                else
                    queryOutcomeLbl.Content = "Query Status: Nothing Found.";
            }

            // Small extract helpers
            static string Title(string s) => string.IsNullOrWhiteSpace(s) ? s : char.ToUpper(s[0]) + s[1..];

            // Common regex fragments
            string thing = @"(?<thing>[a-z0-9\-\._ ]+?)";
            string thing2 = @"(?<thing2>[a-z0-9\-\._ ]+?)";
            string verbing = @"(?<verb>[a-z][a-z\- ]*?)";

            // 2) High-priority Q&A forms

            // "Is A a/an B?" -> A is-a B
            var m = Regex.Match(q, $@"^is {thing} an? {thing2}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "is-a", Title(m.Groups["thing2"].Value)); return; }

            // "What is X?" / "Who is X?" -> show ancestors (classes) of X: X is-a ?
            m = Regex.Match(q, $@"^(what|who) is {thing}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "is-a", ""); return; }

            // "What are the children/subclasses of X?" -> X has-child ?
            m = Regex.Match(q, $@"^what (are|is) (the )?(children|subclasses) of {thing}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "has-child", ""); return; }

            // "What does A have?" -> A has ?
            m = Regex.Match(q, $@"^what does {thing} have\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "has", ""); return; }

            // "Does A have B?" -> A has B
            m = Regex.Match(q, $@"^does {thing} have {thing2}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "has", Title(m.Groups["thing2"].Value)); return; }

            // "What does A own?" / "Who owns B?" -> owns
            m = Regex.Match(q, $@"^what does {thing} own\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "owns", ""); return; }
            m = Regex.Match(q, $@"^who owns {thing}\s*\?$");
            if (m.Success) { Run("", "owns", Title(m.Groups["thing"].Value)); return; }

            // "Where does A go?" / "Where is A?" -> goes
            m = Regex.Match(q, $@"^where does {thing} go\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "goes", ""); return; }
            m = Regex.Match(q, $@"^where is {thing}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "goes", ""); return; }

            // "What can A do?" -> A can ?
            m = Regex.Match(q, $@"^what can {thing} do\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "can", ""); return; }

            // "Can A <verb>?" -> A can <verb>
            m = Regex.Match(q, $@"^can {thing} {verbing}\s*\?$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "can", Title(m.Groups["verb"].Value)); return; }

            // 3) Declarative short forms (no '?')

            // "A is B" (or "A is-a B")
            m = Regex.Match(q, $@"^{thing} is(-a)? {thing2}$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "is-a", Title(m.Groups["thing2"].Value)); return; }

            // "A has B"
            m = Regex.Match(q, $@"^{thing} has {thing2}$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "has", Title(m.Groups["thing2"].Value)); return; }

            // "A owns B"
            m = Regex.Match(q, $@"^{thing} owns {thing2}$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "owns", Title(m.Groups["thing2"].Value)); return; }

            // "A goes B" (location)
            m = Regex.Match(q, $@"^{thing} goes to {thing2}$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "goes", Title(m.Groups["thing2"].Value)); return; }

            // "A can V"
            m = Regex.Match(q, $@"^{thing} can {verbing}$");
            if (m.Success) { Run(Title(m.Groups["thing"].Value), "can", Title(m.Groups["verb"].Value)); return; }

            // 4) Fallbacks

            // Single token: show all relationships for that Thing (leave type/target blank)
            if (!q.Contains(' '))
            {
                Run(Title(q), "", "");
                return;
            }

            // Try "source type target" naive split (3 tokens)
            var parts = q.Split(' ');
            if (parts.Length >= 3)
            {
                // e.g., "dog is-a mammal", "hand has fingers"
                // We'll map the *first* token as source, the *second* as type, the rest as target.
                var src = Title(parts[0]);
                var typ = parts[1];
                var tgt = Title(string.Join(' ', parts.Skip(2)));
                Run(src, typ, tgt);
                return;
            }

            // Last resort: treat as source-only search
            Run(Title(q), "", "");
        }

        /// <summary>
        /// Function that takes natural language sentences, from a textbox, and sends them to ModuleDemoQA to handle.
        /// </summary>
        private async void ProcessNLPAsync()
        {
            try
            {
                MainWindow.SuspendEngine();
                inputOutcomeLbl.Content = "Input Status: Processing input...";

                // 1) Read the textbox text as text
                string text;
                try
                {
                    text = inputTextBox.Text;
                }
                catch (IOException ioex)
                {
                    inputOutcomeLbl.Content = "Input Status: An error occurred while reading the file:\n" + ioex.Message;
                    return;
                }

                // 2) Split into sentences for counting purposes only for now. (simple heuristic; adjust if one has a better splitter)
                //    This splits on ., !, ? followed by whitespace. Keeps punctuation attached to the sentence.
                var sentences = System.Text.RegularExpressions.Regex
                    .Split(text, @"(?<=[\.!\?])\s+")
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                // Make sure there is at least one sentence.
                if (sentences.Count == 0)
                {
                    inputOutcomeLbl.Content = "Input Status: No sentences found in the file.";
                    return;
                }

                // Optional: cap the number of sentences if one wants a quick test cycle for a larger file.
                // int sentenceMax = 50;
                // if (sentences.Count > sentenceMax) sentences = sentences.Take(sentenceMax).ToList();

                inputOutcomeLbl.Content = $"Input Status: Read (an estimated) {sentences.Count} sentence(s). Sending to GPT→UKS module…";

                // 3) Hand off to the LLM→UKS module (adjust signature/types as needed)
                string report;
                try
                {
                    await ModuleDemoQA.NaturalToUKS(text);
                    //Debug.WriteLine("Report is " + report);
                }
                catch (Exception ex)
                {
                    // If the module throws, surface a friendly message
                    inputOutcomeLbl.Content = "Input Status: An error occurred in ModuleDemoQA:\n" + ex.Message;
                    return;
                }

                // 4) Show result/summary in the UI
                inputOutcomeLbl.Content = $"Input Status: Processed (an estimated) {sentences.Count} sentence(s) via GPT→UKS.";
            }
            finally
            {
                // Make sure the engine is resumed even if something fails
                try { MainWindow.ResumeEngine(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Function that takes natural language sentences, from a text file, and sends them to ModuleDemoQA to handle.
        /// Copied from ModuleGPTInfoDlg, TODO: Place both functions in a single location.
        /// </summary>
        private async void ProcessNLPAsyncFile()
        {
            try
            {
                MainWindow.SuspendEngine();
                inputOutcomeLbl.Content = "Input Status: Waiting for text file input...";

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
                    inputOutcomeLbl.Content = "Input Status: No file selected.";
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
                    inputOutcomeLbl.Content = "Input Status: An error occurred while reading the file:\n" + ioex.Message;
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
                    inputOutcomeLbl.Content = "Input Status: No sentences found in the file.";
                    return;
                }

                // Optional: cap the number of sentences if one wants a quick test cycle for a larger file.
                // int sentenceMax = 50;
                // if (sentences.Count > sentenceMax) sentences = sentences.Take(sentenceMax).ToList();

                inputOutcomeLbl.Content = $"Input Status: Read (an estimated) {sentences.Count} sentence(s). Sending to GPT→UKS module…";

                // 4) Hand off to the LLM→UKS module (adjust signature/types as needed)
                string report;
                try
                {
                    await ModuleDemoQA.NaturalToUKS(text);
                    //Debug.WriteLine("Report is " + report);
                }
                catch (Exception ex)
                {
                    // If the module throws, surface a friendly message
                    inputOutcomeLbl.Content = "Input Status: An error occurred in ModuleDemoQA:\n" + ex.Message;
                    return;
                }

                // 5) Show result/summary in the UI
                inputOutcomeLbl.Content = $"Input Status: Processed (an estimated) {sentences.Count} sentence(s) via GPT→UKS.";
            }
            finally
            {
                // Make sure the engine is resumed even if something fails
                try { MainWindow.ResumeEngine(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Outputs results from the Query.
        /// Copied directly from ModuleUKSQueryDlg with minor changes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="noSource"></param>
        /// <param name="noTarget"></param>
        private string OutputResults<T>(IList<T> r, bool noSource = false, bool noTarget = false)
        {
            string resultString = "";
            if (r == null || r.Count == 0)
                return "No Results Found.";
            else
                foreach (var r1 in r)
                {
                    if (r1 is Relationship r2)
                    {
                        /*
                        if (noSource && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                            resultString += $"{r2.relType?.ToString()} {r2.target.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                        else if (noTarget && r2.Clauses.Count == 0 && fullCB.IsChecked == false)
                            resultString += $"{r2.source.ToString()} {r2.relType.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                        else
                            resultString += $"{r2.source.ToString()} {r2.relType.ToString()} {r2.target.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                        
                        //                            resultString += r2.ToString() + "\n";
                                            */
                        if (noSource && r2.Clauses.Count == 0)
                            resultString += $"{r2.relType?.ToString()} {r2.target.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                        else if (noTarget && r2.Clauses.Count == 0)
                            resultString += $"{r2.source.ToString()} {r2.relType.ToString()}  ({r2.Weight.ToString("0.00")})\n";
                    }
                    else
                        resultString += r1.ToString() + "\n";
                }
            return resultString;
        }


    }


}
