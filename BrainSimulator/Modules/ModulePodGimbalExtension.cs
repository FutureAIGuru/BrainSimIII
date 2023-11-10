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
        public float MinPan { get => minPan; }
        public float MaxPan { get => maxPan; }
        public float MinTilt { get => minTilt; }
        public float MaxTilt { get => maxTilt; }

        float distanceToPan = 90;
        float distanceToTilt = 90;
        //*************************************CameraControl****************************************
        float minPan = -90;
        float maxPan = 90;
        float minTilt = -20;
        float maxTilt = 90;
        public void pan(Angle value)
        {
            distanceToPan = value.Degrees;
            if (distanceToPan < minPan) distanceToPan = minPan;
            if (distanceToPan > maxPan) distanceToPan = maxPan;

            distanceToPan += 90;
            string bck1 = "A2 T" + (int)distanceToPan + " ";
            SendStringToPodTCP(bck1);
        }

        public void tilt(Angle value)
        {
            distanceToTilt = -value.Degrees;
            if (distanceToTilt < minTilt) distanceToTilt = minTilt;
            if (distanceToTilt > maxTilt) distanceToTilt = maxTilt;

            distanceToTilt += 90;
            string bck1 = "A3 T" + (int)distanceToTilt + " ";
            SendStringToPodTCP(bck1);
        }

        public void centerCam()
        {
            int panCenter = 90;
            int tiltCenter = 90;
            distanceToPan = panCenter;
            distanceToTilt = tiltCenter;
            string panServo = "A2 T" + panCenter + " ";
            string tiltServo = "A3 T" + tiltCenter + " ";
            SendStringToPodTCP(panServo);
            SendStringToPodTCP(tiltServo);
        }
        //**********************************EndCameraControl****************************************
    }
}