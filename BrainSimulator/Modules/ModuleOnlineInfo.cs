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
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleOnlineInfo : ModuleBase
    {

        //this contains access methods for getting information from
        //   chatGPT
        //   wikidata
        //   wiktionary
        //   conceptnet
        //   kidsWortsmyth dictionary*Fk
        //   webseters elementrary dictionary
        //   dictionaryAPI
        //   CSKG (common sense knowledge graph)
        //
        //   Only the kids actually works

        public string Output = "";

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleOnlineInfo()
        {
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (wordsToLookUp is null) wordsToLookUp = new();

            if (wordsToLookUp.Count > 0)
            {
                string word = wordsToLookUp[0].Item1;
                int mostUsed = wordsToLookUp.FindIndex(t => t.Item2 == wordsToLookUp.Max(t => t.Item2));
                var x = wordsToLookUp[0];
                wordsToLookUp.RemoveAt(0);
                ConceptNetLocal(x.Item1);
            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        List<(string, int)> wordsToLookUp;
        List<string> CSKGContent;
        public void GetCSKGData(string word)
        {

            if (CSKGContent is not null)
            {
                foreach (string line in CSKGContent)
                {
                    if (line is null) break;
                    var fields = line.Split("\t");
                    string source = fields[4];
                    string target = fields[5];
                    string rel = fields[6];

                    if (fields[6] == "is a" &&
                        source == word && !target.Contains(" ") && !target.Contains("|"))// && fields[8] == "CN")
                    {
                        // Process the line
                        Debug.WriteLine($"{source} -> {rel} -> {target}");
                        //theUKS.AddStatement(source, "is-a", target);
                    }
                }
            }

            else
            {
                int count = 0;
                int countEN = 0;
                CSKGContent = new();
                GetUKS();
                try
                {
                    // Open the file
                    using (StreamReader reader = new StreamReader(@"C:\Users\c_sim\source\cskg.tsv\cskg.tsv"))
                    {
                        //skip the header line
                        string header = reader.ReadLine();
                        var headerFields = header.Split("\t");
                        // Read the first 20 lines
                        for (int i = 0; i < 10_000_000; i++)
                        {
                            count++;
                            if (count % 100000 == 0) Debug.Write(".");
                            string line = reader.ReadLine();
                            if (line is null) break;
                            var fields = line.Split("\t");
                            string source = fields[4];
                            string target = fields[5];
                            string rel = fields[6];

                            if (fields[1].StartsWith("/c/en") && fields[3].StartsWith("/c/en"))
                            {
                                CSKGContent.Add(line);
                                countEN++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                { }
                Debug.WriteLine($"{count.ToString("N0")} entries with {countEN.ToString("N0")} in english");
            }

        }

        Dictionary<string, List<Result>> sourceHash;
        Dictionary<string, List<Result>> targetHash;
        class Result
        {
            public string sourceURI;
            public string targetURI;
            public string sourceText;
            public string targetText;
            public string rel;
            public float fWeight;
            public string json;
            public override string ToString()
            {
                string resultString = $"{sourceText} -> {rel} -> {targetText} :  {fWeight.ToString("F2")}";
                return resultString;
            }
            public static bool operator ==(Result r1, Result r2)
            {
                if (r1.rel == r2.rel && r1.sourceURI == r2.sourceURI && r1.targetURI == r2.targetURI && r1.fWeight == r2.fWeight) return true;
                return false;
            }
            public static bool operator !=(Result r1, Result r2)
            {
                return !(r1 == r2);
            }
            public override bool Equals(object obj)
            {
                if (obj is Result r)
                    return this == r;
                return false;
            }
        }

        public void ConceptNetLocal(string word)
        {
            if (sourceHash is null)
            {
                ReadConceptNetFile();
                return;
            }
            if (word == "")
            {
                if (sourceHash is not null)
                {
                    //delete orphas
                    Thought objectRoot = theUKS.GetOrAddThought("Object", "Thought");
                    if (objectRoot is null) return;
                    for (int i = 0; i < objectRoot.Children.Count; i++)
                    {
                        Thought child = (Thought)objectRoot.Children[i];
                        int descendentCount = child.DescendantsList().Count;
                        if (descendentCount == 1)
                        {
                            i--;
                            theUKS.DeleteAllChildren(child);
                            theUKS.DeleteThought(child);
                        }
                    }
                    int counter = 0;
                    foreach (var l in sourceHash.Values)
                    {
                        //if (counter > 20000) break;
                        if (counter % 100 == 0)
                            Debug.Write(".");
                        counter++;
                        foreach (Result r in l)
                        {
                            if (wordList2.Any(x => x.Item1 == r.sourceText) && wordList2.Any(x => x.Item1 == r.targetText) && r.rel == "IsA")
                                theUKS.AddStatement(r.sourceText, "is-a", r.targetText);
                        }
                    }
                    return;
                }
            }
            //check UKS for circular references
            //foreach (Thought t1 in theUKS.GetTheUKS())
            //{
            //    if (t1.HasAncestor(t1))
            //    {
            //        //find a direct path which is the circle
            //        Thought t2 = t1;
            //        List<Thought> x;
            //        while ((x = t2.Parents.FindAll(x => x.HasAncestor(t1))).Count > 0)
            //        {
            //            if (x.Count == 1)
            //            {
            //                Debug.Write($"{x[0].Label} -> ");
            //                t2 = x[0];
            //            }
            //            else { }
            //            if (t2 == t1) break;
            //        }
            //    }
            //}


            List<Result> resultListHash = new();
            if (sourceHash.ContainsKey(word))
                resultListHash.AddRange(sourceHash[word].FindAll(x => x.rel == "IsA"));
            if (targetHash.ContainsKey(word))
                resultListHash.AddRange(targetHash[word].FindAll(x => x.rel == "IsA"));

            if (resultListHash is null) return;
            foreach (Result r in resultListHash)
            {
                try
                {
                    string source = r.sourceText;
                    string target = r.targetText;
                    if (!wordList2.Any(x => x.Item1 == source)) continue;
                    if (!wordList2.Any(x => x.Item1 == target)) continue;

                    string rel = r.rel;

                    //get the surfacetext if any
                    string surfaceText = "";
                    int index = r.json.LastIndexOf("surfaceText");
                    if (index != -1)
                    {
                        surfaceText = r.json.Substring(index + 15);
                        index = surfaceText.IndexOf("\"");
                        if (index != -1) { surfaceText = surfaceText.Substring(0, index); }
                    }

                    if (rel.Contains("/"))
                        rel = rel.Substring(rel.LastIndexOf("/") + 1);
                    float fWeight = r.fWeight;

                    bool sourceMatch = (source == word);// || source.Contains($"_{word}_") || source.StartsWith(word + "_") || source.EndsWith("_" + word));
                    bool targetMatch = (target == word);// || target.Contains($"_{word}_") || target.StartsWith(word + "_") || target.EndsWith("_" + word));
                    if ((sourceMatch || targetMatch) && source != target && rel == "IsA")// && fWeight >= 2.0)
                    {
                        Result result = new Result() { fWeight = fWeight, sourceText = source, targetText = target, rel = rel, };
                        //                            if (!target.Contains("_"))
                        {
                            // Process the line
                            //Debug.WriteLine($"{source} -> {rel} -> {target} :  {fWeight.ToString("F2")}");
                            //Debug.Write(".");
                            if (wordsToLookUp is null) wordsToLookUp = new();
                            Thought t = theUKS.Labeled(target);
                            if (t is null)
                            {
                                //    int index1 = wordsToLookUp.FindIndex(x => x.Item1 == target);
                                //    if (index1 == -1)
                                //        wordsToLookUp.Add((target, 1));
                                //    else
                                //        wordsToLookUp[index1] = (wordsToLookUp[index1].Item1, wordsToLookUp[index1].Item2 + 1);
                                //
                            }
                            //if (sourceHash[source].Count > 5 && targetHash[target].Count > 5)
                            {
                                Thought r1 = theUKS.AddStatement(source, "is-a", target);
                                if (r1 is not null)
                                {
                                    if (r1.TimeToLive == TimeSpan.MaxValue)
                                        r1.TimeToLive = TimeSpan.FromSeconds(1130);
                                    else if (r1.TimeToLive > TimeSpan.FromSeconds(30))
                                        r1.TimeToLive = TimeSpan.FromDays(1);
                                    else
                                        r1.TimeToLive += TimeSpan.FromSeconds(90);
                                }
                                else
                                { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                { }
            }

        }

        private string RemoveParentheticals(string line)
        {
            string pattern = @"\([^()]*\)";

            // Use the Regex.Replace method to replace any matches with an empty string
            line = Regex.Replace(line, pattern, "");
            return line;
        }
        private string GetParenthetical(string line)
        {
            string retVal = "";
            int startIndex = line.IndexOf('(');
            int endIndex = line.IndexOf(')');

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                retVal = line.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
            return retVal;
        }

        private void ReadConceptNetFile()
        {
            string EnglishPath = @"C:\Users\c_sim\source\conceptnet-assertions-5.7.0 (1).csv\assertionsEng.csv";
            int count = 0;
            int countEN = 0;
            sourceHash = new();
            targetHash = new();
            GetUKS();
            try
            {
                if (!File.Exists(EnglishPath))
                {
                    using (StreamReader reader = new StreamReader(@"C:\Users\c_sim\source\conceptnet-assertions-5.7.0 (1).csv\assertions.csv"))
                    using (StreamWriter writer = new StreamWriter(EnglishPath))
                    {
                        string line = reader.ReadLine();
                        while (line is not null)
                        {
                            count++;
                            if (count % 100000 == 0) Debug.Write(".");
                            if (line.Contains("c/en/") && line.Contains("IsA"))
                                writer.WriteLine(line);
                            line = reader.ReadLine();
                        }
                    }
                }
                // Open the file
                using (StreamReader reader = new StreamReader(EnglishPath))
                {
                    //skip the header line
                    string line = reader.ReadLine();
                    while (line is not null)
                    {
                        count++;
                        if (count % 100000 == 0) Debug.Write(".");
                        var fields = line.Split("\t");
                        string sourceURI = fields[2];
                        string targetURI = fields[3];
                        string rel = fields[1];
                        string json = fields[4];
                        if (sourceURI.Contains("c/en/") && targetURI.Contains("c/en/") && rel != "r/ExternalURL")
                        {
                            string source = sourceURI;
                            string target = targetURI;
                            string partOfSpeechS = "";
                            string partOfSpeechT = "";
                            GetValueFromURI(ref source, ref partOfSpeechS);
                            GetValueFromURI(ref target, ref partOfSpeechT);
                            //string weight = fields[4].Substring(fields[4].LastIndexOf("weight")).Substring(8).Replace("}", "").Trim();
                            //float.TryParse(weight, out float fWeight);
                            //if (fWeight >= 1)
                            {
                                Result newEntry = new Result()
                                {
                                    sourceURI = sourceURI,
                                    sourceText = source,
                                    rel = rel.Substring(3),
                                    targetURI = targetURI,
                                    targetText = target,
                                    fWeight = 0,// fWeight,
                                    json = json,
                                };
                                if (!sourceHash.ContainsKey(source))
                                    sourceHash.Add(source, new List<Result>());
                                if (newEntry.sourceText == source)
                                    sourceHash[source].Add(newEntry);
                                if (!targetHash.ContainsKey(target))
                                    targetHash.Add(target, new List<Result>());
                                if (newEntry.targetText == target)
                                    targetHash[target].Add(newEntry);
                                countEN++;
                            }
                        }
                        line = reader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            { }
            foreach (var w in wordList2)
            {
                if (!sourceHash.ContainsKey(w.Item1))
                    Debug.WriteLine(w);
            }
            Debug.WriteLine($"\n{count.ToString("N0")} entries with {countEN.ToString("N0")} in english");
        }

        static char[] partsOfSpeech = new char[] { 'n', 'v', 'a', 's', 'r' };

        private static void GetValueFromURI(ref string URI, ref string partOfSpeech)
        {
            //is the part-of-speech at the end
            if (URI[URI.Length - 2] == '/')
                if (partsOfSpeech.Contains(URI[URI.Length - 1]))
                {
                    partOfSpeech = URI.Substring(URI.Length - 1);
                    URI = URI.Substring(0, URI.Length - 2);
                }
            int index = URI.IndexOf("/n/");
            if (index != -1) partOfSpeech = "n";
            else
            {
                index = URI.IndexOf("/v/");
                if (index != -1) partOfSpeech = "v";
                else
                {
                    index = URI.IndexOf("/a/");
                    if (index != -1) partOfSpeech = "a";
                    else
                    {
                        index = URI.IndexOf("/s/");
                        if (index != -1) partOfSpeech = "s";
                        else
                        {
                            index = URI.IndexOf("/r/");
                            if (index != -1) partOfSpeech = "r";
                        }
                    }
                }
            }
            if (index != -1)
                URI = URI.Substring(0, index);
            URI = URI.Substring(URI.LastIndexOf("/") + 1);
        }

        public async void GetConceptNetData(string text)
        {
            ConceptNetLocal(text);
            return;
            var url = @"https://api.conceptnet.io/c/en/" + text;
            var myClient = new HttpClient();
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(propertyURL);
            Output = "";
            foreach (var edge in myDeserializedClass.edges)
            {
                if (edge.start.language != "en") continue;
                if (edge.end.language != "en") continue;
                string start = GetTailOfURL(edge.start.term);
                string end = GetTailOfURL(edge.end.term);
                string rel = edge.rel.label;
                if (rel == "IsA")
                    Output += start + "->is-a-> " + end + "   " + edge.surfaceText + " " + edge.weight.ToString("f3") + "\n";
                else
                    Output += start + "->" + rel + "-> " + end + "   " + edge.surfaceText + " " + edge.weight.ToString("f3") + "\n";
            }
            return;
        }
        string GetTailOfURL(string url)
        {
            int index = url.LastIndexOf("/");
            if (index == -1) return url;
            return url.Substring(index + 1);
        }

        List<(string, string)> wordList2 = new();
        List<string> wordList = new List<string>();
        private void SetupWordList()
        {
            wordList = new();
            try
            {

                // also a possible resource Vocabulary.com

                // common words.pdf
                ///https://cehs.unl.edu/documents/secd/aac/vocablists/VLN1.pdf
                string wordListPath = @"C:\Users\c_sim\source\wordlist.txt";
                using (StreamReader reader = new StreamReader(wordListPath))
                {
                    string line = reader.ReadLine();
                    while (line is not null)
                    {
                        line = line.Replace("\t", " ");
                        string[] words = line.Split(" ");
                        foreach (string s in words)
                        {
                            if (s.Trim() != "")
                            {
                                if (!wordList.Contains(s))
                                    wordList.Add(s);
                            }
                        }
                        line = reader.ReadLine();
                    }
                }
                wordList = wordList.OrderBy(x => x).ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void SetupWordList2(string wordIn = "")
        {
            if (wordIn != "")
            {
                Output = "";
                var w = wordList2.FindAll(x => x.Item1 == wordIn);
                foreach (var s in w)
                    Output += $"\n{s.Item1}, {s.Item2}";
                return;
            }
            wordList2 = new();
            try
            {
                //The Oxford 3000  (the list of most important words (A1-B2 level)
                //https://www.oxfordlearnersdictionaries.com/external/pdf/wordlists/oxford-3000-5000/American_Oxford_3000.pdf
                string wordList2Path = @"C:\Users\c_sim\source\3000 words.txt";
                using (StreamReader reader = new StreamReader(wordList2Path))
                {
                    string line2;
                    string prevWord = "";
                    while ((line2 = reader.ReadLine()) is not null)
                    {
                        string line = line2;
                        string[] lineBreaks = new string[] { "A1", "B1", "A2", "B2" };
                        string[] words = line.Split(lineBreaks, StringSplitOptions.None);
                        for (int i = 0; i < words.Length; i++)
                        {
                            List<string> pos = new();
                            string word = words[i];
                            if (word == "") continue;
                            if (word.Contains(" v.")) pos.Add("verb");
                            if (word.Contains("adj.")) pos.Add("adjective");
                            if (word.Contains("adv.")) pos.Add("adverb");
                            if (word.Contains("prep.")) pos.Add("preposition");
                            if (word.Contains("n.")) pos.Add("noun");
                            if (word.Contains("number")) pos.Add("number");
                            if (word.Contains("det.")) pos.Add("determinant");
                            word = word.Replace("A1", "");
                            word = word.Replace("A2", "");
                            word = word.Replace("B1", "");
                            word = word.Replace("B2", "");
                            word = word.Replace(",", "");
                            word = word.Replace("adj.", "");
                            word = word.Replace("prep.", "");
                            word = word.Replace("conj.", "");
                            word = word.Replace("exclam.", "");
                            word = word.Replace("adv.", "");
                            word = word.Replace("det.", "");
                            word = word.Replace("pron.", "");
                            word = word.Replace("noun.", "");
                            word = word.Replace("n.", "");
                            word = word.Replace("v.", "");
                            word = word.Replace("indefinite article", "");
                            word = word.Replace("definite article", "");
                            word = word.Replace("/", "");
                            word = word.Replace(",", "");
                            word = word.Replace("\t", " ");
                            word = word.Replace("-", "");
                            word = word.Replace("’", "");
                            word = word.ToLower();
                            word = RemoveParentheticals(word);
                            //word = Thought.TrimDigits(word.Trim());

                            if (word == "" && pos.Count > 0)
                                word = prevWord;
                            prevWord = word;
                            foreach (string p in pos)
                            {
                                if (word == "")
                                { }
                                wordList2.Add((word, p));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            { }
            wordList2 = wordList2.OrderBy(x => x.Item1).ToList();
            GetUKS();
            foreach (var word in wordList2)
            {
                if (word.Item1 == "")
                { continue; }
                Thought existingThought = null;
                /* rewrite for new single-label concept
                 * var existingThoughts = theUKS.Labeled(word.Item1);
                                foreach (var t in existingThoughts)
                                    if (t.HasAncestor("Words"))
                                    {
                                        existingThought = t;
                                        break;
                                    }
                   */
                string firstLetter = word.Item1.Substring(0, 1).ToUpper();
                theUKS.GetOrAddThought("Words", "Object");
                theUKS.GetOrAddThought(word.Item2, "Words");
                Thought letterParent = theUKS.GetOrAddThought(firstLetter, word.Item2);

                if (existingThought is null)
                    existingThought = theUKS.GetOrAddThought(word.Item1, letterParent);
                else
                    existingThought.AddParent(letterParent);
            }
        }

        //TODO: properly handle phrases, related, similar...currently only picks up first instance

        public class KidsWord
        {
            public string word;
            public List<KidsDefinition> definitions = new();
            public string related;
            public string similar;
            public string phrase;
            public override string ToString()
            {
                string retVal = word + "\n";
                foreach (var def in definitions)
                    retVal += def.definition + "\n";
                return retVal;
            }
        }
        public class KidsDefinition
        {
            public string definition;
            public List<string> pharses = new();
            public List<string> imageURLs = new();
            public List<string> inflections = new();
            public string pOS;
        }

        public List<KidsWord> kids;


        public void GetKidsDefinition(string text)
        {
            foreach (var kid in kids)
            {
                if (kid.word == text)
                {
                    Output = kid.ToString();
                    GetUKS();
                    Thought incomingInfo = theUKS.GetOrAddThought("CurrentIncomingDefinition", "Attention");
                    incomingInfo.V = kid;
                    return;
                }
            }
            //GetKidsWordsmythNet(text);
            return;
        }

        public async void GetKidsWordsmythNet(string text)
        {
            var client = new HttpClient();
            var response = await client.GetAsync($"https://kids.wordsmyth.net/we/?ent={text}");
            response.EnsureSuccessStatusCode();
            string htmlContent = await response.Content.ReadAsStringAsync();

            if (htmlContent.Contains("Did you mean this word")) return;

            Output = "";
            string posSearch = "return false;\">";
            List<int> posIndices = FindAllIndices(htmlContent, "part of speech:");
            List<string> poss = new();
            foreach (int posIndex in posIndices)
            {
                int i1 = htmlContent.IndexOf(posSearch, posIndex);
                if (i1 == -1) continue;
                i1 += posSearch.Length;
                int i2 = htmlContent.IndexOf("<", i1);
                string pos = htmlContent.Substring(i1, i2 - i1);
                poss.Add(pos);
            }


            if (kids is null) kids = new();
            KidsWord kidsdef = new() { word = text };
            kids.Add(kidsdef);
            string defSearch = "data\">";
            List<int> defIndices = FindAllIndices(htmlContent, ">definition");

            string GetPartOfSpeech(int index)
            {
                for (int i = poss.Count - 1; i >= 0; i--)
                    if (index > posIndices[i])
                        return poss[i];
                return "";
            }
            int GetDefinition(int index)
            {
                for (int i = defIndices.Count - 1; i >= 0; i--)
                    if (index > defIndices[i])
                        return i;
                return -1;
            }


            foreach (int defIndex in defIndices)
            {
                int i1 = htmlContent.IndexOf(defSearch, defIndex);
                if (i1 == -1) continue;
                i1 += defSearch.Length;
                int i2 = htmlContent.IndexOf("<", i1);
                string defn = htmlContent.Substring(i1, i2 - i1);
                defn = GetPartOfSpeech(i1) + ": " + defn;
                kidsdef.definitions.Add(new KidsDefinition() { definition = defn });
            }
            Output = kidsdef.ToString();
            GetUKS();
            Thought incomingInfo = theUKS.GetOrAddThought("CurrentIncomingInfo", "Attention");
            incomingInfo.V = kidsdef;

            int index = 0;
            kidsdef.phrase = GetStringValue(htmlContent, "phrase:", ref index, posSearch, "<");
            index = 0;
            kidsdef.similar = GetStringValue(htmlContent, "similar words:", ref index, posSearch, "<");
            index = 0;
            kidsdef.related = GetStringValue(htmlContent, "related words:", ref index, posSearch, "<");
            index = 0;
            string retString = "";
            index = 0;
            do
            {
                retString = GetStringValue(htmlContent, "background-image", ref index, "url(", ")");
                if (retString != "")
                {
                    int defNo = GetDefinition(index);
                    if (defNo != -1)
                        kidsdef.definitions[defNo].imageURLs.Add(retString);
                }
                else break;
            } while (true);
        }
        string GetStringValue(string content, string target, ref int startIndex, string startTag, string endTag)
        {
            int i1 = content.IndexOf(target, startIndex);
            if (i1 == -1) return "";
            i1 = content.IndexOf(startTag, i1);
            i1 += startTag.Length;
            int i2 = content.IndexOf(endTag, i1);
            string retVal = content.Substring(i1, i2 - i1);
            startIndex = i2;
            return retVal;
        }

        public List<int> FindAllIndices(string str, string substr)
        {
            var indices = new List<int>();

            int index = str.IndexOf(substr);
            while (index != -1)
            {
                indices.Add(index);
                index = str.IndexOf(substr, index + 1);
            }

            return indices;
        }

        public async void GetWiktionaryData(string textIn)
        {
            var word = textIn; // the word to look up

            var client = new HttpClient();
            var response = await client.GetAsync($"https://en.wiktionary.org/api/rest_v1/page/definition/{word}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var en = document.RootElement.GetProperty("en");
            if (en.ValueKind == JsonValueKind.Array)
            {
                Output = "";
                foreach (var item in en.EnumerateArray())
                {
                    var definitions = item.GetProperty("definitions");
                    foreach (var definition in definitions.EnumerateArray())
                    {
                        foreach (var x in definition.EnumerateObject())
                        {
                            if (x.Name == "definition")
                            {
                                string y = x.Value.ToString();
                                var z = Regex.Replace(y, "<.*?>", string.Empty);
                                if (z.Trim() != "")
                                    Output += z + "\n";
                            }
                        }
                    }
                }
            }
        }

        public enum QueryType { general, isa, hasa, can, count, list, listCount, types, partsOf };
        public async void GetChatGPTResult(string textIn, QueryType qtIn = QueryType.isa, string altLabel = "")
        {
            try
            {
                QueryType qType = qtIn;
                if (altLabel == "") altLabel = textIn;
                string prompt;
                string apiKey = ConfigurationManager.AppSettings["APIKey"];
                var client = new HttpClient();
                var url = "https://api.openai.com/v1/chat/completions";
                string queryText = textIn;
                textIn = textIn.ToLower();

                switch (qType)
                {
                    case QueryType.general:
                        queryText = textIn;
                        break;
                    case QueryType.isa:
                        queryText = $"Provide common-sense facts about the following: {textIn}";
                        break;
                    case QueryType.hasa:
                        queryText = $"Provide common-sense facts about the following: {textIn}";
                        break;
                }


                string answerString = await GPT.GetGPTResult("Answer appropriate for a 10 year old.",queryText);
                if (!answerString.StartsWith("ERROR"))
                {

                    // Extract the generated text from the CompletionResult object
                    Output = answerString.Trim().ToLower();
                    Debug.WriteLine(">>>" + queryText);
                    Debug.WriteLine(Output);
                    //some sort of error occurred
                    if (Output.Contains("language model")) return;

                    //// Split by comma (,) to get individual values
                    //string[] values = Output.Split(",");
                    //// Get the UKS
                    //GetUKS();
                    //foreach (string s in values)
                    //{
                    //    Debug.WriteLine("Individual Itm: " + s);
                    //    switch (qType)
                    //    {
                    //        case QueryType.isa:
                    //            theUKS.AddStatement(textIn, "is-a", s);
                    //            // theUKS.AddStatement(parameters[0], "is-a", parameters[2]);
                    //            break;
                    //        case QueryType.hasa:
                    //            theUKS.AddStatement(textIn, "has", s);
                    //            break;
                    //    }
                    //}


                }
                else
                    Output = answerString;
            }
            catch (Exception ex)
            {

            }
        }
        IPluralize pluralizer = new Pluralizer();
        string SingularizePhrase(string input)
        {
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
                words[i] = pluralizer.Singularize(words[i].ToLower().Trim());
            input = "";
            foreach (string word in words)
                input += " " + word;
            return input.Trim();
        }
        string PluralizePhrase(string input)
        {
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
                words[i] = pluralizer.Pluralize(words[i].ToLower().Trim());
            input = "";
            foreach (string word in words)
                input += " " + word;
            return input.Trim();
        }

        //This method rerieves the wikidata query number value from wikidata query
        // for the Thought item and passes that value to GetPropertiesFromURL along
        // with the property query named prop
        public async void GetWikidataData(string item, string prop)
        {
            var itemLabel = item;
            try
            {
                Network.httpClientBusy = true;
                var url = "http://" + "www.wikidata.org/w/api.php?action=wbsearchentities&search=" +
                          itemLabel +
                          "&language=en&format=xml";
                var response = await Network.theHttpClient.GetAsync(url);

                if (response is not null)
                {
                    var content = response.Content.ReadAsStringAsync();
                    XmlDocument xmlItemDoc = new XmlDocument();
                    xmlItemDoc.LoadXml(content.Result.ToString());
                    var xmlItemDocValue = xmlItemDoc.GetElementsByTagName("entity");
                    var xmlItemDocValueFirst = xmlItemDocValue[0];
                    if (xmlItemDocValueFirst is not null)
                    {
                        var nameLabel = xmlItemDocValueFirst.Attributes[0];
                        string itemID = nameLabel.InnerXml;
                        Network.httpClientBusy = false;
                        GetPropertiesFromURL(itemID, prop);///
                    }
                }
            }
            catch { }

        }
        //This method gets all the properties or subproperties of a property propName from a Thought
        // with name propName and wikidata query number value numberOfName associated with propName
        private async void GetPropertiesFromURL(string itemID, string propName)
        {
            Output = "";
            propName = propName.ToLower();
            Network.httpClientBusy = true;
            var urlBegin = @"https://query.wikidata.org/sparql?query=";
            var url = @"SELECT ?wdLabel ?ps_Label ?wdpqLabel ?pq_Label " +
                      @"{VALUES(?company) { (wd:" + itemID + @")} " +
                      @"?company ?p ?statement. ?statement ?ps ?ps_. ?wd " +
                      @"wikibase:claim ?p. ?wd wikibase:statementProperty " +
                      @"?ps.OPTIONAL{?statement ?pq ?pq_. ?wdpq wikibase:qualifier " +
                      @"?pq .} SERVICE wikibase:label { bd:serviceParam wikibase:language ""en"" }} " +
                      @"ORDER BY ?wd ?statement ?ps_";
            url = urlBegin + Uri.EscapeUriString(url);
            var myClient = new HttpClient();
            myClient.DefaultRequestHeaders.Add("User-Agent", "c# program");
            var responseURL = await myClient.GetAsync(url);
            var propertyURL = await responseURL.Content.ReadAsStringAsync();
            XmlDocument propertyDoc = new XmlDocument();
            string propertyURLResult = propertyURL.ToString();
            propertyDoc.LoadXml(propertyURLResult);

            //Thought props = theUKS.GetOrAddThought("Properties", item);
            if (propName != "")
            {
                //Thought prop = theUKS.GetOrAddThought(propName, props);
                var propertyToCheck = propName;

                var docPropertyValues = propertyDoc.GetElementsByTagName("result");
                string[][] end = new string[docPropertyValues.Count][];
                int docCount = 0;
                foreach (XmlElement xn in docPropertyValues) //for each result
                {
                    bool found = false;
                    string[] fn = new string[xn.ChildNodes.Count];
                    for (int i = 0; i < xn.ChildNodes.Count; i++)
                    {
                        if (xn.ChildNodes[i].InnerText == propertyToCheck)
                        {
                            var t = xn.ChildNodes[i].Value;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        for (int i = 0; i < xn.ChildNodes.Count; i++)
                        {
                            if (xn.ChildNodes[i].OuterXml.Contains("ps_Label"))
                            {
                                if (!Output.Contains(xn.ChildNodes[i].InnerText))
                                {
                                    // Debug.WriteLine(xn.ChildNodes[i].InnerText);
                                    fn[i] = (xn.ChildNodes[i].InnerText.ToString());
                                    Output += xn.ChildNodes[i].InnerText + "\n";
                                }
                                break;

                            }
                        }
                    }
                    if (fn is not null)
                    {
                        end[docCount] = fn;
                    }
                    docCount++;
                }
                string propString = "";
                //TextBoxWiki.Text = "";
                for (int i = 0; i < docPropertyValues.Count; i++)
                {
                    foreach (var text in end[i])
                    {
                        if (text is not null)
                        {
                            Debug.WriteLine(text);

                            //theUKS.GetOrAddThought(text, prop);
                            //AddObjectPropertyToMentalModel(name, text, prop.Label);
                            //TextBoxWiki.Text += text + '\n';
                        }
                    }
                }
            }
            else
            {
                var docPropertyValues = propertyDoc.GetElementsByTagName("result");
                string[][] end = new string[docPropertyValues.Count][];
                int docCount = 0;
                foreach (XmlElement xn in docPropertyValues)
                {
                    bool found = false;
                    string[] fn = new string[xn.ChildNodes.Count];
                    //Thought prop = new Thought();
                    for (int i = 0; i < xn.ChildNodes.Count; i++)
                    {
                        Debug.WriteLine(xn.ChildNodes[i].InnerText + " :: ");
                        //if (i == 0)
                        //{
                        //    prop = theUKS.GetOrAddThought(xn.ChildNodes[i].InnerText, props);
                        //}
                        //else
                        //{
                        //    fn[i] = (xn.ChildNodes[i].InnerText.ToString());
                        //    theUKS.GetOrAddThought(xn.ChildNodes[i].InnerText, prop);
                        //}
                    }

                    if (fn is not null)
                    {
                        end[docCount] = fn;
                    }
                    docCount++;
                }

                string propString = "";
                //TextBoxWiki.Text = "";
                for (int i = 0; i < docPropertyValues.Count; i++)
                {

                    foreach (var text in end[i])
                    {
                        if (text is not null)
                        {
                            Debug.Print(text);
                            //theUKS.GetOrAddThought(text, props);
                            //AddObjectPropertyToMentalModel(name, text, props.Label);
                            //TextBoxWiki.Text += text + '\n';
                        }
                    }
                }
            }
        }


        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            //SetupWordList();
            //SetupWordList2();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
//            if (mv is null) return; //this is called the first time before the module actually exists
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Edge
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string dataset { get; set; }
            public End end { get; set; }
            public string license { get; set; }
            public Rel rel { get; set; }
            public List<Source> sources { get; set; }
            public Start start { get; set; }
            public string surfaceText { get; set; }
            public double weight { get; set; }
        }

        public class End
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string label { get; set; }
            public string language { get; set; }
            public string term { get; set; }
            public string sense_label { get; set; }
        }

        public class Rel
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string label { get; set; }
        }

        public class Root
        {
            [JsonProperty("@context")]
            public List<string> context { get; set; }

            [JsonProperty("@id")]
            public string id { get; set; }
            public List<Edge> edges { get; set; }
            public string version { get; set; }
            public View view { get; set; }
        }

        public class Source
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string activity { get; set; }
            public string contributor { get; set; }
            public string process { get; set; }
        }

        public class Start
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string label { get; set; }
            public string language { get; set; }
            public string term { get; set; }
            public string sense_label { get; set; }
        }

        public class View
        {
            [JsonProperty("@id")]
            public string id { get; set; }

            [JsonProperty("@type")]
            public string type { get; set; }
            public string comment { get; set; }
            public string firstPage { get; set; }
            public string nextPage { get; set; }
            public string paginatedProperty { get; set; }
        }


        public async void GetFreeDictionaryAPIData(string text)
        {
            var url = @"https://api.dictionaryapi.dev/api/v2/entries/en/" + text;
            var myClient = new HttpClient();
            var responseURL = await myClient.GetAsync(url);
            var myJsonResponse = await responseURL.Content.ReadAsStringAsync();
            List<Root1> myDeserializedClass = JsonConvert.DeserializeObject<List<Root1>>(myJsonResponse);
            Output = "";
            foreach (var entry in myDeserializedClass)
            {
                foreach (var meaning in entry.meanings)
                {
                    foreach (var definition in meaning.definitions)
                    {
                        Output += meaning.partOfSpeech + ":  " + definition.definition + "\n";
                    }
                }
            }
            return;

        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class Definition
        {
            public string definition { get; set; }
            public List<string> synonyms { get; set; }
            public List<object> antonyms { get; set; }
            public string example { get; set; }
        }

        public class License
        {
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Meaning
        {
            public string partOfSpeech { get; set; }
            public List<Definition> definitions { get; set; }
            public List<string> synonyms { get; set; }
            public List<object> antonyms { get; set; }
        }

        public class Phonetic
        {
            public string text { get; set; }
            public string audio { get; set; }
            public string sourceUrl { get; set; }
            public License license { get; set; }
        }

        public class Root1
        {
            public string word { get; set; }
            public string phonetic { get; set; }
            public List<Phonetic> phonetics { get; set; }
            public List<Meaning> meanings { get; set; }
            public License license { get; set; }
            public List<string> sourceUrls { get; set; }
        }


        public async void GetWebstersDictionaryAPIData(string text)
        {
            var url = @"https://dictionaryapi.com/api/v3/references/sd2/json/" + text + "?key=a22c5742-ad8e-44b4-b4c4-9f3f9ac7aedb";
            var myClient = new HttpClient();
            var responseURL = await myClient.GetAsync(url);
            var myJsonResponse = await responseURL.Content.ReadAsStringAsync();
            try
            {
                List<Root2> myDeserializedClass = JsonConvert.DeserializeObject<List<Root2>>(myJsonResponse);
                Output = "";
                foreach (var entry in myDeserializedClass)
                {
                    //foreach (var definition in entry.def)
                    //{
                    //    Output += entry.fl + ":  " + definition+ "\n";
                    //}
                    foreach (var definition in entry.shortdef)
                    {
                        Output += entry.fl + ":  " + definition + "\n";
                    }
                }
            }
            catch
            {
                Output = "Similar\n";
                var s = JsonConvert.DeserializeObject<List<string>>(myJsonResponse);
                foreach (var s1 in s)
                    Output += s1 + "\n";
            }
            return;
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class Def
        {
            public List<List<List<object>>> sseq { get; set; }
        }

        public class History
        {
            public string pl { get; set; }
            public List<List<string>> pt { get; set; }
        }

        public class Hwi
        {
            public string hw { get; set; }
            public List<Pr> prs { get; set; }
        }

        public class In
        {
            public string @if { get; set; }
        }

        public class Meta
        {
            public string id { get; set; }
            public string uuid { get; set; }
            public string sort { get; set; }
            public string src { get; set; }
            public string section { get; set; }
            public List<string> stems { get; set; }
            public bool offensive { get; set; }
        }

        public class Pr
        {
            public string mw { get; set; }
            public Sound sound { get; set; }
        }

        public class Root2
        {
            public Meta meta { get; set; }
            public int hom { get; set; }
            public Hwi hwi { get; set; }
            public string fl { get; set; }
            public List<Def> def { get; set; }
            public History history { get; set; }
            public List<string> shortdef { get; set; }
            public List<In> ins { get; set; }
        }

        public class Sound
        {
            public string audio { get; set; }
        }


    }
}