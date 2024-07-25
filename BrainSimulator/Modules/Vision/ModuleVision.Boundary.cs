
using System.Collections.Generic;
using System.Windows.Media;
using System;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;
using static System.Math;


namespace BrainSimulator.Modules;

public partial class ModuleVision
{
    private void FindBoundaries(Color[,] imageArray)
    {
        strokePoints.Clear();
        boundaryPoints.Clear();

        bool horizScan = true;
        bool vertScan = true;
        bool fortyFiveScan = true;
        bool minusFortyFiveScan = true;

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
        if (fortyFiveScan)
        {
            dx = 1;
            dy = 1;
            List<PointPlus> ptsInThisScan = new();
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, 0, sy, imageArray);
                var pts = FindStrokePtsInRay(0, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                FindBoundaryPtsInRay(0, sy, dx, dy, rayThruImage);
            }
            for (sx = 0; sx < imageArray.GetLength(0); sx++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, 0, imageArray);
                var pts = FindStrokePtsInRay(sx, 0, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                FindBoundaryPtsInRay(sx, 0, dx, dy, rayThruImage);
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
                FindBoundaryPtsInRay(sx, 0, dx, dy, rayThruImage);
            }
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, imageArray.GetLength(0) - 1, sy, imageArray);
                var pts = FindStrokePtsInRay(imageArray.GetLength(0) - 1, sy, dx, dy, rayThruImage);
                ptsInThisScan.AddRange(pts);
                FindBoundaryPtsInRay(imageArray.GetLength(0) - 1, sy, dx, dy, rayThruImage);
            }
            RemoveOrphanPoints(ptsInThisScan);
            strokePoints.AddRange(ptsInThisScan);
        }
    }

    private void RemoveOrphanPoints(List<PointPlus> points)
    {
        //Remove orpha points which can be caused by curved edges
        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                PointPlus pt1 = points[i];
                PointPlus pt2 = points[j];
                if ((pt1 - pt2).R < 2)
                    goto neighborFound;
            }
            points.RemoveAt(i);
            i--;
        neighborFound: continue;
        }
    }
    private List<PointPlus> FindStrokePtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        //find stroke centers in Ray
        if (sy == 50)
        { }
        List<PointPlus> ptsInThisScan = new();
        int startStroke = 0;
        int endStroke = 0;
        for (int i = 0; i < rayThruImage.Count - 1; i++)
        {
            Color colorVal = rayThruImage[i];
            if (rayThruImage[i].B == 0 || colorVal == Colors.White) continue;
            startStroke = i;
            while (i < rayThruImage.Count && (rayThruImage[i].B != 0 && rayThruImage[i] != Colors.White))
            {
                endStroke = i;
                i++;
            }

            //TODO: handle double peaks as two stroke points
            for (int j = startStroke; j <= endStroke - 2; j++)
            {
                byte colorVal0 = rayThruImage[j].B;
                byte colorVal1 = rayThruImage[j + 1].B;
                byte colorVal2 = rayThruImage[j + 2].B;
                if (colorVal0 > colorVal1 && colorVal2 > colorVal1)
                {
                    endStroke = j;
                    i = j + 1;
                }
            }

            int brightest = 0;
            for (int j = startStroke; j <= endStroke; j++)
            {
                byte colorVal3 = rayThruImage[j].B;
                if (colorVal3 > brightest) { brightest = colorVal3; }
            }

            float boundaryPos = (startStroke + endStroke) / 2f;
            float numerator = 0;
            float denominator = 0;
            //find the actual brightest point in this range
            for (int j = startStroke; j <= endStroke; j++)
            {
                byte colorVal3 = rayThruImage[j].B;
                numerator += j * colorVal3;
                denominator += rayThruImage[j].B;
            }
            if (endStroke - startStroke > 5) continue;
            if (brightest < 0x0b0) continue;

            boundaryPos = numerator / denominator;
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


        //given a ray of color values through an image, find the boundaries
        //todo: filter out noisy areas in the ray
        float boundaryThreshold = 100;
        //float boundaryThreshold = 0.01f;

        int start = -1;
        for (int i = 0; i < rayThruImage.Count - 1; i++)
        {
            float boundaryPos = -1;
            float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);

            if (Abs(diff) < boundaryThreshold)
            {
                //pixels are the same...could be the start of a boundary
                start = i;
            }
            else if (start != -1 && Abs(diff) > boundaryThreshold)
            {
                //find the end of the boundary and enter it
                for (int j = i + 1; j < rayThruImage.Count - 1; j++)
                {
                    float diffEnd = PixelDifference(rayThruImage[j], rayThruImage[j + 1]);
                    if (Abs(diffEnd) < boundaryThreshold)
                    {
                        boundaryPos = (start + j + 1) / 2.0f;
                        if (j == start + 3)
                        {
                            //this will offset the boundary point based on the intensity of the intervening point
                            HSLColor c0 = new(rayThruImage[start]);
                            HSLColor c1 = new(rayThruImage[start + 1]);
                            HSLColor c2 = new(rayThruImage[start + 2]);
                            HSLColor c3 = new(rayThruImage[j]);
                            float offset = (c1.saturation - c3.saturation) * c2.saturation;
                            if (sy == 35)
                            { }
                            //boundaryPos += offset/4;
                        }
                        i = j;
                        start = i;
                        break;
                    }
                }
            }
            if (boundaryPos != -1)
            {
                if (sx == -1)
                    strokePoints.Add(new Point(boundaryPos, sy));
                else
                    strokePoints.Add(new Point(sx, boundaryPos));
            }
        }
    }

    private void FindBoundaryPtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        //given a ray of color values through an image, find the boundaries
        //todo: filter out noisy areas in the ray
        float boundaryThreshold = 100;

        int start = -1;
        for (int i = 0; i < rayThruImage.Count - 1; i++)
        {
            float boundaryPos = -1;
            float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);

            if (Abs(diff) < boundaryThreshold)
            {
                //pixels are the same...could be the start of a boundary
                start = i;
            }
            else if (start != -1 && Abs(diff) > boundaryThreshold)
            {
                //find the end of the boundary and enter it
                for (int j = i + 1; j < rayThruImage.Count - 1; j++)
                {
                    float diffEnd = PixelDifference(rayThruImage[j], rayThruImage[j + 1]);
                    if (Abs(diffEnd) < boundaryThreshold)
                    {
                        boundaryPos = (start + j + 1) / 2.0f;
                        if (j == start + 3)
                        {
                            //this will offset the boundary point based on the intensity of the intervening point
                            HSLColor c0 = new(rayThruImage[start]);
                            HSLColor c1 = new(rayThruImage[start + 1]);
                            HSLColor c2 = new(rayThruImage[start + 2]);
                            HSLColor c3 = new(rayThruImage[j]);
                            float offset = (c1.saturation - c3.saturation) * c2.saturation;
                            //boundaryPos += offset/4;
                        }
                        i = j;
                        start = i;
                        break;
                    }
                }
            }
            //boundaryPos = (start + end) / 2f;
            float numerator = 0;
            float denominator = 0;
            //find the actual brightest point in this range
            //for (int j = start; j <= end; j++)
            //{
            //    byte colorVal3 = rayThruImage[j].B;
            //    numerator += j * colorVal3;
            //    denominator += rayThruImage[j].B;
            //}

            //boundaryPos = numerator / denominator;
            if (boundaryPos == -1) continue;
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

