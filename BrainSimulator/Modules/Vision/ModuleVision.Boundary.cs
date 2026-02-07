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
using System.Windows.Media;
using System.Windows;
using static System.Math;
using System.Linq;
using System;


namespace BrainSimulator.Modules;
public partial class ModuleVision
{
    Color backgroundColor = Colors.Black;
    private void FindBackgroundColor()
    {
        Dictionary<Color, int> colorCount = new Dictionary<Color, int>();

        int width = imageArray.GetLength(0);
        int height = imageArray.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = imageArray[x, y];
                if (colorCount.ContainsKey(color))
                {
                    colorCount[color]++;
                }
                else
                {
                    colorCount[color] = 1;
                }
            }
        }
        backgroundColor = colorCount.OrderByDescending(x => x.Value).First().Key;
    }

    public bool horizScan = true;
    public bool vertScan = true;
    public bool fortyFiveScan = true;
    public bool minusFortyFiveScan = true;
    private void FindBoundaries(Color[,] imageArray)
    {
        strokePoints.Clear();
        boundaryPoints.Clear();


        float dx = 1;
        float dy = 0;
        int sx = 0;
        int sy = 0;
        if (horizScan)
        {
            List<PointPlus> ptsInThisScan = new();
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                var pts = FindStrokePtsInRay(sx, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                FindBoundaryPtsInRay(sx, sy, dx, dy, rayThruImage);
            }
            RemoveOrphanPoints(ptsInThisScan);
            strokePoints.AddRange(ptsInThisScan);
        }
        if (vertScan)
        {
            dx = 0;
            dy = 1;
            sy = 0;
            List<PointPlus> ptsInThisScan = new();
            for (sx = 0; sx < imageArray.GetLength(0); sx++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                var pts = FindStrokePtsInRay(sx, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                FindBoundaryPtsInRay(sx, sy, dx, dy, rayThruImage);
            }
            RemoveOrphanPoints(ptsInThisScan);
            strokePoints.AddRange(ptsInThisScan);
        }
        MergeNearbyPoints(strokePoints);
/*        if (fortyFiveScan)
        {
            dx = 1;
            dy = 1;
            List<PointPlus> ptsInThisScan = new();
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, 0, sy, imageArray);
                var pts = FindStrokePtsInRay(0, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                //FindBoundaryPtsInRay(0, sy, dx, dy, rayThruImage);
            }
            for (sx = 0; sx < imageArray.GetLength(0); sx++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, 0, imageArray);
                var pts = FindStrokePtsInRay(sx, 0, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                //FindBoundaryPtsInRay(sx, 0, dx, dy, rayThruImage);
            }
            RemoveOrphanPoints(ptsInThisScan);
            strokePoints.AddRange(ptsInThisScan);
        }
        if (minusFortyFiveScan)
        {
            dx = -1;
            dy = 1;
            List<PointPlus> ptsInThisScan = new();
            for (sx = 0; sx < imageArray.GetLength(0); sx++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, 0, imageArray);
                var pts = FindStrokePtsInRay(sx, 0, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                //FindBoundaryPtsInRay(sx, 0, dx, dy, rayThruImage);
            }
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, imageArray.GetLength(0) - 1, sy, imageArray);
                var pts = FindStrokePtsInRay(imageArray.GetLength(0) - 1, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                //FindBoundaryPtsInRay(imageArray.GetLength(0) - 1, sy, dx, dy, rayThruImage);
            }
            RemoveOrphanPoints(ptsInThisScan);
            strokePoints.AddRange(ptsInThisScan);
        }
  */
    }

    private void MergeNearbyPoints (List<PointPlus>pts)
    {
        List<(int p1, int p2, float val)> distances = new();
        for (int i = 0; i < pts.Count; i++)
        {
            for (int j = i+1; j < pts.Count; j++)
            {
                if (i == j) continue;
                distances.Add(new (i,j,(pts[i] - pts[j]).R));
            }
        }
        distances = distances.OrderBy(v=>v.val).ToList();
        List<int> ptsToDelete = new();
        for (int i = 0;i < distances.Count; i++)
        {
            if (distances[i].val >= 1) break;
            pts[distances[i].p1].X = (pts[distances[i].p1].X + pts[distances[i].p2].X) / 2;
            pts[distances[i].p1].Y = (pts[distances[i].p1].Y + pts[distances[i].p2].Y) / 2;
            ptsToDelete.Add(distances[i].p2);
        }
        ptsToDelete = ptsToDelete.OrderByDescending(x=>x).Distinct().ToList();
        foreach (int i in ptsToDelete)
        {
            pts.RemoveAt(i);
        }
    }

    private void RemoveOrphanPoints(List<PointPlus> points)
    {
        //Remove orphan points which can be caused by curved edges
        for (int i = 0; i < points.Count; i++)
        {
            PointPlus pt1 = points[i];
            float dist = 0;
            for (int j = 0; j < points.Count; j++)
            {
                if (j == i) continue;
                PointPlus pt2 = points[j];
                dist = (pt1 - pt2).R;
                if (dist < 2)
                    goto neighborFound;
            }
            points.RemoveAt(i);
            i = (i == 0) ? i - 1 : i - 2;
        neighborFound: continue;
        }
    }

    List<PointPlus> FindStrokeeCentersFromBoundaryPoints(List<PointPlus> points)
    {
        List<PointPlus> strokeCenters = new List<PointPlus>();
        foreach (PointPlus pt in points)
        {
            List<PointPlus> nearbyPts = GetNearbyPoints(pt, 5f, points);
            foreach (PointPlus pt2 in nearbyPts)
            {
                if ((pt - pt2).R < 2f) continue;
                PointPlus possibleStrokeCenter = (new Segment(pt2, pt).MidPoint);
                List<PointPlus> nearbyPts2 = GetNearbyPoints(possibleStrokeCenter, 1f, points);
                if (nearbyPts2.Count == 0 && GetLuminanceAtPoint(possibleStrokeCenter) > .8)
                {
                    List<PointPlus> nearbyPts3 = GetNearbyPoints(possibleStrokeCenter, .7f, strokeCenters);
                    if (nearbyPts3.Count == 0)
                        strokeCenters.Add(possibleStrokeCenter);
                    else
                    {
                        //PointPlus averagePt = new PointPlus(nearbyPts3.Average(x=>x.X),nearbyPts3.Average(x=>x.Y) );
                    }

                }
            }
        }
        return strokeCenters;
    }
    float GetLuminanceAtPoint(PointPlus pt)
    {
        var c = imageArray[(int)Round(pt.X), (int)Round(pt.Y)];
        HSLColor c1 = new(c);
        return c1.luminance;
    }
    private List<PointPlus> FindStrokePtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        if (sy == 10)
        { }
        List<float> luminance = new List<float>();
        foreach (var color in rayThruImage)
        {
            float lum = new HSLColor(color).luminance;
            luminance.Add(lum );
        }
        List<PointPlus> ptsInThisScan = new();
        (List<(int index, float value)> maxima, List<(int index, float value)> minima) v = FindLocalExtrema(luminance, 0.06f);
        for (int i = 0; i < v.Item1.Count; i++)
        {
            (int index, float value) item = v.maxima[i];
            float minBrightness = 0.35f;  //max brightnesee for a "black" pixel
            float minBrightness1 = 0.7f;  //min brightness for white pixel
            int maxStrokeWidth = 6;

            if (item.Item2 < minBrightness1) continue;
            //find start and end of stroke
            int startStroke = item.Item1;
            int minPos = 0;
            if (i > 0)
                minPos = v.Item2[i - 1].Item1;
            while (startStroke > minPos && luminance[startStroke] > minBrightness)
                startStroke--;
            int endStroke = item.Item1;
            int maxPos = luminance.Count - 1;
            if (i < v.Item2.Count)
                maxPos = v.Item2[i].Item1;
            while (endStroke < maxPos && luminance[endStroke] > minBrightness)
                endStroke++;

            if (endStroke - startStroke > maxStrokeWidth)
                continue; //stroke is too wide to be a stroke

            //find the position based on weighted average
            float boundaryPos = (startStroke + endStroke) / 2f; //for debug
            float numerator = 0;
            float denominator = 0;
            for (int j = startStroke+1; j < endStroke; j++)
            {
                numerator += j * luminance[j];
                denominator += luminance[j];
            }

            boundaryPos = numerator / denominator;
            //boundaryPos = (float)Round(boundaryPos, 1);
            if (dx == 1 && dy == 0)
                ptsInThisScan.Add(new Point(boundaryPos, sy));
            else if (dx == 0 && dy == 1)
                ptsInThisScan.Add(new Point(sx, boundaryPos));
            else if (dx >= 0 && dy >= 0)
                ptsInThisScan.Add(new Point(boundaryPos + sx, boundaryPos + sy));
            else
                ptsInThisScan.Add(new Point(sx - boundaryPos, boundaryPos + sy));
        }

        return ptsInThisScan;
    }

    public static (List<(int index, float value)>, List<(int index, float value)>) FindLocalExtrema(List<float> data, float minDifference)
    {
        List<(int index, float value)> maxima = new();
        List<(int index, float value)> minima = new();

        bool? increasing = null; // Null indicates no trend established yet
        float highestValue = 0;
        float lowestValue = 0;
        for (int i = 1; i < data.Count - 1; i++)
        {
            if (data[i] > data[i - 1])
            {
                highestValue = data[i];
                if (Math.Abs(data[i - 1] - data[i]) > minDifference)
                {
                    if (increasing == false)
                    {
                        minima.Add(new(i - 1, lowestValue));
                    }
                    increasing = true;
                }
            }
            else if (data[i] < data[i - 1])
            {
                lowestValue = data[i];
                if (Math.Abs(highestValue - data[i]) > minDifference)
                {
                    if (increasing == true)
                    {
                        maxima.Add(new(i - 1, highestValue));
                        increasing = false;
                    }
                }
            }
        }

        return (maxima, minima);
    }


    private void FindBoundaryPtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        if (sx == 20)
        { }
        List<PointPlus> ptsInThisScan = new();

        List<float> luminances = new();
        foreach (var v in rayThruImage)
            luminances.Add(new HSLColor(v).luminance);

        List<int> minima = new();
        List<int> maxima = new();

        int direction = 0; //-1 for decreasing, 1 for increasing, 0 for flat
        float previous = luminances[0];
        if (previous > 0.5)
        {
            direction = -1;
            maxima.Add(0);
        }
        else
        {
            direction = 1;
            minima.Add(0);
        }

        for (int i = 1; i < luminances.Count - 1; i++)
        {
            float delta = luminances[i] - previous;
            if (Abs(delta) < .1f) continue;

            if (luminances[i] > previous)
            {
                if (direction == -1 && previous < 0.6f) // local minima
                {
                    if (i > 1 && luminances[i - 2] < luminances[i - 1])
                        minima.Add(i - 2);
                    else
                        minima.Add(i - 1);
                }
                direction = 1;
            }
            else if (luminances[i] < previous)
            {
                if (direction == 1 && previous > 0.6f) // local maxima
                {
                    if (i > 1 && luminances[i - 2] > luminances[i - 1])
                        maxima.Add(i - 2);
                    else
                        maxima.Add(i - 1);
                }
                direction = -1;
            }
            else
                direction = 0;
            previous = luminances[i];
        }

        if (direction == 1) maxima.Add(luminances.Count - 1);
        if (direction == -1) minima.Add(luminances.Count - 1);

        int curMin = 0;
        int curMax = 0;
        while (curMin < minima.Count && curMax < maxima.Count)
        {
            float boundaryPos = -1;
            if (minima[curMin] < maxima[curMax])
            {
                boundaryPos = FindMidValuePt(luminances, minima[curMin], maxima[curMax]);
                curMin++;
            }
            else
            {
                boundaryPos = FindMidValuePt(luminances, maxima[curMax], minima[curMin]);
                curMax++;
            }
            //no boundary found 
            if (boundaryPos < 0 || boundaryPos >= rayThruImage.Count) continue;
            boundaryPos = (float)Round(boundaryPos, 1);

            if (dx == 1 && dy == 0)
                ptsInThisScan.Add(new Point(boundaryPos, sy));
            else if (dx == 0 && dy == 1)
                ptsInThisScan.Add(new Point(sx, boundaryPos));
            else if (dx >= 0 && dy >= 0)
                ptsInThisScan.Add(new Point(boundaryPos + sx, boundaryPos + sy));
            else
                ptsInThisScan.Add(new Point(sx - boundaryPos, boundaryPos + sy));
        }
        foreach (var pt in ptsInThisScan)
            boundaryPoints.Add(pt);
    }
    private float FindMidValuePt(List<float> pts, int start, int end)
    {
        //if start and end are far apart, move them closer to eleminate noise values
        float val1 = pts[start];
        float val2 = pts[end];
        if (Abs(val1 - val2) < .14f) return -1;
        if (end - start > 10)
        {
            while (start < pts.Count - 2 && Abs(val1 - pts[start + 1]) < .1) start++;
            while (end > 1 && Abs(val2 - pts[end - 1]) < .1) end--;
        }

        float crossingPoint = -1;
        float targetValue = (pts[start] + pts[end]) / 2f;
        for (int i = start; i < end; i++)
        {
            float currVal = pts[i];
            float nextVal = pts[i + 1];
            if ((currVal <= targetValue && nextVal >= targetValue) ||
                    (currVal >= targetValue && nextVal <= targetValue))
            {
                float t = (targetValue - currVal) / (nextVal - currVal);
                crossingPoint = i + t;
                break;
            }
        }
        return crossingPoint;
    }

    private void FindBoundaryPtsInRay2(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        //given a ray of color values through an image, find the boundaries
        //todo: filter out noisy areas in the ray
        if (sx == 21 || sy == 10)
        { }

        int start = -1;
        for (int i = 0; i < rayThruImage.Count - 1; i++)
        {
            float boundaryPos = -1;
            float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);

            if (diff < 200)
            {
                //pixels are the same...move the start of a boundary
                start = i;
            }
            else
            {
                //find the end of the boundary There are 2 pixels the same 
                for (int j = start + 1; j < rayThruImage.Count - 1; j++)
                {
                    int end = j + 1;
                    float diffEnd = PixelDifference(rayThruImage[j], rayThruImage[end]);
                    if (diffEnd < 200)
                    {
                        //this will offset the boundary point based on the intensity of the intervening point
                        List<HSLColor> colors = new List<HSLColor>();
                        for (int k = start; k <= end; k++)
                            colors.Add(new HSLColor(rayThruImage[k]));

                        List<float> lums = new();
                        for (int k = start; k <= end; k++)
                        {
                            lums.Add((new HSLColor(rayThruImage[k])).luminance);
                        }


                        boundaryPos = (i + j) / 2f;
                        if (colors.Count == 4)
                        { }
                        else if (colors.Count == 5 || colors.Count == 6)
                        {
                            if (colors.Count == 6)
                            {
                                boundaryPos -= 0.25f;
                                colors.RemoveAt(3);
                            }
                            float startingluminance = colors[0].luminance;
                            float endingluminance = colors.Last().luminance;
                            float centerluminance = colors[(int)colors.Count / 2].luminance;
                            float t = 1 - (startingluminance - centerluminance) / (startingluminance - endingluminance);
                            boundaryPos += t - 0.5f;
                        }
                        else boundaryPos = -1;
                        i = j - 1;
                        start = i;
                        break;
                    }
                }
            }
            //boundaryPos = (start + end) / 2f;
            if (boundaryPos < 0 || boundaryPos >= rayThruImage.Count) continue;
            if (dx == 1 && dy == 0)
                boundaryPoints.Add(new Point(boundaryPos, sy));
            else if (dx == 0 && dy == 1)
                boundaryPoints.Add(new Point(sx, boundaryPos));
            else if (dx >= 0 && dy >= 0)
                boundaryPoints.Add(new Point(boundaryPos + sx, boundaryPos + sy));
            else
                boundaryPoints.Add(new Point(sx - boundaryPos, boundaryPos + sy));
        }
    }

    List<Color> LineThroughArray(float dx, float dy, int startX, int startY, Color[,] imageArray)
    {
        List<Color> retVal = new();
        float x = startX; float y = startY;

        while (x >= 0 && y >= 0 && x < imageArray.GetLength(0) && y < imageArray.GetLength(1))
        {
            Color c = imageArray[(int)x, (int)y];
            if (c != null)
                retVal.Add(c);
            x += dx;
            y += dy;
        }
        return retVal;
    }

}

