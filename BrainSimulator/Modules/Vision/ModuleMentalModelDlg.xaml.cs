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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using UKS;

namespace BrainSimulator.Modules
{
    public partial class ModuleMentalModelDlg : ModuleBaseDlg
    {
        public ModuleMentalModelDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            ModuleMentalModel parent = (ModuleMentalModel)base.ParentModule;

            Thing mentalModel = parent.theUKS.Labeled("MentalModel");
            if (mentalModel == null) return false;
            //if (environmentModel.lastFiredTime < DateTime.Now - TimeSpan.FromSeconds(1)) return false;

            int pixelSize = 6;
            int scale = (int)(theCanvas.ActualHeight / 25);
            theCanvas.Children.Clear();

            Thing currentShape = parent.theUKS.Labeled("CurrentShape");
            if (currentShape == null) return false;
            foreach (Thing t in currentShape?.Children)
            {
                Thing shape = t.GetAttribute("storedshape");
                Thing size = t.GetAttribute("size");
                Thing position = t.GetAttribute("position");
                Thing rotation = t.GetAttribute("rotation");
                Thing mmOffset = t.GetAttribute("offset");
                Thing aColor = t.GetAttribute("color");
                if (mmOffset == null || rotation == null || position == null || size == null || shape == null)
                    return false;

                HSLColor theColor = new HSLColor(Colors.Pink);
                if (aColor != null)
                    theColor = new HSLColor((Color)aColor.V);

                //hacks to get a few properties which are stashed in labels
                int offsetVal = int.Parse(mmOffset.Label[9..]);
                float sizeVal = float.Parse(size.Label.Replace("size", ""));
                string[] temp1 = position.Label.Split(':');
                string[] temp2 = temp1[1].Split(',');
                float x1 = float.Parse(temp2[0]) * scale / 4 + 10;
                float y1 = float.Parse(temp2[1]) * scale / 4 + 10;
                PointPlus curPos = new(x1, y1);
                Angle currDir = Angle.FromDegrees(float.Parse(rotation.Label[6..]));

                Polyline poly = new Polyline()
                {
                    Fill = new SolidColorBrush(theColor.ToColor()),
                    Stroke = new SolidColorBrush(Colors.Green),
                };
                poly.Points.Add(curPos);

                foreach (Relationship r in t.Relationships)
                {
                    if (!r.reltype.Label.StartsWith("go")) continue;
                    if (r.target.HasAncestor("Rotation"))
                    {
                        Angle theta = Angle.FromDegrees(float.Parse(r.target.Label[5..]));
                        currDir += theta;
                    }
                    else if (r.target.HasAncestor("Distance"))
                    {
                        try
                        {
                            float dist = float.Parse(r.target.Label[8..]) * scale * 2.5f;
                            PointPlus offset = new PointPlus(dist * sizeVal, currDir);
                            curPos += offset;
                            poly.Points.Add(curPos);
                        }
                        catch { }
                    }
                }
                if (poly.Points.Count < 2) //there is no recenetly-refreshed data, use the stored shape
                {
                    theColor.saturation /= 2;
                    if (offsetVal < 0) offsetVal = 0;
                    poly.Fill = new SolidColorBrush(theColor.ToColor());
                    if (shape != null)
                    {
                        for (int i = 0; i < shape.Relationships.Count; i++)
                        {
                            Relationship r = shape.Relationships[(i + offsetVal) % shape.Relationships.Count];
                            if (!r.reltype.Label.StartsWith("go")) continue;
                            if (r.target.HasAncestor("Rotation"))
                            {
                                Angle theta = Angle.FromDegrees(float.Parse(r.target.Label[5..]));
                                currDir += theta;
                            }
                            else if (r.target.HasAncestor("Distance"))
                            {
                                float dist = float.Parse(r.target.Label[8..]) * scale * 2.5f;
                                PointPlus offset = new PointPlus(dist * sizeVal, currDir);
                                curPos += offset;
                                poly.Points.Add(curPos);
                            }
                        }
                    }
                }
                theCanvas.Children.Add(poly);

                for (int x = 0; x < 25; x++)
                    for (int y = 0; y < 25; y++)
                    {
                        string name = $"mm{x},{y}";
                        Thing pixel = parent.theUKS.Labeled(name);
                        if (pixel == null) continue;
                        Color pixelColor = (Color)pixel.V;

                        if (pixelColor != null)
                        {
                            //pixelColor.luminance /= 2;
                            SolidColorBrush b = new SolidColorBrush(pixelColor);
                            Ellipse e = new Ellipse()
                            {
                                Height = pixelSize,
                                Width = pixelSize,
                                Stroke = b,
                                Fill = b,
                            };
                            Canvas.SetLeft(e, 10 + x * scale - pixelSize / 2);
                            Canvas.SetTop(e, 10 + y * scale - pixelSize / 2);
                            theCanvas.Children.Add(e);
                        }
                    }

            }

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }
    }
}
