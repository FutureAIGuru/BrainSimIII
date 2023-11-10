//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleAudioInputStreamTestFromFile : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public PushAudioInputStream pushStream;
        SpeechRecognizer speechClient;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleAudioInputStreamTestFromFile()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //pushStream.Write(new byte[2], 2);

            //UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {            
            string SubscriptionKey = "088390467bda4b7fa3b7dc9827b7650a";
            string ServiceRegion = "eastus";

            //FOR AUDIOSTREAM TESTING.UNCOMMENT audioStream.Write line ~81
            //var audioStream = new VoiceAudioStream();
            //var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
            //var audioConfig = AudioConfig.FromStreamInput(audioStream, audioFormat);

            // FOR PUSHSTREAM TESTING. UNCOMMENT pushStream.Write line ~82
            pushStream = AudioInputStream.CreatePushStream();
            var audioConfig = AudioConfig.FromStreamInput(pushStream);

            var speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, ServiceRegion);
            speechClient = new SpeechRecognizer(speechConfig, audioConfig);

            const string FileName = "Networks\\SallieKeyword.table";
            KeywordRecognitionModel keywordModel = KeywordRecognitionModel.FromFile(FileName);

            speechClient.Recognized += _speechClient_Recognized;
            speechClient.Recognizing += _speechClient_Recognizing;
            speechClient.SpeechStartDetected += _startDetected;
            speechClient.SpeechEndDetected += _endDetected;
            speechClient.Canceled += _speechClient_Canceled;
            speechClient.SessionStopped += _sessionStopped;
            speechClient.SessionStarted += _sessionStarted;
            speechClient.StartKeywordRecognitionAsync(keywordModel);
            Debug.WriteLine(DateTime.Now + ": " + "Started Continuous Recognition");
        }

        internal void insertIntoBuffer()
        {
            if (pushStream == null) Initialize();
            String file = "Resources/ryanHexOutput_test.bin";
            using (FileStream fs = File.OpenRead(file))
            {
                byte[] sourceSample = new byte[4];
                byte[] waveBytes = new byte[1600000];
                int outCount = 0;
                while (fs.Read(sourceSample, 0, sourceSample.Length) > 0)
                {
                    int theSample = sourceSample[0] +(sourceSample[1] << 8) + (sourceSample[2] << 16)+ (sourceSample[3] << 24);
                    theSample = (theSample >> 4);
                    //waveBytes[outCount++] =  (byte)(theSample & 0xff);
                    waveBytes[outCount++] = (byte)((theSample & 0xff00) >> 8);
                    waveBytes[outCount++] = (byte)((theSample & 0xff0000) >> 16);
                    //waveBytes[outCount++] = (byte)((theSample & 0xff000000) >> 24);
                    byte[] forAzure = new byte[2];
                    forAzure[0] = (byte)((theSample & 0xff00) >> 8);
                    forAzure[1] = (byte)((theSample & 0xff0000) >> 16);
                    //audioStream.Write(forAzure, 0, forAzure.Length);
                    pushStream.Write(forAzure, forAzure.Length);
                }
                for (int i = 0; i < 500000; i++)
                {
                    pushStream.Write(new byte[2], 2);
                }
                //for (int i = 0; i < waveBytes.Length / 3200; i++)
                //{
                //    pushStream.Write(waveBytes[i..3200], 3200);
                //}

                //building and playing wave file
                WaveMemoryStream wave = new WaveMemoryStream(waveBytes[0..outCount], 16000, 16, 1);
                var sound = new System.Media.SoundPlayer();
                sound.Stream = wave;
                sound.Play();
            }
        }

        private void _endDetected(object sender, RecognitionEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Detected End");
        }

        private void _sessionStarted(object sender, SessionEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Client Session Started");
        }

        private void _sessionStopped(object sender, SessionEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Client Session Stopped");
        }

        private void _speechClient_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + e.Reason + ": " + e.ErrorDetails);
        }

        private void _speechClient_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            //Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Interim Result: " + e.Result.Text);
        }

        private void _startDetected(object sender, RecognitionEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Detected Start");
        }

        private void _speechClient_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            Debug.WriteLine(DateTime.Now  + ": " + e.SessionId + ": " + "Speech Final Result: " + e.Result.Text);
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}