//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
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

        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        public static async Task GetChatGPTVerifyParentChild(string child,string parent)
        {
            child = child.ToLower();
            child = child.Replace(".", "");
            parent = parent.ToLower();
            parent = parent.Replace(".", "");
            try
            {

                string answerString = "";
                string systemText = $"Provide commonsense facts about the following: ";
                string userText = $"Is it reasonable to say in common usage: {child} is a {parent}? (yes or now, no explanation)" ;

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
                        if (tParent == null) tParent = MainWindow.theUKS.Labeled("."+parent);
                        if (tParent == null) return;
                        Thing tChild = MainWindow.theUKS.Labeled(child);
                        if (tChild== null) tChild = MainWindow.theUKS.Labeled("." + child);
                        if (tChild== null) return;
                        tChild.RemoveParent(tParent);
                        Debug.WriteLine($"Relationship: {child} is-a {parent} has been removed. ");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with getting Chat GPT Fine tuning. Error is {ex}.");
            }

        }

        public static async Task GetChatGPTParents(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                if (textIn.StartsWith("."))
                    textIn = textIn.Replace(".", "");

                string answerString = "";
                string userText = $"Provide commonsense clasification answer the request: what is-a {textIn}";
                string systemText = 
                    "This is a classification request. Examples: horse is-a | animal, mammal \n\r chimpanzee is-a | primate, mammal"+
                    "Answer is formatted: is-a | VALUE, VALUE, VALUE with no more than 3 values and VAIUES are 1 or 2 words\n\r" +
                    $"Answer should ONLY contain VALUEs where it is reasonable to say: '{textIn} is-a VALUE' and exclue: 'VALUE is-a {textIn}'";
                //string systemText = "Provide answers that are common sense to a 10 year old. \n\r" +
                //                                 $"if {textIn} is not a noun, answer with the word's part of speech. \r\n "+
                //                                 "Each Answer is formatted: IS A | VALUE, VALUE, VALUE with no more than 3 values\n\r" +
                //                                 "If there is more than one VALUE, these should be saparated by commas. \n\r" +
                //                                 "Each VALUE should be anoun \r\n"+
                //                                 "Individual VALUEs should not be more than two words. \n\r" +
                //                                 "All answers should be reasonable to say in common useage. \n\r" +
                //                                 "For example, if given dog, answer: is-a | animal, mammal, pet";

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
        public static async Task GetChatGPTData(string textIn)
        {
            try
            {
                textIn = textIn.ToLower();
                textIn = textIn.Replace(".", "");

                string answerString = "";
                string userText = $"Provide commonsense facts to answer the request: what is a {textIn}";
                string systemText = "Provide answers that are common sense to a 10 year old. \n\r" +
                                    "Each Answer in the formatted: VALUE-NAME | VALUE, VALUE, VALUE \r\n"+
                                    "**Individual ANSWERS should contain no more than 3 values** \n\r" +
                                    "**Individual VALUE should be ONE or TWO words.** \n\r" +
                                    "Example for dog: is-a | animal, pet \r\n" +
                                    "Example for numerical VALUE: contains (with counts) always contains parts | 2 eyes, 4 legs, 1 tail \n\r" +
                                    "Use any VALUE only once. \r\n" +
                                    "Use the following VALUE-NAMEs if appropriate: " +
                                    "is-a (where each value is a physical thing), " +
                                    "can, " +
                                    "always contains parts (with counts), " +
                                    "usually contains parts (with counts) " +
                                    "has unique characteristics, " +
                                    $"list examples of {textIn} (up to 5) do not include things which have a property of {textIn} , " +
                                    //the following are not very consistent/useful
                                    //"needs, " +
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
                        if (valueType.StartsWith("examples"))
                        {
                            theUKS.AddStatement("." + value, "is-a", "." + textIn); //note reversal
                        }
                        else if (valueType.StartsWith("is-part-of-speech"))
                        {
                            theUKS.AddStatement("." + textIn, "is-a", "." + value);
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
                            if (valueType.Contains("usually"))
                                r.Weight = .75f;
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