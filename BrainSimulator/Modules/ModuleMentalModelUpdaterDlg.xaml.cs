//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainSimulator.Modules
{
    public partial class ModuleMentalModelUpdaterDlg : ModuleBaseDlg
    {
        public ModuleMentalModelUpdaterDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;

            ModuleMentalModelUpdater parent = (ModuleMentalModelUpdater)base.ParentModule;
            if (parent.UKS == null) return false;

            Thing tFrameToProcess = parent.UKS.Labeled("FrameNow");
            if (tFrameToProcess == null) return false;

            if (parent == null || parent.frameObjectsToProcess == null) return false;
            if (parent.frameObjectsToProcess.Count == 0)
            {
                theCanvas.Children.Clear();
                return false;
            }

            int count = parent.frameObjectsToProcess.Count;

            theCanvas.Children.Clear();
            for (int i = 0; i < count; i++)
            {
                Thing t = parent.frameObjectsToProcess[i];
                AddUnknownAreaToCanvas(t.V as UnknownArea, false);
            }
            
            return true;
        }

        public bool AddUnknownAreaToCanvas(UnknownArea a, bool setTitle)
        {
            string colorName = Utils.GetColorNameFromHSL(a.AvgColor);
            Polyline pl = new Polyline()
            {
                Stroke = new SolidColorBrush(a.AvgColor.ToColor()),
                StrokeThickness = 4,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
            if (a.AreaSegments.Count > 0)
            {
                foreach (SegmentTwoD s in a.AreaSegments)
                {
                    pl.Points.Add(a.GetDrawingPoint(s.p1.X, s.p1.Y));
                }
                pl.Points.Add(a.GetDrawingPoint(a.AreaSegments[0].p1.X, a.AreaSegments[0].p1.Y));
                theCanvas.Children.Add(pl);
            }
            if (setTitle)
            {
                Title += System.Math.Sqrt(a.Area) + ", ";
            }

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

    }
}