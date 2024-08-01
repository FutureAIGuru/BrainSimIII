using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using static System.Math;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.CodeDom;

namespace BrainSimulator.Modules.Vision
{

    public class HoughTransform
    {
        // Hough Transform parameters
        public int numAngles = 180; // Number of angles to consider (e.g., 180 degrees)
        private double angleStep; // Angle step size
        private double rhoStep = 1; // Rho step size (can be adjusted based on image resolution)
        public int maxDistance; // Maximum possible distance from the origin to the image corner
        public List<Point>[,] accumulator; // Accumulator array
        //private float[,] boundaries;
        private List<PointPlus> boundaryPoints;
        int minLength = 4;
        int minVotes = 15;

        // Constructor
        public HoughTransform(int width, int height)
        {
            // Calculate maximum distance from the origin to the image corner
            maxDistance = (int)Math.Sqrt(width * width + height * height);

            // Calculate angle step size
            angleStep = PI / numAngles;

            // Initialize accumulator array
            accumulator = new List<Point>[maxDistance * 2, numAngles]; // Double the size for negative rho values
            for (int rIndex = 0; rIndex < accumulator.GetLength(0); rIndex++)
                for (int thetaIndex = 0; thetaIndex < accumulator.GetLength(1); thetaIndex++)
                    accumulator[rIndex, thetaIndex] = new List<Point>();
        }

        //same as above but works with a list of boundary points instead of an array
        public void Transform2(List<PointPlus> boundaries)
        {
            boundaryPoints = boundaries;
            foreach (var pt in boundaryPoints)
                // Loop over all possible lines passing through the edge pixel
                for (int thetaIndex = 0; thetaIndex < numAngles; thetaIndex++)
                {
                    double theta = thetaIndex * angleStep;
                    Angle a = theta;
                    double rho = pt.X * Cos(theta) + pt.Y * Sin(theta);
                    int rhoIndex = (int)Round(rho / rhoStep + maxDistance);

                    accumulator[rhoIndex, thetaIndex].Add(pt);
                }
        }

        public List<Segment> FindSegments()
        {
            List<Segment> segments = new List<Segment>();

            List<PointPlus> availablePointList = new List<PointPlus>();
            //copy the list of boundarypoints to a temp list
            foreach (var pt in boundaryPoints)
                availablePointList.Add(pt);

            //pick a boundary point from the temp list.
            while (availablePointList.Count > 0)
            {
                //find the best segment through/near that point
                Segment s1 = BestSegmentThroughPoint(availablePointList[0], availablePointList);
                //if there is none at least 3 pixels long, remove the point from the temp list, continue
                if (s1 == null || segments.Contains(s1))
                {
                    availablePointList.RemoveAt(0);
                    continue;
                }

                //find all the points near the segment
                List<Point> linePoints = new();
                foreach (var pt in boundaryPoints)
                {
                    float dist = Utils.DistancePointToSegment(s1, pt);
                    if (Abs(dist) < .5)
                    {
                        linePoints.Add((pt));
                    }
                }
                OrderPointsAlongSegment(ref linePoints);
                //move this to "best" below
                int start = 0; int last = linePoints.Count - 1;
                OptimizeSegment2(linePoints, ref start, ref last);
                s1.P1 = linePoints[start];
                s1.P2 = linePoints[last];
                ExtendSegment(s1);

                //add segment to segment list
                if (!AddSegmentToList(segments, s1))
                {
                    //if it is already there, hack to get out of loop
                    availablePointList.RemoveAt(0);
                    continue;
                }

                //what points are actually on the segment
                linePoints = new();
                foreach (var pt in availablePointList)
                {
                    float dist = Utils.DistancePointToSegment(s1, pt);
                    if (Abs(dist) < .5)
                    {
                        linePoints.Add((pt));
                    }
                }

                //delete all but the first and the last 2 pts
                //(there may be 2 very close points at one end and we should check out both
                for (int i = 2; i <= linePoints.Count-2; i++)
                {
                    Point pt = linePoints[i];
                    availablePointList.Remove(pt);
                }
                //loop until the temp list is empty
            }


            return segments;
        }

        private Segment BestSegmentThroughPoint(PointPlus pt, List<PointPlus> availablePoints)
        {
            //Find the longest segment through the given point

            //start from the given point
            //Get all the points within 2 pixels 
            List<PointPlus> candidatePts = GetNearbyPoints(pt, 2.5f, availablePoints);
            List<Segment> candidateSegs = new();
            //get a list of all segments at least 3 pixels long
            foreach (var pt1 in candidatePts)
                foreach (var pt2 in candidatePts)
                {
                    if (pt1 == pt2) continue;
                    Segment s = new Segment(pt1, pt2);
                    if (s.Length < 2) continue;
                    float weight = GetSegmentWeight3(s);
                    if (weight >= 2.5f)
                        AddSegmentToList(candidateSegs, s);
                }
            //extend each of these segments
            float weightToBeat = 3.0f;
            foreach (var seg in candidateSegs)
            {
                weightToBeat = GetSegmentWeight3(seg);
                bool improved = true;
                while (improved)
                {
                    improved = false;
                    candidatePts = GetNearbyPoints(seg.P1, 1.7f, availablePoints);
                    candidatePts.AddRange(GetNearbyPoints(seg.P2, 1.7f, availablePoints));
                    foreach (var pt1 in candidatePts)
                        foreach (var pt2 in candidatePts)
                        {
                            if (pt1 == pt2) continue;
                            Segment s = new Segment(pt1, pt2);
                            float weight = GetSegmentWeight3(s);
                            if (weight > weightToBeat)
                            {
                                seg.P1 = s.P1;
                                seg.P2 = s.P2;
                                weightToBeat = weight;
                                improved = true;
                            }
                        }
                }
            }
            //choose the longest
            weightToBeat = 3; //minpoints? minlength?
            Segment bestSegment = null;
            foreach (var seg in candidateSegs)
            {
                float weight = GetSegmentWeight3(seg);
                if (weight > weightToBeat && seg.Length >= 3)
                {
                    bestSegment = seg;
                    weightToBeat = weight;
                }
            }
            return bestSegment;
        }
        private bool AddSegmentToList(List<Segment> segs, Segment seg)
        {
            if (segs.FindFirst(x => (x.P1.Near(seg.P1, 1) && x.P2.Near(seg.P2, 1)) || (x.P1.Near(seg.P2, 1) && x.P2.Near(seg.P1, 1))) == null)
            {
                seg.debugIndex = segs.Count;
                segs.Add(seg);
                return true;
            }
            else
                return false;
        }
        private List<PointPlus> GetNearbyPoints(PointPlus pt, float dist, List<PointPlus> availablePoints)
        {
            //this is exhaustive and can be replaced with a QuadTree for performance
            List<PointPlus> points = new();
            foreach (PointPlus point in availablePoints)
            {
                float d1 = (point - pt).R;
                if (d1 <= dist)
                    points.Add(point);
            }
            return points;
        }

        public List<Segment> FindSegments2()
        {
            int maxRho = accumulator.GetLength(0);
            int maxTheta = accumulator.GetLength(1);
            List<(float weight, Segment s)> testSegments = new();

            for (int rhoIndex = 0; rhoIndex < maxRho; rhoIndex++)
            {
                for (int thetaIndex = 0; thetaIndex < maxTheta; thetaIndex++)
                {
                    if (accumulator[rhoIndex, thetaIndex].Count < minVotes) continue;
                    //if (rhoIndex != 99) continue;
                    //if (thetaIndex > 92 || thetaIndex < 88) continue;


                    Point p1, p2;
                    float rho1 = rhoIndex - maxDistance;

                    if (thetaIndex == 0) //special case for vertical lines
                    {
                        p1 = new(rho1, 0); p2 = new(rho1, 100);
                    }
                    else
                    {
                        double fTheta = thetaIndex * Math.PI / numAngles;
                        double b = rho1 / Math.Sin(fTheta);
                        double m = -Math.Cos(fTheta) / Math.Sin(fTheta);
                        p1 = new(0, b); p2 = new(100, 100 * m + b);
                    }

                    // Calculate points on the line in the Cartesian coordinate system
                    List<(float dist, Point pt)> linePoints = new();
                    foreach (var pt in boundaryPoints)
                    {
                        float dist = Utils.DistancePointToLine2(pt, p1, p2);
                        if (Abs(dist) < .8)
                        {
                            //if (dist <= .5) dist = 0;
                            linePoints.Add((dist, pt));
                        }
                    }
                    if (linePoints.Count < 7) continue;
                    //find contiguous segments within the Line
                    OrderPointsAlongSegment2(ref linePoints);
                    int start = 0; //possible start of segment
                    int last = linePoints.Count - 1;
                    PointPlus startPt = new(linePoints[start].pt);
                    PointPlus lastPt = new(linePoints.Last().pt);

                    for (int i = 1; i < linePoints.Count; i++)
                    {
                        PointPlus currPt = new(linePoints[i].pt);
                        PointPlus prevPt = new(linePoints[i - 1].pt);
                        if ((prevPt - currPt).R <= 3) //allow for skipped pixels
                        {
                            //pts are near enough, keep going
                        }
                        else
                        {
                            //pts are not contiguous --add the previous section as a segment
                            last = i - 1;
                            OptimizeSegment(linePoints, ref start, ref last);

                            if (last - start > minLength)
                            {
                                float confidence =
                                    GetConfidence(linePoints, start, last);
                                if (confidence > minLength)
                                    AddTheSegment(testSegments, linePoints, confidence, linePoints[start].pt, linePoints[last].pt);
                            }
                            start = i;
                        }
                    }
                    if (start < last - minLength)
                    {
                        last = linePoints.Count - 1;
                        //shorten the segment to remove points which might be the beginning of another segment
                        OptimizeSegment(linePoints, ref start, ref last);

                        if (last - start > minLength)
                        {
                            float confidence = GetConfidence(linePoints, start, last);
                            float err = GetAverageError(linePoints, start, last);
                            if (err < 0.5 && confidence > minLength)
                                AddTheSegment(testSegments, linePoints, confidence, linePoints[start].pt, linePoints[last].pt);
                        }
                    }
                }
            }
            //when we get here, we've found all the segments and assigned them a weight value based on their hit rate of boundary pixels

            testSegments = testSegments.OrderByDescending(x => x.weight).ToList();

            for (int i = 0; i < testSegments.Count - 1; i++)
            {
                for (int j = i + 1; j < testSegments.Count; j++)
                {
                    if (testSegments[i].s.debugIndex == 907)
                    { }
                    if (testSegments[j].s.debugIndex == 907)
                    { }
                    if (MergeSegments(testSegments[i], testSegments[j], out var sNew))
                    {
                        testSegments[i] = sNew;
                        testSegments.RemoveAt(j);
                        j = i; //having changed the {i} entry, restart this inner loop
                    }
                }
            }
            foreach (var testSegment in testSegments)
            {
                ExtendSegment(testSegment.s);
            }

            return testSegments.Select(x => x.s).ToList();
        }

        private void ExtendSegment(Segment s)
        {
            float weight = GetSegmentWeight3(s);
            Segment s2 = new Segment(Utils.ExtendSegment(s.P1, s.P2, 1, true), s.P2); ;
            float weight1 = GetSegmentWeight3(s2);
            while (weight1 > weight + 0.25f)
            {
                s.P1 = s2.P1;
                s2 = new Segment(Utils.ExtendSegment(s.P1, s.P2, 1, true), s.P2);
                weight = weight1;
                weight1 = GetSegmentWeight3(s2);
            }
            s2 = new Segment(s.P1, Utils.ExtendSegment(s.P1, s.P2, 1, false));
            weight1 = GetSegmentWeight3(s2);
            while (weight1 > weight + 0.25f)
            {
                s.P2 = s2.P2;
                s2 = new Segment(s.P1, Utils.ExtendSegment(s.P1, s.P2, 1, false));
                weight = weight1;
                weight1 = GetSegmentWeight3(s2);
            }
        }

        private static void AddTheSegment(List<(float weight, Segment s)> testSegments, List<(float dist, Point pt)> linePoints, float confidence, PointPlus startPt, PointPlus lastPt)
        {
            //build the new segment
            Segment s2 = new Segment(startPt, lastPt);
            if (startPt.Y > lastPt.Y)
                s2 = new Segment(lastPt, startPt);
            s2.debugIndex = testSegments.Count;


            //only add the segment if it's not already there
            if (testSegments.FindFirst(x => x.s.P1 == startPt && x.s.P2 == lastPt) == default)
            {
                if (testSegments.Count == 1005)
                { }
                testSegments.Add((confidence, s2));
            }
        }

        private static void OptimizeSegment2(List<Point> linePoints, ref int start, ref int last)
        {
            //try contracting the segment by a few pts on each end to see if it matches better
            float currWeight = GetAverageError2(linePoints, start, last);
            if (currWeight < 0.1) return;

            int bestStart = start;
            int bestLast = last;
            float bestWeight = currWeight;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    if ((last - j) - (start + i) < 3) //don't let the segment get smaller than 3
                        continue;
                    float testWeight = GetAverageError2(linePoints, start + i, last - j);
                    if (testWeight < bestWeight)
                    {
                        bestWeight = testWeight;
                        bestLast = last - j;
                        bestStart = start + i;
                    }
                }
            start = bestStart;
            last = bestLast;
        }
        private static void OptimizeSegment(List<(float dist, Point pt)> linePoints, ref int start, ref int last)
        {
            //try contracting the segment by a few pts on each end to see if it matches better
            float currWeight = GetAverageError(linePoints, start, last);
            for (int i = 0; i < 5; i++)
            {
                if (last - start < 3) break;
                float testWeight = GetAverageError(linePoints, start + 1, last);
                if (testWeight < currWeight)
                {
                    start += 1;
                    currWeight = testWeight;
                }
                testWeight = GetAverageError(linePoints, start, last - 1);
                if (testWeight < currWeight)
                {
                    last -= 1;
                    currWeight = testWeight;
                }
                testWeight = GetAverageError(linePoints, start + 1, last - 1);
                if (testWeight < currWeight)
                {
                    last -= 1;
                    currWeight = testWeight;
                }
            }
        }

        private static float GetAverageError2(List<Point> linePoints, int start, int last)
        {
            float aveError = 0;
            Segment s = new Segment(linePoints[start], linePoints[last]);
            for (int j = start; j <= last; j++)
            {
                float dist = Utils.DistancePointToSegment(s, linePoints[j]);
                //if (dist > 0.5) dist = 1;
                aveError += dist;
            }
            aveError /= (last - start + 1);
            return aveError;
        }
        private static float GetAverageError(List<(float dist, Point pt)> linePoints, int start, int last)
        {
            float aveError = 0;
            Segment s = new Segment(linePoints[start].pt, linePoints[last].pt);
            for (int j = start; j <= last; j++)
            {
                float dist = Utils.DistancePointToSegment(s, linePoints[j].pt);
                if (dist > 0.5) dist = 1;
                aveError += dist;
            }
            aveError /= (last - start + 1);
            return aveError;
        }
        private static float GetConfidence(List<(float dist, Point pt)> linePoints, int start, int last)
        {
            float aveWeight = 0;
            Segment s = new Segment(linePoints[start].pt, linePoints[last].pt);
            for (int j = start; j <= last; j++)
                aveWeight += 1 - Utils.DistancePointToSegment(s, linePoints[j].pt);
            return aveWeight;
        }

        float GetSegmentWeight3(Segment s)
        {
            float retVal = 0;
            foreach (var pt in boundaryPoints)
            {
                float dist = Utils.DistancePointToSegment(s, pt);
                if (dist < .5)
                {
                    retVal += 1 - dist;
                }
            }
            return retVal;
        }


        //returns true if one of the segments is unnecessary and the first should be replaced by sNew
        bool MergeSegments((float weight, Segment s) s1, (float weight, Segment s) s2, out (float weight, Segment s) sNew)
        {
            sNew = s1;
            if (s1.weight < s2.weight)
                sNew = s2;
            if ((s1.s.debugIndex == 1005 && s2.s.debugIndex == 1013) ||
                (s2.s.debugIndex == 1005 && s1.s.debugIndex == 1013))
            { }

            if (Utils.FindIntersection(s1.s, s2.s, out PointPlus intersection, out Angle a))
            {

            }

            //are the segments nearly the same angle?
            float angleDiff = Abs((s1.s.Angle - s2.s.Angle).Degrees);
            float minLength = (float)Min(s2.s.Length, s1.s.Length);
            float angleLimit = ((Angle)Asin(4 / minLength)).Degrees; //must be within 4 pixels of parallel

            if (angleDiff > angleLimit && angleDiff < 180 - angleLimit || angleDiff > 180 + angleLimit && angleDiff < 360 - angleLimit) return false;
            //are the segments near each other?
            float d1 = Utils.DistanceBetweenTwoSegments(s1.s, s2.s);
            if (d1 > 5) return false;

            d1 = Utils.DistancePointToSegment(s1.s, s2.s.P1);
            float d2 = Utils.DistancePointToSegment(s1.s, s2.s.P2);
            float d3 = Utils.DistancePointToSegment(s2.s, s1.s.P1);
            float d4 = Utils.DistancePointToSegment(s2.s, s1.s.P2);

            //do the segments overlap?
            if (d1 < 2 || d2 < 2 || d3 < 2 || d4 < 2)
            {
                if (s1.s.debugIndex == 152)
                { }
                if (s2.s.debugIndex == 152)
                { }
                float v1 = GetSegmentWeight3(s1.s);
                float v2 = GetSegmentWeight3(s2.s);
                //do the segments extend one another?
                //mix and match endpoints go get segment with highest Weight
                PointPlus[] endPts = { new(s1.s.P1), new(s1.s.P2), new(s2.s.P1), new(s2.s.P2) };
                float maxDist = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = i + 1; j < 4; j++)
                    {
                        if (endPts[i] == endPts[j]) continue;
                        float dist = GetSegmentWeight3(new Segment(endPts[i], endPts[j]));
                        if (dist > maxDist)
                        {
                            sNew.s.P1 = endPts[i];
                            sNew.s.P2 = endPts[j];
                            sNew.weight = dist;
                            maxDist = dist;
                        }
                    }
                }
                return true;
            }
            return false;
        }


        void OrderPointsAlongSegment2(ref List<(float dist, Point pt)> points)
        {
            float bestDist = 0;
            if (points.Count == 0) return;
            Point p1 = points[0].pt;
            Point p2 = points[0].pt;
            foreach (var pa in points)
                foreach (var pb in points)
                {
                    float newDist = (float)(Sqrt((pa.pt.X - pb.pt.X) * (pa.pt.X - pb.pt.X) + (pa.pt.Y - pb.pt.Y) * (pa.pt.Y - pb.pt.Y)));
                    if (newDist > bestDist)
                    {
                        bestDist = newDist;
                        p1 = pa.pt;
                        p2 = pb.pt;
                    }
                }

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dSquared = dx * dx + dy * dy;

            // Project each point onto the direction vector and compute the projection scalar t
            var projectionScalars = points.Select(p => new
            {
                Point = p,
                Scalar = ((p.pt.X - p1.X) * dx + (p.pt.Y - p1.Y) * dy) / dSquared
            });
            // Sort the points based on the projection scalar
            points = projectionScalars.OrderBy(ps => ps.Scalar).Select(ps => ps.Point).ToList();
        }
        void OrderPointsAlongSegment(ref List<Point> points)
        {
            float bestDist = 0;
            if (points.Count == 0) return;
            Point p1 = points[0];
            Point p2 = points[0];
            foreach (Point pa in points)
                foreach (Point pb in points)
                {
                    float newDist = (float)(Sqrt((pa.X - pb.X) * (pa.X - pb.X) + (pa.Y - pb.Y) * (pa.Y - pb.Y)));
                    if (newDist > bestDist)
                    {
                        bestDist = newDist;
                        p1 = pa;
                        p2 = pb;
                    }
                }

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dSquared = dx * dx + dy * dy;

            // Project each point onto the direction vector and compute the projection scalar t
            var projectionScalars = points.Select(p => new
            {
                Point = p,
                Scalar = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / dSquared
            });
            // Sort the points based on the projection scalar
            points = projectionScalars.OrderBy(ps => ps.Scalar).Select(ps => ps.Point).ToList();
        }

    }
}
