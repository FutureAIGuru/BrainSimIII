//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using UKS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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

            Thing environmentModel = parent.theUKS.Labeled("Environment");
            if (environmentModel == null) return false;
            //if (environmentModel.lastFiredTime < DateTime.Now - TimeSpan.FromSeconds(1)) return false;

            int pixelSize = 6;
            int scale = (int)(theCanvas.ActualHeight / 25);
            theCanvas.Children.Clear();

            foreach (Thing t in parent.theUKS.Labeled("CurrentShape").Children)
            {
                var shapeList = t.GetRelationshipsWithAncestor("storedshape");
                var sizeLists = t.GetRelationshipsWithAncestor("size");
                var positionList = t.GetRelationshipsWithAncestor("mmposition");
                var rotationList = t.GetRelationshipsWithAncestor("mmrotation");
                if (sizeLists.Count == 0) continue;

                float size = float.Parse(sizeLists[0].target.Label.Replace("size", ""));
                string[] temp1 = positionList[0].target.Label.Split(':');
                string[] temp2 = temp1[1].Split(',');
                float x1 = float.Parse(temp2[0]) * scale / 4 + 10;
                float y1 = float.Parse(temp2[1]) * scale / 4 + 10;
                PointPlus curPos = new(x1, y1);
                var rels = shapeList[0].target.Relationships;
                Angle currDir = Angle.FromDegrees(float.Parse(rotationList[0].target.Label[6..]));

                HSLColor theColor = new HSLColor(Colors.Pink);
                Thing t1 = t.AttributeOfType("hasColor");
                if (t1 != null)
                    theColor = (HSLColor)t1.V;
                Polyline poly = new Polyline() 
                {
                    Fill=new SolidColorBrush(theColor.ToColor()), 
                    Stroke=new SolidColorBrush(Colors.Green),
                };
                poly.Points.Add(curPos);

                foreach (Relationship r in t.Relationships)
                {
                    if (!r.reltype.Label.StartsWith("go")) continue;
                    if (r.target.HasAncestor("Angle"))
                    {
                        Angle theta = Angle.FromDegrees(float.Parse(r.target.Label[5..]));
                        currDir += Angle.FromDegrees(180) - theta;
                    }
                    else if (r.target.HasAncestor("Distance"))
                    {
                        float dist = float.Parse(r.target.Label[8..]) * scale * 2.5f;
                        PointPlus offset = new PointPlus(dist * size, currDir);
                        curPos += offset;
                        poly.Points.Add(curPos);
                    }
                }
                theCanvas.Children.Add(poly);

                for (int x = 0; x < 25; x++)
                    for (int y = 0; y < 25; y++)
                    {
                        string name = $"mm{x},{y}";
                        Thing pixel = parent.theUKS.Labeled(name);
                        if (pixel == null) continue;
                        HSLColor pixelColor = (HSLColor)pixel.V;

                        if (pixelColor != null)
                        {
                            //pixelColor.luminance /= 2;
                            SolidColorBrush b = new SolidColorBrush(pixelColor.ToColor());
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
