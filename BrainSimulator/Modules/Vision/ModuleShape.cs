//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using static System.Math;
using UKS;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using System.Linq;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleShape : ModuleBase
    {

        public int mnistHitCount = 0;
        public int mnistMissCount = 0;
        public int mnistAskCount = 0;
        public string mnistMessage = "";

        const int secondsForTemporaryShapes = 6000;

        //timer only processes recently-changed items.
        DateTime lastScan = new DateTime();
        public string MNISTDigit = "";
        public Thing newStoredShape = null;
        public override void Fire()
        {
            Init();

            GetUKS();
            if (theUKS == null) return;
            theUKS.GetOrAddThing("Sense", "Thing");
            theUKS.GetOrAddThing("Visual", "Sense");
            Thing tCorners = theUKS.GetOrAddThing("corner", "Visual");

            //check to see if there are any new corners which need checking
            foreach (Thing tCorner in tCorners.Children)
                if (tCorner.lastFiredTime > lastScan)
                    goto processingNeeded;
            UpdateDialog();
            return;

        processingNeeded:
            lastScan = DateTime.Now;

            CreateShapeFromCorners();

            Thing storedShapes = theUKS.GetOrAddThing("StoredShape", "Visual");
            Thing mnistParent = theUKS.GetOrAddThing("MNISTDigit", "unknownObject");

            //if there is more than one shape found, we need to create a toplevel which has these as offspring
            int shapeCount = theUKS.Labeled("currentShape").Children.Count;
            Thing newParent = null;
            if (shapeCount > 1)
            {
                newParent = theUKS.GetOrAddThing("storedShape*", "storedShape");
            }

            foreach (Thing shape in theUKS.Labeled("currentShape").Children)
            {
                if (shape == newParent) continue;
                float bestValue = 0;
                //currehtShape has a set of attributes. We search the descendents of StoredShape for the
                // entry with the best match.
                Thing foundShape = theUKS.SearchForClosestMatch(shape, storedShapes, ref bestValue);
                //if (foundShape != null)
                {
                    Thing nextBest = foundShape;
                    float bestSeqsScore = -1;
                    float angleOffset = 0; //degrees
                    int bestOffset = -1;
                    while (nextBest != null && foundShape != null)
                    {
                        float score = theUKS.HasSequence(nextBest, shape, out int offset, true);
                        if (score > bestSeqsScore)
                        {
                            for (int i = 0; i < offset && i < nextBest.Relationships.Count; i++)
                            {
                                if (nextBest.Relationships[i].target.HasAncestorLabeled("Rotation"))
                                {
                                    angleOffset += float.Parse(nextBest.Relationships[i].target.Label[5..]);
                                }
                            }
                            bestSeqsScore = score;
                            bestOffset = offset;
                            foundShape = nextBest;
                        }
                        nextBest = theUKS.GetNextClosestMatch(ref bestValue);
                    }
                    if (bestSeqsScore > 0.5 && foundShape != null)
                    {
                        Relationship r = shape.SetAttribute(foundShape);
                        r.Weight = bestSeqsScore;
                        Thing offsetThing = theUKS.GetOrAddThing("mmOffset:" + bestOffset, "offset");
                        shape.SetAttribute(offsetThing);
                        foundShape.SetFired();
                        //TODO: add the offset & rotation to the parent
                        if (newParent != null)
                            theUKS.AddStatement(newParent, "go*", foundShape);

                        newStoredShape = null;
                        //are we running MNIST and did we get the correct answer?
                        if (MNISTDigit != "" && shapeCount == 1)
                        {
                            Thing mnistDigit = theUKS.GetOrAddThing(MNISTDigit, mnistParent);
                            //is foundShape a known shape of mmistDigit?
                            Relationship r1 = theUKS.GetRelationship(mnistDigit, "hasAttribute", foundShape);
                            if (r1 == null) //erroneous recognition
                            {
                                mnistMessage = foundShape.Label + " seen for " + mnistDigit.Label;
                                mnistMissCount++;
                            }
                            else //correct recognition
                            {
                                mnistHitCount++;
                            }
                        }
                    }
                    else
                    {
                        newStoredShape = AddShapeToLibrary(shape);
                        if (newStoredShape != null)
                        {
                            //set a time-to-live on this new shape
                            if (MNISTDigit == "")
                                newStoredShape.RelationshipsFrom.FindFirst(x => x.reltype.Label == "has-child").TimeToLive = TimeSpan.FromSeconds(secondsForTemporaryShapes);
                            Relationship r = shape.SetAttribute(newStoredShape);
                            r.Weight = 1;
                            Thing offsetThing = theUKS.GetOrAddThing("mmOffset:" + 0, "offset");
                            shape.SetAttribute(offsetThing);
                            newStoredShape.SetFired();
                            //TODO: add the offset & rotation to the parent
                            if (newParent != null)
                                theUKS.AddStatement(newParent, "go*", newStoredShape);
                            if (MNISTDigit != "" && shapeCount == 1)
                            {
                                Thing mnistDigit = theUKS.GetOrAddThing(MNISTDigit, mnistParent);
                                mnistDigit.SetAttribute(newStoredShape);
                                newStoredShape.Label = MNISTDigit + ".*";
                                mnistMessage = MNISTDigit + " added new image ";
                                mnistAskCount++;
                            }
                        }
                    }
                }
            }

            Thing foundParent = newParent;
            if (newParent != null)
            {
                //determine if there is already a child of storedShape with the same attriburtes
                foreach (Thing t in storedShapes.Children)
                {
                    if (t == newParent) continue;
                    if (theUKS.HasSequence(t, newParent, out int offset, true) > 0.5)
                    {
                        foundParent = t;
                        t.SetFired();
                        theUKS.DeleteAllChildren(newParent);
                        theUKS.DeleteThing(newParent);
                        newParent = null;
                        break;
                    }
                }
            }
            if (newParent != null) newParent.SetFired();
            if (foundParent != null && MNISTDigit != "" && shapeCount > 1)
            {
                foundParent.SetAttribute("notSolidArea");
                Thing mnistDigit = theUKS.GetOrAddThing(MNISTDigit, mnistParent);
                mnistDigit.SetAttribute(foundParent);
                foundParent.Label = MNISTDigit + ".*";
                if (newParent != null)
                {
                    mnistMessage = MNISTDigit + " added new image ";
                    mnistAskCount++;
                }
                else
                {
                    mnistMessage = MNISTDigit + " hit ";
                    mnistHitCount++;
                }
            }


            UpdateDialog();
        }

        //note there are two kinds of corners
        //1. Transient: seen in space which has a position and an angle (descendent of visual | 
        //2. abstract: which has only an angle (perhaps a relative orientation)

        void CreateShapeFromCorners()
        {
            //UKS setup
            Thing currentShape = theUKS.GetOrAddThing("currentShape", "Visual");
            theUKS.GetOrAddThing("Go", "RelationshipType");

            //clear out any previous currentShape
            theUKS.DeleteAllChildren(currentShape);

            foreach (Thing outline in theUKS.Labeled("outline").Children)
            {
                ExtractShape(theUKS.GetOrAddThing("currentShape*", currentShape), outline);
            }
        }

        private void ExtractShape(Thing currentShape, Thing outline)
        {
            var cornersTmp = outline.GetRelationshipsWithAncestor(theUKS.Labeled("corner"));

            if (cornersTmp.Count == 0)
                return;

            //create a convenient list of the corneres for this outline
            List<ModuleVision.Corner> corners = new();
            foreach (var c in cornersTmp)
            {
                var corner = c.target.V as ModuleVision.Corner;
                if (corner == null) continue;
                corners.Add(corner);
            }

            //setup to normalize the distances
            float maxDist = 0;
            Angle prefTheta = (corners[0].nextPt - corners[0].pt).Theta;
            foreach (Relationship r in outline.Relationships)
            {
                if (r.reltype.Label == "hasAttribute")
                    currentShape.SetAttribute(r.target);
            }

            bool isClosed = currentShape.GetAttribute("notSolidArea") == null;

            //find the length of the longest segment of this shape
            //offset is the index of the that segment
            
            if (corners.Count == 1)
            {
                maxDist = Max((corners[0].pt - corners[0].prevPt).R, (corners[0].pt - corners[0].nextPt).R);
            }
            else
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    ModuleVision.Corner c = corners[i];
                    PointPlus theEdge = (c.nextPt - c.pt);
                    float dist = theEdge.R;
                    if (dist > maxDist) maxDist = dist;
                    theEdge = (c.prevPt - c.pt);
                    dist = theEdge.R;
                    if (dist > maxDist) maxDist = dist;
                    if (c is ModuleVision.Arc a)
                    {
                        theEdge = (c.nextPt - c.prevPt);
                        dist = theEdge.R;
                        if (dist > maxDist) maxDist = dist;
                    }
                }
            }
            //TODO: put any other attributes on your shape here

            //add the scale to the object
            string sizeName = "size" + (int)maxDist / 10;
            currentShape.SetAttribute(theUKS.GetOrAddThing(sizeName, "Size"));

            string location = $"mmPos:{(int)corners[0].pt.X},{(int)corners[0].pt.Y}";
            Thing locationThing = theUKS.GetOrAddThing(location, "Position");
            currentShape.SetAttribute(locationThing);

            //get the rotation
            string rotation = prefTheta.Degrees.ToString("0.0");
            rotation = "mmRot:" + rotation;
            Thing rotationThing = theUKS.GetOrAddThing(rotation, "Rotation");
            currentShape.SetAttribute(rotationThing);

            //add the corners to the currentShape
            //in the case of an Arc, the previous corner must be maintained
            for (int i = 0; i < corners.Count; i++)
            {
                ModuleVision.Corner theCorner = corners[i];
                var currPt = corners[0].pt;
                var currCorner = corners[i].pt;
                var prevCorner = corners[i].prevPt;
                var nextCorner = corners[i].nextPt;

                if (corners[i] is ModuleVision.Arc a)
                {
                    //if this is an arc, we can't use prevPt as the previous corner
                    ModuleVision.Corner prevArc = null;
                    if (i != 0) prevArc = corners[i - 1];
                    if (prevArc != null)
                    {
                        float dist = (prevCorner - prevArc.pt).R;
                        InsertDistanceEntry(currentShape, maxDist, dist);
                    }
                    var circle = a.GetCircleFromArc();
                    int sweep = IRound(a.SweepAngle.Degrees, 10);
                    float radius = circle.radius / maxDist;
                    int iDiameter = ((int)Round(radius*20));
                    Thing theArc = theUKS.GetOrAddThing("arc" + sweep + "D." + iDiameter, "Arc");
                    Relationship r1 = theUKS.AddStatement(currentShape, "go*", theArc);
                    r1.TimeToLive = TimeSpan.FromSeconds(secondsForTemporaryShapes);
                }
                else
                {
                    //This is a corner
                    float dist;
                    string distName;
                    Thing theDist;
                    if (prevCorner != null)
                    {// how far to move
                        dist = (currCorner - prevCorner).R;
                        InsertDistanceEntry(currentShape, maxDist, dist);
                    }
                    //how much to turn
                    Segment s1 = new Segment(corners[i].pt, corners[i].prevPt);
                    Segment s2 = new Segment(corners[i].pt, corners[i].nextPt);
                    Angle turn = s2.Angle - s1.Angle;
                    while (turn.Degrees > 180) turn -= Angle.FromDegrees(360);
                    while (turn.Degrees <= -180) turn += Angle.FromDegrees(360);
                    int a1 = IRound(turn.Degrees, 10);
                    Thing theAngle = theUKS.GetOrAddThing("angle" + a1, "Angle");
                    Relationship r = theUKS.AddStatement(currentShape, "go*", theAngle);
                    r.TimeToLive = TimeSpan.FromSeconds(secondsForTemporaryShapes);
                    if (!isClosed && i == corners.Count - 1 && corners.Count != 1) //only include a following move if this is not a closed shape
                    {
                        dist = (currCorner - nextCorner).R;
                        InsertDistanceEntry(currentShape, maxDist, dist);
                    }
                }
            }
        }

        private void InsertDistanceEntry(Thing currentShape, float maxDist, float dist)
        {
            dist /= maxDist;
            if (dist < 0.1) return;
            string distName = "distance." + ((int)Round(dist*10)).ToString();
            if (Round(dist * 10) == 10) distName = "distance1.0";
            Thing theDist = theUKS.GetOrAddThing(distName, "distance");
            Relationship r = theUKS.AddStatement(currentShape, "go*", theDist);
            r.TimeToLive = TimeSpan.FromSeconds(secondsForTemporaryShapes);
        }

        int IRound(float number, int multiple = 1)
        {
            float retVal = (float)Round(number / multiple) * multiple;
            return (int)retVal;
        }

        public Thing AddShapeToLibrary(Thing currentShape)
        {
            if (currentShape == null) return null;
            theUKS.GetOrAddThing("StoredShape", "Visual");
            Thing newShape = theUKS.GetOrAddThing("storedShape*", "StoredShape");
            //TODO: fix this so we can indicate which item we mearn
            foreach (Relationship r in currentShape.Relationships)
            {
                if (r.target.Label.Contains("SolidArea"))
                    newShape.SetAttribute(r.target);
                if (r.relType.Label.StartsWith("has")) continue;
                newShape.AddRelationship(r.target, r.relType);
            }
            return newShape;
        }

        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        // The following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        // called whenever the UKS performs an Initialize()
        public override void UKSInitializedNotification()
        {

        }
    }
}