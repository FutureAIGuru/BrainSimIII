//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModulePodConnection : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        [XmlIgnore]
        public bool podConnected = false;
        private string PreferredPodIP;
        [XmlIgnore]
        public string PodName;
        [XmlIgnore]
        public IPAddress CameraIP;
        [XmlIgnore]
        public IPAddress PairedPodIP;
        [XmlIgnore]
        public bool listChanged = false;
        [XmlIgnore]
        public Dictionary<string,PodIPInfo> PodList = new();
        [XmlIgnore]
        public PodIPInfo PodSelected;

        public class PodIPInfo
        {
            public string PodName;
            public IPAddress PodIP;
            public IPAddress CameraIP;
            public int missedDevicePolls;
        };

        Task UDPListenerTask;
        CancellationTokenSource tokenSource2 = new();

        private bool sendDevicePoll = false;
        private bool AutoPair = true;
        public bool AutoConnection = false;
        DateTime lastDevicePoll;
        DateTime startOfAutomaticPairing;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePodConnection()
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

            if ( AutoPair && PodList.Count == 1 )
            {
                if ( AutoConnection && (DateTime.Now - startOfAutomaticPairing).TotalSeconds > 5)
                {
                    PodName = PodList.Values.ElementAt(0).PodName;
                    PairPod(PodList.Values.ElementAt(0));
                }
            } else
            {
                startOfAutomaticPairing = DateTime.Now;
            }

            ModulePod mp = (ModulePod)FindModule("Pod");
            if ( mp.isEnabled && PodSelected == null )
            {
                if (PodList.Count > 0)
                    PodSelected = PodList[PodName];
            }
            if (PairedPodIP == null || CameraIP == null || ! mp.isEnabled || sendDevicePoll)
            {
                DateTime curTime = DateTime.Now;
                if (curTime - lastDevicePoll >= TimeSpan.FromMilliseconds(500))
                {
                    lastDevicePoll = curTime;
                    foreach (PodIPInfo podIPInfo in PodList.Values)
                    {
                        if (podIPInfo.missedDevicePolls++ == 3)
                        {
                            PodList.Remove(podIPInfo.PodName);
                            Debug.WriteLine(podIPInfo.PodName + " removed as it was unresponsive.");
                        }
                    }
                    Network.Broadcast("DevicePoll");
                    sendDevicePoll = false;
                }
            }

            UpdateDialog();
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            CameraIP = null;
            updateCameras();
        }

         public void updateCameras()
        {
            //This gets the wifi IP address
            Closing();

            _ = Network.UDPReceive;
            _ = Network.UDPBroadcast;

            CancellationToken ct = tokenSource2.Token;
            PodIPInfo podIPInfo = null;
            if (PodName != null && PodName != "" && PodList.ContainsKey(PodName)) podIPInfo = PodList[PodName];
            PodList.Clear();
            if ( podConnected && podIPInfo != null ) PodList.Add(PodName, podIPInfo);
            UDPListenerTask = Task.Run(() =>
            {
                Thread.CurrentThread.Name = "ReceiveFromServer";
                ReceiveFromServer();
            }, tokenSource2.Token); // Pass same token to Task.Run.
        }

        public void ReceiveFromServer()
        {
            if (this.isEnabled == false) return;
            bool running = true;
            while (running)
            {
                bool cancellation = tokenSource2.IsCancellationRequested;
                if (cancellation) return;

                string incomingMessage = "";
                var from = new IPEndPoint(IPAddress.Any, Network.UDPAudioReceivePort);
                try
                {
                    var recvBuffer = Network.UDPReceive.Receive(ref from);
                    incomingMessage += Encoding.UTF8.GetString(recvBuffer);
                    if (incomingMessage.StartsWith("Camera"))
                    {
                        IPAddress fromAddress = from.Address;
                        string[] splitMessage = incomingMessage.Split(" ");
                        string cameraName = splitMessage.Length == 1 ? "UnknownCamera" : incomingMessage.Split(" ")[1];
                        if (cameraName == "UnknownCamera") cameraName = "UnknownPod";

                        if (PodList.ContainsKey(cameraName))
                        {
                            PodList[cameraName].CameraIP = fromAddress;
                        }
                        if (PodSelected?.PodName == cameraName)
                        {
                            CameraIP = fromAddress;
                        }
                    }
                    if (incomingMessage.StartsWith("SalliePod"))
                    {
                        IPAddress fromAddress = from.Address;
                        PodIPInfo podInfo;
                        string[] splitMessage = incomingMessage.Split(" ");
                        string podName = splitMessage.Length == 1 ? "UnknownPod" : incomingMessage.Split(" ")[1];
                        if (PodList.ContainsKey(podName))
                        {
                            podInfo = PodList[podName];
                            PodList[podName].PodIP = fromAddress;
                            PodList[podName].missedDevicePolls = 0;
                        }
                        else
                        {
                            podInfo = new();
                            podInfo.PodName = podName;
                            podInfo.PodIP = fromAddress;
                            PodList.Add(podName, podInfo);
                            listChanged = true;
                        }
                        ModulePod mp = (ModulePod)FindModule("Pod");
                        if (AutoConnection && !mp.isEnabled && (fromAddress.Equals(PairedPodIP) || fromAddress.ToString().Equals(PreferredPodIP)))
                        {
                            PodName = podName;
                            PairPod(podInfo);
                            
                        }                        
                    }
                    Debug.WriteLine("Received from Device: " + from.Address + " " + incomingMessage);
                }
                catch (Exception e)
                {
                    running = false;
                    Debug.WriteLine("ModulePodConnection: RecieveFromServer encountered an exception: " + e.Message);
                }
            }
        }

        internal void DisconnectPod()
        {
            MainWindow.SuspendEngine();
            PodSelected = null;
            AutoPair = false;
            ModulePod mp = (ModulePod)FindModule("Pod");
            mp.initialized = false;
            mp.isEnabled = false;
            mp.Closing();
            Network.podPaired = false;
            podConnected = false;
            SendUDP("Disconnect", PairedPodIP);
            CameraIP = null;
            PairedPodIP = null;
            ModulePodCamera mpc = (ModulePodCamera)FindModule("PodCamera");
            mpc.ClearBitmap();
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            mpi.isLive = false;
            if ( Network.theTcpStreamIn != null ) Network.theTcpStreamIn.Close();
            if ( Network.theTcpStreamOut != null) Network.theTcpStreamOut.Close();

            MainWindow.ResumeEngine();
        }

        public void SendUDP(string message, IPAddress targetAddress)
        {
            try
            {
                byte[] datagram = Encoding.UTF8.GetBytes(message);
                IPEndPoint ipEnd = new(targetAddress, Network.UDPSendPort);
                Network.UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);
            }
            catch ( Exception e)
            {
                Debug.WriteLine("ModulePodConnection: SendUDP encountered an exception: " + e.Message);
            }
        }

        public void RenamePod(string newName)
        {
            byte[] datagram = Encoding.UTF8.GetBytes("Rename " + newName);
            if (PairedPodIP == null || CameraIP == null)
            {
                System.Windows.MessageBox.Show("Please pair to a pod AND camera before attempting to rename.");
                return;
            }
            IPEndPoint ipEnd = new(PairedPodIP, Network.UDPReceivePort);
            Network.UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);
            ipEnd = new(CameraIP, Network.UDPReceivePort);
            Network.UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);
            sendDevicePoll = true;
        }


        public void PairPod(PodIPInfo podInfo)
        {
            //set the pod ipaddress in the pod module
            //audio should pick it from there
            if (podInfo == null) return;
            AutoPair = false;
            PreferredPodIP = "";
            ModulePod thePod = (ModulePod)FindModule("Pod");
            if (thePod != null)
            {
                thePod.PodIPString = podInfo.PodIP.ToString();
                podConnected = true;
            }

            thePod.SetDesiredAngle(0);
            thePod.SetDesiredMoveTarget(0);

            //pair the pod to this brainSim
            byte[] datagram = Encoding.UTF8.GetBytes("Pair");
            PairedPodIP = podInfo.PodIP;
            CameraIP = podInfo.CameraIP;
            PodName = podInfo.PodName;
            IPEndPoint ipEnd = new(PairedPodIP, Network.UDPReceivePort);
            Network.UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);

            System.Threading.Thread.Sleep(1000);
            ModulePodAudio thePodAudio = (ModulePodAudio)FindModule("PodAudio");
            thePodAudio?.Initialize();

            thePod.isEnabled = true;
            Properties.Settings.Default["LastConnectedPodIP"] = podInfo.PodIP.ToString();
            Properties.Settings.Default.Save();
        }

        public override void Closing()
        {
            if (UDPListenerTask != null)
            {
                tokenSource2.Cancel();
                DateTime closeRequestTime = DateTime.Now;
                while (!UDPListenerTask.IsCompleted)
                {
                    if (DateTime.Now - closeRequestTime > TimeSpan.FromSeconds(10))
                    {
                        Debug.WriteLine("PodAudio: UDPListenerTask did not close.");
                        break;
                    }
                }
                UDPListenerTask = null;

                Network.UDPBroadcast.Close();
                Network.UDPReceive.Close();
                tokenSource2 = new CancellationTokenSource();
            }
        }

        public async void OTA_Mode(IPAddress ESPIPAddress, string deviceName)
        {
            MainWindow.SuspendEngine();
            ModulePodConnection mpc = (ModulePodConnection)FindModule("PodConnection");
            if ( ESPIPAddress == null)
            {
                System.Windows.MessageBox.Show($"Please Connect To A {deviceName} Before Initiating OTA.");
                MainWindow.ResumeEngine();
                return;
            }
            string ESPIPString = ESPIPAddress.ToString();
            if (mpc !=null) mpc.SendUDP("OTA_TRIGGER", IPAddress.Parse(ESPIPString));
            DateTime OTAStarted = DateTime.Now;
            bool OTA_Confirmed = false;
            HttpClient client = new HttpClient();
            while (!OTA_Confirmed && (DateTime.Now - OTAStarted).TotalSeconds < 20)
            {
                try
                {
                    var checkingResponse = await client.GetAsync("http://" + ESPIPString + "/serverIndex");
                    if (checkingResponse != null || checkingResponse.StatusCode == HttpStatusCode.OK)
                        OTA_Confirmed = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ModulePodConnection: OTA_Mode encounted an exception: " + e.Message);
                    MainWindow.ResumeEngine();
                    return;
                }
            }
            if (OTA_Confirmed)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    InitialDirectory= deviceName.Equals("Pod") ? Utils.GetOrAddLocalSubFolder(Utils.FolderPodBin):Utils.GetOrAddLocalSubFolder(Utils.FolderCameraBin),
                };
                if (openFileDialog.ShowDialog() != true)
                {
                    MainWindow.ResumeEngine();
                    return;
                }
                using (var multipartFormContent = new MultipartFormDataContent())
                {
                    //Load the file and set the file's Content-Type header
                    var fileStreamContent = new StreamContent(File.OpenRead(openFileDialog.FileName));
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

                    //Add the file
                    multipartFormContent.Add(fileStreamContent, name: "file", fileName: "OTAUpload.bin");

                    //Send it
                    try
                    {
                        var response = await client.PostAsync("http://" + ESPIPString + "/update", multipartFormContent);

                        if (response.IsSuccessStatusCode)
                        {
                            System.Windows.MessageBox.Show("OTA File Uploaded Successfully.");
                            client.GetAsync("http://" + ESPIPString + "/serverReboot");
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("OTA File Upload Failed, Please Restart Pod and try again.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ModulePodConnection: OTA_Mode encounted an exception: " + e.Message);
                        Debug.WriteLine($"If your {deviceName} restarted, OTA was successful. If not please restart the pod and try again.");

                        MainWindow.ResumeEngine();
                        return;
                    }


                }
            } // upload firmware
            else System.Windows.MessageBox.Show("Pod did not enter OTA mode, please try again.");


            MainWindow.ResumeEngine();
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
            initialized = false;
            PreferredPodIP = (string)Properties.Settings.Default["LastConnectedPodIP"];
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
            MenuItem mi = new MenuItem();
            mi.Header = "Reset WiFi Network";
            mi.Click += Mi_Click;
            return mi;
        }

        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            if (PodSelected == null )
            {
                MessageBox.Show("Need to be connected to a pod to reset it's network.");
                return;
            }
            
            byte[] datagram = Encoding.UTF8.GetBytes("NewNetwork");
            IPEndPoint ipEndPod = new(PodSelected.PodIP, Network.UDPSendPort);
            Network.UDPBroadcast.Send(datagram, ipEndPod);

            if (PodSelected.CameraIP != null)
            {
                IPEndPoint ipEndCam = new(PodSelected.CameraIP, Network.UDPSendPort);
                Network.UDPBroadcast.Send(datagram, ipEndCam);
            }
        }
    }
}