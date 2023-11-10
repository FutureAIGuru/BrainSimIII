//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

/*
 * Shooting for around 400 lines of code max for a module
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public partial class ModulePod : ModuleBase
    {
        public ModulePod()
        {
            minHeight = 1;
            maxHeight = 1;
            minWidth = 1;
            maxWidth = 1;

            isEnabled = false;
        }

        [XmlIgnore]
        public int targetSpeed = 0;
        public int speedSet = 0;
        [XmlIgnore]
        public float motionSensorRatio = 0f;
        bool busyFlag;
        private DateTime heartBeatLastPoll = DateTime.Now;

        enum sensorType { none, servoPosition, motorPosition, motorRate, mpuSensor, analogSensor, heartBeat };
        public enum actuatorType { motor, servo };
        private class PodActuator
        {

            public string name;
            public int pin = -1;
            public int currentValue;
            public actuatorType type;
            public int timing;
            public int isEnabled;
        }

        private class PodSensor
        {
            public string name;
            public int pin = -1;
            public int pollingPeriod;// = 100;
            public int reportingPeriod;// = 1000;
            public int minChange;// = -10000;
            public int maxChange;// = 1000;
            public long currentValue;
            public long prevValue;
            public sensorType type;
            public int isEnabled;
            public string virtualPin;
        }

        List<PodActuator> actuators = new();
        List<PodSensor> sensors = new();

        public List<string> GetSensorData()
        {
            List<string> sensorData = new();

            foreach (PodSensor sensor in sensors)
            {
                sensorData.Add(sensor.type + ":" + sensor.pin + ":"
                    + sensor.isEnabled + ":" + sensor.currentValue + ":" + sensor.prevValue);
            }
            return sensorData;
        }

        public List<string> GetActuatorData()
        {
            List<string> actuatorData = new();

            foreach (PodActuator actuator in actuators)
            {
                actuatorData.Add(actuator.type + ":" + actuator.pin + ":" + actuator.isEnabled
                    + ":" + actuator.currentValue + ":" + actuator.timing);
            }
            return actuatorData;
        }


        public void SetPodBusy(bool cond)
        {
            busyFlag = cond;
        }
        public bool IsPodBusy()
        {
            return busyFlag;
        }

        byte[] b = new byte[512];
        int bcount = 0;
        private bool interfaceUp = false; // is podInterface module initialzed
        private bool podPaired = false; //the hardward is initialized

        string inputBuffer = "";
        int heartbeatNum = 9;
        DateTime lastCycleTime = DateTime.Now;
        public string configString = RetrieveConfigString();
        int configStringLinePointer = -1;
        string[] configStringLines = Array.Empty<string>();
        int sensorCount = 0;
        int actuatorCount = 0;
        bool antispam = false;

        public override void Fire()
        {
            Init();  //be sure to leave this here                        

            if (!initialized)
                Initialize();

            if (!podPaired) return;
            CheckMove();

            TimeSpan elapsed = DateTime.Now - lastCycleTime;

            lastCycleTime = DateTime.Now;

            LowBatteryWarning();
        }

        public void LowBatteryWarning()
        {
            GetUKS();
            Thing battery = UKS.GetOrAddThing("Battery Level", "Self");
            float totalCharge = (battery1 + battery2) / 1000f;
            Thing charge = new();
            charge.Label = (totalCharge * 20).ToString();
            battery.RemoveAllRelationships();
            battery.AddRelationship(charge);
            if (totalCharge > 3.6)
                antispam = false;
            else if (totalCharge == 0)
                antispam = true;
            if (!antispam)
            {
                ModulePodAudio modulePodAudio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
                if (modulePodAudio == null) return;
                if (totalCharge < 3)
                    modulePodAudio.PlaySoundEffect("Low Battery v1.wav");
                antispam = true;
            }
        }

        public string podIPString = "0.0.0.0";

        public string PodIPString
        {
            get { return podIPString; }
            set
            {
                podIPString = value;
                //InitTCP(); //this currently blocks if there is no TCP/IP pod listening
            }
        }

        public void RunDownConfigString(string stripline)
        {
            string currentLine = stripline;
            currentLine = currentLine.Trim();
            string[] theParams = currentLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (theParams[0] == "MpuCalibration:")
            {
                string trimmedParam = theParams[1].TrimStart();
                int.TryParse(trimmedParam, out int calibrationCycleNum);
                Debug.WriteLine("MpuCalibration cycle number: " + calibrationCycleNum);
            }
            else if (theParams.Length > 2 && theParams[0].IndexOf("//") != 0)//ignore comment and blank lines
            {
                float value = 0;
                switch (theParams[0])
                {
                    case "Sensor":
                        PodSensor sHandle = new PodSensor();
                        sHandle.name = theParams[1];
                        StripSensorData(theParams, sHandle);
                        sensors.Add(sHandle);
                        if (sHandle.type == sensorType.heartBeat)
                        {
                            heartbeatNum = sensors.IndexOf(sHandle);
                        }
                        sensorCount++;
                        break;
                    case "Actuator":
                        //------------------------------------------->this is for copying the actuator param into an actuator object
                        PodActuator aHandle = new PodActuator();
                        aHandle.name = theParams[1];
                        StripActuatorData(theParams, aHandle);
                        actuators.Add(aHandle);
                        actuatorCount++;
                        break;
                }
                currentLine = String.Join(" ", theParams.Skip(1)).Trim();
            }

            MainWindow.Update();
            return;
        }

        //##########################################################################################
        public void Recalibrate_Mpu()
        {
            //string msg = "S4 r11";
            //desiredAngle = latestYawValue;
            //Network.SendStringToPodTCP(msg);
            Reboot_ESP();
        }
        public void ResetYaw()
        {
            string msg = "S4 r1";
            desiredAngle = 0;
            Network.SendStringToPodTCP(msg);
        }
        public void Reboot_ESP()
        {
            Network.Broadcast("Reset");
        }

        public void ChangeLED(int r, int g, int b)
        {
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            string msg = $"L0 r{r} g{g} b{b}";
            Network.SendStringToPodTCP(msg);
        }

        public void beatHeart(int sensorNum)
        {
            TimeSpan heartBeatSpan = DateTime.Now - heartBeatLastPoll;

            if (heartBeatSpan > TimeSpan.FromMilliseconds(heartBeatPollTime))
            {
                SendStringToPodTCP("S" + sensorNum + " T0"); //??? perhaps I need to get the sensor from the sensor array  but i need to loop through and make sure it stores the new type to match up to firmware config                
                heartBeatLastPoll = DateTime.Now;
            }
        }
        TimeSpan storedTime = new TimeSpan(0, 0, 0, 0, 0);
        int timePolls = 1000;
        int currIteration = 0;
        private int heartBeatPollTime = 300;
        void PodLoopThread() //incoming
        {
            while (true)
            {
                DateTime tHandle = DateTime.Now;
                if (tokenSource.IsCancellationRequested)
                {
                    return;
                }
                if (!Network.theTcpStreamIn.Socket.Connected)
                {
                    Debug.WriteLine("Pod TCP Disconnected: Disabling Pod Module");
                    ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
                    if (mpi != null) mpi.isLive = false;
                    isEnabled = false;
                    initialized = false;
                    return;
                }
                while (Network.theTcpStreamIn.CanRead && Network.theTcpStreamIn.DataAvailable && (bcount < b.Length))
                {
                    byte x = (byte)Network.theTcpStreamIn.ReadByte();
                    b[bcount++] = x;
                    if ((char)x == '\n')
                    {
                        if (b[0] != 0 && ((char)b[0] != '\0'))
                        {
                            var outputmsg = System.Text.Encoding.Default.GetString(b);
                            ParsePodMessage(outputmsg);
                            //Debug.WriteLine("Command: " + outputmsg);
                            bcount = 0;
                            Array.Clear(b, 0, b.Length);
                        }
                    }
                    linearStraightCheck();
                    DateTime currTime = DateTime.Now;
                    TimeSpan dTime = tHandle - currTime;
                    if (storedTime < dTime) storedTime = dTime;
                    currIteration++;
                    if (currIteration >= timePolls)
                    {
                        //Debug.WriteLine("Longest Time Delta Measured: {0}", storedTime.Milliseconds);
                        currIteration = 0;
                        storedTime = new TimeSpan(0, 0, 0, 0, 0);
                    }
                    beatHeart(heartbeatNum);
                }

                //TCP_Blaster(0, "A1 T90");
            }
        }

        public override void Closing()
        {
            if (handleTCPTask != null)
            {
                tokenSource.Cancel();
                DateTime closeRequestTime = DateTime.Now;
                while (!handleTCPTask.IsCompleted)
                {
                    if (DateTime.Now - closeRequestTime > TimeSpan.FromSeconds(10))
                    {
                        Debug.WriteLine("PodAudio: UDPListenerTask did not close.");
                        break;
                    }
                }
                handleTCPTask = null;

                tokenSource = new();
            }
        }

        void enableSensors()
        {
            Debug.WriteLine("Sensors Being enabled");
            for (int sNums = 0; sNums < sensorCount; sNums++)
            {
                SendStringToPodTCP("S" + sNums + " e1");
            }
            Debug.WriteLine("Sensors Are enabled");
        }

        void SendStringToPodTCP(string msg)//will add " \n" to the string passed in
        {
            Network.SendStringToPodTCP(msg);
        }

        int accelYLast = 0, accelXLast = 0, accelZLast = 0;
        int lRateLast = 0, rRateLast = 0;
        public int battery1 = 0, battery2 = 0;
        bool initializeCompleteFlag = false;
        [XmlIgnore]
        public int avgLoopTime = 0;
        void ParsePodMessage(string msg)
        {
            ModulePodConnection mpc = (ModulePodConnection)FindModule("PodConnection");
            if (mpc != null && podPaired == false)
            {
                if (mpc.podConnected == true)
                {
                    if (!initialized) Initialize();

                    podPaired = true;
                    Debug.WriteLine("POD TCP Connected!");
                    //configStringLines = configString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    //configStringLinePointer = 0;
                    //RunDownConfigString();                    
                }
            }
            //Initialization Complete
            //Sensor: 0; Enabled:0; Type:2; pollingPeriod:10; minChange:0; maxChange:10000;
            inputBuffer = msg;
            int lineBreak = inputBuffer.IndexOfAny(new char[] { '\n', '\r' });
            if (lineBreak == -1) return;  //every command should be termineated by a cr or lf                    

            inputBuffer = inputBuffer.TrimEnd('\0');
            inputBuffer = inputBuffer.TrimEnd('\n');
            inputBuffer = inputBuffer.TrimEnd('\r');
            //Debug.WriteLine(inputBuffer);
            if (inputBuffer.Length < 1) return;

            if (inputBuffer == "Initialization Complete" && !initializeCompleteFlag)
            {
                enableSensors();
                initializeCompleteFlag = true;
                return;
            }
            else if (!initializeCompleteFlag)
            {
                RunDownConfigString(inputBuffer);
                return;
            }

            //strip the first command off the head of the input buffer
            string inputLine = inputBuffer;
            //put everything you receive in the debug display but only process sensor input
            //string sndString = "RECD_from_TCP-> " + inputLine;
            //Debug.WriteLine(sndString);
            //get the podInterface which is needed to update sensor values for move and turn
            //TODO move this funcationality elsewhere
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");

            ModuleCollision moduleCollision = (ModuleCollision)FindModule(typeof(ModuleCollision));

            //ModuleUserInterface moduleUserInterface = (ModuleUserInterface)FindModule(typeof(ModuleUserInterface));

            //process the command
            string[] parameters = inputLine.Split(':');
            if (parameters[0] == "LoopTime")
            {
                parameters[1].TrimStart();
                int.TryParse(parameters[1].Substring(1), out int extractedTimeVar);
                avgLoopTime = extractedTimeVar;
                //Debug.WriteLine("LongestLoop: "+extractedTimeVar);
            }
            if (parameters[0] == "LoopCount")
            {
                parameters[1].TrimStart();
                int.TryParse(parameters[1].Substring(1), out int extractedTimeVar);
                avgLoopTime = extractedTimeVar;
                //Debug.WriteLine("Loops_Over_10mS: "+extractedTimeVar);
            }
            // is it a sensor value coming in?
            if (inputLine != "" && inputLine[0] == 'S' && parameters.Length == 2)
            {
                //parse out the two parameeters and exit on error
                if (!int.TryParse(parameters[0].Substring(1), out int senseNumber)) return;
                if (!long.TryParse(parameters[1], out long senseValue)) return;


                //sensorArray[0].configureSensor(1, 2, 10, 200, 1, 10000);    //Lpos
                //sensorArray[1].configureSensor(0, 3, 10, 200, 1, 10000);    //Lrate
                //sensorArray[2].configureSensor(0, 2, 10, 200, 1, 10000);    //Rpos
                //sensorArray[3].configureSensor(2, 3, 10, 200, 1, 10000);    //Rrate
                //sensorArray[4].configureSensor(5, 4, 20, 20, 0, 1);         //yaw
                //sensorArray[5].configureSensor(3, 4, 200, 200, 0, 1);       //roll
                //sensorArray[6].configureSensor(0, 4, 10, 100, 0, 60);       //accelX
                //sensorArray[7].configureSensor(1, 4, 10, 100, 0, 60);       //accelY
                //sensorArray[8].configureSensor(2, 4, 10, 100, 0, 60);       //accelZ
                //sensorArray[9].configureSensor(0, 6, 1000, 1000, 0, 0);	  //heartbeat		
                if (sensors.Count > 0)
                {
                    sensors[senseNumber].prevValue = sensors[senseNumber].currentValue;
                    sensors[senseNumber].currentValue = senseValue;
                    if (senseNumber == 4)//yaw
                    {
                        long prevValue = latestYawValue;
                        latestYawValue = senseValue;
                        sensors[senseNumber].currentValue = latestYawValue;
                        mpi.SenseTurn(Angle.FromDegrees(latestYawValue - prevValue));
                        //if(senseValue != latestYawValue)Debug.WriteLine("Yaw: " + latestYawValue);
                    }
                    else if (senseNumber == 0)//Lpos
                    {
                        if (latestPos != int.MaxValue) mpi.SenseMove(((senseValue - latestPos) / motionSensorRatio) / 2);//was 900
                        latestPos = (int)senseValue;
                        sensors[senseNumber].currentValue = senseValue;
                    }
                    else if (senseNumber == 2)//Rpos
                    {
                        if (lastRPos != int.MaxValue) mpi.SenseMove(((senseValue - lastRPos) / motionSensorRatio) / 2);//was 900
                        lastRPos = (int)senseValue;
                        sensors[senseNumber].currentValue = senseValue;
                    }
                    else if (senseNumber == 7)// accelY
                    {
                        if (accelYLast != (int)senseValue)
                        {
                            accelYLast = (int)senseValue;
                            if (moduleCollision != null && moduleCollision.isEnabled)
                                moduleCollision.CheckCollisionY(accelYLast, lRateLast, rRateLast);
                        }
                    }
                    else if (senseNumber == 6) // accelX
                    {
                        if (accelXLast != (int)senseValue)
                        {
                            accelXLast = (int)senseValue;
                            if (moduleCollision != null && moduleCollision.isEnabled)
                                moduleCollision.CheckCollisionX(accelXLast, lRateLast, rRateLast);
                        }
                    }
                    else if (senseNumber == 8)// accelZ
                    {
                        if (accelZLast != (int)senseValue)
                        {
                            //Debug.WriteLine(senseValue);
                            accelZLast = (int)senseValue;
                            if (moduleCollision != null && moduleCollision.isEnabled)
                                moduleCollision.CheckTilted(accelZLast);
                        }
                    }
                    else if (senseNumber == 1)//Lrate
                    {
                        lRateLast = (int)senseValue;
                    }
                    else if (senseNumber == 3)//Rrate
                    {
                        rRateLast = (int)senseValue;
                    }
                    else if (senseNumber == 10)
                    {
                        battery1 = (int)senseValue;
                        //if(moduleUserInterface != null && moduleUserInterface.IsEnabled())
                        //    moduleUserInterface.SetBatteryLevels(battery1, battery2);
                    }
                    else if (senseNumber == 11)
                    {
                        battery2 = (int)senseValue;
                    }
                }
            }
        }

        //***************************End Communications*********************************************

        CancellationTokenSource tokenSource = new();
        Task handleTCPTask;
        public override void Initialize()
        {
            motionSensorRatio = 420;
            if (isEnabled == false) return;
            podPaired = false;
            if (Network.InitTCP(podIPString) == false)
            {
                Debug.WriteLine("NOT Connected!");
                isEnabled = false;
                initialized = false;
                return;
            }
            initializeCompleteFlag = false;
            sensorCount = 0;
            sensors.Clear();
            actuatorCount = 0;
            actuators.Clear();

            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            if (mpi != null) mpi.isLive = true;

            Debug.WriteLine("Connected!");

            latestYawValue = 0;
            lastRPos = 0;
            latestPos = 0;
            configString = RetrieveConfigString();
            handleTCPTask = Task.Run(() =>
            {
                Thread.CurrentThread.Name = "TCP_Thread";
                PodLoopThread();
            }, tokenSource.Token);

            sensorCount = 0;
            actuatorCount = 0;
        }


        public override void SetUpAfterLoad()
        {
            Init();
            this.isEnabled = false;
            initialized = false;
        }


        private void StripActuatorData(string[] configParams, PodActuator aHandle)
        {
            const string tS = "Type";
            const string timS = "targetTime";
            const string enS = "Enabled";

            for (int i = 0; i < configParams.Length; i++)
            {
                switch (configParams[i])
                {
                    case tS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                aHandle.type = (actuatorType)int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case timS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                aHandle.timing = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case enS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                aHandle.isEnabled = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
        private void StripSensorData(string[] configParams, PodSensor sHandle)
        {
            const string tS = "Type";
            const string pollS = "pollingPeriod";
            const string repS = "reportingPeriod";
            const string enS = "Enabled";
            const string minS = "minChange";
            const string maxS = "maxChange";


            for (int i = 0; i < configParams.Length; i++)
            {
                switch (configParams[i])
                {
                    case tS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.type = (sensorType)int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case pollS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.pollingPeriod = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case repS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.reportingPeriod = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case enS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.isEnabled = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case minS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.minChange = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    case maxS:
                        {
                            string paramHandle = configParams[i + 1];
                            if (!string.IsNullOrEmpty(paramHandle))
                            {
                                sHandle.maxChange = int.Parse(paramHandle);
                                i++;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

            }
        }


    }
}