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
//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using UKS;
using System.Windows.Media;
using System.Windows;
using static System.Math;

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
        //public HoughTransform segmentFinder;
        public List<PointPlus> strokePoints = new();
        public List<PointPlus> boundaryPoints = new();

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
                return $"[x,y:({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")}) " +//A: {angle}] " +
                    $"prevPt:[({prevPt.X.ToString("0.0")},{prevPt.Y.ToString("0.0")})] " +
                    $"nextPt:[({nextPt.X.ToString("0.0")},{nextPt.Y.ToString("0.0")})]";
            }
        }
        public class Arc : Corner
        {
            //an arc is defined by three (non-collinear) points
            //prevPt and nextPt are the endpoints of the arc and pt is any thirde point somewhere on the arc
            public Arc()
            {
                curve = true;
            }
            public override Angle angle
            {
                get
                {
                    var cir = GetCircleFromThreePoints(pt, nextPt, prevPt);
                    Angle startAngle = (prevPt  - cir.center).Theta.Normalize();
                    Angle midAngle = (pt  - cir.center).Theta.Normalize();
                    Angle endAngle = (nextPt - cir.center).Theta.Normalize();

                    Angle a = Abs(startAngle - endAngle);
                    //if the midAngle is not between start and end angles, go the other way areound the arc
                    //TODO: handle other cases
                    if (midAngle > startAngle && midAngle > endAngle)
                        a = 2 * PI - a;
                                                              
                    return a;
                }
            }
            // Function to calculate the center and radius of the circle through three points
            public (PointPlus center, float radius) GetCircleFromThreePoints(PointPlus p1, PointPlus p2, PointPlus p3)
            {
                float x1 = p1.X, y1 = p1.Y;
                float x2 = p2.X, y2 = p2.Y;
                float x3 = p3.X, y3 = p3.Y;

                // Calculate the perpendicular bisectors of two segments
                float ma = (y2 - y1) / (x2 - x1);
                float mb = (y3 - y2) / (x3 - x2);

                // Calculate the center of the circle (intersection of the bisectors)
                float cx = (ma * mb * (y1 - y3) + mb * (x1 + x2) - ma * (x2 + x3)) / (2 * (mb - ma));
                float cy = -1 * (cx - (x1 + x2) / 2) / ma + (y1 + y2) / 2;

                PointPlus center = new PointPlus(cx, cy);

                // Calculate the radius of the circle
                float radius = (center - p1).R;

                return (center, radius);
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

            //strokePoints = FindStrokeeCentersFromBoundaryPoints(boundaryPoints);

            segments = new();
            corners = new();
            if (strokePoints.Count > boundaryPoints.Count / 4)
            {
                FindArcsAndSegments(strokePoints);
                FindCorners(ref segments);
                SaveSymbolToUKS();
            }
            else
            {
                segments = FindSegments(boundaryPoints);
                FindCorners(ref segments);
                FindOutlines();
            }

            WriteBitmapToMentalModel();

            UpdateDialog();
        }

        private void SaveSymbolToUKS()
        {
            int maxExtent = (int)strokePoints.Max(x => Math.Max(x.X, x.Y));
            Thing shapesParent = theUKS.GetOrAddThing("CurrentSymbol", "Visual");
            theUKS.DeleteAllChildren(shapesParent);
            Thing shapeParent = theUKS.GetOrAddThing("Symbol*", shapesParent);
            theUKS.GetOrAddThing("arc", "Visual");
            theUKS.GetOrAddThing("corner", "Visual");
            theUKS.GetOrAddThing("segment", "Visual");
            foreach (var corner in corners)
            {
                Thing item = theUKS.AddThing("Item*", shapeParent);
                if (corner is Arc a)
                {
                    item.AddRelationship("arc", "is");
                    int degrees = (int)a.angle.Degrees;
                    degrees = ((degrees + 5 * Math.Sign(degrees)) / 10) * 10;
                    item.AddRelationship(theUKS.GetOrAddThing("angle" + degrees,"Rotation"), "is");

                }
                else
                {
                    item.AddRelationship("corner", "is");
                    Segment s1 = new Segment(corner.prevPt, corner.pt);
                    Segment s2 = new Segment(corner.pt, corner.nextPt);
                    Thing item1 = theUKS.AddThing("Item*", shapeParent);
                    item1.AddRelationship("segment", "is");
                    //int degrees = (int)s1.Angle.Degrees;
                    //degrees = ((degrees + 5 * Math.Sign(degrees)) / 10) * 10;
                    //item1.AddRelationship("angle" + degrees, "is");
                    int length = (int)(s1.Length * 10 + 5) / maxExtent;
                    item1.AddRelationship("distance." + length, "is");

                    //Thing item2 = theUKS.AddThing("Item*", shapeParent);
                    //item2.AddRelationship("segment", "is");
                }
            }
        }

        public float scale = 1;
        public int offsetX = 0;
        public int offsetY = 0;

        public void LoadImageFileToPixelArray(string filePath)
        {
            using (System.Drawing.Bitmap bitmap2 = new(currentFilePath))
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


        float PixelDifference(Color c1, Color c2)
        {
            float retVal = 0;
            retVal += c1.R - c2.R;
            retVal += c1.G - c2.G;
            retVal += c1.B - c2.B;
            return retVal;
        }

        private class taggedSegment { public Segment s; public bool pt1Used; public bool pt2Used; }
        private void FindCorners(ref List<Segment> segmentsIn)
        {
            MergeSegments(segmentsIn);

            List<taggedSegment> taggedSegments = new();
            foreach (Segment s in segmentsIn)
                taggedSegments.Add(new taggedSegment() { s = s, pt1Used = false, pt2Used = false });


            //build a table of distances between each point and each other
            List<(int i, int j, float p1p1, float p1p2, float p2p1, float p2p2, float closest)> distances = new();
            for (int i = 0; i < taggedSegments.Count - 1; i++)
            {
                var s1 = taggedSegments[i];
                for (int j = i + 1; j < taggedSegments.Count; j++)
                {
                    if (i == j) continue;
                    var s2 = taggedSegments[j];
                    float p1p1 = (s1.s.P1 - s2.s.P1).R;
                    float p1p2 = (s1.s.P1 - s2.s.P2).R;
                    float p2p1 = (s1.s.P2 - s2.s.P1).R;
                    float p2p2 = (s1.s.P2 - s2.s.P2).R;
                    float closest = (float)new List<float> { p1p1, p1p2, p2p1, p2p2 }.Min();
                    distances.Add((i, j, p1p1, p1p2, p2p1, p2p2, closest));
                }
            }
            distances = distances.OrderBy(x => x.closest).ToList();

            foreach (var distance in distances)
            {
                if (distance.closest > 4.2) break; //give up when the distance is large
                var s1 = taggedSegments[distance.i];
                var s2 = taggedSegments[distance.j];
                if (distance.closest == distance.p1p1 && !s1.pt1Used && !s2.pt1Used)
                {
                    bool segmentsIntersect = Utils.LinesIntersect(s1.s, s2.s, out PointPlus intersection);
                    if (segmentsIntersect)
                    {
                        AddCornerToList(intersection, s1.s.P2, s2.s.P2);
                        UpdateCornerPoint(s1.s.P1, intersection);
                        UpdateCornerPoint(s2.s.P1, intersection);
                        s1.pt1Used = true;
                        s2.pt1Used = true;
                    }
                }
                if (distance.closest == distance.p1p2 && !s1.pt1Used && !s2.pt2Used)
                {
                    bool segmentsIntersect = Utils.LinesIntersect(s1.s, s2.s, out PointPlus intersection);
                    if (segmentsIntersect)
                    {
                        AddCornerToList(intersection, s1.s.P2, s2.s.P1);
                        UpdateCornerPoint(s1.s.P1, intersection);
                        UpdateCornerPoint(s2.s.P2, intersection);
                        s1.pt1Used = true;
                        s2.pt2Used = true;
                    }
                }
                if (distance.closest == distance.p2p1 && !s1.pt2Used && !s2.pt1Used)
                {
                    bool segmentsIntersect = Utils.LinesIntersect(s1.s, s2.s, out PointPlus intersection);
                    if (segmentsIntersect)
                    {
                        AddCornerToList(intersection, s1.s.P1, s2.s.P2);
                        UpdateCornerPoint(s1.s.P2, intersection);
                        UpdateCornerPoint(s2.s.P1, intersection);
                        s1.pt2Used = true;
                        s2.pt1Used = true;
                    }
                }
                if (distance.closest == distance.p2p2 && !s1.pt2Used && !s2.pt2Used)
                {
                    bool segmentsIntersect = Utils.LinesIntersect(s1.s, s2.s, out PointPlus intersection);
                    if (segmentsIntersect)
                    {
                        AddCornerToList(intersection, s1.s.P1, s2.s.P1);
                        UpdateCornerPoint(s1.s.P2, intersection);
                        UpdateCornerPoint(s2.s.P2, intersection);
                        s1.pt2Used = true;
                        s2.pt2Used = true;
                    }
                }
            }
            //find any orphans
            foreach (var segment in taggedSegments)
            {
                if (!segment.pt2Used)
                    AddCornerToList(segment.s.P2, segment.s.P1, segment.s.P1);
                if (!segment.pt1Used)
                    AddCornerToList(segment.s.P1, segment.s.P2, segment.s.P2);
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
                    for (int i = 0; i < cornerAvailable.Count; i++)
                    {
                        Corner next = cornerAvailable[i];
                        if (next.angle == 0) continue;
                        if (outline.Contains(next))
                            continue; //should never happen
                        if (curr.nextPt.Near(next.pt, 2) || curr.prevPt.Near(next.pt, 2))
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
                //add it to UKS
                for (int i = 1; i < outline.Count + 1; i++)
                {
                    Corner c = outline[i % outline.Count];

                    //let's update the angle
                    //PointPlus prev = outline[(i - 1) % outline.Count].pt;
                    //PointPlus next = outline[(i + 1) % outline.Count].pt;
                    //                   c.nextPt = next;
                    //                   c.prevPt = prev;


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
                        Color theColor = new() {A=A,R=R,G=G,B=B, };
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
}
