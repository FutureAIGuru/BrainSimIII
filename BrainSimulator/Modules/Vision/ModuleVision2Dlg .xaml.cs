//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using UKS;
using static BrainSimulator.Modules.ModuleVision;
using static System.Math;


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

            scale = (int)(theCanvas.ActualHeight / (parent.imageArray.GetLength(1) + 1));
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
                    for (int x = 0; x < parent.boundaryArray.GetLength(0); x++)
                        for (int y = 0; y < parent.boundaryArray.GetLength(1); y++)
                        {
                            var pixel = parent.boundaryArray[x, y];

                            if (pixel == true)
                            {
                                SolidColorBrush b = new SolidColorBrush(Colors.White);
                                string toolTipString = $"({(int)x},{(int)y}) ";
                                Rectangle e = new()
                                {
                                    Height = pixelSize,
                                    Width = pixelSize,
                                    Stroke = b,
                                    Fill = b,
                                    ToolTip = new System.Windows.Controls.ToolTip
                                    { HorizontalOffset = 100, Content = toolTipString },
                                };
                                Canvas.SetLeft(e, x * scale + pixelSize / 2);
                                Canvas.SetTop(e, y * scale + pixelSize / 2);
                                e.Tag = toolTipString;
                                e.MouseRightButtonDown += E_MouseRightButtonDown;
                                theCanvas.Children.Add(e);
                            }
                        }
                }


                //draw the boxes & contents
                if (cbShowBoxes.IsChecked == true && parent.boundaryArray != null)
                {
                    List<Thing> thingsRecentlyFired = theUKS.UKSList.FindAll(x => x.Label.StartsWith("Patch") &&
                        x.lastFiredTime != new DateTime(0)); //> DateTime.Now - TimeSpan.FromSeconds(10));
                    (float,float) [,] maxWeights = new (float,float)[parent.boundaryArray.GetLength(0), parent.boundaryArray.GetLength(1)];
                    for (int i = 0; i < maxWeights.GetLength(0); i++)
                        for (int j = 0; j < maxWeights.GetLength(1); j++)
                            maxWeights[i, j] = (-1,-1);

                    foreach (Thing t in thingsRecentlyFired)
                    {
                        string[] parts = t.Label.Split('_');
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        SolidColorBrush b = new SolidColorBrush(Colors.Pink);
                        string toolTipString = t.Parents[0].Label[0] + t.Label + " c:" + t.confidence;
                        Rectangle e = new()
                        {
                            Height = pixelSize * (parent.patchSize - .3),
                            Width = pixelSize * (parent.patchSize - .3),
                            Stroke = b,
                            StrokeThickness = 12,
                            //Fill = new SolidColorBrush(Colors.Transparent),
                            ToolTip = new System.Windows.Controls.ToolTip
                            { HorizontalOffset = 100, Content = toolTipString },
                        };
                        Canvas.SetLeft(e, (x + .65f - parent.patchSize / 2) * scale);
                        Canvas.SetTop(e, (y + .65f - parent.patchSize / 2) * scale);
                        theCanvas.Children.Add(e);
                        e.MouseRightButtonDown += E_MouseRightButtonDown;
                        e.Tag = toolTipString;

                        foreach (Relationship pt in t.Relationships)
                        {
                            if (pt.reltype.Label != "hasBoundary") continue;
                            parts = pt.target.Label.Split('_');
                            x = int.Parse(parts[1]);
                            y = int.Parse(parts[2]);
                            if (pt.Weight > maxWeights[x, y].Item1)
                                maxWeights[x, y] = (pt.Weight,maxWeights[x,y].Item2);  //this is to handle overlapping patches
                            if (pt.maxWeight > maxWeights[x, y].Item2)
                                maxWeights[x, y] = (maxWeights[x, y].Item1,pt.maxWeight);  //this is to handle overlapping patches
                        }
                    }

                    for (int x = 0; x < maxWeights.GetLength(0); x++)
                        for (int y = 0; y < maxWeights.GetLength(1); y++)
                        {
                            float theWeight = maxWeights[x, y].Item1;
                            float theMaxWeight = maxWeights[x, y].Item2;
                            //if (theWeight != 0)
                            {
                                SolidColorBrush b = new SolidColorBrush(RainbowColorFromValue(theWeight));
                                string toolTipString = $"({(int)x},{(int)y}) w: {theWeight:F2}  max: {theMaxWeight:F2}  ";
                                Rectangle e = new()
                                {
                                    Height = pixelSize / 2,
                                    Width = pixelSize / 2,
                                    Stroke = b,
                                    Fill = b,
                                    ToolTip = new System.Windows.Controls.ToolTip
                                    { HorizontalOffset = 100, Content = toolTipString },
                                };
                                Canvas.SetLeft(e, x * scale + 3 * pixelSize / 4);
                                Canvas.SetTop(e, y * scale + 3 * pixelSize / 4);
                                theCanvas.Children.Add(e);
                                e.MouseRightButtonDown += E_MouseRightButtonDown;
                                e.Tag = toolTipString;
                            }
                        }
                }

            }
            catch { }
            return true;
        }

        private void E_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle r && r.Tag != null)
                SetStatus(r.Tag.ToString());
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
            if (sender is Button b)
            {
                ModuleVision2 parent = (ModuleVision2)base.ParentModule;
                if (b.Content.ToString() == "Line")
                {
                        parent.SingteTestPattern();
                }
                if (b.Content.ToString() == "100")
                {
                    for (int i = 0; i < 1000; i++)
                        parent.SingteTestPattern();
                }
                if (b.Content.ToString() == "Refresh")
                {
                    parent.Refresh();
                }
                if (b.Content.ToString() == "Prune")
                {
                    parent.Prune();
                }
                if (b.Content.ToString() == "Init")
                {
                    parent.InitArray();
                }
                if (b.Content.ToString() == "Clear")
                {
                    parent.ClearBoundaryArray();
                }
            }
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


        //helper to make rainbow colors
        // Map a value to a rainbow color.
        public static Color RainbowColorFromValue(float value) //value has a range -1,1
        {
            // Convert into a value between 0 and 1023.
            int int_value = (int)(1023 * value);

            if (int_value < -1022) //fully negative
            {
                return Colors.Black;
            }
            else if (int_value >= 1023) //fully positive
            {
                return Colors.White;
            }
            else if (int_value == 0) //0 (blue)
            {
                return Colors.Blue;
            }
            else if (int_value < 0) // -1,0 graysacle
            {
                int_value = (1024 - (Math.Abs(int_value) / 2) + 512) / 4;
                return Color.FromRgb((byte)int_value, (byte)int_value, (byte)int_value);
            }

            int_value = 1023 - int_value;
            // Map different color bands.
            if (int_value < 256)
            {
                // Red to yellow. (255, 0, 0) to (255, 255, 0).
                return Color.FromRgb(255, (byte)int_value, 0);
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return Color.FromRgb((byte)(255 - int_value), 255, 0);
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return Color.FromRgb(0, 255, (byte)int_value);
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return Color.FromRgb(0, (byte)(255 - int_value), 255);
            }
        }


    }
}
