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
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace BrainSimulator.Modules
{
    public class ModuleSafety : ModuleBase
    {
        public ModuleSafety()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }
        float podY = 5;
        float podX = 5;

        public override void Fire()
        {
            Init();
            GetUKS();
            if (UKS == null) return;

        }

        public List<Thing> ObstaclesInDirection(Point3DPlus goalPoint)
        {
            GetUKS();
            if (UKS == null) return null;
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");

            List<Thing> obst = new();
            foreach (Thing T in mentalModel.Children)
            {
                var test = T.GetRelationshipsAsDictionary();
                if (test.ContainsKey("cen") && test.ContainsKey("siz"))
                {
                    var p = test["cen"];

                    if (p is Point3DPlus p1)
                    {
                        Point P = new Point(p1.X, p1.Y);
                        Point P1 = new Point(0, 0);
                        Point P2 = new Point(goalPoint.X, goalPoint.Y);

                        double size = (float)test["siz"] / 2f;

                        double check = Utils.FindDistanceToSegment(P, P1, P2, out Point outP);
                        if (Abs(check) <= podX + Abs(size))
                        {
                            obst.Add(T);
                        }
                    }
                }
                else continue;
            }
            return obst;
        }
        //function takes given point and returns the nearest thing to that point
        public Thing ObstacleInDirection(Point3DPlus goalPoint)
        {
            List<Thing> obst = ObstaclesInDirection(goalPoint);
            if (obst == null) return null;
            float min = float.MaxValue;
            Thing minThing = null;
            foreach (Thing t in obst)
            {
                //if(obst == goalPoint)
                var test = t.GetRelationshipsAsDictionary();
                if (test.ContainsKey("cen"))
                {
                    var p = test["cen"];

                    if (p is Point3DPlus p1)
                    {
                        if (minThing == null)
                        {
                            minThing = t;
                            min = p1.R;
                            continue;
                        }
                        if (Abs(min) > Abs(p1.R))
                        {
                            min = p1.R;
                            minThing = t;
                        }
                    }
                }
            }
            return minThing;
        }

        public bool GoalInDirection(Thing Goal)
        {
            GetUKS();
            if (UKS == null) return false;
            Thing mentalModel = UKS.GetOrAddThing("MentalModel", "Thing");

            List<Thing> obst = new();
            foreach (Thing T in mentalModel.Children)
            {
                var test = T.GetRelationshipsAsDictionary();
                var test2 = Goal.GetRelationshipsAsDictionary();
                if (test.ContainsKey("cen") && test2.ContainsKey("cen") && test.ContainsKey("siz"))
                {
                    var p = test["cen"];
                    var p2 = test2["cen"];

                    if (p is Point3DPlus p1 && p2 is Point3DPlus goalPoint)
                    {
                        Point P = new Point(p1.X, p1.Y);
                        Point P1 = new Point(0, 0);
                        Point P2 = new Point(goalPoint.X, goalPoint.Y);

                        double size = (float)test["siz"] / 2;
                        double check = Utils.FindDistanceToSegment(P, P1, P2, out _);
                        if (Abs(check) <= podX + Abs(size))
                        {
                            obst.Add(T);
                        }


                    }
                }
                else continue;
            }
            if (obst.Count == 1)
            {
                if (obst[0] == Goal)
                    return true;
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
