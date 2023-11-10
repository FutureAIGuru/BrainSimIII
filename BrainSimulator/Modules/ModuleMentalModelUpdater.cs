//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleMentalModelUpdater : ModuleBase
    {
        [XmlIgnore]
        public List<Thing> frameObjectsToProcess;

        Point3DPlus motion = new();
        List<Thing> justAdded2 = new();
        List<Thing> justAdded = new();

        // set size parameters as needed in the constructor
        // set max to be -1 if unlimited

        public ModuleMentalModelUpdater()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }


        // fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            UpdateMentalModel();

            UpdateDialog();
        }

        void UpdateMentalModel()
        {
            GetUKS();
            Thing tFrameToProcess = UKS.GetOrAddThing("FrameNow", "Visual");
            if (tFrameToProcess == null) return;

            // Check to make sure frame is completely recognized...
            if (!tFrameToProcess.HasChildWithLabel("FrameRecognized")) return;

            // Make sure frame is not already processed...
            if (tFrameToProcess.HasChildWithLabel("AlreadyProcessed")) return;

            // Make sure we only process if there is no movement in the frame...
            ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theMentalModel == null) return;
            Thing mentalModelRoot = UKS.Labeled("MentalModel");
            if (mentalModelRoot == null) return;

            Tuple<float, float> curr = HandleMotion(tFrameToProcess, theMentalModel);
            float currentMotion = curr.Item1;
            float currentRotation = curr.Item2;
            if (currentMotion != 0 || currentRotation != 0)
            {
                return;
            }

            frameObjectsToProcess = tFrameToProcess.Children.ToList();

            // Remove the items which are not UnknownAreas from the object list...
            for (int i = frameObjectsToProcess.Count - 1; i >= 0; i--)
            {
                Thing t = frameObjectsToProcess[i];
                if (!(t.V is UnknownArea))
                {
                    frameObjectsToProcess.RemoveAt(i);
                }
            }

            if (frameObjectsToProcess.Count == 0)
            {
                UKS.GetOrAddThing("AlreadyProcessed", tFrameToProcess);
                return;
            }

            // This line is the start of ACTUAL PROCESSING in the MentalModelUpdater

            for (int i = 0; i < frameObjectsToProcess.Count; i++)
            {
                Thing unknownAreaThing = frameObjectsToProcess[i];
                if ((unknownAreaThing.V is not UnknownArea unknownArea) || (unknownArea.TurnToSee != ' ')) continue;

                Point3DPlus cen = unknownArea.AngularCenter;
                cen.R = 40;
                cen.Z = 5f;
                double size = 2 * Sqrt(unknownArea.Area);
                size /= cen.R;

                Dictionary<string, object> propertiesMatchTolerances = new()
                {
                    { "cen", new Point3DPlus(100f, Angle.FromDegrees(3), Angle.FromDegrees(10)) },
                };
                Thing thingInDirection = theMentalModel.SearchPhysicalObject(new Dictionary<string, object> { { "cen", cen } }, propertiesMatchTolerances);
                Thing color = MatchToColorLibrary(unknownArea.AvgColor, thingInDirection);
                Thing t3DShape = MatchTo3DShapeLibrary(unknownAreaThing, thingInDirection);

                Dictionary<string, object> properties = new()
                {
                    { "cen", cen },
                    { "col", color },
                };
                Thing alreadyExists = theMentalModel.SearchPhysicalObject(properties, propertiesMatchTolerances);
                if (alreadyExists == null)
                {

                    ((Point3DPlus)propertiesMatchTolerances["cen"]).Theta = Angle.FromDegrees(10);
                    alreadyExists = theMentalModel.SearchPhysicalObject(properties, propertiesMatchTolerances);

                    if (alreadyExists != null)
                    {
                        Dictionary<string, object> objectProperties = alreadyExists.GetRelationshipsAsDictionary();
                        // Angle deltaTheta = ((Point3DPlus)objectProperties["cen"]).Theta - cen.Theta;
                        // Angle deltaPhi = ((Point3DPlus)objectProperties["cen"]).Phi - cen.Phi;
                        Point3DPlus center = (Point3DPlus)objectProperties["cen"];
                        cen.R = center.R;
                        cen.Conf = center.Conf;
                        theMentalModel.UpdateProperties(alreadyExists, new Dictionary<string, object> { { "cen", cen } });
                        // TODO: update all MM objects
                    }
                }

                if (alreadyExists == null)
                {
                    cen.Conf = 0;
                    // These were added earlier...
                    // properties.Add("cen", cen);
                    // properties.Add("col", color);
                    properties.Add("area", unknownArea.Area);
                    properties.Add("tid", unknownArea.TrackID);
                    properties.Add("prevAngle", unknownArea.AngularCenter.Theta);
                    float dist = ModuleRangeThing.EstimateRangeFromVisualAngle(unknownArea.Lowest.Phi);
                    if (dist > 50) dist = 50;
                    if (dist >= 0)
                        properties.Add("ang", unknownArea.AngleDifference);

                    Thing newThing = AddNewThingToMentalModel(theMentalModel, unknownArea, cen, t3DShape, properties);
                    if (unknownArea.isOuterContour)
                    {
                        newThing.V = unknownArea;
                    }
                    justAdded.Add(newThing);
                }
                else
                {
                    if (justAdded2.Contains(alreadyExists))
                        justAdded2.Remove(alreadyExists);
                    alreadyExists.SetFired();
                    foreach (Thing t1 in alreadyExists.RelationshipsAsThings)
                        t1.SetFired();
                    foreach (Thing parent in alreadyExists.Parents)
                    {
                        if (parent.Label != "MentalModel")parent.SetFired();
                    }
                    alreadyExists.useCount++;

                    if (unknownArea.isOuterContour)
                    {
                        alreadyExists.V = unknownArea;
                    }
                    Dictionary<string, object> prevProperties = alreadyExists.GetRelationshipsAsDictionary();
                    if (!prevProperties.ContainsKey("area"))
                    {
                        theMentalModel.UpdateProperties(alreadyExists, new Dictionary<string, object> { { "area", unknownArea.Area } });
                    }
                    if (!prevProperties.ContainsKey("prevAngle"))
                    {
                        theMentalModel.UpdateProperties(alreadyExists, new Dictionary<string, object> { { "prevAngle", unknownArea.AngularCenter.Theta } });
                    }
                    if (motion.X >= 2)
                    {
                        // Debug.WriteLine("We have moved forward but now have stopped so we can range the object by change in apparent area");
                        Point3DPlus prevCen = (Point3DPlus)prevProperties["cen"];
                        if (Abs(cen.Theta) < Angle.FromDegrees(10) && prevProperties.ContainsKey("area"))
                        {
                            // This algorithm only work when moving nearly straight toward an object
                            // update the area
                            if (prevProperties.ContainsKey("area"))
                            {
                                float prevArea = Convert.ToSingle(prevProperties["area"]);
                                float newR = ModuleRangeThing.EstimateRangeFromChangeInArea(prevArea, (float)unknownArea.Area, motion.X);
                                float newConf = 1 / newR;
                                if (newConf > prevCen.Conf)
                                {
                                    UpdateDistance(theMentalModel, alreadyExists, cen, newR, newConf, (float)unknownArea.Area);
                                }
                            }
                        }
                        else if (Abs(cen.Theta) > Angle.FromDegrees(15) && prevProperties.ContainsKey("prevAngle"))
                        {
                            Debug.WriteLine("We have turned but now have stopped so we can range the object by change in apparent angle");
                            Angle curAngle = unknownArea.AngularCenter.Theta;
                            if (prevProperties.ContainsKey("prevAngle"))
                            {
                                Angle prevAngle = (Angle)prevProperties["prevAngle"];
                                float newR = ModuleRangeThing.EstimateRangeFromChangeInAngle(prevAngle, curAngle, motion.X);
                                float newConf = 1 / newR;
                                if (newConf > prevCen.Conf)
                                {
                                    UpdateDistance(theMentalModel, alreadyExists, cen, newR, newConf, (float)unknownArea.Area);
                                }
                            }
                        }
                    }
                }
            }

            // Mark the frame as processed...
            UKS.GetOrAddThing("AlreadyProcessed", tFrameToProcess);

            // Move measured, delete existing move info
            if (motion.X >= 2)
            {
                ClearDistanceCalculation();
            }
            PruneMentalModel(frameObjectsToProcess.Count);
        }

        
        private static void ReduceWeight(Thing tTarget, Thing t)
        {
            Relationship l = t.HasRelationship(tTarget);
            if (l != null)
            {
                l.weight -= 0.01f;
                if (l.weight <= 0)
                    if (l.T is Thing lT)
                        t.RemoveRelationship(lT);
            }
        }

        private Thing AddNewThingToMentalModel(ModuleMentalModel theMentalModel, UnknownArea a, Point3DPlus cen, Thing t3DShape, Dictionary<string, object> properties)
        {
            //the thing is not already in the MentalModel, add it
            cen.Conf = 0;
            float dist = ModuleRangeThing.EstimateRangeFromVisualAngle(a.Lowest.Phi);
            if (dist == -1)
            {
                Debug.WriteLine("Ranging failed on new object add to MM");
            }
            else
            {
                cen.R = dist;
                cen.Conf = 0.01f;
            }

            // Careful: this does add width and height based on two outer angles...
            Angle w = Abs(a.RightMost.Theta - a.LeftMost.Theta);
            float width = dist * (float)Tan(w);
            Angle h = Abs(a.Highest.Phi - a.Lowest.Phi);
            float height = dist * (float)Tan(h);
            properties.Add("siz", Max(height, width));
            properties.Add("wid", width);
            properties.Add("hgt", height);
            properties.Add("shp", t3DShape);

            Thing newThing = theMentalModel.AddPhysicalObject(properties);

            Thing tNew = UKS.GetOrAddThing("New", "TransientProperty");
            newThing.AddRelationship(tNew);
            newThing.SetFired();
            return newThing;
        }

        private Tuple<float, float> HandleMotion(Thing tFrameToProcess, ModuleMentalModel theMentalModel)
        {
            //If this frame refects motion, accumulate it for ranging purposes
            float currentMotion = 0;
            float currentRotation = 0;
            foreach (Thing t in tFrameToProcess.Children)
            {
                if (t.Label == "FrameBodyMovementDelta")
                {
                    if (t.V is double frameMotion && frameMotion != 0)
                    {
                        motion.X += (float)frameMotion;
                        theMentalModel.Move(new Point3DPlus((float)frameMotion, 0, 0f));
                        if (motion.X < -0.1)
                        {
                            ClearDistanceCalculation();
                        }
                    }
                }
                if (t.Label == "FrameBodyAngleDelta")
                {
                    if (t.V is Angle frameRotation && Abs(frameRotation) != 0)
                    {
                        currentMotion = -1;
                        theMentalModel.Turn(frameRotation);
                        currentRotation = frameRotation;
                        // Debug.WriteLine("MMU Turn:" + frameRotation);
                        currentRotation = frameRotation;
                        motion = new();
                        ClearDistanceCalculation();

                    }
                }
            }
            return new Tuple<float, float>(currentMotion, currentRotation);
        }

        void PruneMentalModel(int visibleObjectCount)
        {
            int maxItemsInMentalModel = 10 + visibleObjectCount;

            GetUKS();
            ModuleMentalModel theMentalModel = (ModuleMentalModel)FindModule("MentalModel");
            if (theMentalModel == null) return;

            // Delete all physicalObjects that are in JustAdded2, then add all objects from justAdded to justAdded2
            foreach (Thing t in justAdded2) 
                theMentalModel.DeletePhysicalObject(t);
            justAdded2.Clear();
            foreach (Thing t in justAdded) 
                justAdded2.Add(t);
            justAdded.Clear();

            Thing mentalModelRoot = UKS.GetOrAddThing("MentalModel", "Thing");
            if (mentalModelRoot == null) return;
            IList<Thing> mmThings = mentalModelRoot.Children;
            for (int i = 0; i < mmThings.Count; i++)
            {
                // If this is a collision object, never prune
                if (mmThings[i].V == null) continue;

                //if this thing should be visible but is not, it is likely a phantom
                Dictionary<string, object> properties = mmThings[i].GetRelationshipsAsDictionary();
                bool cen = properties.ContainsKey("cen");
                bool vis = theMentalModel.ThingIsVisible(mmThings[i], 0, -10);
                if (cen && vis)
                {
                    if (mmThings[i].lastFiredTime < DateTime.Now - TimeSpan.FromSeconds(3))
                    {
                        Debug.WriteLine("MMU: Deleting Thing " + mmThings[i].Label);
                        theMentalModel.DeletePhysicalObject(mmThings[i]);
                        i--;
                    }
                }
            }

            if (mentalModelRoot.Children.Count > maxItemsInMentalModel)
            {
                // get the list of things.  Remove the most recent from the list so they can't be deleted
                // exclude collision objects which are never deleted
                var allMMThings = mentalModelRoot.Children.FindAll(t => t.V != null).OrderBy(t => -t.lastFired).ToList();
                for (int i = 0; i < visibleObjectCount; i++)
                    allMMThings.RemoveAt(0);

                // delete the thing with the lowest useCount
                Thing lowest = null;
                int lowestUseCount = int.MaxValue;
                foreach (Thing t in allMMThings)
                {
                    if (t.useCount < lowestUseCount)
                    {
                        lowestUseCount = t.useCount;
                        lowest = t;
                    }
                }
                if (lowest != null)
                {
                    theMentalModel.DeletePhysicalObject(lowest);
                    UKS.DeleteThing(lowest);
                }
            }
        }

        private static void UpdateDistance(ModuleMentalModel theMentalModel, Thing t, Point3DPlus cen, float newR, float newConf, float area)
        {
            float objectSize = (float)Sqrt(area) * newR / 1330;
            Dictionary<string, object> newProperties = new();
            cen.R = newR;
            cen.Conf = newConf;
            newProperties.Add("cen", cen);
            newProperties.Add("siz", objectSize);
            theMentalModel.UpdateProperties(t, newProperties);
        }

        void ClearDistanceCalculation()
        {
            UKS.DeleteAllChildren(UKS.Labeled("area"));
            UKS.DeleteAllChildren(UKS.Labeled("prevAngle"));
            motion.X = 0;
        }

        public Thing MatchToColorLibrary(HSLColor c, Thing thingInDirection = null)
        {
            GetUKS();
            Thing colorRoot = UKS.GetOrAddThing("col", "Property");
            foreach (Thing colorThing in colorRoot.Children)
            {
                foreach (Thing tColorValue in colorThing.Children)
                {
                    if (c.Equals((HSLColor)tColorValue.V))
                    {
                        HSLColor hsl = (HSLColor)tColorValue.V;
                        if (hsl.luminance < c.luminance) hsl.luminance = c.luminance;
                        return colorThing;
                    }
                }
            }
            if (thingInDirection != null)
            {
                Thing t1 = thingInDirection.GetRelationshipWithAncestor(UKS.Labeled("col"));
                if (t1 != null)
                {
                    UKS.GetOrAddThing("cv*", t1, c);
                    return t1;
                }
            }

            foreach (Thing colorThing in colorRoot.Children)
            {
                foreach (Thing colorVal in colorThing.Children)
                {
                    HSLColor existingColorVal = (HSLColor)colorVal.V;
                    if (Abs(existingColorVal - c) < 10)
                    {
                        colorThing.AddChild(UKS.GetOrAddThing("cv*", colorThing, c));
                        return colorThing;
                    }
                }
            }

            Thing newColorValue = UKS.GetOrAddThing("col*", colorRoot);
            UKS.GetOrAddThing("cv*", newColorValue, c);
            return newColorValue;
        }

        Thing MatchTo3DShapeLibrary(Thing outerUnknownAreaThing, Thing thingInDirection)
        {
            GetUKS();
            Thing t3DShapesRoot = UKS.GetOrAddThing("shp", "Property");

            // Determine the KnownAreaThing
            Thing knownAreaThing = outerUnknownAreaThing.Parents[0];
            if (knownAreaThing != null && knownAreaThing.V is UnknownArea)
            {
                knownAreaThing = knownAreaThing.Parents[0];
            }

            // Iterate over all 3D shapes in 3D shapes root
            Thing matchedThing = null;
            double matchedScore = ModuleIntegratedVision.NoMatchLimit;

            if (outerUnknownAreaThing.V is UnknownArea ua)
            {
                foreach (Thing savedShapeThing in t3DShapesRoot.Children)
                {
                    foreach (Thing savedKnownAreaThing in savedShapeThing.Children)
                    {
                        if (savedKnownAreaThing.V is not UnknownArea savedUnknownArea) continue;

                        double score = savedUnknownArea.ScoreFor(ua, ModuleIntegratedVision.NoMatchLimit);
                        if (score <= matchedScore && score < ModuleIntegratedVision.NoMatchLimit)
                        {
                            matchedScore = score;
                            matchedThing = savedShapeThing;
                        }
                    }
                }
            }
            if (matchedThing != null) 
                return matchedThing;

            // we should add a new 2D shape. Is it a new view to an existing 3D shape?
            if (thingInDirection != null)
            {
                Thing t1 = thingInDirection.GetRelationshipWithAncestor(UKS.Labeled("shp"));
                if (t1 != null)
                {
                    t1.AddChild(knownAreaThing);
                    return t1;
                }
            }

            // it must be a new 3D shape, so add it
            Thing new3DShape = UKS.GetOrAddThing("shp*", t3DShapesRoot);
            new3DShape.AddParent(UKS.Labeled("graphic"));
            var x = outerUnknownAreaThing.V;
            if (x is UnknownArea u)
            {
                List<Point3DPlus> pointList = new();
                float minx = (float)u.AreaCorners.Min(x=>x.loc.X);
                float miny = (float)u.AreaCorners.Min(x=>x.loc.Y);
                float maxx = (float)u.AreaCorners.Max(x=>x.loc.X);
                float maxy = (float)u.AreaCorners.Max(x => x.loc.Y);
                foreach (CornerTwoD c in u.AreaCorners)
                {
                    //rescale for range [-5,+5]
                    Point3DPlus thePoint = new Point3DPlus(
                        -5+((float)c.loc.X - minx) * 10 / (maxx - minx),
                        -5+((float)c.loc.Y - miny) * 10 / (maxy - miny),
                        0f);
                    pointList.Add(thePoint);
                }
                new3DShape.V = pointList;
            }
            new3DShape.AddChild(outerUnknownAreaThing);
            return new3DShape;
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

        public override void UKSReloadedNotification()
        {
            UpdateDialog();
        }
    }
}