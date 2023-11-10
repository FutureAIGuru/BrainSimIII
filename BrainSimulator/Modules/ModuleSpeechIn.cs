//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Recognition;
using System.Text.RegularExpressions;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleSpeechIn : ModuleBase
    {
        SpeechRecognitionEngine recognizer = null;
        public bool speechEnabled;

        public ModuleSpeechIn()
        {
            minHeight = 1;
            maxHeight = 10;
            minWidth = 1;
            maxWidth = 10;
        }

        //keeps the temporary phrase so it can be recognized across multiple engine cycles
        private List<string> words = new List<string>();

        public override void Fire()
        {
            Init();
            if (recognizer == null) return;

            ModuleUKS uks = (ModuleUKS)FindModule(typeof(ModuleUKS));
            if (uks == null) return;
            
            Thing attn = uks.GetOrAddThing("Attention", "Thing");
            if (attn == null) return;

            //if a word is in the input queue...process one word
            if (words.Count > 0)
            {
                string word = words[0].ToLower();

                // store word into UKS

                string label = "w" + char.ToUpper(word[0]) + word.Substring(1);
                Thing w = uks.GetOrAddThing(label, uks.Labeled("Word"), word);
                Thing currentWord = uks.GetOrAddThing("CurrentWord", attn);
                currentWord.RemoveRelationshipAt(0);
                currentWord.AddRelationship(w);

                words.RemoveAt(0);
            }
            else 
            {
                // no new words coming in, ensuring current word is cleared
                Thing currentWord = uks.GetOrAddThing("CurrentWord", attn);
                currentWord.RemoveRelationshipAt(0);
            }
            UpdateDialog();
        }

        private void StartRecognizer()
        {
            // Create an in-process speech recognizer for the en-US locale.  
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            if (recognizer == null)
            {
                MessageBox.Show("Could not open speech recognition engine.");
                return;
            }
            CreateGrammar();

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);

            recognizer.SpeechDetected += Recognizer_SpeechDetected;

            // Configure input to the speech recognizer.  
            try
            {
                recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception e)
            {
                MessageBox.Show("Speech Recognition could not start because: " + e.Message);
                return;
            }
            //// Start asynchronous, continuous speech recognition.  
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void CreateGrammar()
        {
            // Create and load a dictation grammar.  This handles for many words and doesn't work very well
            // recognizer.LoadGrammar(new DictationGrammar());

            // create a small custom grammar for testing
            Choices relations = new("left", "right", "above", "below");
            Choices color = new("blue", "yellow", "cyan", "green", "magenta", "orange");
            Choices shape = new("cube", "sphere", "cone");

            Choices property = new(new GrammarBuilder[] {  shape, color });

            GrammarBuilder whatIsRelation = new("what is");
            whatIsRelation.Append(relations);
            whatIsRelation.Append("of");
            whatIsRelation.Append(property, 1, 2);

            GrammarBuilder howMany = new("how many");
            howMany.Append(property, 1, 2);

            GrammarBuilder isA = new();
            isA.Append(property, 1, 2);
            isA.Append("is a");
            isA.AppendDictation();


            GrammarBuilder goTo = new("go to");
            goTo.Append("the", 0, 1);
            goTo.Append(property, 1, 2);

            GrammarBuilder lookAt = new("look at");
            goTo.Append("the", 0, 1);
            lookAt.Append(property, 1, 2);

            GrammarBuilder thisIs = new("This is");
            thisIs.Append("a", 0, 1);
            thisIs.Append(property, 1, 2);

            Choices query = new Choices("what is behind you", "what is this", "how do you feel", whatIsRelation, howMany);
            Choices declaration = new Choices(isA);
            Choices directive = new Choices("good", goTo, lookAt, "explore");

            Choices allInput = new Choices(query, declaration, directive);
            Choices attentionWord = new Choices("Sallie", "Computer");
            GrammarBuilder attentionInput = new();
            attentionInput.Append(attentionWord, 1, 1);
            attentionInput.Append(allInput);

            Choices stopCommands = new Choices("stop", "no", "don't do that");
            Choices attentionInputWStop = new Choices(attentionInput, stopCommands);

            GrammarBuilder a = new GrammarBuilder();
            a.Append(attentionInputWStop);


            //some words we might need some day
            //Choices article = new Choices("a", "an", "the", "some", "containing", "with", "which are");
            //Choices emotion = new Choices("ecstatic", "happy", "so-so", "OK", "sad", "unhappy");
            //Choices timeOfDay = new Choices("morning", "afternoon", "evening", "night");

            //someday we'll need numbers
            //Choices number = new Choices();
            //for (int i = 1; i < 200; i++)
            //    number.Add(i.ToString());

            //how to add singular/plural to choices
            //PluralizationService ps = PluralizationService.CreateService(new CultureInfo("en-us"));
            //string[] attribList = new string[] { "attributes", "sequences", "colors", "sizes", "shapes", "digits", "things" };
            //string[] attribList1 = new string[attribList.Length];
            //for (int i = 0; i < attribList.Length; i++)
            //    attribList1[i] = ps.Singularize(attribList[i]);


            //how to specify a custom pronunciation with SRGS--these don't integrate with the rest of the grammar
            //SrgsItem cItem = new SrgsItem();
            //SrgsToken cWord = new SrgsToken("computer"); 
            //cWord.Pronunciation = "kəmpjutər";
            //cItem.Add(cWord);
            //SrgsRule srgsRule = new SrgsRule("custom", cItem);
            //SrgsDocument tokenPron = new SrgsDocument(srgsRule);
            //tokenPron.PhoneticAlphabet = SrgsPhoneticAlphabet.Ipa;
            //Grammar g_Custom = new Grammar(tokenPron);
            //recognizer.LoadGrammar(g_Custom);


            //get the words from the grammar and label neurons
            string c = a.DebugShowPhrases;
            c = c.Replace((char)0x2018, ' ');
            c = c.Replace((char)0x2019, ' ');
            string[] c1 = c.Split(new string[] { "[", ",", "]", " " }, StringSplitOptions.RemoveEmptyEntries);
            c1 = c1.Distinct().ToArray();

            //int i1 = 1;
            //na.BeginEnum();
            //for (Neuron n = na.GetNextNeuron(); n != null && i1 < c1.Length; i1++, n = na.GetNextNeuron())
            //    n.Label = c1[i1].ToLower();

            Grammar gr = new Grammar(a);
            recognizer.LoadGrammar(gr);
            //            gr = new Grammar(reward);
            //            recognizer.LoadGrammar(gr);
        }

        // Handle the SpeechRecognized event.  
        //WARNING: this could be asynchronous to everything else
        void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string text = e.Result.Text;

            //get the audio to do something
            /*            RecognizedAudio ra = e.Result.GetAudioForWordRange(e.Result.Words[0], e.Result.Words[e.Result.Words.Count-1]);
                        System.IO.MemoryStream stream = new System.IO.MemoryStream();
                        ra.WriteToAudioStream(stream);
                        stream.Position = 0;
                        byte[] rawBytes = new byte[stream.Length];
                        stream.Read(rawBytes, 0, (int)stream.Length);
                        System.IO.FileStream fStream = new System.IO.FileStream(@"C:\Users\c_sim\Documents\BrainSim\xx.wav",System.IO.FileMode.CreateNew);
                        ra.WriteToAudioStream(fStream);
                        fStream.Close();
            */

            string debug = "";
            foreach (RecognizedWordUnit w in e.Result.Words)
                debug += w.Text + "(" + w.Confidence + ") ";
            bool anyLowConfidence = false;
            Debug.WriteLine("To remove the warning about not being used..." + anyLowConfidence);
            float minConfidence = .91f;
            if (text.IndexOf("Sallie say") == 0)
                minConfidence = .6f;
            foreach (RecognizedWordUnit word in e.Result.Words)
            {
                if (word.Confidence < minConfidence) anyLowConfidence = true;
            }
            //if (e.Result.Confidence < .9 || e.Result.Words[0].Confidence < .92 || anyLowConfidence)
            //{
            //    //System.Media.SystemSounds.Asterisk.Play();
            //    Debug.WriteLine("Words Detected: " + debug + " IGNORED");
            //    return;
            //}
            Debug.WriteLine("Words Detected: " + debug);

            //use this to work with pronunciations instead of words
            //foreach (RecognizedWordUnit word in e.Result.Words)
            //{
            //    words.Add(word.Pronunciation);
            //}
            //return;

            string[] tempWords = text.Split(' ');
            foreach (string word in tempWords)
            {
                string lower = Regex.Replace(word, "[\\W]*", String.Empty).ToLower();
                if (lower != "sallie" && lower != "computer" &&
                    lower != "of")
                    words.Add(lower);
            }
            //ModuleHearWords nmHear = (ModuleHearWords)FindModuleByType(typeof(ModuleHearWords));
            //if (nmHear != null)
            //{
            //    String phrase = e.Result.Text;
            //    if (e.Result.Words.Count != 1)
            //    {
            //        int i = phrase.IndexOf(' ');
            //        phrase = phrase.Substring(i + 1);
            //    }
            //    nmHear.HearPhrase(phrase);
            //}
        }

        public void ReceiveInputFromText(string phrase)
        {
            List<string> words = new();
            foreach ( string word in phrase.Split(' ',StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                words.Add(Regex.Replace(word, "[\\W]*", String.Empty));
            }
            this.words.AddRange(words);
        }

        public void PauseRecognition()
        {
            if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
                Debug.WriteLine("Speech Recognition paused.");
            }
        }

        public void ResumeRecognition()
        {
            if (recognizer != null)
                if (recognizer.AudioState == AudioState.Stopped && speechEnabled)
                {
                    recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    Debug.WriteLine("Speech Recognition resumed.");
                }
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

            if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
                recognizer.Dispose();
            }

            StartRecognizer();
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
        }
    }
}
