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
        public override void Fire()
        {
            Init();

            GetUKS();
            if (UKS == null) return;
            UKS.GetOrAddThing("Sense", "Thing");
            UKS.GetOrAddThing("Visual", "Sense");
            Thing tCorners = UKS.GetOrAddThing("corner", "Visual");

            //check to see if there are any new corners which need checking
            foreach (Thing tCorner in tCorners.Children)
                if (tCorner.lastFiredTime > lastScan)
                    goto processingNeeded;
            return;

        processingNeeded:
            lastScan = DateTime.Now;

            CreateShapeFromCorners();

            UpdateDialog();
        }

        //note there are two kinds of corners
        //1. seen in space which has a position and an angle (descendent of visual | 
        //2. abstract which has only an angle (perhaps a relative orientation)

        void CreateShapeFromCorners()
        {
            //UKS setup
            Thing currentShape = UKS.GetOrAddThing("currentShape", "Visual");
            UKS.GetOrAddThing("Distance", "Visual");
            UKS.GetOrAddThing("Angle", "Visual");
            UKS.GetOrAddThing("Go", "RelationshipType");
            UKS.GetOrAddThing("Turn", "RelationshipType");

            //clear out any previous currentShape
            foreach (var r in currentShape.Relationships)
                currentShape.RemoveRelationship(r);

            Thing outline = UKS.Labeled("outline0");
            if (outline == null) return;
            var corners = outline.GetRelationshipsWithAncestor(UKS.Labeled("corner"));

            //setup to normalize the distance
            float maxDist = 0;
            for (int i = 0; i < corners.Count; i++)
            {
                var corner = corners[i].T.V as ModuleVision.Corner;
                if (corner == null) continue;
                int next = i + 1; if (next >= corners.Count) next = 0;
                var nextCorner = corners[next].T.V as ModuleVision.Corner;
                float dist = (nextCorner.location - corner.location).R;
                if (dist > maxDist) maxDist = dist;
            }

            //add the corners to the currentShape
            for (int i = 0; i < corners.Count; i++)
            {
                var corner = corners[i].T.V as ModuleVision.Corner;
                if (corner == null) continue;
                int next = i + 1; 
                if (next >= corners.Count) next = 0;
                var nextCorner = corners[next].T.V as ModuleVision.Corner;
                float dist = (nextCorner.location - corner.location).R;
                dist /= maxDist;
                int a = (int)Round(corner.angle.Degrees/10)*10;
                a = a / 10;
                a = a * 10;
                string distName = "distance."+((int)Round(dist * 10)).ToString();
                if (Round(dist*10) == 10) distName = "distance1.0";
                Thing theDist = UKS.GetOrAddThing(distName, "distance");
                UKS.AddStatement(currentShape, "go*", theDist);
                Thing theAngle = UKS.GetOrAddThing("angle" + a, "Angle");
                UKS.AddStatement(currentShape, "turn*", theAngle);
            }


            Thing foundShape = SearchForShape(out float score);
            foundShape?.SetFired();
        }

        public void AddShapeToLibrary()
        {
            UKS.GetOrAddThing("StoredShape", "Visual");
            Thing newShape = UKS.GetOrAddThing("shape*", "StoredShape");
            Thing currentShape = UKS.GetOrAddThing("currentShape", "Visual");
            foreach (Relationship r in currentShape.Relationships)
            {
                newShape.AddRelationship(r.target, r.relType);
            }
        }
        Thing SearchForShape(out float bestScore)
        {
            Thing storedShapes = UKS.GetOrAddThing("StoredShape", "Visual");
            Thing currentShape = UKS.GetOrAddThing("currentShape", "Visual");
            Thing bestMatch = null;
            bestScore = 0;
            foreach (Thing testShape in storedShapes.Descendents)
            {
                float score = ScoreMatch(currentShape, testShape);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = testShape;
                }
            }
            return bestMatch; ;
        }
        float ScoreMatch (Thing t1, Thing t2)
        {
            float score = 0;
            //TODO add some score for things which are nearby
            List<Thing> t1Targets = new List<Thing>();
            List<Thing> t2Targets = new List<Thing>();
            foreach (Relationship r in t1.Relationships)
                t1Targets.Add(r.target);
            foreach (Relationship r in t2.Relationships)
                t2Targets.Add(r.target);

            for (int i = 0; i < t1Targets.Count; i++)
            {
                Thing t = t1Targets[i];
                if (t2Targets.Contains(t)) 
                { 
                    t2Targets.Remove(t);
                    t1Targets.Remove(t);
                    score++;
                    i--;
                }
            };
            for (int i = 0; i < t2Targets.Count; i++)
            {
                Thing t = t2Targets[i];
                if (t1Targets.Contains(t))
                {
                    t2Targets.Remove(t);
                    t1Targets.Remove(t);
                    score++;
                    i--;
                }
            }

            //normalize the score 
            score /= t1Targets.Count+t2Targets.Count;
            return score;
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