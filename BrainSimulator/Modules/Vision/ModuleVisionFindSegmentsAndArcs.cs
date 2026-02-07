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
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using static System.Math;
using System.Threading;


namespace BrainSimulator.Modules;

//public class HoughTransform
public partial class ModuleVision
{
    //This is currently used for STROKES while BOUNDARIES are handled by FindSegments (below)
    public void FindArcsAndSegments(List<PointPlus> strokePts)
    {
        corners = new();
        segments = new List<Segment>();
        List<PointPlus> availablePts = [.. strokePts];

        //find all the contiguous sets of points in the boundary array
        List<List<PointPlus>> curves = new();
        while (availablePts.Count > 0)
        {
            List<PointPlus> contigPts = new();
            contigPts.Add(availablePts[0]);
            availablePts.RemoveAt(0);
            //find the contiguout points
            for (int curPt = 0; curPt < contigPts.Count; curPt++)
            {
                PointPlus testPt = contigPts[curPt];
                var nearbyPts = GetNearbyPointsSorted(testPt, 2.5f, availablePts);
                foreach (var ptInSeq in nearbyPts)
                {
                    if (contigPts.Contains(ptInSeq.pt)) continue;
                    contigPts.Add(ptInSeq.pt);
                    availablePts.Remove(ptInSeq.pt);
                }
            }
            //order the continuous points
            List<PointPlus> orderedPts = new();
            PointPlus currentPoint = contigPts.First();
            orderedPts.Add(currentPoint);
            contigPts.Remove(currentPoint);
            while (contigPts.Count > 0)
            {
                var nearest = contigPts.OrderBy(x => (currentPoint - x).R).First();
                float dist = (nearest - currentPoint).R;
                if (dist > 3)
                {
                    curves.Add(orderedPts);
                    orderedPts = new();
                }
                orderedPts.Add(nearest);
                currentPoint = nearest;
                contigPts.Remove(nearest);
            }
            curves.Add(orderedPts);
        }


        //find arc  and straight segments in the curves
        while (curves.Count > 0)
        {
            MergeCurvePointLists(curves);
            List<PointPlus> theCurve = curves[0];
            if (theCurve.Count < 4) //give up on short segments
            {
                curves.RemoveAt(0);
                continue;
            }

            //use sliding window to find straight or curved segments
            //start with the largest window and make it smaller until you find a recognizeable subset
            //length has to be at least 4 to know if it's a curve..the two innter angles must be the same sign
            for (int length = theCurve.Count; length > 3; length--)
            {
                //now slide the window through the curve
                for (int start = 0; start <= theCurve.Count - length; start++)
                {
                    List<PointPlus> windowPoints = theCurve.GetRange(start, length);
                    //is this is not a contigouous set of points, continue
                    bool contiguous = true;
                    for (int i = 0; i < windowPoints.Count - 1; i++)
                        if ((windowPoints[i] - windowPoints[i + 1]).R > 3.5)
                        { contiguous = false; break; }
                    if (!contiguous)
                        continue;

                    //is the set of points a defineable curve or straight line
                    bool isCurve = false;
                    bool isStraight = false;

                    List<Angle> theAngles = new List<Angle>();
                    List<Angle> theDeltas = new List<Angle>();
                    for (int i = 0; i < windowPoints.Count - 1; i++)
                    {
                        Angle dTheta = (windowPoints[i] - windowPoints[i + 1]).Theta;
                        //if (dTheta < 0) dTheta += Angle.FromDegrees(360);
                        theAngles.Add(dTheta);
                        if (theAngles.Count > 1)
                        {
                            Angle ddTheta = theAngles.Last() - theAngles[theAngles.Count - 2];
                            if (ddTheta > Angle.FromDegrees(180))
                                ddTheta = Angle.FromDegrees(360) - ddTheta;
                            if (ddTheta < Angle.FromDegrees(-180))
                                ddTheta = Angle.FromDegrees(-360) - ddTheta;
                            theDeltas.Add(ddTheta);
                            if (Abs(ddTheta) > Angle.FromDegrees(180))
                            { }
                        }
                    }

                    isCurve = IsCurved(theDeltas, Angle.FromDegrees(35));

                    if (!isCurve)
                        isStraight = IsLine(windowPoints, 0.5f);

                    if (isStraight || isCurve)
                    {
                        if (isCurve)
                        {
                            //add the arc to the segment list
                            //put the corner of the arc at the midpoint of the arc
                            //which is either the center pt if the count is odd
                            //OR the midpoint pf the two center points if the count is even
                            PointPlus p1;
                            if ((length % 2) != 0)
                                p1 = new PointPlus(windowPoints[length / 2]);
                            else
                                p1 = new PointPlus(new Segment(windowPoints[length / 2 - 1], windowPoints[length / 2]).MidPoint);
                            Arc newCorner = new()
                            {
                                curve = true,
                                pt = p1,
                                prevPt = theCurve[start],
                                nextPt = theCurve[start + length - 1]
                            };
                            corners.Add(newCorner);
                        }
                        else if (isStraight)
                        {
                            //add the segment to the segment list
                            PointPlus p1 = theCurve[start];
                            PointPlus p2 = theCurve[start + length - 1];
                            segments.Add(new Segment(p1, p2));
                        }
                        //remove the segment/arc from the curve
                        //duplicate the last point so it can be included in the next arc/segment
                        List<PointPlus> newCurve = theCurve.GetRange(start + length - 1, theCurve.Count - (start + length - 1));
                        curves[0] = curves[0].GetRange(0, start + 1);
                        curves.Add(newCurve);
                        goto startAgain;
                    }
                }
            }
            //no curve or straight segments found
            curves.RemoveAt(0);
        startAgain: continue;
        }
        JoinUpArcsAndSegments();
        return;
    }

    private void JoinUpArcsAndSegments()
    {
        //some arcs are discovered as short segments and some are broken into multiple arcs
        //this method intends to merge these
        //return;
        float threshold = 2;
        //do pairs of segments form a (possibly) curved angle
        for (int i = 0; i < segments.Count; i++)
        {
            //pick a segment
            Segment sg1 = segments[i];
            if (sg1.Length > 6) continue;
            //does this segment abut a curve?
            for (int j = 0; j < corners.Count; j++)
            {
                if (corners[j] is Arc a)
                {
                    if ((sg1.P1 - a.prevPt).R < threshold &&
                        Abs(sg1.Angle - (a.prevPt - a.pt).Theta) < Angle.FromDegrees(40))
                    {
                        corners.Add(new Arc { pt = sg1.P1, prevPt = sg1.P2, nextPt = a.nextPt, });
                        corners.RemoveAt(j);
                        goto segmentMergedToArc;
                    }
                    else if ((sg1.P1 - a.nextPt).R < threshold &&
                        Abs(sg1.Angle - (a.nextPt - a.pt).Theta) < Angle.FromDegrees(40))
                    {
                        corners.Add(new Arc { pt = sg1.P1, prevPt = sg1.P2, nextPt = a.prevPt, });
                        corners.RemoveAt(j);
                        goto segmentMergedToArc;
                    }
                    else if ((sg1.P2 - a.prevPt).R < threshold &&
                        Abs(sg1.Angle - (a.prevPt - a.pt).Theta) < Angle.FromDegrees(40))
                    {
                        corners.Add(new Arc { pt = sg1.P2, prevPt = sg1.P1, nextPt = a.nextPt, });
                        corners.RemoveAt(j);
                        goto segmentMergedToArc;
                    }
                    else if ((sg1.P2 - a.nextPt).R < threshold &&
                        Abs(sg1.Angle - (a.nextPt - a.pt).Theta) < Angle.FromDegrees(40))
                    {
                        corners.Add(new Arc { pt = sg1.P2, prevPt = sg1.P1, nextPt = a.prevPt, });
                        corners.RemoveAt(j);
                        goto segmentMergedToArc;
                    }
                }
            }
            //does this abut another segment forming a curve?
            for (int j = i + 1; j < segments.Count; j++)
            {
                Segment sg2 = segments[j];
                if (sg2.Length > 6) continue;
                if (Abs(sg1.Angle - sg2.Angle) > Angle.FromDegrees(60))
                    continue;
                //do they nearly meet up?
                if ((sg1.P2 - sg2.P1).R < threshold)
                {
                    corners.Add(new Arc
                    {
                        pt = sg1.P2,
                        prevPt = sg1.P1,
                        nextPt = sg2.P2,
                    });
                    goto twoSegmentsFormArc;
                }
                if ((sg1.P1 - sg2.P1).R < threshold)
                {
                    corners.Add(new Arc
                    {
                        pt = sg1.P1,
                        prevPt = sg1.P2,
                        nextPt = sg2.P2,
                    });
                    goto twoSegmentsFormArc;
                }
                if ((sg1.P1 - sg2.P2).R < threshold)
                {
                    corners.Add(new Arc
                    {
                        pt = sg1.P1,
                        prevPt = sg1.P2,
                        nextPt = sg2.P1,
                    });
                    goto twoSegmentsFormArc;
                }
                if ((sg1.P2 - sg2.P2).R < threshold)
                {
                    corners.Add(new Arc
                    {
                        pt = sg1.P2,
                        prevPt = sg1.P1,
                        nextPt = sg2.P1,
                    });
                    goto twoSegmentsFormArc;
                }
                continue;
            twoSegmentsFormArc:
                segments.RemoveAt(j);
                segments.RemoveAt(i);
                j--;
                if (i > 0)
                    i--;
            }
            continue;
        segmentMergedToArc:
            segments.RemoveAt(i);
            if (i > 0)
                i--;
        }

        for (int i = 0; i < corners.Count - 1; i++)
        {
            for (int j = i + 1; j < corners.Count; j++)
            {
                //do they have the same curvature?
                Arc arc1 = (Arc)corners[i];
                Arc arc2 = (Arc)corners[j];
                var cir1 = arc1.GetCircleFromThreePoints(arc1.pt, arc1.nextPt, arc1.prevPt);
                var cir2 = arc2.GetCircleFromThreePoints(arc2.pt, arc2.nextPt, arc2.prevPt);
                if ((cir1.center - cir2.center).R > 3) continue;
                if (Abs(cir1.radius - cir2.radius) > 3) continue;
                //do they nearly meet up?
                PointPlus s1 = arc1.prevPt;
                PointPlus e1 = arc1.nextPt;
                PointPlus s2 = arc2.prevPt;
                PointPlus e2 = arc2.nextPt;
                if ((e1 - s2).R < threshold)
                {
                    Arc newCorner = new Arc()
                    {
                        prevPt = s1,
                        nextPt = e2,
                        pt = e1,
                    };
                    corners[i] = newCorner;
                    corners.RemoveAt(j);
                    j--;
                }
            }
        }
    }
    private void MergeCurvePointLists(List<List<PointPlus>> curves)
    {
        float threshold = 2.5f;
        for (int i = 0; i < curves.Count - 1; i++)
        {
            if (curves[i].Count == 0) continue;
            for (int j = i + 1; j < curves.Count; j++)
            {
                if (curves[j].Count == 0) continue;
                PointPlus s1 = curves[i].First();
                PointPlus e1 = curves[i].Last();
                PointPlus s2 = curves[j].First();
                PointPlus e2 = curves[j].Last();
                if ((e1 - s2).R < threshold)
                {
                    curves[i].InsertRange(curves[i].Count, curves[j]);
                    curves.RemoveAt(j);
                    j--;
                }
                else if ((s1 - e2).R < threshold)
                {
                    curves[i].InsertRange(0, curves[j]);
                    curves.RemoveAt(j);
                    j--;
                }
                else if ((s1 - s2).R < threshold)
                {
                    curves[i].Reverse();
                    curves[i].InsertRange(curves[i].Count, curves[j]);
                    curves.RemoveAt(j);
                    j--;
                }
                else if ((e1 - e2).R < threshold)
                {
                    curves[j].Reverse();
                    curves[i].InsertRange(curves[i].Count, curves[j]);
                    curves.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    private static bool IsCurved(List<Angle> dAngles, Angle threshold)
    {
        bool mayBeCurveLeft = true;
        bool mayBeCurveRight = true;
        for (int i = 0; i < dAngles.Count; i++)
        {
            Angle a = dAngles[i];
            if (!mayBeCurveLeft && !mayBeCurveRight) break;
            if (mayBeCurveRight && (a < Angle.FromDegrees(1) || a > threshold))
                mayBeCurveRight = false;
            if (mayBeCurveLeft && (a > Angle.FromDegrees(-1) || a < -threshold))
                mayBeCurveLeft = false;
        }
        return mayBeCurveLeft || mayBeCurveRight;
    }
    static bool IsLine(List<PointPlus> points, double threshold)
    {
        float threshold1 = (points.First() - points.Last()).R / 3f;
        PointPlus pFirst = points[0];
        PointPlus pLast = points.Last();
        int count0 = 0;
        int count1 = 0;
        int count2 = 0;
        for (int i = 1; i < points.Count - 1; i++)
        {
            PointPlus curPt = points[i];
            float dist = Utils.DistancePointToLine(curPt, pFirst, pLast);
            if (dist > threshold)
                return false;

            //which side of the line is this point on?
            float d = (pLast.X - pFirst.X) * (curPt.Y - pFirst.Y) - (pLast.Y - pFirst.Y) * (curPt.X - pFirst.X);
            if (d > threshold1)
                count0++;
            else if (d < -threshold1)
                count1++;
            else
                count2++;
        }
        if (count0 == 0 ^ count1 == 0) //XOR operator, are all the points on one side? then more likely a curve
            return false;
        return true;
    }



    public List<Segment> FindSegments(List<PointPlus> boundaries)
    {
        boundaryPoints = boundaries;
        boundaryPoints = boundaryPoints.OrderBy(pt => pt.X + pt.Y * 1000).ToList();
        List<Segment> segments = new List<Segment>();

        List<PointPlus> availablePointList = new List<PointPlus>();
        //copy the list of boundarypoints to a temp list
        foreach (var pt in boundaryPoints)
            availablePointList.Add(pt);

        //Angle minAngle = Angle.FromDegrees(-90);
        List<Angle> anglesAlreadyFound = new List<Angle>();

        //pick a boundary point from the temp list.
        while (availablePointList.Count > 0)
        {
            //find the best segment through/near that point
            PointPlus ptToTest = availablePointList[0];
            //anglesAlreadyFound = new();
            Segment s1 = BestSegmentThroughPoint(ptToTest, availablePointList, anglesAlreadyFound);

            //if there is no segment at least 3 pixels long, remove the test point from the temp list, continue
            if (s1 is null || s1.Length < 2 || !AddSegmentToList(segments, s1))
            {
                availablePointList.RemoveAt(0);
                anglesAlreadyFound = new();
                continue;
            }

            //a segment has been added

            //what points are actually on the segment
            //delete them from the available point list
            for (int i = 0; i < availablePointList.Count; i++)
            {
                PointPlus pt = availablePointList[i];

                //don't delete the endpoints as there are likely to be additional lines at the corners
                if (pt.Near(s1.P1, 0.2f) || pt.Near(s1.P2, 0.2f))
                    continue;

                float dist = Utils.DistancePointToSegment(s1, pt);
                if (Abs(dist) < .5)
                {
                    availablePointList.RemoveAt(i);
                    i--;
                }
            }
            //loop until the available point list is empty
        }
        return segments;
    }

    Angle angleStep = Angle.FromDegrees(5);
    private Segment BestSegmentThroughPoint(PointPlus pt, List<PointPlus> availablePoints, List<Angle> alreadyFound)
    {

        angleStep = Angle.FromDegrees(1);
        Segment bestSegment = new(pt, pt);
        Angle bestAngle = -1;

        //find the amgle which results in the longed segment through the given point 
        for (Angle a = Angle.FromDegrees(-90); a < Angle.FromDegrees(90); a += angleStep)
        {
            //don't search the same angle at the same point over and over
            if (alreadyFound.FindFirst(x => Abs(x - a) < Angle.FromDegrees(0.9f)) != null)
                continue;

            //at this angle, extend the segment in either direction until you run out of nearby boundary points
            PointPlus step = new((float)Cos(a), (float)Sin(a));
            PointPlus p1 = new(pt);

            float maxDist = Abs(step.X) + Abs(step.Y) + .1f;
            //maxDist = .8f;
            maxDist = .7f;
            //lengthen the first endpoint (if possible)
            float dist = -1;
            do
            {
                p1 += step;
                PointPlus nearest = GetNearestPointInList(p1, availablePoints);
                dist = (p1 - nearest).R;
            } while (dist < maxDist);
            p1 -= step;

            //now the second endpoint
            PointPlus p2 = new(pt);
            dist = -1;
            do
            {
                p2 -= step;
                PointPlus nearest = GetNearestPointInList(p2, availablePoints);
                dist = (p2 - nearest).R;
            } while (dist < maxDist);
            p2 += step;

            //is this new segment better than others we've found at other angles
            Segment newSegment = new(p1, p2);
            if (newSegment.Length > bestSegment.Length)
            {
                bestSegment = newSegment;
                bestAngle = a;
            }
        }

        if (bestSegment.Length >= 2)
            alreadyFound.Add(bestAngle);
        //        if (bestSegment.Length < 3)
        if (bestSegment.Length < 2)
            return bestSegment;

        //Here we have a ressonable segment hypothesis. The endpoints might not be exactly on boundary points
        //try out all combinations of nearby endpoints to see if there is an improvement
        //if you don't find any segment with actual endpoints

        bool improved = true;
        Segment best = null;
        var candidatePts = GetNearbyPoints(bestSegment.P1, 3f, boundaryPoints);
        var candidatePts1 = GetNearbyPoints(bestSegment.P2, 3f, boundaryPoints);

        //best is the best within the loop below, not to be confused with bestSegment from the loop above
        bestSegment = null;
        while (improved)// && bestSegment?.Length > 2)
        {
            improved = false;
            if (best is not null)
            {
                candidatePts = GetNearbyPoints(best.P1, 2f, boundaryPoints);
                candidatePts1 = GetNearbyPoints(best.P2, 2f, boundaryPoints);
            }

            //build a table of all possible candidate segments and their hit-rates
            var tempValues = new List<(int i, int j, float length, float error)>();
            for (int i = 0; i < candidatePts.Count; i++)
            {
                PointPlus pt1 = candidatePts[i];
                for (int j = 0; j < candidatePts1.Count; j++)
                {
                    PointPlus pt2 = candidatePts1[j];
                    if (pt1.Near(pt2, 2f)) continue;
                    Segment testSeg = new Segment(pt1, pt2);
                    float e1 = GetAverageError3(testSeg);
                    tempValues.Add((i, j, testSeg.Length, e1));
                }
            }

            tempValues = tempValues.OrderBy(x => x.error).ToList();

            if (tempValues.Count == 0 || tempValues[0].error > .2f) return null;

            float maxValue = .1f;
            var limitValues = tempValues.FindAll(x => x.error < maxValue).ToList();
            while (limitValues.Count == 0)
            {
                maxValue += .1f;
                limitValues = tempValues.FindAll(x => x.error < maxValue).ToList();
            }
            var t = limitValues.FindFirst(x => x.length == limitValues.Max(x => x.length));
            best = new Segment(candidatePts[t.i], candidatePts1[t.j]);

            if (best != bestSegment && Abs(best.Angle - bestAngle) < Angle.FromDegrees(15))
            {
                bestSegment = best;
                improved = true;
            }
        }

        //has the algorithm strayed too far from the original point?
        if (bestSegment is not null && Utils.DistancePointToSegment(bestSegment, pt) > 1.5f)
        {
            //likely a curve
            //return null;
        }

        return bestSegment;
    }


    //add a new segment to the segment list IF it isn't within 1 of another segment
    private bool AddSegmentToList(List<Segment> segs, Segment seg)
    {
        if (segs.FindFirst(x => (x.P1.Near(seg.P1, 1) && x.P2.Near(seg.P2, 1)) || (x.P1.Near(seg.P2, 1) && x.P2.Near(seg.P1, 1))) is null)
        {
            seg.debugIndex = segs.Count;
            if (segs.Count == 10)
            { }
            segs.Add(seg);
            return true;
        }
        else
            return false;
    }
    private PointPlus GetNearestPointInList(PointPlus pt, List<PointPlus> points)
    {
        PointPlus nearest = new(-10, -10f);
        foreach (var pt1 in points)
        {
            float dist = (pt - pt1).R;
            if (dist < (pt - nearest).R)
            {
                nearest = pt1;
            }
        }
        return nearest;
    }
    private PointPlus GetNearestBoundaryPoint(PointPlus pt)
    {
        return GetNearestPointInList(pt, boundaryPoints);
    }

    bool PointInSegmentRange(Segment s, PointPlus pt)
    {
        float dy = Abs(s.P1.Y - s.P2.Y);
        float dx = Abs(s.P1.X - s.P2.X);
        if (dx > dy)
        {
            if (pt.X - s.P1.X > 0.001f && pt.X - s.P2.X > 0.001f) return false;
            if (pt.X - s.P1.X < -0.001f && pt.X - s.P2.X < -0.001f) return false;
        }
        else
        {
            if (pt.Y - s.P1.Y > 0.001f && pt.Y - s.P2.Y > 0.001f) return false;
            if (pt.Y - s.P1.Y < -0.001f && pt.Y - s.P2.Y < -0.001f) return false;
        }
        return true;
    }
    float GetAverageError3(Segment s)
    {
        List<PointPlus> linePoints = new();
        float sum = 0;
        int count = 0;
        foreach (var pt in boundaryPoints)
        {
            if (!PointInSegmentRange(s, pt)) continue;
            float dist = Utils.DistancePointToSegment(s, pt);
            if (dist < 1f)
            {
                linePoints.Add(pt);
                sum += dist;
                count++;
            }
        }


        //return large numbers on illegal conditions
        //if (count < s.Length-1) average = 3;

        //count the number of gaps points in the segment
        float dx = s.P1.X - s.P2.X;
        float dy = s.P1.Y - s.P2.Y;
        int stepCount;
        if (Abs(dx) > Abs(dy))
        {
            stepCount = (int)Round(dx);
            dy /= dx;
            dx = 1;
        }
        else
        {
            stepCount = (int)Round(dy);
            dx /= dy;
            dy = 1;
        }
        PointPlus step = new(dx, dy);
        PointPlus currPt = s.P2;
        int misses = 0;
        for (int i = 0; i < stepCount; i++)
        {
            float dist = (GetNearestPointInList(currPt, linePoints) - currPt).R;
            if (dist > .5f)
                misses++;
            currPt += step;
        }

        if (s.Length > count)
            //if (s.Length <  10)
            sum += (s.Length - count) / 2; //dock 0.5 point for each missing point on segment
        float average = sum / count;
        if (misses > 1)
            average = 3;
        if (average < 0f)
        { }
        return average;
    }

    List<PointPlus> GetBoundaryPointsOnSegment(Segment seg)
    {
        List<PointPlus> retVal = new();
        foreach (var pt in boundaryPoints)
        {
            if (!PointInSegmentRange(seg, pt)) continue;
            float dist = Utils.DistancePointToSegment(seg, pt);
            if (dist < .5f)
                retVal.Add(pt);
        }
        OrderPointsAlongSegment(ref retVal);
        return retVal;
    }

    float GetSegmentWeight3(Segment s)
    {
        float retVal = 0;
        float sum = 0;
        int count = 0;
        foreach (var pt in boundaryPoints)
        {
            float dist = Utils.DistancePointToSegment(s, pt);
            if (dist < 1)
            {
                sum += dist;
                count++;
                retVal += 1 - dist;
            }
        }
        float average = sum / count;
        return retVal;
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
    private List<(PointPlus pt, float dist, Angle theta)> GetNearbyPointsSorted(PointPlus pt, float dist, List<PointPlus> availablePoints)
    {
        //this is exhaustive and can be replaced with a QuadTree for performance
        List<(PointPlus pt, float dist, Angle theta)> points = new();
        foreach (PointPlus point in availablePoints)
        {
            PointPlus p1 = point - pt;

            float d1 = p1.R;
            Angle a = p1.Theta;
            if (d1 <= dist)
                points.Add(new(point, d1, a));
        }
        points = points.OrderBy(x => x.theta * 1000 + x.dist).ToList();
        return points;
    }
    /*
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


    */

    public static void MergeSegments(List<Segment> segments)
    {
        for (int i = 0; i < segments.Count - 1; i++)
            for (int j = i + 1; j < segments.Count; j++)
            {
                if (Merge2Segments(segments[i], segments[j], out Segment sNew))
                {
                    segments[i] = sNew;
                    segments.RemoveAt(j);
                }
            }
    }

    //returns true if one of the segments is unnecessary and the first should be replaced by sNew
    static bool Merge2Segments(Segment s1, Segment s2, out Segment sNew)
    {
        sNew = s1;

        if (Utils.FindIntersection(s1, s2, out PointPlus intersection, out Angle a))
        {

        }

        //are the segments nearly the same angle?
        float angleDiff = Abs((s1.Angle - s2.Angle).Degrees);
        float minLength = (float)Min(s2.Length, s1.Length);
        float angleLimit = 15;
        if (minLength < 4) angleLimit = 20;

        if (angleDiff > angleLimit &&
            angleDiff < 180 - angleLimit ||
            angleDiff > 180 + angleLimit &&
            angleDiff < 360 - angleLimit) return false;
        //are the segments near each other?
        float d1 = Utils.DistanceBetweenTwoSegments(s1, s2);
        if (d1 > 5) return false;

        d1 = Utils.DistancePointToSegment(s1, s2.P1);
        float d2 = Utils.DistancePointToSegment(s1, s2.P2);
        float d3 = Utils.DistancePointToSegment(s2, s1.P1);
        float d4 = Utils.DistancePointToSegment(s2, s1.P2);

        //do the segments overlap?
        if (d1 < 2 || d2 < 2 || d3 < 2 || d4 < 2)
        {
            //do the segments extend one another?
            //mix and match endpoints go get segment with highest Weight
            PointPlus[] endPts = { new(s1.P1), new(s1.P2), new(s2.P1), new(s2.P2) };
            float maxDist = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    if (endPts[i] == endPts[j]) continue;
                    float dist = (new Segment(endPts[i], endPts[j])).Length;
                    if (dist > maxDist)
                    {
                        sNew.P1 = endPts[i];
                        sNew.P2 = endPts[j];
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
    void OrderPointsAlongSegment(ref List<PointPlus> points)
    {
        float bestDist = 0;
        if (points.Count == 0) return;
        var p1 = points[0];
        var p2 = points[0];
        foreach (var pa in points)
            foreach (var pb in points)
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

    private Segment BestSegmentThroughPoint2(PointPlus pt, List<PointPlus> availablePoints)
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
                candidatePts = GetNearbyPoints(seg.P1, 1.5f, availablePoints);
                candidatePts.AddRange(GetNearbyPoints(seg.P2, 1.5f, availablePoints));
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
}
