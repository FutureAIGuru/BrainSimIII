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
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace BrainSimulator.Modules
{
    public class ModuleOnlineFile : ModuleBase
    {
        public string Output = "";

        Pluralizer pluralizer = new Pluralizer();


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
                    //model = "<YOUR_FINETUNED_MODEL_HERE>",
                    model = "gpt-4o",// ConfigurationManager.AppSettings["FineTunedModel"],
                    //model = "gpt-3.5-turbo-0125",// ConfigurationManager.AppSettings["FineTunedModel"],
                    messages = new[] {
                        new { role = "system", content = "Provide answers that are common sense to a 5 year old. \n\r" +
                        "Answer in ordered pairs of the form value-name | value separated by line-breaks. \n\r" +
                        "Each Answer is formatted: value-name | value, value, value ... \n\r" +
                        "Example of answer with numerical value: contains (with counts) | 2 eyes, 4 legs, 1 tail \n\r"+
                        "If there is more than one value for a given value-name, these should be saparated by commas. \n\r"+
                        "Individual values should not be more than two words. \n\r"+
                        "If an item contains a compound verb such as 'can move', the complete verb should be be included in the value-name. For example: can make | coffee  or can be| happy. \n\r"+
                        "Use the following value-name: " +
                        "is-a, " +
                        "can, " +
                        "usually possesses "+
                        //"needs, " +
                        "has-properties), " +
                        "contains (with counts), " +
                        //"is-part-of, " +
                        //"is-bigger-than, " +
                        //"is-smaller-than, " +
                        //"is-similar-to, " +
                        "is-part-of-speech, " +
                        "represents-a-group-containing (up to 10)" },
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

                    textIn = ParseGPTOutput(textIn, Output);
                }
                else
                    if (completionResult.error != null) Output = completionResult.error.message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }

        string Singularize(string text)
        {
            List<string> ignoreWords = new() { "clothes","wales", };
            string retVal = text.ToLower().Trim();
            if (ignoreWords.Contains(text)) return retVal;
            retVal = pluralizer.Singularize(text);
            return retVal;
        }
        public string ParseGPTOutput(string textIn,string GPTOutput)
        {
            // Get the UKS
            GetUKS();
            // Split by comma (,) to get individual pairs
            string[] valuePairs = GPTOutput.Split(';', '\n');
            // Error check
            if (valuePairs.Length == 0)
            {
                Debug.WriteLine($"Error, length of value '{Output}' is 0.");
                ModuleOnlineFileDlg.errorCount += 1;
            }

            textIn = Singularize(textIn);

            foreach (string s in valuePairs)
            {
                string[] nameValuePair = s.Split("|");
                if (nameValuePair.Length == 2)
                {
                    string valueType = nameValuePair[0].Trim();
                    string valueString = nameValuePair[1].Trim();
                    List<string> values = valueString.Split(',').ToList();
                    for (int i = 0; i < values.Count; i++) values[i] = Singularize(values[i]);

                    for (int j = 0; j < values.Count; j++)
                    {
                        valueType = nameValuePair[0].Trim();
                        List<string> valueTypeProperties = new();
                        string value = values[j].Trim();
                        if (value.Contains("-")) continue; //gets rid of hyphenated numbers
                        string count = "";

                        //capture compound verbs based on "can"
                        List<string> subValues = value.Split(" ").ToList();
                        if (valueType == "can" && subValues.Count > 1)
                        {
                            valueTypeProperties.Add(subValues[0]);
                            subValues.RemoveAt(0);
                        }

                        //parse out multi-word values
                        for (int i = 0; i < subValues.Count; i++)
                        {
                            string subValue = subValues[i].Trim();
                            if (subValue == "a" || subValue == "an")
                            {
                                subValues.RemoveAt(i);
                                i--;
                            }
                            List<string> digitWords = new() { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
                            int index = digitWords.IndexOf(subValue);
                            if (index != -1)
                            {
                                count = index.ToString();
                                subValues.RemoveAt(i);
                                i--;
                            }
                            else if (int.TryParse(subValues[0], out int iCount))
                            {
                                count = iCount.ToString();
                                subValues.RemoveAt(i);
                                i--;
                            }
                            if (new List<string> { "some", "many", "lots", "few" }.Contains(subValue))
                            {
                                count = subValue;
                                subValues.RemoveAt(i);
                                i--;
                            }
                        }

                        //write the valueType|value  to the UKS
                        if (subValues.Count > 0)
                            value = string.Join(" ",subValues);
                        else
                            continue;
                        if (value == textIn) continue;
                        if (valueType.StartsWith("represents-a-group-containing"))
                        {
                            theUKS.AddStatement(value, "is-a", textIn); //note reversal
                        }
                        else if (valueType.StartsWith("usually possesses"))
                        {
                            theUKS.AddStatement(textIn, "has", value,null,"often");
                        }
                        else if (valueType.StartsWith("is-a-part-of-speech"))
                        {
                            theUKS.AddStatement(textIn, "is-a", value);
                        }
                        else if (valueType.StartsWith("has-properties"))
                        {
                            theUKS.AddStatement(textIn, "is", value);
                        }
                        else if (valueType.StartsWith("contains"))
                        {
                            if (count == "" || count == "1")
                                theUKS.AddStatement(textIn, "has", value);
                            else
                                theUKS.AddStatement(textIn, "has", value, null, count);
                        }
                        else
                        {
                            theUKS.AddStatement(textIn, valueType, value, null, valueTypeProperties);
                        }
                    }
                }
            }

            return textIn;
        }
    }
}