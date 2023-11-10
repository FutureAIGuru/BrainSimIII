//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Diagnostics;
using static System.Math;


namespace BrainSimulator.Modules
{
    public partial class ModulePod : ModuleBase
    {
        //*****************************************Variables**************************************
        int latestPos = int.MaxValue;
        int lastRPos = int.MaxValue;
        public float RealMoveRatio = 375;
        public float[] RealMoveRatios = { 500, 375 };

        private long latestYawValue = 0;

        private static long desiredAngle = 0;
        private static int desiredSpeed = 60;
        int moveTarget = 0;
        bool moveFlag = false;
        private static int fineErrorAdj = 60;
        private static int coarseErrorAdj = 80;
        private static int fineErrorAngle = 25;
        private static int coarseErrorAngle = 50;
        private System.DateTime intervalBeatLastPoll = System.DateTime.Now;

        public int FineErrorAngle {
            get { return fineErrorAngle; } 
            set { fineErrorAngle = value; } 
        }
        public int CoarseErrorAngle
        {
            get { return coarseErrorAngle; }
            set { coarseErrorAngle = value; }
        }
        public int FineErrorAdj
        {
            get { return fineErrorAdj; }
            set { fineErrorAdj = value; }
        }
        public int CoarseErrorAdj
        {
            get { return coarseErrorAdj; }
            set { coarseErrorAdj = value; }
        }      

        //------------------------------------------------------------------------------------------

        public int GetDesiredAngle()
        {
            return (int)desiredAngle;
        }
        public void SetDesiredAngle(int angle)
        {
            desiredAngle = angle;
        }
        public void SetDesiredMoveTarget(int target)
        {
            moveTarget = target;
        }
        public void DoTurn(int degrees)
        {
            desiredAngle += degrees;

            //linearStraightFlag = 1;
            SetPodBusy(true);
        }
        public void DoSpeed(int value)
        {
            desiredSpeed = value;
            if (targetSpeed != 0) //don't change the target speed if we're stopped
            {
                targetSpeed = desiredSpeed;
                linearStraightFlag = 0;
            }
        }
        public void DoMove(double distance) //Set distance to check for check move
        {
            if (distance < 0)
                targetSpeed = -desiredSpeed;
            else
                targetSpeed = desiredSpeed;

            int x = (int)distance;
            x = x * 500; //TODO replace with variable to compensate for gear ratio
            moveTarget = x + AvgPos();
            linearStraightFlag = 0;
            SetPodBusy(true);
        }

        public int AvgPos()
        {
            return (int)((latestPos + lastRPos) / 2f);
        }
        public void AbortMotion()
        {
            Stop();
        }

        public void StopMove()
        {
            moveTarget = AvgPos();
        }
        public void StopTurn()
        {
            desiredAngle = latestYawValue;
        }
        public void PrevAngleStop()
        {
            moveTarget = AvgPos();
            string stopLeft = "A0 T90";
            string stopRight = "A1 T90";
            SendStringToPodTCP(stopLeft);
            SendStringToPodTCP(stopRight);
            SetPodBusy(false);
            //Debug.WriteLine("PrevAngleStop Command Sent to Pod");
        }
        public void Stop()
        {
            desiredAngle = latestYawValue;
            moveTarget = AvgPos();
            string stopLeft = "A0 T90";
            string stopRight = "A1 T90";
            SendStringToPodTCP(stopLeft);
            SendStringToPodTCP(stopRight);
            SetPodBusy(false);
            //Debug.WriteLine("Stop Command Sent to Pod");
        }
       
        void CheckMove()
        {
            int avgPos = AvgPos();            
            if (targetSpeed > 0)
            {                
                if (moveTarget < avgPos) //case stop
                {
                    PrevAngleStop();
                    targetSpeed = 0;
                    moveFlag = false;
                    Debug.WriteLine("Stop moveFwd: " + avgPos + "Target:" + moveTarget);
                    return;
                }
                else if(moveFlag == false) // case start
                {
                    string leftW = wheelStringMaker(0, (targetSpeed + 90));
                    string rightW = wheelStringMaker(1, (targetSpeed + 90));
                    string compound = leftW + " \n" + rightW;
                    moveFlag = true;
                    SendStringToPodTCP(compound);
                }
                //SetPodBusy(true);         
            }
            if (targetSpeed < 0)
            {
                if (moveTarget > avgPos) //stop case
                {
                    //SetPodBusy(true);
                    PrevAngleStop();
                    targetSpeed = 0;
                    moveFlag = false;
                    //bckFlag = false;
                    Debug.WriteLine("Stop moveBck: " + avgPos + "Target:" + moveTarget);
                    return;
                }
                else if(moveFlag == false) //start case
                {
                    string leftW = wheelStringMaker(0, (targetSpeed + 90));
                    string rightW = wheelStringMaker(1, (targetSpeed + 90));
                    string compound = leftW + " \n" + rightW;
                    moveFlag = true;
                    SendStringToPodTCP(compound);
                }                
            }
            
        }

        private string wheelStringMaker(int wheelNum, int speed)//Left is 0, Right is 1
        {
            if (speed > 180) speed = 180;
            if (speed < 0) speed = 0;
            string retStr = "A" + wheelNum + " T" + speed;
            return retStr;
        }      

        
        public void TCP_Blaster(int interval_in_millis, string outgoing_payload)
        {
            System.TimeSpan blasterInterval = System.DateTime.Now - intervalBeatLastPoll;

            if (blasterInterval > System.TimeSpan.FromMilliseconds(interval_in_millis))
            {
                SendStringToPodTCP(outgoing_payload); //??? perhaps I need to get the sensor from the sensor array  but i need to loop through and make sure it stores the new type to match up to firmware config                
                intervalBeatLastPoll = System.DateTime.Now;
            }            
        }

        int prevLSpeed = 90;
        int prevRSpeed = 90;
        int linearStraightFlag = 0;        
        public void linearStraightCheck()
        {                                    
            int currLSpeed;
            int currRSpeed;
            int margin = 3; // deg
            int minSpeedChange = 0;
            int coarseError = coarseErrorAngle;//deviation angle that triggers on a dime turning
            double errConstant = desiredSpeed/coarseError;
            int fineError = fineErrorAngle;//deviation angle that triggers more aggressive drift-like turning
            int fineErrorconstant = desiredSpeed/fineErrorAdj;//correction when the deviation error is more than fineError^^
            //double errConstant = 0.6;
            int currErr = (int)(latestYawValue - desiredAngle);            ;            

            if (targetSpeed > 0) //forward
            {                
                if (currErr > fineError)
                {
                    errConstant = fineErrorconstant;
                }
                if (currErr > (0+margin))
                {
                    int leftError = (int)(errConstant*currErr);                    
                    currLSpeed = (90+targetSpeed) - leftError;                    
                    if (currLSpeed < 90-targetSpeed || currErr > coarseError)
                    {
                        currLSpeed = 90-targetSpeed;
                    }
                    if (currLSpeed > 90+targetSpeed)
                    {
                        currLSpeed = 90+targetSpeed;
                    }
                    int lSpeedErr = Abs(currLSpeed - prevLSpeed);
                    if (lSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, currLSpeed); 
                        string rightWheelString = wheelStringMaker(1, (90+targetSpeed)); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        SendStringToPodTCP(compound);                        
                        //Debug.WriteLine("rightcurrError: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                    }
                    linearStraightFlag = 0;
                    prevLSpeed = currLSpeed;
                    return;
                }
                else if (currErr < (0-margin))
                {
                    if (currErr < -fineError)
                    {
                        errConstant = fineErrorconstant;
                    }
                    int rightError = (int)(errConstant*currErr);
                    currRSpeed = (90+targetSpeed) + rightError;
                    if (currRSpeed < 90 - targetSpeed || currErr < -coarseError)
                    {
                        currRSpeed = 90 - targetSpeed;
                    }
                    if (currRSpeed > 90+targetSpeed)
                    {
                        currRSpeed = 90+targetSpeed;
                    }
                    int rSpeedErr = Abs(currRSpeed - prevRSpeed);
                    if (rSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, (90+targetSpeed)); 
                        string rightWheelString = wheelStringMaker(1, currRSpeed); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        SendStringToPodTCP(compound);                        
                        //Debug.WriteLine("leftError: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                    }
                    linearStraightFlag = 0;
                    prevRSpeed = currRSpeed;
                    return;
                }
                else if (linearStraightFlag == 0)
                {
                    string leftWheelString = wheelStringMaker(0, (90 + targetSpeed)); 
                    string rightWheelString = wheelStringMaker(1, (90 + targetSpeed)); 
                    string compound = leftWheelString + " \n" + rightWheelString;
                    SendStringToPodTCP(compound);
                    linearStraightFlag = 1;
                    prevLSpeed = targetSpeed;
                    prevRSpeed = targetSpeed;
                    return;
                }
            }
            if (targetSpeed < 0) //backwards
            {
                
                if (currErr < (0-margin))
                {                    
                    if (currErr < -fineError)
                    {
                        errConstant = fineErrorconstant;
                    }
                    int leftError = (int)(errConstant*currErr);
                    currLSpeed = 90+(targetSpeed - leftError);                    
                    if (currLSpeed < 90+targetSpeed)
                    {
                        currLSpeed = 90+targetSpeed;
                    }
                    if (currLSpeed > 90-targetSpeed || currErr < -coarseError)
                    {
                        currLSpeed = 90-targetSpeed;
                    }
                    int lSpeedErr = Abs(currLSpeed - prevLSpeed);
                    if (lSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, currLSpeed); 
                        string rightWheelString = wheelStringMaker(1, (90+targetSpeed)); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        SendStringToPodTCP(compound);
                        //Debug.WriteLine("right: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                    }
                    linearStraightFlag = 0;
                    prevLSpeed = currLSpeed;
                    return;
                }
                else if (currErr > (0+margin))
                {
                    if (currErr > fineError)
                    {
                        errConstant = fineErrorconstant;
                    }
                    int rightError = (int)(errConstant*currErr);
                    currRSpeed = 90+(targetSpeed + rightError);
                    if (currRSpeed < 90+targetSpeed)
                    {
                        currRSpeed = 90+targetSpeed;
                    }
                    if (currRSpeed > 90-targetSpeed || currErr > coarseError)
                    {
                        currRSpeed = 90-targetSpeed;
                    }
                    int rSpeedErr = Abs(currRSpeed - prevRSpeed);
                    if (rSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, (90+targetSpeed)); 
                        string rightWheelString = wheelStringMaker(1, currRSpeed); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        SendStringToPodTCP(compound);
                        //Debug.WriteLine("left: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                    }
                    linearStraightFlag = 0;
                    prevRSpeed = currRSpeed;
                    return;
                }
                else if (linearStraightFlag == 0)
                {
                    string leftWheelString = wheelStringMaker(0, (90 + targetSpeed)); 
                    string rightWheelString = wheelStringMaker(1, (90 + targetSpeed)); 
                    string compound = leftWheelString + " \n" + rightWheelString;
                    SendStringToPodTCP(compound);
                    linearStraightFlag = 1;
                    prevLSpeed = targetSpeed;
                    prevRSpeed = targetSpeed;
                    return;
                }
            }
            else
            {
                errConstant = 1;
                int oppositeWheelSpeed = 0; // so that when the desired speed is 0 we can turn closer to center rather than only moving one wheel
                if (currErr < (0-margin))
                {
                    SetPodBusy(true);
                    int leftError = (int)(errConstant*currErr);
                    if (leftError > 0)leftError = 0;
                    if (leftError < -90) leftError = -90;
                    currLSpeed = (90+targetSpeed) - leftError;
                    oppositeWheelSpeed = (90+targetSpeed) + leftError;
                    if (currLSpeed < 1)
                    {
                        currLSpeed = 0;
                    }
                    int lSpeedErr = Abs(currLSpeed - prevLSpeed);
                    if (lSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, currLSpeed); 
                        string rightWheelString = wheelStringMaker(1, (oppositeWheelSpeed)); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        //Debug.WriteLine("rightcurrError: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                        SendStringToPodTCP(compound);
                    }
                    linearStraightFlag = 0;
                    prevLSpeed = currLSpeed;
                }
                else if (currErr > (0+margin))
                {
                    SetPodBusy(true);
                    int rightError = (int)(errConstant*currErr);
                    if (rightError < 0) rightError = 0;
                    if (rightError > 90) rightError = 90;
                    currRSpeed = (90+targetSpeed) + rightError;
                    oppositeWheelSpeed = (90+targetSpeed) - rightError;
                    if (currRSpeed < 1)
                    {
                        currRSpeed = 0;
                    }
                    int rSpeedErr = Abs(currRSpeed - prevRSpeed);
                    if (rSpeedErr > minSpeedChange)
                    {
                        string leftWheelString = wheelStringMaker(0, (oppositeWheelSpeed)); 
                        string rightWheelString = wheelStringMaker(1, currRSpeed); 
                        string compound = leftWheelString + " \n" + rightWheelString;
                        //Debug.WriteLine("rightcurrError: "+currErr+"\n");
                        //Debug.WriteLine(compound);
                        SendStringToPodTCP(compound);
                    }
                    linearStraightFlag = 0;
                    prevRSpeed = currRSpeed;
                }
                else if (linearStraightFlag == 0)
                {
                    //SetPodBusy(false);
                    PrevAngleStop();
                    string leftWheelString = wheelStringMaker(0, (90)); 
                    string rightWheelString = wheelStringMaker(1, (90)); 
                    string compound = leftWheelString + " \n" + rightWheelString;
                    SendStringToPodTCP(compound);                    
                    linearStraightFlag = 1;
                    prevLSpeed = targetSpeed;
                    prevRSpeed = targetSpeed;
                }                
            } //stopped
        }



    }
}