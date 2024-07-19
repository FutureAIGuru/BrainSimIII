using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using static System.Math;
using System.Diagnostics;

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
        public List<Tuple<int, int, int, float>> localMaxima;
        private float[,] boundaries;
        private List<Point> boundaryPoints;
        int minLength = 4;

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
        // Perform Hough Transform
        public void Transform(float[,] edges1)
        {
            boundaries = edges1;
            //check every point in the boundary array
            for (int x = 0; x < boundaries.GetLength(0); x++)
            {
                for (int y = 0; y < boundaries.GetLength(1); y++)
                {
                    //is that point a boundary?
                    float pixel = boundaries[x, y];
                    if (pixel == 1)
                    {
                        // Loop over all possible lines passing through the edge pixel
                        for (int thetaIndex = 0; thetaIndex < numAngles; thetaIndex++)
                        {
                            double theta = thetaIndex * angleStep;
                            Angle a = theta;
                            double rho = x * Cos(theta) + y * Sin(theta);
                            int rhoIndex = (int)Round(rho / rhoStep + maxDistance);

                            accumulator[rhoIndex, thetaIndex].Add(new Point(x, y));
                        }
                    }
                }
            }
        }

        //same as above but works with a list of boundary points instead of an array
        public void Transform2(List<Point> boundaries)
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
        public void FindArcs()
        {
            int maxRho = accumulator.GetLength(0);
            int maxTheta = accumulator.GetLength(1);
            for (int rhoIndex = 0; rhoIndex < maxRho; rhoIndex++)
            {
                for (int thetaIndex = 0; thetaIndex < maxTheta; thetaIndex++)
                { }
            }
        }
        public List<Segment> FindSegments()
        {
            int maxRho = accumulator.GetLength(0);
            int maxTheta = accumulator.GetLength(1);
            List<(float weight, Segment s)> testSegments = new();

            for (int rhoIndex = 0; rhoIndex < maxRho; rhoIndex++)
            {
                for (int thetaIndex = 0; thetaIndex < maxTheta; thetaIndex++)
                {
                    if (accumulator[rhoIndex, thetaIndex].Count < minLength) continue;
                    //if (rhoIndex != 99) continue;
                    //if (thetaIndex > 92 || thetaIndex < 88) continue;


                    Point p1, p2;
                    float rho1 = rhoIndex - maxDistance;

                    if (thetaIndex == 0)
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
                        if (Abs(dist) < .9)
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
                                float confidence = GetConfidence(linePoints, start, last);
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
                            if (confidence > minLength)
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
            s2 = new Segment(s.P1,Utils.ExtendSegment(s.P1, s.P2, 1, false));
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
            s2.theColor = testSegments.Count;
            

            //only add the segment if it's not already there
            if (testSegments.FindFirst(x => x.s.P1 == startPt && x.s.P2 == lastPt) == default)
            {
                if (testSegments.Count == 211)
                { }
                testSegments.Add((confidence, s2));
            }
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
            }
        }

        private static float GetAverageError(List<(float dist, Point pt)> linePoints, int start, int last)
        {
            float aveError = 0;
            Segment s = new Segment(linePoints[start].pt, linePoints[last].pt);
            for (int j = start; j <= last; j++)
            {
                float dist = Utils.DistancePointToSegment(s, linePoints[j].pt);
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
                if (dist < .95)
                {
                    retVal += 1 - dist;
                }
            }
            return retVal;
        }


        //returns true if one of the segments is unnecessary and the first should be replaced by sNew
        bool MergeSegments((float weight, Segment s) s1, (float weight, Segment s) s2, out (float weight, Segment s) sNew)
        {

            if ((s1.s.theColor == 145 || s2.s.theColor == 145) && (s1.s.theColor == 31 || s2.s.theColor == 31))
            { }

            sNew = s1;
            if (s1.weight < s2.weight)
                sNew = s2;
            //are the segments nearly the same angle?
            float angleDiff = Abs((s1.s.Angle - s2.s.Angle).Degrees);
            float minLength = (float)Min(s2.s.Length, s1.s.Length);
            float angleLimit = ((Angle)Asin(4 / minLength)).Degrees; //must be within 4 pixels of parallel

            if (angleDiff > angleLimit && angleDiff < 180 - angleLimit || angleDiff > 180 + angleLimit && angleDiff < 360 - angleLimit) return false;
            //are the segments near each other?
            float d1 = Utils.DistanceBetweenTwoSegments(s1.s,s2.s);
            if (d1 > 5) return false;

            d1 = Utils.DistancePointToSegment(s1.s, s2.s.P1);
            float d2 = Utils.DistancePointToSegment(s1.s, s2.s.P2);
            float d3 = Utils.DistancePointToSegment(s2.s, s1.s.P1);
            float d4 = Utils.DistancePointToSegment(s2.s, s1.s.P2);

            //do the segments overlap?
            if  (d1 < 2 || d2 < 2 || d3 < 2 || d4 < 2)
            {
                if ((s1.s.theColor == 250 || s2.s.theColor == 250) && (s1.s.theColor == 250 || s2.s.theColor == 250))
                { }
                //do the segments extend one another?
                //mix and match endpoints go get segment with highest Weight
                PointPlus[] endPts = { new(s1.s.P1), new(s1.s.P2), new(s2.s.P1), new(s2.s.P2) };
                float maxDist = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = i + 1; j < 4; j++)
                    {
                        if (endPts[i] == endPts[j]) continue;
                        float dist = GetSegmentWeight3(new Segment(endPts[i],endPts[j]));
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


/*        public void FindMaxima()
        {
            //find the top vote-getters
            localMaxima = new List<Tuple<int, int, int, float>>(); // (votes,rhoIndex,thetaIndex,lineVotes)

            int maxRho = accumulator.GetLength(0);
            int maxTheta = accumulator.GetLength(1);
            bool[,] visited = new bool[maxRho, maxTheta];
            int minRhoDifference = 1; //pixels
            int minThetaDifference = 10; //degrees


            for (int rhoIndex = 0; rhoIndex < maxRho; rhoIndex++)
            {
                for (int thetaIndex = 0; thetaIndex < maxTheta; thetaIndex++)
                {
                    if (visited[rhoIndex, thetaIndex]) continue;
                    if (thetaIndex == 0 && rhoIndex == 226)
                    { }
                    float votes = accumulator[rhoIndex, thetaIndex].Count;
                    if (votes < minLength) continue;
                    float votesLine = LineWeight(rhoIndex, thetaIndex);
                    if (votesLine < minLength) continue;

                    int votes2 = ContiguousPixels(accumulator[rhoIndex, thetaIndex]);
                    if (votes2 < minLength) continue;

                    int[] dRho = { -1, 1, 0, 0, 1, 1, -1, -1 }; //only use 4?
                    int[] dTheta = { 0, 0, -1, 1, 1, -1, 1, -1 };

                    Stack<(int, int)> stack = new Stack<(int, int)>();
                    stack.Push((rhoIndex, thetaIndex));
                    visited[rhoIndex, thetaIndex] = true;

                    bool isPeak = true;
                    List<(int, int)> peakRegion = new List<(int, int)>();

                    while (stack.Count > 0)
                    {
                        var (currentRow, currentCol) = stack.Pop();
                        peakRegion.Add((currentRow, currentCol));

                        for (int i = 0; i < 8; i++)
                        //for (int rhoOffset = -minRhoDifference; rhoOffset <= minRhoDifference; rhoOffset++)
                        //{
                        //    for (int thetaOffset = -minThetaDifference; thetaOffset <= minThetaDifference; thetaOffset++)
                        {
                            //if (rhoOffset == 0 && thetaOffset == 0) continue;
                            int newRow = currentRow + dRho[i];
                            int newCol = currentCol + dTheta[i];

                            //bounds checking
                            if (newRow < 0) continue;
                            if (newCol < 0) continue;
                            if (newRow >= maxRho) continue;
                            if (newCol >= maxTheta)
                            {
                                if (newRow > 0)
                                    newRow = maxRho - newRow;
                                newCol = newCol - maxTheta;
                            }
                            if (newRow == 228 && newCol == 0)
                            { }

                            if (accumulator[newRow, newCol].Count > accumulator[currentRow, currentCol].Count)
                            {
                                isPeak = false;
                            }
                            else if (accumulator[newRow, newCol].Count == accumulator[currentRow, currentCol].Count && !visited[newRow, newCol])
                            {
                                stack.Push((newRow, newCol));
                                visited[newRow, newCol] = true;
                            }

                        }
                        if (isPeak)
                        {
                            //TODO, find center of regiion
                            if (peakRegion.Count > 1)
                            { }
                            localMaxima.Add(new Tuple<int, int, int, float>((int)votes, peakRegion[0].Item1, peakRegion[0].Item2, votesLine));

                        }
                    }
                    //    for (int rhoOffset = -minRhoDifference; rhoOffset <= minRhoDifference; rhoOffset++)
                    //    {
                    //        for (int thetaOffset = -minThetaDifference; thetaOffset <= minThetaDifference; thetaOffset++)
                    //        {
                    //            int testRho = rhoIndex + rhoOffset;
                    //            int testTheta = thetaIndex + thetaOffset;
                    //            //bounds checks
                    //            if (rhoOffset == 0 && thetaOffset == 0) continue;
                    //            if (testRho < 0) continue;
                    //            if (testRho >= maxRho) continue;
                    //            if (testTheta < 0) continue;
                    //            if (testTheta >= maxTheta)
                    //            {
                    //                testTheta = testTheta - maxTheta;
                    //                testRho = maxRho - testRho;
                    //            }

                    //            //is this a better candidate than the main one?
                    //            float votes1 = accumulator[testRho, testTheta].Count;
                    //            float votes1Line = LineWeight(testRho, testTheta);
                    //            if (votes1 > votes)// || votes1Line > votesLine)
                    //            {
                    //                goto NotAMax;
                    //            }
                    //            else
                    //            {
                    //                var existingEntry = localMaxima.FindFirst(x => x.Item2 == testRho && x.Item3 == testTheta);
                    //                if (existingEntry != null)
                    //                {
                    //                    localMaxima.Remove(existingEntry);
                    //                }
                    //            }
                    //        }
                    //    }
                    //    localMaxima.Add(new Tuple<int, int, int, float>((int)votes, rhoIndex, thetaIndex, votesLine));
                    //NotAMax: continue;
                }
            }
            localMaxima = localMaxima.OrderByDescending(x => x.Item1).ToList();
        }
        int ContiguousPixels(List<Point> points)
        {
            if (points.Count == 0) return 0;
            int max = 0;
            Point startPt = points[0];
            Point end = points[0];
            Point prev = points[0];
            int minimumSegmentLength = minLength;
            int maximumGapSize = 3;
            int startOfPts = 0;
            int endOfPts = 0;


            for (int i = 1; i < points.Count; i++)
            {
                Point current = points[i];
                float dist = (float)DistanceBetweenPoints(prev, current);
                if (dist < maximumGapSize)
                { //points are contignous
                    prev = current;
                    end = current;
                    endOfPts = i;
                }
                else
                {
                    //pts are discontiguous 
                    startPt = current;
                    end = current;
                    prev = current;
                    if (endOfPts - startOfPts > max) max = endOfPts - startOfPts;
                    startOfPts = i;
                }
            }
            if (endOfPts - startOfPts > max) max = endOfPts - startOfPts;

            return max + 1;
        }
*/

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

        public float LineWeight(int rhoIndex, int thetaIndex)
        {
            if (accumulator[rhoIndex, thetaIndex].Count == 0) return 0;
            OrderPointsAlongSegment(ref accumulator[rhoIndex, thetaIndex]);

            Segment s = new Segment()
            {
                P1 = accumulator[rhoIndex, thetaIndex].First(),
                P2 = accumulator[rhoIndex, thetaIndex].Last(),
            };
            return SegmentWeight(s);
        }
        //how many boundary pixels does a segment "center" on.
        public float SegmentWeight(Segment s)
        {
            float retVal = 0;
            PointPlus curPos = s.P1;
            float dx = s.P2.X - s.P1.X;
            float dy = s.P2.Y - s.P1.Y;
            return s.Length;
            //bounds checking
            if (s.P1.X < 1 || s.P1.Y < 1 || s.P2.X < 1 || s.P2.Y < 1 ||
                s.P1.X > boundaries.GetLength(0) - 2 ||
                s.P1.Y > boundaries.GetLength(1) - 2 ||
                s.P2.X > boundaries.GetLength(0) - 2 ||
                s.P2.Y > boundaries.GetLength(1) - 2) return 0;

            int missCount = 0;
            if (Abs(dx) > Abs(dy))
            {
                //step out in the X direction
                PointPlus step = new PointPlus((dx > 0) ? 1 : -1, dy / Abs(dx));
                for (int x = 0; x <= Abs(dx); x++)
                {
                    //if curPos is exactly on a boundary point OR curPos,
                    //OR curPos is between two boundary points, Add 1
                    //Otherwise, add 1-distance away from the nearest boundary point
                    if (curPos.Y == Round(curPos.Y) && boundaries[(int)curPos.X, (int)curPos.Y] == 1)
                        retVal += 1;
                    else if (curPos.Y > Round(curPos.Y) &&
                        boundaries[(int)curPos.X, (int)curPos.Y] > 0 &&
                        boundaries[(int)curPos.X, (int)curPos.Y + 1] > 0)
                        retVal += 1;
                    else if (curPos.Y < Round(curPos.Y) &&
                        boundaries[(int)curPos.X, (int)curPos.Y] > 0 &&
                        boundaries[(int)curPos.X, (int)curPos.Y - 1] > 0)
                        retVal += 1;
                    else if (boundaries[(int)curPos.X, (int)Floor(curPos.Y)] > 0)
                        retVal += 1 - (float)Abs(curPos.Y - Floor(curPos.Y));
                    else if (boundaries[(int)curPos.X, (int)Ceiling(curPos.Y)] > 0)
                        retVal += 1 - (float)Abs(curPos.Y - Ceiling(curPos.Y));
                    else
                        missCount++;
                    curPos += step;
                }
            }
            else
            {
                //step out in the Y direction
                PointPlus step = new PointPlus(dx / Abs(dy), (dy > 0) ? 1f : -1f);
                for (int y = 0; y <= Abs(dy); y++)
                {
                    if (curPos.X == Round(curPos.X) && boundaries[(int)curPos.X, (int)curPos.Y] == 1)
                        retVal += 1;
                    else if (curPos.X > Round(curPos.X) &&
                        boundaries[(int)curPos.X, (int)curPos.Y] > 0 &&
                        boundaries[(int)curPos.X + 1, (int)curPos.Y] > 0)
                        retVal += 1;
                    else if (curPos.X < Round(curPos.X) &&
                        boundaries[(int)curPos.X, (int)curPos.Y] > 0 &&
                        boundaries[(int)curPos.X - 1, (int)curPos.Y] > 0)
                        retVal += 1;
                    else if (boundaries[(int)Round(curPos.X), (int)Floor(curPos.Y)] > 0)
                        retVal += 1 - (float)Abs(curPos.X - Floor(curPos.X));
                    else if (boundaries[(int)Round(curPos.X), (int)Ceiling(curPos.Y)] > 0)
                        retVal += 1 - (float)Abs(curPos.X - Ceiling(curPos.X));
                    else
                        missCount++;
                    curPos += step;
                }
            }
            //if (missCount > 4) retVal = 0;
            return retVal;
        }

        void AddNearbyPointsToLines()
        {
            foreach (var max in localMaxima)
            {
                AddNearbyPointsToLine2(max.Item2, max.Item3);
            }
        }
        void AddNearbyPointsToLine2(int rho, int theta)
        {
            Point p1, p2;
            float rho1 = rho - maxDistance;

            if (theta == 0)
            {
                p1 = new(rho1, 0); p2 = new(rho1, 100);
            }
            else
            {
                double fTheta = theta * Math.PI / numAngles;
                double b = rho1 / Math.Sin(fTheta);
                double m = -Math.Cos(fTheta) / Math.Sin(fTheta);
                p1 = new(0, b); p2 = new(100, 100 * m + b);
            }

            // Calculate points on the line in the Cartesian coordinate system
            //Point p1 = new Point(rho * cosTheta, rho * sinTheta);
            //Point p2 = new Point(-rho * sinTheta,rho * cosTheta);
            foreach (var pt in boundaryPoints)
            {
                float dist = Utils.DistancePointToLine(pt, p1, p2);
                if (dist < .95)
                {
                    if (!accumulator[rho, theta].Contains(pt)) accumulator[rho, theta].Add(pt);
                }
            }
            OrderPointsAlongSegment(ref accumulator[rho, theta]);
        }

        void AddNearbyPointsToLine(int rho, int theta)
        {
            PointPlus start;
            PointPlus step;

            if (theta == 0 || theta == 180) //line is vertical
            {
                step = new PointPlus(0, 1f);
                start = new PointPlus(rho - maxDistance, 0f);
            }
            else
            {
                //calculate (m,b) for y=mx+b
                Angle fTheta = theta * Math.PI / numAngles;
                double b = (rho - maxDistance) / Math.Sin(fTheta);
                double m = -Math.Cos(fTheta) / Math.Sin(fTheta);
                start = new PointPlus(0, (float)b);
                if (Abs(m) < 1)
                    step = new PointPlus(1, (float)m);
                else
                    step = new PointPlus((float)(1 / Abs(m)), (float)Sign(m));

            }
            accumulator[rho, theta].Clear();
            while ((step.X != 0 && start.X < boundaries.GetLength(0)) || (step.X == 0 && start.Y < boundaries.GetLength(1)))
            {
                if (start.X < 0 || start.X >= boundaries.GetLength(0) ||
                       start.Y < 0 || start.Y >= boundaries.GetLength(1))
                { }
                else
                {
                    ShowArray(start);
                    if (PointNearABoundaryPoint(start, theta, out PointPlus thePoint) > 0)
                    {
                        //if (!accumulator[rho, theta].Contains(thePoint))
                        //    accumulator[rho, theta].Add(thePoint);
                        accumulator[rho, theta].Add(new PointPlus((int)start.X, (float)(int)start.Y));
                    }
                }
                start = start + step;
            }
            OrderPointsAlongSegment(ref accumulator[rho, theta]);
        }
        private void ShowArray(PointPlus start)
        {
            return;
            Debug.Write(start.X + "," + start.Y);
            bool allZero = true;
            for (int i = -3; i < 4; i++)
            {
                for (int j = -3; j < 4; j++)
                {
                    int x = i + (int)Round(start.X);
                    int y = j + (int)Round(start.Y);
                    if (x < 0) continue;
                    if (y < 0) continue;
                    if (x > 99) continue;
                    if (y > 99) continue;
                    if (boundaries[x, y] != 0)
                    {
                        allZero = false;
                        break;
                    }
                }
                if (!allZero) break;
            }
            if (allZero) { Debug.WriteLine("----"); return; }
            Debug.WriteLine("");
            for (int j = -3; j < 4; j++)
            {
                for (int i = -3; i < 4; i++)
                {
                    int x = i + (int)Round(start.X);
                    int y = j + (int)Round(start.Y);
                    if (x < 0) continue;
                    if (y < 0) continue;
                    if (x > 99) continue;
                    if (y > 99) continue;
                    if (i == 0 && j == 0)
                        Debug.Write("X");
                    else
                    {
                        if (boundaries[x, y] == 1)
                            Debug.Write("1");
                        else
                            Debug.Write("-");
                    }
                }
                Debug.WriteLine("");
            }
        }


        //determine if a given point is near a boundary
        float PointNearABoundaryPoint(PointPlus start, int theta, out PointPlus thePoint)
        {
            thePoint = new PointPlus(0, 0f);
            float GetValue(PointPlus pt, int dx, int dy, ref PointPlus thePoint)
            {
                thePoint = new PointPlus((int)Round(start.X) + dx, (float)(int)Round(start.Y) + dy);
                if (thePoint.X < 0 || thePoint.X >= boundaries.GetLength(0) || thePoint.Y < 0 || thePoint.Y >= boundaries.GetLength(1)) return 0;
                return boundaries[(int)thePoint.X, (int)thePoint.Y];
            }
            float val = GetValue(start, 0, 0, ref thePoint);
            if (val > 0) return val;
            if (theta > 158)
            {
                val = GetValue(start, 1, 0, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, -1, 0, ref thePoint);
                if (val > 0) return val;
            }
            else if (theta > 112)
            {
                val = GetValue(start, 1, -1, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, -1, 1, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, 1, 0, ref thePoint) +
                        GetValue(start, 0, -1, ref thePoint);
                if (val > 1) return val;
                val = GetValue(start, -1, 0, ref thePoint) +
                        GetValue(start, 0, 1, ref thePoint);
                if (val > 1) return val;
            }
            else if (theta > 68)
            {
                val = GetValue(start, 0, 1, ref thePoint);
                if (val > 0)
                    return val;
                val = GetValue(start, 0, -1, ref thePoint);
                if (val > 0)
                    return val;
            }
            else if (theta > 22)
            {
                val = GetValue(start, 1, 1, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, -1, -1, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, -1, 0, ref thePoint) +
                        GetValue(start, 0, -1, ref thePoint);
                if (val > 1) return val;
                val = GetValue(start, 1, 0, ref thePoint) +
                        GetValue(start, 0, 1, ref thePoint);
                if (val > 1) return val;
            }
            else
            {
                val = GetValue(start, 1, 0, ref thePoint);
                if (val > 0) return val;
                val = GetValue(start, -1, 0, ref thePoint);
                if (val > 0) return val;
            }
            return 0;
        }


        // Extract line segments from accumulator array
        public List<Segment> ExtractLineSegments()
        {
            localMaxima = localMaxima.OrderByDescending(x => x.Item1).ToList();
            AddNearbyPointsToLines();
            List<Segment> segments = new List<Segment>();
            foreach (var max in localMaxima)
            {
                List<Point> points = accumulator[max.Item2, max.Item3];
                int votes = points.Count;

                if (votes < minLength) continue;
                //the final endpoints are significantly further than point run...must be multiple segments
                Point start = points[0];
                Point end = points[0];
                Point prev = points[0];
                int minimumSegmentLength = minLength;
                int maximumGapSize = 3;

                for (int i = 1; i < points.Count; i++)
                {
                    Point current = points[i];
                    float dist = (float)DistanceBetweenPoints(prev, current);
                    if (dist < maximumGapSize)// && boundaries[(int)Round(current.X), (int)Round(current.Y)] == 1)
                    { //points are contignous
                        prev = current;
                        end = current;
                    }
                    else
                    {
                        //pts are discontiguous 
                        if (DistanceBetweenPoints(start, end) >= minimumSegmentLength)
                            AddSegment(start, end, segments);
                        start = current;
                        end = current;
                        prev = current;
                    }
                }
                if (DistanceBetweenPoints(start, end) > minimumSegmentLength)
                    AddSegment(start, end, segments);
            }

            return segments;
        }
        void AddSegment(Point p1, Point p2, List<Segment> segments)
        {
            Segment newSegment = new Segment(p1, p2);
            //is a similar segment already in the list?
            for (int i = 0; i < segments.Count; i++)
            {
                Segment s = segments[i];
                Angle angleBetweenSegments = Math.Abs(s.Angle - newSegment.Angle);
                //segments nearly match
                if (Utils.FindDistanceToSegment(p1, s) < minLength && Utils.FindDistanceToSegment(p2, s) < 4)
                {
                    float w = SegmentWeight(s);
                    float newW = SegmentWeight(newSegment);
                    if (newW > w && Abs(newSegment.Length - s.Length) < 5)
                        segments[i] = newSegment;
                    return;
                }
            }
            segments.Add(newSegment);
        }
        static double DistanceBetweenPoints(Point point1, Point point2)
        {
            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
