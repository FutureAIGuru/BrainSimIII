/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
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
using System.CodeDom;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static BrainSimulator.Utils;
using static System.Math;

namespace BrainSimulator.Modules
{
    //this is an extension of a position point which allows access via both polar and cartesian coordinates
    //it also accepts a "conf"idence value which can be used to indicate the accuracy of the position
    public class PointPlus
    {
        private Point p;
        private float r;
        private Angle theta;
        private bool polarDirty = false;
        private bool xyDirty = false;

        public static implicit operator Point(PointPlus a) => a.p;
        public static implicit operator PointPlus(Point a) => new PointPlus(a);


        public PointPlus()
        {
            r = 0;
            theta = 0;
            P = new Point(0, 0);
            Conf = 0;
        }
        public PointPlus(float x, float y)
        {
            P = new Point(x, y);
            Conf = 0;
        }
        public PointPlus(float R1, Angle theta1)
        {
            R = R1;
            theta = theta1;
        }
        public PointPlus(PointPlus pp)
        {
            R = pp.R;
            theta = pp.Theta;
            Conf = pp.Conf;
        }
        public PointPlus(Point pp)
        {
            X = (float)pp.X;
            Y = (float)pp.Y;
            Conf = 0;
        }

        [XmlIgnore]
        public Point P
        {
            get { if (xyDirty) UpdateXY(); return p; }
            set { polarDirty = true; p = value; }
        }
        public float X { get { if (xyDirty) UpdateXY(); return (float)p.X; } set { if (xyDirty) UpdateXY(); p.X = value; polarDirty = true; } }
        public float Y { get { if (xyDirty) UpdateXY(); return (float)p.Y; } set { if (xyDirty) UpdateXY(); p.Y = value; polarDirty = true; } }
        [XmlIgnore]
        public Vector V { get => (Vector)P; }
        [XmlIgnore]
        public float Degrees { get => (float)(Theta * 180 / PI); }
        public float Conf { get; set; }
        [XmlIgnore]
        public float R { get { if (polarDirty) UpdatePolar(); return r; } set { if (polarDirty) UpdatePolar(); r = value; xyDirty = true; } }
        [XmlIgnore]
        public Angle Theta
        {
            get { if (polarDirty) UpdatePolar(); return theta; }
            set
            {//keep theta within the range +/- PI
                if (polarDirty) UpdatePolar();
                theta = value;
                //if (theta > PI) theta -= 2 * (float)PI;
                //if (theta < -PI) theta += 2 * (float)PI;
                xyDirty = true;
            }
        }
        private void UpdateXY()
        {
            p.X = r * Cos(theta);
            p.Y = r * Sin(theta);
            xyDirty = false;
        }
        public PointPlus Clone()
        {
            PointPlus p1 = new PointPlus() { R = this.R, Theta = this.Theta, Conf = this.Conf };
            return p1;
        }
        public void UpdatePolar()
        {
            theta = (float)Atan2(p.Y, p.X);
            r = (float)Sqrt(p.X * p.X + p.Y * p.Y);
            polarDirty = false;
        }
        public bool Near(PointPlus PP, float toler)
        {
            float dist = (this - PP).R;
            if (dist < toler) return true;
            return false;
        }
        public override string ToString()
        {
            //            string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ") Conf:" + Conf.ToString("F3");
            string s = $"({X.ToString("0.0")}.{Y.ToString("0.0")})";
            return s;
        }

        //these make comparisons by value instead of by reference
        public static bool operator ==(PointPlus a, PointPlus b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return (a.P.X == b.P.X && a.P.Y == b.P.Y && a.Conf == b.Conf);
        }
        public static bool operator !=(PointPlus a, PointPlus b)
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return (a.P.X != b.P.X || a.P.Y != b.P.Y || a.Conf != b.Conf);
        }
        public static PointPlus operator +(PointPlus a, PointPlus b)
        {
            Point p = new Point(a.P.X + b.P.X, a.P.Y + b.P.Y);
            PointPlus retVal = new PointPlus
            {
                P = p,
            };
            return retVal;
        }
        public static PointPlus operator -(PointPlus a, PointPlus b)
        {
            PointPlus retVal = new PointPlus
            {
                P = new Point(a.P.X - b.P.X, a.P.Y - b.P.Y)
            };
            return retVal;
        }
        public static PointPlus operator *(PointPlus a, double b)
        {
            PointPlus retVal = new PointPlus
            {
                P = new Point(a.P.X*b, a.P.Y * b)
            };
            return retVal;
        }
        public override bool Equals(object p1)
        {
            if (p1 is not null && p1 is PointPlus p2)
            {
                return (p2.P.X == P.X && p2.P.Y == P.Y);

            }
            return false;
        }
    }

    // this is an extension of a 3D position point which allows access via both polar and cartesian coordinates
    // it also accepts a "conf"idence value which can be used to indicate the accuracy of the position
    public class Point3DPlus
    {
        private Point3D p;
        private float r;
        private Angle theta;
        private Angle phi;
        private bool polarDirty = false;  // polar values invalid?
        private bool xyzDirty = false;    // xyz values invalid?

        public static implicit operator Point3D(Point3DPlus a) => a.p;
        public static implicit operator Point3DPlus(Point3D a) => new Point3DPlus(a);

        public Point3DPlus()
        {
            r = 0;
            theta = 0;
            phi = 0;
            P = new Point3D(0, 0, 0);
            Conf = 0;
        }
        public Point3DPlus(float x, float y, float z)
        {
            P = new Point3D(x, y, z);
            Conf = 0;
        }
        public Point3DPlus(float R1, Angle theta1, Angle phi1)
        {
            R = R1;
            theta = theta1;
            phi = phi1;
        }
        public Point3DPlus(Point3DPlus pp)
        {
            R = pp.R;
            theta = pp.Theta;
            phi = pp.Phi;
            Conf = pp.Conf;
        }
        public Point3DPlus(Point3D pp)
        {
            X = (float)pp.X;
            Y = (float)pp.Y;
            Z = (float)pp.Z;
            Conf = 0;
        }

        // This constructor used to calculate R based on the size of the object. 
        // This is still intact if the final boolean parameter is left at its default.
        // The new option is meant for cases where we use the LOWEST 2D coorcinate of an object.
        // We will assume the bottom of the object is on the floor, which means Phi and the height of 
        // the camera will be sufficient to determine R pretty accurately.
        // if currentPosition is passed in, we will take that as the base position.
        public Point3DPlus(float x, float y, float w, float h, float xsize, float ysize, Point3DPlus currentPosition = null)
        {
            // this assumes x and y are 2D coordinates on a field of view bitmap,
            // with 0, 0 at center. xsize and ysize are the size of the bitmap. 
            double maxTheta = 22.5 / 180.0 * Math.PI;
            double max_X = xsize / 2.0;
            Theta = -x / max_X * maxTheta;
            double maxPhi = 15.2 / 180.0 * Math.PI;
            double max_Y = ysize / 2.0;
            Phi = -y / max_Y * maxPhi;

            if (currentPosition is null)
            {
                //                      real height (mm) * image height (pixels) 
                // distance estimate = -------------------------------------------
                //                     object height (pixels) * sensor height (mm)
                // we can take real height to be some initial estimate, and sensor height is fixed, 
                // so distance becomes (image height / object height) * some constant
                // but we use both width and height and average them... 
                R = ((float)xsize / (float)w + (float)ysize / (float)h) / 2.0f * 4.1658f;
            }
            else
            {
                R = (float)Math.Abs(4.0f / Math.Sin(Phi));
                Theta += (float)currentPosition.Theta;
                Phi += (float)currentPosition.Phi;
            }

            Conf = 0;
        }

        public Point Get2DPoint(float xsize, float ysize)
        {
            Point result = new Point();
            // this assumes x and y are 2D coordinates on a field of view bitmap,
            // with 0, 0 at center. xsize and ysize are the size of the bitmap. 
            double maxTheta = 22.5 / 180.0 * Math.PI;
            double max_X = xsize / 2.0;
            result.X = max_X * Theta / maxTheta;

            double maxPhi = 15.2 / 180.0 * Math.PI;
            double max_Y = ysize / 2.0;
            result.Y = max_Y * Phi / maxPhi;

            return result;
        }

        public Point3D P
        {
            get { if (xyzDirty) UpdateXYZ(); return p; }
            set { polarDirty = true; p = value; }
        }
        public float X { get { if (xyzDirty) UpdateXYZ(); return (float)p.X; } set { if (xyzDirty) UpdateXYZ(); p.X = value; polarDirty = true; } }
        public float Y { get { if (xyzDirty) UpdateXYZ(); return (float)p.Y; } set { if (xyzDirty) UpdateXYZ(); p.Y = value; polarDirty = true; } }
        public float Z { get { if (xyzDirty) UpdateXYZ(); return (float)p.Z; } set { if (xyzDirty) UpdateXYZ(); p.Z = value; polarDirty = true; } }
        [XmlIgnore]
        public Vector3D V { get => (Vector3D)P; }
        [XmlIgnore]
        public float DegreesTheta { get => (float)(Theta * 180 / PI); }
        public float DegreesPhi { get => (float)(Phi * 180 / PI); }
        public float Conf { get; set; }
        [XmlIgnore]
        public float R { get { if (polarDirty) UpdatePolar(); return r; } set { if (polarDirty) UpdatePolar(); r = value; xyzDirty = true; } }
        [XmlIgnore]
        public Angle Theta
        {
            get { if (polarDirty) UpdatePolar(); return theta; }
            set
            {   //keep theta within the range +/- PI
                if (polarDirty) UpdatePolar();
                theta = value;
                while (theta > PI) theta -= 2 * (float)PI;
                while (theta < -PI) theta += 2 * (float)PI;
                xyzDirty = true;
            }
        }
        [XmlIgnore]
        public Angle Phi
        {
            get { if (polarDirty) UpdatePolar(); return phi; }
            set
            {   //keep phi within the range +/- PI/2
                if (polarDirty) UpdatePolar();
                phi = value;
                while (phi > PI / 2.0) phi -= (float)PI;
                while (phi < -PI / 2.0) phi += (float)PI;
                xyzDirty = true;
            }
        }
        private void UpdateXYZ()
        {
            p.X = r * Cos(theta) * Cos(phi);
            p.Y = r * Sin(theta) * Cos(phi);
            p.Z = r * Sin(phi);
            xyzDirty = false;
        }
        public Point3DPlus Clone()
        {
            Point3DPlus p1 = new Point3DPlus() { R = this.R, Theta = this.Theta, Phi = this.Phi, Conf = this.Conf };
            return p1;
        }

        public bool NearBy(Point3DPlus otherPoint, double maxDistance)
        {
            if (otherPoint is null) return false;
            double deltaX = Math.Abs((double)X - (double)otherPoint.X);
            double deltaY = Math.Abs((double)Y - (double)otherPoint.Y);
            double deltaZ = Math.Abs((double)Z - (double)otherPoint.Z);
            if (deltaX < maxDistance &&
                deltaY < maxDistance &&
                deltaZ < maxDistance)
            {
                return true;
            }
            return false;
        }

        public void UpdatePolar()
        {
            r = (float)Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
            theta = (float)Atan2(p.Y, p.X);
            if (r == 0.0)
            {
                phi = 0.0;
            }
            else
            {
                phi = (float)Asin(p.Z / r);
            }
            polarDirty = false;
        }
        public bool Near(Point3DPlus PP, float toler)
        {
            if ((this - PP).R < toler) return true;
            return false;
        }
        public override string ToString()
        {
            string s = "R: " + R.ToString("F3") + ", Theta: " + DegreesTheta.ToString("F3") + "°, Phi: " + DegreesPhi.ToString("F3") + "° (" +
                       X.ToString("F2") + "," + Y.ToString("F2") + "," + Z.ToString("F2") + ") Conf:" + Conf.ToString("F3");
            return s;
        }

        //these make comparisons by value instead of by reference
        public static bool operator ==(Point3DPlus a, Point3DPlus b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return (a.P.X == b.P.X && a.P.Y == b.P.Y && a.P.Z == b.P.Z && a.Conf == b.Conf);
        }
        public static bool operator !=(Point3DPlus a, Point3DPlus b)
        {
            if (a is null && b is null) return false;
            if (a is null || b is null) return true;
            return (a.P.X != b.P.X || a.P.Y != b.P.Y || a.P.Z != b.P.Z || a.Conf != b.Conf);
        }
        public static Point3DPlus operator +(Point3DPlus a, Point3DPlus b)
        {
            Point3D p = new Point3D(a.P.X + b.P.X, a.P.Y + b.P.Y, a.P.Z + b.P.Z);
            Point3DPlus retVal = new Point3DPlus
            {
                P = p,
            };
            return retVal;
        }
        public static Point3DPlus operator *(Point3DPlus a, float scale)
        {
            Point3DPlus p = new Point3DPlus((float)a.P.X * scale, a.P.Y * scale, a.P.Z * scale);
            return p;
        }
        public static Point3DPlus operator -(Point3DPlus a, Point3DPlus b)
        {
            Point3DPlus retVal = new Point3DPlus
            {
                P = new Point3D(a.P.X - b.P.X, a.P.Y - b.P.Y, a.P.Z - b.P.Z)
            };
            return retVal;
        }
        public override bool Equals(object p1)
        {
            if (p1 is not null && p1 is Point3DPlus p2)
            {
                return (p2.P.X == P.X && p2.P.Y == P.Y && p2.P.Z == P.Z);
            }
            return false;
        }

        public bool NearPolar(Point3DPlus p2, int withinDegrees = 5)
        {
            if (Math.Abs(this.Theta - p2.Theta) < Angle.FromDegrees(withinDegrees) &&
                Math.Abs(this.Phi - p2.Phi) < Angle.FromDegrees(withinDegrees))
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // Calculates distance between two Point3DPlus objects.
        internal double Distance(Point3DPlus otherPoint)
        {
            Point3DPlus difference = this - otherPoint;
            return difference.R;
        }
    }

    public class Motion : PointPlus
    {
        public Angle rotation = 0;
        public override string ToString()
        {
            string s = "R: " + R.ToString("F3") + ", Theta: " + Degrees.ToString("F3") + "° (" + X.ToString("F2") + "," + Y.ToString("F2") + ") Rot:" + rotation;
            return s;
        }
    }

    public class Segment
    {
        public PointPlus P1;
        public PointPlus P2;
        public int debugIndex;

        public Segment() { }
        public Segment(Segment s)
        {
            P1 = s.P1;
            P2 = s.P2;
        }
        public override string ToString()
        {
            string retVal = $"L: {(int)Length} ({P1.X.ToString("0.0")},{P1.Y.ToString("0.0")}) : ({P2.X.ToString("0.0")},{P2.Y.ToString("0.0")}) A: {Angle.Degrees.ToString("0.0")}°";
            return retVal;
        }

        public static  bool operator == (Segment s1, Segment s2)
        {
            if (s1 is null && s2 is null) return true;
            if (s1 is null || s2 is null) return false;
            float toler = 0.1f;
            if ((s1.P1.Near(s2.P1, toler) && s1.P2.Near(s2.P2, toler)) || (s1.P1.Near(s2.P2, toler) && s1.P2.Near(s2.P1, toler))) return true;
            return false;
        }
        public static bool operator !=(Segment s1, Segment s2)
        {
            return !(s1 == s2);
        }

        public Segment(PointPlus P1i, PointPlus P2i)
        {
            P1 = P1i;
            P2 = P2i;
            debugIndex = -1;
        }
        public Segment(PointPlus P1i, PointPlus P2i, ColorInt theColori)
        {
            P1 = P1i;
            P2 = P2i;
            debugIndex = theColori;
        }
        public PointPlus MidPoint
        {
            get
            {
                return new PointPlus { X = (P1.X + P2.X) / 2, Y = (P1.Y + P2.Y) / 2 };
            }
        }

        public PointPlus ClosestPoint()
        {
            Utils.FindDistanceToSegment(new Point(0, 0), P1.P, P2.P, out Point closest);
            return new PointPlus { P = closest };
        }
        public float Length
        {
            get
            {
                float length = (float)((Vector)P2.V - P1.V).Length;
                return length;
            }
        }
        public float VisualWidth()
        {
            float length = P2.Theta - P1.Theta;
            return length;
        }
        public Angle Angle
        {
            get
            {
                PointPlus pTemp = new PointPlus() { P = (Point)(P1.V - P2.V) };
                return pTemp.Theta;
            }
        }

        public Segment Clone()
        {
            Segment s = new Segment
            {
                P1 = this.P1.Clone(),
                P2 = this.P2.Clone(),
                debugIndex = this.debugIndex
            };
            return s;
        }
    }

    //this little helper adds the convenience of displaying angles in radians AND degrees even though they are stored in radians
    //it's really just an extension of float...it also accepts assignment from a double without an explicit cast
    public class Angle
    {
        private float theAngle = 0;
        public Angle() { this.theAngle = 0; }  // Don't remove, needed to save Angles to XML!
        public Angle(float angle) { this.theAngle = angle; }
        public static implicit operator float(Angle a) => (a is not null) ? a.theAngle : 0;
        public static implicit operator Angle(float a) => new Angle(a);
        public static implicit operator Angle(double a) => new Angle((float)a);
        public static Angle operator -(Angle a, Angle b)
        {
            Angle c = (float)a - (float)b;
            //c = ((float)c + PI) % (2 * PI) - PI;
            return c;
        }
        public static Angle operator +(Angle a, Angle b)
        {
            Angle c = (float)a + (float)b;
            return c;
        }
        public override string ToString()
        {
            float degrees = theAngle * 180 / (float)PI;
            string s = theAngle.ToString("0.00") + " " + degrees.ToString("0.0") + "°";
            return s;
        }
        public int CompareTo(Angle a)
        {
            return theAngle.CompareTo(a.theAngle);
        }

        public float Degrees
        {
            get { return theAngle * 180 / (float)PI; }
            set { theAngle = (float)(value * PI / 180.0); }
        }
        public static Angle FromDegrees(float degrees)
        {
            return (float)(degrees * PI / 180.0);
        }
        public Angle Normalize()
        {
            Angle a = theAngle;
            a = (float)a % (2 * PI);
            if (a < 0) a += 2 * PI;
            return a;
        }
    }

    public class ColorInt
    {
        private readonly int theColor;
        public ColorInt(int aColor) { this.theColor = aColor; }
        public static implicit operator int(ColorInt c) => c.theColor;
        public static implicit operator ColorInt(int aColor) => new ColorInt(aColor);
        public static implicit operator ColorInt(Color aColor) => new ColorInt(ColorToInt(aColor));
        public override string ToString()
        {
            int A = theColor >> 24 & 0xff;
            int R = theColor >> 16 & 0xff;
            int G = theColor >> 8 & 0xff;
            int B = theColor & 0xff;
            string s = "ARGB: " + A + "," + R + "," + G + "," + B;
            return s;
        }
        public static bool operator ==(ColorInt a, ColorInt b)
        {
            return (a.theColor == b.theColor);
        }
        public static bool operator !=(ColorInt a, ColorInt b)
        {
            return (a.theColor != b.theColor);
        }
        public int CompareTo(ColorInt c)
        {
            return theColor.CompareTo(c.theColor);
        }
        public override bool Equals(object a)
        {
            if (a is ColorInt c)
                return this.theColor.Equals(c.theColor);
            return false;
        }
        public override int GetHashCode()
        {
            return theColor;
        }
    }

}