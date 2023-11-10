//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.NetworkInformation;
using static System.Math;
using System.Timers;
using System.Threading;

namespace BrainSimulator.Modules
{
    public partial class ModulePod : ModuleBase
    {
        public long getrawYaw()
        {
            return latestYawValue;
        }
        public bool getRobotInitStatus()
        {
            return podPaired;
        }
        public bool getInterfaceStatus()
        {
            return interfaceUp;
        }

        public static string RetrieveSerialPort()
        {
            string result = "COM3";
            return result;
        }
        public static string RetrieveConfigString()
        {
            string configFilename = null;
            string configString = "";
            if (configFilename == null)
            {
                configFilename = "DEFAULT";
                // Default is as before...
                configString ="";
            }
            else
            {
                Console.WriteLine("Reading pod config" + configFilename);
                configString = File.ReadAllText(configFilename);
            }
            return configString;
        }

    }
}