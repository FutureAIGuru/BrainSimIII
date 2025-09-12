//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using static System.Math;

namespace BrainSimulator.Modules;
public partial class ModuleVision
{
    public class Arc : Corner
    {
        //an arc is defined by three (non-collinear) points
        //prevPt and nextPt are the endpoints of the arc and pt is any thirde point somewhere on the arc
        public Arc()
        {
            curve = true;
        }
        public Angle StartAngle
        {
            get
            {
                var circle = GetCircleFromThreePoints(pt, nextPt, prevPt);
                return (prevPt - circle.center).Theta;
            }
        }
        public Angle EndAngle
        {
            get
            {
                var circle = GetCircleFromThreePoints(pt, nextPt, prevPt);
                return (nextPt - circle.center).Theta;
            }
        }
        public Angle SweepAngle
        {
            get => EndAngle - StartAngle;
        }
        public Angle MidAngle
        {
            get
            {
                var circle = GetCircleFromThreePoints(pt, nextPt, prevPt);
                Angle midAngle = (pt - circle.center).Theta;

                //given the three points, calculate the midpoint of the arc
                //if the midAngle is not between start and end angles, go the other way areound the arc
                if (midAngle > StartAngle && midAngle > EndAngle)
                {
                    midAngle = (StartAngle + EndAngle) / 2;
                    midAngle += PI;
                }
                else if (midAngle < StartAngle && midAngle < EndAngle)
                {
                    midAngle = (StartAngle + EndAngle) / 2;
                    midAngle += PI;
                }
                else
                {
                    midAngle = (StartAngle + EndAngle) / 2;
                }
                pt = circle.center + new PointPlus(circle.radius, midAngle);
                //pt = cir.center + new PointPlus(cir.radius, midAngle);
                return midAngle;
            }
        }

        public override Angle angle
        {
            get
            {
                var circle = GetCircleFromThreePoints(pt, nextPt, prevPt);
                Angle startAngle = (prevPt - circle.center).Theta;
                Angle midAngle = (pt - circle.center).Theta;
                Angle endAngle = (nextPt - circle.center).Theta;

                //given the three points, calculate the midpoint of the arc
                //if the midAngle is not between start and end angles, go the other way areound the arc
                if (midAngle > startAngle && midAngle > endAngle)
                {
                    midAngle = (startAngle + endAngle) / 2;
                    midAngle += PI;
                }
                else if (midAngle < startAngle && midAngle < endAngle)
                {
                    midAngle = (startAngle + endAngle) / 2;
                    midAngle += PI;
                }
                else
                {
                    midAngle = (startAngle + endAngle) / 2;
                }
                pt = circle.center + new PointPlus(circle.radius, midAngle);
                //pt = cir.center + new PointPlus(cir.radius, midAngle);
                return midAngle;
            }
        }
        // Function to calculate the center and radius of the circle through three points
        public (PointPlus center, float radius) GetCircleFromArc()
        {
            return GetCircleFromThreePoints(pt, prevPt, nextPt);
        }
        public (PointPlus center, float radius) GetCircleFromThreePoints(PointPlus p1, PointPlus p2, PointPlus p3)
        {
            float x1 = p1.X, y1 = p1.Y;
            float x2 = p2.X, y2 = p2.Y;
            float x3 = p3.X, y3 = p3.Y;

            // Calculate the perpendicular bisectors of two segments
            float ma = (y2 - y1) / (x2 - x1);
            float mb = (y3 - y2) / (x3 - x2);

            // Calculate the center of the circle (intersection of the bisectors)
            float cx = (ma * mb * (y1 - y3) + mb * (x1 + x2) - ma * (x2 + x3)) / (2 * (mb - ma));
            float cy = -1 * (cx - (x1 + x2) / 2) / ma + (y1 + y2) / 2;

            PointPlus center = new PointPlus(cx, cy);

            // Calculate the radius of the circle
            float radius = (center - p1).R;

            return (center, radius);
        }
    }
}

