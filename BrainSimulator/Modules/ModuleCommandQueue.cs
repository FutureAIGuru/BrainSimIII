//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;
using static System.Threading.Timer;

namespace BrainSimulator.Modules
{
    public class ModuleCommandQueue : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;


        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleCommandQueue()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        public bool Recording;
        public bool Live;
        public List<Thing> RecList;
        int x = 0;
        DateTime startTime = DateTime.Now;
        TimeSpan endTime = new();

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();
            UpdateDialog();

        }
        int LastTurnPosition = 0;
        float R = 0;

        public override void UKSInitializedNotification()
        {
            Initialize();
        }

        public List<string> FillSequenceList()
        {
            GetUKS();
            List<string> list = new List<string>();
            Thing seq = UKS.GetOrAddThing("Sequences", "Behavior");
            if (seq == null || seq.DescendentsList == null)
                return null;
            foreach (Thing t in seq.Descendents)
            {
                list.Add(t.Label);
            }
            list.Sort();
            return list;
        }

        public void SaveRecording(string name)
        {
            GetUKS();
            Thing seq = UKS.GetOrAddThing(name, "Sequences");
            seq.RemoveAllRelationships();
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");
            foreach (Thing t in RecList)
            {
                if (t.V == null && t.Label != "Stop")
                    seq.AddRelationship(t);
                if (t.Label == "Stop" && !Live)
                    continue; //reimplement stop later
                seq.AddRelationship(UKS.GetOrAddThing(t.Label, prim, t.V, true));
            }
        }

        public void executeCommandQueue(string Sequence)
        {
            ModulePodInterface mpi = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            //do I execute them here or send them to the PodInterface queue
            GetUKS();
            Thing seq = UKS.GetOrAddThing(Sequence, "Sequences");
            if (seq == null || seq.Relationships.Count() == 0)
                return;

            foreach (Thing command in seq.RelationshipsAsThings)
            {
                if (!command.HasAncestor(UKS.Labeled("Primitives")))
                {
                    executeCommandQueue(command.Label);
                }
                else
                {
                    switch (command.Label)
                    {
                        case "Turn":
                            {
                                mpi.CommandTurn(Angle.FromDegrees((float)command.V));
                                break;
                            }
                        case "MovingTurn":
                            {
                                mpi.CommandTurn(Angle.FromDegrees((float)command.V), false, true);
                                break;
                            }
                        case "Pan":
                            {
                                mpi.CommandPan(Angle.FromDegrees((float)command.V));
                                break;
                            }
                        case "MovingPan":
                            {
                                mpi.CommandPan(Angle.FromDegrees((float)command.V), false, false, true);
                                break;
                            }
                        case "Tilt":
                            {
                                mpi.CommandTilt(Angle.FromDegrees((float)command.V));
                                break;
                            }
                        case "MovingTilt":
                            {
                                mpi.CommandTilt(Angle.FromDegrees((float)command.V), false, false, true);
                                break;
                            }
                        case "Move":
                            {
                                mpi.CommandMove((float)command.V, false, false);
                                break;
                            }
                        case "ImmediateMove":
                            {
                                mpi.CommandMove((float)command.V, true, false);
                                break;
                            }
                        case "Pause":
                            {
                                mpi.CommandPause((float)command.V);
                                break;
                            }
                        case "MovingPause":
                            {
                                mpi.CommandPause((float)command.V, false, true);
                                break;
                            }
                        case "QueueSound":
                            {
                                mpi.QueueSound((string)command.V);
                                break;
                            }
                        case "Sound":
                            {
                                mpi.ImmediateSound((string)command.V);
                                break;
                            }
                        case "Speech":
                            {
                                mpi.Speech((string)command.V);
                                break;
                            }
                        case "Stop":
                            {
                                mpi.QueueStop();
                                break;
                            }
                        case "StopMove":
                            {
                                mpi.QueueStopMove();
                                break;
                            }
                        case "StopTurn":
                            {
                                mpi.QueueStopTurn();
                                break;
                            }
                        case "Reset":
                            {
                                mpi.QueueReset();
                                break;
                            }
                        case "RGB":
                            {
                                string str = (string)command.V;
                                string[] s = str.Split(',');
                                Tuple<int, int, int> rgb = Tuple.Create(Int32.Parse(s[0]), Int32.Parse(s[1]), Int32.Parse(s[2]));
                                mpi.QueueLED(rgb);
                                break;
                            }
                        case "FineAng":
                            {
                                mpi.QueueFineAngle((int)command.V);
                                break;
                            }
                        case "FineAdj":
                            {
                                mpi.QueueFineAdj((int)command.V);
                                break;
                            }
                        case "CoarseAng":
                            {
                                mpi.QueueCoarseAngle((int)command.V);
                                break;
                            }
                        case "CoarseAdj":
                            {
                                mpi.QueueCoarseAdj((int)command.V);
                                break;
                            }
                    }
                }
            }
        }

        public void CurveHandler(Thing obj, float stepTiming, Angle finalAngle, int stepCount)
        {
            //stepTime = milliseconds between steps, finalAngle = goal angle, stepCount = # of steps turn is divided into
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");
            for (int i = 0; i < stepCount; i++)
            {
                obj.AddRelationship(UKS.GetOrAddThing("Pause", prim, stepTiming, true));
                obj.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, finalAngle / stepCount, true));

            }

        }

        public void PauseHandler()
        {
            if (RecList.Count == 1)
            {
                x = RecList.Count;
                startTime = DateTime.Now;
                //start timer
            }
            if (RecList.Count > 1)
            {
                if (x < RecList.Count)
                {
                    endTime = DateTime.Now - startTime;
                    //end timer and use that value for pause
                    //push pause into RecList

                    Thing temp = new();
                    temp.Label = "Pause";
                    temp.V = (float)endTime.TotalMilliseconds;
                    RecList.Insert(x, temp);

                    x = RecList.Count;
                    startTime = DateTime.Now;
                }
            }
        }

        public void CirclingQueue(Thing t)
        {
            ModulePodCamera cam = (ModulePodCamera)FindModule("PodCamera");
            if (!cam.saveImagesToDisk)
            {
                System.Windows.MessageBox.Show($"Activate Image Recognition to use.");
                return;
            }
            ModulePod pod = (ModulePod)FindModule("Pod");
            pod.FineErrorAngle = 15;
            pod.FineErrorAdj = 40;
            pod.CoarseErrorAdj = 40;
            pod.CoarseErrorAngle = 45;
            LastTurnPosition = 0;
            ModulePodInterface mpi = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            Dictionary<string, object> properties = t.GetRelationshipsAsDictionary();
            Point3DPlus cen = (Point3DPlus)properties["cen"];
            float R = cen.R;
            float dist = 2 * R * ((float)Math.PI) + 50;
            //distance made too large so that it will be stopped by the circle algorithm
            mpi.CommandTurn(-cen.Theta, true);
            //turn to face object
            mpi.CommandMove(dist, true, false);
            for (int i = 0; i < 25; i++)
            {
                if (LastTurnPosition == 0 && i == 0)
                {
                    // cam.fullDelta = 0;
                    mpi.CommandTurn(Angle.FromDegrees(90), false, true);
                    mpi.CommandPan(Angle.FromDegrees(90), false, false, true);

                }

                if (i < 24)
                {
                    mpi.CommandPause(R / 6, false, true);
                    mpi.CommandTurn(Angle.FromDegrees(-15), false, true);
                }

                if (i == 24)
                {

                    mpi.CommandTurn(Angle.FromDegrees(-90), false, true);
                    mpi.QueueFineAngle(10);
                    mpi.QueueFineAdj(30);
                    mpi.QueueCoarseAngle(50);
                    mpi.QueueCoarseAdj(40);
                    mpi.QueueReset();
                    mpi.QueueStop();
                    mpi.QueueSound("SallieConfirm1.wav");

                }
            }
        }

        public void SallieNameCommand()
        {
            Thing name = UKS.GetOrAddThing("SallieName", "Sequences");
            Thing S = UKS.GetOrAddThing("S", "Sequences");
            Thing A = UKS.GetOrAddThing("A", "Sequences");
            Thing L = UKS.GetOrAddThing("L", "Sequences");
            Thing I = UKS.GetOrAddThing("I", "Sequences");
            Thing E = UKS.GetOrAddThing("E", "Sequences");
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");

            S.AddRelationship(UKS.GetOrAddThing("FineAng", prim, 30, true));
            S.AddRelationship(UKS.GetOrAddThing("FineAdj", prim, 40, true));
            S.AddRelationship(UKS.GetOrAddThing("CoarseAng", prim, 40, true));
            S.AddRelationship(UKS.GetOrAddThing("CoarseAdj", prim, 60, true));
            S.AddRelationship(UKS.GetOrAddThing("Speech", prim, "S", true));
            S.AddRelationship(UKS.GetOrAddThing("Move", prim, 200f, true));

            CurveHandler(S, 500f, -180, 6);
            CurveHandler(S, 500f, 180, 6);

            S.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2500f, true));
            S.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, 90f, true));
            S.AddRelationship(UKS.GetOrAddThing("Pause", prim, 3400f, true));
            S.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, -90f, true));
            S.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2000f, true));
            S.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));

            name.AddRelationship(S);

            A.AddRelationship(UKS.GetOrAddThing("Speech", prim, "A", true));
            A.AddRelationship(UKS.GetOrAddThing("Move", prim, 200f, true));
            A.AddRelationship(UKS.GetOrAddThing("FineAdj", prim, 60, true));
            A.AddRelationship(UKS.GetOrAddThing("CoarseAdj", prim, 60, true));

            CurveHandler(A, 400f, -360, 12);

            A.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2500f, true));
            A.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));

            name.AddRelationship(A);

            L.AddRelationship(UKS.GetOrAddThing("Speech", prim, "L", true));
            L.AddRelationship(UKS.GetOrAddThing("Move", prim, 200f, true));
            L.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, -90f, true));
            L.AddRelationship(UKS.GetOrAddThing("Pause", prim, 4000f, true));
            L.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, 180f, true));
            L.AddRelationship(UKS.GetOrAddThing("Pause", prim, 4600f, true));
            L.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, -90f, true));
            L.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2000f, true));
            L.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));

            name.AddRelationship(L);
            name.AddRelationship(L);

            I.AddRelationship(UKS.GetOrAddThing("Speech", prim, "I", true));
            I.AddRelationship(UKS.GetOrAddThing("Move", prim, 200f, true));
            I.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, -90f, true));
            I.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2000f, true));
            I.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, 180f, true));
            I.AddRelationship(UKS.GetOrAddThing("Pause", prim, 1200f, true));
            I.AddRelationship(UKS.GetOrAddThing("MovingTurn", prim, -90f, true));
            I.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2000f, true));
            I.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));

            name.AddRelationship(I);

            E.AddRelationship(UKS.GetOrAddThing("Speech", prim, "E", true));
            E.AddRelationship(UKS.GetOrAddThing("Move", prim, 200f, true));
            CurveHandler(E, 500f, -360, 10);
            E.AddRelationship(UKS.GetOrAddThing("Pause", prim, 2500f, true));
            E.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));

            name.AddRelationship(E);

            name.AddRelationship(UKS.GetOrAddThing("FineAng", prim, 10, true));
            name.AddRelationship(UKS.GetOrAddThing("FineAdj", prim, 30, true));
            name.AddRelationship(UKS.GetOrAddThing("CoarseAng", prim, 50, true));
            name.AddRelationship(UKS.GetOrAddThing("CoarseAdj", prim, 40, true));
            name.AddRelationship(UKS.GetOrAddThing("Sound", prim, "SallieConfirm1.wav", true));
            name.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));
        }

        public void CircleCommand()
        {
            Thing cir = UKS.GetOrAddThing("Circle", "Sequences");
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");

            cir.AddRelationship(UKS.GetOrAddThing("FineAng", prim, 15, true));
            cir.AddRelationship(UKS.GetOrAddThing("FineAdj", prim, 40, true));
            cir.AddRelationship(UKS.GetOrAddThing("CoarseAng", prim, 40, true));
            cir.AddRelationship(UKS.GetOrAddThing("CoarseAdj", prim, 45, true));

            cir.AddRelationship(UKS.GetOrAddThing("ImmediateMove", prim, 200f, true));

            CurveHandler(cir, 500f, -360, 24);

            cir.AddRelationship(UKS.GetOrAddThing("FineAng", prim, 10, true));
            cir.AddRelationship(UKS.GetOrAddThing("FineAdj", prim, 30, true));
            cir.AddRelationship(UKS.GetOrAddThing("CoarseAng", prim, 50, true));
            cir.AddRelationship(UKS.GetOrAddThing("CoarseAdj", prim, 40, true));

            cir.AddRelationship(UKS.GetOrAddThing("Sound", prim, "SallieConfirm1.wav", true));
            cir.AddRelationship(UKS.GetOrAddThing("Stop", prim, true));
        }

        public void TriangleCommand()
        {
            Thing tri = UKS.GetOrAddThing("Triangle", "Sequences");
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");

            tri.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            tri.AddRelationship(UKS.GetOrAddThing("Turn", prim, 120f, true));
            tri.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            tri.AddRelationship(UKS.GetOrAddThing("Turn", prim, 120f, true));
            tri.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            tri.AddRelationship(UKS.GetOrAddThing("Turn", prim, 120f, true));

            tri.AddRelationship(UKS.GetOrAddThing("QueueSound", prim, "SallieConfirm1.wav", true));
        }

        public void SquareCommand()
        {
            Thing squ = UKS.GetOrAddThing("Square", "Sequences"); Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");

            squ.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Turn", prim, 90f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Turn", prim, 90f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Turn", prim, 90f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Move", prim, 15f, true));
            squ.AddRelationship(UKS.GetOrAddThing("Turn", prim, 90f, true));

            squ.AddRelationship(UKS.GetOrAddThing("QueueSound", prim, "SallieConfirm1.wav", true));
        }

        public void DANCECommand()
        {
            ModulePodAudio mpo = (ModulePodAudio)FindModule("PodAudio");
            ModulePodInterface mpi = (ModulePodInterface)FindModule("PodInterface");
            string filePath = Utils.GetOrAddLocalSubFolder(Utils.FolderAudioFiles) + "\\" + "SallieDance15Secs.wav";
            //mpo.PlaySoundEffect(filePath);
            bool musicPlaying = true;
            GetUKS();
            var seconds = 5;
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddSeconds(seconds);
            TimeSpan remaintingTime = endTime - startTime;
            
            musicPlaying = false;
/*            if (remaintingTime >= TimeSpan.Zero)
                mpi.CommandStop();*/
            Thing dan = UKS.GetOrAddThing("Dance", "Sequences");
            Thing dan1 = UKS.GetOrAddThing("DanceMove1", "Sequences");
            Thing dan2 = UKS.GetOrAddThing("DanceMove2", "Sequences");
            Thing playMusic = UKS.GetOrAddThing("DanceMusic", "Sequences");
            Thing prim = UKS.GetOrAddThing("Primitives", "Behavior");
            dan1.AddRelationship(UKS.GetOrAddThing("RGB", prim, "255,128,0", true));
            //Thing playMusic = UKS.GetOrAddThing

            playMusic.AddRelationship(UKS.GetOrAddThing("Sound", prim, "SallieDance15Secs.wav", true));
            dan1.AddRelationship(UKS.GetOrAddThing("MovingPan", prim, -30f, true));
            dan1.AddRelationship(UKS.GetOrAddThing("Turn", prim, 30f, true));
            for (int i = 0; i < 1; i++)
            {
                dan1.AddRelationship(UKS.GetOrAddThing("RGB", prim, "255,0,255", true));
                dan1.AddRelationship(UKS.GetOrAddThing("MovingPan", prim, 60f, true));
                dan1.AddRelationship(UKS.GetOrAddThing("Turn", prim, -60f, true));
                dan1.AddRelationship(UKS.GetOrAddThing("RGB", prim, "0,255,255", true));
                dan1.AddRelationship(UKS.GetOrAddThing("MovingPan", prim, -60f, true));
                dan1.AddRelationship(UKS.GetOrAddThing("Turn", prim, 60f, true));
            }
            dan1.AddRelationship(UKS.GetOrAddThing("RGB", prim, "0,0,255", true));
            dan1.AddRelationship(UKS.GetOrAddThing("MovingPan", prim, 30f, true));
            dan1.AddRelationship(UKS.GetOrAddThing("Turn", prim, -30f, true));
            dan.AddRelationship(playMusic);
            dan.AddRelationship(dan1);

            dan2.AddRelationship(UKS.GetOrAddThing("RGB", prim, "127,0,255", true));
            dan2.AddRelationship(UKS.GetOrAddThing("Move", prim, 1f, true));
            dan2.AddRelationship(UKS.GetOrAddThing("Turn", prim, 180f, true));
            dan2.AddRelationship(UKS.GetOrAddThing("RGB", prim, "0,255,0", true));
            dan2.AddRelationship(UKS.GetOrAddThing("Turn", prim, 180f, true));
            dan2.AddRelationship(UKS.GetOrAddThing("Move", prim, -1f, true));
            //dan2.AddReference(UKS.GetOrAddThing("Stop", prim, 0, true));
            
            //Each about 5 seconds
            dan.AddRelationship(dan2);
            dan.AddRelationship(dan1);
            dan.AddRelationship(dan2);
            //dan.AddReference(dan1);
            //dan.AddReference(dan2);
            //dan.AddReference(dan1);
            //dan.AddReference(dan2);

            

        }

        public bool FindObject(string s)
        {
            GetUKS();
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            foreach (Thing T in mentalModel.Children)
            {
                if (T.Label == s)
                    CirclingQueue(T);
                return true;
            }
            return false;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
            GetUKS();
            UKS.GetOrAddThing("Primitives", "Behavior");
            UKS.GetOrAddThing("Sequences", "Behavior");
            TriangleCommand();
            SquareCommand();
            CircleCommand();
            DANCECommand();
            SallieNameCommand();
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }
}
