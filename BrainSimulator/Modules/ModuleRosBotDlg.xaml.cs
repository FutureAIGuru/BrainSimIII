//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
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
    public partial class ModuleRosBotDlg : ModuleBaseDlg
    {
        public ModuleRosBotDlg()
        {
            InitializeComponent();
            Rot.Value = 90;
            TiltM2.Value = 180;
            TiltM3.Value = 0;
            TiltM4.Value = 0;
            RotM5.Value = 90;
            GripM6.Value = 45;
        }
        static double yaw = 0;
        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            //this has a timer so that no matter how often you might call draw, the dialog
            //only updates 10x per second

            //use a line like this to gain access to the parent's public variables
            //ModuleEmpty parent = (ModuleEmpty)base.ParentModule;

            //here are some other possibly-useful items
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;            
            if (parent.botToUse.zSense.currentValue != yaw)
            {
                yaw = parent.botToUse.zSense.currentValue;
                Yaw.Content = yaw;                
            }
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }
        //######################################ArmMovement################################################################
        private void Rot_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(1, (int)Rot.Value);
        }
        private void TiltM2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(2, (int)TiltM2.Value);
        }
        private void TiltM3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(3, (int)TiltM3.Value);
        }
        private void TiltM4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(4, (int)TiltM4.Value);
        }
        private void RotM5_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(5, (int)RotM5.Value);
        }
        private void GripM6_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
                parent.moveArm(6, (int)GripM6.Value);
        }
        private void ResetArm_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)                
            Rot.Value = 90;
            TiltM2.Value = 180;
            TiltM3.Value = 0;
            TiltM4.Value = 0;
            RotM5.Value = 90;
            GripM6.Value = 45;
        }
        private void ResetArmwoGrip_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent != null)
            Rot.Value = 90;
            TiltM2.Value = 180;
            TiltM3.Value = 0;
            TiltM4.Value = 0;
            RotM5.Value = 90;            
        }
        private void Extend_Click(object sender, RoutedEventArgs e)
        {
            TiltM4.Value += 5;
            TiltM2.Value -= 5;
        }
        private void Retract_Click(object sender, RoutedEventArgs e)
        {
            TiltM4.Value -= 5;
            TiltM2.Value += 5;
        }
        //###################################EndArmMovement################################################################
        //#####################################BaseMovement################################################################
        float speed = (float)0.4;
        private void right_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if(parent != null)
                parent.moveBase(1, -speed);
        }

        private void left_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if( parent != null)
                parent.moveBase(1, speed);
        }

        private void fwd_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if(parent != null)
                parent.moveBase(0, speed);
        }

        private void bck_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if( parent != null )
                parent.moveBase(0, -speed);
        }
        private void stop_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if( parent!=null )
                parent.moveBase(0, (float)0.0);
        }

        private void TurnCCW_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBase(2, speed*3);
        }

        private void TurnCW_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBase(2,-speed*3);
        }
        private void D1_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBaseDiagonal(speed*3, speed*3);                
        }
        private void D2_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBaseDiagonal(speed*3, -speed*3);
        }
        private void D3_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBaseDiagonal(-speed*3, speed*3);
        }
        private void D4_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
                parent.moveBaseDiagonal(-speed*3, -speed*3);
        }

        private void SetIP_Click(object sender, RoutedEventArgs e)
        {
            ModuleRosBot parent = (ModuleRosBot)base.ParentModule;
            if (parent!=null)
            {
                string handle = ipBox.Text;
                parent.botToUse.setBotIP(handle);
            }
        }
        //##########################EndBaseMovement##############################################################
    }
}