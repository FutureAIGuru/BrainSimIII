//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleGPTInfo : ModuleBase
    {
        public static string Output = "";


        public ModuleGPTInfo()
        {

        }

        public override void Initialize()
        {

        }

        public override void SetUpAfterLoad()
        {
            string apiKey = ConfigurationManager.AppSettings["APIKey"];
            if (!apiKey.StartsWith("sk"))
            {
                MessageBox.Show(@"OpenAI GPT API Key was not found in the app.config file. GPT Info requests will be ignored. To set up: after getting an API key from OpenAI, put it in the app.config file in the form:
    <appSettings>
        <add key=""APIKey"" value=""<YOUR_API_KEY_HERE>"" />
    </appSettings>
", "API Key Not found", MessageBoxButton.OK);
            }
        }

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        //these are static so they can be called from the UKS dialog context menu
        //This can verify parent-child releationships.
        public static async Task GetChatGPTVerifyParentChild(string child,string parent)
        {
            //these turns dotted names back into more english-language strings
            string child1 = GetStringFromThingLabel(child);
            string parent1 = GetStringFromThingLabel(parent);

            try
            {

                string answerString = "";
                string systemText = $"Provide commonsense facts about the following: ";
                string userText = $"Is the following true: a(n) {child1} is a(n) {parent1}? (yes or no, no explanation)";

                answerString = await GPT.GetGPTResult(userText, systemText);
                if (!answerString.StartsWith("ERROR") && answerString != "")
                {
                    Output = answerString;
                    Debug.WriteLine(">>>" + userText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;
                    if (!answerString.Contains("yes"))
                    {
                        Thing tParent = MainWindow.theUKS.Labeled(parent);
                        if (tParent == null) tParent = MainWindow.theUKS.Labeled("." + parent);
                        if (tParent == null) return;
                        Thing tChild = MainWindow.theUKS.Labeled(child);
                        if (tChild == null) tChild = MainWindow.theUKS.Labeled("." + child);
                        if (tChild == null) return;
                        tChild.RemoveParent(tParent);
                        if (tChild.Parents.Count == 0)
                            tChild.AddParent(MainWindow.theUKS.Labeled("unknownObject"));
                        Debug.WriteLine($"Relationship: {child} is-a {parent} has been removed. ");
                    }
                    else
                    {
                        //tag item as verified so we don't try it again
                        Relationship r = MainWindow.theUKS.GetRelationship(parent, "has-child", child);
                        if (r != null)
                        {
                            r.GPTVerified = true;
                            Debug.WriteLine($"Relationship: {child} is-a {parent} has verified. ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting data from GPT. Error is {ex}.");
            }

        }

        //this turns dotted names back into more english-language strings
        private static string GetStringFromThingLabel(string thingLabel)
        {
            string theString = thingLabel.ToLower();
            if (theString[0] == '.') theString = theString.Substring(1);
            string[] s = theString.Split('.');
            theString = "";
            for (int i = 1; i < s.Length; i++)
                theString += ((theString.Length == 0) ? "" : " ") + s[i];
            theString += ((theString.Length == 0) ? "" : " ") + s[0];
            return theString;
        }

        //this is used to add parents to unknownObjects
        public static async Task GetChatGPTParents(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                if (textIn.StartsWith("."))
                    textIn = textIn.Substring(1);

                string answerString = "";
                string userText = $"Provide commonsense clasification answer the request which is appropriate for a 5 year old: What is-a {textIn}";
                string systemText = 
$@"This is a classification request. Examples: horse is-a | animal, mammal \n\r chimpanzee is-a | primate, mammal
Answer is formatted: is-a | VALUE, VALUE, VALUE with no more than 3 values and VAIUES are 1 or 2 words.
Answer should ONLY contain VALUEs where it is reasonable to say: '{textIn} is-a VALUE' and exclude: 'VALUE is-a {textIn}' 
Never include {textIn} in the result.";

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

        //this is a general factual information request from GPT
        public static async Task GetChatGPTData(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                textIn = textIn.Replace(".", "");

                string answerString = "";
                string userText = $"Provide commonsense facts to answer the request: what is a {textIn}";
                string systemText = 
@"Provide answers that are common sense to a 10 year old. 
Each Answer in the formatted: VALUE-NAME | VALUE, VALUE, VALUE
**Individual ANSWERS should contain no more than 3 values**
**Individual VALUE should be ONE or TWO words.**
Example for dog: is-a | animal, pet
Example for numerical VALUE: contains (with counts) always contains parts | 2 eyes, 4 legs, 1 tail
Use any VALUE only once. 
Use the following VALUE-NAMEs if appropriate: 
is-a (where each value is a physical thing), 
can, 
always contains parts (with counts),
usually contains parts (with counts)
has unique characteristics,
is-part-of-speech, ";
                //the following have been tried but are not very consistent/useful
                //$"list examples of {textIn} (up to 5) do not include things which have a property of {textIn} , " +
                //"needs, " +
                //"is-part-of, " +
                //"is-bigger-than, " +
                //"is-smaller-than, " +
                //"is-similar-to, " +

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

        //given general information output from GPT, parse it into UKS
        public static void ParseGPTOutput(string textIn, string GPTOutput)
        {
            // Get the UKS
            UKS.UKS theUKS = MainWindow.theUKS;
            //GetUKS();
            // Split by comma (,) to get individual pairs
            string[] valuePairs = GPTOutput.Split(';', '\n');
            // Error check
            if (valuePairs.Length == 0)
            {
                Debug.WriteLine($"Error, length of value '{Output}' is 0.");
                ModuleGPTInfoDlg.errorCount += 1;
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
                        if (value.Contains('(') || value.Contains(')') || value.Contains(".") || value.Contains("\""))
                        {
                            continue;
                        }
                        string count = "";

                        //capture compound verbs based on "can"
                        List<string> subValues = value.Split(" ").ToList();
                        if (subValues.Count > 2) continue;
                        if (valueType == "can" && subValues.Count > 1)
                        {
                            valueTypeAttributes.Add("."+subValues[0]);
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

                            //trap numbers...sometimes GPT writes them out in text
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
                                valueProperties.Add("."+subValues[0]);
                                subValues.RemoveAt(0);
                            }
                        }

                        //write the valueType|value  to the UKS
                        if (subValues.Count > 0)
                            value = string.Join(" ", subValues);
                        else
                            continue;
                        if (value == textIn) continue;
                        if (valueType.StartsWith("examples"))
                        {
                            theUKS.AddStatement("." + value, "is-a", "." + textIn); //note reversal
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        else if (valueType.StartsWith("is-part-of-speech"))
                        {
                            theUKS.AddStatement("." + textIn, "is-a", "." + value);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        else if (valueType.StartsWith("has-properties"))
                        {
                            theUKS.AddStatement("." + textIn, "is", "." + value, null, null, valueProperties);
                        }
                        else if (valueType.Contains("contains"))
                        {
                            Relationship r;
                            if (count == "" || count == "1")
                                r = theUKS.AddStatement("." + textIn, "has", "." + value, null, null, valueProperties);
                            else
                                r = theUKS.AddStatement("." + textIn, "has", "." + value, null, count, valueProperties);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                            if (valueType.Contains("usually"))
                                r.Weight = .75f;
                        }
                        else
                        {
                            theUKS.AddStatement("." + textIn, valueType, "." + value, null, valueTypeAttributes, valueProperties);
                            ModuleGPTInfoDlg.relationshipCount += 1;
                        }
                        ///////   null reltypes? This was a safety check
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