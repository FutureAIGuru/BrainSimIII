//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModulePodInterface : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive

        public int MinSpeed { get => minSpeed; }
        public int MaxSpeed { get => maxSpeed; }

        private Thing ThingBodyPosition = null;
        private Thing ThingBodyAngle = null;
        private Thing ThingCameraPan = null;
        private Thing ThingCameraTilt = null;

        // These are only getters, and so are not part of the split command structure
        public Point3DPlus HeadPosition { get => Sallie.HeadPosition; }
        public Vector3D BodyUpDirection { get => Sallie.BodyUpDirection; }
        public Vector3D BodyDirection { get => Sallie.BodyDirection; }
        public Vector3D HeadDirection { get => Sallie.HeadDirection; }
        public double lastMoved { get => Sallie.lastMoved; }
        Queue<object> commandQueue = new Queue<object>();

        private bool selfLoaded = false;

        [XmlIgnore]
        public bool isLive = false;  // gets set from the PodInterface dialog checkbox

        DateTime lastMove;
        float moveStep = 1;
        Angle turnStep = Angle.FromDegrees(5);
        Angle tiltStep = Angle.FromDegrees(1);
        Angle panStep = Angle.FromDegrees(1);
        int i = 0;
        float lastturnposition = 0f;

        bool timedPause = false;
        bool movedPause = false;
        bool first = true;
        float wait = 0;
        DateTime timePauseStarted;

        int minSpeed = 20, maxSpeed = 90;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModulePodInterface()
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

            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));

            if (!selfLoaded)
            {
                InitializeSallieSelf();
                selfLoaded = true;
            }


            if (movedPause)
            {

                ModuleRangeThing range = (ModuleRangeThing)FindModule(typeof(ModuleRangeThing));
                ModulePodCamera cam = (ModulePodCamera)FindModule(typeof(ModulePodCamera));
                //Thing podDelta = UKS.GetOrAddThing("PodDeltaMove", "Self");
                if (!isLive)
                {
                    movedPause = false;
                    return;
                }
                if (first)
                {
                    range.LastTurnPosition = 0;
                    cam.fullDelta = 0;
                    first = false;
                }
                //full delta is a measure of how far a pod moves, but only works while image recognition is active
                //wait contains distance to move in inches durring movedPause
                if (cam.fullDelta > range.LastTurnPosition + wait)
                {
                    movedPause = false;
                    first = true;
                    //waitTime = 0;
                }
                else
                {
                    return;
                }

            }

            if (timedPause)
            {
                //wait contains time in milliseconds durring timedPause
                double miliSecondsPaused = (DateTime.Now - timePauseStarted).TotalMilliseconds;
                if (miliSecondsPaused > wait)
                {
                    timedPause = false;
                    // waitTime = 0;
                }
                else
                {
                    return;
                }
            }

            //cap move speed to prevent command overload
            TimeSpan timePerStep = TimeSpan.FromMilliseconds(100);
            if (!isLive && DateTime.Now - lastMove <= timePerStep) return;
            lastMove = DateTime.Now;

            //process any commands in the queue
            if (commandQueue.Count != 0)
            {
                object command = new();
                if (!commandQueue.TryPeek(out command))
                    return;
                if (command is ValueTuple<String, float> tuple)
                {

                    if (!IsPodBusy() && tuple.Item1 == "Move")
                    {
                        commandQueue.Dequeue();
                        float dist = tuple.Item2;
                        CommandMove(dist, true);
                    }
                    if (!IsPodBusy() && tuple.Item1 == "Speed")
                    {
                        commandQueue.Dequeue();
                        CommandSpeed(tuple.Item2, true);
                    }
                    if (tuple.Item1 == "MovingSpeed")
                    {
                        commandQueue.Dequeue();
                        CommandSpeed(tuple.Item2, true);
                    }
                    if (tuple.Item1 == "Pause")
                    {
                        commandQueue.Dequeue();
                        CommandPause(tuple.Item2, true);
                    }
                    if (tuple.Item1 == "MovingPause")
                    {
                        commandQueue.Dequeue();
                        MovingPause(tuple.Item2);
                    }
                }
                else if (command is ValueTuple<String, Angle> tuple1)
                {
                    if (!IsPodBusy() && tuple1.Item1 == "Pan")
                    {
                        commandQueue.Dequeue();
                        CommandPan(tuple1.Item2, false, true);
                    }
                    else if (!IsPodBusy() && tuple1.Item1 == "Tilt")
                    {
                        commandQueue.Dequeue();
                        CommandTilt(tuple1.Item2, false, true);
                    }

                    else if (tuple1.Item1 == "MovingPan")
                    {
                        commandQueue.Dequeue();
                        CommandPan(tuple1.Item2, false, true);
                    }

                    else if (tuple1.Item1 == "MovingTilt")
                    {
                        commandQueue.Dequeue();
                        CommandTilt(tuple1.Item2, false, true);
                    }

                    else if (!IsPodBusy() && tuple1.Item1 == "Turn")
                    {
                        commandQueue.Dequeue();
                        CommandTurn(tuple1.Item2, true);
                    }

                    else if (tuple1.Item1 == "MovingTurn")
                    {
                        commandQueue.Dequeue();
                        CommandTurn(tuple1.Item2, true);
                    }


                }
                else if (command is ValueTuple<String, string> tuple2)
                {
                    if (tuple2.Item1 == "ImmediateSound")
                    {
                        commandQueue.Dequeue();
                        Audio.PlaySoundEffect(tuple2.Item2);
                    }
                    if (!IsPodBusy() && tuple2.Item1 == "QueueSound")
                    {
                        commandQueue.Dequeue();
                        Audio.PlaySoundEffect(tuple2.Item2);
                    }
                    if (tuple2.Item1 == "Speech")
                    {
                        ModuleSpeechOut Speech = (ModuleSpeechOut)FindModule(typeof(ModuleSpeechOut));
                        commandQueue.Dequeue();
                        Speech.SpeakText(tuple2.Item2);
                    }
                }
                else if (command is string stop)
                {
                    if (stop == "QueueStop")
                    {
                        commandQueue.Dequeue();
                        CommandStop(false);
                    }
                    if (stop == "QueueStopMove")
                    {
                        commandQueue.Dequeue();
                        CommandStopMove();
                    }
                    if (stop == "QueueStopTurn")
                    {
                        commandQueue.Dequeue();
                        CommandStopTurn();
                    }
                    if (stop == "QueueReset")
                    {
                        commandQueue.Dequeue();
                        ResetCamera();
                    }
                }
                else if (command is ValueTuple<String, ValueTuple<int, int, int>> tuple3)
                {
                    if (tuple3.Item1 == "SetLED")
                    {
                        commandQueue.Dequeue();
                        ValueTuple<int, int, int> values = tuple3.Item2;
                        SetLED(values.Item1, values.Item2, values.Item3);
                    }

                }
                else if (command is ValueTuple<String, int> tuple4)
                {
                    ModulePod pod = (ModulePod)FindModule("Pod");
                    if (tuple4.Item1 == "FineAngle")
                    {
                        commandQueue.Dequeue();
                        pod.FineErrorAngle = tuple4.Item2;
                    }

                    if (tuple4.Item1 == "FineAdj")
                    {
                        commandQueue.Dequeue();
                        pod.FineErrorAdj = tuple4.Item2;
                    }

                    if (tuple4.Item1 == "CoarseAngle")
                    {
                        commandQueue.Dequeue();
                        pod.CoarseErrorAngle = tuple4.Item2;
                    }

                    if (tuple4.Item1 == "CoarseAdj")
                    {
                        commandQueue.Dequeue();
                        pod.CoarseErrorAdj = tuple4.Item2;
                    }

                }

            }

            //if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();

        }

        //public void QueueCommand(object x)
        //{
        //    commandQueue.Enqueue(x);
        //}

        public void QueueSound(string soundfile)
        {
            commandQueue.Enqueue(("QueueSound", soundfile));
        }
        public void ImmediateSound(string soundfile)
        {
            commandQueue.Enqueue(("ImmediateSound", soundfile));
        }
        public void Speech(string phrase)
        {
            commandQueue.Enqueue(("Speech", phrase));
        }

        public void AlertMove(Point3DPlus Move)
        {
            //call alert sound
            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (Audio != null)
                Audio.PlaySoundEffect("SallieConfirm1.wav");
            ModuleGoTo moduleGoTo = (ModuleGoTo)FindModule("GoTo");
            moduleGoTo.RouteToPoint(Move);
        }

        public void QueueLED(Tuple<int,int,int> RGB)//int R, int G, int B)
        {
            var (R, B, G) = RGB;
            commandQueue.Enqueue(("SetLED", (R, G, B)));
        }
        internal void SetLED(int R, int G, int B)
        {
            ModulePod mp = (ModulePod)FindModule("Pod");
            if (mp == null) return;

            mp.ChangeLED(R, G, B);
        }
        public void QueueFineAngle(int x)
        {
            commandQueue.Enqueue(("FineAngle", x));
        }
        public void QueueFineAdj(int x)
        {
            commandQueue.Enqueue(("FineAdj", x));
        }
        public void QueueCoarseAngle(int x)
        {
            commandQueue.Enqueue(("CoarseAngle", x));
        }
        public void QueueCoarseAdj(int x)
        {
            commandQueue.Enqueue(("CoarseAdj", x));
        }

        //Detects movement then updates the mental position;

        public override void Initialize()
        {
            if (ThingBodyPosition != null)
                InitializeSallieSelf();

            UpdateSallieSelf();
        }

        public bool QueueIsEmpty()
        {
            return commandQueue.Count == 0;
        }

        public void QueueSpeed(float speed)
        {
            commandQueue.Enqueue(("MovingSpeed", speed));
        }
        public void CommandSpeed(float speed, bool immediate = false)
        {
            if (immediate)
            {
                if (!isLive) return;
                ModulePod pod = (ModulePod)FindModule("Pod");
                if (pod == null) return;
                pod.DoSpeed((int)speed);
                return;
            }
            commandQueue.Enqueue(("Speed", speed));
        }

        public void CommandMove(float distance, bool immediate = false, bool ObsCheck = true)
        {
            GetUKS();
            if (immediate)
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if(CQ.Recording)
                    {
                        if (CQ.Live)
                        {
                            Thing temp = new();
                            temp.Label = "Move";
                            temp.V = distance;
                            CQ.RecList.Add(temp);
                            CQ.PauseHandler();
                        }
                        else
                        {
                            //create a list to store the command here
                            Thing temp = new();
                            temp.Label = "Move";
                            temp.V = distance;
                            CQ.RecList.Add(temp);
                        }
                        //maybe add info to recording texbox
                    }
                }
                if (!isLive)
                {
                    SenseMove(distance);
                    return;
                }
                ModulePod pod = (ModulePod)FindModule("Pod");
                if (pod == null) return;
                pod.DoMove(distance);
                return;
            }
            else if (ObsCheck)//queue the command
            {

                Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
                foreach (Thing T in mentalModel.Children)
                {
                    var test = T.GetRelationshipsAsDictionary();
                    if (test.ContainsKey("ang"))
                    {
                        var p = test["ang"];

                        if (p is Angle p1)
                        {
                            if (test.ContainsKey("cen"))
                            {
                                var q = test["cen"];
                                if (distance > 0)
                                {
                                    if (q is Point3DPlus q1)
                                    {
                                        if (q1.Theta > 0)
                                        {
                                            if (q1.Theta - p1 > Angle.FromDegrees(0))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                TryMove(ref distance, q1);
                                                return;
                                            }
                                            //send alert, then attempt to path around obstacle
                                        }
                                        else if (q1.Theta < 0)
                                        {
                                            if (q1.Theta + p1 < Angle.FromDegrees(0))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                TryMove(ref distance, q1);
                                                return;
                                            }
                                            //send alert, then attempt to path around obstacle
                                        }
                                        else if (q1.Theta == 0)
                                        {
                                            Point3DPlus goal = new Point3DPlus(distance, (Angle)0, (Angle)0);
                                            //goal should always be directly ahead from 0, as the pod position is the base

                                            TryMove(ref distance, goal);
                                            return;
                                        }
                                    }
                                }
                                else if (distance < 0)
                                {
                                    if (q is Point3DPlus q1)
                                    {

                                        if (q1.Theta > 0)
                                        {
                                            if (q1.Theta + p1 < Angle.FromDegrees(180))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                TryMove(ref distance, q1);
                                                return;
                                            }
                                            //send alert, then attempt to path around obstacle
                                        }
                                        else if (q1.Theta < 0)
                                        {
                                            if (q1.Theta - p1 > Angle.FromDegrees(-180))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                TryMove(ref distance, q1);
                                                return;
                                            }
                                            //send alert, then attempt to path around obstacle
                                        }
                                        else if (q1.Theta == 0)
                                        {
                                            Point3DPlus goal = new Point3DPlus(distance, (Angle)0, (Angle)0);
                                            //goal should always be directly ahead from 0, as the pod position is the base

                                            TryMove(ref distance, goal);
                                            return;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (!isLive && Abs(distance) > moveStep)
            {
                commandQueue.Enqueue(("Move", (distance > 0) ? moveStep : -moveStep));

                distance -= (distance > 0) ? moveStep : -moveStep;
            }
            if (distance != 0)
            {
                commandQueue.Enqueue(("Move", distance));
            }
        }

        private void TryMove(ref float distance, Point3DPlus q1)
        {

            if (Abs(q1.R) > Abs(distance))
            {
                if (!isLive)
                {
                    while (Abs(distance) > moveStep)
                    {
                        commandQueue.Enqueue(("Move", (distance > 0) ? moveStep : -moveStep));

                        distance -= (distance > 0) ? moveStep : -moveStep;
                    }
                    return;
                }
                commandQueue.Enqueue(("Move", distance));
                return;
            }
            Point3DPlus goal = new Point3DPlus(distance, (Angle)0, (Angle)0);
            //goal should always be directly ahead from 0, as the pod position is the base
            AlertMove(goal);
            return;
        }

        public void CommandTurn(Angle a, bool immediate = false, bool movingTurn = false)
        {
            if (immediate)
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if (CQ.Recording)
                    {
                        if (CQ.Live)
                        {
                            Thing temp = new();
                            temp.Label = "MovingTurn";
                            temp.V = a.ToDegrees();
                            CQ.RecList.Add(temp);
                            CQ.PauseHandler();
                        }
                        else
                        {
                            //create a list to store the command here
                            Thing temp = new();
                            temp.Label = "Turn";
                            temp.V = a.ToDegrees();
                            CQ.RecList.Add(temp);
                            //maybe add info to recording texbox
                        }
                    }
                }
                if (!isLive)
                {
                    SenseTurn(a);
                    return;
                }
                ModulePod pod = (ModulePod)FindModule("Pod");
                if (pod == null) return;
                pod.DoTurn((int)a.ToDegrees());
                return;
            }
            //only do the steps if there is no pod enabled
            while (!isLive && Abs(a) > turnStep)
            {
                if (movingTurn)
                {
                    Angle item = (a > 0) ? turnStep : -turnStep;
                    commandQueue.Enqueue(("MovingTurn", item));
                    a -= (a > 0) ? turnStep : -turnStep;
                }
                else
                {
                    Angle item = (a > 0) ? turnStep : -turnStep;
                    commandQueue.Enqueue(("Turn", item));
                    a -= (a > 0) ? turnStep : -turnStep;
                }
            }
            if (a != 0)
            {
                if (movingTurn == true)
                {
                    commandQueue.Enqueue(("MovingTurn", a));
                }
                else
                    commandQueue.Enqueue(("Turn", a));

            }
        }
        [XmlIgnore]
        public bool tiltInverted = true;
        public void CommandTilt(Angle target, bool relative = false, bool immediate = false, bool movingTilt = false)
        {
            Angle currentTilt = GetCameraTilt();
            if (relative)
            {
                target += currentTilt;
            }
            if (immediate)
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if (CQ.Recording)
                    {
                        if (CQ.Live)
                        {
                            Thing temp = new();
                            temp.Label = "MovingTilt";
                            temp.V = target.ToDegrees();
                            CQ.RecList.Add(temp);
                            CQ.PauseHandler();
                        }
                        else
                        {
                            //create a list to store the command here
                            Thing temp = new();
                            temp.Label = "Tilt";
                            temp.V = target.ToDegrees();
                            CQ.RecList.Add(temp);
                            //maybe add info to recording texbox
                        }
                    }
                }
                SenseCameraTilt(target, false);
                if (!isLive) return;
                ModulePod pod = (ModulePod)FindModule("Pod");
                if (pod == null) return;
                if (tiltInverted)
                    pod.tilt(-target);
                else
                    pod.tilt(target);
                return;
            }

            //pan/tilt commands are queued as absolute
            //only do the steps if there is no pod enabled
            while (!isLive && Abs(target - currentTilt) >= tiltStep)
            {
                currentTilt += (target > currentTilt) ? tiltStep : -tiltStep;
                if (movingTilt)
                {
                    commandQueue.Enqueue(("MovingTilt", currentTilt));
                }
                else
                    commandQueue.Enqueue(("Tilt", currentTilt));
            }
            if (target != currentTilt)
            {
                if (movingTilt)
                {
                    commandQueue.Enqueue(("MovingTilt", target));
                }
                else
                    commandQueue.Enqueue(("Tilt", target));
            }

        }
        public void CommandPan(Angle target, bool relative = false, bool immediate = false, bool movingPan = false)
        {
            //pan/tilt commands are queued as absolute
            Angle currentPan = GetCameraPan();
            if (relative)
                target += currentPan;

            if (immediate)
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if (CQ.Recording)
                    {
                        if (CQ.Live)
                        {
                            Thing temp = new();
                            temp.Label = "MovingPan";
                            temp.V = target.ToDegrees();
                            CQ.RecList.Add(temp);
                            CQ.PauseHandler();
                        }
                        else
                        {
                            //create a list to store the command here
                            Thing temp = new();
                            temp.Label = "Pan";
                            temp.V = target.ToDegrees();
                            CQ.RecList.Add(temp);
                            //maybe add info to recording texbox
                        }
                    }
                }
                SenseCameraPan(target, false);
                if (!isLive) return;
                ModulePod pod = (ModulePod)FindModule("Pod");
                if (pod == null) return;
                pod.pan(target);
                return;
            }

            //only do the steps if there is no pod enabled
            { }
            while (!isLive && Abs(target - currentPan) >= panStep)
            {
                currentPan += (target > currentPan) ? panStep : -panStep;
                if (movingPan)
                {
                    commandQueue.Enqueue(("MovingPan", currentPan));
                }
                else
                    commandQueue.Enqueue(("Pan", currentPan));
            }
            if (target != currentPan)
            {
                if (movingPan)
                {
                    commandQueue.Enqueue(("MovingPan", target));
                }
                else
                    commandQueue.Enqueue(("Pan", target));
            }
        }
        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
        }

        public void CommandPause(float millisToWait, bool immediate = false, bool movingPause = false)
        {
            if (immediate)
            {
                ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
                if (CQ != null)
                {
                    if (CQ.Recording)
                    {
                        //if (CQ.Live)
                        //{
                        //    Thing temp = new();
                        //    temp.Label = "MovingPause";
                        //    temp.V = millisToWait;
                        //    CQ.RecList.Add(temp);
                        //}
                        //else
                        //{
                            //create a list to store the command here
                            Thing temp = new();
                            temp.Label = "Pause";
                            temp.V = millisToWait;
                            CQ.RecList.Add(temp);
                            //maybe add info to recording texbox
                        //}
                    }
                }
                timedPause = true;
                wait = millisToWait;
                timePauseStarted = DateTime.Now;
                return;
            }
            if (movingPause)
            {
                commandQueue.Enqueue(("MovingPause", millisToWait));
            }
            else
                commandQueue.Enqueue(("Pause", millisToWait));
        }
        public void MovingPause(float DistanceToWait)
        {
            wait = DistanceToWait;
            movedPause = true;
        }

        internal void CommandUnpause()
        {
            timedPause = false;
            movedPause = false;
        }

        public bool IsPodBusy()
        {
            ModulePod pod = (ModulePod)FindModule("Pod");
            if (pod != null && pod.isEnabled)
            {
                bool retVal = pod.IsPodBusy();
                return retVal;
            }
            //if we are local, say we're busy if we're still moving
            return false;
        }

        //display a warning if the pod is ready but live is not checked
        public void IsPodActive()
        {
            ModulePod pod = (ModulePod)FindModule("Pod");
            if (isLive == false && pod != null && pod.isEnabled && pod.getRobotInitStatus() == true)
            {
                MessageBox.Show("The pod is active but live is not checked.");
            }
        }

        public void SenseStop()
        {
            GetUKS();
            UKS.GetOrAddThing("Stop", "Attention");
        }

        public void QueueStop()
        {
            commandQueue.Enqueue("QueueStop");
        }

        public void QueueStopMove()
        {
            commandQueue.Enqueue("QueueStopMove");
        }

        public void QueueStopTurn()
        {
            commandQueue.Enqueue("QueueStopTurn");
        }

        public void CommandStop(bool ClearQueue = true)
        {
            wait = 0;
            ModulePod pod = (ModulePod)FindModule("Pod");
            pod.AbortMotion();
            SenseStop();
            ModuleGoTo GoTo = (ModuleGoTo)FindModule("GoTo");
            ModuleSpeechInPlus mSpip = (ModuleSpeechInPlus)FindModule("SpeechInPlus");
            ModuleTurnAround moduleTurnAround = (ModuleTurnAround)FindModule("TurnAround");
            if (mSpip != null)
                mSpip.CancelCommandQueue();
            if (moduleTurnAround != null)
                moduleTurnAround.StopTurning();

            ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
            if (CQ != null)
            {
                if (CQ.Recording)
                {
                    if (CQ.Live)
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "Stop";
                        CQ.RecList.Add(temp);
                        CQ.PauseHandler();
                        //maybe add info to recording texbox
                    }
                    else
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "Stop";
                        CQ.RecList.Add(temp);
                        //maybe add info to recording texbox
                    }
                }
            }

            Thing attention = UKS.GetOrAddThing("Attention", "Thing");
            if(ClearQueue)
                commandQueue.Clear();
            if (GoTo != null)
            {
                GoTo.imaginedPathSave.Clear();
                GoTo.endTarget = null;
            }
            if (attention == null) return;
            foreach (Thing t in attention.Children)
            {
                if (t.Label == "Explore")
                {
                    attention.RemoveChild(t);
                    break;
                }
            }
            foreach (Thing t in attention.Children)
            {
                if (t.Label == "GoTo")
                {
                    attention.RemoveChild(t);
                    break;
                }
            }

            Thing findLandmarks = UKS.Labeled("FindLandmarks", attention.Children);
            if (findLandmarks != null) UKS.DeleteThing(findLandmarks);

            if (pod == null)
            {
                Debug.WriteLine("ERROR:  ModulePodInterface needs ModulePod to function with a real Pod.");
                return;
            }

            UpdateSallieSelf();
        }

        public void CommandStopMove()
        {
            ModulePod pod = (ModulePod)FindModule("Pod");
            if (pod == null) return;
            ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
            if (CQ != null)
            {
                if (CQ.Recording)
                {
                    if (CQ.Live)
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "StopMove";
                        CQ.RecList.Add(temp);
                        CQ.PauseHandler();
                        //maybe add info to recording texbox
                    }
                    else
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "StopMove";
                        CQ.RecList.Add(temp);
                        //maybe add info to recording texbox
                    }
                }
            }
            pod.StopMove();
        }

        public void CommandStopTurn()
        {
            ModulePod pod = (ModulePod)FindModule("Pod");
            if (pod == null) return;
            ModuleCommandQueue CQ = (ModuleCommandQueue)FindModule("CommandQueue");
            if (CQ != null)
            {
                if (CQ.Recording)
                {
                    if (CQ.Live)
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "StopTurn";
                        CQ.RecList.Add(temp);
                        CQ.PauseHandler();
                        //maybe add info to recording texbox
                    }
                    else
                    {
                        //create a list to store the command here
                        Thing temp = new();
                        temp.Label = "StopTurn";
                        CQ.RecList.Add(temp);
                        //maybe add info to recording texbox
                    }
                }
            }
            pod.StopTurn();
        }



        public void SenseMove(double distance)//helper function
        {
            // First let's update the 3D world...
            GetUKS();
            ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
            ModulePodCamera cam = (ModulePodCamera)FindModule("PodCamera");

            if (theMentalModel == null) return;
            Sallie.Move(distance);

            // Now calculate the correct move in Sallie coordinates...
            Point3DPlus moveSallie = new Point3DPlus((float)distance, (float)0, (float)0);
            ModulePodCamera mpc = (ModulePodCamera)FindModule("PodCamera");
            ModuleIntegratedVision mir = (ModuleIntegratedVision)FindModule("IntegratedVision");
            Module3DSimView m3d = (Module3DSimView)FindModule("3DSimView");
            if (mir == null || (mpc != null && mpc.isEnabled && !mpc.saveImagesToDisk) || (m3d != null && m3d.isEnabled && !m3d.ProduceOutput))
            {
                theMentalModel.Move(moveSallie);
            }

            Thing podDelta = UKS.GetOrAddThing("PodDeltaMove", "Self");
            if (podDelta.V == null)
            {
                podDelta.V = (float)distance;
            }
            else
            {
                podDelta.V = (float)podDelta.V + (float)distance;
            }

            UpdateSallieSelf();
        }
        public void SenseTurn(Angle Theta)//helper functions
        {
            // turns detect off with moves queueing only?
            // First let's update the 3D world...
            if (Theta == 0) return;

            GetUKS();

            //for debugging, if there is no vision for turn detection, handle it directly
            ModuleIntegratedVision mir = (ModuleIntegratedVision)FindModule("IntegratedVision");
            ModulePodCamera mpc = (ModulePodCamera)FindModule("PodCamera");
            Module3DSimView m3d = (Module3DSimView)FindModule("3DSimView");
            if (mir == null || (mpc != null && mpc.isEnabled && !mpc.saveImagesToDisk) || (m3d != null && m3d.isEnabled && !m3d.ProduceOutput))
            {
                ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
                if (theMentalModel == null) return;
                theMentalModel.Turn(Theta);
            }
            Sallie.BodyAngle += Theta;

            Thing podDelta = UKS.GetOrAddThing("PodDeltaTurn", "Self");
            if (podDelta.V == null)
            {
                podDelta.V = (Angle)Theta;
            }
            else
            {
                podDelta.V = (Angle)podDelta.V + (Angle)Theta;
            }

            UpdateSallieSelf();
        }

        // These are the setters and getters for Camera Pan angle.
        // Get can immediately return values whether live or virtual.
        public Angle GetCameraPan()
        {
            if (ThingCameraPan == null) Initialize();
            if (ThingCameraPan.V == null) ThingCameraPan.V = (Angle)0;
            if (ThingCameraPan == null) return Sallie.CameraPan;
            return (Angle)(ThingCameraPan.V);
        }

        // Sense gets called by Command if not Live, or by pysical Pod if Live.
        public void SenseCameraPan(Angle value, bool relative)
        {
            if (ThingCameraPan == null) Initialize();
            Angle delta = 0;
            if (relative)
            {
                Sallie.CameraPan += (Angle)value;
                delta = value;
            }
            else
            {
                delta = value - Sallie.CameraPan;
                Sallie.CameraPan = (Angle)value;
            }
            if (ThingCameraPan == null) return;
            ThingCameraPan.V = (Angle)Sallie.CameraPan;

            Thing panDelta = UKS.GetOrAddThing("PanDelta", "Self");
            if (panDelta.V == null)
                panDelta.V = (Angle)delta;
            else
                panDelta.V = (Angle)panDelta.V + (Angle)delta;

            UpdateSallieSelf();
        }

        // This sets the camera tilt angle to an absolute value
        // Get can immediately return values whether live or virtual.
        public Angle GetCameraTilt()
        {
            if (ThingCameraTilt == null) Initialize();
            if (ThingCameraTilt.V == null) ThingCameraTilt.V = (Angle)0;
            if (ThingCameraTilt == null) return Sallie.CameraTilt;
            return (Angle)(ThingCameraTilt.V);
        }

        // Sense gets called by Command if not Live, or by pysical Pod if Live.
        public void SenseCameraTilt(Angle value, bool relative)
        {
            if (ThingCameraTilt == null) Initialize();
            Angle delta = 0;
            if (relative)
            {
                Sallie.CameraTilt += (Angle)value;
                delta = value;
            }
            else
            {
                delta = value - Sallie.CameraTilt;
                Sallie.CameraTilt = (Angle)value;
            }
            if (ThingCameraTilt == null) return;
            ThingCameraTilt.V = (Angle)Sallie.CameraTilt;

            Thing tiltDelta = UKS.GetOrAddThing("TiltDelta", "Self");
            if (tiltDelta.V == null)
                tiltDelta.V = (Angle)delta;
            else
                tiltDelta.V = (Angle)tiltDelta.V + (Angle)delta;

            UpdateSallieSelf();
        }


        public void ResetPosition()
        {
            Sallie.ResetPosition();
            ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theMentalModel == null) return;
        }

        public void QueueReset()
        {
            commandQueue.Enqueue("QueueReset");
        }

        public void ResetCamera()
        {
            Sallie.ResetCamera();
            if (isLive == false)
            {
                return;
            }

            ModulePod pod = (ModulePod)FindModule("Pod");
            if (pod == null)
            {
                Debug.WriteLine("ERROR:  ModulePodInterface needs ModulePod to function with a real Pod.");
                return;
            }
            pod.centerCam();

            UpdateSallieSelf();
        }

        public void ResetSallie()
        {
            // if image recognition is on, old objects will be recognized after the mental model is deleted
            ModulePodInterface thePod = (ModulePodInterface)FindModule("PodInterface");
            if (thePod != null)
            CommandStop();
            ResetPosition();
            UpdateSallieSelf();
            //if (!isLive)
                if (false)
                {
                    ModuleMentalModel theModel = (ModuleMentalModel)FindModule("MentalModel");
                if (theModel == null) return;
                theModel.Clear();
                Thing trans = UKS.Labeled("TransientProperty");
                for (int i = 0; trans.Children.Count > i;)
                {
                    UKS.DeleteAllChildren(trans);
                }
                trans = UKS.Labeled("col");
                for (int i = 0; trans.Children.Count > i;)
                {
                    UKS.DeleteAllChildren(trans);
                }
                trans = UKS.Labeled("siz");
                for (int i = 0; trans.Children.Count > i;)
                {
                    UKS.DeleteAllChildren(trans);
                }
                trans = UKS.Labeled("shp");
                for (int i = 0; trans.Children.Count > i;)
                {
                    UKS.DeleteAllChildren(trans);
                }
            }
        }

        private void CreateSallieInSelf()
        {
            GetUKS();
            if (UKS is null) return;
            Thing selfRoot = UKS.GetOrAddThing("Self", "Sense");
            if (selfRoot.Children.Count == 2 || (selfRoot.Children.Count > 2 && selfRoot.Children[2].V == null))
            {
                ThingBodyPosition = UKS.GetOrAddThing("BodyPosition", selfRoot, Sallie.BodyPosition.Clone());
                ThingBodyAngle = UKS.GetOrAddThing("BodyAngle", selfRoot, Sallie.BodyAngle);
                ThingCameraPan = UKS.GetOrAddThing("CameraPan", selfRoot, Sallie.CameraPan);
                ThingCameraTilt = UKS.GetOrAddThing("CameraTilt", selfRoot, Sallie.CameraTilt);
            }
            else
            {
                ThingBodyPosition = UKS.GetOrAddThing("BodyPosition", selfRoot);
                ThingBodyAngle = UKS.GetOrAddThing("BodyAngle", selfRoot);
                ThingCameraPan = UKS.GetOrAddThing("CameraPan", selfRoot);
                ThingCameraTilt = UKS.GetOrAddThing("CameraTilt", selfRoot);
            }
        }

        public void InitializeSallieSelf()
        {
            GetUKS();
            if (ThingBodyPosition == null)
            {
                CreateSallieInSelf();
            }
            else if (ThingBodyPosition.V == null)
            {
                CreateSallieInSelf();
            }
            Sallie.BodyPosition = (Point3DPlus)ThingBodyPosition.V;
            Sallie.BodyAngle = (Angle)ThingBodyAngle.V;
            Sallie.CameraPan = (Angle)ThingCameraPan.V;
            Sallie.CameraTilt = (Angle)ThingCameraTilt.V;
        }

        public void UpdateSallieSelf()
        {
            GetUKS();
            if (ThingBodyPosition == null)
            {
                CreateSallieInSelf();
                return;
            }
            ThingBodyPosition.V = Sallie.BodyPosition.Clone();
            ThingBodyAngle.V = Sallie.BodyAngle;
            ThingCameraPan.V = Sallie.CameraPan;
            ThingCameraTilt.V = Sallie.CameraTilt;
        }

        private void UpdateSallieFromSelf()
        {
            GetUKS();
            if (UKS is null) return;
            if (ThingBodyPosition == null)
            {
                CreateSallieInSelf();
                return;
            }
            Point3DPlus body = (Point3DPlus)ThingBodyPosition.V;
            Sallie.BodyPosition = body.Clone();
            Sallie.BodyAngle = (Angle)ThingBodyAngle.V;
            Sallie.CameraPan = (Angle)ThingCameraPan.V;
            Sallie.CameraTilt = (Angle)ThingCameraTilt.V;
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }


        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            CreateSallieInSelf();
            MainWindow.ResumeEngine();
        }
    }
}