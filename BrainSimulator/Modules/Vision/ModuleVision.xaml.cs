//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;


using System.Windows.Shapes;

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
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleVision parent = (ModuleVision)base.ParentModule;

            if (parent.bitmap == null) return false;
            labelProperties.Content = "Image: " + Math.Round(parent.bitmap.Width) + "x" + Math.Round(parent.bitmap.Height) +
                "\r\nBit Depth: " + parent.bitmap.Format.BitsPerPixel +
                "\r\nSegments: " + parent.segments.Count +
                "\r\nCorners: " + parent.corners.Count;


            theCanvas.Children.Clear();

            int pixelSize = 6;
            int scale = (int)(theCanvas.ActualHeight / parent.imageArray.GetLength(1));

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
                        {
                            var pixel = new HSLColor(parent.imageArray[x, y]);

                            if (pixel != null)
                            {
                                pixel.luminance /= 2;
                                SolidColorBrush b = new SolidColorBrush(pixel.ToColor());
                                Ellipse e = new Ellipse()
                                {
                                    Height = pixelSize,
                                    Width = pixelSize,
                                    Stroke = b,
                                    ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"({(int)x},{(int)y}) {pixel}" },
                                };
                                Canvas.SetLeft(e, x * scale - pixelSize / 2);
                                Canvas.SetTop(e, y * scale - pixelSize / 2);
                                theCanvas.Children.Add(e);
                            }
                        }
                    }
            }

            //draw the boundaries
            if (cbShowBoundaries.IsChecked == true && parent.boundaryArray != null)
            {
                for (int x = 0; x < parent.boundaryArray.GetLength(0); x++)
                    for (int y = 0; y < parent.boundaryArray.GetLength(1); y++)
                    {
                        if (parent.boundaryArray[x, y] != 0)
                        {
                            Ellipse e = new Ellipse()
                            {
                                Height = pixelSize,
                                Width = pixelSize,
                                Stroke = Brushes.Black,
                                Fill = Brushes.Black,
                                ToolTip = new System.Windows.Controls.ToolTip { HorizontalOffset = 100, Content = $"({(int)x},{(int)y})" },
                            };
                            Canvas.SetLeft(e, x * scale - pixelSize / 2);
                            Canvas.SetTop(e, y * scale - pixelSize / 2);
                            theCanvas.Children.Add(e);
                        }
                    }
            }

            //draw the lines
            if (cbShowLines.IsChecked == true && parent.segments != null & parent.segments.Count > 0)
            {
                for (int i = parent.segmentFinder.localMaxima.Count-1; i >=0 ; i--)
                {
                    Tuple<int, int, int> line = parent.segmentFinder.localMaxima[i];
                    float maxVotes = parent.segmentFinder.localMaxima[0].Item1;
                    float votes = line.Item1;
                    int minVodes = 10;
                    if (votes < minVodes) continue;
                    float intensity = (votes - minVodes) / maxVotes;
                    HSLColor hSLColor = new HSLColor(Colors.Green);
                    hSLColor.luminance = intensity;
                    Brush brush = new SolidColorBrush(hSLColor.ToColor());
                    int rho = line.Item2 - parent.segmentFinder.maxDistance;
                    int theta = line.Item3;
                    if (theta == 0 || theta == 180) //line is vertical
                    {
                        Line l = new Line()
                        {
                            X1 = rho * scale,
                            X2 = rho * scale,
                            Y1 = 0 * scale,
                            Y2 = theCanvas.ActualHeight * scale,
                            Stroke = brush,
                        };
                        theCanvas.Children.Add(l);
                    }
                    else
                    {
                        //calculate (m,b) for y=mx+b
                        double fTheta = theta * Math.PI / parent.segmentFinder.numAngles;
                        double b = rho / Math.Sin(fTheta);
                        double m = -Math.Cos(fTheta) / Math.Sin(fTheta);
                        Line l = new Line()
                        {
                            X1 = 0,
                            X2 = 1000 * scale,
                            Y1 = b * scale,
                            Y2 = (b+m*1000)* scale,
                            Stroke = brush,
                        };
                        theCanvas.Children.Add(l);
                    }
                }
            }

            //draw the segments
            if (cbShowSegments.IsChecked == true && parent.segments != null & parent.segments.Count > 0)
            {
                foreach (var segment in parent.segments)
                {
                    Line l = new Line()
                    {
                        X1 = segment.P1.X * scale,
                        X2 = segment.P2.X * scale,
                        Y1 = segment.P1.Y * scale,
                        Y2 = segment.P2.Y * scale,
                        Stroke = Brushes.Red,
                        StrokeThickness = 4,
                    };
                    theCanvas.Children.Add(l);
                }
            }


            //draw the corners
            if (cbShowCorners.IsChecked == true && parent.corners != null && parent.corners.Count > 0)
            {
                foreach (var corner in parent.corners)
                {
                    float size = 15;
                    Brush b = Brushes.White;
                    if (corner.angle == 0)
                        b = Brushes.Pink;
                    Ellipse e = new Ellipse()
                    {
                        Height = size,
                        Width = size,
                        Stroke = b,
                        Fill = b
                    };
                    Canvas.SetTop(e, corner.location.Y * scale - size / 2);
                    Canvas.SetLeft(e, corner.location.X * scale - size / 2);
                    theCanvas.Children.Add(e);
                }
            }

            return true;
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

                parent.currentFilePath = curPath;
                //parent.SetParameters(fileList, curPath, (bool)cbAutoCycle.IsChecked, (bool)cbNameIsDescription.IsChecked);
            }
        }

        private List<string> GetFileList(string filePath)
        {
            SearchOption subFolder = SearchOption.AllDirectories;
            //if (!(bool)cbSubFolders.IsChecked)
            //    subFolder = SearchOption.TopDirectoryOnly;
            string dir = filePath;
            FileAttributes attr = File.GetAttributes(filePath);
            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                dir = System.IO.Path.GetDirectoryName(filePath);
            return new List<string>(Directory.EnumerateFiles(dir, "*.png", subFolder));
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
    }
}
