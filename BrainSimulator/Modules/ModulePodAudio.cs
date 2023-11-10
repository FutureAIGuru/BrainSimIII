//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModulePodAudio : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePodAudio()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        private string podIPString = "";
        IPAddress broadCastAddress;
        private static ConcurrentQueue<byte> AudioByteQueue = new ConcurrentQueue<byte>();
        Task UDPListenerTask;
        Task UDPSenderTask = null;
        CancellationTokenSource UDPListenerToken = new();
        CancellationTokenSource UDPSenderToken = new();
        System.Media.SoundPlayer snd;

        [XmlIgnore]
        public bool recordMic = false;
        private FileStream fs;

        // Volume and Volume Slider info
        [XmlIgnore]
        public double volume = 0.5;
        [XmlIgnore]
        public int sliderValue = 10;
        [XmlIgnore]
        public int sliderNumTicks = 4;

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();
        }

        public void PlaySoundEffect(string soundfile, string localDirectory = null)
        {
            string defaultPath;
            if (localDirectory == null)
                defaultPath = Path.Combine(Utils.GetOrAddLocalSubFolder(Utils.FolderAudioFiles), soundfile);
            else
                defaultPath = Path.Combine(Utils.GetOrAddLocalSubFolder(localDirectory), soundfile);

            if (File.Exists(defaultPath))
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if (CQ.Recording)
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "QueueSound";
                        temp.V = soundfile;
                        CQ.RecList.Add(temp);
                        //maybe add info to recording texbox
                    }
                }
                ModuleSpeechOut audioOutput = (ModuleSpeechOut)FindModule(typeof(ModuleSpeechOut));
                if (audioOutput == null ||
                    !audioOutput.remoteSpeakerEnabled)
                {
                    snd = new(defaultPath);
                    snd.Play();
                }
                else
                {
                    FileStream str = File.Open(defaultPath, FileMode.Open);
                    byte[] wavToSend = new byte[str.Length];
                    str.Read(wavToSend);
                    // Slicing the array to get rid of wav header
                    AddByteArrayToQueue(wavToSend[44..]);

                    str.Close();
                }
            }
        }

        public void AddByteArrayToQueue(byte[] bytes, bool pad = true)
        {
            int rem = bytes.Length % 1024;
            int padding = (rem == 0) ? 0 : 1024 - rem;

            foreach (byte b in bytes) AudioByteQueue.Enqueue(b);
            if (pad)
            {
                for (int i = 0; i < padding; i++) AudioByteQueue.Enqueue(new byte());
            }
        }

        public void PadQueue(int numBytes)
        {
            for (int i = 0; i < numBytes; i++)
            {
                AudioByteQueue.Enqueue(new byte());
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        internal void ClearAudioQueue()
        {
            AudioByteQueue.Clear();
            if (snd != null) snd.Stop();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            Init();
            if (mv == null) return; //this is called the first time before the module actually exists            

            AudioByteQueue.Clear();
            //PlaySoundEffect("ES_Magical Whoosh 4 WAV.wav");

            //This gets the wifi IP address
            string ip = GetLocalIPAddress();
            string[] ipComps = ip.Split(".");

            broadCastAddress = IPAddress.Parse(ipComps[0] + "." + ipComps[1] + "." + ipComps[2] + ".255");
            Closing();
            var x = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            CancellationToken ct = UDPListenerToken.Token;

            UDPListenerTask = Task.Run(() =>
            {
                Thread.CurrentThread.Name = "RecieveFromMicrophone";
                ReceiveFromMicrophone();
            }, UDPListenerToken.Token); // Pass same token to Task.Run.
            initialized = true;

            if (UDPSenderTask == null)
            {
                UDPSenderTask = Task.Run(() =>
                   {
                       Thread.CurrentThread.Name = "SendToSpeaker";
                       SendToSpeaker();
                   }, UDPSenderToken.Token);
            }
        }
        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            if (byteArray == null) return false;
            try
            {
                using (var fs = new FileStream("micAudio\\" + fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }

        public void SendToSpeaker()
        {
            if (this.isEnabled == false) return;
            byte[] wavToSend = new byte[1024];

            ModuleSpeechOut audioOutput = (ModuleSpeechOut)FindModule(typeof(ModuleSpeechOut));
            if (audioOutput == null ||
                !audioOutput.remoteSpeakerEnabled) return;

            DateTime startTime = DateTime.Now;
            bool done = false;
            while (!done)
            {
                bool cancellation = UDPSenderToken.IsCancellationRequested;
                if (cancellation) return;

                if (podIPString == "" || !Network.UDPAudioBroadcast.Client.Connected)
                {
                    ModulePod thePod = (ModulePod)FindModule("Pod");
                    if (thePod != null)
                    {
                        podIPString = thePod.podIPString;
                        IPAddress podIP = IPAddress.Parse(podIPString);
                    }
                    Network.UDPAudioBroadcast.Connect(podIPString, Network.UDPAudioReceivePort);
                }

                if (DateTime.Now - startTime > TimeSpan.FromMilliseconds(28))
                {
                    if (AudioByteQueue.Count >= 1024)
                    {
                        int index;
                        for (index = 0; index < wavToSend.Length; index += 2)
                        {
                            AudioByteQueue.TryDequeue(out byte firstByte);
                            AudioByteQueue.TryDequeue(out byte secondByte);
                            short sample = (short)(secondByte << 8 | firstByte);
                            sample = (short)(sample * volume);
                            wavToSend[index] = (byte)(sample & 0xFF);
                            wavToSend[index + 1] = (byte)(sample >> 8);
                        }
                        Network.UDPAudioBroadcast.Send(wavToSend, wavToSend.Length);
                        startTime = DateTime.Now;
                    }
                }
            }
            //fs.Close();
        }

        [XmlIgnore]
        public int[] waveFormBuffer = new int[1000];
        int bufferPtr = 0;

        public void ReceiveFromMicrophone()
        {
            if (this.isEnabled == false) return;
            ModuleSpeechInPlus audioInput = (ModuleSpeechInPlus)FindModule(typeof(ModuleSpeechInPlus));
            ModuleIntercom mi = (ModuleIntercom)FindModule("Intercom");
            bool running = true;
            //DateTime startTime = DateTime.Now;
            //long prevTime = Utils.GetPreciseTime();

            while (running)
            {
                bool cancellation = UDPSenderToken.IsCancellationRequested;
                if (cancellation) return;

                //int incomingMessage;
                var from = new IPEndPoint(IPAddress.Any, Network.UDPAudioReceivePort);
                try
                {
                    var recvBuffer = Network.UDPAudioReceive.Receive(ref from);
                    //Debug.WriteLine("Time to read packet: " + (DateTime.Now - beforePacket).TotalMilliseconds);
                    //long v = Utils.GetPreciseTime();
                    //Debug.Write((v - prevTime).ToString() + " ");
                    //prevTime = v;
                    
                    byte[] singleSample = new byte[4];//16000 samples per sec on arduino
                    byte[] decondInt16 = new byte[256];
                    int[] decondInt32 = new int[256];


                    //convert byte array to int array, 4 entries in a block
                    for (int i = 0; i < recvBuffer.Length; i += 4)
                    {

                        decondInt32[i / 4] = (BitConverter.ToInt32(
                            new byte[] {
                                recvBuffer[i+3],
                                recvBuffer[i + 2],
                                recvBuffer[i + 1],
                                recvBuffer[i] }));

                        int theSample = recvBuffer[i + 0] + (recvBuffer[i + 1] << 8) + (recvBuffer[i + 2] << 16) + (recvBuffer[i + 3] << 24);
                        //Debug.WriteLine(theSample);
                        theSample = (theSample >> 4);

                        byte[] forAzure = new byte[2];
                        forAzure[0] = (byte)((theSample & 0xff00) >> 8);
                        forAzure[1] = (byte)((theSample & 0xff0000) >> 16);
                        audioInput.pushStream.Write(forAzure, 2);
                        if (mi != null && mi.waveProvider != null)
                        {
                            mi.waveProvider.AddSamples(forAzure, 0, 2);
                        }
                        if (recordMic)
                        {
                            if (fs == null) fs = new FileStream(Utils.GetOrAddLocalSubFolder(Utils.FolderAudioFiles + "\\micAudio\\") + "audioOut-" + Guid.NewGuid(), FileMode.Create, FileAccess.Write);
                            fs.Write(forAzure, 0, 2);
                        }
                        else
                        {
                            if (fs != null)
                            {
                                fs.Dispose();
                                fs = null;
                            }
                        }

                        int azureLikeSample = ((theSample & 0xff00) >> 8);
                        int partB = ((theSample & 0xff0000) >> 8);
                        azureLikeSample = azureLikeSample | partB;
                        //waveFormBuffer[bufferPtr++] = azureLikeSample;
                        waveFormBuffer[bufferPtr++] = theSample;
                        if (bufferPtr >= waveFormBuffer.Length) bufferPtr = 0;
                    }
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    running = false;
                    Debug.WriteLine("PodAudio: ReceiveFromMicrophone has encountered an error: " + e.Message);
                }
                catch (Exception e)
                {
                    running = false;
                    Debug.WriteLine("PodAudio: ReceiveFromMicrophone has encountered an error: " + e.Message);
                }
            }

        }

        public void Broadcast(string message)
        {
            //Debug.WriteLine("Broadcast: " + message);
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new(broadCastAddress, Network.UDPAudioReceivePort);
            Network.UDPAudioBroadcast.SendAsync(datagram, datagram.Length, ipEnd);
        }
        public override void Closing()
        {
            GetUKS();

            DateTime closeRequestTime = DateTime.Now;
            if (UDPListenerTask != null)
            {
                UDPListenerToken.Cancel();
                while (!UDPListenerTask.IsCompleted)
                {
                    if (DateTime.Now - closeRequestTime > TimeSpan.FromSeconds(10))
                    {
                        if (!UDPListenerTask.IsCompleted) Debug.WriteLine("PodAudio: UDPListenerTask did not close.");
                        break;
                    }
                }

                UDPListenerTask = null;
                UDPListenerToken = new CancellationTokenSource();
            }
            if (UDPSenderTask != null)
            {
                UDPSenderToken.Cancel();
                while (!UDPSenderTask.IsCompleted)
                {
                    if (DateTime.Now - closeRequestTime > TimeSpan.FromSeconds(10))
                    {
                        if (!UDPSenderTask.IsCompleted) Debug.WriteLine("PodAudio: UDPSenderTask did not close.");
                        break;
                    }
                }

                UDPSenderTask = null;
                UDPSenderToken = new CancellationTokenSource();
            }

            Network.UDPAudioReceive.Close();
            Network.UDPAudioBroadcast.Close();
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            // Initialize();
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }

        public override MenuItem CustomContextMenuItems()
        {
            StackPanel s = new StackPanel { Orientation = Orientation.Horizontal };
            s.Children.Add(new Label { Content = "Volume:", Width = 70 });
            Slider sl1 = new Slider { Name = "Volume", Minimum = 0, Maximum = sliderNumTicks, IsSnapToTickEnabled = true, TickFrequency = 1, SmallChange = 1, Width = 100, Height = 20, Value = sliderValue };

            sl1.ValueChanged += Sl1_ValueChanged;
            s.Children.Add(sl1);

            return new MenuItem { Header = s, StaysOpenOnClick = true };
        }

        private void Sl1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider sl)
            {
                if (sl.Name == "Volume")
                {
                    //volume = sl.Value == 0 ? 0 : sl.Value*2;
                    volume = sl.Value == 0 ? 0 : 1 / Math.Pow(2, Math.Abs(sl.Value - (sliderNumTicks + 1)));
                    sliderValue = (int)sl.Value;
                }
                Debug.WriteLine("Volume: " + volume);
            }
        }
    }
}