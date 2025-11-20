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


    public bool[,] boundaryArray;
    public int hSize = 5;
    public int vSize = 5;
    public int patchSize = 5;
    public int stride = 1;
    public int counter = 0;



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
    public override void Fire()
    {
        Init();  //be sure to leave this here

        UpdateDialog();

        if (CurrentFilePath == previousFilePath) return;
        previousFilePath = CurrentFilePath;

        LoadImageFileToPixelArray(CurrentFilePath);
        FindBoundaries(imageArray);

        segments = new();
    }

    Random rand = new();

    public void Refresh()
    {
        //ClearBoundaryArray();
        //DrawLine(p1, p2);
        SearchAndLearn();
    }

    public void InitArray()
    {
        boundaryArray = new bool[hSize, vSize];
        theUKS.GetOrAddThing("boundaryPoint");
        theUKS.GetOrAddThing("hasBoundary", "RelationshipType");
        theUKS.GetOrAddThing("patch");

        //initialize the boundary array
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                string attrName = $"Pt_{x:D2}_{y:D2}";
                theUKS.GetOrAddThing(attrName, "boundaryPoint");
            }

        //initialiize the patch array 
        //create a Thing for each possible patch based on patchSize and stride
        //set all the weights so that the center has the highest weight and weights decrease radially from the center
        int numPatchesX = (hSize - patchSize) / stride + 1;
        int numPatchesY = (vSize - patchSize) / stride + 1;
        int numPatchesPerPixel = patchSize * 2 - 2;

        int half = patchSize / 2;
        float minWeight = 0.1f;
        float maxRadius = (float)Math.Sqrt(half * half + half * half);

        for (int i = 0; i < numPatchesPerPixel; i++)
            for (int patchX = 0; patchX < numPatchesX; patchX++)
                for (int patchY = 0; patchY < numPatchesY; patchY++)
                {
                    int patchCenterX = patchX * stride + patchSize / 2;
                    int patchCenterY = patchY * stride + patchSize / 2;
                    string patchName = $"Patch_{patchCenterX:D2}_{patchCenterY:D2}_{i}";
                    Thing patchThing = theUKS.GetOrAddThing(patchName, "patch");

                    // this is the maximum weight at the center for this patch index i
                    //float centerMaxWeight = (float)(patchSize - i * 0.1);
                    float centerMaxWeight = 1f;

                    for (int x = -half; x < half + 1; x++)
                        for (int y = -half; y < half + 1; y++)
                        {
                            int imgX = patchCenterX + x;
                            int imgY = patchCenterY + y;
                            string attrName = $"Pt_{imgX:D2}_{imgY:D2}";

                            // distance from center
                            float dx = x;
                            float dy = y;
                            float r = (float)Math.Sqrt(dx * dx + dy * dy);

                            // radial factor: 1 at center, ~0 at farthest corner
                            float radial = (maxRadius > 0f) ? 1f - (r / maxRadius) : 1f;
                            if (radial < 0f) radial = 0f;

                            // interpolate between minWeight and centerMaxWeight based on distance
                            float maxWeight = minWeight + (centerMaxWeight - minWeight) * radial;

                            //float maxWeight = minWeight + (1.5f - minWeight) * radial;
                            float initialWeight = maxWeight / 2;
                            if (x == 0 && y == 0) initialWeight = centerMaxWeight;
                            if (x == 0 && y == 0) maxWeight = centerMaxWeight;

                            var rRel = patchThing.AddRelationship(attrName, "hasBoundary", true, initialWeight);
                            if (rRel.target == null)
                            {
                                // handle missing target if needed
                            }
                            rRel.maxWeight = maxWeight;
                        }
                }

        //InitHVLInes();
    }


    Point p1, p2;
    public void SingteTestPattern()
    {
        do
        {
            p1 = new Point((int)(rand.NextDouble() * hSize), (int)(rand.NextDouble() * vSize));
        } while (p1.X != 0 && p1.Y != 0);

        float bias = .8f;
        if (rand.NextDouble() < bias)
        {
            p2 = new Point(patchSize - 1 - p1.X, patchSize - 1 - p1.Y);
        }
        else
            p2 = new Point((int)(rand.NextDouble() * hSize), (int)(rand.NextDouble() * vSize));

        ClearBoundaryArray();

        DrawLine(p1, p2);


        /*
         * //build a sample arc for testing
                ClearBoundaryArray();
                boundaryArray[1, 0] = true;
                boundaryArray[2, 1] = true;
                boundaryArray[2, 2] = true;
                boundaryArray[2, 3] = true;
                boundaryArray[1, 4] = true;
        */
        SearchAndLearn();

        //        SetBoundaryArrayFromImage();
        //        SearchAndLearn();

        UpdateDialog();

    }

    private void InitHVLInes()
    {
        Point p1, p2;

        //draw vertical lines
        for (int x = 1; x < hSize; x++)
        {
            p1 = new Point(x, 0);
            p2 = new Point(x, 27);
            DrawLine(p1, p2);
            SearchAndLearn();
            ClearBoundaryArray();
        }
        //draw horizontallines
        for (int y = 1; y < vSize; y++)
        {
            p1 = new Point(0, y);
            p2 = new Point(27, y);
            DrawLine(p1, p2);
            SearchAndLearn();
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

    public void ClearBoundaryArray()
    {
        foreach (var t in theUKS.UKSList) t.confidence = 0;
        foreach (var t in theUKS.UKSList) t.lastFiredTime = new DateTime(0);
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
                boundaryArray[x, y] = false;
    }

    void SetBoundaryArrayFromImage()
    {
        ClearBoundaryArray();
        foreach (var pt in boundaryPoints)
        {
            int x = (int)pt.X;
            int y = (int)pt.Y;
            if (x < 0 || y < 0) continue;
            if (x >= boundaryArray.GetLength(0)) continue;
            if (y >= boundaryArray.GetLength(1)) continue;
            boundaryArray[x, y] = true;
        }
    }

    private void SearchAndLearn(Thing parent = null)
    {
        //create an array of the boundary points at 10x the resolution of the original image.
        int sizeMultipler = 1;

        Thing queryThing = new Thing() { Label = "theQuery" };
        //convert to parallel for speed 
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
        //System.Threading.Tasks.Parallel.For(0, boundaryArray.GetLength(0) - patchSize, offsetX =>
        {
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                if (boundaryArray[x, y]) //is this a boundary point?
                {
                    string attrName = $"Pt_{x:D2}_{y:D2}";
                    queryThing.AddRelationship(attrName, "hasBoundary");
                }
            }
        }
        if (queryThing.Relationships.Count > 1)
        {
            var match = theUKS.SearchForClosestMatch(queryThing, "Thing");

            match.RemoveAll(x => x.t.Label.StartsWith("theQuery"));

            if (match[0].conf < 1) return;

            int pixelCount = queryThing.Relationships.Count(x => x.Weight == 1);

            //mutual suppression
            for (int i = 0; i < match.Count; i++)
            {
                Thing t0 = match[i].t;
                if (match[i].conf <= .31) //remove low-value hits.
                {
                    match.RemoveAt(i);
                    i--;
                    continue;
                }
                string l0 = t0.Label[..11];
                for (int j = i + 1; j < match.Count; j++)
                {
                    Thing t1 = match[j].t;
                    string l1 = t1.Label[..11];
                    if (l1 == l0)
                    {
                        match.RemoveAt(j);
                        j--;
                    }
                }
            }


            foreach (var item in match)
            {
                AdjustWeights(item.t, queryThing);
                item.t.SetFired();
            }

            void AdjustWeights(Thing patch, Thing inputPattern)
            {
                string[] nameFields = patch.Label.Split("_");
                int x = int.Parse(nameFields[1]);
                int y = int.Parse(nameFields[2]);
                string centerPtLabel = $"Pt_{x:D2}_{y:D2}";

                patch.SetFired(); //this patch was the winner.

                //we are adjusting weights of an already-set patch
                foreach (Relationship r in patch.Relationships)
                {
                    if (r.reltype.Label != "hasBoundary") continue;
                    //do not adjust the center point
                    if (r.target.Label == centerPtLabel) continue;

                    //did the input point fire?
                    Relationship rFound = inputPattern.Relationships.FindFirst(x => x.target == r.target);

                    // targets: ON -> +1, OFF -> -0.5
                    float tp = (rFound != null) ? r.maxWeight : -0.5f;
                    float eta = (rFound != null) ? 0.06f : 0.03f; // example: smaller step for OFF
                    r.Weight += eta * (tp - r.Weight);
                    // clamp to keep things well-behaved
                    if (r.Weight > r.maxWeight) r.Weight = r.maxWeight;
                    if (r.Weight < -1f) r.Weight = -1f;
                }
            }
        }
        else
        {
            theUKS.DeleteThing(queryThing);
        }
        //});
    }


    int count = 0;
    public void Show()
    {
        ClearBoundaryArray();
        Thing t = theUKS.GetOrAddThing("patch");
        if (count >= t.Children.Count)
        {
            count = 0;
        }
        t.Children[count].SetFired();
        count++;

        ////for now, only things without children are pruneable
        //for (int i = 0; i < theUKS.UKSList.Count; i++)
        //{
        //    Thing t = theUKS.UKSList[i];
        //    if (t.Children.Count > 0) continue;
        //    if (!t.HasAncestor("UnknownObject")) continue;
        //    if (t.useCount == 1)
        //    {
        //        theUKS.DeleteThing(t);
        //        i--;
        //    }
        //}
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
            imageArray = new Color[hSize, vSize];
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

