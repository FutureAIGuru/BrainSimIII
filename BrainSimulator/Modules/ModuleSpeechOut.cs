//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleSpeechOut : ModuleBase
    {
        SpeechSynthesizer synth;
        public bool speak = true;
        public PullAudioOutputStream pullStream;
        [XmlIgnore]
        public string toSpeak = ""; //accumulates an array of words to speak
        string prePend = "";
        string postPend = "";
        public bool remoteSpeakerEnabled;

        public ModuleSpeechOut()
        {
            minHeight = 1;
            minWidth = 1;
        }

        public override void Fire()
        {
            Init();
            if (synth == null) Initialize();

            GetUKS();
            if (UKS == null) return;

            Thing currentVerbalResponse = UKS.GetOrAddThing("CurrentVerbalResponse", "Attention");
            if (currentVerbalResponse == null) return;
            if (currentVerbalResponse.Relationships.Count == 0) return;
            toSpeak = "";
            toSpeak += prePend;
            for (int i = 0; i < currentVerbalResponse.Relationships.Count; i++)
            {
                if (!currentVerbalResponse.RelationshipsAsThings[i].Label.StartsWith("punc") )
                {
                    toSpeak += " ";
                }
                String currentWord = currentVerbalResponse.Relationships[i].T.V?.ToString().ToLower();
                if (currentWord == null)
                    currentWord = "none";
                if (currentWord.Equals("a") && i < currentVerbalResponse.Relationships.Count - 1)
                {
                    string tempLabel = currentVerbalResponse?.Relationships[i + 1].T.V.ToString();
                    if (tempLabel.Length > 0 && "aeiouAEIOU".Contains(tempLabel[0]))
                    {
                        currentWord = "an";
                    }
                }
                toSpeak += currentWord;
            }

            toSpeak = toSpeak.Trim();
            toSpeak += postPend + ".";
            toSpeak = char.ToUpper(toSpeak[0]) + toSpeak.Substring(1);
            ModuleSpeechInPlus msi = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
            if (msi != null && speak) msi.PauseRecognition(); //if there is a recognizer active

            SpeakText(toSpeak);
            ModuleUserInterface moduleUserInterface = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));
            if(moduleUserInterface != null)
                moduleUserInterface.DrawSpeechOut(toSpeak);

            currentVerbalResponse.RemoveAllRelationships();
            UpdateDialog();
        }
        public void SpeakText(string toSpeak)
        {
            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));

            if (string.IsNullOrEmpty(toSpeak))
            {
                Audio.PlaySoundEffect("SallieNegative1.wav");
            }
            if (speak && synth != null)
                synth.SpeakTextAsync(toSpeak);
        }
        public override void Initialize()
        {
            //string SubscriptionKey = "d9439a3d1452434db04bebe83626aa2e";
            //string ServiceRegion = "westus";
            string SubscriptionKey = "6971d548b1f64fde849082224acc54d8";
            string ServiceRegion = "eastus";
            byte[] wavToSend = new byte[102400];
            var config = SpeechConfig.FromSubscription(SubscriptionKey, ServiceRegion);
            config.SpeechSynthesisVoiceName = "en-US-SaraNeural";
            AudioConfig audioConfig;
            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (Audio == null) return;
            if (remoteSpeakerEnabled)
            {
                pullStream = AudioOutputStream.CreatePullStream();
                audioConfig = AudioConfig.FromStreamOutput(pullStream);
            }
            else
                audioConfig = AudioConfig.FromDefaultSpeakerOutput();

            // Configure the audio output.
            synth = new SpeechSynthesizer(config, audioConfig);
            synth.SynthesisCompleted += Synth_SynthesisCompleted;
        }

        private void Synth_SynthesisCompleted(object sender, SpeechSynthesisEventArgs e)
        {
            // Restart speech recognition.  
            ModuleSpeechInPlus msi = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
            ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
            if (remoteSpeakerEnabled && mpa != null)
            {
                mpa.AddByteArrayToQueue(e.Result.AudioData[44..]);
                // Code To Save Speech Out Bytes to a local file
                //string filePath = Utils.GetOrAddLocalSubFolder(Utils.FolderAudioFiles);
                //string fileName = "MicAudio_"+DateTime.Now.ToString("HHmmssfff");
                //using (FileStream fileStream = File.Create(Path.Combine(filePath, fileName)))
                //{
                //    fileStream.Write(e.Result.AudioData, 0, e.Result.AudioData.Length);
                //}
            }

            if (msi != null) msi.ResumeRecognition();
        }

        internal void PauseSpeech()
        {
            speak = false;
        }

        internal void ResumeSpeech()
        {
            speak = true;
        }

        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();
            MainWindow.ResumeEngine();
        }
    }
}
