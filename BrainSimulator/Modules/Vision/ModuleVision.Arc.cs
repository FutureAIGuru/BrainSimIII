using System.Collections.Generic;
using System.Windows.Media;
using System;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;


namespace BrainSimulator.Modules;

public partial class ModuleVision
{
    public class Arc : Corner
    {
        float dist1 = 0; //number of units down the first leg
        float dist2 = 0;
    }

    public void FindArcs()
    {
        if (corners == null) return;
        foreach (var corner in corners)
        {
            ConvertCornerToArc(corner);
        }
        MergeArcs();
    }

    public void ConvertCornerToArc(Corner c)
    {
        //start with circular arcs
        //for (int ix = 2; ix < c.s1.Length;ix++)
        //{
        //    for (int iy= ix; iy==ix &&  iy < c.s1.Length; iy++)
        //    {
        //        //create an arc based on these two parameters
        //        //is this arec a better fit to the boundary points?
        //    }
        //}
    }
    public void MergeArcs()
    {
        //following along an outline, are there adjacent ARCs which can be merged?
    }

    //TODO, move this to the dialog box
    /*        private void DrawEllipticalArc()
            {
                Point corner = new Point(100, 100);
                double distance1 = 100;
                double distance2 = 50;
                double angle = 45;
                double startAngle = 0;
                double endAngle = 180;
                int numPoints = 100;

                List<Point> arcPoints = CalculateEllipticalArc(corner, distance1, distance2, angle, startAngle, endAngle, numPoints);

                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = arcPoints[0];

                PolyLineSegment polyLineSegment = new PolyLineSegment();
                foreach (var point in arcPoints)
                {
                    polyLineSegment.Points.Add(point);
                }

                pathFigure.Segments.Add(polyLineSegment);

                PathGeometry pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                Path path = new Path
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Data = pathGeometry
                };

                DrawingCanvas.Children.Add(path);
            }
    */
    public static List<Point> CalculateEllipticalArc(Point corner, double distance1, double distance2, double angle, double startAngle, double endAngle, int numPoints)
    {
        List<Point> points = new List<Point>();

        double a = distance1;
        double b = distance2;

        double angleRad = angle * Math.PI / 180.0;
        double startAngleRad = startAngle * Math.PI / 180.0;
        double endAngleRad = endAngle * Math.PI / 180.0;

        double h = corner.X + a * Math.Cos(angleRad) / 2;
        double k = corner.Y + b * Math.Sin(angleRad) / 2;

        for (int i = 0; i <= numPoints; i++)
        {
            double t = startAngleRad + i * (endAngleRad - startAngleRad) / numPoints;
            double x = h + a * Math.Cos(t) * Math.Cos(angleRad) - b * Math.Sin(t) * Math.Sin(angleRad);
            double y = k + a * Math.Cos(t) * Math.Sin(angleRad) + b * Math.Sin(t) * Math.Cos(angleRad);
            points.Add(new Point(x, y));
        }

        return points;
    }
}

