/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
using Newtonsoft.Json;
using Pluralize.NET;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

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


            // Define the request body
            var requestBody = new
            {
                temperature = 0.2,
                max_tokens = 100,
                // NOT CURRENTLY USED: IMPORTANT: If you use GPT "Fine Tuning" put the fine-tuning model key here in the app.config file and change the line here:.
                //model = "<YOUR_FINETUNED_MODEL_HERE>",
                //model = ConfigurationManager.AppSettings["FineTunedModel"],
                model = "gpt-4o", //not fine-tuned
                //model = "gpt-3.5-turbo", //not fine-tuned
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
                if (completionResult.usage is not null)
                {
                    int tokensUsed = completionResult.usage.TotalTokens;
                    totalTokensUsed += tokensUsed;
                }
                if (completionResult.choices is not null)
                {
                    // Extract the generated text from the CompletionResult object
                    answerString = completionResult.choices[0].message.content.Trim().ToLower();
                }
                else
                    if (completionResult.error is not null) answerString = "ERROR: " + completionResult.error.message;
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

    }
}