using System;
using System.Collections.Generic;
using System.Linq;


namespace BrainSimulator.Modules;

public partial class ModuleVision
{
    public struct Vec
    {
        public double X, Y;
        public Vec(double x, double y) { X = x; Y = y; }
        public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
        public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
        public static Vec operator *(Vec a, double s) => new(a.X * s, a.Y * s);
        public static Vec operator /(Vec a, double s) => new(a.X / s, a.Y / s);
        public double Dot(Vec b) => X * b.X + Y * b.Y;
        public double Cross(Vec b) => X * b.Y - Y * b.X;
        public double Len() => Math.Sqrt(X * X + Y * Y);
        public Vec PerpCW() => new(Y, -X);    // (x,y)->(y,-x)
        public Vec PerpCCW() => new(-Y, X);   // (x,y)->(-y,x)
        public Vec Norm() { var l = Len(); return l > 0 ? this / l : new Vec(0, 0); }
        public string ToString() { return $"{X.ToString("0.0")},{Y.ToString("0.0")}"; }
    }

    public List<PointPlus> GetCenterlinePoints(List<PointPlus> boundaryPoints, List<PointPlus> strokePoints, double epsMove = 1e-3, int maxIters = 50, double suppression = 1.0)
    {
        List<Vec> boundary = boundaryPoints.Select(p => new Vec(p.X, p.Y)).ToList();
        List<Vec> seeds = strokePoints.Select(p => new Vec(p.X, p.Y)).ToList();
        //Func<Vec, bool> inside = v => isInsideFunc(Vec pt);
        List<(Vec center, double radius)> centers = ComputeCenters(boundary, strokePoints, epsMove, maxIters, suppression);
        return centers.Select(c => new PointPlus((float)c.center.X, (float)c.center.Y)).ToList();
    }

    // boundary: polygon vertices in order (CCW or CW), closed implicitly
    public  List<(Vec center, double radius)> ComputeCenters(
        List<Vec> boundaries,
        IList<PointPlus> seeds,
        double epsMove = 1e-3,
        int maxIters = 50,
        double suppression = 0.5)
    {
        suppression = 0.5;

        var centers = new List<(Vec center, double radius)>();
        foreach (var s in seeds)
        {
            if (GetLuminanceAtPoint(s) < .8) continue;
            Vec v = new(s.X, s.Y);
            var (c, r, ok) = AscendToMedialCenter(v, boundaries, epsMove, maxIters);
            if (!ok) continue;

            // Non-maximum suppression
            bool tooClose = centers.Any(e => (e.center - c).Len() < suppression * Math.Min(e.radius, r));
            if (!tooClose)
                centers.Add((c, r));
            else
            { }
        }
        return centers;
    }

    // ---- core ascent ----
    private static (Vec center, double radius, bool ok) AscendToMedialCenter(
        Vec seedPt, List<Vec> boundaries, double epsMove, int maxIters)
    {
        Vec prev = new(1e30, 1e30);
        double lastR = 0;
        Vec xInit = new(seedPt.X, seedPt.Y);
        epsMove = .1;
        if (Near(seedPt, new Vec(15.7, 14.7)))
        { }
        maxIters = 1;

        for (int k = 0; k < maxIters; k++)
        {
            // get closest boundary contact: projection to nearest segment
            if (Near(seedPt, new Vec(13.3, 5.0)))
            { }
            var p1 = ClosestBoundaryPoint(seedPt, boundaries);
            var d2 = (p1 - seedPt).Len();
            if (d2 < .75)
                return (default, 0, false);  //if it's really close to an edge, delete it

            //get first boundary hit by ray in opposide direction
            if (!RayFirstHit(seedPt, p1, boundaries, out Vec p3))
            {
                if (Near(seedPt, new Vec(14.1, 4.9)))
                { }
                return (seedPt, lastR, false); // reached maxIters; accept
            }
            // midpoint update
            var mid = (p1 + p3) / 2.0;
            double move = (mid - seedPt).Len();
            //if (move > .5)
            //    return (seedPt, lastR, true);
            double r = 0.5 * (p1 - p3).Len();
            var oldSeed = seedPt;
            seedPt = mid;
            if (Near(seedPt, new Vec(14.1, 4.9)))
            { }
            if (move < epsMove)
                return (seedPt, r, true);

            prev = seedPt; lastR = r;
        }
        return (seedPt, lastR, true); // reached maxIters; accept
    }

    static bool AngleOpposite(Vec a, Vec b, double maxAwayDeg = 12)
    {
        double aa = a.X * a.X + a.Y * a.Y;
        double bb = b.X * b.X + b.Y * b.Y;
        if (aa <= 1e-12 || bb <= 1e-12) return false;

        double dot = a.X * b.X + a.Y * b.Y;
        double cosE = Math.Cos(maxAwayDeg * Math.PI / 180.0);
        // θ ≈ 180°  ⇔  cosθ ≈ -1  ⇔  dot ≤ -|a||b| cos(ε)
        return dot <= -Math.Sqrt(aa * bb) * cosE;
    }


    // ---- helpers ----
    private static Vec ClosestBoundaryPoint(Vec x, List<Vec> boundaries)
    {
        double bestD2 = double.PositiveInfinity;
        Vec bestP = default;
        int bestI = -1;
        for (int i = 0; i < boundaries.Count; i++)
        {
            var dist = (boundaries[i] - x).Len();
            if (dist < 1e-9)
                return boundaries[i]; // exactly on vertex
            if (dist < bestD2)
            {
                bestD2 = dist;
                bestP = boundaries[i];
                bestI = i;
            }
        }
        return bestP;
    }

    static bool Near(Vec p1, Vec p2)
    {
        return ((p1 - p2).Len() < .1);
    }

    static PointPlus GetPPfromVec(Vec v) { return new PointPlus((float)v.X, (float)v.Y); }
    private static bool RayFirstHit(Vec origin, Vec p1, List<Vec> boundaries, out Vec hit)
    {
        PointPlus pp1 = new((float)p1.X, (float)p1.Y);
        PointPlus ppOrigin = new((float)origin.X, (float)origin.Y);

        hit = default;
        double bestT = double.PositiveInfinity;
        if (Near(origin, new Vec(15.4, 17.0)))
        { }

        Vec dir = origin - (p1 - origin); //get the opposite direction

        Segment s1 = new(GetPPfromVec(origin), GetPPfromVec(dir));
        Segment s2 = new(s1);
        s2.P2 = Utils.ExtendSegment(s1.P1, s1.P2, 10, false);
        s2.P1 = Utils.ExtendSegment(s1.P1, s1.P2, -.5f, true);

        foreach (var pt in boundaries)
        {
            var distToPt = (pt - origin).Len();
            if (distToPt > 10) continue;
            if (distToPt > bestT) continue; // already have a closer hit
            if (pt.X == p1.X && pt.Y == p1.Y) continue;

            //find the distance from the pt to the ray defined by origin, dir
            var distToRay = Utils.DistancePointToSegment(s2, GetPPfromVec(pt));
            var distToRay2 = Utils.FindDistanceToSegment(GetPPfromVec(pt), s2.P1, s2.P2, out System.Windows.Point closest);
            Vec closestV = new(closest.X, closest.Y);
            if (Near(closestV, new Vec(19.9, 23.0)))
            { }
            //hack because closest might be in the wrong direction completely
            if (closest == (System.Windows.Point)s2.P1)
            { }
            else if (closest == (System.Windows.Point)s2.P2)
            { }
            else if (distToRay2 < 4)
            {
                hit = pt;
                bestT = distToPt;
            }
        }
        return bestT < double.PositiveInfinity;
    }
}

