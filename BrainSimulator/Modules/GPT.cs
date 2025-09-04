using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pluralize.NET;

namespace BrainSimulator.Modules
{
    /// <summary>
    /// encapsulate access to the GPT API
    /// </summary>
    public static class GPT
    {
        static Pluralizer pluralizer = new Pluralizer();

        class CompletionResult
        {
            public string text { get; set; }
            public string finish_reason { get; set; }
            public string model { get; set; }
            public string prompt { get; set; }
            public string created { get; set; }
            public string id { get; set; }
            public CompletionUsage usage { get; set; }
            public Choice[] choices { get; set; }
            public Error error { get; set; }

            public class CompletionUsage
            {
                [JsonProperty("completion_tokens")]
                public int CompletionTokens { get; set; }

                [JsonProperty("prompt_tokens")]
                public int PromptTokens { get; set; }

                [JsonProperty("total_tokens")]
                public int TotalTokens { get; set; }
            }

            public class Choice
            {
                public string text { get; set; }
                public float? score { get; set; }
                public Message message { get; set; }
            }
            public class Message
            {
                public string role { get; set; }
                public string content { get; set; }
            }
            public class Error
            {
                public string message { get; set; }
            }
        }

        public static int totalTokensUsed = 0;
        public static async Task<string> GetGPTResult(string userText, string systemText)
        {
            string apiKey = ConfigurationManager.AppSettings["APIKey"];
            if (!apiKey.StartsWith("sk")) return "";
            var url = "https://api.openai.com/v1/chat/completions";
            var client = new HttpClient();
            string answerString = "";

            // Current model being used (as of 9/2/2025) is gpt-4.1-nano-2025-04-14
            string model_name = ConfigurationManager.AppSettings["OPENAI_MODEL"];
            if (string.IsNullOrWhiteSpace(model_name)) model_name = "gpt-4o-mini";

            // Define the request body
            var requestBody = new
            {
                temperature = 0.2,
                max_tokens = 100,
                // NOT CURRENTLY USED: IMPORTANT: If you use GPT "Fine Tuning" put the fine-tuning model key here in the app.config file and change the line here:.
                //model = "<YOUR_FINETUNED_MODEL_HERE>",
                //model = ConfigurationManager.AppSettings["FineTunedModel"],
                //model = "gpt-4o", //not fine-tuned
                //model = "gpt-3.5-turbo", //not fine-tuned
                model = model_name,
                messages = new[] { new { role = "system", content = systemText }, new { role = "user", content = userText } },
            };

            // Serialize the request body to JSON
            var requestBodyJson = JsonConvert.SerializeObject(requestBody);

            // Create the request message
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            try
            {            // Send the request and get the response
                var response = await client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();
                CompletionResult completionResult = JsonConvert.DeserializeObject<CompletionResult>(responseJson);
                if (completionResult.usage != null)
                {
                    int tokensUsed = completionResult.usage.TotalTokens;
                    totalTokensUsed += tokensUsed;
                }
                if (completionResult.choices != null)
                {
                    // Extract the generated text from the CompletionResult object
                    answerString = completionResult.choices[0].message.content.Trim().ToLower();
                }
                else
                    if (completionResult.error != null) answerString = "ERROR: " + completionResult.error.message;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // Deserialize the response body to a CompletionResult object
            return answerString;
        }

        //encapsulates a place where you can call the pluralizer and handle known special cases
        public static string Singularize(string text)
        {
            List<string> ignoreWords = new() { "clothes", "wales", };
            string retVal = text.ToLower().Trim();
            if (ignoreWords.Contains(text)) return retVal;
            retVal = pluralizer.Singularize(text);
            return retVal;
        }

        // ===== Provider selection helpers =====
        private static readonly HttpClient _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(120) };

        private static string Get(string key, string? fallback = null)
        {
            // Prefer App.config; fall back to env var; then explicit fallback.
            var v = System.Configuration.ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrWhiteSpace(v)) return v!;
            v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(v) ? (fallback ?? "") : v!;
        }

        private static string ProviderName()
        {
            var p = Get("LLM_PROVIDER", "openai").Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(p) ? "openai" : p;
        }

        // ===== Public entrypoints you can call from elsewhere =====
        public static async Task<string> RunTextAsync(string prompt, string system, bool asChat = false)
        {
            switch (ProviderName())
            {
                case "ollama":
                    if (asChat)
                        return await OllamaChatAsync(new[] { Msg("user", prompt) });
                    return await OllamaGenerateAsync(prompt, system);

                // keep existing OpenAI path intact
                case "openai":
                default:
                    // If one wishes to call OpenAI, route to it:
                    return await GetGPTResult(prompt, system);
            }
        }

        // Optional helper to pass multi-turn messages to the chat endpoint
        public static (string role, string content) Msg(string role, string content) => (role, content);

        // ===== OLLAMA IMPLEMENTATION =====
        private static async Task<string> OllamaGenerateAsync(string prompt, string system)
        {
            var baseUrl = Get("OLLAMA_BASE_URL", "http://localhost:11434").TrimEnd('/');
            var model = Get("OLLAMA_MODEL", "llama3:8b");

            string combined = system + " " + prompt;

            var payload = new JObject
            {
                ["model"] = model,
                ["prompt"] = combined,
                ["stream"] = false
            };

            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/generate"))
                {
                    req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                    using (var res = await _http.SendAsync(req))
                    {
                        res.EnsureSuccessStatusCode();

                        var json = await res.Content.ReadAsStringAsync();
                        var obj = JObject.Parse(json);

                        string? response = obj.Value<string>("response")
                                         ?? obj.SelectToken("message.content")?.ToString();

                        if (string.IsNullOrWhiteSpace(response))
                        {
                            Debug.WriteLine("Ollama: response field missing or empty.");
                            return "";
                        }

                        Debug.WriteLine("Ollama return: " + response);
                        return response;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[Ollama] HTTP error: {ex.Message}");
                return $"[Error contacting Ollama at {baseUrl}: {ex.Message}]";
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"[Ollama] Timeout: {ex.Message}");
                return "[Error: Ollama request timed out]";
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[Ollama] JSON parse error: {ex.Message}");
                return "[Error: Invalid JSON from Ollama]";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Ollama] Unexpected error: {ex}");
                return $"[Unexpected Ollama error: {ex.Message}]";
            }
        }


        private static async Task<string> OllamaChatAsync(IEnumerable<(string role, string content)> messages)
        {
            var baseUrl = Get("OLLAMA_BASE_URL", "http://localhost:11434").TrimEnd('/');
            var model = Get("OLLAMA_MODEL", "llama3:8b");

            var jMsgs = new JArray();
            foreach (var (role, content) in messages)
            {
                jMsgs.Add(new JObject
                {
                    ["role"] = role,
                    ["content"] = content
                });
            }

            var payload = new JObject
            {
                ["model"] = model,
                ["messages"] = jMsgs,
                ["stream"] = false
            };

            using (var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/chat"))
            {
                req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                using (var res = await _http.SendAsync(req))
                {
                    res.EnsureSuccessStatusCode();
                    var json = await res.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    // Try chat shape first, then fallback
                    return obj.SelectToken("message.content")?.ToString()
                           ?? obj.Value<string>("response")
                           ?? "";
                }
            }
        }
    }
}