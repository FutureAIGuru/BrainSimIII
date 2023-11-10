
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleGoTo : ModuleBase
    {
        //globals for pod dimensions
        float podY = 5;
        float podZ = 5;
        public float podX = 5;

        public ModuleGoTo()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        public override void Fire()
        {

            Init();  //be sure to leave this here
            GetUKS();
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            Thing attention = UKS.GetOrAddThing("Attention", "Thing");
            if (attention == null) return;
            IList<Thing> attentionRoot = attention.Children;
            if (podInterface.IsPodBusy()) return;

            if (imaginedPathSave.Count > 0)
            {
                Point3DPlus p = imaginedPathSave.Pop();
                DirectlyToPoint(p);

                return;
            }

            if (endTarget != null)
            {
                RouteToThing(endTarget);
                endTarget = null;
                return;
            }

            //is there a command to parse
            foreach (Thing attentionObject in attentionRoot)
            {//determine if Explore or GoTo are in attention
                if (attentionObject.Label == "GoTo")
                {
                    if (attentionObject.Relationships.Count == 1)
                    {
                        //if GoToObject returns true, repeat on the next fire, else remove label to end loop
                        RouteToThing((attentionObject.Relationships[0].T as Thing));
                        attention.RemoveChild(attentionObject);
                    }
                }
            }
            UpdateDialog();
        }

        int HeightLimit = 10;
        int bounds = 100;
        int goal = -2;
        int start = -4;
        int obstacle = -1;
        int possible = -3;

        public int[,] FillArray(Point3DPlus Target)
        {

            int XLimit = Abs((int)Math.Round(Target.X)) + 2 * bounds;
            int YLimit = Abs((int)Math.Round(Target.Y)) + 2 * bounds;
            bool valid = false;
            int[,] LeeArray = new int[Abs(XLimit) + 1, Abs(YLimit) + 1];
            GetUKS();
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");
            foreach (Thing T in mentalModel.Children)
            {
                var test = T.GetRelationshipsAsDictionary();
                if (test.ContainsKey("cen"))
                {
                    var p = test["cen"];

                    if (p is Point3DPlus p1)
                    {
                        double size = ((float)test["siz"] / 2f) + podX;
                        //if (Abs((float)test["siz"] / 2f + p1.Z) <= HeightLimit)
                        LeeArray = FindOverlap(p1, size, LeeArray, Target);
                    }
                }
            }
            for (int i = 0; i < Abs(XLimit) + 1; i++)
            {
                for (int j = 0; j < Abs(YLimit) + 1; j++)
                {
                    //stop outer bounds from being exceded by setting bounds to obstacle
                    if (i == 0 || j == 0 || i == Abs(XLimit) || j == Abs(YLimit))
                        LeeArray[i, j] = obstacle;
                    if (!(LeeArray[i, j] == obstacle))
                        LeeArray[i, j] = possible;
                }
            }
            LeeArray[bounds, bounds] = start;
            if (LeeArray[(int)Math.Round(Abs(Target.X)) + bounds, (int)Math.Round(Abs(Target.Y)) + bounds] != obstacle)
                LeeArray[(int)Math.Round(Abs(Target.X)) + bounds, (int)Math.Round(Abs(Target.Y)) + bounds] = goal;
            else
            {
                ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
                if (Audio != null)
                    Audio.PlaySoundEffect("SallieNegative1.wav");
                Debug.WriteLine("Goal cannot be reached");
            }
            return LeeArray;
        }

        private int[,] FindOverlap(Point3DPlus p1, double size, int[,] LeeArray, Point3DPlus Target)
        {
            int lowX = (int)Math.Round(p1.X - size);
            int lowY = (int)Math.Round(p1.Y - size);
            int highX = (int)Math.Round(p1.X + size);
            int highY = (int)Math.Round(p1.Y + size);
            if (Target.X < 0 && Target.Y < 0)
            {
                //no points intersect
                if (highX < Target.X - bounds || lowX > bounds)
                { return LeeArray; }
                else if (highY < Target.Y - bounds || lowY > bounds)
                { return LeeArray; }
                else
                {
                    //at least one point intersects
                    for (int i = 0; i < Abs(lowX - highX); i++)
                    {
                        for (int j = 0; j < Abs(lowY - highY); j++)
                        {
                            if (lowX + i > Target.X - bounds && lowX + i < bounds)
                                if (lowY + j > Target.Y - bounds && lowY + j < bounds)
                                {

                                    int x = Abs(lowX + i - bounds);
                                    int y = Abs(lowY + j - bounds);
                                    LeeArray[x, y] = obstacle;

                                }
                        }
                    }
                }
            }
            else if (Target.X < 0)
            {
                if (highX < Target.X - bounds || lowX > bounds)
                { return LeeArray; }
                else if (lowY > Target.Y + bounds || highY < -bounds)
                { return LeeArray; }
                else
                {
                    //at least one point intersects
                    for (int i = 0; i < Abs(lowX - highX); i++)
                    {
                        for (int j = 0; j < Abs(lowY - highY); j++)
                        {
                            if (lowX + i > Target.X - bounds && lowX + i < bounds)
                                if (lowY + j < Target.Y + bounds && lowY + j > -bounds)
                                {
                                    int x = Abs(lowX + i - bounds);
                                    int y = Abs(lowY + j + bounds);
                                    LeeArray[x, y] = obstacle;
                                }
                        }
                    }
                }
            }
            else if (Target.Y < 0)
            {
                if (lowX > Target.X + bounds || highX < -bounds)
                { return LeeArray; }
                else if (highY < Target.Y - bounds || lowY > bounds)
                { return LeeArray; }
                else
                {
                    //at least one point intersects
                    for (int i = 0; i < Abs(lowX - highX); i++)
                    {
                        for (int j = 0; j < Abs(lowY - highY); j++)
                        {
                            if (lowX + i < Target.X + bounds && lowX + i > -bounds)
                                if (lowY + j > Target.Y - bounds && lowY + j < bounds)
                                {
                                    int x = Abs(lowX + i + bounds);
                                    int y = Abs(lowY + j - bounds);
                                    LeeArray[x, y] = obstacle;
                                }
                        }
                    }
                }
            }
            else
            {
                if (lowX > Target.X + bounds || highX < -bounds)
                { return LeeArray; }
                else if (highY > Target.Y + bounds || lowY < -bounds)
                { return LeeArray; }
                else
                {
                    //at least one point intersects
                    for (int i = 0; i < Abs(lowX - highX); i++)
                    {
                        for (int j = 0; j < Abs(lowY - highY); j++)
                        {
                            if (lowX + i < Target.X + bounds && lowX + i > -bounds)
                                if (lowY + j < Target.Y + bounds && lowY + j > -bounds)
                                {
                                    int x = Abs(lowX + i + bounds);
                                    int y = Abs(lowY + j + bounds);
                                    LeeArray[x, y] = obstacle;
                                }
                        }
                    }
                }
            }
            return LeeArray;
        }

        int[] Xmove = { -1, 0, 1, 0, 1, -1, 1, -1 }; // these arrays will help you travel in the 4 directions more easily
        int[] Ymove = { 0, 1, 0, -1, 1, -1, -1, 1 };

        public class intPt
        {
            public int X;
            public int Y;

            public intPt(int x, int y)
            {
                X = x;
                Y = y;
            }
            public intPt(intPt p)
            {
                X = p.X;
                Y = p.Y;
            }
            public intPt()
            { }

            public string ToString()
            {
                return "X: " + X + "Y: " + Y;
            }
        }

        Queue<intPt> Wave = new();
        float oldX = 0;
        float oldY = 0;
        private Queue<intPt> Lee(int[,] LeeArray, intPt start, intPt Target)
        {
            Wave.Clear();
            Wave.Enqueue(start);

            Queue<intPt> result;
            intPt currpt, nextpt;

            bool x = true;
            bool y = true;

            start.X = bounds;
            start.Y = bounds;

            if (Target.X < 0)
                x = false;
            if (Target.X >= 0)
                x = true;
            if (Target.Y < 0)
                y = false;
            if (Target.Y >= 0)
                y = true;

            Target.X = Abs(Target.X) + bounds;
            Target.Y = Abs(Target.Y) + bounds;
            //if(!x)
            //    Target.X *= -1;
            //if(!y)
            //    Target.Y *= -1;

            do
            {
                currpt = Wave.Dequeue();
                //check if loop has reached the next layer
                //check each adjacent point
                for (int i = 0; i < 8; i++)
                {
                    nextpt = new(currpt.X + Xmove[i], currpt.Y + Ymove[i]);
                    //check if point is the destination
                    if (LeeArray[nextpt.X, nextpt.Y] == goal)
                    {
                        Wave.Enqueue(nextpt);
                        LeeArray[nextpt.X, nextpt.Y] = i;
                        //enqueue the steps to get back to the origin, then return the queue
                        return TraceBack(LeeArray, Target, x, y);
                    }//if point is a valid point, enqueue it for the next layer of loops
                    else if (LeeArray[nextpt.X, nextpt.Y] == possible)
                    {
                        Wave.Enqueue(nextpt);
                        LeeArray[nextpt.X, nextpt.Y] = i;
                    }
                }
                //if all points have been check unsuccessfully, return null
            } while (!(Wave.Count == 0));
            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (Audio != null)
                Audio.PlaySoundEffect("SallieNegative1.wav");
            Debug.WriteLine("Goal not found!");
            return null;
        }

        public Queue<intPt> TraceBack(int[,] LeeArray, intPt Target, bool x, bool y)
        {
            intPt Pt = new();
            Queue<intPt> result = new();
            oldX = 0;
            oldY = 1;
            if (x && y)
            {
                Pt.X = Target.X - bounds;
                Pt.Y = Target.Y - bounds;
            }
            if (x && !y)
            {
                Pt.X = Target.X - bounds;
                Pt.Y = Target.Y - bounds;
                Pt.Y *= -1;
            }
            if (!x && y)
            {
                Pt.X = Target.X - bounds;
                Pt.Y = Target.Y - bounds;
                Pt.X *= -1;
            }
            if (!x && !y)
            {
                Pt.X = Target.X - bounds;
                Pt.Y = Target.Y - bounds;
                Pt.X *= -1;
                Pt.Y *= -1;
            }
            result.Enqueue(Pt);
            Pt = new(Pt);
            intPt currPt = new(Target);
            for (int i = 0; i < 1001; i++)
            {
                int direction = LeeArray[currPt.X, currPt.Y];

                switch (direction)
                {
                    case 0: currPt.X++; break;
                    case 1: currPt.Y--; break;
                    case 2: currPt.X--; break;
                    case 3: currPt.Y++; break;
                    case 4: currPt.Y--; currPt.X--; break;
                    case 5: currPt.Y++; currPt.X++; break;
                    case 6: currPt.Y++; currPt.X--; break;
                    case 7: currPt.Y--; currPt.X++; break;
                    case -4: return result;
                    default: MessageBox.Show("Illegal Lee array value detected!"); return null;

                }
                if (x && y)
                {
                    Pt.X = currPt.X - bounds;
                    Pt.Y = currPt.Y - bounds;
                }
                if (x && !y)
                {
                    Pt.X = currPt.X - bounds;
                    Pt.Y = currPt.Y - bounds;
                    Pt.Y *= -1;
                }
                if (!x && y)
                {
                    Pt.X = currPt.X - bounds;
                    Pt.Y = currPt.Y - bounds;
                    Pt.X *= -1;
                }
                if (!x && !y)
                {
                    Pt.X = currPt.X - bounds;
                    Pt.Y = currPt.Y - bounds;
                    Pt.X *= -1;
                    Pt.Y *= -1;
                }

                result.Enqueue(Pt);
                Pt = new(Pt);
                currPt = new(currPt);
            }
            return null;
        }

        //most simple go to method, takes a point, and turns or moves without any checks 
        public bool RouteToPoint(Point3DPlus point)
        {
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            podInterface.CommandSpeed(40);
            if (podInterface.IsPodBusy()) return false;
            int[,] LeeArray = FillArray(point);
            //// Code for printing Lee Array
            //string s = "Lee Array: \n";
            //for (int i = 0; i < LeeArray.GetUpperBound(1); i++)
            //{
            //    for (int j = 0; j < LeeArray.GetUpperBound(0); j++)
            //    {
            //        if (LeeArray[j, i] == -1)
            //            s += "x";
            //        else if (LeeArray[j, i] == -3)
            //            s += " ";
            //        else
            //            s += LeeArray[j, i].ToString();
            //    }
            //    s += "\n";
            //}
            ////MessageBox.Show(s);
            //Debug.WriteLine(s);
            intPt Start = new() { X = 0, Y = 0 };
            intPt Target = new() { X = (int)Math.Round(point.X), Y = (int)Math.Round(point.Y) };
            float totalMove = 0;
            Queue<intPt> path = Lee(LeeArray, Start, Target);
            if (path == null) { return false; }
            if (path.Count > 0)
            {
                IEnumerable<intPt> path2 = path.Reverse();
                intPt prevPt = new();

                foreach (intPt currPt in path2)
                {
                    if (currPt == path2.First())
                    {
                        prevPt = currPt;
                        continue;
                    }
                    float Y = currPt.X - prevPt.X;
                    float X = currPt.Y - prevPt.Y;

                    Angle newangle = new();

                    if (X == 0 && Y == 1)
                    {
                        newangle = new(Angle.FromDegrees(0));
                    }
                    else if (X == 1 && Y == 1)
                    {
                        newangle = new(Angle.FromDegrees(45));
                    }
                    else if (X == 1 && Y == 0)
                    {
                        newangle = new(Angle.FromDegrees(90));
                    }
                    else if (X == 1 && Y == -1)
                    {
                        newangle = new(Angle.FromDegrees(135));
                    }
                    else if (X == 0 && Y == -1)
                    {
                       newangle = new(Angle.FromDegrees(180));
                    }
                    else if (X == -1 && Y == -1)
                    {
                        newangle = new(Angle.FromDegrees(-135));
                    }
                    else if (X == -1 && Y == 0)
                    {
                        newangle = new(Angle.FromDegrees(-90));
                    }
                    else if (X == -1 && Y == 1)
                    {
                        newangle = new(Angle.FromDegrees(-45));
                    }

                    Angle oldangle = new();

                    if (oldX == 0 && oldY == 1)
                    {
                        oldangle = new(Angle.FromDegrees(0));
                    }
                    else if (oldX == 1 && oldY == 1)
                    {
                        oldangle = new(Angle.FromDegrees(45));
                    }
                    else if (oldX == 1 && oldY == 0)
                    {
                        oldangle = new(Angle.FromDegrees(90));
                    }
                    else if (oldX == 1 && oldY == -1)
                    {
                        oldangle = new(Angle.FromDegrees(135));
                    }
                    else if (oldX == 0 && oldY == -1)
                    {
                        oldangle = new(Angle.FromDegrees(180));
                    }
                    else if (oldX == -1 && oldY == -1)
                    {
                        oldangle = new(Angle.FromDegrees(-135));
                    }
                    else if (oldX == -1 && oldY == 0)
                    {
                        oldangle = new(Angle.FromDegrees(-90));
                    }
                    else if (oldX == -1 && oldY == 1)
                    {
                        oldangle = new(Angle.FromDegrees(-45));
                    }

                    Angle trueAngle = oldangle - newangle;
                    if (trueAngle > 180)
                        trueAngle -= 360;
                    if (trueAngle < -180)
                        trueAngle += 360;
                    if (trueAngle.ToDegrees() == 0f)
                    {
                        if (Abs(newangle) == Angle.FromDegrees(45) || Abs(newangle) == Angle.FromDegrees(135))
                        {
                            totalMove += (float)Sqrt(2);
                        }
                        else
                            totalMove += 1;
                    }
                    else
                    {
                        podInterface.CommandMove(totalMove, false, false);
                        totalMove = 0;
                        podInterface.CommandTurn(trueAngle);
                        if (Abs(newangle) == Angle.FromDegrees(45) || Abs(newangle) == Angle.FromDegrees(135))
                        {
                            totalMove += (float)Sqrt(2);
                        }
                        else
                            totalMove += 1;
                    }
                    prevPt = currPt;
                    oldX = X;
                    oldY = Y;
                }
                podInterface.CommandMove(totalMove, false, false);
            }
            ModulePodAudio Audio = (ModulePodAudio)FindModule(typeof(ModulePodAudio));
            if (Audio != null)
                Audio.PlaySoundEffect("SalliePositive1.wav");
            return true;
        }

        //method parses a thing and runs referenced point through GoToPoint method
        public bool DirectlyToPoint(Point3DPlus point)
        {
            ModulePodInterface podInterface = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (point == null) return false;
            podInterface.CommandTurn(-point.Theta);
            podInterface.CommandMove((float)Sqrt(Pow(point.X, 2) + Pow(point.Y, 2)));
            return true;
        }
        [XmlIgnore]
        public Stack<Point3DPlus> imaginedPathSave = new();
        [XmlIgnore]
        public Thing endTarget;

        //this function will either go straight to the object if unobstructed,
        //or use imagined pathing to get to object around objects
        public bool RouteToThing(Thing thing)
        {
            //can Sallie go straight there

            ModuleSafety Safety = (ModuleSafety)FindModule(typeof(ModuleSafety));
            var test = thing.GetRelationshipsAsDictionary();
            if (test.ContainsKey("cen") && test.ContainsKey("siz"))//needs a check that takes size/shape into account when approaching
            {
                var p = test["cen"];
                double size = 0;

                size = (float)test["siz"] / 2f;

                if (p is Point3DPlus p1)
                {
                    Point3DPlus p2 = p1.Clone();
                    p2.R -= (float)(podX + size + 5);
                    return RouteToPoint(p2);
                }
            }
            return false;
        }

        public override void Initialize()
        {
        }
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }
        public override void SizeChanged()
        {
            if (mv == null) return;
        }
        public override void UKSInitializedNotification()
        {
            MainWindow.SuspendEngine();
            GetUKS();

            MainWindow.ResumeEngine();
        }
    }
}
