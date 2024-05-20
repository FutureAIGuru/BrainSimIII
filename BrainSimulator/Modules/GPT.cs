﻿using Newtonsoft.Json;
using Pluralize.NET;
using System.Text.RegularExpressions;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainSimulator.Modules
{
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
            public Choice[] choices { get; set; }
            public Error error { get; set; }

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

        public static async Task<string> GetGPTResult(string userText, string systemText)
        {
            string apiKey = ConfigurationManager.AppSettings["APIKey"];
            var url = "https://api.openai.com/v1/chat/completions";
            var client = new HttpClient();
            string answerString = "";


            // Define the request body
            var requestBody = new
            {
                temperature = 0,
                max_tokens = 200,
                // OBSOLETE: IMPORTANT: Add your model here after fine tuning on OpenAI using word_only_dataset.jsonl.
                //model = "<YOUR_FINETUNED_MODEL_HERE>",
                model = "gpt-4o",// ConfigurationManager.AppSettings["FineTunedModel"],
                                 //model = "gpt-3.5-turbo-0125",// ConfigurationManager.AppSettings["FineTunedModel"],
                messages = new[] { new { role = "system", content = systemText }, new { role = "user", content = userText } },
            };

            // Serialize the request body to JSON
            var requestBodyJson = JsonConvert.SerializeObject(requestBody);

            // Create the request message
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            // Send the request and get the response
            var response = await client.SendAsync(request);

            // Deserialize the response body to a CompletionResult object
            var responseJson = await response.Content.ReadAsStringAsync();
            CompletionResult completionResult = JsonConvert.DeserializeObject<CompletionResult>(responseJson);
            if (completionResult.choices != null)
            {
                // Extract the generated text from the CompletionResult object
                answerString = completionResult.choices[0].message.content.Trim().ToLower();
            }
            else
                if (completionResult.error != null) answerString = "ERROR: " + completionResult.error.message;
            return answerString;
        }

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