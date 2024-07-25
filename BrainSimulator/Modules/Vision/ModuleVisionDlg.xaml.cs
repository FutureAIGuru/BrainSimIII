//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Math;


namespace BrainSimulator.Modules
{
    public partial class ModuleVisionDlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleVisionDlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        int scale;
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleVision parent = (ModuleVision)base.ParentModule;

            if (parent.imageArray == null) return false;
            try
            {
                labelProperties.Content = "Image: " + parent.imageArray.GetLength(0) + "x" + parent.imageArray.GetLength(1) +
                //    "\r\nBit Depth: " + parent.bitmap.Format.BitsPerPixel +
                    "\r\nSegments: " + parent.segments.Count +
                    "\r\nCorners: " + parent.corners.Count;
            }
            catch { return false; }

            theCanvas.Children.Clear();

            scale = (int)(theCanvas.ActualHeight / parent.imageArray.GetLength(1));
            int pixelSize = scale - 2;
            if (pixelSize < 2) pixelSize = 2;

            //draw the image
            if (cbShowImage.IsChecked == true && parent.bitmap != null)
            {
                //TODO: images with bit depth < 32 display at slightly wrong scale
                Image i = new Image
                {
                    Height = parent.bitmap.Height * scale,
                    Width = parent.bitmap.Width * scale,
                    Source = (ImageSource)parent.bitmap,
                };
                Canvas.SetLeft(i, 0);
                Canvas.SetTop(i, 0);
                theCanvas.Children.Add(i);
            }

            //draw the pixels
            if (cbShowPixels.IsChecked == true && parent.imageArray != null)
            {
                for (int x = 0; x < parent.imageArray.GetLength(0); x++)
                    for (int y = 0; y < parent.imageArray.GetLength(1); y++)
                    {
                        var pixel = parent.imageArray[x, y];
                        var s = pixel.ToString();
                        if (pixel.ToString() != "#01FFFFFF")
                        { }
                        pixel.A = 255;

                        if (pixel != null)
                        {
                            //pixel.luminance /= 2;
                            SolidColorBrush b = new SolidColorBrush(pixel);
                            Rectangle e = new()
                            {
                                Height = pixelSize,
                                Width = pixelSize,
                                Stroke = b,
                                Fill = b,
                                ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 50, Content = $"({(int)x},{(int)y}) {pixel}" },
                            };
                            Canvas.SetLeft(e, x * scale - pixelSize / 2);
                            Canvas.SetTop(e, y * scale - pixelSize / 2);
                            theCanvas.Children.Add(e);
                        }
                    }
            }

            //draw the strokes
            if (cbShowSrokes.IsChecked == true && parent.strokePoints != null)
            {
                foreach (var pt in parent.strokePoints)
                {
                    Rectangle e = new()
                    {
                        Height = pixelSize / 2,
                        Width = pixelSize / 2,
                        Stroke = Brushes.Blue,
                        Fill = Brushes.Blue,
                        //                                ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"({(int)x},{(int)y})" },
                    };
                    Canvas.SetLeft(e, pt.X * scale - pixelSize / 4);
                    Canvas.SetTop(e, pt.Y * scale - pixelSize / 4);
                    theCanvas.Children.Add(e);
                }
            }
            //draw the strokes
            if (cbShowBoundaries.IsChecked == true && parent.boundaryPoints != null)
            {
                foreach (var pt in parent.boundaryPoints)
                {
                    Rectangle e = new()
                    {
                        Height = pixelSize / 2,
                        Width = pixelSize / 2,
                        Stroke = Brushes.Blue,
                        Fill = Brushes.Blue,
                        //                                ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"({(int)x},{(int)y})" },
                    };
                    Canvas.SetLeft(e, pt.X * scale - pixelSize / 4);
                    Canvas.SetTop(e, pt.Y * scale - pixelSize / 4);
                    theCanvas.Children.Add(e);
                }
            }
            //if (cbShowBoundaries.IsChecked == true && parent.boundaryArray != null)
            //{
            //    for (int x = 0; x < parent.boundaryArray.GetLength(0); x++)
            //        for (int y = 0; y < parent.boundaryArray.GetLength(1); y++)
            //        {
            //            if (parent.boundaryArray[x, y] != 0)
            //            {
            //                Ellipse e = new Ellipse()
            //                {
            //                    Height = pixelSize,
            //                    Width = pixelSize,
            //                    Stroke = Brushes.Black,
            //                    Fill = Brushes.Black,
            //                    ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"({(int)x},{(int)y})" },
            //                };
            //                Canvas.SetLeft(e, x * scale - pixelSize / 2);
            //                Canvas.SetTop(e, y * scale - pixelSize / 2);
            //                theCanvas.Children.Add(e);
            //            }
            //        }
            //}

            //draw the hough transform
            if (cbShowHough.IsChecked == true)
            {
                var acc = parent.segmentFinder.accumulator;
                int maxRho = acc.GetLength(0);
                int maxTheta = acc.GetLength(1);
                float scalex = (float)theCanvas.ActualWidth / acc.GetLength(0);
                float scaley = (float)theCanvas.ActualHeight / acc.GetLength(1);
                for (int rhoIndex = 0; rhoIndex < maxRho; rhoIndex++)
                {
                    for (int thetaIndex = 0; thetaIndex < maxTheta; thetaIndex++)
                    {
                        var votes = parent.segmentFinder.accumulator[rhoIndex, thetaIndex].Count;
                        if (votes > 19) votes = 19;
                        if (votes > 4)// && votes < 40)
                        {

                            //                            var maxItem = parent.segmentFinder.localMaxima.FindFirst(x => x.Item2 == rhoIndex && x.Item3 == thetaIndex);

                            HSLColor hSLColor = new HSLColor(Colors.Green);
                            float intensity = (float)votes / 20;
                            hSLColor.luminance = intensity;
                            //if (maxItem != null)
                            //{
                            //    hSLColor = new HSLColor(Colors.Red);
                            //    hSLColor.luminance = 0.5f;
                            //}
                            Brush brush1 = new SolidColorBrush(hSLColor.ToColor());
                            Rectangle e = new()
                            {
                                Height = scaley - 1,
                                Width = scalex - 1,
                                Stroke = brush1,
                                Fill = brush1,
                                ToolTip = new System.Windows.Controls.ToolTip { Content = $"({(int)rhoIndex},{(int)thetaIndex})" },
                            };
                            Canvas.SetLeft(e, rhoIndex * scalex);
                            Canvas.SetTop(e, thetaIndex * scaley);
                            e.MouseEnter += E_MouseEnter;
                            theCanvas.Children.Add(e);
                        }
                    }
                }
            }
            //draw the segments
            if (cbShowSegments.IsChecked == true && parent.segments != null & parent.segments.Count > 0)
            {
                for (int i = 0; i < parent.segments.Count; i++)
                {
                    Segment segment = parent.segments[i];
                    Line l = new Line()
                    {
                        X1 = segment.P1.X * scale,
                        X2 = segment.P2.X * scale,
                        Y1 = segment.P1.Y * scale,
                        Y2 = segment.P2.Y * scale,
                        Stroke = Brushes.Red,
                        StrokeThickness = 4,
                        ToolTip = new System.Windows.Controls.ToolTip { Content = $"{segment.debugIndex}:({segment.P1.X},{segment.P1.Y}) - -({segment.P2.X},{segment.P2.Y})" },
                    };
                    theCanvas.Children.Add(l);
                }
            }

            //draw the corners
            if (cbShowCorners.IsChecked == true && parent.corners != null && parent.corners.Count > 0)
            {
                for (int i = 0; i < parent.corners.Count; i++)
                {
                    ModuleVision.Corner corner = parent.corners[i];
                    float size = 15;
                    Brush b = Brushes.White;
                    if (corner.angle.Degrees == 180 || corner.angle.Degrees == -180)
                        b = Brushes.Pink;
                    Ellipse e = new Ellipse()
                    {
                        Height = size,
                        Width = size,
                        Stroke = b,
                        Fill = b,
                        ToolTip = new System.Windows.Controls.ToolTip{ HorizontalOffset=100,  Content = $"{i}", },
                    };
                    Canvas.SetTop(e, corner.pt.Y * scale - size / 2);
                    Canvas.SetLeft(e, corner.pt.X * scale - size / 2);
                    theCanvas.Children.Add(e);

                    //test out drawing little lines to represent the angle (then an elliptical arc, soon)
                    int i1 = 5;
                    PointPlus delta = corner.prevPt  - corner.pt;
                    delta.R = i1;
                    PointPlus pt1 = corner.pt + delta;

                    Line l = new Line()
                    {
                        X1 = corner.pt.X * scale,
                        Y1 = corner.pt.Y * scale,
                        X2 = pt1.X * scale,
                        Y2 = pt1.Y * scale,
                        Stroke = Brushes.DarkGray,
                        StrokeThickness = 2,
                    };
                    theCanvas.Children.Add(l);

                    delta = corner.nextPt - corner.pt;
                    delta.R = i1;
                    PointPlus pt2 = corner.pt + delta;

                    l = new Line()
                    {
                        X1 = corner.pt.X * scale,
                        Y1 = corner.pt.Y * scale,
                        X2 = pt2.X * scale,
                        Y2 = pt2.Y * scale,
                        Stroke = Brushes.DarkGray,
                        StrokeThickness = 2,
                    };
                    theCanvas.Children.Add(l);

                    var arc = new EllipticalCornerArc(corner.pt * scale, pt1 * scale, pt2 * scale);
                    ////var arc = new EllipticalCornerArc(corner.location, s1.P2, s2.P2);
                    var path = arc.GetEllipticalArcPath();
                    if (path != null)
                        theCanvas.Children.Add(path);
                }

            }

            return true;
        }

        public class EllipticalCornerArc
        {
            public PointPlus Corner { get; }
            public PointPlus tangencyPoint1 { get; set; }
            public PointPlus tangencyPoint2 { get; set; }
            public double Distance1 { get; }
            public double Distance2 { get; }

            public EllipticalCornerArc(PointPlus corner, PointPlus tangencyPoint1, PointPlus tangencyPoint2)
            {
                Corner = corner;
                this.tangencyPoint1 = tangencyPoint1;
                this.tangencyPoint2 = tangencyPoint2;
                Distance1 = (corner - tangencyPoint1).R;
                Distance2 = (corner - tangencyPoint2).R;
            }

            public Polyline GetEllipticalArcPath()
            {
                //get the center of rotation
                PointPlus pt1 = tangencyPoint1 - Corner;
                pt1.Theta += Angle.FromDegrees(90);
                pt1 += tangencyPoint1;
                PointPlus pt2 = tangencyPoint2 - Corner;
                pt2.Theta += Angle.FromDegrees(90);
                pt2 += tangencyPoint2;

                Utils.FindIntersection(tangencyPoint1, pt1, tangencyPoint2, pt2, out Point c1, out Angle a);
                PointPlus center = new(c1);

                //get the lengths of the major and minor axes
                float r1 = (center - tangencyPoint1).R;
                float r2 = (center - tangencyPoint2).R;

                //get the angle of the major axis

                //draw the arc from the major to the minor axis

                //circular arc
                Polyline poly = new Polyline()
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                };
                //poly.Points.Add(center);

                PointPlus ptCurr = tangencyPoint1 - center;
                PointPlus ptLast = tangencyPoint2 - center;
                if (ptCurr.Theta - ptLast.Theta > Angle.FromDegrees(180))
                { ptLast.Theta += Angle.FromDegrees(360); }
                if (ptLast.Theta - ptCurr.Theta > Angle.FromDegrees(180))
                { ptCurr.Theta += Angle.FromDegrees(360); }
                float steps = (ptLast.Theta - ptCurr.Theta) / Angle.FromDegrees(5);
                if (steps > 0)
                {
                    for (int i = 0; i < steps; i++)
                    {
                        Angle theta = i * Angle.FromDegrees(5);
                        //heres the formula for a function defining an ellipse in terms of r(theta)
                        //TODO: this only really works on circular arcs
                        float r = r1 * r2 / (float)Math.Sqrt((r2 * r2 * Cos(theta) * Cos(theta)) + (r1 * r1 * Sin(theta) * Sin(theta)));
                        PointPlus pp = new(r, theta + ptCurr.Theta);
                        poly.Points.Add(center + pp);
                    }
                }
                else
                {
                    for (int i = 0; i < Abs(steps); i++)
                    {
                        Angle theta = i * Angle.FromDegrees(5);
                        float r = r1 * r2 / (float)Math.Sqrt((r2 * r2 * Math.Cos(theta) * Math.Cos(theta)) + (r1 * r1 * Math.Sin(theta) * Math.Sin(theta)));
                        PointPlus pp = new(r, theta + ptLast.Theta);
                        poly.Points.Add(center + pp);
                    }
                }
                return poly;
            }
        }




        Line tempLine;
        private void E_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Rectangle e1)
            {
                if (tempLine != null)
                    theCanvas.Children.Remove(tempLine);

                var xx = e1.ToolTip.ToString();
                xx = xx[xx.IndexOf("(")..];
                xx = xx.Replace("(", "");
                xx = xx.Replace(")", "");
                string[] coords = xx.Split(",");
                int.TryParse(coords[0], out int rho);
                int.TryParse(coords[1], out int theta);
                ModuleVision parent = (ModuleVision)base.ParentModule;
                errorText.Content = $"{(int)rho},{(int)theta} : {parent.segmentFinder.accumulator[rho, theta].Count} votes";
                errorText.Foreground = new SolidColorBrush(Colors.White);
                rho = rho - parent.segmentFinder.maxDistance;

                if (theta == 0 || theta == 180)
                {
                    tempLine = new Line()
                    {
                        X1 = rho * scale,
                        X2 = rho * scale,
                        Y1 = 0 * scale,
                        Y2 = theCanvas.ActualHeight * scale,
                        Stroke = new SolidColorBrush(Colors.Blue),
                        StrokeThickness = 4,
                    };
                }
                else
                {

                    //calculate (m,b) for y=mx+b
                    double fTheta = theta * Math.PI / parent.segmentFinder.numAngles;
                    double b = rho / Math.Sin(fTheta);
                    double m = -Math.Cos(fTheta) / Math.Sin(fTheta);
                    tempLine = new Line()
                    {
                        X1 = 0,
                        X2 = 1000 * scale,
                        Y1 = b * scale,
                        Y2 = (b + m * 1000) * scale,
                        Stroke = new SolidColorBrush(Colors.Blue),
                        StrokeThickness = 4,
                    };
                }
                theCanvas.Children.Add(tempLine);
            }
        }

        string defaultDirectory = "";
        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            if (defaultDirectory == "")
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(MainWindow.currentFileName);
            }
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image Files| *.png;*.jpg",
                Title = "Select an image file",
                Multiselect = true,
                InitialDirectory = defaultDirectory,
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                ModuleVision parent = (ModuleVision)base.ParentModule;

                textBoxPath.Text = openFileDialog1.FileName;
                List<string> fileList;
                string curPath;
                if (openFileDialog1.FileNames.Length > 1)
                {
                    fileList = new List<string>(openFileDialog1.FileNames);
                    curPath = fileList[0];
                }
                else
                {
                    fileList = GetFileList(openFileDialog1.FileName);
                    curPath = openFileDialog1.FileName;
                }
                parent.previousFilePath = "";
                parent.currentFilePath = curPath;
                //parent.SetParameters(fileList, curPath, (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
            }
        }

        private List<string> GetFileList(string filePath)
        {
            //"using System.IO" conflicts with graphics
            System.IO.SearchOption subFolder = System.IO.SearchOption.AllDirectories;
            //if (!(bool)cbSubFolders.IsChecked)
            //    subFolder = SearchOption.TopDirectoryOnly;
            string dir = filePath;
            System.IO.FileAttributes attr = System.IO.File.GetAttributes(filePath);
            if ((attr & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory)
                dir = System.IO.Path.GetDirectoryName(filePath);
            return new List<string>(System.IO.Directory.EnumerateFiles(dir, "*.png", subFolder));
        }

        private void cb_Checked(object sender, RoutedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleVision parent = (ModuleVision)base.ParentModule;
            parent.previousFilePath = "";
        }

        private void ModuleBaseDlg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }

        private void ModuleBaseDlg_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.R || e.Key == Key.F5)
                Button_Click(null, null);
        }
    }
}
