using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using BrainSimulator.Modules;
using System.Net.Http;

namespace BrainSimulator
{
    internal static class Network
    {
        static TcpListener server;
        static TcpClient tcpClient;
        public static NetworkStream theTcpStreamIn { get; private set; }
        public static NetworkStream theTcpStreamOut { get; private set; }
        public static bool podPaired = false;

        public static HttpClient theHttpClient = new() { Timeout = TimeSpan.FromSeconds(2), };
        public static bool httpClientBusy;
        private static UdpClient _UDPBroadcast;
        public static UdpClient UDPBroadcast {
            get
            {
                if (_UDPBroadcast == null || _UDPBroadcast.Client == null )
                {
                    _UDPBroadcast = new UdpClient
                    {
                        EnableBroadcast = true
                    };
                }
                return _UDPBroadcast;
            }
        }

        private static UdpClient _UDPReceive;
        public static UdpClient UDPReceive {
            get
            {
                if (_UDPReceive == null || _UDPReceive.Client == null)
                {
                    _UDPReceive = new UdpClient(UDPReceivePort);
                    _UDPReceive.Client.ReceiveBufferSize = 10000000;
                }
                return _UDPReceive;
            }
        }
        private static UdpClient _receiveSubscribedMessages;
        public static UdpClient receiveSubscribedMessages
        {
            get
            {
                if (_receiveSubscribedMessages == null || _receiveSubscribedMessages.Client == null)
                {
                    _receiveSubscribedMessages = new UdpClient(9090);
                    _receiveSubscribedMessages.Client.ReceiveBufferSize = 10000000;
                }
                return _receiveSubscribedMessages;
            }
        }
        private static IPAddress broadcastAddress;
        public static int UDPReceivePort = 3333;
        public static int UDPSendPort = 3333;
        public static int UDPAudioReceivePort = 666;


        private static UdpClient _UDPAudioBroadcast;
        public static UdpClient UDPAudioBroadcast
        {
            get
            {
                if (_UDPAudioBroadcast == null || _UDPAudioBroadcast.Client == null)
                {
                    _UDPAudioBroadcast = new UdpClient
                    {
                        EnableBroadcast = true
                    };
                }
                return _UDPAudioBroadcast;
            }
        }

        private static UdpClient _UDPAudioReceive;
        public static UdpClient UDPAudioReceive
        {
            get
            {
                if (_UDPAudioReceive == null || _UDPAudioReceive.Client == null)
                {
                    _UDPAudioReceive = new UdpClient(UDPAudioReceivePort);
                    _UDPAudioReceive.Client.ReceiveBufferSize = 10000000;
                }
                return _UDPAudioReceive;
            }
        }

        public static void Broadcast(string message)
        {
            //Debug.WriteLine("Broadcast: " + message);
            if (broadcastAddress == null) SetBroadcastAddress();
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new(broadcastAddress, UDPSendPort);

            UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);
        }        
        public static bool UDP_Send(string message,IPAddress ipToSend, int udpPort)
        {
            if (ipToSend == null) return false;
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(ipToSend, udpPort);
            UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);                        
            return true;
        }
        public static bool UDP_Setup_Send(string message, IPAddress ipToSend, int udpPort)
        {
            if (ipToSend == null) return false;
            byte[] datagram = Encoding.UTF8.GetBytes(message);
            IPEndPoint ipEnd = new IPEndPoint(ipToSend, udpPort);
            //UDPBroadcast.SendAsync(datagram, datagram.Length, ipEnd);            
            UdpClient outClient = new UdpClient(9090);
            outClient.Send(datagram, datagram.Length, "192.168.0.103", 9090);
            outClient.Close();
            return true;
        }
        public static bool InitTCP(string podIPString)//This really needs to have some checking happening during the handshake to make sure we are initialized on the mega and whatnot
        {
            //if (podIPString == "0.0.0.0") return false;
            
            int portN = 54321;
            IPAddress deviceIp = IPAddress.Parse(podIPString);
            try
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
                if ( server != null )
                {
                    server.Stop();
                }
                server = new TcpListener(IPAddress.Any, portN);
                server.Start();
                

                tcpClient = new();
                //var task = server.AcceptTcpClientAsync();
                //var result = tcpChecker.BeginConnect(ip, portN, null, null);
                DateTime start = DateTime.Now;
                while (!server.Pending() && (DateTime.Now - start) < TimeSpan.FromSeconds(15))
                {
                }
                if (!server.Pending())
                {
                    return false;
                }
                tcpClient = server.AcceptTcpClient();
                podPaired = true;

                theTcpStreamOut = tcpClient.GetStream();
                theTcpStreamIn = tcpClient.GetStream();
                theTcpStreamIn.Socket.NoDelay = true;
                theTcpStreamOut.Socket.NoDelay = true;
                //SendStringToPodTCP("BrainsimWaiting");
            }
            catch (Exception e)
            {
                server.Stop();
                //this.isEnabled = false;
                Debug.WriteLine("Init TCP Exception: " + e.Message);
                return false;
            }
            return true;
        }

        public static void SetBroadcastAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    string[] ipComps = ipStr.Split(".");

                    broadcastAddress = IPAddress.Parse(ipComps[0] + "." + ipComps[1] + "." + ipComps[2] + ".255");
                    return;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static void SendStringToPodTCP(string msg)
        {
            if ( !podPaired) return;
            if (theTcpStreamOut == null) return;
            msg += " \n";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            try
            {
                theTcpStreamOut.Write(data, 0, data.Length);
                //Debug.Write("Message-> ");
                //Debug.WriteLine(msg);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception" + e.Message);
            }
        }
    }
}
