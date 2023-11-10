//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using NAudio.Wave;
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
    public class ModuleIntercom : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public bool Listen = false;
        [XmlIgnore]
        public bool Speak = false;
        WaveInEvent waveIn;
        WaveOut waveOut = new WaveOut();
        [XmlIgnore]
        public BufferedWaveProvider waveProvider;

        static int sampleRate = 16000; // 16 kHz
        static int channels = 1; // mono
        static int bits = 16;
        int totalBytesRecorded = 0;

        WaveFormat recordingFormat = new WaveFormat(sampleRate, bits, channels);
        
        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleIntercom()
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

            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            if ( waveIn != null )
            {
                waveIn.Dispose();
                waveIn = null;
            }
            waveIn = new WaveInEvent();
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.WaveFormat = recordingFormat;
        }

        private DateTime startOfMicRecord;
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
            mpa.AddByteArrayToQueue(e.Buffer[..e.BytesRecorded], false);
            totalBytesRecorded += e.BytesRecorded;
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            Initialize();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public void SetListenToPod(bool listen)
        {
            Listen = listen;
            if (Listen)
            {
                waveProvider = new BufferedWaveProvider(recordingFormat);
                waveProvider.BufferDuration = TimeSpan.FromSeconds(10);
                waveProvider.DiscardOnBufferOverflow = true;
                waveOut = new WaveOut();
                waveOut.DesiredLatency = 100;
                waveOut.NumberOfBuffers = 3;
                waveOut.Init(waveProvider);
                waveOut.Play();
            }
            else
            {
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                }
                waveOut = null;
                waveProvider = null;
             }
        }

        public void SendMicrophoneToPod(bool send)
        {
            if (send)
            {
                Speak = true;
                waveIn.StartRecording();
                startOfMicRecord = DateTime.Now;
            }
            else 
            {
                Speak = false;
                waveIn.StopRecording();
                ModulePodAudio mpa = (ModulePodAudio)FindModule("PodAudio");
                mpa.PadQueue(1024 - (totalBytesRecorded % 1024));
                totalBytesRecorded = 0;
            }
        }
    }
}