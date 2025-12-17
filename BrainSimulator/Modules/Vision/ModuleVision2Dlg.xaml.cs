//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UKS;
using static System.Math;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;


namespace BrainSimulator.Modules
{
    public partial class ModuleVision2Dlg : ModuleBaseDlg
    {
        int pixelSize = 1;
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

            if (Mouse.RightButton == MouseButtonState.Pressed) return false;

            if (parent.imageArray == null) return false;
            try
            {
                labelProperties.Content = "Image: " + parent.imageArray.GetLength(0) + "x" + parent.imageArray.GetLength(1) +
                    "\r\nOutlines: " + parent.theUKS.Labeled("Outline")?.Children.Count;
            }
            catch { return false; }

            theCanvas.Children.Clear();

            scale = (int)(theCanvas.ActualHeight / (parent.imageArray.GetLength(1) + 1));
            pixelSize = scale - 2;
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


                //draw the patch centers
                if (cbShowCenterPts.IsChecked == true && parent.boundaryArray != null)
                {
                    List<Thing> patchesRecentlyFired = theUKS.UKSList.FindAll(x => x.Label.StartsWith("patch") &&
                        x.lastFiredTime != new DateTime(0)); //> DateTime.Now - TimeSpan.FromSeconds(10));

                    for (int x = 2; x < parent.boundaryArray.GetLength(0) - 2; x++)
                        for (int y = 2; y < parent.boundaryArray.GetLength(1) - 2; y++)
                        {
                            SolidColorBrush b = new SolidColorBrush(Colors.Yellow);
                            string toolTipString = $"patch_{x:d2}_{y:d2}_0";
                            Rectangle e = new()
                            {
                                Height = pixelSize * .5,
                                Width = pixelSize * .5,
                                Stroke = b,
                                StrokeThickness = 12,
                                //Fill = new SolidColorBrush(Colors.Transparent),
                                ToolTip = new System.Windows.Controls.ToolTip
                                { HorizontalOffset = 100, Content = toolTipString },
                            };
                            Canvas.SetLeft(e, (x + .75f) * scale);
                            Canvas.SetTop(e, (y + .75f) * scale);
                            theCanvas.Children.Add(e);
                            e.MouseRightButtonDown += E_MouseRightButtonDown;
                            e.Tag = toolTipString;
                        }
                    foreach (Thing t in patchesRecentlyFired)
                    {
                        string[] parts = t.Label.Split('_');
                        if (parts.Length < 4) continue;
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        SolidColorBrush b = new SolidColorBrush(Colors.Pink);
                        string toolTipString = t.Label + " c:" + t.confidence;
                        Rectangle e = new()
                        {
                            Height = pixelSize * .5,
                            Width = pixelSize * .5,
                            Stroke = b,
                            StrokeThickness = 12,
                            //Fill = new SolidColorBrush(Colors.Transparent),
                            ToolTip = new System.Windows.Controls.ToolTip
                            { HorizontalOffset = 100, Content = toolTipString },
                        };
                        Canvas.SetLeft(e, (x + .75f) * scale);
                        Canvas.SetTop(e, (y + .75f) * scale);
                        theCanvas.Children.Add(e);
                        e.MouseRightButtonDown += E_MouseRightButtonDown;
                        e.Tag = toolTipString;
                    }
                }
                //draw the patches & contents
                if (cbShowPatches.IsChecked == true && parent.boundaryArray != null)
                {
                    List<Thing> thingsRecentlyFired = theUKS.UKSList.FindAll(x => x.Label.ToLower().StartsWith("patch") &&
                        x.lastFiredTime > DateTime.Now - TimeSpan.FromSeconds(3));
                    foreach (Thing t in thingsRecentlyFired)
                        DrawAPatch(t);
                }
                //draw the corner content
                if (cbShowCorners.IsChecked == true && parent.boundaryArray != null)
                {
                    List<Thing> cornersRecentlyFired = theUKS.UKSList.FindAll(x => x.Label.StartsWith("corner") &&
                        x.lastFiredTime != new DateTime(0)); //> DateTime.Now - TimeSpan.FromSeconds(10));

                    foreach (Thing t in cornersRecentlyFired)
                    {
                        string[] parts = t.Label.Split('_');
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        SolidColorBrush b = new SolidColorBrush(Colors.Green);
                        string toolTipString = t.Label + " c:" + t.confidence;
                        Rectangle e = new()
                        {
                            Height = pixelSize * .25,
                            Width = pixelSize * .25,
                            Stroke = b,
                            StrokeThickness = 12,
                            //Fill = new SolidColorBrush(Colors.Transparent),
                            ToolTip = new System.Windows.Controls.ToolTip
                            { HorizontalOffset = 100, Content = toolTipString },
                        };
                        Canvas.SetLeft(e, (x + .875f) * scale);
                        Canvas.SetTop(e, (y + .875f) * scale);
                        theCanvas.Children.Add(e);
                        e.MouseRightButtonDown += E_MouseRightButtonDown;
                        e.Tag = toolTipString;
                    }
                }
            }
            catch { }
            return true;
        }

        private void DrawPatchRelative(Thing t, PointPlus center, int patchSize)
        {
            float tScale = scale * .75f;
            string[] parts = t.Label.Split('_');
            int patchX = int.Parse(parts[1]);
            int patchY = int.Parse(parts[2]);
            SolidColorBrush b = new SolidColorBrush(Colors.Pink);
            Rectangle e = new()
            {
                Height = pixelSize * (patchSize - .3),
                Width = pixelSize * (patchSize - .3),
                Stroke = b,
                StrokeThickness = 12,
            };
            Canvas.SetLeft(e, (center.X + .65f - patchSize / 2) * tScale);
            Canvas.SetTop(e, (center.Y + .65f - patchSize / 2) * tScale);
            theCanvas.Children.Add(e);



            foreach (Relationship pt in t.Relationships.Where(x => x.relType.Label == "hasBoundary"))
            {
                parts = pt.target.Label.Split('_');
                float x = int.Parse(parts[1]);
                x = center.X - (patchX - x);
                float y = int.Parse(parts[2]);
                y = center.Y - (patchY - y);
                float theWeight = pt.Weight;
                b = new SolidColorBrush(RainbowColorFromValue(theWeight));
                e = new()
                {
                    Height = pixelSize / 2,
                    Width = pixelSize / 2,
                    Stroke = b,
                    Fill = b,
                };
                Canvas.SetLeft(e, x * tScale + 3 * pixelSize / 4);
                Canvas.SetTop(e, y * tScale + 3 * pixelSize / 4);
                theCanvas.Children.Add(e);
            }
            float val = GetPatchConfidence(t);
            Label tb = new() { Content = $"{val:F2}", FontSize = 10, };
            Canvas.SetLeft(tb, (center.X + 2 + .65f - patchSize / 2) * tScale);
            Canvas.SetTop(tb, (center.Y + 2 + .65f - patchSize / 2) * tScale);
            theCanvas.Children.Add(tb);
        }

        float GetPatchConfidence(Thing t)
        {
            if (!t.Label.StartsWith("patch")) return -1;
            float val = 0;

            foreach (Relationship r in t.Relationships.Where(x=>x.relType.Label == "hasBoundary"))
            {
                ModuleVision2 parent = (ModuleVision2)base.ParentModule;
                string[] parts = r.target.Label.Split('_');
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);
                if (parent.boundaryArray[x, y])
                    val += r.Weight;
            }

            return val;
        }

        private void DrawAPatch(Thing t)
        {
            ModuleVision2 parent = (ModuleVision2)base.ParentModule;
            string[] parts = t.Label.Split('_');
            int x = int.Parse(parts[1]);
            int y = int.Parse(parts[2]);
            SolidColorBrush b = new SolidColorBrush(Colors.Pink);
            string toolTipString = t.Label + " c:" + t.confidence;
            Rectangle e = new()
            {
                Height = pixelSize * (parent.patchSize - .3),
                Width = pixelSize * (parent.patchSize - .3),
                Stroke = b,
                StrokeThickness = 12,                //Fill = new SolidColorBrush(Colors.Transparent),
                ToolTip = new System.Windows.Controls.ToolTip
                { HorizontalOffset = 100, Content = toolTipString },
            };
            Canvas.SetLeft(e, (x + .65f - parent.patchSize / 2) * scale);
            Canvas.SetTop(e, (y + .65f - parent.patchSize / 2) * scale);
            theCanvas.Children.Add(e);
            e.MouseRightButtonDown += E_MouseRightButtonDown;
            e.Tag = toolTipString;


            foreach (Relationship pt in t.Relationships.Where(x => x.relType.Label == "hasBoundary"))
            {
                parts = pt.target.Label.Split('_');
                x = int.Parse(parts[1]);
                y = int.Parse(parts[2]);
                float theWeight = pt.Weight;
                b = new SolidColorBrush(RainbowColorFromValue(theWeight));
                toolTipString = $"({(int)x},{(int)y}) w: {theWeight:F2}  max: {pt.maxWeight:F2}  ";
                e = new()
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
            foreach (Relationship pt in t.Relationships.Where(x => x.relType.Label == "collinearWith"))
            {
                if (pt.Weight < .2f) continue;
                if (cbShowCorners.IsChecked != true) continue;
                parts = pt.target.Label.Split('_');
                x = int.Parse(parts[1]);
                y = int.Parse(parts[2]);
                float theWeight = pt.Weight;
                b = new SolidColorBrush(Colors.Red);
                toolTipString = $"({(int)x},{(int)y}) w: {theWeight:F2}  max: {pt.maxWeight:F2}  ";
                e = new()
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
            //draw a segmentwhich indicates the patch orientation based on the weights
            //the arrow will point in the direction of the strongest weight sum and go through the center of the patch
            DrawPatchOrientation(t);
        }

        void DrawPatchOrientation(Thing patch)
        {
            // 1. accumulate weighted direction
            Thing centerT = patch.Relationships.FindFirst(x => x.Weight == 1 && x.relType.Label == "hasBoundary").target;
            string[] parts = centerT.Label.Split('_');
            PointPlus center = new PointPlus(int.Parse(parts[1]), (float)int.Parse(parts[2]));

            float Sxx = 0f, Syy = 0f, Sxy = 0f;

            foreach (var rel in patch.Relationships.Where(x => x.relType.Label == "hasBoundary"))
            {
                string[] p = rel.target.Label.Split('_');
                float px = int.Parse(p[1]);
                float py = int.Parse(p[2]);

                float dx = px - center.X;
                float dy = (py - center.Y);  // correct Y orientation

                float w = rel.Weight;

                Sxx += w * dx * dx;
                Syy += w * dy * dy;
                Sxy += w * dx * dy;
            }

            // orientation axis angle
            float theta = 0.5f * (float)Math.Atan2(2 * Sxy, Sxx - Syy);

            int half = 2;
            float L = half; // length of line

            float dx2 = (float)Math.Cos(theta);
            float dy2 = (float)Math.Sin(theta);

            var p1 = new Point(center.X - dx2 * L, center.Y - dy2 * L);
            var p2 = new Point(center.X + dx2 * L, center.Y + dy2 * L);

            // 4. draw main line
            Brush b = new SolidColorBrush(Colors.Black);
            Line e = new()
            {
                X1 = p1.X * scale + 3 * pixelSize / 4,
                Y1 = p1.Y * scale + 3 * pixelSize / 4,
                X2 = p2.X * scale + 3 * pixelSize / 4,
                Y2 = p2.Y * scale + 3 * pixelSize / 4,
                Stroke = b,
                StrokeThickness = 6,
            };
            theCanvas.Children.Add(e);
        }


        private void E_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle r && r.Tag != null)
            {
                SetStatus(r.Tag.ToString());
                statusLabel.MouseRightButtonDown += StatusLabel_MouseRightButtonDown;
                if (r.Tag.ToString().StartsWith("patch"))
                {
                    ModuleVision2 parent = (ModuleVision2)base.ParentModule;
                    var theUKS = parent.theUKS;
                    var patchName = r.Tag.ToString().Split(' ')[0];
                    var t = theUKS.Labeled(patchName);
                    if (t != null)
                    {
                        //t.SetFired();
                        DrawAPatch(t);

                        string[] parts = t.Label.Split('_');
                        int patchX = int.Parse(parts[1]);
                        int patchY = int.Parse(parts[2]);

                        for (int i = 0; i < 8; i++)
                        {
                            string thingLabel = $"patch_{parts[1]}_{parts[2]}_{i}";
                            Thing t1 = theUKS.Labeled(thingLabel);
                            if (t1 != null)
                                DrawPatchRelative(t1, new PointPlus(20 + 5 * (i / 4), (float)(1 + 5 * (i % 4))), 5);
                        }
                    }
                }
                StatusLabel_MouseRightButtonDown(null, null);
            }
        }

        private void StatusLabel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            string s = statusLabel.Content.ToString().Split(' ')[0];

            //set the root of the UKS dieplsy
            var mod = MainWindow.theWindow.GetModule("ModuleUKS0");
            mod.SetSavedDlgAttribute("Root", s);
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
                if (b.Content.ToString() == "Test")
                {
                    parent.SingteTestPattern();
                }
                if (b.Content.ToString() == "100")
                {
                    //this doesn't work because everybody uses the same boundary array
                    //System.Threading.Tasks.Parallel.For(0, 1000, i => parent.SingteTestPattern());

                    //spawn the following as a separate thread so the UI can update
                    Task backgroundTask = Task.Run(() =>
                    {
                        for (int i = 0; i < 10000; i++)
                            parent.SingteTestPattern();
                    });
                }
                if (b.Content.ToString() == "Refresh")
                {
                    parent.Refresh();
                }
                if (b.Content.ToString() == "Show")
                {
                    parent.Show();
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                ModuleVision2 parent = (ModuleVision2)base.ParentModule;
                parent.testMethod = cb.SelectedIndex;
            }
        }
    }
}
