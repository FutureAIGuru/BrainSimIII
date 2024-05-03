//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
using Pluralize.NET;
using System.Windows.Xps;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Configuration;
using System.Windows.Documents;
using System.Threading.Tasks;

namespace BrainSimulator.Modules
{
    public class ModuleOnlineFile : ModuleBase
    {
        public string Output = "";

        public ModuleOnlineFile()
        {

        }

        public override void Initialize()
        {

        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

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

        public enum QueryType { general, isa, hasa, can, count, list, listCount, types, partsOf };
        public async Task GetChatGPTDataFine(string textIn, QueryType qtIn = QueryType.isa, string altLabel = "")
        {
            try
            {
                bool isError = true;
                QueryType qType = qtIn;
                if (altLabel == "") altLabel = textIn;
                string prompt;
                string apiKey = ConfigurationManager.AppSettings["APIKey"];
                var client = new HttpClient();
                var url = "https://api.openai.com/v1/chat/completions";
                string queryText = textIn;
                textIn = textIn.ToLower();


                queryText = $"Provide commonsense facts about the following: {textIn}";

                // Define the request body
                var requestBody = new
                {
                    temperature = 0,
                    max_tokens = 200,
                    // IMPORTANT: Add your model here after fine tuning on OpenAI using word_only_dataset.jsonl.
                    model = ConfigurationManager.AppSettings["FineTunedModel_File"],
                    messages = new[] {
                        new { role = "system", content = "Provide answers that are common sense to a 10 year old." },
                        new { role = "user", content = queryText}
                    },
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
                    Output = completionResult.choices[0].message.content.Trim().ToLower();
                    Debug.WriteLine(">>>" + queryText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    // Split by comma (,) to get individual pairs
                    string[] values = Output.Split(",");
                    // Error check
                    if (values.Length == 0)
                    {
                        Debug.WriteLine($"Error, length of value '{Output}' is 0.");
                        ModuleOnlineFileDlg.errorCount += 1;
                    }
                    string[] relationships = new string[Output.Length];
                    string[] targets = new string[Output.Length];
                    // Split by pipe (|) to get relationships and targets
                    for (int i = 0; i < values.Length; i++)
                    {
                        string[] parts = values[i].Split("|");
                        // Make sure the length of each pair is 2.
                        if (parts.Length == 2)
                        {
                            relationships[i] = parts[0];
                            targets[i] = parts[1];
                        }
                        else
                        {
                            Debug.WriteLine($"Error, unexpected format in values[i]: {values[i]}");
                            ModuleOnlineFileDlg.errorCount += 1;
                        }
                        
                    }
                    // Get the UKS
                    GetUKS();
                    
                    // Add the statements to the UKS
                    for (int i = 0; i < relationships.Length; i++)
                    {
                        UKS.AddStatement(textIn, relationships[i], targets[i]);
                    }
                }
                else
                    if (completionResult.error != null) Output = completionResult.error.message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }
    }
}