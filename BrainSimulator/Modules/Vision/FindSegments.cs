using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using static System.Math;

namespace BrainSimulator.Modules.Vision
{

    public class HoughTransform
    {
        // Hough Transform parameters
        private int numAngles = 180; // Number of angles to consider (e.g., 180 degrees)
        private double angleStep; // Angle step size
        private double rhoStep = 1; // Rho step size (can be adjusted based on image resolution)
        public int maxDistance; // Maximum possible distance from the origin to the image corner
        private List<Point>[,] accumulator; // Accumulator array
        public List<Tuple<int, int, int>> localMaxima;
        private float[,] boundaries;

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
                            double rho = x * Cos(theta) + y * Sin(theta);
                            int rhoIndex = (int)Round(rho / rhoStep + maxDistance); 

                            accumulator[rhoIndex, thetaIndex].Add(new Point(x, y));
                        }
                    }
                }
            }
            FindMaxima(boundaries);
        }

        private void FindMaxima(float[,] edges)
        {
            //find the top vote-getters
            localMaxima = new List<Tuple<int, int, int>>();

            for (int rhoIndex = 0; rhoIndex < accumulator.GetLength(0); rhoIndex++)
                for (int thetaIndex = 0; thetaIndex < accumulator.GetLength(1); thetaIndex++)
                {
                    int votes = accumulator[rhoIndex, thetaIndex].Count;
                    var points = accumulator[rhoIndex, thetaIndex]; //for debug only
                    if (votes < 5) continue;

                    bool isLocalMaximum = true;
                    int distToSearch = 3;
                    for (int x = -distToSearch; x <= distToSearch; x++)
                    {
                        for (int y = -distToSearch; y <= distToSearch; y++)
                        {
                            //bounds checking
                            if (x == 0 && y == 0) continue;
                            if (x + rhoIndex < 0 || y + thetaIndex < 0) continue;
                            if (x + rhoIndex >= accumulator.GetLength(0) || y + thetaIndex >= accumulator.GetLength(1)) continue;
                            //are these setgments nearly colinear but extend one-another?
                            List<Point> points1 = accumulator[rhoIndex + x, thetaIndex + y];
                            if (points1.Count < 5) continue;
                            if (DistanceBetweenPoints(points.First(), points1.First()) > 5) continue;
                            if (DistanceBetweenPoints(points.Last(), points1.Last()) > 5) continue;

                            int votes1 = points1.Count;
                            if (votes1 > votes)
                            {
                                float w = SegmentWeight(new Segment(points.First(), points.Last()));
                                float w1 = SegmentWeight(new Segment(points1.First(), points1.Last()));
                                if (w < w1)
                                {
                                    isLocalMaximum = false;
                                    break;
                                }
                            }
                        }
                        if (!isLocalMaximum) break;
                    }
                    if (isLocalMaximum)
                        localMaxima.Add(new Tuple<int, int, int>(votes, rhoIndex, thetaIndex));
                }
            localMaxima = localMaxima.OrderByDescending(x => x.Item1).ToList();
        }

        //how many boundary pixels does a segment "center" on.
        public float SegmentWeight(Segment s)
        {
            float retVal = 0;
            PointPlus curPos = s.P1;
            float dx = s.P2.X - s.P1.X;
            float dy = s.P2.Y - s.P1.Y;
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
                for (int x = 0; x < Abs(dx); x++)
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
                    else if (boundaries[(int)curPos.X, (int)curPos.Y] == 1)
                        retVal += 1 - (float)Abs(curPos.Y - Round(curPos.Y));
                    else 
                        missCount++;
                    curPos += step;
                }
            }
            else
            {
                //step out in the Y direction
                PointPlus step = new PointPlus(dx / Abs(dy), (dy > 0) ? 1f : -1f);
                for (int y = 0; y < Abs(dy); y++)
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
                    else if (boundaries[(int)Round(curPos.X), (int)Round(curPos.Y)] == 1)
                        retVal += 1 - (float)Abs(curPos.X - Round(curPos.X));
                    else
                        missCount++;
                    curPos += step;
                }
            }
            if (missCount > 3) retVal = 0;
            return retVal;
        }


        // Extract line segments from accumulator array
        public List<Segment> ExtractLineSegments()
        {
            List<Segment> segments = new List<Segment>();
            foreach (var max in localMaxima)
            {
                List<Point> points = accumulator[max.Item2, max.Item3];
                int votes = points.Count;
                if (votes > 4)
                {
                    Point p1 = points.First();
                    Point p2 = points.Last();
                    if (p1.Y > p2.Y)
                    {
                        points = points.OrderByDescending(x => x.Y).ToList();
                    }
                    else
                        points = points.OrderBy(x => x.Y).ToList();
                    p1 = points.First();
                    p2 = points.Last();

                    //the final endpoints are significantly further than point run...must be multiple segments
                    Point start = points[0];
                    Point end = points[0];
                    Point prev = points[0];
                    int minimumSegmentLength = 3;
                    int maximumGapSize = 3;

                    for (int i = 1; i < points.Count; i++)
                    {
                        Point current = points[i];
                        float dist = (float)DistanceBetweenPoints(prev, current);
                        if (dist < maximumGapSize && boundaries[(int)current.X,(int)current.Y] == 1)
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
                if (Utils.FindDistanceToSegment(p1, s) < 4 && Utils.FindDistanceToSegment(p2, s) < 4)
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
