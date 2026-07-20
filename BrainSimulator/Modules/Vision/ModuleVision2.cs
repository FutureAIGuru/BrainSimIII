//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2025 Charles Simon, all rights reserved
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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

    //to read in image files and detect boundary points
    public BitmapImage bitmap = null;
    public Color[,] imageArray;
    public List<PointPlus> strokePoints = new(); //boundary detector fills this in 
    public List<PointPlus> boundaryPoints = new();

    //to hold the detected boundary points
    public bool[,] boundaryArray;
    public int hSize = 15;
    public int vSize = 15;
    public int patchSize = 5;
    public int stride = 1;
    public int counter = 0;
    // Original value: 4
    public int patchesPerPixel = 8;
    // Original Weights: etaOn = 0.06f, etaOff = 0.03f, tpOff = -0.05f, tpOnWeight = 1
    public float etaOn = 0.06f;
    public float etaOff = 0.03f;
    public float tpOff = -0.05f;
    public float tpOnWeight = 1;



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

        if (imageArray == null)
            imageArray = new Color[hSize, vSize];

        if (boundaryArray == null) return;

        LoadImageFileToPixelArray(CurrentFilePath);
        FindBoundaries(imageArray);
        SetBoundaryArrayFromImage();
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
        theUKS.GetOrAddThing("hasBoundary", "RelationshipType");

        boundaryArray = new bool[hSize, vSize];
        string prevLayerName = "Pt";
        theUKS.GetOrAddThing(prevLayerName);

        //initialize the boundary array
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                string attrName = $"{prevLayerName}_{x:D2}_{y:D2}";
                theUKS.GetOrAddThing(attrName, prevLayerName);
            }

        //initialiize the patch array 
        //create a Thing for each possible patch based on patchSize and stride
        //set all the weights so that the center has the highest weight and weights decrease radially from the center
        int numPatchesX = (hSize - patchSize) / stride + 1;
        int numPatchesY = (vSize - patchSize) / stride + 1;
        //patchesPerPixel = patchSize * 2 - 2;

        int half = patchSize / 2;

        string layerName = "patch";

        InitializeLayer(prevLayerName, numPatchesX, numPatchesY, patchesPerPixel, half, layerName);
        //InitializeLayer("patch", numPatchesX, numPatchesY, 16, 1, "corner");
        InitCornerPoints();
    }
    void InitCornerPoints()
    {
        theUKS.GetOrAddThing("corner");
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                string attrName = $"corner_{x:D2}_{y:D2}";
                theUKS.GetOrAddThing(attrName, "corner");
            }
    }
    void TestCounterPatch()
    {
        int half = 1;
        Point center = new PointPlus(1, 1f);
        Thing parent = theUKS.GetOrAddThing("counter");
        Thing relType = theUKS.GetOrAddThing("count", "RelationshipType");
        Thing notype = theUKS.GetOrAddThing("not", "RelationshipType");

        //connections from lower levels
        for (int i = 0; i < 8; i++)
        {
            Thing counter = theUKS.GetOrAddThing($"counter_{center.X:F0}_{center.Y:F0}_{i}", parent);
            for (int x = -half; x < half + 1; x++)
                for (int y = -half; y < half + 1; y++)
                {
                    float weight = (i == 0) ? 0 : .1f / (float)i;
                    float centerWeight = (i == 0) ? 1.0f : .9f;
                    if (x == 0 && y == 0) weight = centerWeight;
                    string targetName = $"pt_{(int)(center.X + x):D2}_{((int)center.Y + y):D2}";
                    Thing pt = theUKS.Labeled(targetName);
                    if (pt == null) continue;
                    Relationship r = counter.AddRelationship(pt, relType, true, weight);
                }
        }

        //mutual suppression
        for (int i = 7; i >= 0; i--)
            for (int j = i - 1; j >= 0; j--)
            {
                if (i == j) continue;
                string srcName = $"counter_{center.X:F0}_{center.Y:F0}_{i}";
                string trgName = $"counter_{center.X:F0}_{center.Y:F0}_{j}";
                theUKS.Labeled(srcName).AddRelationship(trgName, notype, true, -1.0f);
            }
    }

    private void InitializeLayer(string prevLayerName, int numPatchesX, int numPatchesY, int numPatchesPerPixel, int half, string layerName)
    {
        // allocate all the nodes            
        theUKS.GetOrAddThing(layerName);
        for (int i = 0; i < numPatchesPerPixel; i++)
            for (int patchX = 0; patchX < numPatchesX; patchX++)
                for (int patchY = 0; patchY < numPatchesY; patchY++)
                {

                    int patchCenterX = patchX * stride + half;
                    int patchCenterY = patchY * stride + half;
                    string patchName = $"{layerName}_{patchCenterX:D2}_{patchCenterY:D2}_{i}";
                    var grandParent = theUKS.GetOrAddThing($"{layerName}_{patchCenterX:D2}", layerName);
                    var parent = theUKS.GetOrAddThing($"{layerName}_{patchCenterX:D2}_{patchCenterY:D2}", grandParent);
                    Thing patchThing = theUKS.GetOrAddThing(patchName, parent);
                }

        // add the connections from the previous layer
        float minWeight = 0.1f;
        float maxRadius = (float)Math.Sqrt(half * half + half * half);
        for (int i = 0; i < numPatchesPerPixel; i++)
            for (int patchX = 0; patchX < numPatchesX; patchX++)
                for (int patchY = 0; patchY < numPatchesY; patchY++)
                {
                    int patchCenterX = patchX * stride + half;
                    int patchCenterY = patchY * stride + half;
                    string patchName = $"{layerName}_{patchCenterX:D2}_{patchCenterY:D2}_{i}";
                    Thing patchThing = theUKS.GetOrAddThing(patchName);

                    float centerMaxWeight = 1f;

                    // Each of the numPatchesPerPixel copies at this location is a distinct
                    // orientation-tuned "simple cell", evenly spaced across the 180-degree
                    // range of undirected line orientations (e.g. 0, 45, 90, 135 degrees for
                    // numPatchesPerPixel == 4). Without this bias, all copies start out as
                    // identical isotropic blobs and competitive learning has nothing to break
                    // the tie on, so one copy wins every time and the others never specialize.
                    double preferredAngle = (numPatchesPerPixel > 0) ? Math.PI * i / numPatchesPerPixel : 0;

                    for (int x = -half; x < half + 1; x++)
                        for (int y = -half; y < half + 1; y++)
                        {
                            int imgX = patchCenterX + x;
                            int imgY = patchCenterY + y;
                            // distance from center
                            float dx = x;
                            float dy = y;
                            float r = (float)Math.Sqrt(dx * dx + dy * dy);

                            // radial factor: 1 at center, ~0 at farthest corner
                            float radial = (maxRadius > 0f) ? 1f - (r / maxRadius) : 1f;
                            if (radial < 0f) radial = 0f;

                            // angular factor: elongates the receptive field along preferredAngle
                            // and suppresses it perpendicular to that axis. cos^2 is naturally
                            // symmetric under a 180-degree rotation, so it matches undirected
                            // line orientation without any extra mod-180 handling.
                            float orientationFactor = 1f;
                            if (!(x == 0 && y == 0) && numPatchesPerPixel > 1)
                            {
                                double offsetAngle = Math.Atan2(dy, dx);
                                double cosDiff = Math.Cos(offsetAngle - preferredAngle);
                                orientationFactor = (float)(cosDiff * cosDiff);

                                // keep a floor so off-axis connections are weakened, not severed
                                const float minOrientationFactor = 0.15f;
                                orientationFactor = minOrientationFactor + (1f - minOrientationFactor) * orientationFactor;
                            }

                            // interpolate between minWeight and centerMaxWeight based on distance and orientation
                            float maxWeight = minWeight + (centerMaxWeight - minWeight) * radial * orientationFactor;
                            maxWeight *= .75f;  //reduces extraneous hits

                            //float maxWeight = minWeight + (1.5f - minWeight) * radial;
                            float initialWeight = maxWeight / 2;
                            if (x == 0 && y == 0) initialWeight = centerMaxWeight;
                            if (x == 0 && y == 0) maxWeight = centerMaxWeight;



                            string attrName = $"{prevLayerName}_{imgX:D2}_{imgY:D2}";
                            Thing t = theUKS.Labeled(attrName);
                            if (t != null && t.Children.Count > 0)
                            {
                                foreach (Thing child in t.Children)
                                {
                                    theUKS.GetRelationship(patchThing, "hasBoundary", child);
                                    var rRel1 = patchThing.AddRelationship(child, "hasBoundary", true, initialWeight);
                                    if (rRel1.target == null)
                                    {
                                        // handle missing target if needed
                                    }
                                    rRel1.maxWeight = maxWeight;
                                }
                                continue; ;
                            }

                            attrName = $"{prevLayerName}_{imgX:D2}_{imgY:D2}";
                            if (theUKS.Labeled(attrName) == null) continue;

                            var rRel = patchThing.AddRelationship(attrName, "hasBoundary", true, initialWeight);
                            if (rRel.target == null)
                            {
                                // handle missing target if needed
                            }
                            rRel.maxWeight = maxWeight;
                        }
                }

        //add connections to nearest-neighbor patches which can be strengthened later to represent linear features
        theUKS.GetOrAddThing("collinearWith", "RelationshipType");
        for (int i = 0; i < numPatchesPerPixel; i++)
            for (int patchX = 0; patchX < numPatchesX; patchX++)
                for (int patchY = 0; patchY < numPatchesY; patchY++)
                {
                    int patchCenterX = patchX * stride + half;
                    int patchCenterY = patchY * stride + half;
                    string patchName = $"{layerName}_{patchCenterX:D2}_{patchCenterY:D2}_{i}";
                    if (theUKS.Labeled(patchName) == null) continue;
                    Thing patchThing = theUKS.GetOrAddThing(patchName);
                    for (int x = -1; x < 2; x++)
                        for (int y = -1; y < 2; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            int nnX = patchCenterX + x;
                            int nnY = patchCenterY + y;
                            if (nnX < 0 || nnY < 0 || nnX >= hSize || nnY >= vSize) continue;
                            for (int j = 0; j < numPatchesPerPixel; j++)
                            {
                                string nnPatchName = $"{layerName}_{nnX:D2}_{nnY:D2}_{j}";
                                if (theUKS.Labeled(nnPatchName) == null) continue;
                                Thing nnPatchThing = theUKS.GetOrAddThing(nnPatchName);
                                if (nnPatchThing != null)
                                    patchThing.AddRelationship(nnPatchThing, "collinearWith", true, 0.1f);
                            }
                        }
                }

        //add relationships for nearly-collinear patches
        theUKS.GetOrAddThing("nearlyCollinearWith", "RelationshipType");
        for (int patchX = 2; patchX < numPatchesX + 2; patchX++)
            for (int patchY = 2; patchY < numPatchesY + 2; patchY++)
                for (int i = 0; i < numPatchesPerPixel; i++)
                    for (int j = 0; j < numPatchesPerPixel; j++)
                    {
                        if (j == i) continue;
                        string patchName1 = $"{layerName}_{patchX:D2}_{patchY:D2}_{i}";
                        string patchName2 = $"{layerName}_{patchX:D2}_{patchY:D2}_{j}";
                        Thing source = theUKS.Labeled(patchName1);
                        if (source == null) continue;
                        Thing target = theUKS.Labeled(patchName2);
                        if (target == null) continue;
                        source.AddRelationship(target, "nearlyCollinearWith", true, .1f);
                    }
    }

    public int testMethod = 1;
    public void SingteTestPattern()
    {
        PointPlus p1, p2, p3;
        ClearBoundaryArray();

        if (testMethod == 0)  //fixed little segment
        {
            p1 = new PointPlus(4, 3f);
            p2 = new PointPlus(4, 7f);
            DrawLine(p1, p2);
            p1 = new PointPlus(9, 7f);
            p2 = new PointPlus(4, 7f);
            DrawLine(p1, p2);
        }
        else if (testMethod == 1) //random line
        {
            Point RandomBorderPoint()
            {
                int perimeter = 2 * (hSize + vSize) - 4; // all edge points
                int r = rand.Next(perimeter);

                if (r < hSize)                     // top edge (x = 0..hSize-1, y = 0)
                    return new Point(r, 0);

                r -= hSize;
                if (r < vSize - 1)                 // right edge (x = hSize-1, y = 1..vSize-1)
                    return new Point(hSize - 1, r + 1);

                r -= (vSize - 1);
                if (r < hSize - 1)                 // bottom edge (x = hSize-2..0, y = vSize-1)
                    return new Point(hSize - 2 - r, vSize - 1);

                r -= (hSize - 1);                  // left edge (x = 0, y = vSize-2..1)
                return new Point(0, vSize - 2 - r);
            }

            do
            {
                p1 = RandomBorderPoint();
                p2 = RandomBorderPoint();
            } while (p1 == p2);
            DrawLine(p1, p2);
        }
        else if (testMethod == 2) //random corner
        {
            p1 = new Point((int)(rand.NextDouble() * (hSize - 4) + 2), (int)(rand.NextDouble() * (vSize - 4) + 2));

            do
            {
                p2 = new Point((int)(rand.NextDouble() * (hSize - 4) + 2), (int)(rand.NextDouble() * (vSize - 4) + 2));
            } while ((p2 - p1).R < 5);

            var selector = rand.NextDouble();
            //if (selector > .4)  //random mix with lines
            {
                Angle a;
                do
                {
                    p3 = new Point((int)(rand.NextDouble() * hSize), (int)(rand.NextDouble() * vSize));
                    a = Abs((p3 - p2).Theta - (p2 - p1).Theta);
                } while ((a < Angle.FromDegrees(40) || a > Angle.FromDegrees(140)) && (p3 - p2).R < 5);
                DrawLine(p2, p3);
            }
            DrawLine(p1, p2);
        }
        else if (testMethod == 3) //random counter-test
        {
            int numPts = (int)(rand.NextDouble() * 9);
            //int numPts = 2;

            boundaryArray[1, 1] = true;

            for (int i = 0; i < numPts; i++)
            {
                int x, y;
                do
                {
                    x = (int)(rand.NextDouble() * 3);
                    y = (int)(rand.NextDouble() * 3);
                    if (x == 1 && y == 1)
                    { }
                } while (boundaryArray[x, y] || (x == 1 && y == 1));
                boundaryArray[x, y] = true;
            }
        }
        SearchAndLearn();
        UpdateDialog();
    }

    void DrawLine(Point p1, Point p2)
    {
        if (boundaryArray == null)
        {
            return;
        }

        //create a line between p1 and pt in imageArray
        int x0 = (int)p1.X;
        int y0 = (int)p1.Y;
        int x1 = (int)p2.X;
        int y1 = (int)p2.Y;
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
        //hide any currently-displayed patches
        bool dontClearBoundaryImage = false;
        //foreach (var t in theUKS.UKSList) if (t.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(10)) dontClearBoundaryImage = true;

        foreach (var t in theUKS.UKSList) t.confidence = 0;
        foreach (var t in theUKS.UKSList) t.lastFiredTime = new DateTime(0);

        if (dontClearBoundaryImage || boundaryArray == null)
            return;

        // Debug.WriteLine("Boundary Array: " + boundaryArray);

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
        // Check if boundary array exists to prevent errors.
        if (boundaryArray == null)
        {
            return;
        }

        //Build the queryThing from the boundaryPoints Array


        Thing queryThing = new Thing() { Label = "theQuery" };
        for (int x = 0; x < boundaryArray.GetLength(0); x++)
        {
            for (int y = 0; y < boundaryArray.GetLength(1); y++)
            {
                if (boundaryArray[x, y]) //is this a boundary point?
                {
                    string attrName = $"Pt_{x:D2}_{y:D2}";
                    queryThing.AddRelationship(attrName, "hasBoundary");
                    //queryThing.AddRelationship(attrName, "count");
                }
            }
        }

        var resultl = LearnConnections(queryThing);
        theUKS.DeleteThing(queryThing);
        //queryThing = new Thing() { Label = "theQuery" };

        //foreach (var v in resultl)
        //{
        //    Relationship r = queryThing.AddRelationship(v.t, "hasBoundary");
        //    r.Weight = v.conf;
        //}
        //var result2 = LearnConnections(queryThing);
        //theUKS.DeleteThing(queryThing);
    }

    private List<(Thing t, float conf)> LearnConnections(Thing queryThing)
    {
        List<(Thing t, float conf)> matchOrig = new();
        List<(Thing t, float conf)> match = new();
        if (queryThing.Relationships.Count > 0)
        {
            matchOrig = theUKS.SearchForClosestMatch(queryThing, "Thing");


            matchOrig.RemoveAll(x => x.t.Label.StartsWith("theQuery"));
            matchOrig.RemoveAll(x => x.conf < 1.3);  //TODO this const changes with patch size  (patch 5 = 1.3f)
            //match.RemoveAll(x => x.conf < .9999f);  //TODO this const changes with patch size  (patch 5 = 1.3f)

            if (matchOrig.Count == 0)
            {
                theUKS.DeleteThing(queryThing);
                return matchOrig;
            }

            match = new(matchOrig);

            //mutual suppression
            for (int i = 0; i < match.Count; i++)
            {
                string s = match[i].t.Label;
                var label0 = s.Contains('_') ? s[..s.LastIndexOf('_')] : s;
                for (int j = i + 1; j < match.Count; j++)
                {
                    s = match[j].t.Label;
                    var label1 = s.Contains('_') ? s[..s.LastIndexOf('_')] : s;
                    if (label1 == label0)
                    {
                        match.RemoveAt(j);
                        j--;
                    }
                }
            }

            //adjust weights between layers
            foreach (var item in match)
            {
                AdjustWeights(item.t, queryThing);
                item.t.SetFired();
            }

            float etaPlus = 0.1f;    // LTP rate (co-active)
            float etaMinus = 0.1f;
            //adjust weights within layers
            foreach (var item in match)
            {
                foreach (Relationship r in item.t.Relationships.Where(x => x.relType.Label == "collinearWith"))
                {

                    if (match.Any(x => x.t == r.target))
                    {
                        r.Weight += etaPlus * (1f - r.Weight);
                        if (r.Weight > 1)
                            r.Weight = 1;
                    }
                    else
                    {
                        r.Weight += -etaMinus * r.Weight;
                        if (r.Weight < 0.001f)
                            r.source.RemoveRelationship(r);
                    }
                    r.Fire();
                }
            }

            //check for corners
            foreach (var item in match)
            {
                int targetFiredCount = 0;
                int lastX = -1; int lastY = -1;
                foreach (Relationship r in item.t.Relationships.OrderBy(x => x.target.Label).Where(x => x.relType.Label == "collinearWith"))
                {
                    string[] parts = r.target.Label.Split("_");
                    int curX = int.Parse(parts[1]);
                    int curY = int.Parse(parts[2]);
                    if (curX == lastX && curY == lastY) continue;  //do not cuplicate count on the same point
                    // if (r.target.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(10))
                    if (matchOrig.FindAll(x => x.t == r.target).Count > 0)
                    {
                        targetFiredCount++;
                        lastX = curX;
                        lastY = curY;
                    }
                }
                if (item.t.Label.Contains("03_04")) //BREAKPOINT
                { }
                if (targetFiredCount < 2)
                {
                    string[] parts = item.t.Label.Split("_");
                    string cornerLabel = $"corner_{parts[1]}_{parts[2]}";
                    Thing corner = theUKS.GetOrAddThing(cornerLabel);
                    corner.SetFired();
                }
            }

            //set up nearlyCollinearWith Relationships
            //foreach primary value (in match) get a list of all the others in matchOrig
            foreach (var item in match)
            {
                List<(Thing t, float conf)> patchesAtThisPixel = matchOrig.Where(x =>
                {
                    string[] parts1 = item.t.Label.Split("_");
                    string[] parts2 = x.t.Label.Split("_");
                    return parts1[1] == parts2[1] && parts1[2] == parts2[2];
                }).Select(x => (x.t, x.conf)).ToList();

                //in this list, the primary patch is [0], while [1] and [2] are candidate for nearlyColinearWith
                //this must be modified to work with 0, 1, or 2 results without indexing error
                if (patchesAtThisPixel.Count < 2) continue;
                Thing candidate1 = patchesAtThisPixel[1].t;
                float value1 = patchesAtThisPixel[1].conf;
                Thing candidate2 = null;
                float value2 = -1; //dummy value
                if (patchesAtThisPixel.Count > 2)
                {
                    candidate2 = patchesAtThisPixel[2].t;
                    value2 = patchesAtThisPixel[2].conf;
                }
                Thing primary = item.t;
                float primaryValue = item.conf;
                foreach (Relationship r in primary.Relationships.Where(x => x.relType.Label == "nearlyCollinearWith"))
                {
                    //increase weight to candidate1 if it is sufficiently lower in value than primary
                    if (r.target == candidate1)
                    {
                        //but not if the weights are nearly the same
                        if (primaryValue - value1 / primaryValue > 0.15f) //far enough in value
                        {
                            float deltaWeight = (primaryValue - value1) / primaryValue;
                            r.Weight += deltaWeight * 0.1f;
                        }
                    }
                    else if (r.target == candidate2)
                    {
                        if (primaryValue - value1 / primaryValue > 0.15f) //far enough in value
                        {
                            float deltaWeight = (primaryValue - value1) / primaryValue;
                            r.Weight += deltaWeight * 0.1f;
                        }
                    }
                    else //lower the weight to other nearlyCollinearWith patches
                    {
                        float deltaWeight = -0.005f;
                        r.Weight += deltaWeight;
                    }
                    //remove existing nearlyCollinearWith low weight relationships
                    if (r.Weight < 0.05f)
                    {
                        r.source.RemoveRelationship(r);
                    }
                }
            }

            void AdjustWeights(Thing patch, Thing inputPattern)
            {
                string[] nameFields = patch.Label.Split("_");
                int x = int.Parse(nameFields[1]);
                int y = int.Parse(nameFields[2]);
                string centerPtLabel = $"Pt_{x:D2}_{y:D2}";

                //we are adjusting weights of an already-set patch
                foreach (Relationship r in patch.Relationships)
                {
                    //only adjust hasBoundary relationships
                    if (r.reltype.Label != "hasBoundary") continue;
                    //do not adjust the center point
                    if (r.target.Label == centerPtLabel) continue;

                    //did the input point fire?
                    Relationship rFound = inputPattern.Relationships.FindFirst(x => x.target == r.target);

                    // targets: ON -> +1, OFF -> -0.5
                    //float tp = (rFound != null) ? r.maxWeight : -r.maxWeight / 2f;
                    float tp = (rFound != null) ? r.maxWeight * tpOnWeight : tpOff;
                    float eta = (rFound != null) ? etaOn : etaOff; // example: smaller step for OFF
                    r.Weight += eta * (tp - r.Weight);

                    // clamp to keep things well-behaved
                    if (r.Weight > r.maxWeight) r.Weight = r.maxWeight;
                    if (r.Weight < -1f) r.Weight = -1f;
                    //if (r.Weight < 0.0) r.source.RemoveRelationship(r);
                    if (r.Weight < 0.0) r.Weight = 0;
                }
            }
        }

        return match;
    }

    int count = 0;
    public void Show()
    {
        if (boundaryArray == null)
        {
            return;
        }
        ClearBoundaryArray();
        Thing t = theUKS.GetOrAddThing("patch");
        var patches = t.DescendentsList();
        if (count >= patches.Count)
            count = 0;

        while (patches[count].Relationships.Count < patchSize * patchSize)
        {
            count++;
            if (count >= patches.Count) count = 0;
        }
        patches[count].SetFired();
        count++;

        //this will form the "prune" function when implemented
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

