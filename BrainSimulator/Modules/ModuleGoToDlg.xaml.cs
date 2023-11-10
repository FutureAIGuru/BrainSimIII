//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

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

namespace BrainSimulator.Modules
{
    public partial class ModuleGoToDlg : ModuleBaseDlg
    {
        public ModuleGoToDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theCanvas.Children.Clear();
            //Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

       

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            ModuleGoTo moduleGoTo = (ModuleGoTo)ParentModule;
            moduleGoTo.GetUKS();
            Thing mentalModel = moduleGoTo.UKS.Labeled("MentalModel");
            if (mentalModel == null) return;

            cbThings.Items.Clear();
            foreach (Thing child in mentalModel.Children)
            {
                string s = child.Label;
                var props = child.GetRelationshipsAsDictionary();
                if (props.ContainsKey("col") && props.ContainsKey("siz"))
                {
                    s += " [" + props["col"] + ", " + props["siz"] + "]";
                    cbThings.Items.Add(s);
                }
            }

        }

        private void ButtonGoToThing_Click(object sender, System.EventArgs e)
        {
            ModuleGoTo moduleGoTo = (ModuleGoTo)ParentModule;
            string s = cbThings.Text;
            if (s == "") return;
            int i = s.IndexOf(" ");
            if (i > 0)
                s = s.Substring(0, i);
            moduleGoTo.GetUKS();
            Thing mentalModel = moduleGoTo.UKS.Labeled("MentalModel");
            if (mentalModel == null) return;
            Thing target = moduleGoTo.UKS.Labeled(s, mentalModel.Children);
            if (target == null) return;
            var t = target.GetRelationshipsAsDictionary();
            if (t.ContainsKey("cen"))
            {
                Point3DPlus temp = (Point3DPlus)t["cen"];
                moduleGoTo.DirectlyToPoint(temp);
            }
        }
        private void ButtonRouteToThing_Click(object sender, System.EventArgs e)
        {
            ModuleGoTo moduleGoTo = (ModuleGoTo)ParentModule;
            string s = cbThings.Text;
            if (s == "") return;
            int i = s.IndexOf(" ");
            if (i > 0)
                s = s.Substring(0, i);
            moduleGoTo.GetUKS();
            Thing mentalModel = moduleGoTo.UKS.Labeled("MentalModel");
            if (mentalModel == null) return;
            Thing target = moduleGoTo.UKS.Labeled(s, mentalModel.Children);
            if (target == null) return;
            moduleGoTo.RouteToThing(target);
        }

    }
}