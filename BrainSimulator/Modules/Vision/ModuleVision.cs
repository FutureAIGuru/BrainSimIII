//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules;

public partial class ModuleVision : ModuleBase
{
    private string currentFilePath = "";
    public string previousFilePath = "";
    public BitmapImage bitmap = null;
    public List<Corner> corners;
    public List<Segment> segments;
    public Color[,] imageArray;
    //public HoughTransform segmentFinder;
    public List<PointPlus> strokePoints = new();
    public List<PointPlus> CenterLinePts = null;
    public List<PointPlus> boundaryPoints = new();
    bool isSingleDigit = false;

    public class Corner
    {
        public PointPlus pt;
        public virtual Angle angle
        {
            get
            {
                Segment s1 = new Segment(prevPt, pt);
                Segment s2 = new Segment(pt, nextPt);
                Angle a = s2.Angle - s1.Angle;
                while (a.Degrees > 180)
                    a = a - Angle.FromDegrees(180);
                while (a.Degrees < -180)
                    a = a + Angle.FromDegrees(180);
                return a;
            }
        }
        public bool curve = false;
        public PointPlus prevPt;
        public PointPlus nextPt;
        public override string ToString()
        {
            if (curve)
                return $"[mid:({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")}) " +//A: {angle}] " +
                $"start:[({prevPt.X.ToString("0.0")},{prevPt.Y.ToString("0.0")})] " +
                $"end:[({nextPt.X.ToString("0.0")},{nextPt.Y.ToString("0.0")})]";
            else
                return $"[x,y:({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")}) " +//A: {angle}] " +
                $"prevPt:[({prevPt.X.ToString("0.0")},{prevPt.Y.ToString("0.0")})] " +
                $"nextPt:[({nextPt.X.ToString("0.0")},{nextPt.Y.ToString("0.0")})]";
        }
    }
    public ModuleVision()
    {
    }

    //fill this method in with code which will execute
    //once for each cycle of the engine
    public override void Fire()
    {
        Init();  //be sure to leave this here

        if (CurrentFilePath == previousFilePath) return;
        previousFilePath = CurrentFilePath;

        LoadImageFileToPixelArray(CurrentFilePath);

        FindBackgroundColor();


        segments = new();
        corners = new();
        FindBoundaries(imageArray);
        CenterLinePts = GetCenterlinePoints(boundaryPoints, strokePoints);

        //FindSegments();
        //FindArcs();

        //strokePoints = FindStrokeeCentersFromBoundaryPoints(boundaryPoints);
        isSingleDigit = false;
        int theDigit = -1;
        if (!int.TryParse(System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(CurrentFilePath)), out theDigit))
            if (!int.TryParse(System.IO.Path.GetFileName(System.IO.Path.GetFileNameWithoutExtension(CurrentFilePath)), out theDigit))
                theDigit = -1;
        if (theDigit >= 0)
        {
            isSingleDigit = true;
            ModuleShape ms = (ModuleShape)MainWindow.theWindow.GetModule("ModuleShape0");
            if (ms != null)
                ms.MNISTDigit = "MNIST" + theDigit;
        }

        if (isSingleDigit)
        {
            //FindArcsAndSegments(strokePoints);
            FindArcsAndSegments(CenterLinePts);
            FindCorners(ref segments);
            SaveOutlinesToUKS();
        }
        else
        {
            segments = FindSegments(boundaryPoints);
            FindCorners(ref segments);
            SaveOutlinesToUKS();
        }

        WriteBitmapToMentalModel();

        UpdateDialog();
    }

    void FindSegments()
    {
        segments = new();
        for (int x = 0; x < imageArray.GetLength(0); x++)
        {
            for (int y = 0; y < imageArray.GetLength(1); y++)
            {
                if (x == 9 && y == 5)
                { }
                Color c = imageArray[x, y];
                HSLColor hslC = new HSLColor(c);
                if (hslC.luminance < .9) continue;
                PointPlus start = new((float)x, (float)y);
                for (Angle a1 = 0; a1 < Angle.FromDegrees(180); a1 += Angle.FromDegrees(10))
                {
                    PointPlus end = new(start);
                    for (int i = 1; i < 20; i++)
                    {
                        PointPlus pos = new((float)(start.X + i * Cos(a1)), (float)(start.Y + i * Sin(a1)));
                        int x1 = (int)Round(pos.X);
                        int y1 = (int)Round(pos.Y);
                        if (x1 < 0 || x1 >= imageArray.GetLength(0)) break;
                        if (y1 < 0 || y1 >= imageArray.GetLength(1)) break;
                        c = imageArray[x1, y1];
                        hslC = new HSLColor(c);
                        if (hslC.luminance < .75) break;
                        end = pos;
                    }
                    IsSegmentCenteredBySum(start, end, boundaryPoints, out double score);
                    if (a1 == 0 && (end - start).R > 4 && end.Y > 20)
                    { }
                    if ((end - start).R > 3 && score > .9)// && a1 == 0)
                        segments.Add(new Segment(start, end));
                }
            }
        }
    }

    //interpolate the luminance in the image array givine a real-valued point
    float GetLuminanceAtPoint(PointPlus pt)
    {
        if (pt.X < 0 || pt.Y < 0) return 0;
        if ((int)pt.X > imageArray.GetLength(0) - 2) return 0;
        if ((int)pt.Y > imageArray.GetLength(1) - 2) return 0;

        int x0 = (int)Math.Floor(pt.X);
        int y0 = (int)Math.Floor(pt.Y);

        float a = GetLuminanceFromColor(imageArray[(int)pt.X, (int)pt.Y]);
        float b = GetLuminanceFromColor(imageArray[(int)pt.X + 1, (int)pt.Y]);
        float c = GetLuminanceFromColor(imageArray[(int)pt.X, (int)pt.Y + 1]);
        float d = GetLuminanceFromColor(imageArray[(int)pt.X + 1, (int)pt.Y + 1]);

        float top = a + (pt.X - x0) * (b - a);
        float bottom = c + (pt.X - x0) * (d - c);

        float result = top + (pt.Y - y0) * (bottom - top);

        return result;

    }
    float GetLuminanceFromColor(Color c)
    {
        HSLColor hSLColor = new(c);
        return hSLColor.luminance;
    }

    bool IsSegmentCenteredBySum(
    PointPlus A, PointPlus B,
    IReadOnlyList<PointPlus> boundary,
    out double balance                 // 0..1; 1 = perfectly balanced
)
    {
        balance = 0;
        if (boundary == null || boundary.Count == 0) return false;

        // Sampling
        double weightL = 0, weightR = 0;

        Segment s = new(A, B);
        foreach (PointPlus pt in boundary)
        {
            float dist = PerpendicularDistancePointToSegment(s, pt);
            if (Abs(dist) > 2) continue;
            if (dist >= 0)
                weightL += dist;
            else
                weightR += Abs(dist);
        }
        double denom = Math.Max(weightL, weightR);
        if (denom == 0) return false;
        balance = denom > 0 ? 1.0 - Math.Abs(weightL - weightR) / denom : 0.0;
        return true;
    }

    //Move this to Utils
    float PerpendicularDistancePointToSegment(Segment ABin, PointPlus pt)
    {
        var AP = pt - ABin.P1;
        var AB = ABin.P2 - ABin.P1;
        float magnituesAB = AB.R * AB.R;
        float ABAProduct = (float)Vector.Multiply(AP.V, AB.V);
        float distance = ABAProduct / magnituesAB;
        if (distance >= 0 && distance <= 1) //does the projections fall along the segment?
        {
            PointPlus closestOnSegment = ABin.P1 + AB * distance;
            int sign = 0;
            if (closestOnSegment.Y - pt.Y > .1)
                sign = 1;
            else if (closestOnSegment.Y - pt.Y < -.1)
                sign = -1;
            else if (closestOnSegment.X > pt.X)
                sign = -1;
            else
                sign = 1;
            return sign * (closestOnSegment - pt).R;
        }
        return 0;
    }

    void FindArcs()
    {
        Arc a = new();
        (PointPlus center, float radius) bestCirc = new();
        int bestNumHits = 0;
        for (int x = 0; x < imageArray.GetLength(0); x++)
        {
            for (int y = 0; y < imageArray.GetLength(1); y++)
            {
                PointPlus center = new((float)x, (float)y);
                for (int r = 4; r < 10; r++)
                {
                    int numHits = 0;
                    for (Angle a1 = 0; a1 < Angle.FromDegrees(360); a1 += Angle.FromDegrees(10))
                    {
                        PointPlus pos = new PointPlus((float)(x + r * Cos(a1)), (float)(y + r * Sin(a1)));
                        int x1 = (int)pos.X;
                        int y1 = (int)pos.Y;
                        if (x1 < 0 || x1 >= imageArray.GetLength(0)) continue;
                        if (y1 < 0 || y1 >= imageArray.GetLength(1)) continue;
                        PointPlus nearby = GetNearestPointInList(pos, CenterLinePts);
                        float dist = (nearby - pos).R;
                        if (dist < 1)
                            numHits += (int)(5 * (1 - dist));
                        Color c = imageArray[x1, y1];
                        HSLColor hslC = new HSLColor(c);
                        if (hslC.luminance > .9)
                            numHits++;
                    }
                    if (numHits > bestNumHits)
                    {
                        bestNumHits = numHits;
                        bestCirc = new(center, (float)r);
                    }
                }
            }
        }
        bool inArc = false;
        Angle startAngle = 0;

        for (Angle a1 = 0; a1 < Angle.FromDegrees(360); a1 += Angle.FromDegrees(10))
        {
            PointPlus pos = new PointPlus((float)(bestCirc.center.X + bestCirc.radius * Cos(a1)),
                (float)(bestCirc.center.Y + bestCirc.radius * Sin(a1)));
            int x1 = (int)pos.X;
            int y1 = (int)pos.Y;
            if (x1 < 0 || x1 >= imageArray.GetLength(0)) continue;
            if (y1 < 0 || y1 >= imageArray.GetLength(1)) continue;
            Color c = imageArray[x1, y1];
            HSLColor hslC = new HSLColor(c);
            if (hslC.luminance > .9 && !inArc)
            {
                inArc = true;
                a.curve = true;
                a.prevPt = pos;
                startAngle = a1;
            }
            if (hslC.luminance < .9 && inArc)
            {
                inArc = false;
                a.curve = true;
                a.nextPt = pos;
                Angle a2 = (startAngle + a1) / 2;
                a.pt = new PointPlus((float)(bestCirc.center.X + bestCirc.radius * Cos(a2)),
                (float)(bestCirc.center.Y + bestCirc.radius * Sin(a2)));
                corners.Add(a);
                a = new();
            }
        }
    }

    //This is different from FindOutlines in several ways
    // 1. It assumes the entire visual field is a single symbol
    // 2. It does not assume the symbole is a closed figure
    // 3. It supports arcs

    public float scale = 1;
    public int offsetX = 0;
    public int offsetY = 0;

    public string CurrentFilePath { get => currentFilePath; set => currentFilePath = value; }

    public void LoadImageFileToPixelArray(string filePath)
    {
        using (System.Drawing.Bitmap bitmap2 = new(CurrentFilePath))
        {
            System.Drawing.Bitmap theBitmap = bitmap2;

            int bitmapSizeX = theBitmap.Width;
            int bitmapSizeY = theBitmap.Height;

            float max = int.Max(bitmapSizeX, bitmapSizeY);
            if (max > 50)
            {
                bitmapSizeX = (int)(bitmapSizeX * 50f / max);
                bitmapSizeY = (int)(bitmapSizeY * 50f / max);
            }

            //do not expand an image if it is smaller than the bitmap...it can introduce problems
            if (theBitmap.Width < bitmapSizeX) scale = (float)theBitmap.Width / bitmapSizeX;
            if (scale > theBitmap.Width / bitmapSizeX) scale = theBitmap.Width / bitmapSizeX;
            //limit the x&y offsets so the picture will be displayed
            float maxOffset = bitmapSizeX * scale - bitmapSizeX;
            if (offsetX > 0) offsetX = 0;
            if (offsetX < -maxOffset) offsetX = -(int)maxOffset;
            if (offsetY > 0) offsetY = 0;
            if (offsetY < -maxOffset) offsetY = -(int)maxOffset;
            System.Drawing.Bitmap resizedImage = new(bitmapSizeX, bitmapSizeY);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                graphics.DrawImage(bitmap2, offsetX, offsetY, bitmapSizeX * scale, bitmapSizeY * scale);
            }

            imageArray = new Color[resizedImage.Width, resizedImage.Height];

            for (int i = 0; i < resizedImage.Width; i++)
                for (int j = 0; j < resizedImage.Height; j++)
                {
                    var c = resizedImage.GetPixel(i, j);
                    imageArray[i, j] = new Color() { A = 0xff, R = c.R, G = c.G, B = c.B };
                }
        }
        dlg.Draw(false);
    }
    private void WriteBitmapToMentalModel()
    {
        Thing mentalModel = theUKS.GetOrAddThing("MentalModel", "Thing");
        Thing mentalModelArray = theUKS.GetOrAddThing("MentalModelArray", "MentalModel");
        mentalModel.SetFired();
        //TODO Make angular
        //TODO Make 0 center
        for (int x = 0; x < 25; x++)
            for (int y = 0; y < 25; y++)
            {
                string name = $"mm{x},{y}";
                Thing theEntry = theUKS.GetOrAddThing(name, mentalModelArray);
                theEntry.V = GetAverageColor(x * 4, y * 4);
            }
    }
    private Color GetAverageColor(int x, int y)
    {
        Color retVal = Color.FromArgb(1, 0, 0, 0);
        int size = 2;
        for (int i = -size; i <= size; i++)
            for (int j = -size; j <= size; j++)
            {
                if (x + i < 0) continue;
                if (y + j < 0) continue;
                if (x + i >= imageArray.GetLength(0)) continue;
                if (y + j >= imageArray.GetLength(1)) continue;
                retVal.R += imageArray[x + i, y + j].R;
                retVal.G += imageArray[x + i, y + j].G;
                retVal.B += imageArray[x + i, y + j].B;
            }
        retVal.R /= 25;
        retVal.R /= 25;
        retVal.R /= 25;
        return retVal;
    }


    private Color[,] GetImageArrayFromBitmapImage()
    {
        int height = (int)bitmap.Height;
        int width = (int)bitmap.Width;
        if (height > bitmap.PixelHeight) height = (int)bitmap.PixelHeight;
        if (width > bitmap.PixelWidth) width = (int)bitmap.PixelWidth;
        imageArray = new Color[width, height];
        int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
        byte[] pixelBuffer = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixelBuffer, stride, 0);
        for (int i = 0; i < imageArray.GetLength(0); i++)
        {
            for (int j = 0; j < imageArray.GetLength(1); j++)
            {
                //upper for jpeg, lower for png
                int index = j * stride + i * 4; // Assuming 32 bits per pixel (4 bytes: BGRA)
                if (bitmap.Format.BitsPerPixel == 8)
                    index = j * stride * 3 + i * 3;
                if (index >= pixelBuffer.Length) continue;

                if (bitmap.Format.BitsPerPixel != 8 && index < pixelBuffer.Length - 3)
                {
                    byte blue = pixelBuffer[index];
                    byte green = pixelBuffer[index + 1];
                    byte red = pixelBuffer[index + 2];
                    byte alpha = pixelBuffer[index + 3];
                    Color pixelColor = Color.FromArgb(1, red, green, blue);
                    imageArray[i, j] = pixelColor;
                }
                else
                {
                    byte red, green, blue, alpha;
                    if (bitmap.Palette != null)
                    {
                        var c = bitmap.Palette.Colors[pixelBuffer[index]];
                        blue = c.B;
                        red = c.R;
                        green = c.G;
                    }
                    else
                    {
                        blue = pixelBuffer[index];
                        if (bitmap.Format.BitsPerPixel > 8)
                        {
                            green = pixelBuffer[index + 1];
                            red = pixelBuffer[index + 2];
                            alpha = pixelBuffer[index + 3];
                        }
                        else
                        {
                            red = blue;
                            green = blue;

                        }
                    }
                    Color pixelColor = Color.FromArgb(1, red, green, blue);
                    imageArray[i, j] = pixelColor;

                }
            }
        }

        return imageArray;
    }


    void ExtendSegments()
    {
        foreach (var segment in segments)
        {
            //extend the segment as far as possible as long as there are white pixels
            Angle a = segment.Angle;
            //at this angle, extend the segment in either direction until you run out of nearby boundary points
            PointPlus step = new((float)Cos(a), (float)Sin(a));

            int maxExtend = 5;

            //lengthen the first endpoint to another boundary point unless it is already attached to another corner or arc

            int count = 0;
            PointPlus betterP1 = new(segment.P1);
            PointPlus betterP2 = new(segment.P2);


            PointPlus p1 = new(segment.P1);
            bool p1Attached = false;
            PointPlus p2 = new(segment.P2);
            bool p2Attached = false;
            //is the point already attached to another?
            AreEndpointsAttached(segment, ref p1Attached, ref p2Attached);

            if (!p1Attached)
            {
                do
                {
                    p1 += step;
                    //make sure we're still on the white part of the image
                    if (GetLuminanceAtPoint(p1) < 0.85) break;

                    betterP1 = p1;

                } while (count++ < maxExtend);
            }

            //now the second endpoint
            if (!p2Attached)
            {
                count = 0;
                do
                {
                    p2 -= step;
                    //make sure we're still on the white part of the image
                    if (GetLuminanceAtPoint(p2) < 0.85) break;
                    betterP2 = p2;
                } while (count++ < maxExtend);
            }
            segment.P1 = betterP1;
            segment.P2 = betterP2;
        }
    }

    private void AreEndpointsAttached(Segment segment, ref bool p1Attached, ref bool p2Attached)
    {
        foreach (Segment segTemp in segments)
        {
            if (segTemp == segment) continue;
            if (segment.P1.Near(segTemp.P1, 1) || segment.P1.Near(segTemp.P2, 1))
            {
                p1Attached = true; ;
            }
            if (segment.P2.Near(segTemp.P1, 1) || segment.P2.Near(segTemp.P2, 1))
            {
                p2Attached = true; ;
            }
        }

        foreach (Corner c in corners) //repeat for ARC endpoints 
        {
            if (c is Arc a1)
            {
                if (segment.P1.Near(a1.prevPt, 1) || segment.P1.Near(a1.nextPt, 1))
                {
                    p1Attached = true;
                }
                if (segment.P2.Near(a1.prevPt, 1) || segment.P2.Near(a1.nextPt, 1))
                {
                    p2Attached = true;
                }
            }
        }
    }

    void ExtendArcs()
    {
        int maxExtend = 2;
        foreach (Corner c in corners)
        {
            Arc a = c as Arc;
            if (a == null) continue;
            var circle = a.GetCircleFromArc();
            Angle startAngle = (a.prevPt - circle.center).Theta;
            Angle midAngle = (a.pt - circle.center).Theta;
            Angle endAngle = (a.nextPt - circle.center).Theta;

            Angle angleStep = Angle.FromDegrees(10);
            if (endAngle < startAngle) angleStep = -angleStep;
            if ((midAngle > startAngle && midAngle > endAngle) ||
                (midAngle < startAngle && midAngle < endAngle))
                angleStep = -angleStep;

            int count = 0;
            Angle a1 = endAngle;
            PointPlus p1;
            do
            {
                a1 += angleStep;
                p1 = circle.center + new PointPlus(circle.radius, a1);
                int x = (int)Round(p1.X);
                int y = (int)Round(p1.Y);
                try
                {
                    HSLColor pixel = new HSLColor(imageArray[x, y]);
                    if (pixel.luminance < .9)
                        break;
                }
                catch
                {
                    break;
                }
            } while (count++ < maxExtend);
            a1 -= angleStep;
            a.nextPt = circle.center + new PointPlus(circle.radius, a1);

            count = 0;
            a1 = startAngle;
            do
            {
                a1 -= angleStep;
                p1 = circle.center + new PointPlus(circle.radius, a1);
                int x = (int)Round(p1.X);
                int y = (int)Round(p1.Y);
                try
                {
                    HSLColor pixel = new HSLColor(imageArray[x, y]);
                    if (pixel.luminance < .9)
                        break;
                }
                catch
                {
                    break;
                }
            } while (count++ < maxExtend);
            a1 += angleStep;
            a.prevPt = circle.center + new PointPlus(circle.radius, a1);



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

        }
    }

    //given a set of segments, find intersection points
    //TODO handle intersections of arcs
    private void FindCorners(ref List<Segment> segmentsIn)
    {
        ExtendSegments();
        ExtendArcs();

        //put arcs in correct order
        for (int i = 0; i < corners.Count; i++)
        {
            if (corners[i] is Arc a1)
            {
                if (a1.StartAngle > a1.EndAngle)
                    (a1.prevPt, a1.nextPt) = (a1.nextPt, a1.prevPt);
            }
        }
        //merge arcs
        for (int i = 0; i < corners.Count - 1; i++)
        {
            // break;
            if (corners[i] is Arc a1)
            {
                for (int j = i + 1; j < corners.Count; j++)
                {
                    if (corners[j] is Arc a2)
                    {
                        var cir1 = a1.GetCircleFromArc();
                        var cir2 = a2.GetCircleFromArc();
                        if (cir1.center.Near(cir2.center, 2) && Abs(cir1.radius - cir2.radius) < 1)
                        {
                            if (a1.StartAngle > a2.StartAngle)
                                a1.prevPt = a2.prevPt;
                            if (a1.EndAngle < a2.EndAngle && a2.MidAngle < a2.EndAngle)
                                a1.nextPt = a2.nextPt;
                            else if (a1.EndAngle < a2.EndAngle && a2.MidAngle > a2.EndAngle)
                                a1.prevPt = a2.nextPt;
                            corners.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
        }

        //merge overlapping/duplicate segmenst
        for (int i = 0; i < segments.Count; i++)
        {
            Segment s1 = segments[i];
            for (int j = 0; j < corners.Count; j++)
            {
                if (corners[j] is Arc arc)
                {
                    if (arc.prevPt.Near(s1.P1, 3))
                    {
                        PointPlus newPoint = new Segment(s1.P1, arc.prevPt).MidPoint;
                        arc.prevPt = newPoint;
                        AddCornerToList(newPoint, s1.P2, arc.pt);
                        s1.P1 = newPoint;
                        continue;
                    }
                    if (arc.prevPt.Near(s1.P2, 3))
                    {
                        PointPlus newPoint = new Segment(s1.P2, arc.prevPt).MidPoint;
                        arc.prevPt = newPoint;
                        AddCornerToList(newPoint, s1.P1, arc.pt);
                        s1.P2 = newPoint;
                        continue;
                    }
                    if (arc.nextPt.Near(s1.P1, 3))
                    {
                        PointPlus newPoint = new Segment(s1.P1, arc.nextPt).MidPoint;
                        arc.nextPt = newPoint;
                        AddCornerToList(newPoint, s1.P2, arc.pt);
                        s1.P1 = newPoint;
                        continue;
                    }
                    if (arc.nextPt.Near(s1.P2, 3))
                    {
                        PointPlus newPoint = new Segment(s1.P2, arc.nextPt).MidPoint;
                        arc.nextPt = newPoint;
                        AddCornerToList(newPoint, s1.P1, arc.pt);
                        s1.P2 = newPoint;
                        continue;
                    }
                }
            }

            for (int j = i + 1; j < segments.Count; j++)
            {
                Segment s2 = segments[j];
                if (AreCollinear(s1, s2) && Overlap(s1, s2))
                {
                    // Merge the segments
                    if (s1.P1.X > s1.P2.X || s1.P1.X == s1.P2.X && s1.P1.Y > s1.P2.Y)
                        (s1.P1, s1.P2) = (s1.P2, s1.P1);
                    if (s2.P1.X > s2.P2.X || s2.P1.X == s2.P2.X && s2.P1.Y > s2.P2.Y)
                        (s2.P1, s2.P2) = (s2.P2, s2.P1);
                    Segment mergedSegment = new Segment(
                        new Point(Math.Min(s1.P1.X, s2.P1.X), Math.Min(s1.P1.Y, s2.P1.Y)),
                        new Point(Math.Max(s1.P2.X, s2.P2.X), Math.Max(s1.P2.Y, s2.P2.Y))
                    );
                    segments[i] = mergedSegment;
                    segments.RemoveAt(j);
                    j--;
                    continue;
                }
            }
        }
        float tolerance = 2.7f;
        for (int i = 0; i < segments.Count - 1; i++)
        {
            Segment s1 = segments[i];
            for (int j = i + 1; j < segments.Count; j++)
            {
                Segment s2 = segments[j];
                bool segmentsIntersect = Utils.LinesIntersect(s1, s2, out PointPlus intersection);
                if (!segmentsIntersect) continue; //(segments are parallel)
                if (intersection.Near(s1.P1, tolerance))
                {
                    if (intersection.Near(s2.P1, tolerance))
                    {
                        AddCornerToList(intersection, s1.P2, s2.P2);
                        UpdateCornerPoint(s1.P1, intersection);
                        UpdateCornerPoint(s2.P1, intersection);
                    }
                    if (intersection.Near(s2.P2, tolerance))
                    {
                        AddCornerToList(intersection, s1.P2, s2.P1);
                        UpdateCornerPoint(s1.P1, intersection);
                        UpdateCornerPoint(s2.P2, intersection);
                    }
                }
                else if (intersection.Near(s1.P2, tolerance))
                {
                    if (intersection.Near(s2.P1, tolerance))
                    {
                        AddCornerToList(intersection, s1.P1, s2.P2);
                        UpdateCornerPoint(s1.P2, intersection);
                        UpdateCornerPoint(s2.P1, intersection);
                    }
                    if (intersection.Near(s2.P2, tolerance))
                    {
                        AddCornerToList(intersection, s1.P1, s2.P1);
                        UpdateCornerPoint(s1.P2, intersection);
                        UpdateCornerPoint(s2.P2, intersection);
                    }
                }
            }
        }
        return;
    }

    private void UpdateCornerPoint(PointPlus oldValue, PointPlus newValue)
    {
        for (int i = 0; i < corners.Count; i++)
        {
            //if (corners[i].pt == oldValue) corners[i].pt = newValue; 
            if (corners[i].nextPt == oldValue) corners[i].nextPt = newValue;
            if (corners[i].prevPt == oldValue) corners[i].prevPt = newValue;
        }
    }
    private void AddCornerToList(PointPlus intersection, PointPlus prevPt, PointPlus nextPt)
    {
        //allow things to be offset by a few pixels
        //Is this corner already in the list?
        Corner alreadyInList = corners.FindFirst(x =>
            (x.pt - intersection).R < 2 &&
            (((x.prevPt - prevPt).R < 2 && (x.nextPt - nextPt).R < 2) ||
            ((x.prevPt - nextPt).R < 2 && (x.nextPt - prevPt).R < 2)));
        if (alreadyInList == null && prevPt == nextPt)
        {
            //is it an endpoint of a curve?
            alreadyInList = corners.FindFirst(x =>
                x.curve &&
                ((x.nextPt - intersection).R < 2 ||
                (x.prevPt - intersection).R < 2));
        }
        if (alreadyInList == null)
            corners.Add(new Corner { pt = intersection, prevPt = prevPt, nextPt = nextPt });
        else { }
    }


    private void FindOutlines()
    {
        //Find the orphans in the corners list
        //and add corners there.
        for (int i = 0; i < corners.Count; i++)
        {
            Corner c1 = corners[i];
            if (corners.FindFirst(x => c1.nextPt.Near(x.pt,1)) == null)
                corners.Add(new Corner() { pt = c1.nextPt, nextPt = c1.pt, prevPt = c1.pt,curve=false });
            if (corners.FindFirst(x => c1.prevPt.Near(x.pt,1)) == null)
                corners.Add(new Corner() { pt = c1.prevPt, nextPt = c1.pt, prevPt = c1.pt,curve=false });
        }

        List<Corner> newCorners = new();
        while (corners.Count > 0)
        {
            Corner start = corners.FindFirst(x => x.prevPt == x.nextPt);
            if (start == null)
                start = corners[0];
            newCorners.Add(start);
            corners.Remove(start);
            Corner next = start;
            while (next == start || next.nextPt != next.prevPt)
            {
                var temp = corners.FindFirst(x => x.pt.Near(next.nextPt,1));
                if (temp == null)
                    temp = corners.FindFirst(x => x.pt.Near(next.prevPt,1));
                if (temp == null)
                    break;
                next = temp;
                if (next != null)
                {
                    newCorners.Add(next);
                    corners.Remove(next);
                }
                else
                {
                    break;//problem with corners list
                }
            }
        }
        corners = newCorners;
        return;
    }


    void OrderSegmentsInGroup(List<Segment> segments)
    {
        //find an endpoint
        Segment currSegment = null;
        Segment s1 = null;
        foreach (Segment segment in segments)
        {
            s1 = segment;
            Segment s2 = segments.FindFirst(x => x != segment && (x.P1 == segment.P1 || x.P2 == segment.P1));
            if (s2 == null)
                goto endPointFound;
            s2 = segments.FindFirst(x => x != segment && (x.P1 == segment.P2 || x.P2 == segment.P2));
            if (s2 == null)
            {
                //swap the points within the segmene
                (s1.P1, s1.P2) = (s1.P2, s1.P1);
                goto endPointFound;
            }
        }
        if (currSegment == null) currSegment = segments[0];
        //There are no endpoints, this is a closed loop
        endPointFound:
        if (currSegment == null) currSegment = s1;

        List<Segment> ordered = new();
        ordered.Add(currSegment);
        segments.Remove(currSegment);
        while (segments.Count > 0)
        {
            Segment nextSegment = segments.FirstOrDefault(s => s.P1.Near(currSegment.P2, 2) ||
                s.P2.Near(currSegment.P2, 2));
            if (nextSegment != default)
            {
                segments.Remove(nextSegment);
                if (currSegment.P2.Near(nextSegment.P2, 2))
                    (nextSegment.P1, nextSegment.P2) = (nextSegment.P2, nextSegment.P1);
                currSegment = nextSegment;
                ordered.Add(currSegment);
            }
            else
                break;
        }
        foreach (Segment s in ordered)
            segments.Add(s);
    }
    void AddSegmentToList(Segment newSegment, List<Segment> mergedSegments)
    {
        if (newSegment.P1.X > newSegment.P2.X ||
            newSegment.P1.X == newSegment.P2.X && newSegment.P1.Y > newSegment.P2.Y)
            (newSegment.P1, newSegment.P2) = (newSegment.P2, newSegment.P1);
        for (int i = 0; i < mergedSegments.Count; i++)
        {
            var existingSegment = mergedSegments[i];
            if (AreCollinear(existingSegment, newSegment) && Overlap(existingSegment, newSegment))
            {
                // Merge the segments
                mergedSegments[i] = new Segment(
                    new Point(Math.Min(existingSegment.P1.X, newSegment.P1.X), Math.Min(existingSegment.P1.Y, newSegment.P1.Y)),
                    new Point(Math.Max(existingSegment.P2.X, newSegment.P2.X), Math.Max(existingSegment.P2.Y, newSegment.P2.Y))
                );
                return; // Stop after merging
            }
        }
        mergedSegments.Add(newSegment);
        ////is the segment already on the list
        //var existing = segments.FindFirst(x => x.P1.Near(s.P1, 3) && x.P2.Near(s.P2, 3) ||
        //    x.P1.Near(s.P2, 3) && x.P2.Near(s.P1, 3));
        //if (existing != null) return;
        ////is the segment colinear with any on the list
        //segments.Add(s);
    }
    const float tolerance = 2;

    private bool AreCollinear(Segment seg1, Segment seg2)
    {
        //do they have a similar slope?
        if (seg1.Angle - seg2.Angle > Angle.FromDegrees(5) ||
                           seg1.Angle - seg2.Angle < Angle.FromDegrees(-5))
            return false;

        //is the endpoint of one segment near the line-extension of the other segment?
        float dist = Utils.DistancePointToLine(seg1.P1, seg2.P1, seg2.P2);
        if (dist > tolerance) return false;
        return true;
    }

    private bool Overlap(Segment seg1, Segment seg2)
    {
        // Check overlap in the projected line (collinearity ensures they are in the same line)
        return Math.Max(seg1.P1.X, seg2.P1.X) - tolerance <= Math.Min(seg1.P2.X, seg2.P2.X) + tolerance;
    }
    private List<List<Segment>> GroupLineSegments(List<Segment> segments)
    {
        List<List<Segment>> groups = new();
        foreach (Segment segment in segments)
        {
            var connectedGroups = groups
                .Where(g => g.Any(s => s.P1.Near(segment.P1, 2) ||
                s.P1.Near(segment.P2, 2) ||
                s.P2.Near(segment.P1, 2) ||
                s.P2.Near(segment.P2, 2)))
                .ToList();
            if (connectedGroups.Count == 0)
                groups.Add(new List<Segment>() { segment });
            else
            {
                connectedGroups[0].Add(segment);
                for (int i = 1; i < connectedGroups.Count; i++)
                {
                    connectedGroups[0].AddRange(connectedGroups[i]);
                    groups.Remove(connectedGroups[i]);
                }
            }
        }
        return groups;
    }

    //trace around the outlines to get the order of corners and relative distances
    private void SaveOutlinesToUKS()
    {
        //set up the UKS structure for outlines
        GetUKS();
        if (theUKS == null) return;
        theUKS.GetOrAddThing("Sense", "Thing");
        theUKS.GetOrAddThing("Visual", "Sense");
        Thing outlineParent = theUKS.GetOrAddThing("Outline", "Visual");
        Thing tCorners = theUKS.GetOrAddThing("Corner", "Visual");
        theUKS.DeleteAllChildren(outlineParent);
        theUKS.DeleteAllChildren(tCorners);

        if (corners.Count == 0) return;

        FindOutlines();

        bool outlineIsClosed = (corners[0].nextPt != corners[0].prevPt);

        Thing outlineThing = theUKS.GetOrAddThing("Outline*", "Outlines");
        outlineThing.SetAttribute(theUKS.GetOrAddThing(outlineIsClosed ? "isSolidArea" : "notSolidArea", "Attribute"));

        foreach (var c in corners)
        {
            //add each corner/arc to UKS as a "V" (value on a Thing)
            //let's add it to the UKS
            Thing corner1 = theUKS.GetOrAddThing("corner*", tCorners);
            corner1.V = c;
            theUKS.AddStatement(outlineThing.Label, "has*", corner1.Label);
        }
    }

    private static void MakeClosedOutlineRightHanded(List<Corner> outline)
    {
        double sum = 0;
        int cnt = outline.Count;
        for (int i = 0; i < outline.Count; i++)
        {
            Corner p1 = outline[i];
            Corner p2 = outline[(i + 1) % cnt];
            sum += (p2.pt.X - p1.pt.X) *
                (p2.pt.Y + p1.pt.Y);
        }
        if (sum < 0)
        {
            outline.Reverse();
            foreach (Corner c in outline)
                (c.prevPt, c.nextPt) = (c.nextPt, c.prevPt);
        }
    }

    Thing GetOrAddColor(Color color)
    {
        Thing colorParent = theUKS.GetOrAddThing("Color", "Attribute");
        foreach (Thing t in colorParent.Children)
        {
            if (t.V is Color c && c.Equals(color))
                return t;
        }
        Thing theColor = theUKS.GetOrAddThing("color*", "Color");
        theColor.V = color;
        return theColor;
    }


    // fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
    }

    // the following can be used to massage public data to be different in the xml file
    // delete if not needed
    public override void SetUpBeforeSave()
    {
        Thing t = theUKS.Labeled("currentShape");
        if (t != null) { theUKS.DeleteAllChildren(t); }
        t = theUKS.Labeled("corner");
        if (t != null) { theUKS.DeleteAllChildren(t); }
        t = theUKS.Labeled("Outline");
        if (t != null) { theUKS.DeleteAllChildren(t); }
        t = theUKS.Labeled("MentalModel");
        if (t != null) { theUKS.DeleteAllChildren(t); }
    }


    public override void SetUpAfterLoad()
    {
        SetUpUKSEntries();

        //here we parse
        //objects out of the Xml stream
        foreach (Thing t in theUKS.UKSList)
        {
            if (t.V is System.Xml.XmlNode[] nodes)
            {
                if (nodes[0].Value == "Color")
                {
                    byte A = byte.Parse(nodes[1].InnerText);
                    byte R = byte.Parse(nodes[2].InnerText);
                    byte G = byte.Parse(nodes[3].InnerText);
                    byte B = byte.Parse(nodes[4].InnerText);
                    Color theColor = new() { A = A, R = R, G = G, B = B, };
                    t.V = theColor;
                }
                if (nodes[0].Value == "HSLColor")
                {
                    float hue = float.Parse(nodes[1].InnerText);
                    float saturation = float.Parse(nodes[2].InnerText);
                    float luminance = float.Parse(nodes[3].InnerText);
                    HSLColor theColor = new(hue, saturation, luminance);
                    t.V = theColor;
                }
                if (nodes[0].Value == "Corner")
                {
                    Corner c = new();
                    //get a pointplus node
                    float x = float.Parse(nodes[1].FirstChild.InnerText);
                    float y = float.Parse(nodes[1].FirstChild.NextSibling.InnerText);
                    float conf = float.Parse(nodes[1].FirstChild.NextSibling.NextSibling.InnerText);
                    c.pt = new PointPlus { X = x, Y = y, Conf = conf, };
                    //get the angle node
                    float theta = float.Parse(nodes[2].FirstChild.InnerText);
                    //get the orientation node
                    float theta1 = float.Parse(nodes[3].FirstChild.InnerText);
                    //c.orientation = Angle.FromDegrees(theta1);
                    t.V = c;
                }
            }
        }

    }

    private void SetUpUKSEntries()
    {
        theUKS.AddStatement("Attribute", "is-a", "Thing");
        theUKS.AddStatement("Color", "is-a", "Attribute");
        theUKS.AddStatement("Size", "is-a", "Attribute");
        theUKS.AddStatement("Position", "is-a", "Attribute");
        theUKS.AddStatement("Rotation", "is-a", "Attribute");
        theUKS.AddStatement("Shape", "is-a", "Attribute");
        theUKS.AddStatement("Offset", "is-a", "Attribute");
        theUKS.AddStatement("Distance", "is-a", "Attribute");

        //Set up angles and distances so they are near each other
        Relationship r2 = null;
        r2 = theUKS.AddStatement("isSimilarTo", "is-a", "relationshipType");
        r2 = theUKS.AddStatement("isSimilarTo", "hasProperty", "isCommutative");
        r2 = theUKS.AddStatement("isSimilarTo", "hasProperty", "isTransitive");

        for (int i = 1; i < 10; i++)
        {
            theUKS.AddStatement("distance." + i, "is-a", "distance");
            if (i < 9)
                r2 = theUKS.AddStatement("distance." + i, "isSimilarTo", "distance." + (i + 1));
            r2.Weight = 0.8f;
        }
        theUKS.AddStatement("distance1.0", "is-a", "distance");
        r2 = theUKS.AddStatement("distance1.0", "isSimilarTo", "distance.9");
        r2.Weight = 0.8f;

        for (int i = -17; i < 18; i++)
        {
            theUKS.AddStatement("angle" + (i * 10), "is-a", "Rotation");
            r2 = theUKS.AddStatement("angle" + (i * 10), "isSimilarTo", "angle" + ((i + 1) * 10));
            r2.Weight = 0.8f;
        }
        r2 = theUKS.AddStatement("angle180", "is-a", "rotation");
        r2 = theUKS.AddStatement("angle180", "isSimilarTo", "angle-170");
        r2.Weight = 0.8f;
    }

    // called whenever the size of the module rectangle changes
    // for example, you may choose to reinitialize whenever size changes
    // delete if not needed
    public override void SizeChanged()
    {

    }
    public override void UKSInitializedNotification()
    {
        SetUpUKSEntries();
    }
}

