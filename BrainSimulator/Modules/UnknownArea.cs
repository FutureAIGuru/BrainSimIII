//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    // This class represents a simple 2D polar, because Windows has two Point classes
    // that don't mix well in saved XML files. So we roll our own...
    public class PointTwoD
    {
        public double X { get; set; }
        public double Y { get; set; }
        public PointTwoD()
        {
            X = 0;
            Y = 0;
        }
        public PointTwoD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y;
        }

        public bool NearBy(PointTwoD other)
        {
            if (other == null) return false;
            if (Math.Abs((double)X - (double)other.X) <= 3.0 && Math.Abs((double)Y - (double)other.Y) <= 3.0)
            {
                // Debug.WriteLine("NEARBY: " + X + " " + other.X + " " + Y + " " + other.Y);
                return true;
            }
            // Debug.WriteLine("FARAWAY: " + X + " " + other.X + " " + Y + " " + other.Y);
            return false;
        }
    }

    public class Polar
    {
        public int CentralX { get; set; } = -1;
        public int CentralY { get; set; } = -1;
        public int OutsideX { get; set; } = -1;
        public int OutsideY { get; set; } = -1;
        public double ScaleFactor { get; set; } = 1;

        public Polar()
        {
            // Empty for instantiation from XML...
        }

        public Polar(int centralX, int centralY, Angle polar, int outsideX, int outsideY)
        {
            CentralX = centralX;
            CentralY = centralY;
            OutsideX = outsideX;
            OutsideY = outsideY;
        }

        public double Length()
        {
            if (CentralX == -1 || CentralY == -1 || OutsideX == -1 || OutsideY == -1) return double.NaN;
            double deltaX = Math.Abs((double)CentralX - (double)OutsideX);
            double deltaY = Math.Abs((double)CentralY - (double)OutsideY);
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY) / ScaleFactor;
        }
    }

    public class UnknownArea
    {
        public static int FieldOfVision = 52;
        public static float AspectRatio = 16/9;
        public float ImageWidth { get; set; }
        public float ImageHeight { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }

        public Mat DetailImage { get; set; }
        [XmlIgnore]
        public System.Drawing.Rectangle ROI { get; set; }

        public double CameraPan { get; set; }
        public double CameraTilt { get; set; }


        public double ScaleFactor { get; set; }

        public HSLColor AvgColor { get; set; }
        // public double GreatestLength { get; set; }
        // public Angle Orientation { get; set; }

        public PointTwoD Centroid { get; set; } = new();
        public Point3DPlus AngularCenter { get; set; } = new();
        public Point3DPlus Lowest { get; set; } = new();
        public Point3DPlus Highest { get; set; } = new();
        public Point3DPlus LeftMost { get; set; } = new();
        public Point3DPlus RightMost { get; set; } = new();
        public Angle AngleDifference { get; set; } = new();
        // public Guid ID { get; set; } = Guid.NewGuid();

        public double Area { get; set; }

        public char TurnToSee { get; set; } = '_';
        // public Angle SeenFrom { get; set; }

        // public int OffScreenCounter { get; set; }
        public string TrackID { get; set; }
        public List<SegmentTwoD> AreaSegments { get; set; } = new List<SegmentTwoD>();
        public List<CornerTwoD> AreaCorners { get; set; } = new List<CornerTwoD>();
        public List<Polar> PolarScores { get; set; } = new();

        // public double PolarMinLength { get; set; } = Double.MaxValue;
        // public double PolarAvgLength { get; set; }
        public double PolarMaxLength { get; set; }
        // public double PolarAvgPercent { get => PolarAvgLength / PolarMaxLength; }
        // public double PolarMinPercent { get => PolarMinLength / PolarMaxLength; }
        // public double PolarMinScore { get; set; }
        // public double PolarAvgScore { get; set; }
        // public double PolarMaxScore { get; set; }
        public bool isOuterContour;

        // Not referenced, but is needed to store UnknownArea in XML
        public UnknownArea()
        {
            AreaSegments = new();
            AreaCorners = new();
            PolarScores = new();
            TrackID = Utils.NewTrackID();
        }

        public System.Windows.Point GetDrawingPoint(double X, double Y)
        {
            double newX = (X + Centroid.X + XOffset * 2 + CameraPan) / 2;
            double newY = (Y + Centroid.Y + YOffset * 2 + CameraTilt) / 2;
            return new System.Windows.Point((int)newX, (int)newY);
        }

        // This constructor takes an OpenCV contour and transforms it into an UnknownArea object.
        public UnknownArea(Bitmap inputImage,
                           VectorOfPoint contour,
                           Angle cameraPan,
                           Angle cameraTilt,
                           int xOffset = 0,
                           int yOffset = 0,
                           HSLColor color = null)
        {
            if (contour == null || inputImage == null) return;

            TrackID = Utils.NewTrackID();

            AreaSegments.Clear();
            AreaCorners.Clear();
            PolarScores.Clear();

            ImageWidth = inputImage.Width;
            ImageHeight = inputImage.Height;
            AspectRatio = ImageWidth / ImageHeight;

            XOffset = xOffset;
            YOffset = yOffset;
            CameraPan = cameraPan;
            CameraTilt = cameraTilt;
            // SeenFrom = Sallie.BodyAngle + Sallie.CameraPan + Angle.FromDegrees(180);
            ROI = CvInvoke.BoundingRectangle(contour);
            CalculateCentroid(contour);
            CalculatePolarScores(contour);
            CalculateAngularCenter(cameraPan, cameraTilt);
            CalculateOrSetColor(inputImage, color);
            // CalculateGreatestLength(contour);
            MarkAreaAsOnEdge();
            BuildSegmentList(contour);
            BuildCornerList();
            CalculateBounds(cameraPan, cameraTilt);
            CalculateArea();
            // CalculateOrientation();
            // DbgWrite();
        }

        public void SetDetailImage(Mat image, VectorOfPoint contour)
        {
            VectorOfPoint approxContour = new();
            CvInvoke.ApproxPolyDP(contour, approxContour, 1.5, true);
            ROI = CvInvoke.BoundingRectangle(contour);
            ROI.Inflate(7, 7);
            Math.Clamp(XOffset, 0, image.Cols - 1);
            Math.Clamp(YOffset, 0, image.Rows - 1);
            Math.Clamp(ROI.Width, 0, image.Cols - XOffset - 1);
            Math.Clamp(ROI.Height, 0, image.Rows - YOffset - 1);
            DetailImage = new Mat(image, ROI);
        }

        public void MarkAreaAsOnEdge()
        {
            // if (TurnToSee != '_') return;  // already marked...

            TurnToSee = ' ';
            foreach (Polar polar in PolarScores)
            {
                System.Drawing.Point c = new(polar.OutsideX, polar.OutsideY);
                if (!(c.X == 0 && c.Y == 0))
                {
                    if (c.X < 3)
                    {
                        TurnToSee = 'L';
                    }
                    if (c.X >= ImageWidth - 3)
                    {
                        TurnToSee = 'R';
                    }
                    if (c.Y < 3)
                    {
                        TurnToSee = 'U';
                    }
                    if (c.Y >= ImageHeight - 3)
                    {
                        TurnToSee = 'D';
                    }
                }
            }
        }


        public void CalculateCentroid(VectorOfPoint contour)
        {
            if (contour == null) return;
            if (TurnToSee == '_') MarkAreaAsOnEdge();
            if (TurnToSee == ' ')
            {
                // For non-partials using moments to calculate Centroid will do...
                Moments moments = CvInvoke.Moments(contour, true);
                Centroid.X = (int)(moments.M10 / moments.M00) + XOffset;
                Centroid.Y = (int)(moments.M01 / moments.M00) + YOffset;
            }
            else
            {
                // For partials we will use another method
                CircleF circle = CvInvoke.MinEnclosingCircle(contour);
                Centroid.X = (int)circle.Center.X + XOffset;
                Centroid.Y = (int)circle.Center.Y + YOffset;
            }
            Centroid.X = Math.Clamp(Centroid.X, 0, (int)ImageWidth - 1);
            Centroid.Y = Math.Clamp(Centroid.Y, 0, (int)ImageHeight - 1);
        }

        void CalculateAngularCenter(Angle cameraPan, Angle cameraTilt)
        {
            float centerX = ImageWidth / 2;
            float centerY = ImageHeight / 2;

            AngularCenter.Theta = (double)(centerX - Centroid.X) / (double)ImageWidth * Angle.FromDegrees(FieldOfVision);
            Angle v = Angle.FromDegrees((float)(FieldOfVision * ((double)ImageHeight / (double)ImageWidth)));
            AngularCenter.Phi = (double)(centerY - Centroid.Y) / (double)ImageHeight * v;
            AngularCenter.R = 0;

            AngularCenter.Theta += cameraPan;
            AngularCenter.Phi += cameraTilt;
        }

        void CalculateBounds(Angle cameraPan, Angle CameraTilt)
        {
            float lowest = float.MinValue;
            float highest = float.MaxValue;
            float leftest = float.MaxValue;
            float rightest = float.MinValue;

            PointTwoD best0 = new();
            PointTwoD best1 = new();
            PointTwoD best2 = new();
            PointTwoD best3 = new();

            foreach (var corner in AreaCorners)
            {
                if ((float)corner.loc.Y > lowest)
                {
                    lowest = (float)corner.loc.Y;
                    best0 = corner.loc;
                }
                if ((float)corner.loc.Y < highest)
                {
                    highest = (float)corner.loc.Y;
                    best1 = corner.loc;
                }
                if ((float)corner.loc.X < leftest)
                {
                    leftest = (float)corner.loc.X;
                    best2 = corner.loc;
                }
                if ((float)corner.loc.X > rightest)
                {
                    rightest = (float)corner.loc.X;
                    best3 = corner.loc;
                }
            }
            float centerX = ImageWidth / 2;
            float centerY = ImageHeight / 2;

            Lowest = new Point3DPlus(30, 0, 0f);
            Angle v = Angle.FromDegrees((float)(FieldOfVision * ((double)ImageHeight / (double)ImageWidth)));
            Lowest.Phi = (double)(centerY - (best0.Y + Centroid.Y)) / (double)ImageHeight * v;
            //the following is commented until CameraTilt is made more accurate
            Lowest.Phi += CameraTilt;

            Highest = new Point3DPlus(30, 0, 0f);
            v = Angle.FromDegrees((float)(FieldOfVision * ((double)ImageHeight / (double)ImageWidth)));
            Highest.Phi = (double)(centerY - (best1.Y + Centroid.Y)) / (double)ImageHeight * v;
            //the following is commented until CameraTilt is made more accurate
            Highest.Phi += CameraTilt;

            LeftMost = new Point3DPlus(30, 0, 0f);
            v = Angle.FromDegrees(FieldOfVision);
            LeftMost.Theta = (double)(centerX - (best2.X + Centroid.X)) / (double)ImageWidth * v;
            LeftMost.Theta += cameraPan;

            RightMost = new Point3DPlus(30, 0, 0f);
            v = Angle.FromDegrees(FieldOfVision);
            RightMost.Theta = (double)(centerX - (best3.X + Centroid.X)) / (double)ImageWidth * v;
            RightMost.Theta += cameraPan;

            AngleDifference = Abs(LeftMost.Theta - RightMost.Theta);
        }

        int numberOfPolars = 90;

        public void CalculatePolarScores(VectorOfPoint contour)
        {
            Angle polarAngle = 2 * Math.PI / numberOfPolars;
            if (contour == null) return;
            System.Drawing.Point[] corners = contour.ToArray();
            System.Windows.Point centralPoint = new((int)Centroid.X, (int)Centroid.Y);
            System.Windows.Point intersection;

            //EXPERIMENT
            List<float> lengths = new();

            for (Angle a = 0; a < 2 * Math.PI; a += polarAngle)
            {
                System.Windows.Point extension = new((int)(centralPoint.X + Math.Sin(a) * 5000),
                                                     (int)(centralPoint.Y + Math.Cos(a) * 5000));
                System.Windows.Point prevPoint = new(corners[corners.Length - 1].X, corners[corners.Length - 1].Y);
                for (int i = 0; i < corners.Length; i++)
                {
                    System.Windows.Point currentPoint = new(corners[i].X, corners[i].Y);
                    if (Utils.FindIntersection(centralPoint, extension, prevPoint, currentPoint, out intersection))
                    {
                        break;
                    }
                    prevPoint = currentPoint;
                }
                Polar newPolar = new Polar((int)Centroid.X, (int)Centroid.Y, a, (int)intersection.X, (int)intersection.Y);
                PolarScores.Add(newPolar);
                double newLength = newPolar.Length();
                lengths.Add((float)newLength); ;
                PolarMaxLength = Math.Max(newLength, PolarMaxLength);
            }
            NormalizePolars();

            //EXPERIMENT
            //TODO: the key is to find arcs BEFORE searching for corners
            List<float> deltas = new();
            deltas.Add(lengths[0] - lengths[lengths.Count - 1]);
            for (int j = 1; j < lengths.Count; j++)
            {
                deltas.Add(lengths[j] - lengths[j - 1]);
            }

        }

        public void NormalizePolars()
        {
            ScaleFactor = PolarMaxLength;
            foreach (Polar polar in PolarScores)
            {
                polar.ScaleFactor = PolarMaxLength;
            }
        }

        public void DrawPolars(Mat img)
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)(Centroid.X - 1), (int)(Centroid.Y - 1), 3, 3);
            CvInvoke.Rectangle(img, rect, new Bgr(0, 255, 255).MCvScalar, 4);
            foreach (Polar polar in PolarScores)
            {
                rect = new System.Drawing.Rectangle((int)(polar.OutsideX - 1), (int)(polar.OutsideY - 1), 3, 3);
                CvInvoke.Rectangle(img, rect, new Bgr(0, 255, 255).MCvScalar, 5);
            }
        }

        public void CalculateOrSetColor(Bitmap inputImage, HSLColor hslColor = null)
        {
            int numSamplesPerAxis = 10;
            if (inputImage == null) return;
            if (hslColor != null)
            {
                AvgColor = hslColor;
                return;
            }

            List<HSLColor> hslcolors = new();
            float hue = 0;
            float saturation = 0;
            float luminance = 0;
            int count = 0;

            int minX = PolarScores.Select(p => p.OutsideX).Min();
            int maxX = PolarScores.Select(p => p.OutsideX).Max();
            int minY = PolarScores.Select(p => p.OutsideY).Min();
            int maxY = PolarScores.Select(p => p.OutsideY).Max();

            int objectWidth = maxX - minX;
            int objectHeight = maxY - minY;
            int startX = (int)Math.Clamp(minX, 0, ImageWidth - 1);
            int startY = (int)Math.Clamp(minY, 0, ImageHeight - 1);
            int endX = (int)Math.Clamp(startX + objectWidth, 0, ImageWidth);
            int endY = (int)Math.Clamp(startY + objectHeight, 0, ImageHeight);
            int xStep = objectWidth / numSamplesPerAxis;
            int yStep = objectHeight / numSamplesPerAxis;
            if (xStep == 0) xStep = 1;
            if (yStep == 0) yStep = 1;
            System.Windows.Point[] cornersAsWindowsPoints = PolarScores.Select(p => new System.Windows.Point(p.OutsideX, p.OutsideY)).ToArray();

            for (int i = startX; i < endX; i += xStep)
            {
                for (int j = startY; j < endY; j += yStep)
                {
                    if (i == 0 || j == 0 || i == inputImage.Width - 1 || j == inputImage.Height - 1)
                    {
                        continue;
                    }
                    if (!Utils.IsPointInPolygon(cornersAsWindowsPoints, new System.Windows.Point(i, j)))
                    {
                        // Debug.WriteLine("midpoint out of polygon");
                        continue;
                    }

                    count++;
                    System.Drawing.Color color = inputImage.GetPixel(i, j);
                    hslcolors.Add(new HSLColor(color.GetHue(), color.GetSaturation(), color.GetBrightness()));
                }
            }
            if (count == 0)
            {
                AvgColor = new HSLColor(0, 0, 0);
                return;
            }

            IGrouping<int, HSLColor> modeHSLColorGroup = hslcolors.GroupBy(n => ((int)((n.hue + 5) / 10) * 10)).OrderByDescending(g => g.Count()).FirstOrDefault();
            hue = (float)modeHSLColorGroup.Key;
            saturation = modeHSLColorGroup.Select(n => n.saturation).Max(); // Take max saturation value seen.
            //saturation = modeHSLColorGroup.GroupBy(n=> ((int)(n.saturation*10))/10f).OrderByDescending(g=>g.Count()).FirstOrDefault().Key;
            luminance = modeHSLColorGroup.GroupBy(n => n.luminance).OrderByDescending(g => g.Count()).FirstOrDefault().Key;
            //luminance = modeHSLColorGroup.Select(n => n.saturation).Average();

            if (hue > 345) hue = 0;
            //AvgColor = new HSLColor(hue, saturation, luminance);
            AvgColor = new HSLColor(hue, saturation, .4f); //because of lighting, face is often within shadow and luminance is too dark
            //Debug.WriteLine(AvgColor.hue + "\t" + AvgColor.saturation.ToString("#.##") + "\t" +AvgColor.luminance.ToString("#.##") + "\t" + hslcolors.Count);
        }

        public void BuildSegmentList(VectorOfPoint contour = null)
        {
            if (contour == null)
            {
                BuildSegmentListFromPolars();
                return;
            }
            AreaSegments = new();
            System.Drawing.Point[] corners = contour.ToArray();
            int pointCount = corners.Length;
            double X = (double)contour[pointCount - 1].X - (double)Centroid.X - (double)XOffset;
            double Y = (double)contour[pointCount - 1].Y - (double)Centroid.Y - (double)YOffset;
            PointTwoD previousPoint = new PointTwoD(X, Y);
            PointTwoD newPoint;
            for (int i = 0; i < pointCount; i++)
            {
                double sysPointX = (double)contour[i].X - (double)Centroid.X - (double)XOffset;
                double sysPointY = (double)contour[i].Y - (double)Centroid.Y - (double)YOffset;
                newPoint = new(sysPointX, sysPointY);
                SegmentTwoD newSegment = new SegmentTwoD(newPoint, previousPoint);
                AreaSegments.Add(newSegment);
                previousPoint = newPoint;
            }
        }

        public void BuildSegmentListFromPolars()
        {
            AreaSegments = new();
            int pointCount = PolarScores.Count;
            double X = (double)PolarScores.Last().OutsideX - (double)Centroid.X - (double)XOffset;
            double Y = (double)PolarScores.Last().OutsideY - (double)Centroid.Y - (double)YOffset;
            PointTwoD previousPoint = new PointTwoD(X * ScaleFactor, Y * ScaleFactor);
            PointTwoD newPoint;
            foreach (Polar polar in PolarScores)
            {
                double sysPointX = (double)polar.OutsideX - (double)Centroid.X - (double)XOffset;
                double sysPointY = (double)polar.OutsideY - (double)Centroid.Y - (double)YOffset;
                newPoint = new(sysPointX * ScaleFactor, sysPointY * ScaleFactor);
                SegmentTwoD newSegment = new SegmentTwoD(newPoint, previousPoint);
                AreaSegments.Add(newSegment);
                previousPoint = newPoint;
            }
        }

        public void BuildCornerList()
        {
            AreaCorners = new();
            SegmentTwoD previousSegment = AreaSegments.Last();
            foreach (SegmentTwoD segment in AreaSegments)
            {
                CornerTwoD newCorner = new(previousSegment, segment);
                AreaCorners.Add(newCorner);
                previousSegment = segment;
            }
        }

        public Point3DPlus GetPoint3DPlusFromPixel(System.Windows.Point p)
        {
            Point3DPlus retVal = new(30, 0, 0f);
            retVal.Theta = p.X / ScaleFactor * Angle.FromDegrees(FieldOfVision);
            retVal.Phi = -p.Y / ScaleFactor * Angle.FromDegrees(FieldOfVision * AspectRatio);
            retVal.R = 20;
            retVal.Theta += CameraPan;
            retVal.Phi += CameraTilt;
            return retVal;
        }

        public void CalculateArea()
        {
            var array = AreaSegments.ToArray();
            double area = 0;
            int j = array.Length - 1;
            for (int i = 0; i < array.Length; i++)
            {
                area += (array[j].p1.X + array[i].p1.X) * (array[j].p1.Y - array[i].p1.Y);
                j = i;
            }
            area = Math.Abs(area / 2);
            Area = area;
        }

        public double ScoreFor(UnknownArea otherArea, double NoMatchLimit)
        {
            double DifferencesSum = 0;
            if (otherArea == null) return NoMatchLimit;
            for (int i = 0; i < numberOfPolars; i++)
            {
                double difference = Math.Abs((PolarScores[i].Length() - otherArea.PolarScores[i].Length()));
                DifferencesSum += difference;
            }
            DifferencesSum = Math.Abs(DifferencesSum);
            if (DifferencesSum > NoMatchLimit) 
                return NoMatchLimit;
            return DifferencesSum;
        }

        public override string ToString()
        {
            return "ID: " + TrackID + " Count: " +
                   // PolarMinPercent.ToString("0.0000") + " Avg% " +
                   // PolarAvgPercent.ToString("0.0000") + " MinS " +
                   // PolarMinScore.ToString("0.0000") + " MaxS " +
                   // PolarMaxScore.ToString("0.0000") + " AvgS " +
                   // PolarAvgScore.ToString("0.0000") + " Count " +
                   AreaSegments.Count.ToString("####") + " Outer: " + isOuterContour;
        }

        public bool IsMostlyInsideOf(UnknownArea outerArea)
        {
            if (outerArea == null)
            {
                // Debug.WriteLine("20221127 IsMostlyInsideOf() returning true because outer == null");
                return true;
            }

            System.Drawing.Point TopLeft = new(ROI.Left, ROI.Top);
            System.Drawing.Point TopRight = new(ROI.Right, ROI.Top);
            System.Drawing.Point BotLeft = new(ROI.Left, ROI.Bottom);
            System.Drawing.Point BotRight = new(ROI.Right, ROI.Bottom);

            bool tl = outerArea.ROI.Contains(TopLeft);
            bool tr = outerArea.ROI.Contains(TopRight);
            bool bl = outerArea.ROI.Contains(BotLeft);
            bool br = outerArea.ROI.Contains(BotRight);
            double ov = ImgUtils.PercentageOverlap(outerArea.ROI, ROI);

            if (tl && tr && bl && br || ov > 0.95)
            {
                return true;
            }
            return false;
        }
    }

    public class CornerTwoD
    {
        public PointTwoD loc { get; set; } = new();
        public SegmentTwoD seg1 { get; set; }
        public SegmentTwoD seg2 { get; set; }
        public PointTwoD p1 { get; set; } = new();
        public double dist1 { get; set; }
        public PointTwoD p2 { get; set; } = new();
        public double dist2 { get; set; }
        public Angle Angle
        {
            get
            {
                Angle a = seg1.Angle - seg2.Angle;
                a = Math.Abs(a);
                if (a.ToDegrees() >= 180)
                    a -= Math.PI;
                return a;
            }
        }

        public CornerTwoD()
        {

        }

        public CornerTwoD(SegmentTwoD s1, SegmentTwoD s2)
        {
            if (s1 == null || s2 == null) return;

            seg1 = s1;
            seg2 = s2;
            if (s1.p1.X == s2.p1.X && s1.p1.Y == s2.p1.Y)
            {
                loc = s1.p1;
                p1 = s1.p2;
                p2 = s2.p2;
            }
            else if (s1.p2.X == s2.p2.X && s1.p2.Y == s2.p2.Y)
            {
                loc = s1.p2;
                p1 = s1.p1;
                p2 = s2.p1;
            }
            else if (s1.p1.X == s2.p2.X && s1.p1.Y == s2.p2.Y)
            {
                loc = s1.p1;
                p1 = s1.p2;
                p2 = s2.p1;
            }
            else if (s1.p2.X == s2.p1.X && s1.p2.Y == s2.p1.Y)
            {
                loc = s1.p2;
                p1 = s1.p1;
                p2 = s2.p2;
            }
            else
            {
                Debug.WriteLine("ERROR!!!");
                Debug.WriteLine("Seg1: " + s1);
                Debug.WriteLine("Seg2: " + s2);
            }
            dist1 = s1.Length;
            dist2 = s2.Length;
        }

        public override string ToString()
        {
            return ("(" + seg1.Length.ToString("f2") + "," + seg2.Length.ToString("f2") + ") " + Angle.ToString() + " " + p1 + " " + loc + " " + p2);
        }
    }

    public class SegmentTwoD
    {
        public PointTwoD p1 { get; set; } = new();
        public PointTwoD p2 { get; set; } = new();
        private double SetLength = -1.0;

        public double Length
        {
            get
            {
                if (SetLength == -1.0)
                {
                    double dx = p1.X - p2.X;
                    double dy = p1.Y - p2.Y;
                    SetLength = Math.Sqrt(dx * dx + dy * dy);
                }
                return SetLength;
            }

            set => SetLength = value;
        }

        public Angle Angle
        {
            get
            {
                Angle angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                if (angle < 0) angle += Math.PI;
                return angle;
            }
        }
        public System.Drawing.Point MidPoint { get => new System.Drawing.Point((int)(p1.X + p2.X) / 2, (int)(p1.Y + p2.Y) / 2); }
        public System.Drawing.Point MidPointI { get => new System.Drawing.Point((int)Math.Round((double)(p1.X + p2.X) / 2), (int)Math.Round((double)(p1.Y + p2.Y) / 2)); }

        public SegmentTwoD()
        {

        }
        public SegmentTwoD(PointTwoD point1, PointTwoD point2)
        {
            p1 = point1;
            p2 = point2;
        }

        public override string ToString()
        {
            return ("(" + p1.X + "," + p1.Y + ") (" + p2.X + "," + p2.Y + ")" + Angle.ToString() + " " + Length.ToString());
        }
    }

    public class KnownArea
    {
        public List<UnknownArea> UnknownAreas { get; set; } = new();

        public KnownArea()
        {
        }

        public KnownArea(UnknownArea area)
        {
            if (area == null) return;
            UnknownAreas.Add(area);
        }

        public override string ToString()
        {
            if (UnknownAreas == null || UnknownAreas.Count == 0) return "Empty KnownArea (should not happen!)";
            return "Count: " + UnknownAreas.Count;
        }

        public bool AddUnknownArea(UnknownArea area)
        {
            if (UnknownAreas == null)
            {
                UnknownAreas = new();
            }
            if (UnknownAreas.Count == 0)
            {
                UnknownAreas.Add(area);
            }
            if (UnknownAreas.Contains(area))
            {
                return false;
            }
            UnknownAreas.Add(area);
            return true;
        }

        // instead of a highscore we use a lowscore here, marking those areas with less 
        // differences as better candidates. We check against each UnknownArea, and return
        // the average score
        public double ScoreFor(UnknownArea otherArea, double NoMatchLimit)
        {
            if (otherArea == null) return 100;

            double avgScore = 0;
            foreach (UnknownArea area in UnknownAreas)
            {
                double score = area.ScoreFor(otherArea, NoMatchLimit);
                // Debug.WriteLine("TRACE: Score per UnknownArea: " + score);
                if (score >= NoMatchLimit) return score;
                avgScore += score;
            }

            avgScore /= UnknownAreas.Count;
            // Debug.WriteLine("TRACE: Average score per KnownArea: " + avgScore);
            return avgScore;
        }
    }
}
