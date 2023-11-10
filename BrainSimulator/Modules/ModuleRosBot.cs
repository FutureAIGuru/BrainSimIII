//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using static System.Math;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{        
    public class RosBot
    {
        //####################################Containers#########################################
        [XmlIgnore]
        public IPAddress botIP = IPAddress.Parse("192.168.0.103");
        private int botPort = 9090;
        string subscribeToArm = "{\"op\": \"subscribe\", \"topic\": \"/TargetAngle\"}";
        string subOdomCmd = "{\"op\": \"subscribe\", \"topic\": \"/odom\"}";
        string publishToArm = "/TargetAngle";
        string publishToBase = "/cmd_vel";
        enum rosbotSenseType {none, servoPosition, motorPosition, imuSensor, lidarSensor};

        enum rosbotactuatorType {driveMotor, armServo};
        public class rosbotSensor
        {
            public string name;
            public double currentValue;
            public bool isEnabled = false;
            int id = 0;
            rosbotSenseType type;            
        }
        public class velocitySensor : rosbotSensor
        {            
            public void velocity_Accumulator(double inputVel)
            {
                if (Abs(inputVel)*3.21 >= 0.1)
                {
                    currentValue -= inputVel*3.21;
                }
            }
        }
        public class rosbotActuator
        {
            public string name;
            public int currentValue;
            public bool isEnabled = false;
            int id = 0;
            rosbotactuatorType type;
        }
        List<rosbotSensor> sensorArray = new();
        List<velocitySensor> velocityTracker = new();
        [XmlIgnore]
        public velocitySensor xSense = new();
        [XmlIgnore]
        public velocitySensor ySense = new();
        [XmlIgnore]
        public velocitySensor zSense = new();                
            
        List<rosbotActuator> actuatorArray = new();
        //#######################################End of Containers##############################
        //######################################Functions#######################################
        public string getPublishToArmString(int id, int angle)//need a properly formatted string for ros to drive the arm to set angle, each id is a servo chained together starting with 1 the rotating base up to 6 being the gripper
        {
            if(id > 6)id = 6;
            if(id < 0)id = 0;
            if(angle > 180) angle=180;
            if (angle < 0) angle=0;
            return ("{\"op\": \"publish\", \"topic\": \"/TargetAngle\", \"msg\": {\"id\": "+id+",\"angle\": "+angle+"}}");
        }
        public string getPublishToBaseString(int CommandXYZ, float speed)//CommandXYZ takes 0,1,2 for x,y,z. x is fwd/bck, y is sideways, z is turning. Default Call will force CommandXYZ to be 0/X.
        {
            string defaultReturn = "{\"op\": \"publish\", \"topic\": \"/cmd_vel\", \"msg\": {\"linear\":{\"x\":0.0, \"y\":0.0,\"z\":0.0}, \"angular\":{\"x\":0.0,\"y\":0.0,\"z\":0.0} }}";
            if (CommandXYZ != 0 && CommandXYZ != 1 && CommandXYZ != 2) return defaultReturn;
            switch (CommandXYZ)
            {
                case 0:
                    {                        
                        return ("{\"op\": \"publish\", \"topic\": \"/cmd_vel\", \"msg\": {\"linear\": {\"x\": "+speed+", \"y\": 0.0,\"z\": 0.0}, \"angular\": {\"x\": 0.0,\"y\": 0.0,\"z\": 0.0} }}");
                        break;
                    }
                case 1:
                    {
                        return ("{\"op\": \"publish\", \"topic\": \"/cmd_vel\", \"msg\": {\"linear\": {\"x\": 0.0, \"y\": "+speed+",\"z\": 0.0}, \"angular\": {\"x\": 0.0,\"y\": 0.0,\"z\": 0.0} }}");
                        break;
                    }
                case 2:
                    {
                        return ("{\"op\": \"publish\", \"topic\": \"/cmd_vel\", \"msg\": {\"linear\": {\"x\": 0.0, \"y\": 0.0,\"z\": 0.0}, \"angular\": {\"x\": 0.0,\"y\": 0.0,\"z\": "+speed+"} }}");
                        break;
                    }
                default:
                    return defaultReturn;
            }

        }
        public string getPublishDiagonalToBaseString(float xSpeed, float ySpeed)
        {
            return "{\"op\": \"publish\", \"topic\": \"/cmd_vel\", \"msg\": {\"linear\": {\"x\": "+xSpeed+", \"y\": "+ySpeed+",\"z\": 0.0}, \"angular\": {\"x\": 0.0,\"y\": 0.0,\"z\": 0.0} }}";
        }
        public bool sendCommand_UDP(string message)
        {
            return Network.UDP_Send(message, botIP, botPort);
        }
        public bool setBotIP(string ipIn)//Setup the publishers and subscribers needed to get info
        {            
            bool retVal = IPAddress.TryParse(ipIn, out IPAddress temp);
            if (retVal)
            {
                botIP = temp;
            }
            else
            {
                return false;
            }            
            return true;
        }                
        public void setupInitPubSub()
        {
            Network.UDP_Setup_Send(subOdomCmd, botIP, botPort);
            Network.UDP_Setup_Send(subscribeToArm, botIP, botPort);
        }
    }
    public class ModuleRosBot : ModuleBase
    {
        public RosBot botToUse;
        public void moveArm(int id, int angle)
        {
            botToUse.sendCommand_UDP(botToUse.getPublishToArmString(id, angle));
        }
        public void moveBase(int XYZ, float speed)//{X,Y,Z} correspond to {0,1,2} for direction X is fwd/back, y is side-side, z is turning
        {
            botToUse.sendCommand_UDP(botToUse.getPublishToBaseString(XYZ, speed));
        }
        public void moveBaseDiagonal(float xSpeed, float ySpeed)
        {
            botToUse.sendCommand_UDP(botToUse.getPublishDiagonalToBaseString(xSpeed, ySpeed));
        }
        void subscribeToLidar()
        {
            string subCmd = "{\"op\": \"subscribe\", \"topic\": \"/scan\"}";
            botToUse.sendCommand_UDP(subCmd);
        }
        static void HandleArmAngles(JsonNode baseNode)
        {
            JsonNode angleD = baseNode["msg"]["angle"];
            JsonNode armID = baseNode["msg"]["id"];            
        }
        void HandleOdomValues(JsonNode baseNode)
        {
            JsonNode xVel = baseNode["msg"]["twist"]["twist"]["linear"]["x"];
            JsonNode yVel = baseNode["msg"]["twist"]["twist"]["linear"]["y"];
            JsonNode zVel = baseNode["msg"]["twist"]["twist"]["angular"]["z"];
            botToUse.xSense.velocity_Accumulator((double) xVel);
            botToUse.ySense.velocity_Accumulator((double) yVel);
            botToUse.zSense.velocity_Accumulator((double) zVel);            
            UpdateDialog();         
        }        
        void parseJSONInfo(string dgramInput)
        {            
            JsonNode baseNode = JsonNode.Parse(dgramInput);
            var options = new JsonSerializerOptions { WriteIndented = true };
            //Debug.WriteLine(baseNode!.ToJsonString(options));
            JsonNode topic = baseNode!["topic"];            
            if (topic.GetValue<string>() == "/TargetAngle")
            {
                HandleArmAngles(baseNode);
            }
            if (topic.GetValue<string>()=="/odom")
            {
                HandleOdomValues(baseNode);
            }
            return;
        }
        void handle_UDP_packet()
        {            
            var fromIP = new IPEndPoint(IPAddress.Parse("192.168.0.103"), 9090);
            for (; ; )
            {
                var recvBuffer = Network.receiveSubscribedMessages;
                var datagramIn = recvBuffer.Receive(ref fromIP);
                string passInto = "";
                for (int i = 0; i<datagramIn.Length;i++)
                {
                    passInto += (char)datagramIn[i];
                }
                parseJSONInfo(passInto);                
            }
        }
        public override void Fire()
        {
            Init();
        }
        public void resetArm()
        {
            moveArm(1, (int)90);
            moveArm(2, (int)180);
            moveArm(3, (int)0);
            moveArm(4, (int)0);
            moveArm(5, (int)90);
            moveArm(6, (int)45);
        }
        CancellationTokenSource cts = new();
        Task rosbotUDPTask;
        public override void Initialize()
        {            
            if (rosbotUDPTask == null)
            {
                botToUse = new();
                botToUse.setupInitPubSub();
                resetArm();                   
                rosbotUDPTask = Task.Run(() => {
                    Thread.CurrentThread.Name = "RosBotUDP";
                    handle_UDP_packet();
                }, cts.Token);
            }
        }
    }
}