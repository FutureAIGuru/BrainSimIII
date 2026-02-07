/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using static System.Math;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleShape : ModuleBase
    {

        //timer only processes recently-changed items.
        DateTime lastScan = new DateTime();
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
            return;

        processingNeeded:
            lastScan = DateTime.Now;

            CreateShapeFromCorners();

            Thing storedShapes = theUKS.GetOrAddThing("StoredShape", "Visual");
            foreach (Thing shape in theUKS.Labeled("currentShape").Children)
            {
                float bestValue = 0;
                //currehtShape has a set of attriburtes. We search the descendents of StoredShape for the
                // entry with the best match.
                Thing foundShape = theUKS.SearchForClosestMatch(shape, storedShapes, ref bestValue);
                if (foundShape != null)
                {
                    Thing nextBest = foundShape;
                    float bestSeqsScore = -1;
                    float angleOffset = 0; //degrees
                    int bestOffset = -1;
                    while (nextBest != null)
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
                    if (bestSeqsScore > 0.5)
                    {
                        Relationship r = shape.SetAttribute(foundShape);
                        r.Weight = bestSeqsScore;
                        Thing offsetThing = theUKS.GetOrAddThing("mmOffset:" + bestOffset, "offset");
                        shape.SetAttribute(offsetThing);
                        foundShape.SetFired();
                        newStoredShape = null;
                    }
                    else
                    {
                        newStoredShape = AddShapeToLibrary();
                        //set a time-to-live on this new image
                        newStoredShape.RelationshipsFrom.FindFirst(x => x.reltype.Label == "has-child").TimeToLive = TimeSpan.FromSeconds(15);
                        Relationship r = shape.SetAttribute(newStoredShape);
                        r.Weight = 1;
                        Thing offsetThing = theUKS.GetOrAddThing("mmOffset:" + 0, "offset");
                        shape.SetAttribute(offsetThing);
                        newStoredShape.SetFired();
                    }
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
                ExtractShape(theUKS.GetOrAddThing("currentShape*",currentShape), outline);
            }
        }

        private void ExtractShape(Thing currentShape, Thing outline)
        {
            var cornersTmp = outline.GetRelationshipsWithAncestor(theUKS.Labeled("corner"));

            if (cornersTmp.Count < 2)
                return;

            //create a convenient list of the corneres for this outline
            List<ModuleVision.Corner> corners = new();
            foreach (var c in cornersTmp)
            {
                var corner = c.target.V as ModuleVision.Corner;
                if (corner == null) continue;
                corners.Add(corner);
            }
            //setup to normalize the distance
            float maxDist = 0;
            Angle prefTheta = (corners[1].pt - corners[0].pt).Theta;
            Thing theColor = outline.GetAttribute("Color");
            if (theColor != null)
                currentShape.SetAttribute(theColor);

            //find the length of the longest segment of this shape
            //offset is the index of the that segment
            int offset = -1;
            for (int i = 0; i < corners.Count; i++)
            {
                int next = i + 1; 
                if (next >= corners.Count) next = 0;
                var nextCorner = corners[next];
                PointPlus theEdge  = (nextCorner.pt - corners[i].pt);
                float dist = theEdge.R;
                if (dist > maxDist)
                {
                    maxDist = dist;
                    offset = i;
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
            Thing rotationThing = theUKS.GetOrAddThing(rotation,"Rotation");
            currentShape.SetAttribute(rotationThing);

            //add the corners to the currentShape
            for (int i = 0; i < corners.Count; i++)
            {
                var prevCorner = corners[i];
                var currCorner = corners[(i + 1) % corners.Count];
                var nextCorner = corners[(i + 2) % corners.Count];

                //how far to move
                float dist = (currCorner.pt - prevCorner.pt).R;
                dist /= maxDist;
                string distName = "distance." + ((int)Round(dist * 10)).ToString();
                if (Round(dist * 10) == 10) distName = "distance1.0";
                Thing theDist = theUKS.GetOrAddThing(distName, "distance");
                Relationship r = theUKS.AddStatement(currentShape, "go*", theDist);
                r.TimeToLive = TimeSpan.FromSeconds(10);

                //how much to turn
                Segment s1 = new Segment(prevCorner.pt, currCorner.pt);
                Segment s2 = new Segment(currCorner.pt, nextCorner.pt);
                Angle turn = s2.Angle - s1.Angle;
                while (turn.Degrees > 180) turn -= Angle.FromDegrees(360);
                while (turn.Degrees <= -180) turn += Angle.FromDegrees(360);
                int a = IRound(turn.Degrees, 10);
                Thing theAngle = theUKS.GetOrAddThing("angle" + a, "Angle");
                r = theUKS.AddStatement(currentShape, "go*", theAngle);
                r.TimeToLive = TimeSpan.FromSeconds(10);
            }
        }

        int IRound (float number, int multiple = 1)
        {
            float retVal = (float)Round(number / multiple) * multiple;
            return (int)retVal;
        }

        public Thing AddShapeToLibrary()
        {
            Thing currentShape = theUKS.Labeled("currentShape0");
            if (currentShape == null) return null;
            if (currentShape.HasRelationshipWithAncestorLabeled("distance") == null) return null;
            theUKS.GetOrAddThing("StoredShape", "Visual");
            Thing newShape = theUKS.GetOrAddThing("storedShape*", "StoredShape");
            //TODO: fix this so we can indicate which item we mearn
            foreach (Relationship r in currentShape.Relationships)
            {
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