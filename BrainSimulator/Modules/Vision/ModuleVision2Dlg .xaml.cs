//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Math;
using System.Windows.Threading;
using static BrainSimulator.Modules.ModuleVision;
using UKS;


namespace BrainSimulator.Modules
{
    public partial class ModuleVision2Dlg : ModuleBaseDlg
    {
        // Constructor of the ModuleUKSStatement dialog
        public ModuleVision2Dlg()
        {
            InitializeComponent();
        }

        // Draw gets called to draw the dialog when it needs refreshing
        int scale;
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleVision2 parent = (ModuleVision2)base.ParentModule;
            var theUKS = parent.theUKS;

            if (parent.imageArray == null) return false;
            try
            {
                labelProperties.Content = "Image: " + parent.imageArray.GetLength(0) + "x" + parent.imageArray.GetLength(1) +
                    "\r\nSegments: " + parent.segments?.Count +
//                    "\r\nCorners: " + parent.corners?.Count +
                    "\r\nOutlines: " + parent.theUKS.Labeled("Outline")?.Children.Count;
            }
            catch { return false; }

            theCanvas.Children.Clear();

            scale = (int)(theCanvas.ActualHeight / parent.imageArray.GetLength(1));
            int pixelSize = scale - 2;
            if (pixelSize < 2) pixelSize = 2;

            try
            {
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
                                float lum = new HSLColor(pixel).luminance;
                                Rectangle e = new()
                                {
                                    Height = pixelSize,
                                    Width = pixelSize,
                                    Stroke = b,
                                    Fill = b,
                                    ToolTip = new System.Windows.Controls.ToolTip
                                    { HorizontalOffset = 100, Content = $"({(int)x},{(int)y}) {lum.ToString("0.00")}" },
                                };
                                Canvas.SetLeft(e, x * scale + pixelSize / 2);
                                Canvas.SetTop(e, y * scale + pixelSize / 2);
                                theCanvas.Children.Add(e);
                            }
                        }
                }

                //new showBoundaries (old version below)
                if (cbShowBoundaries.IsChecked == true && parent.boundaryArray != null)
                {
                    for (int x = 0; x < parent.imageArray.GetLength(0); x++)
                        for (int y = 0; y < parent.imageArray.GetLength(1); y++)
                        {
                            var pixel = parent.boundaryArray[x, y];
                            
                            if (pixel == true)
                            {
                                //pixel.luminance /= 2;
                                SolidColorBrush b = new SolidColorBrush(Colors.White);
                                Rectangle e = new()
                                {
                                    Height = pixelSize,
                                    Width = pixelSize,
                                    Stroke = b,
                                    Fill = b,
                                    ToolTip = new System.Windows.Controls.ToolTip
                                    { HorizontalOffset = 100, Content = $"({(int)x},{(int)y}) " },
                                };
                                Canvas.SetLeft(e, x * scale + pixelSize / 2);
                                Canvas.SetTop(e, y * scale + pixelSize / 2);
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
                            Stroke = Brushes.DarkGreen,
                            Fill = Brushes.DarkGreen,
                            ToolTip = new System.Windows.Controls.ToolTip
                            {
                                HorizontalOffset = 100,
                                Content = $"({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")})"
                            },
                        };
                        Canvas.SetLeft(e, pt.X * scale - pixelSize / 4);
                        Canvas.SetTop(e, pt.Y * scale - pixelSize / 4);
                        theCanvas.Children.Add(e);
                    }
                }
                //draw the strokes
                if (cbShowCenterPts.IsChecked == true && parent.CenterLinePts != null)
                {
                    foreach (var pt in parent.CenterLinePts)
                    {
                        Rectangle e = new()
                        {
                            Height = pixelSize / 2,
                            Width = pixelSize / 2,
                            Stroke = Brushes.DarkRed,
                            Fill = Brushes.DarkRed,
                            ToolTip = new System.Windows.Controls.ToolTip
                            {
                                HorizontalOffset = 100,
                                Content = $"({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")})"
                            },
                        };
                        Canvas.SetLeft(e, pt.X * scale - pixelSize / 4);
                        Canvas.SetTop(e, pt.Y * scale - pixelSize / 4);
                        theCanvas.Children.Add(e);
                    }
                }


                //draw the boxes & contents
                if (cbShowBoxes.IsChecked == true && parent.boundaryArray != null)
                {
                    List<Thing> thingsRecentlyFired = theUKS.UKSList.FindAll(x => x.Label.StartsWith("Box") &&
                        x.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(10));
                    float[,] maxWeights = new float[parent.boundaryArray.GetLength(0), parent.boundaryArray.GetLength(1)];

                    foreach (Thing t in thingsRecentlyFired)
                    {
                        string[] parts = t.Label.Split('_');
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        SolidColorBrush b = new SolidColorBrush(Colors.Pink);
                        Rectangle e = new()
                        {
                            Height = pixelSize*(parent.squareSize+1),
                            Width = pixelSize*(parent.squareSize+1),
                            Stroke = b,
                            StrokeThickness=4,
                            //Fill = b,
                            ToolTip = new System.Windows.Controls.ToolTip
                            { HorizontalOffset = 100, Content = t.Parents[0].Label[0]+ t.Label },
                        };
                        Canvas.SetLeft(e, (x+.5f) * scale - pixelSize / 2);
                        Canvas.SetTop(e, (y+.5f) * scale - pixelSize / 2);
                        theCanvas.Children.Add(e);

                        foreach (Relationship pt in t.Relationships)
                        {
                            if (pt.reltype.Label != "hasBoundary") continue;
                            parts = pt.target.Label.Split('_');
                            x = int.Parse(parts[1]);
                            y = int.Parse(parts[2]);
                            if (pt.Weight > maxWeights[x, y]) 
                                maxWeights[x, y] = pt.Weight;
                        }
                    }

                    for (int x = 0; x < maxWeights.GetLength(0); x++)
                        for(int y = 0; y < maxWeights.GetLength(1); y++)
                        {
                            float theWeight = maxWeights[x, y];
                            if (theWeight != 0)
                            {
                                SolidColorBrush b = new SolidColorBrush(Colors.Green);
                                if (theWeight < 1) b = new SolidColorBrush(Colors.LightGreen);
                                Rectangle e = new()
                                {
                                    Height = pixelSize/2,
                                    Width = pixelSize/2,
                                    Stroke = b,
                                    Fill = b,
                                    ToolTip = new System.Windows.Controls.ToolTip
                                    { HorizontalOffset = 100, Content = $"({(int)x},{(int)y}) " },
                                };
                                Canvas.SetLeft(e, x * scale + 3* pixelSize / 4);
                                Canvas.SetTop(e, y * scale + 3*pixelSize / 4);
                                theCanvas.Children.Add(e);
                            }
                        }
                }

                ////draw the corners
                //if (cbShowCorners.IsChecked == true && parent.corners != null && parent.corners.Count > 0)
                //{
                //    for (int i = 0; i < parent.corners.Count; i++)
                //    {
                //        var corner = parent.corners[i];
                //        float size = 15;
                //        Brush b = Brushes.LightBlue;
                //        if (Abs(corner.angle.Degrees - 180) < .1 ||
                //            Abs(corner.angle.Degrees - -180) < .1)
                //            b = Brushes.Pink;
                //        Ellipse e = new Ellipse()
                //        {
                //            Height = size,
                //            Width = size,
                //            Stroke = b,
                //            Fill = b,
                //            ToolTip = new ToolTip { HorizontalOffset = 100, Content = $"{i}", },
                //        };
                //        Canvas.SetTop(e, corner.pt.Y * scale - size / 2);
                //        Canvas.SetLeft(e, corner.pt.X * scale - size / 2);
                //        theCanvas.Children.Add(e);

                //        //test out drawing little lines to represent the angle (then an elliptical arc, soon)
                //        int i1 = 3;
                //        PointPlus delta = corner.prevPt - corner.pt;
                //        delta.R = i1;
                //        PointPlus pt1 = corner.pt + delta;

                //        if (!corner.curve)
                //        {
                //            ModuleVision2.Corner target = parent.corners.FindFirst(x => x.pt == corner.prevPt);
                //            if (target != null && !target.curve)
                //            {
                //                Line l1 = new Line()
                //                {
                //                    X1 = corner.pt.X * scale,
                //                    Y1 = corner.pt.Y * scale,
                //                    X2 = corner.prevPt.X * scale,
                //                    Y2 = corner.prevPt.Y * scale,
                //                    Stroke = Brushes.DarkGray,
                //                    StrokeThickness = 4,
                //                };
                //                theCanvas.Children.Add(l1);
                //            }
                //            delta = corner.nextPt - corner.pt;
                //            delta.R = i1;
                //            PointPlus pt2 = corner.pt + delta;

                //            target = parent.corners.FindFirst(x => x.pt == corner.nextPt);
                //            if (target != null && !target.curve)
                //            {
                //                Line l2 = new Line()
                //                {
                //                    X1 = corner.pt.X * scale,
                //                    Y1 = corner.pt.Y * scale,
                //                    X2 = corner.nextPt.X * scale,
                //                    Y2 = corner.nextPt.Y * scale,
                //                    Stroke = Brushes.DarkGray,
                //                    StrokeThickness = 4,
                //                };
                //                theCanvas.Children.Add(l2);
                //            }
                //        }
                //    }
                //}
            }
            catch { }
            return true;
        }


        public Polyline DrawArc(PointPlus center, float radius, Angle startAngle, Angle endAngle)
        {
            Angle angleStep = Angle.FromDegrees(1);
            Polyline poly = new Polyline()
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 3,
            };
            if (endAngle < startAngle)
            {
                endAngle += 2 * PI;
            }
            for (Angle a = startAngle; a <= endAngle; a += angleStep)
            {
                PointPlus pp = new(radius, a);
                poly.Points.Add(center + pp);
            }
            return poly;
        }

        string defaultDirectory = "";
        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            if (defaultDirectory == "")
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(MainWindow.currentFileName);
            }
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Image Files| *.png;*.jpg",
                Title = "Select an image file",
                Multiselect = true,
                InitialDirectory = defaultDirectory,
            };
            // Show the Dialog.  
            // If the user clicked OK in the dialog  
            System.Windows.Forms.DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                defaultDirectory = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                ModuleVision2 parent = (ModuleVision2)base.ParentModule;

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
                //parent.previousFilePath = "";
                parent.CurrentFilePath = curPath;
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
            if (sender is CheckBox cb)
            {
                ModuleVision2 parent = (ModuleVision2)base.ParentModule;
                if (parent == null) return;
                bool cbState = cb.IsChecked == true;
                //switch (cb.Content)
                //{
                //    case "Horiz": parent.horizScan = cbState; parent.previousFilePath = ""; break;
                //    case "Vert": parent.vertScan = cbState; parent.previousFilePath = ""; break;
                //    case "45": parent.fortyFiveScan = cbState; parent.previousFilePath = ""; break;
                //    case "-45": parent.minusFortyFiveScan = cbState; parent.previousFilePath = ""; break;
                //}
            }
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleVision2 parent = (ModuleVision2)base.ParentModule;
            parent.previousFilePath = null;
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

        private void ModuleBaseDlg_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //TODO, make bitmapsize a variable here
            //TODO, at max scale, do not change offsetX,Y
            ModuleVision2 parent = (ModuleVision2)base.ParentModule;
            parent.scale *= 1 + e.Delta / 1000f;

            parent.offsetX = +25 + (int)((parent.offsetX - 25) * (1 + e.Delta / 1000f));
            parent.offsetY = +25 + (int)((parent.offsetY - 25) * (1 + e.Delta / 1000f));
            if (parent.scale < 1) parent.scale = 1;
            parent.LoadImageFileToPixelArray(parent.CurrentFilePath);

            ResetTimer();
        }

        private Point prevPoint;
        private void ModuleBaseDlg_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //if (prevPoint == null)
                //    prevPoint = new();
                //else
                //{
                //    Point diff = pos - (Vector)prevPoint;
                //    ModuleVision parent = (ModuleVision)base.ParentModule;
                //    parent.offsetX += (int)diff.X / 5;
                //    parent.offsetY += (int)diff.Y / 5;

                //    parent.LoadImageFileToPixelArray(parent.CurrentFilePath);
                //    ResetTimer();
                //}
            }
            prevPoint = pos;
        }

        private DispatcherTimer refreshTimer = null;
        private void ResetTimer()
        {
            if (refreshTimer == null)
            {
                refreshTimer = new DispatcherTimer();
                refreshTimer.Tick += RefreshTimer_Tick;
                refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
            }
            refreshTimer.Stop();
            refreshTimer.Start();
        }
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            refreshTimer.Stop();
            ModuleVision2 parent = (ModuleVision2)base.ParentModule;
            parent.previousFilePath = "";
        }

    }
}
