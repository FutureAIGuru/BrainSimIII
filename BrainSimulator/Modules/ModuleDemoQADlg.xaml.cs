using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator.Modules
{
    public partial class ModuleDemoQADlg : ModuleBaseDlg
    {
        private enum ProviderType { openai, ollama }
        private ProviderType _provider = ProviderType.openai;

        public ModuleDemoQADlg()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                providerCombo.SelectedIndex = 0; // fires after all named elements exist
            };
        }


        private void AppendMessage(string speaker, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            chatTranscript.AppendText($"{speaker}: {message.Trim()}\n");
            chatTranscript.CaretIndex = chatTranscript.Text.Length;
            chatTranscript.ScrollToEnd();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendCurrentInputAsync();
        }

        private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true; // prevent newline
                await SendCurrentInputAsync();
            }
        }

        private async Task SendCurrentInputAsync()
        {
            var userText = chatInput.Text;
            if (string.IsNullOrWhiteSpace(userText)) return;

            chatInput.IsEnabled = false;
            sendButton.IsEnabled = false;

            AppendMessage("You", userText);
            chatInput.Clear();

            try
            {
                // Replace this with your actual chatbot call
                var botReply = await CallChatbotAsync(userText);
                AppendMessage("Bot", botReply);
            }
            catch (Exception ex)
            {
                AppendMessage("System", $"Error: {ex.Message}");
            }
            finally
            {
                chatInput.IsEnabled = true;
                sendButton.IsEnabled = true;
                chatInput.Focus();
            }
        }

        /// <summary>
        /// Return a string reply to show in the transcript.
        /// TODO: Integrate with UKS, for now it just is pure LLMs.
        /// </summary>
        /*
        private async Task<string> CallChatbotAsync(string userText)
        {
            var reply = await GPT.RunTextAsync(userText, "You are the QA Demo Bot. ", _provider.ToString());
            return reply?.Trim() ?? "(no reply)";
        }
        */

        // Classes for helping chat and UKS integration.
        // TODO: Place in a seperate file for longer term ease of use.
        private sealed class ChatPlan
        {
            public string Mode { get; set; } = "question";  // "statement" | "question" | "instruction" | "multi"
            public double Confidence { get; set; } = 0.8;
            public List<Fact>? Facts { get; set; }
            public UksQuery? Query { get; set; }
            public List<string>? Notes { get; set; }
        }
        private sealed class Fact { public string S { get; set; } = ""; public string P { get; set; } = ""; public string O { get; set; } = ""; }
        private sealed class UksQuery
        {
            public List<string>? Entities { get; set; }
            public List<string>? Predicates { get; set; }
            public Dictionary<string, string>? Constraints { get; set; }
            public string? Natural { get; set; }      // normalized NL query (preferred)
            public string? SparqlLike { get; set; }   // optional if there should be support for a formal query
        }

        // Stub this out so the code compiles for now.
        private string QueryNLP(string nlQuery)
        {
            // TODO: replace with the real UKS query call (ModuleUKSQuery, etc.)
            System.Diagnostics.Debug.WriteLine("QueryNLP placeholder called with: " + nlQuery);

            return "(query engine not wired yet)";
        }

        private async Task<string> CallChatbotAsync(string userText)
        {
            // 1) Get a STRICT JSON plan from the LLM (no prose)
            string system = """
You are the QA Demo Bot operating over a Universal Knowledge Store (UKS).
Return STRICT JSON ONLY (no markdown, no commentary) with this schema:

{
  "Mode": "statement" | "question" | "instruction" | "multi",
  "Confidence": number, 
  "Facts": [ {"S": "...", "P": "...", "O": "..."} ],   // for statements
  "Query": {                                            // for questions
    "Entities": [string], 
    "Predicates": [string], 
    "Constraints": {string:string},
    "Natural": "recommended natural-language query phrasing",
    "SparqlLike": "optional formal query if applicable"
  },
  "Notes": [string]
}

Rules and exceptions are handled internally by UKS—DO NOT output rules; convert everything to facts (triples).
If the user asks a question, leave Facts empty and fill Query.Natural with a normalized question.
Return ONLY the JSON object.
""";

            string user = $"UserMessage: {userText}\nReturn ONLY the JSON object.";

            string raw = await GPT.RunTextAsync(user, system, _provider.ToString());
            if (string.IsNullOrWhiteSpace(raw))
                return "(no reply)";

            // 2) Parse JSON (strip ``` fences if provider adds them)
            raw = raw.Trim().ToLower();
            if (raw.StartsWith("```"))
                raw = TrimCodeFences(raw);

            ChatPlan plan;
            try
            {
                plan = System.Text.Json.JsonSerializer.Deserialize<ChatPlan>(
                    raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new ChatPlan();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChatPlan JSON parse error: " + ex);
                return "I couldn't parse the model's plan into structured statements/queries.";
            }

            // 3) Execute plan
            try
            {
                var mode = plan.Mode?.ToLowerInvariant() ?? "question";

                // ——— STATEMENTS: commit triples to UKS via AddStatement ———
                if (mode is "statement" or "instruction" ||
                   (mode == "multi" && (plan.Facts?.Count ?? 0) > 0))
                {
                    if (plan.Facts == null || plan.Facts.Count == 0)
                        return "I didn’t detect any concrete triples to add.";

                    var added = new List<string>();
                    var errors = new List<string>();

                    foreach (var f in plan.Facts)
                    {
                        if (string.IsNullOrWhiteSpace(f.S) || string.IsNullOrWhiteSpace(f.P) || string.IsNullOrWhiteSpace(f.O))
                        {
                            errors.Add($"Skipped incomplete triple: S='{f.S}', P='{f.P}', O='{f.O}'");
                            continue;
                        }
                        try
                        {
                            // The provided API: Relationship AddStatement(source, relationshipType, target, isStatement=true)
                            var rel = MainWindow.theUKS.AddStatement(f.S, f.P, f.O);
                            added.Add($"{f.S} -[{f.P}]-> {f.O}");
                        }
                        catch (Exception e)
                        {
                            errors.Add($"Failed {f.S}-[{f.P}]->{f.O}: {e.Message}");
                        }
                    }

                    var ack = added.Count > 0
                        ? $"Added {added.Count} statement(s) to the UKS:\n - " + string.Join("\n - ", added)
                        : "No statements were added.";

                    if (errors.Count > 0)
                        ack += "\n\nWarnings:\n - " + string.Join("\n - ", errors);

                    return ack;
                }

                // ——— QUESTIONS: route to the UKS query path ———
                // Prefer the normalized natural-language query produced by the model.
                if (mode is "question" or "multi")
                {
                    string nl = plan.Query?.Natural;
                    if (string.IsNullOrWhiteSpace(nl))
                    {
                        // fallback: use user text as the NL query if LLM didn’t normalize
                        nl = userText;
                    }

                    var result = QueryNLP(nl);

                    // If one can surface a proof/explanation from your query engine, append it here.
                    return string.IsNullOrWhiteSpace(result)
                        ? "I don’t have enough grounded knowledge to answer that yet."
                        : result.Trim();
                }

                return "I’m not sure if that was a statement to store or a question to answer. Please rephrase.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Chat plan execution error: " + ex);
                return "There was an error applying/querying the UKS.";
            }
        }

        // Helper to strip ```json fences if a provider returns them
        private static string TrimCodeFences(string s)
        {
            var lines = s.Split('\n').ToList();
            if (lines.Count >= 2 && lines[0].StartsWith("```"))
            {
                lines.RemoveAt(0);
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].TrimStart().StartsWith("```"))
                    {
                        lines.RemoveAt(i);
                        break;
                    }
                }
            }
            return string.Join("\n", lines);
        }


        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _provider = providerCombo.SelectedIndex == 1 ? ProviderType.ollama : ProviderType.openai;
            AppendMessage("System", $"Provider set to {_provider.ToString()}.");
        }
    }

}
