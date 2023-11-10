using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class PhysicalObject
    {
        // All other attributes are coming from this thing, so make sure we don't cache them!
        [XmlIgnore]
        public Thing sourceThing = null;

        public string handle { get; set; }
        public Point3DPlus center { get; set; }
        public Point3DPlus topleft { get; set; }
        public Point3DPlus topright { get; set; }
        public Point3DPlus bottomleft { get; set; }
        public Point3DPlus bottomright { get; set; }
        public double width { get; set; }

        public double height { get; set; }
        public string shape { get; set; }
        public string color { get; set; }
        public string visibility { get; set; }
        public string appearance { get; set; }
        public string trackid { get; set; }

        // Contructor to create a dynamic PhysicalObject based on a Thing PhysicalObject in the UKS
        public PhysicalObject(Thing t)
        {
            sourceThing = t;
            FromDict(t.GetRelationshipsAsDictionary());
        }

        // Method to refresh the PhysicalObject from the UKS if possible...
        public void RefreshFromSourceThingIfApplicable()
        {
            if (sourceThing == null) return;
            FromDict(sourceThing.GetRelationshipsAsDictionary());
        }

        public Dictionary<string, object> ToDict()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            RefreshFromSourceThingIfApplicable();
            dict.Add("cen", center);
            dict.Add("tpl", topleft);
            dict.Add("tpr", topright);
            dict.Add("btl", bottomleft);
            dict.Add("btr", bottomright);
            dict.Add("wid", width);
            dict.Add("hig", height);
            dict.Add("shp", shape);
            dict.Add("col", color);
            dict.Add("vsb", visibility);
            dict.Add("app", appearance);
            dict.Add("tid", trackid);

            return dict;
        }

        public Dictionary<string, object> MatchDict()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            RefreshFromSourceThingIfApplicable();
            dict.Add("cen", center);
            dict.Add("shp", shape);
            dict.Add("col", color);

            return dict;
        }

        public void FromDict(Dictionary<string, object> properties)
        {
            // remember to clone this since we don't want to inadvertently update the UKS
            if (properties.ContainsKey("cen"))
            {
                center = ((Point3DPlus)properties["cen"]).Clone();
            }
            if (properties.ContainsKey("tpl"))
            {
                topleft = ((Point3DPlus)properties["tpl"]).Clone();
            }
            if (properties.ContainsKey("tpr"))
            {
                topright = ((Point3DPlus)properties["tpr"]).Clone();
            }
            if (properties.ContainsKey("btl"))
            {
                bottomleft = ((Point3DPlus)properties["btl"]).Clone();
            }
            if (properties.ContainsKey("btr"))
            {
                bottomright = ((Point3DPlus)properties["btr"]).Clone();
            }

            // that does not apply to these (I think)
            if (properties.ContainsKey("wid"))
            {
                width = (float)properties["wid"];
            }
            if (properties.ContainsKey("len"))
            {
                width = (double)properties["len"];
            }
            if (properties.ContainsKey("hig"))
            {
                height = (double)properties["hig"];
            }
            if (properties.ContainsKey("shp") && properties["shp"] != null)
            {
                shape = properties["shp"].ToString();
            }
            if (properties.ContainsKey("col") && properties["col"] != null)
            {
                color = Utils.GetColorNameFromHSL((HSLColor)properties["col"]);
            }
            if (properties.ContainsKey("vsb"))
            {
                visibility = (string)properties["vsb"];
            }
            if (properties.ContainsKey("app"))
            {
                appearance = (string)properties["app"];
            }
            if (properties.ContainsKey("tid"))
            {
                trackid = (string)properties["tid"];
            }
        }

        public void ApplyViewMovement(Angle averageTheta, Angle averagePhi)
        {
            RefreshFromSourceThingIfApplicable();
            center.Theta += averageTheta;
            center.Phi += averagePhi;
        }

        public void ApplyCameraAngles(Angle pan, Angle tilt)
        {
            RefreshFromSourceThingIfApplicable();
            center.Theta += pan;
            center.Phi += tilt;
        }

        public override string ToString()
        {
            RefreshFromSourceThingIfApplicable();
            string result = "";
            result += color + " " + shape + " " + visibility;
            return result;
        }

        public void CompensatePointsFor3DModel()
        {
            center.Theta -= Angle.FromDegrees(270);// + Sallie.BodyAngle;
            if (topleft != null && topright != null && bottomleft != null && bottomright != null)
            {
                topleft.Theta -= Angle.FromDegrees(270);
                topright.Theta -= Angle.FromDegrees(270);
                bottomleft.Theta -= Angle.FromDegrees(270);
                bottomright.Theta -= Angle.FromDegrees(270);
            }
        }
    }

    // This class is used to accumulate the changes in detected objects 
    // between the output of the Module3DBoundingBox objects, and those
    // stored in a previous cycle in the ModuleMentalModel class.
    // These are then used to detect various motions of objects.
    public class MotionDetector
    {
        private List<MovementDelta> deltaList = new List<MovementDelta>();

        // constructor ensures the list is initialized.
        public MotionDetector()
        {
            deltaList = new List<MovementDelta>();
        }

        // this clears the list if we wish to reuse an existing MotionDetector
        // for a new comparison of two sets.
        public void Clear()
        {
            deltaList.Clear();
        }

        // This method adds a pair of PhysicalObjects that match to the set. 
        public void Add(PhysicalObject newOne, Thing t)
        {
            PhysicalObject oldOne = new PhysicalObject(t);
            if (oldOne.visibility == "Visible" && newOne.visibility == "Visible")
            {
                deltaList.Add(new MovementDelta(newOne, t));
            }
        }

        public void HandleApparentMotion(ModuleMentalModel theMentalModel)
        {
            if (theMentalModel == null) return;
            if (Math.Abs(theMentalModel.LastMove.X) > 0.1f)
            {
                //TODO handle change in Z
                foreach (var delta in deltaList)
                {
                 //   delta.UpdateDistances(theMentalModel);
                }
                theMentalModel.LastMove = new();
            }
        }

        // This method determines the average horizontal movement between two inputs
        // in the list of matched objects. It looks at all five Point3DPlus's for the objects.
        public Angle EstimateHorizontalAngularMotion()
        {
            double ThetaAverage = 0;
            if (deltaList.Count == 0) return (Angle)0;
            foreach (MovementDelta delta in deltaList)
            {
                PhysicalObject oldObj = new PhysicalObject(delta.tInMentalModel);
                ThetaAverage += delta.newObj.center.Theta - oldObj.center.Theta;
            }
            ThetaAverage /= deltaList.Count;
            return (Angle)ThetaAverage;
        }

        // This method determines the average vertical movement between two inputs
        // in the list of matched objects. It looks at all five Point3DPlus's for the objects.
        public Angle EstimateVerticalAngularMotion()
        {
            double PhiAverage = 0;
            if (deltaList.Count == 0) return (Angle)0;
            foreach (MovementDelta delta in deltaList)
            {
                PhysicalObject oldObj = new PhysicalObject(delta.tInMentalModel);
                PhiAverage += delta.newObj.center.Phi - oldObj.center.Phi;
            }
            PhiAverage /= (deltaList.Count);
            return (Angle)PhiAverage;
        }
    }

    public class MovementDelta
    {
        public PhysicalObject newOne;
        public Thing tInMentalModel;

        public MovementDelta(PhysicalObject newObject, Thing t)
        {
            newOne = newObject;
            tInMentalModel = t;
        }

        public PhysicalObject newObj { get => newOne; }

        // override the ToString() method so the uniqueness is conserved for the UKS.
        public override string ToString()
        {
            PhysicalObject oldOne = new PhysicalObject(tInMentalModel);
            return oldOne.ToString() + " " + newOne.ToString();
        }

        public double CalculateMovement()
        {
            double result = newOne.center.Theta * newOne.center.Phi;

            return result;
        }
        /*
        public void UpdateDistances(ModuleMentalModel theMentalModel)
        {
            // update the R's for each point...
            PhysicalObject oldObj = new PhysicalObject(tInMentalModel);
            newObj.center = UpdateOneDistance(theMentalModel, oldObj.center, newObj.center);
            newObj.topleft = UpdateOneDistance(theMentalModel, oldObj.topleft, newObj.topleft);
            newObj.topright = UpdateOneDistance(theMentalModel, oldObj.topright, newObj.topright);
            newObj.bottomleft = UpdateOneDistance(theMentalModel, oldObj.bottomleft, newObj.bottomleft);
            newObj.bottomright = UpdateOneDistance(theMentalModel, oldObj.bottomright, newObj.bottomright);

            // create an update Dictionary and update properties...
            Dictionary<string, object> updateDict = new();
            updateDict.Add("cen", newObj.center);
            theMentalModel.UpdateProperties(tInMentalModel, updateDict);
        }
        */
//        public static Point3DPlus UpdateOneDistance(ModuleMentalModel theMentalModel, Point3DPlus oldObj, Point3DPlus newObj)
        public static Point3DPlus UpdateOneDistance(Point3DPlus motionVector, Point3DPlus oldObj, Point3DPlus newObj)
        {
            Point3DPlus result = newObj;
            result.Conf = oldObj.Conf;

            // We need the motion vector to restore the old Point3DPlus since it was updated by the Mental Model
            //Point3DPlus motionVector = new Point3DPlus(-theMentalModel.LastMove.X, 0f, 0f);
            Point3DPlus previousVector = oldObj - motionVector;

            if (oldObj.R < 5 || newObj.R < 5)
            {
                Debug.WriteLine("ASA: R too small, skipping");
                return newObj;
            }
            if (Math.Abs(newObj.Theta - previousVector.Theta) < Angle.FromDegrees(0.25f))
            {
                Debug.WriteLine("ASA: angle change too small, skipping");
                return newObj;
            }
            Angle oldAngle = previousVector.Theta - motionVector.Theta;
            if (oldAngle < 0) oldAngle *= -1;
            if (oldAngle > Angle.FromDegrees(90)) oldAngle = Math.PI - oldAngle;

            Angle newAngle = newObj.Theta - motionVector.Theta;
            if (newAngle < 0) newAngle *= -1;
            if (newAngle > Angle.FromDegrees(90)) newAngle = Math.PI - newAngle;

            Angle A = Math.PI - newAngle - oldAngle;
            if (A < 0) A *= -1;
            if (Math.Abs(oldAngle.Degrees + newAngle.Degrees + A.Degrees - 180) > 3)
            {
                Debug.WriteLine("ASA: sum of angles not 180, skipping");
                return newObj;
            }

            double newR = Math.Sin(newAngle) * motionVector.R / Math.Sin(A);

            if (Math.Abs(A) < Angle.FromDegrees(0.5f))
            {
                Debug.WriteLine("ASA: angle A too small, skipping");
                return newObj;
            }

            result = newObj;
            result.R = (float)newR;
            result.Conf += 0.1f;
            if (result.Conf > 1) result.Conf = 1;

            // Debug.WriteLine("ASA: result reliable, using it");
            return result;
        }
    }
}
