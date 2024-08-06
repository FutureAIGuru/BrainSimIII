
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using static System.Math;
using System.Linq;


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
    private List<PointPlus> FindStrokePtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        //find stroke centers in Ray
        if (sy == 8)
        { }
        List<PointPlus> ptsInThisScan = new();
        int startStroke = 0;
        int endStroke = 0;
        for (int i = 1; i < rayThruImage.Count - 1; i++)
        {
            Color colorVal = rayThruImage[i];
            if (rayThruImage[i] == backgroundColor) continue;
            //here is the start of a (possible) stroke
            startStroke = i;
            //now find the end of the stroke
            while (i < rayThruImage.Count && (rayThruImage[i] != backgroundColor))
            {
                endStroke = i;
                i++;
            }

            //possible bifurcations...two strokes without an intervening background
            //for (int j = startStroke; j <= endStroke - 2; j++)
            //{
            //    byte colorVal0 = rayThruImage[j].B;
            //    byte colorVal1 = rayThruImage[j + 1].B;
            //    byte colorVal2 = rayThruImage[j + 2].B;
            //    if (colorVal0 > colorVal1 && colorVal2 > colorVal1)
            //    {
            //        endStroke = j;
            //        i = j + 1;
            //    }
            //}

            //find the brightest point in the stroke
            float brightest = 0;
            for (int j = startStroke; j <= endStroke; j++)
            {
                float colorVal3 =  new HSLColor( rayThruImage[j]).luminance;
                if (colorVal3 > brightest) { brightest = colorVal3; }
            }

            //find the stroke position by calculating the weighted average of the brightnesses in the range
            float boundaryPos = (startStroke + endStroke) / 2f;
            float numerator = 0;
            float denominator = 0;
            for (int j = startStroke; j <= endStroke; j++)
            {
                float colorVal3 = new HSLColor( rayThruImage[j]).luminance;
                numerator += j * colorVal3;
                denominator += colorVal3;
            }
            if (endStroke - startStroke > 5) continue; //stroke is too wide to be a stroke
            if (brightest < 0.1f) continue; //stroke is too dim to be a stroke

            boundaryPos = numerator / denominator;
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

        return ptsInThisScan;


    }

    private void FindBoundaryPtsInRay(float sx, float sy, float dx, float dy, List<Color> rayThruImage)
    {
        List<PointPlus> ptsInThisScan = new();

        List<float> arr = new();
        foreach (var v in rayThruImage)
            arr.Add(new HSLColor(v).luminance);

        List<int> minima = new();
        List<int> maxima = new();

        int direction = 0; //-1 for decreasing, 1 for increasing, 0 for flat
        float previous = arr[0];
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

        for (int i = 1; i < arr.Count - 1; i++)
        {
            float delta = arr[i] - previous;
            if (Abs(delta) < .1f) continue;

            if (arr[i] > previous)
            {
                if (direction == -1) // local minima
                {
                    if (i > 1 && arr[i - 2] < arr[i - 1])
                        minima.Add(i - 2);
                    else
                        minima.Add(i - 1);
                }
                direction = 1;
            }
            else if (arr[i] < previous)
            {
                if (direction == 1) // local maxima
                {
                    if (i > 1 && arr[i - 2] > arr[i - 1])
                        maxima.Add(i - 2);
                    else
                        maxima.Add(i - 1);
                }
                direction = -1;
            }
            else 
                direction = 0;
            previous = arr[i];
        }

        if (direction == 1) maxima.Add(arr.Count - 1);
        if (direction == -1) minima.Add(arr.Count - 1);

        int curMin = 0;
        int curMax = 0;
        while (curMin < minima.Count && curMax < maxima.Count)
        {
            float boundaryPos = -1;
            if (minima[curMin] < maxima[curMax])
            {
                boundaryPos = FindMidValuePt(arr, minima[curMin], maxima[curMax]);
                curMin++;
            }
            else
            {
                boundaryPos = FindMidValuePt(arr, maxima[curMax], minima[curMin]);
                curMax++;
            }
            //no boundary found 
            if (boundaryPos < 0 || boundaryPos >= rayThruImage.Count) continue;
            boundaryPos = (float) Round(boundaryPos,1);

            if (dx == 1 && dy == 0)
                ptsInThisScan.Add(new Point(boundaryPos, sy));
            else if (dx == 0 && dy == 1)
                ptsInThisScan.Add(new Point(sx, boundaryPos));
            else if (dx >= 0 && dy >= 0)
                ptsInThisScan.Add(new Point(boundaryPos + sx, boundaryPos + sy));
            else
                ptsInThisScan.Add(new Point(sx - boundaryPos, boundaryPos + sy));
        }

/*        //merge nearby boundaries
        for (int i = 0; i < ptsInThisScan.Count-1; i++)
        {
            PointPlus pt = ptsInThisScan[i];
            PointPlus pt1 = ptsInThisScan[i+1];
            if ((pt-pt1).R <= 2)
            {
                int x1 = (int)Round(pt.X);
                int y1 = (int)Round(pt.Y);
                if (dx == 1 && x1 > 0 && x1 < imageArray.GetLength(0) - 3)
                {
                    if (imageArray[x1 - 1, y1] == backgroundColor && imageArray[x1 + 2, y1] == backgroundColor)
                    {
                        pt.X = (pt.X + pt1.X) / 2;
                        pt.Y = (pt.Y + pt1.Y) / 2;
                        ptsInThisScan.RemoveAt(i + 1);
                        i--;
                    }
                }
                if (dy == 1 && y1 > 0 && y1 < imageArray.GetLength(0) - 3)
                {
                    if (imageArray[x1, y1-1] == backgroundColor && imageArray[x1, y1+2] == backgroundColor)
                    {
                        pt.X = (pt.X + pt1.X) / 2;
                        pt.Y = (pt.Y + pt1.Y) / 2;
                        ptsInThisScan.RemoveAt(i + 1);
                        i--;
                    }
                }
            }
        }
*/
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

