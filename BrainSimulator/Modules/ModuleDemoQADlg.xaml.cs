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
                if (b.Content.ToString().StartsWith("B) Ingest File -> UKS"))
                {
                    ProcessNLPAsyncFile();
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

                if (sentences.Count == 0)
                {
                    inputOutcomeLbl.Content = "Input Status: No sentences found in the file.";
                    return;
                }

                // Optional: cap the number of sentences if one wants a quick test cycle for a larger file.
                // int sentenceMax = 50;
                // if (sentences.Count > sentenceMax) sentences = sentences.Take(sentenceMax).ToList();

                inputOutcomeLbl.Content = $"Input Status: Read (an estimated count of) {sentences.Count} sentence(s). Sending to GPT→UKS module…";

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
                inputOutcomeLbl.Content = $"Input Status: Processed (an estimated count of) {sentences.Count} sentence(s) via GPT→UKS.";
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

    }


}
