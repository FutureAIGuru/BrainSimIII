//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Math;
using UKS;

namespace BrainSimulator.Modules
{
    public class ModuleShape : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;


        // Fill this method in with code which will execute
        // once for each cycle of the engine
        DateTime lastScan = new DateTime();
        public Thing foundShape = null;
        public float confidence = 0;
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
            Thing hasShape = theUKS.GetOrAddThing("hasShape", "RelationshipType");
            foreach (Thing shape in theUKS.Labeled("currentShape").Children)
            {
                float bestValue = 0;
                //currehtShape has a set of attriburtes. We search the descendents of StoredShape for the
                // entry with the best match.
                Thing foundShape = theUKS.SearchForClosestMatch(shape, storedShapes, ref bestValue);
                if (foundShape != null)
                {
                    Relationship r = shape.AddRelationship(foundShape, hasShape);
                    r.Weight = bestValue;
                    foundShape.SetFired();
                }
            }
            //foundShape = SearchForShape(out float score);

            //if (foundShape == null || score < 0.5)
            //{
            //    foundShape = AddShapeToLibrary();
            //    confidence = 1;
            //}

            //confidence = score;
            UpdateDialog();
        }

        //note there are two kinds of corners
        //1. Transient: seen in space which has a position and an angle (descendent of visual | 
        //2. abstract: which has only an angle (perhaps a relative orientation)

        void CreateShapeFromCorners()
        {
            //UKS setup
            Thing currentShape = theUKS.GetOrAddThing("currentShape", "Visual");
            theUKS.GetOrAddThing("Distance", "Visual");
            theUKS.GetOrAddThing("Angle", "Visual");
            theUKS.GetOrAddThing("Size", "Visual");
            theUKS.GetOrAddThing("Go", "RelationshipType");
            theUKS.GetOrAddThing("Turn", "RelationshipType");

            //clear out any previous currentShape
            theUKS.DeleteAllChildren(currentShape);

            foreach (Thing outline in theUKS.Labeled("outline").Children)
            {
                ExtractShape(theUKS.GetOrAddThing("currentShape*",currentShape), outline);
            }
        }

        private void ExtractShape(Thing currentShape, Thing outline)
        {
            var corners = outline.GetRelationshipsWithAncestor(theUKS.Labeled("corner"));

            //setup to normalize the distance
            float maxDist = 0;
            for (int i = 0; i < corners.Count; i++)
            {
                var corner = corners[i].target.V as ModuleVision.Corner;
                if (corner == null) continue;
                int next = i + 1; if (next >= corners.Count) next = 0;
                var nextCorner = corners[next].target.V as ModuleVision.Corner;
                float dist = (nextCorner.location - corner.location).R;
                if (dist > maxDist) maxDist = dist;
            }

            //TODO: put any other attributes on your shape here
            //add the scale to the object
            Thing hasSize = theUKS.GetOrAddThing("hasSize", "RelationshipType");
            currentShape.AddRelationship(GetSizeThing(maxDist),hasSize);

            //add the corners to the currentShape
            for (int i = 0; i < corners.Count; i++)
            {
                var corner = corners[i].target.V as ModuleVision.Corner;
                if (corner == null) continue;
                int next = i + 1;
                if (next >= corners.Count) next = 0;
                var nextCorner = corners[next].target.V as ModuleVision.Corner;
                float dist = (nextCorner.location - corner.location).R;
                dist /= maxDist;
                int a = (int)Round(corner.angle.Degrees / 10) * 10;
                a = a / 10; //round angles to the nearest 10 degrees
                a = a * 10;
                string distName = "distance." + ((int)Round(dist * 10)).ToString();
                if (Round(dist * 10) == 10) distName = "distance1.0";
                Thing theDist = theUKS.GetOrAddThing(distName, "distance");
                theUKS.AddStatement(currentShape, "go*", theDist);
                Thing theAngle = theUKS.GetOrAddThing("angle" + a, "Angle");
                theUKS.AddStatement(currentShape, "turn*", theAngle);
            }
        }
        Thing GetSizeThing(float size)
        {
            string sizeName = "size" + (int)size / 10;
            return theUKS.GetOrAddThing(sizeName, "Size");
        }

        public Thing AddShapeToLibrary()
        {
            theUKS.GetOrAddThing("StoredShape", "Visual");
            Thing newShape = theUKS.GetOrAddThing("storedShape*", "StoredShape");
            Thing currentShape = theUKS.GetOrAddThing("currentShape", "Visual");
            foreach (Relationship r in currentShape.Relationships)
            {
                newShape.AddRelationship(r.target, r.relType);
            }
            return newShape;
        }
        Thing SearchForShape(out float bestValue)
        {
            Thing storedShapes = theUKS.GetOrAddThing("StoredShape", "Visual");
            Thing currentShape = theUKS.GetOrAddThing("currentShape", "Visual");

            bestValue = 0;

            //currehtShape has a set of attriburtes. We search the descendents of StoredShape for the
            // entry with the best match.
            Thing bestThing = theUKS.SearchForClosestMatch(currentShape, storedShapes, ref bestValue);

            return bestThing;
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