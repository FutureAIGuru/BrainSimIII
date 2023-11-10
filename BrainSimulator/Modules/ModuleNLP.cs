//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Pluralize.NET;

namespace BrainSimulator.Modules
{
    public partial class ModuleNLP : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;
        public class NLPItem
        {
            public int index;
            public string text;
            public string lemma;
            public string dependency;
            public string partOfSpeech;
            public string tag;
            public int head;
            public string morph;

            public string ToString(string param)
            {
                string text1 = text;
                string lemma1 = lemma;
                string dependency1 = dependency;

                if (text1.Length < 9) text1 += "              ";
                if (lemma1.Length < 9) lemma1 += "              ";
                if (dependency1.Length < 9) dependency1 += "              ";

                return index + param + text1 + param + lemma1 + param + dependency1 + param + partOfSpeech + param + tag + param + head + param + morph;
            }
            public override string ToString()
            {
                return lemma + " " + dependency + " " + partOfSpeech + " " + tag;
            }
            public NLPItem() { }
            public NLPItem(NLPItem item)
            {
                index = item.index;
                text = item.text;
                lemma = item.lemma;
                dependency = item.dependency;
                partOfSpeech = item.partOfSpeech;
                tag = item.tag;
                head = item.head;
                morph = item.morph;
            }
        }

        private ProcessStartInfo start;
        Process process = new();
        [XmlIgnore]
        public List<NLPItem> nlpResults = new();
        bool nlpComplete = false;
        [XmlIgnore]
        public Relationship r;
        [XmlIgnore]
        public string Phrase; //so the dialog can show the phrase
        int currentNLPLine;
        int relPos = -1;
        int srcPos = -1;
        int targPos = -1;
        [XmlIgnore]
        public bool nlpOnly = false;

        const float weightThreshold = 0.7f;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleNLP()
        {
            minHeight = 1;
            maxHeight = 10;
            minWidth = 1;
            maxWidth = 10;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine

        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (pendingInput.Count > 0)
            {
                QueueItem queueItem = pendingInput.Dequeue();
                HandleDeclaration(queueItem.items, queueItem.phrase, queueItem.desiredResult);
                return;
            }
            //check for phrase in the attention object
            Thing incomingPhrase = UKS.GetOrAddThing("CurrentNLPPhrase", "Attention");

            if (incomingPhrase != null && incomingPhrase.V is string phrase)
            {
                incomingPhrase.V = null;
                ParsePhrase(phrase);
            }
        }

        // This parses a phrase, first part of this is hack for movement commands.
        // Afterwards turning the phrase, into a parameterized phrase.
        public void ParsePhrase(String phrase)
        {
            GetUKS();

            //strip out parenthetical required test output
            string desiredResult = RemovedDesiredStringFromPhrase(ref phrase);
            bool isDefinition = false;
            if (phrase.ToLower().StartsWith("def:"))
            {
                isDefinition = true;
                phrase = phrase.Substring(5);
                if (phrase.StartsWith("yes, "))
                    phrase = phrase.Replace("yes, ", "");
            }
            if (phrase == "") return;

            Phrase = phrase;

            // Getting NLP results. 
            nlpResults.Clear();
            nlpComplete = false;
            if (!(phrase.EndsWith(".") || phrase.EndsWith("!") || phrase.EndsWith("?")))
            {
                phrase += ".";
            }
            process.StandardInput.WriteLine(phrase);

            while (!nlpComplete)
            {
                if (process.HasExited)
                {
                    Debug.WriteLine("Something went wrong in Python Land. Unable to process text.");
                    return;
                }
            }
            UpdateDialog();

            ChangeNumbersToDigits(nlpResults);

            if (HandleHardCodedPhrases(phrase, desiredResult))
                return;

            relPos = -1;

            HandleKnownParseErrors(nlpResults);

            //clone the list
            List<NLPItem> nlpItems = new List<NLPItem>();
            foreach (NLPItem item in nlpResults)
                nlpItems.Add(new NLPItem(item));

            if (nlpOnly)
            {
                HandlePronouns(nlpResults);
                return;
            }
            HandlePronouns(nlpItems);

            if (phrase.StartsWith("tell me about") || phrase.StartsWith("describe"))
            {
                HandlePossessives(nlpItems, true);
                HandleCompounds(nlpItems);
                HandleTellMeAbout(nlpItems, desiredResult);
            }
            else if (isDefinition)
            {
                HandlePossessives(nlpItems);
                HandleCompounds(nlpItems);
                HandleDefinition(nlpItems, phrase);
            }
            else if (nlpResults.Count > 0 &&
            (nlpResults.Last().text == "?" ||
            nlpResults[0].lemma == "who" ||
            nlpResults[0].lemma == "what" ||
            nlpResults[0].lemma == "how" ||
            nlpResults[0].lemma == "when" ||
            nlpResults[0].lemma == "why" ||
            nlpResults[0].lemma == "where" ||
            nlpResults[0].lemma == "do"
            ))
            {
                HandlePossessives(nlpItems, true);
                HandleCompounds(nlpItems);
                HandleQuery(nlpItems, desiredResult);
            }
            else
            {
                HandlePossessives(nlpItems);
                HandleCompounds(nlpItems);
                HandleDeclaration(nlpItems, phrase, desiredResult);
            }
        }

        public class QueueItem
        {
            public List<NLPItem> items;
            public string phrase;
            public string desiredResult;
        }
        Queue<QueueItem> pendingInput = new();

        int BeginningOfClause(List<NLPItem> items, int index)
        {
            int retVal = 0;
            retVal = items.FindIndex(x => x.head == index);
            if (retVal == -1) retVal = index;
            return retVal;
        }
        private void HandleDefinition(List<NLPItem> nlpItemsIn, string phrase, string desiredResult = "")
        {
            if (pendingInput.Count > 0) pendingInput = new();
            List<NLPItem> nlpItems = CloneNLPResults(nlpItemsIn);
            List<NLPItem> remainder = CloneNLPResults(nlpItemsIn);
            List<(int, int)> rangeList = new();
            List<NLPItem> subjItems = nlpItems.FindAll(x => x.dependency == "nsubj");
            if (subjItems.Count > 1 &&
                (nlpItems[subjItems[1].index - 1].lemma == "and" || nlpItems[subjItems[1].index - 1].lemma == ","))
            {
                rangeList.Add((0, BeginningOfClause(remainder, subjItems[1].index) - 1));
                rangeList.Add((BeginningOfClause(remainder, subjItems[1].index), nlpItems.Count - 1));
                RemoveRange(remainder, rangeList[1]);
                pendingInput.Enqueue(new() { desiredResult = desiredResult, items = remainder, phrase = phrase });
                remainder = CloneNLPResults(nlpItemsIn);
                RemoveRange(remainder, rangeList[0]);
                pendingInput.Enqueue(new() { desiredResult = desiredResult, items = remainder, phrase = phrase });
                return;
            }


            NLPItem withItem = nlpItems.FindFirst(x => x.lemma == "with" || x.lemma == "that");
            NLPItem subjItem = nlpItems.FindFirst(x => x.dependency == "nsubj");
            if (subjItem == null) return;
            rangeList.Add((0, subjItem.index));
            if (withItem != null)
            {
                rangeList.Add((subjItem.index + 1, withItem.index - 1));
                rangeList.Add((withItem.index, nlpItems.Count - 1));
                RemoveRange(remainder, rangeList[2]);
            }
            //prevent or lists in the target of the definition
            if (remainder.FindFirst(x => x.lemma == "or") == null)
                pendingInput.Enqueue(new() { desiredResult = desiredResult, items = remainder, phrase = phrase });
            //                HandleDeclaration(remainder, phrase, "");

            if (withItem != null)
            {
                //a which or that clause
                remainder = CloneNLPResults(nlpItemsIn);
                if (withItem.lemma == "with")
                {
                    //with
                    remainder[withItem.index].lemma = "have";
                    remainder[withItem.index].partOfSpeech = "VERB";
                    RemoveRange(remainder, rangeList[1]);
                }
                else
                {
                    //that
                    if (rangeList[1].Item2 < remainder.Count - 1)
                        rangeList[1] = (rangeList[1].Item1, rangeList[1].Item2 + 1);
                    RemoveRange(remainder, rangeList[1]);
                    //add implied "can" if actions ae specified
                    if (remainder.FindFirst(x => x.lemma == "can") == null)
                    {
                        var item = remainder.FindFirst(x => x.partOfSpeech == "VERB");
                        if (item != null)
                            ResultsInsertAt(remainder, item.index, new NLPItem { text = "can", lemma = "can", dependency = "aux", partOfSpeech = "AUX", head = item.index + 1 });
                    }
                }
                pendingInput.Enqueue(new() { desiredResult = desiredResult, items = remainder, phrase = phrase });
            }
        }


        //PROPERTY LIST/////////////////////////////////////////////////
        private void RemoveDeterminants(List<NLPItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].dependency == "det")
                    ResultsRemoveAt(items, i);
            }
        }

        private static List<NLPItem> CloneNLPResults(List<NLPItem> nlpResultsIn)
        {
            List<NLPItem> nlpResults = new();  //cloned list so we can restart in the debugger
            foreach (NLPItem item in nlpResultsIn)
                nlpResults.Add(new NLPItem(item));
            return nlpResults;
        }

        Thing GetNonInstanceAncestor(Thing t)
        {
            if (!Char.IsDigit(t.Label[t.Label.Length - 1])) return t;
            while (Char.IsDigit(t.Label[t.Label.Length - 1]))
                t = t.Parents[0];
            return t;
        }
        string AddArticleIfNeeded(string s)
        {
            IPluralize pluralizer = new Pluralizer();
            if (!pluralizer.IsPlural(s))
                s = "a " + s;
            return s;
        }
        string AddArticleIfNeeded(Thing item)
        {
            string retVal = "";
            if (item != null)
                if (item.Label == "weather")
                    retVal = "the " + item.Label;
                else
                    retVal = "a " + item.Label;
            return retVal;
        }
        string AddArticleIfNeeded(NLPItem item)
        {
            string input = item.lemma;
            string retVal = input;
            if (item.partOfSpeech == "PRON")
                return retVal;


            if (item.morph.Contains("Sing") && item.partOfSpeech != "PROPN")
            {
                if (item.lemma == "weather")
                    retVal = "the " + retVal;
                else
                    retVal = "a " + retVal;
            }
            if (item.morph.Contains("Plur"))
                retVal = item.text;
            return retVal;
        }

        string RemovedDesiredStringFromPhrase(ref string phrase)
        {
            string testOutput = "";
            int start = phrase.IndexOf("(");
            int end = phrase.LastIndexOf(")");
            if (start != -1 && end != -1)
            {
                testOutput = phrase.Substring(start + 1, end - start - 1);
                phrase = phrase.Remove(start, end - start + 1);
            }
            //remove internal extra spaces
            phrase = Regex.Replace(phrase.Trim(), @"\s+", " ");
            return testOutput;
        }


        private static string Pluralize(string targ, int count)
        {
            if (count > 1 && targ != null)
            {
                //this library only works with nouns
                IPluralize pluralizer = new Pluralizer();
                targ = pluralizer.Pluralize(targ);
            }
            return targ;
        }

        bool CompareOutputToDesired(string responseString, string desiredResult)
        {
            //strip punctuation out of the response before testing for equality
            responseString = new string(responseString.Where(c => !char.IsPunctuation(c)).ToArray());
            responseString = responseString.Trim();
            responseString = responseString.Replace("  ", " ");
            desiredResult = new string(desiredResult.Where(c => !char.IsPunctuation(c)).ToArray());
            desiredResult = desiredResult.Trim();
            desiredResult = desiredResult.Replace("  ", " ");
            if (responseString.ToLower() != desiredResult.ToLower() && desiredResult != "")
            {
                Debug.WriteLine($"Mismatch RESULT: {responseString} \n        DESIRED: {desiredResult}");
                return false;
            }
            return true;
        }


        private void HandleKnownParseErrors(List<NLPItem> nlpResults)
        {
            foreach (NLPItem item in nlpResults)
            {
                if (item.lemma == "people")
                    item.lemma = "person";
                if (item.text == "says")
                { item.partOfSpeech = "VERB"; item.dependency = "ROOT"; item.lemma = "say"; item.morph = ""; }
                if (item.lemma == "I") item.lemma = "i";
                if (item.text == "charles")
                { item.partOfSpeech = "PROPN"; item.lemma = "charles"; item.morph = ""; }
                if (item.lemma == "bobby")
                    item.partOfSpeech = "PROPN";
                if (item.lemma == "buddy")
                    item.partOfSpeech = "PROPN";
                if (item.lemma == "kitty")
                    item.partOfSpeech = "PROPN";
                if (item.lemma == "billy" && item.dependency == "pcomp")
                { item.partOfSpeech = "PROPN"; item.dependency = "pobj"; }
                if (item.lemma == "fido")
                    item.partOfSpeech = "PROPN";
                if (item.lemma == "suzie")
                    item.partOfSpeech = "PROPN";
                if (item.lemma == "masculine")
                    item.dependency = "acomp";
                if (item.lemma == "sing")
                    item.partOfSpeech = "VERB";
                if (item.lemma == "dance")
                    item.partOfSpeech = "VERB";
                if (Utils.ColorFromName(item.lemma) != Utils.ColorFromName(null))
                { item.dependency = "adj"; item.partOfSpeech = "ADJ"; }
                if (item.lemma == "a" && item.index > 0 && (nlpResults[item.index - 1].partOfSpeech == "DET" || nlpResults[item.index - 1].partOfSpeech == "ADJ"))
                { item.partOfSpeech = "NOUN"; item.dependency = "dobj"; }
            }
        }


        void HandlePossessives(List<NLPItem> nlpResults, bool isQuery = false)
        {
            for (int i = 0; i < nlpResults.Count; i++)
            {
                NLPItem item = nlpResults[i];
                if (item.tag == "POS" && i != 0)
                {
                    //sometimes parses with 2 posessive markers "'s"  OR "/" "s"
                    if (i < nlpResults.Count - 1 && nlpResults[i + 1].tag == "POS")
                        ResultsRemoveAt(nlpResults, i + 1);
                    NLPItem source = nlpResults[i - 1];
                    NLPItem target = nlpResults[source.head];
                    NLPItem newItem = new NLPItem()
                    {
                        index = i,
                        text = item.text,
                        lemma = "have",
                        dependency = "ccomp",
                        partOfSpeech = "VERB",
                        tag = "VBZ",
                    };
                    if (!isQuery)
                    {
                        List<NLPItem> possessiveClause = CloneNLPResults(nlpResults);
                        //add the statement about ownership
                        possessiveClause[i] = newItem;
                        RemoveRange(possessiveClause, (target.index + 1, nlpResults.Count - 1));
                        RemoveRange(possessiveClause, (0, source.index - 1));
                        HandleDeclaration(possessiveClause);
                    }

                    newItem = new NLPItem()
                    {
                        index = i,
                        text = item.text,
                        lemma = "have",
                        dependency = "ccomp",
                        partOfSpeech = "VERB",
                        tag = "VBZ",
                    };
                    //you still need the ownership to know which specific instance you are referring to
                    ResultsRemoveAt(nlpResults, i);
                    ResultsInsertAt(nlpResults, i, newItem);
                    //ResultsRemoveAt(nlpResults, i - 1);

                }
            }
        }
        //TODO add rest of cases
        private void HandlePronouns(List<NLPItem> nlpItems)
        {
            string[] persons = { "i", "you", "he", "she", "it", "we", "you", "they" };
            NLPItem subject = nlpItems.FindFirst(x => x.dependency.Contains("subj"));

            for (int i = 0; i < nlpItems.Count; i++)
            {
                NLPItem item = nlpItems[i];
                if (item.partOfSpeech != "PRON") continue;
                if (item.morph.Contains("Person=1"))
                    item.lemma = "i";
                if (item.morph.Contains("Person=2"))
                    item.lemma = "you";
                if (item.morph.Contains("Person=3") & subject != null)
                {
                    item.lemma = subject.lemma;
                    item.partOfSpeech = subject.partOfSpeech;
                }
                if (item.morph.Contains("Poss=Yes"))
                {
                    //possessives are handled by inserting a ' line after the pronour so it works like mary's
                    NLPItem newItem = new NLPItem()
                    {
                        index = i + 1,
                        text = "'s",
                        lemma = "'s",
                        dependency = "case",
                        partOfSpeech = "PART",
                        tag = "POS",
                        head = i,
                    };
                    ResultsInsertAt(nlpItems, i + 1, newItem);
                }
            }
        }
        //Query about "you" should return "i" etc.
        NLPItem SwapPronounPerson(NLPItem s)
        {
            if (s is null) return null;
            if (s.morph.Contains("Person=2"))
            {
                s.lemma = "i";
                s.morph.Replace("Person=2", "Person=1");
            }
            else
            if (s.morph.Contains("Person=1"))
            {
                s.lemma = "you";
                s.morph.Replace("Person=1", "Person=2");
            }
            return s;
        }

        string CorrectOutputString(string s)
        {
            s = s.Replace(" i is", " you are");
            s = s.Replace("you is", "i am");
            return s;
        }
        string SingularizeVerb(string s)
        {
            s = s.Replace("have", "has");
            s = s.Replace("be", "is");
            return s;
        }

        string ConjugateRootVerb(string root, NLPItem source = null)
        {
            if (source != null && source.morph.Contains("Person=1"))
                switch (root)
                {
                    case "have": return "have";
                    case "be": return "am";
                }
            else if (source != null && source.morph.Contains("Person=2"))
                switch (root)
                {
                    case "have": return "have";
                    case "be": return "are";
                }
            else if (source != null && source.morph.Contains("Plur"))
                switch (root)
                {
                    case "have": return "have";
                    case "be": return "are";
                }
            else
                switch (root)
                {
                    case "have": return "has";
                    case "be": return "is";
                    case "say": return "says";
                }
            return root;
        }
        List<Thing> ThingProperties(Thing t)
        {
            List<Thing> retVal = new();
            if (t == null) return retVal;
            foreach (Relationship r in t.RelationshipsWithoutChildren)
            {
                if (r.relType?.Label == "is" && r.weight > weightThreshold)
                    retVal.Add(r.T);
            }
            return retVal;

        }

        void OutputReponseString(string response)
        {
            if (response == null || response == "") return;
            if (MainWindow.engineIsPaused)
                System.Windows.MessageBox.Show("Engine must be running for query result");
            MainWindow.SuspendEngine();
            Thing currentResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            foreach (string s in response.Split(" "))
            {
                string wordLabel = s.Trim();
                if (wordLabel == "") continue;
                wordLabel = "w" + char.ToUpper(wordLabel[0]) + wordLabel.Substring(1);
                Thing word = UKS.GetOrAddThing(wordLabel, "Word", s);
                currentResponse.AddRelationship(word);
            }
            MainWindow.ResumeEngine();
        }

        void HandleCompounds(List<NLPItem> nlpResults)
        {
            for (int i = 0; i < nlpResults.Count; i++)
            {
                NLPItem si = nlpResults[i];
                if (si.dependency == "compound")
                {
                    NLPItem siTarget = nlpResults[si.head];
                    //if (siTarget.lemma.Contains("ball") || siTarget.lemma == "s.") continue;//hack to make textured balls work
                    si.lemma += " " + siTarget.lemma;
                    si.morph = siTarget.morph;
                    si.dependency = siTarget.dependency;
                    si.partOfSpeech = siTarget.partOfSpeech;
                    if (i < nlpResults.Count - 1 && nlpResults[i + 1].lemma == "-")
                        ResultsRemoveAt(nlpResults, i + 1);
                    ResultsRemoveAt(nlpResults, siTarget.index);
                }
                if (si.lemma == "-" && si.index < nlpResults.Count - 1 && si.index > 0)
                {
                    nlpResults[i - 1].lemma += " " + nlpResults[i + 1].lemma;
                    ResultsRemoveAt(nlpResults, i);
                    ResultsRemoveAt(nlpResults, i);
                    i -= 2;
                }
                if (si.partOfSpeech == "VERB" && nlpResults[si.head].partOfSpeech == "NOUN" &&
                    si.morph.Contains("Form=Part"))
                {
                    nlpResults[si.head].lemma = si.text + " " + nlpResults[si.head].lemma;
                    ResultsRemoveAt(nlpResults, i);
                    i--;
                }
            }
        }
        //removes a line from the nlpResults list and keeps all the pointers correct
        private void ResultsRemoveAt(List<NLPItem> nlpResults, int index)
        {
            nlpResults.RemoveAt(index);
            foreach (NLPItem si in nlpResults)
            {
                if (si.index >= index) si.index--;
                if (si.head > index) si.head--;
            }
        }
        //inserts a line from the nlpResults list and keeps all the pointers correct
        private void ResultsInsertAt(List<NLPItem> nlpResults, int index, NLPItem newItem)
        {
            foreach (NLPItem si in nlpResults)
            {
                if (si.index >= index) si.index++;
                if (si.head >= index) si.head++;
            }
            nlpResults.Insert(index, newItem);
        }

        private void FindClauses(List<NLPItem> nlpResults, int start, int end, AppliesTo source, List<ClauseType> clauses)
        {
            List<string> properties = new();
            for (int i = start + 1; i < end; i++)
            {
                if (nlpResults[i].dependency == "prep" || nlpResults[i].dependency == "cc" ||
                    (source == AppliesTo.source && nlpResults[i].dependency == "nsubj"))
                {
                    for (int j = i; j < end; j++)
                    {
                        if (IsAdjective(nlpResults[j]))
                            properties.Add(nlpResults[j].lemma);
                        if (IsNoun(nlpResults[j]))
                        {
                            clauses.Add(new ClauseType { a = source, clause = UKS.AddStatement("", nlpResults[i].lemma, nlpResults[j].lemma, null, null, properties) });
                            i = j;
                            properties.Clear();
                            break;
                        }
                    }
                }
            }
        }
        bool IsNoun(NLPItem si)
        {
            if (si == null) return false;
            if (si.dependency == "nummod") return false;
            if (si.dependency.StartsWith('n')) return true;
            if (si.partOfSpeech == "NOUN") return true;
            if (si.partOfSpeech == "PROPN") return true;
            if (si.tag.StartsWith('W')) return true;
            if (si.dependency == "conj" && si.partOfSpeech == "ADJ") return true;
            return false;
        }
        bool IsAdjective(NLPItem si)
        {
            if (si == null) return false;
            if (si.partOfSpeech == "ADJ") return true;
            return false;
        }


        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine("ModuleSpeechParser - NLP Process Error: " + e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Debug.WriteLine(e.Data);
            if (e.Data == null) return;
            if (e.Data == "Process Complete")
            {
                nlpComplete = true;
                currentNLPLine = 0;
            }
            else
            {
                // "TEXT", "LEMMA", "DEPENDENCY", "PART OF SPEECH", "TAG", "HEAD", "MORPH"
                string[] results = e.Data.Split("\t");
                nlpResults.Add(new NLPItem()
                {
                    index = currentNLPLine++,
                    text = results[0],
                    lemma = results[1],
                    dependency = results[2],
                    partOfSpeech = results[3],
                    tag = results[4],
                    head = int.Parse(results[5]),
                    morph = results[6],
                }); ;
            }
        }

        private static string RemovePunctuation(string phrase)
        {
            var sb = new StringBuilder();
            foreach (char c in phrase)
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            return sb.ToString();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
            string path = Utils.GetOrAddLocalSubFolder("python-3.10.8");
            string cmd = path + @"\nlp_processing.py";

            start = new ProcessStartInfo();
            start.FileName = path + @"\python.exe";
            start.UseShellExecute = false;// Do not use OS shell
            start.CreateNoWindow = true; // We don't need new window
            start.RedirectStandardInput = true;
            start.RedirectStandardOutput = true;// Any output, generated by application will be redirected back
            start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
            start.Arguments = string.Format("\"{0}\" \"{1}\"", cmd, "");
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.StartInfo = start;
            process.Start();
            process.BeginOutputReadLine();

            GetUKS();
        }

        //calculate the difference between two strings
        //this will hopefully be useful in handling ambiguity
        static int LevenshteinDistance(string s, string t)
        {
            int m = s.Length;
            int n = t.Length;
            int[,] d = new int[m + 1, n + 1];

            if (m == 0) return n;
            if (n == 0) return m;

            for (int i = 0; i <= m; i++)
                d[i, 0] = i;

            for (int j = 0; j <= n; j++)
                d[0, j] = j;

            for (int j = 1; j <= n; j++)
            {
                for (int i = 1; i <= m; i++)
                {
                    if (s[i - 1] == t[j - 1])
                        d[i, j] = d[i - 1, j - 1];
                    else
                        d[i, j] = Math.Min(Math.Min(d[i - 1, j], d[i, j - 1]), d[i - 1, j - 1]) + 1;
                }
            }

            return d[m, n];
        }
        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public override void UKSInitializedNotification()
        {
        }

    }
}