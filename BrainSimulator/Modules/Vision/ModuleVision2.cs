//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules;

public partial class ModuleVision2 : ModuleBase
{
    private string currentFilePath = "";
    public string previousFilePath = null;
    public BitmapImage bitmap = null;
    //    public List<Corner> corners;
    public List<Segment> segments;
    public Color[,] imageArray;
    //public HoughTransform segmentFinder;
    public List<PointPlus> strokePoints = new();
    public List<PointPlus> CenterLinePts = null;
    public List<PointPlus> boundaryPoints = new();
    bool isSingleDigit = false;


    public string CurrentFilePath
    {
        get { return currentFilePath; }
        set
        {
            if (currentFilePath != value)
            {
                currentFilePath = value;
            }
        }
    }

    public ModuleVision2()
    {
    }

    //fill this method in with code which will execute
    //once for each cycle of the engine
    bool init = false;
    public override void Fire()
    {
        Init();  //be sure to leave this here

        UpdateDialog();

        if (CurrentFilePath == previousFilePath) return;
        previousFilePath = CurrentFilePath;

        LoadImageFileToPixelArray(CurrentFilePath);

        segments = new();
        boundaryArray = new bool[28, 28];
        theUKS.GetOrAddThing("boundaryPoint");
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                string attrName = $"Pt_{x:D2}_{y:D2}";
                theUKS.GetOrAddThing(attrName, "boundaryPoint").SetFired();
            }
        Random rand = new();

        if (!init)
            InitHVLInes();
        init = true;


        Point p1, p2;
        p1 = new Point(1, 1);
        p2 = new Point(20, counter*10);
        counter++;
        DrawLine(p1, p2);
        CreateBoundaryArray();

        UpdateDialog();
    }

    private void InitHVLInes()
    {
        Thing horiz = theUKS.GetOrAddThing("Horiz");
        Thing vert = theUKS.GetOrAddThing("Vert");
        Point p1 = new(5, 5);
        Point p2 = new(15, 5);
        for (int x = 0; x < 28; x++)
        {
            p1 = new Point(x, 0);
            p2 = new Point(x, 27);
            DrawLine(p1, p2);
            CreateBoundaryArray(vert);
            ClearBoundaryArray();
        }
        for (int y = 0; y < 28; y++)
        {
            p1 = new Point(0, y);
            p2 = new Point(27, y);
            DrawLine(p1, p2);
            CreateBoundaryArray(horiz);
            ClearBoundaryArray();
        }
    }

    void DrawLine(Point p1, Point pt)
    {

        //create a line between p1 and pt in imageArray
        int x0 = (int)p1.X;
        int y0 = (int)p1.Y;
        int x1 = (int)pt.X;
        int y1 = (int)pt.Y;
        int dx = Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2; /* error value e_xy */
        while (true)
        {
            if (x0 >= 0 && x0 < boundaryArray.GetLength(0) && y0 >= 0 && y0 < boundaryArray.GetLength(1))
                boundaryArray[x0, y0] = true;
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            } /* e_xy+e_x > 0 */
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            } /* e_xy+e_y < 0 */
        }
    }

    public bool[,] boundaryArray;

    void ClearBoundaryArray()
    {
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
                boundaryArray[x, y] = false;
    }


    public int squareSize = 5;
    public int offset = 5;
    public int counter = 0;
    private void CreateBoundaryArray(Thing parent = null)
    {
        theUKS.GetOrAddThing("boundaryPoint", "Thing");
        theUKS.GetOrAddThing("hasBoundary", "RelationshipType");
        //create an array of the boundary points at 10x the resolution of the original image.
        int sizeMultipler = 1;

        int squareSize = 5;
        int offset = 5;
        for (int offsetX = 0; offsetX < boundaryArray.GetLength(0) - squareSize; offsetX += offset)
        {
            for (int offsetY = 0; offsetY < boundaryArray.GetLength(1) - squareSize; offsetY += offset)
            {
                Thing queryThing = new Thing() { Label = "theQuery" };
                for (int x = 0; x < squareSize; x++)
                {
                    for (int y = 0; y < squareSize; y++)
                    {
                        if (boundaryArray[offsetX + x, offsetY + y])
                        {
                            string attrName = $"Pt_{offsetX + x:D2}_{offsetY + y:D2}";
                            queryThing.AddRelationship(attrName, "hasBoundary", true, 1);
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    // Skip the center point itself
                                    if (dx == 0 && dy == 0)
                                        continue;

                                    int nx = x + dx + offsetX;
                                    int ny = y + dy + offsetY;
                                    if (nx < 0 || nx >= boundaryArray.GetLength(0)) continue;
                                    if (ny < 0 || ny >= boundaryArray.GetLength(1)) continue;

                                    attrName = $"Pt_{nx:D2}_{ny:D2}";
                                    Relationship r = queryThing.HasRelationship(queryThing, "hasBoundary", attrName);
                                    if (r == null || r.Weight < 1)
                                        queryThing.AddRelationship(attrName, "hasBoundary", true, .5f);
                                }
                            }
                        }
                    }
                }
                if (queryThing.Relationships.Count > 1)
                {
                    var match = theUKS.SearchForClosestMatch(queryThing, "Thing");
                    int pixelCount = queryThing.Relationships.Count(x => x.Weight == 1);
                    if (match.Count == 0 || match[0].conf < pixelCount*1.5)
                    {
                        //learn this pattern
                        lock (theUKS.UKSList)
                        {
                            theUKS.UKSList.Add(queryThing);
                        }
                        string boxName = $"Box_{offsetX:D2}_{offsetY:D2}_*";
                        queryThing.Label = boxName;
                        if (parent == null)
                            queryThing.AddParent("UnknownObject");
                        else
                            queryThing.AddParent(parent);
                        queryThing.SetFired();
                        continue;
                    }
                    else
                    {
                        theUKS.DeleteThing(queryThing);
                        if (match[0].conf < 5)
                            continue;
                        Thing t = match[0].t;
                        if (t.lastFiredTime < DateTime.Now - TimeSpan.FromSeconds(10))
                            match[0].t.SetFired();
                    }
                }
            }
        }





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

    public float scale = 1;
    public int offsetX = 0;
    public int offsetY = 0;

    public void LoadImageFileToPixelArray(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            imageArray = new Color[28, 28];
            return;
        }
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
                //if (nodes[0].Value == "Corner")
                //{
                //    Corner c = new();
                //    //get a pointplus node
                //    float x = float.Parse(nodes[1].FirstChild.InnerText);
                //    float y = float.Parse(nodes[1].FirstChild.NextSibling.InnerText);
                //    float conf = float.Parse(nodes[1].FirstChild.NextSibling.NextSibling.InnerText);
                //    c.pt = new PointPlus { X = x, Y = y, Conf = conf, };
                //    //get the angle node
                //    float theta = float.Parse(nodes[2].FirstChild.InnerText);
                //    //get the orientation node
                //    float theta1 = float.Parse(nodes[3].FirstChild.InnerText);
                //    //c.orientation = Angle.FromDegrees(theta1);
                //    t.V = c;
                //}
            }
        }

    }

    private void SetUpUKSEntries()
    {
        //theUKS.AddStatement("Attribute", "is-a", "Thing");
        //theUKS.AddStatement("Color", "is-a", "Attribute");
        //theUKS.AddStatement("Size", "is-a", "Attribute");
        //theUKS.AddStatement("Position", "is-a", "Attribute");
        //theUKS.AddStatement("Rotation", "is-a", "Attribute");
        //theUKS.AddStatement("Shape", "is-a", "Attribute");
        //theUKS.AddStatement("Offset", "is-a", "Attribute");
        //theUKS.AddStatement("Distance", "is-a", "Attribute");

        //Set up angles and distances so they are near each other
        Relationship r2 = null;
        r2 = theUKS.AddStatement("isSimilarTo", "is-a", "relationshipType");
        r2 = theUKS.AddStatement("isSimilarTo", "hasProperty", "isCommutative");
        r2 = theUKS.AddStatement("isSimilarTo", "hasProperty", "isTransitive");

        //for (int i = 1; i < 10; i++)
        //{
        //    theUKS.AddStatement("distance." + i, "is-a", "distance");
        //    if (i < 9)
        //        r2 = theUKS.AddStatement("distance." + i, "isSimilarTo", "distance." + (i + 1));
        //    r2.Weight = 0.8f;
        //}
        //theUKS.AddStatement("distance1.0", "is-a", "distance");
        //r2 = theUKS.AddStatement("distance1.0", "isSimilarTo", "distance.9");
        //r2.Weight = 0.8f;

        //for (int i = -17; i < 18; i++)
        //{
        //    theUKS.AddStatement("angle" + (i * 10), "is-a", "Rotation");
        //    r2 = theUKS.AddStatement("angle" + (i * 10), "isSimilarTo", "angle" + ((i + 1) * 10));
        //    r2.Weight = 0.8f;
        //}
        //r2 = theUKS.AddStatement("angle180", "is-a", "rotation");
        //r2 = theUKS.AddStatement("angle180", "isSimilarTo", "angle-170");
        //r2.Weight = 0.8f;
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

