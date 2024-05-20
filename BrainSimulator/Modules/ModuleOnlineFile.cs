//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using UKS;
using System.Security.Policy;

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


        public async Task GetChatGPTDataFine(string textIn)
        {
            try
            {
                bool isError = true;
                string prompt;
                textIn = textIn.ToLower();

                string answerString = "";
                string userText = $"Provide commonsense facts about the following: {textIn}";
                string systemText = "Provide answers that are common sense to a 10 year old. \n\r" +
                                                 "Answer in ordered pairs of the form value-name | value separated by line-breaks. \n\r" +
                                                 "Each Answer is formatted: value-name | value, value, value  \n\r" +
                                                 "Example of answer with numerical value: contains (with counts) | 2 eyes, 4 legs, 1 tail \n\r" +
                                                 "If there is more than one value for a given value-name, these should be saparated by commas. \n\r" +
                                                 "Individual values should not be more than two words. \n\r" +
                                                 "Use the following value-name: " +
                                                 "is-a, " +
                                                 "can, " +
                                                 "usually possesses " +
                                                 //"needs, " +
                                                 "has-properties), " +
                                                 "contains (with counts), " +
                                                 //"is-part-of, " +
                                                 //"is-bigger-than, " +
                                                 //"is-smaller-than, " +
                                                 //"is-similar-to, " +
                                                 "is-part-of-speech, ";

                //                                         "then list up to 10 'values' for which it would be reasonable to say 'a ''value'' is-a "+textIn  

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    ParseGPTOutput(textIn, Output);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }
        }

        public void ParseGPTOutput(string textIn, string GPTOutput)
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

            textIn = GPT.Singularize(textIn);

            foreach (string s in valuePairs)
            {
                string[] nameValuePair = s.Split("|");
                if (nameValuePair.Length == 2)
                {
                    string valueType = nameValuePair[0].Trim();
                    string valueString = nameValuePair[1].Trim();
                    List<string> values = valueString.Split(',').ToList();
                    for (int i = 0; i < values.Count; i++) values[i] = GPT.Singularize(values[i]);

                    for (int j = 0; j < values.Count; j++)
                    {
                        valueType = nameValuePair[0].Trim();
                        List<string> valueTypeAttributes = new();
                        List<string> valueProperties = new();
                        string value = values[j].Trim();
                        if (value.Contains("-")) continue; //gets rid of hyphenated numbers
                        string count = "";

                        //capture compound verbs based on "can"
                        List<string> subValues = value.Split(" ").ToList();
                        if (valueType == "can" && subValues.Count > 1)
                        {
                            valueTypeAttributes.Add(subValues[0]);
                            subValues.RemoveAt(0);
                        }

                        //parse out multi-word values
                        for (int i = 0; i < subValues.Count; i++)
                        {
                            string subValue = subValues[i].Trim();

                            //ignore extraneous articles
                            if (subValue == "a" || subValue == "an")
                            {
                                subValues.RemoveAt(i);
                                i--;
                            }

                            //trap numbers
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
                            //turn all extra values into properties
                            while (subValues.Count > 1)
                            {
                                valueProperties.Add(subValues[0]);
                                subValues.RemoveAt(0);
                            }
                        }

                        //write the valueType|value  to the UKS
                        if (subValues.Count > 0)
                            value = string.Join(" ", subValues);
                        else
                            continue;
                        if (value == textIn) continue;
                        if (valueType.StartsWith("names a category containing"))
                        {
                            theUKS.AddStatement("." + value, "is-a", "." + textIn); //note reversal
                        }
                        else if (valueType.StartsWith("usually possesses"))
                        {
                            theUKS.AddStatement("." + textIn, "has", "." + value, null, "often", valueProperties);
                        }
                        else if (valueType.StartsWith("is-a-part-of-speech"))
                        {
                            theUKS.AddStatement("." + textIn, "is-a", "." + value);
                        }
                        else if (valueType.StartsWith("has-properties"))
                        {
                            theUKS.AddStatement("." + textIn, "is", "." + value, null, null, valueProperties);
                        }
                        else if (valueType.StartsWith("contains"))
                        {
                            if (count == "" || count == "1")
                                theUKS.AddStatement("." + textIn, "has", "." + value, null, null, valueProperties);
                            else
                                theUKS.AddStatement("." + textIn, "has", "." + value, null, count, valueProperties);
                        }
                        else
                        {
                            theUKS.AddStatement("." + textIn, valueType, "." + value, null, valueTypeAttributes, valueProperties);
                        }
                        ///////   null reltypes?
                        ///
                        foreach (Thing t in theUKS.UKSList)
                            foreach (Relationship r in t.Relationships)
                                if (r.reltype == null)
                                {
                                    t.RemoveRelationship(r);
                                }

                    }
                }
            }
        }
    }
}