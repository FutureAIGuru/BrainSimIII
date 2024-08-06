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
using System.Windows.Media.Media3D;
using System.Security.Cryptography.Xml;

namespace BrainSimulator.Modules
{
    public partial class ModuleVision : ModuleBase
    {
        public string currentFilePath = "";
        public string previousFilePath = "";
        public BitmapImage bitmap = null;
        public List<Corner> corners;
        public List<Segment> segments;
        public Color[,] imageArray;
        public HoughTransform segmentFinder;
        public List<PointPlus> strokePoints = new();
        public List<PointPlus> boundaryPoints = new();

        public class Corner
        {
            public PointPlus pt;
            public Angle angle
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
            public PointPlus prevPt;
            public PointPlus nextPt;
            public override string ToString()
            {
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

            if (currentFilePath == previousFilePath) return;
            previousFilePath = currentFilePath;

            LoadImageFileToPixelArray(currentFilePath);

            FindBackgroundColor();

            FindBoundaries(imageArray);

            segmentFinder = new();// (imageArray.GetLength(0), imageArray.GetLength(1));
            //if (imageArray.GetLength(0) < 50)
            //    segmentFinder.Transform2(strokePoints);
            //else
            //    segmentFinder.Transform2(boundaryPoints);

            //FindArcs();

            segments = segmentFinder.FindSegments(boundaryPoints);

            FindCorners(ref segments);

            //FindOrphanSegmentEnds();

            //FindOutlines();

            //WriteBitmapToMentalModel();

            UpdateDialog();

        }

        public float scale = 1;
        public int offsetX = 0;
        public int offsetY = 0;

        public void LoadImageFileToPixelArray(string filePath)
        {
            using (System.Drawing.Bitmap bitmap2 = new(currentFilePath))
            {
                System.Drawing.Bitmap theBitmap = bitmap2;

                int bitmapSize = 50;
                if (bitmapSize > theBitmap.Width) bitmapSize = theBitmap.Width;
                //do not expand an image if it is smaller than the bitmap...it can introduce problems
                if (theBitmap.Width < bitmapSize) scale = (float)theBitmap.Width / bitmapSize;
                if (scale > theBitmap.Width / bitmapSize) scale = theBitmap.Width / bitmapSize;
                //limit the x&y offsets so the picture will be displayed
                float maxOffset = bitmapSize * scale - bitmapSize;
                if (offsetX > 0) offsetX = 0;
                if (offsetX < -maxOffset) offsetX = -(int)maxOffset;
                if (offsetY > 0) offsetY = 0;
                if (offsetY < -maxOffset) offsetY = -(int)maxOffset;
                System.Drawing.Bitmap resizedImage = new(bitmapSize, bitmapSize);
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                    graphics.DrawImage(bitmap2, offsetX, offsetY, bitmapSize * scale, bitmapSize * scale);
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
            List<Segment> segments = new List<Segment>();
            foreach (Segment s in segmentsIn)
                segments.Add(new Segment(s.P1, s.P2) { debugIndex = s.debugIndex, });

            //Now, find the corners
            corners = new List<Corner>();
            segments = segments.OrderByDescending(x => x.Length).ToList();

            corners = new();
            //for (int maxExt = -1;maxExt <0;maxExt++)
            for (int i = 0; i < segments.Count - 1; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    if (i == 5 && j == 7)
                    { }
                    Segment s1 = new(segments[i]);
                    Segment s2 = new(segments[j]);
                    bool segmentsIntersect = Utils.FindIntersection(s1, s2, out PointPlus intersection, out Angle angle);
                    float dist = (Utils.DistanceBetweenTwoSegments(s1, s2));

                    //if there is no intersection, extend the segments a few pixels because they may miss by a bit because of the boundary/segment algorithm
                    int maxExt = 2;
                    while (dist < 3 && !segmentsIntersect && maxExt-- >= 0)
                    {
                        s2 = Utils.ExtendSegment(s2, 1); //one pixel extension
                        s1 = Utils.ExtendSegment(s1, 1); //one pixel extension
                        dist = (Utils.DistanceBetweenTwoSegments(s1, s2));
                        segmentsIntersect = Utils.FindIntersection(s1, s2, out intersection, out angle);
                    }
                    if (segmentsIntersect)
                    {
                        //if the intersection is not at the end of a segment, ignore it
                        //float limit = 1.5f;
                        //if (((intersection - s1.P1).R > limit &&
                        //    (intersection - s1.P2).R > limit) ||
                        //    ((intersection - s2.P1).R > limit &&
                        //    (intersection - s2.P2).R > limit))
                        //{
                        //    continue;
                        //}

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
                        AddCornerToList(intersection, A, C);
                    }
                }
            }
        }

        private void AddCornerToList(PointPlus intersection, PointPlus prevPt, PointPlus nextPt)
        {
            //allow things to be offset by a few pixels
            Corner alreadyInList = corners.FindFirst(x =>
                (x.pt - intersection).R < 2 &&
                (((x.prevPt - prevPt).R < 2 && (x.nextPt - nextPt).R < 2) ||
                ((x.prevPt - nextPt).R < 2 && (x.nextPt - prevPt).R < 2)));
            if (alreadyInList == null)
                corners.Add(new Corner { pt = intersection, prevPt = prevPt, nextPt = nextPt });
            else { }
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
                    if ((s.P1 - c.pt).R < 5)
                        p1isOrphanEnd = false;
                    if ((s.P2 - c.pt).R < 5)
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
                    corners.Add(new Corner { pt = s.P1, prevPt = s.P2, nextPt = s.P2 });
                if (p2isOrphanEnd)
                    corners.Add(new Corner { pt = s.P2, prevPt = s.P1, nextPt = s.P1 });
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
                    //if (x >= boundaryArray.GetLength(0) || y >= boundaryArray.GetLength(1)) return false;
                    //if (boundaryArray[x, y] > 0) count++;
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
            corners = corners.OrderBy(x => x.pt.X).OrderBy(x => x.pt.Y).ToList();

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
                    for (int i = 0; i < corners.Count; i++)
                    {
                        Corner next = corners[i];
                        if (outline.Contains(next)) continue;
                        if (curr.pt.Near(next.prevPt, 5) || curr.pt.Near(next.nextPt, 5))
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
                    sum += (p2.pt.X - p1.pt.X) *
                        (p2.pt.Y + p1.pt.Y);
                }
                if (sum > 0)
                    outline.Reverse();
                //find the color at the center of the polygon
                List<Point> thePoints = new();
                foreach (Corner c in outline)
                    thePoints.Add(c.pt);
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
                for (int i = 1; i < outline.Count + 1; i++)
                {
                    Corner c = outline[i % outline.Count];

                    //let's update the angle
                    PointPlus prev = outline[(i - 1) % outline.Count].pt;
                    PointPlus next = outline[(i + 1) % outline.Count].pt;
                    c.nextPt = next;
                    c.prevPt = prev;


                    //TODO: modify to reuse existing (shared) points
                    //let's add it to the UKS
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
}
