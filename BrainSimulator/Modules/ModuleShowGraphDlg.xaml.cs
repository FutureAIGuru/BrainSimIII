//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved

using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System.Drawing; // for Color
using System.Windows;
using UKS;


namespace BrainSimulator.Modules
{
    public partial class ModuleShowGraphDlg : ModuleBaseDlg
    {
        public ModuleShowGraphDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second
            ModuleShowGraph parent = (ModuleShowGraph)base.ParentModule;
            //DrawTheGraph();
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //DrawTheGraph();
        }

        private void DrawTheGraph()
        {
            //get the root to save the contents of from the UKS dialog root
            string root = "Object";
            ModuleShowGraph parent = (ModuleShowGraph)base.ParentModule;
            Thing uksDlg = parent.theUKS.Labeled("ModuleUKS0");
            if (uksDlg != null)
                foreach (var r in uksDlg.Relationships)
                {
                    if (r.reltype.Label == "hasAttribute" && r.target.Label.StartsWith("Root"))
                    {
                        root = (string)r.target.V;
                    }
                }

            var viewer = new GViewer();

            var g = new Graph();

            g.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Black;
            viewer.OutsideAreaBrush = Brushes.Black;

            Thing theRoot = parent.theUKS.Labeled(root);
            foreach (Thing t in theRoot.Descendents)
            {
                foreach (Relationship r in t.Relationships)
                {
                    if (r.source == theRoot) continue;
                    string label = r.reltype.Label;
                    foreach (Clause c in r.Clauses)
                        label += $"\n{c.clauseType.Label} {c.clause.source.Label} {c.clause.reltype.Label} {c.clause.target.Label}";
                    var e = g.AddEdge(t.Label, label, r.target.Label);
                    e.Attr.Color = Microsoft.Msagl.Drawing.Color.Yellow;
                    e.Label.FontColor = Microsoft.Msagl.Drawing.Color.White;
                    e.Label.FontSize = 6;
                    e.SourceNode.Attr.Color = Microsoft.Msagl.Drawing.Color.Pink;
                    e.SourceNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightBlue;
                    //e.SourceNode.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Circle; //doesn't really work


                    e.TargetNode.Attr.Color = Microsoft.Msagl.Drawing.Color.Pink;
                    e.TargetNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightBlue;
                    //e.TargetNode.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Circle;
                }
            }

            viewer.Graph = g;
            wfHost.Child = viewer;   //
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            DrawTheGraph();
        }
    }
}

