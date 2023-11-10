//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleRangeThing : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XlmIgnore] 
        //public theStatus = 1;
        ModulePodInterface mpi = null;

        enum currentState { idle, turning, fwd, back, }

        currentState state = currentState.idle;

        //set size parameters as needed in the constructor
        //set max to be -1 if unlimited
        public ModuleRangeThing()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        //fill this method in with code which will execute
        //once for each cycle of the engine

        Thing targetThing = null;
        public float LastTurnPosition = 0;
        public int i = 0;
        float R = 0;
        public override void Fire()
        {
            Init();  //be sure to leave this here
            GetUKS();

            mpi = (ModulePodInterface)FindModule(typeof(ModulePodInterface));
            if (mpi == null) return;
            Thing attentionRoot = UKS.GetOrAddThing("Attention", "Thing");

            if (i == 0)
            {
                foreach (Thing t in attentionRoot.Children)
                {
                    if (t.Label == "Range")
                    {
                        Range();
                        UKS.DeleteThing(t);
                    }
                    else if (t.Label == "RangeThing")
                    {
                        if (t.Relationships.Count > 0)
                        {
                            RangeThing((t.Relationships[0].T as Thing));
                        }
                    }
                }
            }

            //Thing podDelta = UKS.GetOrAddThing("PodDeltaMove", "Self");
            //if (attentionRoot == null) return;
            //foreach (Thing t in attentionRoot.Children)
            //{
            //    if (t.Label == "RangeThing" && podDelta.V != null)
            //    {
            //        if (t.References.Count > 0)
            //        {
            //            if (LastTurnPosition == 0 && i == 0)
            //            {
            //                ModulePodCamera cam = (ModulePodCamera)FindModule(typeof(ModulePodCamera));
            //                cam.fullDelta = 0;
            //                mpi.CommandTurn(Angle.FromDegrees(90), false, true);
            //                mpi.CommandPan(Angle.FromDegrees(90), false, false, true);

            //            }


            //            if (i < 24)
            //            {
            //                mpi.CommandPause(R/6, false, true);
            //                mpi.CommandTurn(Angle.FromDegrees(-15), false, true);
            //                i++;
            //            }

            //            if (i >= 24)
            //            {
            //                mpi.CommandTurn(Angle.FromDegrees(-90), false, true);
            //            }

            //            if (i >= 24)
            //            {

            //                if (t.Label == "Range")
            //                {
            //                    UKS.DeleteThing(t);
            //                }
            //                else if (t.Label == "RangeThing")
            //                {
            //                    if (t.References.Count > 0)
            //                    {
            //                        UKS.DeleteThing(t);

            //                        mpi.ResetCamera(true);
                                   
            //                       mpi.QueueSound("SallieConfirm1.wav");
            //                        mpi.CommandStop(true);
            //                    }
            //                }
            //                //ModulePodCamera cam = (ModulePodCamera)FindModule(typeof(ModulePodCamera));
            //                //cam.fullDelta = 0;
            //                i = 0;
            //            }

                        

            //            if (mpi.IsPodBusy()) return;
            //        }
            //    }

            //}

            //if you want the dlg to update, use the following code whenever any parameter changes
            // UpdateDialog();
           
        }

        // This is a basic function that:
        //  - Turns, Pans, Pauses To Get Angle.
        //  - Moves forward, pauses to range object.
        //  - Moves back, pans back, turns back.
        // It doesn't care if or where objects are.
        void Range()
        {
            if (mpi == null) return;
            mpi.CommandPan(Angle.FromDegrees(90));
            mpi.CommandTurn(Angle.FromDegrees(90));
            mpi.CommandPause(2000);
            mpi.CommandMove(-5);
            mpi.CommandPause(2000);
            mpi.CommandMove(5);
            mpi.CommandPause(2000);
            mpi.CommandTurn(Angle.FromDegrees(-90));
            mpi.CommandPan(Angle.FromDegrees(0));
        }

        void RangeThing(Thing targetThing)
        {
            if (mpi == null) return;
            Dictionary<string, object> properties = targetThing.GetRelationshipsAsDictionary();
            Point3DPlus cen = (Point3DPlus)properties["cen"];
            Angle angleToThing = cen.Theta;
            Angle PhiToThing = cen.Phi;
            RangeType rangeType = WhichRangeType(angleToThing);
            if (rangeType == RangeType.StraightForward) RangeStraightForward(angleToThing);
            else if (rangeType == RangeType.Nautical) RangeNautical(angleToThing);
           
        }

        private void RangeNautical(Angle angleToThing)
        {
            Angle panAngle = Sallie.CameraPan;
            Angle absAngleToThing = Angle.FromDegrees(Math.Abs(angleToThing.Degrees));
            if (absAngleToThing.Degrees < 45 || absAngleToThing.Degrees > 90)
            {
                Angle targetAngle = Angle.FromDegrees(absAngleToThing.Degrees < 45 ? 45 : 90);
                float signOfAngle = angleToThing / Math.Abs(angleToThing);
                Angle angleToTurn = (targetAngle - Math.Abs(angleToThing)) * signOfAngle;
                mpi.CommandTurn(angleToTurn);
                mpi.CommandPan(angleToThing - panAngle + angleToTurn);
                mpi.CommandPause(1000);
                MoveBackThenForward();
                mpi.CommandPause(1000);
                mpi.CommandPan(-1 * (angleToThing - panAngle + angleToTurn));
                mpi.CommandTurn(-angleToTurn);
            }
            else // Can pan to object.
            {
                mpi.CommandPan(angleToThing - panAngle);
                mpi.CommandPause(1000);
                MoveBackThenForward();
                mpi.CommandPause(1000);
                mpi.CommandPan(-1 * (angleToThing - panAngle));
            }
        }

        //public void RangeCircle(Point3DPlus target)
        //{

        //    //mpi.CommandPause(2);
        //    //for (int i = 0; i < 32; i++)
        //    //{
        //    //mpi.CommandPause(1, true);
        //    R = target.R;
        //    float dist = 2* target.R * ((float)Math.PI);
        //    mpi.CommandMove(dist, true);
        //    LastTurnPosition = 0;
        //    i = 0;

        //    // mpi.CommandPause(1);
        //    //}
        //    //mpi.CommandPause(2);
        //    //mpi.CommandPan(Angle.FromDegrees(90),false, true);
        //    //mpi.CommandTurn(Angle.FromDegrees(90), true);

        //}

        private void RangeStraightForward(Angle angleToThing)
        {
            if (mpi == null) return;
            Angle panAngle = Sallie.CameraPan;

            mpi.CommandTurn(-angleToThing);
            mpi.CommandPan(-1 * panAngle);
            if ( angleToThing != 0 || panAngle != 0 ) mpi.CommandPause(1000);
            MoveForwardThenBack();
            mpi.CommandPause(1000);
            mpi.CommandPan(panAngle);
            mpi.CommandTurn(angleToThing);
        }

        private void MoveForwardThenBack(int distance = 4)
        {
            mpi.CommandMove(distance);
            mpi.CommandPause(1000);
            mpi.CommandMove(-distance);
        }

        private void MoveBackThenForward(int distance = 4)
        {
            mpi.CommandMove(-distance);
            mpi.CommandPause(1000);
            mpi.CommandMove(distance);
        }

        enum RangeType { StraightForward, Nautical, Visual, Circle }
        RangeType WhichRangeType(Angle angleToThing)
        {
            // Calculate minimum angle for each range type
            // Counts both directions.
            float minimumAngle;
            RangeType rangeType;

            // Calculated min angle for straightForward
            // turn to object, pan to 0.
            minimumAngle = 2 * Math.Abs(angleToThing.Degrees);
            minimumAngle += 2 * Math.Abs(Sallie.CameraPan.Degrees);
            rangeType = RangeType.StraightForward;

            if (Math.Abs(angleToThing.Degrees) < 45) // need to turn away and pan
            {
                float angleToTurn = 45 - Math.Abs(angleToThing.Degrees);
                float minAngle = 4 * angleToTurn; // For turning and additional pan
                minAngle += 2 * Math.Abs(angleToThing.Degrees - Sallie.CameraPan.Degrees);
                if (minAngle < minimumAngle)
                {
                    minimumAngle = minAngle;
                    rangeType = RangeType.Nautical;
                }
            }
            else if (Math.Abs(angleToThing.Degrees) < 90) // Only have to pan to thing
            {
                float minAngle = 2 * Math.Abs(angleToThing.Degrees - Sallie.CameraPan.Degrees);
                if (minAngle < minimumAngle)
                {
                    minimumAngle = minAngle;
                    rangeType = RangeType.Nautical;
                }
            }
            else // Need to turn towards and pan
            {
                float turnAngle = (Math.Abs(angleToThing.Degrees) - 90);
                float minAngle = 2 * turnAngle;
                minAngle += 2 * Math.Abs(angleToThing.Degrees - turnAngle - Sallie.CameraPan.Degrees);
                if (minAngle < minimumAngle)
                {
                    minimumAngle = minAngle;
                    rangeType = RangeType.Nautical;
                }
            }

            return rangeType;
        }

        // TurnPanToThing() Well turn/pan to thing.
        bool TurnPanToThing()
        {
            return false;
        }
        public static float EstimateRangeFromVisualAngle(Angle phi)
        {
            float height = 4;
            if (phi >= 0) return -1;
            Angle h = Abs(phi);
            Angle d = Angle.FromDegrees(90) - h;
            float distance = height * (float)Sin(d) / (float)Sin(h);
            return distance;
        }

        public static float EstimateRangeFromChangeInArea(float prevArea, float currentArea, float motionToward)
        {
            double areaRatio = (double)currentArea / prevArea;

            double newR = (Sqrt(areaRatio) * motionToward) / (Sqrt(areaRatio) - 1);
            //System.Diagnostics.Debug.WriteLine(motionToward + " " + (newR - motionToward) + " " + prevArea + " " + currentArea);
            return (float)newR - motionToward;
        }
        public static float EstimateRangeFromChangeInAngle(Angle prevAngle, Angle currentAngle, float motionX)
        {
            //the 3rd angle of the triangle
            Angle B = Abs(PI - currentAngle);
            Angle C = Abs(prevAngle);

            Angle A = Abs(PI - (B + C));
            float newR = (float)Math.Sin(C) * motionX / (float)Math.Sin(A);

            return newR;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
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