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
using System.Runtime.Intrinsics.Arm;
using System.Windows.Media;
using System.Xml;

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
        public HSLColor[,] imageArray;
        [XmlIgnore]
        public HoughTransform segmentFinder;
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

            HSLColor[,] imageArray;
            imageArray = GetImageArrayFromBitmapImage();
            //segY = 9;
            //imageArray = GenerateImage();
            boundaryArray = FindBoundaries(imageArray);

            segmentFinder = new(boundaryArray.GetLength(0), boundaryArray.GetLength(1));
            segmentFinder.Transform(boundaryArray);

            segments = segmentFinder.ExtractLineSegments();

            FindCorners(segments);

            FindOrphanSegmentEnds();

            FindOutlines();

            WriteBitmapToMentalModel();

            UpdateDialog();

        }

        private void WriteBitmapToMentalModel()
        {
            Thing environmentModel = theUKS.GetOrAddThing("Environment", "Thing");
            Thing environmentModelArray = theUKS.GetOrAddThing("envPointArray","Environment");
            environmentModel.SetFired();
            //TODO Make angular
            //TODO Make 0 center
            for (int x = 0; x < 25; x++)
                for (int y = 0; y < 25; y++)
                {
                    string name = $"mm{x},{y}";
                    Thing theEntry = theUKS.GetOrAddThing(name, environmentModelArray);
                    theEntry.V = GetAverageColor(x*4, y*4);
                }
        }
        private HSLColor GetAverageColor (int x, int y)
        {
            HSLColor retVal = new(1,0,0,0);
            int size = 2;
            for (int i = -size; i <= size; i++)
                for (int j = -size; j <= size; j++)
                {
                    if (x + i < 0)continue;
                    if (y + j < 0)continue;
                    if (x + i >= imageArray.GetLength(0)) continue;
                    if (y + j >= imageArray.GetLength(1)) continue;
                    retVal.hue += imageArray[x + i, y + j].hue;
                    retVal.luminance += imageArray[x + i, y + j].luminance;
                    retVal.saturation += imageArray[x + i, y + j].saturation;
                }
            retVal.hue /= 25;
            retVal.luminance /= 25;
            retVal.saturation /= 25;
            return retVal;
        }


        int segY = 3;

        private HSLColor[,] GenerateImage()
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

        private HSLColor[,] GetImageArrayFromBitmapImage()
        {
            imageArray = new HSLColor[(int)bitmap.Width, (int)bitmap.Height];
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
                        HSLColor pixelColor = new HSLColor(1, red, green, blue);
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
                        HSLColor pixelColor = new HSLColor(1, red, green, blue);
                        imageArray[i, j] = pixelColor;

                    }
                }
            }

            return imageArray;
        }

        private float[,] FindBoundaries(HSLColor[,] imageArray)
        {
            float[,] boundaryArray = new float[imageArray.GetLength(0), imageArray.GetLength(1)];

            float dx = 1;
            float dy = 0;
            int sx = 0;
            int sy = 0;
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                if (sy == 33)
                { }
                int[] bRay = new int[imageArray.GetLength(0)];
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                FindBoundariesInRay(bRay, rayThruImage);
                for (int i = 0; i < bRay.GetLength(0); i++)
                {
                    if (boundaryArray[i, sy] == 0)
                        boundaryArray[i, sy] = bRay[i];
                }
            }
            dx = 0;
            dy = 1;
            sy = 0;
            for (sx = 0; sx < imageArray.GetLength(0); sx++)
            {
                int[] bRay = new int[imageArray.GetLength(1)];
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                FindBoundariesInRay(bRay, rayThruImage);
                for (int i = 0; i < bRay.GetLength(0); i++)
                {
                    if (boundaryArray[sx, i] == 0)
                        boundaryArray[sx, i] = bRay[i];
                }
            }
            return boundaryArray;
        }

        private void FindBoundariesInRay(int[] bRay, List<HSLColor> rayThruImage)
        {
            //given a ray of color values through an image, find the boundaries
            //todo: filter out noisy areas in the ray
            float boundaryThreshold = 0.35f;

            int start = -1;
            for (int i = 0; i < rayThruImage.Count - 1; i++)
            {
                float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);
                if (diff != 0)
                { }
                if (diff < boundaryThreshold)
                {
                    //pixels are the same...could be the start of a boundary
                    start = i;
                }
                else if (i < rayThruImage.Count - 2 &&
                    PixelDifference(rayThruImage[i], rayThruImage[i + 2]) < boundaryThreshold)
                {
                    //there is a single-pixel blip
                    bRay[i + 1] = 1;
                    start = -1;
                }
                else if (i < rayThruImage.Count - 3 &&
                    PixelDifference(rayThruImage[i], rayThruImage[i + 3]) < boundaryThreshold)
                {
                    //there is a stwo-pixel blip
                    bRay[i + 2] = 1;
                    start = -1;
                }
                else
                if (start != -1 && diff > boundaryThreshold)
                {
                    //find the end of the boundary and enter it
                    for (int j = i + 1; j < rayThruImage.Count - 1; j++)
                    {
                        float diffEnd = PixelDifference(rayThruImage[j], rayThruImage[j + 1]);
                        if (diffEnd < boundaryThreshold)
                        {
                            bRay[(int)Round((start + j) / 2.0)] = 1;
                            i = j;
                            start = -1;
                            break;
                        }
                    }
                }
            }
        }

        List<HSLColor> LineThroughArray(float dx, float dy, int startX, int startY, HSLColor[,] imageArray)
        {
            List<HSLColor> retVal = new();
            float x = startX; float y = startY;

            while (x < imageArray.GetLength(0) && y < imageArray.GetLength(1))
            {
                HSLColor c = imageArray[(int)x, (int)y];
                if (c != null)
                    retVal.Add(c);
                x += dx;
                y += dy;
            }
            return retVal;
        }

        float PixelDifference(HSLColor c1, HSLColor c2)
        {
            float hueWeight = .05f;
            float satWeight = .2f;
            float lumWeight = 1.0f;
            float retVal = Abs(c1.hue - c2.hue) * hueWeight + Abs(c1.saturation - c2.saturation) * satWeight +
                Abs(c1.luminance - c2.luminance) * lumWeight;
            return retVal;
        }

        private void FindCorners(List<Segment> segmentsIn)
        {
            //first lengthen the segments
            List<Segment> segments = new List<Segment>();
            foreach (Segment s in segmentsIn)
                segments.Add(new Segment(s.P1, s.P2));
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                //try to extend each end of the segment until it runs out of boundary pixels
                while (boundaryArray[(int)Round(segment.P1.X), (int)Round(segment.P1.Y)] > .1)
                {
                    PointPlus p = Utils.ExtendSegment(segment.P1, segment.P2, 1, true);
                    if (p.X < 0 || p.Y < 0 || Round(p.X) >= boundaryArray.GetLength(0) || Round(p.Y) >= boundaryArray.GetLength(1)) break;
                    if (boundaryArray[(int)Round(p.X), (int)Round(p.Y)] > .1)
                        segment.P1 = p;
                    else
                        break;
                }
                while (boundaryArray[(int)Round(segment.P2.X), (int)Round(segment.P2.Y)] > .1)
                {
                    PointPlus p = Utils.ExtendSegment(segment.P1, segment.P2, 1, false);
                    if (p.X < 0 || p.Y < 0 || Round(p.X) >= boundaryArray.GetLength(0) || Round(p.Y) >= boundaryArray.GetLength(1)) break;
                    if (boundaryArray[(int)Round(p.X), (int)Round(p.Y)] > .1)
                        segment.P2 = p;
                    else
                        break;
                }
            }
            for (int i = 0; i < segments.Count; i++)
            {
                //now extend another 3 or more pixels, just for good measure
                float distToExtend = (float)Math.Max(4, segments[i].Length * 0.10);
                segments[i] = Utils.ExtendSegment(segments[i], distToExtend);
            }

            //Now, find the corners
            corners = new List<Corner>();

            segments = segments.OrderByDescending(x => x.Length).ToList();
            corners = new();
            for (int i = 0; i < segments.Count; i++)
            {
                Segment s1 = segments[i];
                for (int j = i; j < segments.Count; j++)
                {
                    Segment s2 = segments[j];
                    if (Utils.FindIntersection(s1, s2, out PointPlus intersection, out Angle angle))
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
            minAngle.Degrees = 5;
            //if (s1.Length < 20 || s2.Length < 20) minAngle.Degrees = 20;


            if (Abs(angle.Degrees) > minAngle.Degrees && 180 - Abs(angle.Degrees) > minAngle.Degrees)
            {
                Angle cornerOrientation = ((s1.Angle - s2.Angle) > 0) ? s1.Angle : s2.Angle + (s1.Angle + s2.Angle) / 2;
                Corner alreadyInList = corners.FindFirst(x => (x.location - intersection).R < 2); //allow things to be offset by a few pixels
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
                    sum += (outline[i].location.X - outline[(i + 1) % cnt].location.X) *
                        (outline[i].location.Y - outline[(i + 1) % cnt].location.Y);
                }
                if (sum > 0) outline.Reverse();

                //we now have an ordered, right-handed outline
                //let's add it to the UKS

                //TODO: here's where we'll have a loop for multiple outlines
                Thing currOutline = theUKS.GetOrAddThing("Outline*", "Outlines");
                foreach (Corner c in outline)
                {
                    //TODO: modify to reuse existing (shared) points
                    Thing corner = theUKS.GetOrAddThing("corner*", tCorners);
                    corner.V = c;
                    theUKS.AddStatement(currOutline, "has*", corner);
                }
            }
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
            //here we parse Corner objects out of the Xml stream
            //this should no lonber be necessary since we are no longer storing points and corners (these are transient)
            foreach (Thing t in theUKS.UKSList)
            {
                if (t.V is System.Xml.XmlNode[] nodes)
                {
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

        // called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {

        }

    }
}
