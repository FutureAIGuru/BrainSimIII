//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using BrainSimulator.Modules.Vision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Diagnostics;
using static System.Math;
using UKS;
using System.Windows.Media;
using System.Windows;

namespace BrainSimulator.Modules
{
    public class ModuleVision : ModuleBase
    {
        public string currentFilePath = "";
        public string previousFilePath = "";
        [XmlIgnore]
        public BitmapImage bitmap = null;
        [XmlIgnore]
        public float[,] boundaryArray;
        [XmlIgnore]
        public List<Corner> corners;
        [XmlIgnore]
        public List<Segment> segments;
        [XmlIgnore]
        //public HSLColor[,] imageArray;
        public Color[,] imageArray;
        [XmlIgnore]
        public HoughTransform segmentFinder;
        public List<Point> boundaryPoints = new List<Point>();

        public class Corner
        {
            public PointPlus location;
            public Angle angle;
            public Angle orientation;
            public Segment s1;
            public Segment s2;
            public override string ToString()
            {
                return $"[x,y:({(int)Round(location.X)},{(int)Round(location.Y)}) A: {angle}] " +
                    $"s1:[({s1.P1.X},{s1.P1.Y}),({s1.P2.X},{s1.P2.Y})]] " +
                    $"s2:[({s2.P1.X},{s2.P1.Y}),({s2.P2.X},{s2.P2.Y})]]";
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

            if (currentFilePath == previousFilePath) return;
            previousFilePath = currentFilePath;

            bitmap = new BitmapImage();

            try
            {
                // Initialize the bitmap with the URI of the file
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(currentFilePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // To load the image fully at load time
                bitmap.EndInit();
                bitmap.Freeze(); // Make the BitmapImage thread safe
            }
            catch (Exception ex)
            {
                // Handle exceptions such as file not found or invalid file format
                Debug.WriteLine($"An error occurred: {ex.Message}");
                return;
            }


            if (bitmap.Width > 200 || bitmap.Height > 200)
            {
                Debug.WriteLine($"Image too large for current implementation");
                return;
            }

            Color[,] imageArray;
            imageArray = GetImageArrayFromBitmapImage();
            //segY = 9;
            //imageArray = GenerateImage();
            boundaryArray = FindBoundaries(imageArray);

            segmentFinder = new(boundaryArray.GetLength(0), boundaryArray.GetLength(1));
            //segmentFinder.Transform(boundaryArray);
            segmentFinder.Transform2(boundaryPoints);
            segmentFinder.FindArcs();
            segments = segmentFinder.FindSegments();
            //segmentFinder.FindMaxima();

            //segments = segmentFinder.ExtractLineSegments();

            FindCorners(ref segments);

            FindOrphanSegmentEnds();

            FindOutlines();

            WriteBitmapToMentalModel();

            UpdateDialog();

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
            //HSLColor retVal = new(1, 0, 0, 0);
            int size = 2;
            for (int i = -size; i <= size; i++)
                for (int j = -size; j <= size; j++)
                {
                    if (x + i < 0) continue;
                    if (y + j < 0) continue;
                    if (x + i >= imageArray.GetLength(0)) continue;
                    if (y + j >= imageArray.GetLength(1)) continue;
                    //retVal.hue += imageArray[x + i, y + j].hue;
                    //retVal.luminance += imageArray[x + i, y + j].luminance;
                    //retVal.saturation += imageArray[x + i, y + j].saturation;
                    retVal.R += imageArray[x + i, y + j].R;
                    retVal.G += imageArray[x + i, y + j].G;
                    retVal.B += imageArray[x + i, y + j].B;
                }
            retVal.R /= 25;
            retVal.R /= 25;
            retVal.R /= 25;
            return retVal;
        }


        int segY = 3;

        /*
         * private HSLColor[,] GenerateImage()
                {
                    HSLColor white = new HSLColor(0xff, 0xff, 0xff, 0xff);
                    imageArray = new HSLColor[100, 100];
                    for (int x = 0; x < 100; x++)
                        for (int y = 0; y < 100; y++)
                            imageArray[x, y] = white;

                    SetSingleSegment(imageArray, new PointPlus(10, 20f), new PointPlus(22, (float)segY));
                    segY++;
                    if (segY == 96) segY = 1;
                    return imageArray;
                }

                private void SetSingleSegment(HSLColor[,] imageArray, PointPlus start, PointPlus end)
                {
                    HSLColor theColor = new HSLColor(Colors.Red);

                    PointPlus curPos = new(start);

                    float dx = end.X - start.X;
                    float dy = end.Y - start.Y;

                    if (Abs(dx) > Abs(dy))
                    {
                        //step out in the X direction
                        PointPlus step = new PointPlus((dx > 0) ? 1 : -1, dy / Abs(dx));
                        for (int x = 0; x <= Abs(dx); x++)
                        {
                            imageArray[(int)Round(curPos.X), (int)Round(curPos.Y)] = theColor;
                            curPos += step;
                        }
                    }
                    else
                    {
                        //step out in the Y direction
                        PointPlus step = new PointPlus(dx / Abs(dy), (dy > 0) ? 1f : -1f);
                        for (int y = 0; y <= Abs(dy); y++)
                        {
                            imageArray[(int)Round(curPos.X), (int)Round(curPos.Y)] = theColor;
                            curPos += step;
                        }
                    }

                    return;
                }
        */
        private Color[,] GetImageArrayFromBitmapImage()
        {
            int height = (int)bitmap.Height;
            int width = (int)bitmap.Width;
            if (height > bitmap.PixelHeight) height = (int)bitmap.PixelHeight;
            if (width > bitmap.PixelWidth) width = (int)bitmap.PixelWidth;
            imageArray = new Color[width, height];
            //imageArray = new Color[(int)bitmap.PixelWidth, (int)bitmap.PixelHeight];
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
                            green = pixelBuffer[index + 1];
                            red = pixelBuffer[index + 2];
                            alpha = pixelBuffer[index + 3];
                        }
                        Color pixelColor = Color.FromArgb(1, red, green, blue);
                        imageArray[i, j] = pixelColor;

                    }
                }
            }

            return imageArray;
        }

        private float[,] FindBoundaries(Color[,] imageArray)
        {
            float[,] boundaryArray = new float[imageArray.GetLength(0), imageArray.GetLength(1)];
            boundaryPoints.Clear();
            bool horizScan = true;
            bool vertScan = true;

            float dx = 1;
            float dy = 0;
            int sx = 0;
            int sy = 0;
            if (horizScan)
            {
                for (sy = 0; sy < imageArray.GetLength(1); sy++)
                {
                    int[] bRay = new int[imageArray.GetLength(0)];
                    var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                    FindBoundariesInRay(bRay, rayThruImage, -1, sy);
                }
            }
            if (vertScan)
            {
                dx = 0;
                dy = 1;
                sy = 0;
                for (sx = 0; sx < imageArray.GetLength(0); sx++)
                {
                    int[] bRay = new int[imageArray.GetLength(1)];
                    var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                    FindBoundariesInRay(bRay, rayThruImage, sx, -1);
                }
            }
            return boundaryArray;
        }

        private void FindBoundariesInRay(int[] bRay, List<Color> rayThruImage, int x, int y)
        {
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
                                if (y == 35)
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
                    if (x == -1)
                        boundaryPoints.Add(new Point(boundaryPos, y));
                    else
                        boundaryPoints.Add(new Point(x, boundaryPos));
                }
            }
        }

        List<Color> LineThroughArray(float dx, float dy, int startX, int startY, Color[,] imageArray)
        {
            List<Color> retVal = new();
            float x = startX; float y = startY;

            while (x < imageArray.GetLength(0) && y < imageArray.GetLength(1))
            {
                Color c = imageArray[(int)x, (int)y];
                if (c != null)
                    retVal.Add(c);
                x += dx;
                y += dy;
            }
            return retVal;
        }

        /*        float PixelDifference(HSLColor c1, HSLColor c2)
                {
                    float hueWeight = .05f;
                    float satWeight = 1f;
                    float lumWeight = 1f;
                    float retVal = Abs(c1.hue - c2.hue) * hueWeight + Abs(c1.saturation - c2.saturation) * satWeight +
                        Abs(c1.luminance - c2.luminance) * lumWeight;
                    return retVal;
                }
        */
        float PixelDifference(Color c1, Color c2)
        {
            float retVal = 0;
            retVal += c1.R - c2.R;
            retVal += c1.G - c2.G;
            retVal += c1.B - c2.B;
            return retVal;
        }
        private void FindCorners(ref List<Segment> segmentsIn)
        {
            //first lengthen the segments
            List<Segment> segments = new List<Segment>();
            foreach (Segment s in segmentsIn)
                segments.Add(new Segment(s.P1, s.P2) { theColor = s.theColor, });

            segmentsIn = segments;
            //Now, find the corners
            corners = new List<Corner>();

            segments = segments.OrderByDescending(x => x.Length).ToList();
            corners = new();
            for (int i = 0; i < segments.Count - 1; i++)
            {
                Segment s1 = new(segments[i]);
                for (int j = i + 1; j < segments.Count; j++)
                {
                    Segment s2 = new(segments[j]);
                    bool segmentsIntersect = Utils.FindIntersection(s1, s2, out PointPlus intersection, out Angle angle);
                    float dist = (Utils.DistanceBetweenTwoSegments(s1, s2));
                    while (dist < 4 && !segmentsIntersect)
                    {
                        s2 = Utils.ExtendSegment(s2, 1); //one pixel extension
                        s1 = Utils.ExtendSegment(s1, 1); //one pixel extension
                        dist = (Utils.DistanceBetweenTwoSegments(s1, s2));
                        segmentsIntersect = Utils.FindIntersection(s1, s2, out intersection, out angle);
                    }
                    if (segmentsIntersect)
                    {
                        //find the angle properly
                        PointPlus A, B, C;
                        B = intersection;
                        if ((s1.P1 - intersection).R > (s1.P2 - intersection).R)
                            A = s1.P1;
                        else
                            A = s1.P2;
                        if ((s2.P1 - intersection).R > (s2.P2 - intersection).R)
                            C = s2.P1;
                        else
                            C = s2.P2;
                        Segment sA = new() { P1 = A, P2 = B };
                        Segment sB = new() { P1 = C, P2 = B };
                        angle = Abs(sA.Angle - sB.Angle);
                        AddCornerToList(s1, s2, intersection, angle);
                    }
                }
            }
        }

        private void AddCornerToList(Segment s1, Segment s2, PointPlus intersection, Angle angle)
        {
            //ignore angles whih are nearly straight
            //if the segments are short, the angle between them must be larger
            if (angle.Degrees > 180)
                angle.Degrees = 360 - angle.Degrees;
            Angle minAngle = new Angle();
            minAngle.Degrees = 1;
            //if (s1.Length < 20 || s2.Length < 20) minAngle.Degrees = 20;


            if (Abs(angle.Degrees) > minAngle.Degrees && 180 - Abs(angle.Degrees) > minAngle.Degrees)
            {
                Angle cornerOrientation = ((s1.Angle - s2.Angle) > 0) ? s1.Angle : s2.Angle + (s1.Angle + s2.Angle) / 2;
                Corner alreadyInList = corners.FindFirst(x => (x.location - intersection).R < 1.5); //allow things to be offset by a few pixels
                if (alreadyInList == null)
                    corners.Add(new Corner { location = intersection, angle = Abs(angle), orientation = cornerOrientation, s1 = s1, s2 = s2 });
            }
            else
            { }
        }

        void FindOrphanSegmentEnds()
        {
            foreach (Segment s in segments)
            {
                bool p1isOrphanEnd = true;
                bool p2isOrphanEnd = true;
                //is either endpoint of the segment near an existing corner?
                //This is because segments may intersect (corner) at their middles
                foreach (Corner c in corners)
                {
                    if ((s.P1 - c.location).R < 5)
                        p1isOrphanEnd = false;
                    if ((s.P2 - c.location).R < 5)
                        p2isOrphanEnd = false;
                }
                //is either endpoint near annother segment?
                foreach (Segment s1 in segments)
                {
                    if (s1 == s) continue;
                    if (s1.Length > 3)
                    {
                        float d1 = Utils.DistancePointToSegment(s1, s.P1);
                        float d2 = Utils.DistancePointToSegment(s1, s.P2);
                        if (d1 < 4)
                        {
                            if (!IsEndPoint((int)s.P1.X, (int)s.P1.Y))
                                p1isOrphanEnd = false;
                        }
                        if (d2 < 4)
                        {
                            if (!IsEndPoint((int)s.P2.X, (int)s.P2.Y))
                                p2isOrphanEnd = false;
                        }
                    }
                    if (!IsEndPoint((int)s.P1.X, (int)s.P1.Y))
                        p1isOrphanEnd = false;
                    if (!IsEndPoint((int)s.P2.X, (int)s.P2.Y))
                        p2isOrphanEnd = false;
                }
                if (p1isOrphanEnd)
                    corners.Add(new Corner { location = s.P1, angle = 0, s1 = s, s2 = s });
                if (p2isOrphanEnd)
                    corners.Add(new Corner { location = s.P2, angle = 0, s1 = s, s2 = s });
            }
        }

        //this checks the boundary array to see if a point is an orphan
        bool IsEndPoint(int sx, int sy)
        {
            int distanceToCheck = 1;
            int count = 0;
            for (int dx = -distanceToCheck; dx <= distanceToCheck; dx++)
                for (int dy = -distanceToCheck; dy <= distanceToCheck; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = sx + dx;
                    int y = sy + dy;
                    if (x < 0 || y < 0) return false;
                    if (x >= boundaryArray.GetLength(0) || y >= boundaryArray.GetLength(1)) return false;
                    if (boundaryArray[x, y] > 0) count++;
                    if (count > 1) return false;
                }
            return true;
        }

        //trace around the outlines to get the order of corners and relative distances
        private void FindOutlines()
        {
            //set up the UKS structure for outlines
            GetUKS();
            if (theUKS == null) return;
            theUKS.GetOrAddThing("Sense", "Thing");
            theUKS.GetOrAddThing("Visual", "Sense");
            Thing outlines = theUKS.GetOrAddThing("Outline", "Visual");
            Thing tCorners = theUKS.GetOrAddThing("Corner", "Visual");
            theUKS.DeleteAllChildren(outlines);
            theUKS.DeleteAllChildren(tCorners);

            if (corners.Count == 0) return;

            //for convenience in debugging
            corners = corners.OrderBy(x => x.location.X).OrderBy(x => x.location.Y).ToList();

            //perhaps there are multiple shapes?
            List<Corner> cornerAvailable = new List<Corner>();
            for (int i = 0; i < corners.Count; i++)
                cornerAvailable.Add(corners[i]);

            while (cornerAvailable.Count > 0)
            {
                Corner curr = cornerAvailable[0];
                List<Corner> outline = new();
                bool outlineClosed = false;
                Corner start = curr;
                outline.Add(curr);
                cornerAvailable.Remove(curr);
                while (!outlineClosed)
                {
                    foreach (Corner next in corners)
                    {
                        if (outline.Contains(next)) continue;
                        if (next.s1 == curr.s1 || next.s1 == curr.s2 || next.s2 == curr.s1 || next.s2 == curr.s2)
                        {
                            outline.Add(next);
                            cornerAvailable.Remove(next);
                            curr = next;
                            goto pointAdded;
                        }
                    }
                    outlineClosed = true; //no more points to add 
                pointAdded: continue;
                }

                //make this a right-handed list of points  
                double sum = 0;
                int cnt = outline.Count;
                for (int i = 0; i < outline.Count; i++)
                {
                    Corner p1 = outline[i];
                    Corner p2 = outline[(i + 1) % cnt];
                    sum += (p2.location.X - p1.location.X) *
                        (p2.location.Y + p1.location.Y);
                }
                if (sum > 0)
                    outline.Reverse();
                //find the color at the center of the polygon
                List<Point> thePoints = new();
                foreach (Corner c in outline)
                    thePoints.Add(c.location);
                Point centroid = Utils.GetCentroid(thePoints);

                Thing currOutline = theUKS.GetOrAddThing("Outline*", "Outlines");

                //get the color (the centroid might be outside the image)
                try
                {
                    //HSLColor theCenterColor = imageArray[(int)centroid.X, (int)centroid.Y];
                    Color theCenterColor = imageArray[(int)centroid.X, (int)centroid.Y];
                    Thing theColor = GetOrAddColor(theCenterColor);
                    currOutline.SetAttribute(theColor);
                }
                catch (Exception e) { }
                //we now have an ordered, right-handed outline
                //let's add it to the UKS
                foreach (Corner c in outline)
                {
                    //TODO: modify to reuse existing (shared) points
                    Thing corner = theUKS.GetOrAddThing("corner*", tCorners);
                    corner.V = c;
                    theUKS.AddStatement(currOutline, "has*", corner);
                }
            }
        }

        Thing GetOrAddColor(Color color)
        {
            foreach (Thing t in theUKS.Labeled("Color").Children)
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
                        c.location = new PointPlus { X = x, Y = y, Conf = conf, };
                        //get the angle node
                        float theta = float.Parse(nodes[2].FirstChild.InnerText);
                        c.angle = Angle.FromDegrees(theta);
                        //get the orientation node
                        float theta1 = float.Parse(nodes[3].FirstChild.InnerText);
                        c.orientation = Angle.FromDegrees(theta1);
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
}
