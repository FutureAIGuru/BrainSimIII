//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Emgu.CV.ImgHash;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleSpeechInPlus : ModuleBase
    {
        [XmlIgnore]
        public string FilePath = "Resources\\KeywordTables\\";
        [XmlIgnore]
        public string FileSuffix = "Keyword.table";
        [XmlIgnore]
        public PushAudioInputStream pushStream;
        KeywordRecognitionModel keywordModel;
        [XmlIgnore]
        private SpeechRecognizer recognizer;
        public bool speechEnabled;
        public bool remoteMicEnabled;
        [XmlIgnore]
        public string text2dialog = "";
        [XmlIgnore]
        public String wakeWord = "";
        [XmlIgnore]
        private int waitTimer = 0;
        [XmlIgnore]
        public bool keywordRecognition = true;
        [XmlIgnore]
        public bool speechRecognitionPaused = true;
        [XmlIgnore]
        public int run;

        DateTime pauseStartTime = DateTime.Now;
        TimeSpan pauseReqestedTime = TimeSpan.Zero;

        public ModuleSpeechInPlus()
        {
            minHeight = 1;
            maxHeight = 10;
            minWidth = 1;
            maxWidth = 10;
        }

        //keeps the temporary phrase so it can be recognized across multiple engine cycles
        private List<string> words = new List<string>();
        public void CancelCommandQueue()
        {
            words.Clear();
        }

        DateTime pauseEndTime = DateTime.Now;
        public override void Fire()
        {
            Init();

            GetUKS();
            if (UKS == null) return;
            if (DateTime.Now < pauseEndTime) return;
            
            //block input if the nlp is still busy.
            Thing incomingPhrase = UKS.GetOrAddThing("CurrentNLPPhrase", "Attention");
            if (incomingPhrase != null && incomingPhrase.V != null) return;

            if (linesFromFile.Count > 0 && run > 0)
            {
                string theLine = linesFromFile[0];
                text2dialog = theLine;
                if (theLine.StartsWith("pause"))
                {
                    if (int.TryParse(theLine.Substring(5), out int secs))
                    {
                        pauseEndTime = DateTime.Now + TimeSpan.FromSeconds(secs);
                    }
                    text2dialog = theLine;
                }
                else
                {
                    ReceiveInputFromText(theLine);
                }
                if (dlg != null && dlg is ModuleSpeechInPlusDlg dlg1)
                    dlg1.AddToHistory(theLine);

                linesFromFile.RemoveAt(0);
                if (run == 1) run = 0;
                if (linesFromFile.Count == 0) run = 0;
            }

            Thing attn = UKS.GetOrAddThing("Attention", "Thing");
            if (attn == null)
                return;

            ModuleWordSequencing moduleWordSequencing = (ModuleWordSequencing)FindModule(typeof(ModuleWordSequencing));

            if (DateTime.Now < pauseStartTime + pauseReqestedTime)
            {
                ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
                if (mpi != null)
                    if (mpi.IsPodBusy()) pauseStartTime = DateTime.Now;

                return;
            }
            //if a word is in the input queue...process one word
            if (words.Count > 0)
            {
                string word = words[0].ToLower();

                if (word == "pause")
                {
                    words.RemoveAt(0);
                    word = words[0].ToLower();
                    words.RemoveAt(0); // deleted integer
                    words.RemoveAt(0); // deletes endofphrase

                    int.TryParse(word, out waitTimer);

                    pauseStartTime = DateTime.Now;
                    pauseReqestedTime = TimeSpan.FromSeconds(waitTimer);
                    return;
                }

                words.RemoveAt(0);
                // store word into UKS
                if (word == "")
                {
                    UpdateDialog();
                    return;
                }
                string label = "w" + char.ToUpper(word[0]) + word.Substring(1);
                Thing w = UKS.GetOrAddThing(label, UKS.Labeled("Word"), word);
                Thing currentWord = UKS.GetOrAddThing("CurrentWord", attn);
                currentWord.RemoveRelationshipAt(0);
                currentWord.AddRelationship(w);

                if (moduleWordSequencing != null && w.Label != "wEndofphrase")
                    moduleWordSequencing.inputWords.Add(w);
            }
            else
            {
                // no new words coming in, ensuring current word is cleared
                Thing currentWord = UKS.GetOrAddThing("CurrentWord", attn);
                currentWord.RemoveRelationshipAt(0);

                if (moduleWordSequencing != null)
                    moduleWordSequencing.AddSequences();
            }
            UpdateDialog();
        }

        public string GetInputText()
        {
            return ((ModuleSpeechInPlusDlg)dlg).GetEntryText();
        }

        private void StartRecognizer()
        {
            try
            {
                string SubscriptionKey = "d9439a3d1452434db04bebe83626aa2e";
                string ServiceRegion = "westus";

                var speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, ServiceRegion);
                speechConfig.SpeechRecognitionLanguage = "en-US";

                GetUKS();
                wakeWord = UKS.Labeled("WakeWord")?.V?.ToString();

                if (String.IsNullOrEmpty(wakeWord))
                {
                    wakeWord = "Sallie";
                    SetWakeWord(wakeWord);
                }
                keywordModel = KeywordRecognitionModel.FromFile(FilePath + wakeWord + FileSuffix);

                pushStream = AudioInputStream.CreatePushStream();
                AudioConfig audioConfig;
                if (remoteMicEnabled) audioConfig = AudioConfig.FromStreamInput(pushStream);
                else audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                if (recognizer != null)
                    recognizer.Dispose();
                recognizer = new SpeechRecognizer(speechConfig, audioConfig);

                recognizer.Recognized += (s, e) =>
                {
                    Recognizer_SpeechRecognized(e);
                };

                if (speechEnabled)
                {
                    if (keywordRecognition) recognizer.StartKeywordRecognitionAsync(keywordModel);
                    else recognizer.StartContinuousRecognitionAsync();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void StartContinuousRecognition()
        {
            PauseRecognition();
            keywordRecognition = false;
            ResumeRecognition();
        }

        // Handle the SpeechRecognized event.  
        //WARNING: this could be asynchronous to everything else
        void Recognizer_SpeechRecognized(SpeechRecognitionEventArgs r)
        {
            ModulePodLEDControl plc = (ModulePodLEDControl)FindModule(typeof(ModulePodLEDControl));
            if (r.Result.Reason == ResultReason.RecognizedKeyword)
            {
                if (plc != null)
                    plc.SetLED(255, 255, 0);
                return;
            }
            if (r.Result.Duration >= TimeSpan.FromSeconds(10))
            {
                if (plc != null)
                    plc.SetLED(0, 255, 0);
                return;
            }
            string text = r.Result.Text;

            text = MakeReplacements(text);
            parseInput(text);

            if (!keywordRecognition && text != "")
            {
                PauseRecognition();
                keywordRecognition = true;
                ResumeRecognition();
            }
            text2dialog = text;

            ModuleUserInterface moduleUserInterface = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
            if (moduleUserInterface != null)
                moduleUserInterface.DrawSpeechIn(text);
            plc?.SetLED(0, 255, 0);
        }

        private void parseInput(string text)
        {
            string DeleteAtBeginning(string s, string target)
            {
                if (s.ToLower().StartsWith(target))
                    s = s.Substring(target.Length).Trim();
                return s;
            }
            //text = MakeReplacements(text); //this is now on the NLP side
            Debug.WriteLine("Words Detected: " + text);
            text = DeleteAtBeginning(text, wakeWord.ToLower());

            Thing incomingPhrase = UKS.GetOrAddThing("CurrentNLPPhrase", "Attention");
            incomingPhrase.V = text;
        }

        private string MakeReplacements(string input)
        {
            string desiredText = "";
            int desiredTextPos = input.IndexOf("(");
            if (desiredTextPos != -1)
            {
                desiredText = input.Substring(desiredTextPos);
                input = input.Substring(0, desiredTextPos).Trim();
            }
            string ret = input.ToLower();
            Thing ReplacementBase = UKS.Labeled("ReplacementPhrase");
            if (ReplacementBase != null)
            {
                foreach (Thing t in ReplacementBase.Children)
                {
                    ValueTuple<string, string> values = (ValueTuple<string, string>)t.V;
                    string pattern = "(\\b)" + values.Item1 + "(\\b)";
                    ret = Regex.Replace(ret, pattern, values.Item2);
                }
            }
            ret += " " + desiredText;
            return ret;
        }

        internal void SetWakeWord(string name)
        {
            GetUKS();
            wakeWord = name;
            Thing t = UKS.GetOrAddThing("WakeWord", UKS.Labeled("Self"));
            t.V = name;
        }

        List<string> linesFromFile = new();
        public void SetLinesFromFile(string[] lines)
        {
            linesFromFile.Clear();
            foreach (string line in lines)
                linesFromFile.Add(line);
        }

        public void ReceiveInputFromText(string phrase)
        {
            if (phrase.StartsWith("//")) return;

            ModuleUserInterface moduleUserInterface = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
            if (moduleUserInterface != null)
                moduleUserInterface.DrawSpeechIn(phrase);

            string firstWord = Regex.Replace(phrase.Split(' ')[0], "[\\W]*", String.Empty).ToLower();
            if (keywordRecognition && !(firstWord.Equals(wakeWord.ToLower()))) phrase = wakeWord + " " + phrase;
            parseInput(phrase);
        }

        public void PauseRecognition()
        {
            if (recognizer != null)
            {
                if (keywordRecognition)
                {
                    recognizer.StopKeywordRecognitionAsync();
                    //Debug.WriteLine("Keyword Recognition paused.");
                }
                else
                {
                    recognizer.StopContinuousRecognitionAsync();
                    //Debug.WriteLine("Open Speech Recognition paused.");
                }
                speechRecognitionPaused = true;
            }
        }

        public void ResumeRecognition()
        {
            StartRecognizer();
            if (keywordRecognition) Debug.WriteLine("Keyword Recognition resumed.");
            else Debug.WriteLine("Open Speech Recognition resumed.");
            speechRecognitionPaused = false;
        }

        public List<string> GetWakeWords()
        {
            return ((ModuleSpeechInPlusDlg)dlg).GetCBWakeWords();
        }

        public override void SetUpAfterLoad()
        {
            base.SetUpAfterLoad();
            Init();
            Initialize();
            if (!speechEnabled) PauseRecognition();
        }

        public override void Initialize()
        {
            // ClearNeurons();

            //if (recognizer != null)
            //{
            //    recognizer.StopKeywordRecognitionAsync();
            //    recognizer.Dispose();
            //}

            StartRecognizer();
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            Thing AttentionRoot = UKS.GetOrAddThing("Attention", "Thing");
            UKS.GetOrAddThing("CurrentWord", AttentionRoot);
            Thing SelfRoot = UKS.GetOrAddThing("Self", "Sense");
            UKS.GetOrAddThing("WakeWord", SelfRoot, wakeWord);

            Thing ReplacementRoot = UKS.Labeled("ReplacementPhrase");
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("one", "1"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("two", "2"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("too", "2"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("three", "3"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("science fear", "cyan sphere"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("science fair", "cyan sphere"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("sadly", "Sallie"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("sally", "Sallie"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("sallie's", "Sallie"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("named", "name"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("told", "tell"));
            //UKS.GetOrAddThing("replacement*", ReplacementRoot, ("people", "person"));

            if (dlg != null)
            {
                ((ModuleSpeechInPlusDlg)dlg).Set_cbWakeWords();
            }

            MainWindow.ResumeEngine();
        }
    }
}
