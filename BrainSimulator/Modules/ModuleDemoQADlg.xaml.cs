using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UKS;

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

        // Overload that prefers a structured query (from the LLM) but also works with plain NL.
        private string QueryNLP(string nlQuery, UksQuery? q = null)
        {
            if (MainWindow.theUKS == null)
                return "UKS is not initialized. Open the UKS module or load a knowledge base first.";


            // Map to the UKS static call args
            string src = "";
            string typ = "";
            string tgt = "";
            string? filt = null;

            // 1) Prefer structured fields if provided by the LLM
            if (q != null)
            {
                if (q.Entities is { Count: > 0 }) src = q.Entities[0] ?? "";
                if (q.Entities is { Count: > 1 }) tgt = q.Entities[1] ?? "";
                if (q.Predicates is { Count: > 0 }) typ = q.Predicates[0] ?? "";

                if (q.Constraints is { Count: > 0 })
                    filt = string.Join(";", q.Constraints.Select(kv => $"{kv.Key}={kv.Value}"));

                // If nothing useful was provided, fall back to NL parse
                if (string.IsNullOrWhiteSpace(src) && string.IsNullOrWhiteSpace(typ) && string.IsNullOrWhiteSpace(tgt))
                    FillFromNatural(nlQuery, ref src, ref typ, ref tgt, ref filt);
            }
            else
            {
                // 2) No structured query → parse simple NL patterns
                FillFromNatural(nlQuery, ref src, ref typ, ref tgt, ref filt);
            }

            // Normalize common types.
            typ = (typ ?? "").Trim().ToLowerInvariant() switch
            {
                "is a" or "isa" or "is_a" => "is-a",
                "type-of" or "typeof" => "type-of",
                "hasprop" or "has_prop" => "hasProp",
                _ => typ
            };


            // 3) Call the UKS query
            List<Thing> things;
            List<Relationship> rels;
            try
            {
                ModuleUKSQuery.QueryUKSStatic(MainWindow.theUKS, src, typ, tgt, filt, out things, out rels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("QueryUKSStatic error: " + ex);
                return "There was an error while querying the UKS.";
            }

            // 4) Render a friendly answer
            if (rels != null && rels.Count > 0)
            {
                var lines = rels.Take(20).Select(RelToString);
                var more = rels.Count > 20 ? $"\n(+{rels.Count - 20} more…)" : "";
                return "I found these grounded relationships:\n - " + string.Join("\n - ", lines) + more;
            }

            if (things != null && things.Count > 0)
            {
                var names = things.Take(20).Select(ThingToString);
                var more = things.Count > 20 ? $"\n(+{things.Count - 20} more…)" : "";
                return "I found these related things:\n - " + string.Join("\n - ", names) + more;
            }

            Debug.WriteLine($"NL Query: {nlQuery}");
            return "I don’t have a grounded fact for that yet.";
        }

        // ——— Helpers ———

        // Very light NL → (src, typ, tgt) patterns.
        private static void FillFromNatural(string nl, ref string src, ref string typ, ref string tgt, ref string? filt)
        {
            string s = (nl ?? "").Trim();

            // Pattern 1: "Is A a/an B?" or "Is A B?"
            var m1 = Regex.Match(s, @"^\s*is\s+(?<s>.+?)\s+(?:an?\s+)?(?<o>.+?)\s*\?\s*$", RegexOptions.IgnoreCase);
            if (m1.Success)
            {
                if (string.IsNullOrWhiteSpace(src)) src = m1.Groups["s"].Value;
                if (string.IsNullOrWhiteSpace(typ)) typ = "isA";
                if (string.IsNullOrWhiteSpace(tgt)) tgt = m1.Groups["o"].Value;
                return;
            }


            // Pattern 2: "Does/Do/Can S (P) O ?"  e.g., "Do penguins fly?"
            var m2 = Regex.Match(s, @"^\s*(?:does|do|can)\s+(?<s>.+?)\s+(?<p>\w+)(?:\s+(?<o>.+?))?\s*\?\s*$", RegexOptions.IgnoreCase);
            if (m2.Success)
            {
                if (string.IsNullOrWhiteSpace(src)) src = m2.Groups["s"].Value;
                if (string.IsNullOrWhiteSpace(typ)) typ = m2.Groups["p"].Value;
                if (m2.Groups["o"].Success && string.IsNullOrWhiteSpace(tgt)) tgt = m2.Groups["o"].Value;
                return;
            }

            // Pattern 3: "What is the P of S?" → typ=P, src=S
            var m3 = Regex.Match(s, @"^\s*what\s+is\s+(?:the\s+)?(?<p>\w+)\s+of\s+(?<s>.+?)\s*\?\s*$", RegexOptions.IgnoreCase);
            if (m3.Success)
            {
                if (string.IsNullOrWhiteSpace(src)) src = m3.Groups["s"].Value;
                if (string.IsNullOrWhiteSpace(typ)) typ = m3.Groups["p"].Value;
                return;
            }

            // Fallback: treat whole query as a free-form filter
            filt ??= s;
        }

        // Print a relationship even if we don’t know the exact property names.
        private static string RelToString(Relationship r)
        {
            try
            {
                dynamic d = r;
                // Try common field names used in your AddStatement signature
                string s = SafeStr(d.sSource) ?? SafeStr(d.Source) ?? "?";
                string p = SafeStr(d.sRelationshipType) ?? SafeStr(d.RelationshipType) ?? "?";
                string o = SafeStr(d.sTarget) ?? SafeStr(d.Target) ?? "?";
                return $"{s} -[{p}]-> {o}";
            }
            catch
            {
                return r?.ToString() ?? "(relationship)";
            }
        }

        private static string ThingToString(object t)
        {
            // Try to display a human-readable name for Thing using dynamic fallback
            try
            {
                dynamic d = t;
                return SafeStr(d.Name) ?? SafeStr(d.sName) ?? t.ToString();
            }
            catch
            {
                return t?.ToString() ?? "(thing)";
            }
        }

        private static string? SafeStr(object? x) => x == null ? null : x.ToString();

        private async Task<string> CallChatbotAsync(string userText)
        {
            // 1) Get a STRICT JSON plan from the LLM (no prose)
            string system = """
You are the QA Demo Bot operating over a Universal Knowledge Store (UKS).
Return STRICT JSON ONLY (no markdown, no commentary) with this schema:

{
  "Mode": "statement" | "question" | "instruction" | "multi",
  "Confidence": number,
  "Facts": [ {"S": "...", "P": "...", "O": "..."} ],
  "Query": {
    "Entities": [string],
    "Predicates": [string],
    "Constraints": {string:string},
    "Natural": "recommended natural-language query phrasing",
    "SparqlLike": "optional formal query if applicable"
  },
  "Notes": [string]
}

*** RULES FOR FACTS (VERY IMPORTANT) ***
- Emit facts ONLY if they are ATOMIC (a single relation between two nouns).
- NO prepositional phrases or conjunctions in S/P/O (reject: "with", "by", "and", "that", "which", commas).
- NO pronouns in O (reject: "it", "they", "their", "its", "them", "those", "these").
- PREDICATE must be from this whitelist ONLY (lowercase): 
  ["is-a","has","part-of","located-in","can","uses","made-of","causes","prevents"]
- If the user statement is complex or compositional, DO NOT emit any Facts. Use Notes to say "complex".
- If the user asks a question, leave Facts empty and fill Query.Natural.

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
                            // Fixed "is a" bug.
                            if (f.P == "is a")
                            {
                                f.P = "is-a";
                            }
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

                    var result = QueryNLP(nl, plan.Query);

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
