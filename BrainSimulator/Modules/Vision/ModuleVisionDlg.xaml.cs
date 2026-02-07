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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Math;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using static BrainSimulator.Modules.ModuleVision;


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
                    "\r\nSegments: " + parent.segments?.Count +
                    "\r\nCorners: " + parent.corners?.Count +
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
                                    ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 50, Content = $"({(int)x},{(int)y}) {lum.ToString("0.00")}" },
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
                            ToolTip = new System.Windows.Controls.ToolTip
                            {
                                HorizontalOffset = 70,
                                Content = $"({pt.X.ToString("0.0")},{pt.Y.ToString("0.0")})"
                            },
                        };
                        Canvas.SetLeft(e, pt.X * scale - pixelSize / 4);
                        Canvas.SetTop(e, pt.Y * scale - pixelSize / 4);
                        theCanvas.Children.Add(e);
                    }
                }


                //draw the segments
                if (cbShowSegments.IsChecked == true && parent.segments is not null & parent.segments?.Count > 0)
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
                            StrokeThickness = 8,
                            Opacity = .5,
                            ToolTip = new System.Windows.Controls.ToolTip
                            {
                                Content = $"{segment.debugIndex}:({segment.P1.X.ToString("0.0")},{segment.P1.Y.ToString("0.0")}) - " +
                                $"-({segment.P2.X.ToString("0.0")},{segment.P2.Y.ToString("0,0")})"
                            },
                        };
                        theCanvas.Children.Add(l);
                    }
                }

                //draw the corners
                if (cbShowCorners.IsChecked == true && parent.corners != null && parent.corners.Count > 0)
                {
                    for (int i = 0; i < parent.corners.Count; i++)
                    {
                        var corner = parent.corners[i];
                        float size = 15;
                        Brush b = Brushes.LightBlue;
                        if (Abs(corner.angle.Degrees - 180) < .1 ||
                            Abs(corner.angle.Degrees - -180) < .1)
                            b = Brushes.Pink;
                        Ellipse e = new Ellipse()
                        {
                            Height = size,
                            Width = size,
                            Stroke = b,
                            Fill = b,
                            ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"{i}", },
                        };
                        Canvas.SetTop(e, corner.pt.Y * scale - size / 2);
                        Canvas.SetLeft(e, corner.pt.X * scale - size / 2);
                        theCanvas.Children.Add(e);

                        //test out drawing little lines to represent the angle (then an elliptical arc, soon)
                        int i1 = 3;
                        PointPlus delta = corner.prevPt - corner.pt;
                        delta.R = i1;
                        PointPlus pt1 = corner.pt + delta;

                        if (!corner.curve)
                        {
                            Line l = new Line()
                            {
                                X1 = corner.pt.X * scale,
                                Y1 = corner.pt.Y * scale,
                                X2 = corner.prevPt.X * scale,
                                Y2 = corner.prevPt.Y * scale,
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
                                X2 = corner.nextPt.X * scale,
                                Y2 = corner.nextPt.Y * scale,
                                Stroke = Brushes.DarkGray,
                                StrokeThickness = 2,
                            };
                            theCanvas.Children.Add(l);
                        }
                        if (corner is ModuleVision.Arc a)
                        {
                            Corner alreadyInList = parent.corners.FindFirst(x =>
                                x != corner && x.curve && 
                                ((x.nextPt - corner.nextPt).R < 3.5 ||
                                 (x.prevPt - corner.nextPt).R < 3.5));
                            if (alreadyInList == null)
                            {
                                e = new Ellipse()
                                {
                                    Height = size,
                                    Width = size,
                                    Stroke = b,
                                    Fill = Brushes.Pink,
                                };
                                Canvas.SetTop(e, corner.nextPt.Y * scale - size / 2);
                                Canvas.SetLeft(e, corner.nextPt.X * scale - size / 2);
                                theCanvas.Children.Add(e);
                            }
                            alreadyInList = parent.corners.FindFirst(x =>
                                x != corner && x.curve &&
                                ((x.nextPt - corner.prevPt).R < 3.5 ||
                                 (x.prevPt - corner.prevPt).R < 3.5));
                            if (alreadyInList == null)
                            {
                                e = new Ellipse()
                                {
                                    Height = size,
                                    Width = size,
                                    Stroke = b,
                                    Fill = Brushes.Pink,
                                    ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"{i}", },
                                };
                                Canvas.SetTop(e, corner.prevPt.Y * scale - size / 2);
                                Canvas.SetLeft(e, corner.prevPt.X * scale - size / 2);
                                theCanvas.Children.Add(e);
                            }

                            var cir = a.GetCircleFromThreePoints(corner.pt * scale, corner.nextPt * scale, corner.prevPt * scale);
                            Angle startAngle = (corner.prevPt * scale - cir.center).Theta.Normalize();
                            Angle midAngle = (corner.pt * scale - cir.center).Theta.Normalize();
                            Angle endAngle = (corner.nextPt * scale - cir.center).Theta.Normalize();
                            if ((startAngle < midAngle && endAngle < midAngle) ||
                                (startAngle > midAngle && endAngle > midAngle))
                            {
                                if (endAngle < startAngle)
                                    endAngle += 2 * PI;
                                else
                                    (endAngle, startAngle) = (startAngle, endAngle);
                            }
                            else if (endAngle < startAngle)
                            {
                                (startAngle, endAngle) = (endAngle, startAngle);
                            }
                            var path1 = DrawArc(cir.center, cir.radius, startAngle, endAngle);
                            if (path1 != null)
                                theCanvas.Children.Add(path1);
                        }
                    }
                }
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
                //parent.previousFilePath = "";
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
            if (sender is CheckBox cb)
            {
                ModuleVision parent = (ModuleVision)base.ParentModule;
                if (parent == null) return;
                bool cbState = cb.IsChecked == true;
                switch (cb.Content)
                {
                    case "Horiz": parent.horizScan = cbState; parent.previousFilePath = ""; break;
                    case "Vert": parent.vertScan = cbState; parent.previousFilePath = ""; break;
                    case "45": parent.fortyFiveScan = cbState; parent.previousFilePath = ""; break;
                    case "-45": parent.minusFortyFiveScan = cbState; parent.previousFilePath = ""; break;
                }
            }
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

        private void ModuleBaseDlg_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //TODO, make bitmapsize a variable here
            //TODO, at max scale, do not change offsetX,Y
            ModuleVision parent = (ModuleVision)base.ParentModule;
            parent.scale *= 1 + e.Delta / 1000f;

            parent.offsetX = +25 + (int)((parent.offsetX - 25) * (1 + e.Delta / 1000f));
            parent.offsetY = +25 + (int)((parent.offsetY - 25) * (1 + e.Delta / 1000f));
            if (parent.scale < 1) parent.scale = 1;
            parent.LoadImageFileToPixelArray(parent.currentFilePath);

            ResetTimer();
        }

        private Point prevPoint;
        private void ModuleBaseDlg_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (prevPoint == null)
                    prevPoint = new();
                else
                {
                    Point diff = pos - (Vector)prevPoint;
                    ModuleVision parent = (ModuleVision)base.ParentModule;
                    parent.offsetX += (int)diff.X / 5;
                    parent.offsetY += (int)diff.Y / 5;

                    parent.LoadImageFileToPixelArray(parent.currentFilePath);
                    ResetTimer();
                }
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
            ModuleVision parent = (ModuleVision)base.ParentModule;
            parent.previousFilePath = "";
        }

    }
}
