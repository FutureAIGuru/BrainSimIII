//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class ModulePodSensorDisplayDlg : ModuleBaseDlg
    {
        bool mouseInWindow = false;

        public ModulePodSensorDisplayDlg()
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
            //theGrid.Children.Clear();
            //Point windowSize = new Point(theGrid.ActualWidth, theGrid.ActualHeight);
            //Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;
            if ( !mouseInWindow )
            {
                UpdateSensorData();
                UpdateActuatorData();
            }
            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateSensorData();
        }

        private void UpdateActuatorData()
        {
            ModulePodSensorDisplay parent = (ModulePodSensorDisplay)base.ParentModule;
            List<string> actuators = parent.GetPodActuatorData();
            ActuatorDataGrid.Children.Clear();
            CreateActuatorTableHeader();
            
            for (int i = 1; i <= actuators.Count; i++)
            {
                List<string> parsedValues = actuators[i-1].Split(":").ToList();
                ActuatorDataGrid.RowDefinitions.Add(new());
                for (int j = 0; j < parsedValues.Count; j++)
                {
                    TextBlock txt = new();
                    txt.Margin = new Thickness(0, 1, 0, 1);
                    if (i % 2 == 1) txt.Background = new SolidColorBrush(Colors.DarkGray);
                    txt.Text = parsedValues[j];
                    Grid.SetColumn(txt, j);
                    Grid.SetRow(txt, i);
                    ActuatorDataGrid.Children.Add(txt);
                }
            }

        }

        private void UpdateSensorData()
        {
            ModulePodSensorDisplay parent = (ModulePodSensorDisplay)base.ParentModule;
            List<string> sensors = parent.GetPodSensorData();
            SensorDataGrid.Children.Clear();
            CreateSensorTableHeader();

            for (int i = 1; i <= sensors.Count; i++)
            {
                List<string> parsedValues = sensors[i-1].Split(":").ToList();
                SensorDataGrid.RowDefinitions.Add(new());
                for (int j = 0; j < parsedValues.Count; j++)
                {
                    TextBlock txt = new();
                    txt.Margin = new Thickness(0, 1, 0, 1);
                    if (i % 2 == 1) txt.Background = new SolidColorBrush(Colors.DarkGray);
                    txt.Text = parsedValues[j];
                    Grid.SetColumn(txt, j);
                    Grid.SetRow(txt, i);
                    SensorDataGrid.Children.Add(txt);
                }
            }
        }

        private void CreateSensorTableHeader()
        {
            SensorDataGrid.RowDefinitions.Add(new());
            TextBlock type = new();
            type.Text = "TYPE";
            Grid.SetColumn(type, 0);
            Grid.SetRow(type, 0);
            SensorDataGrid.Children.Add(type);

            TextBlock pin = new();
            pin.Text = "PIN";
            Grid.SetColumn(pin, 1);
            Grid.SetRow(pin, 0);
            SensorDataGrid.Children.Add(pin);

            TextBlock enabled = new();
            enabled.Text = "Enabled";
            Grid.SetColumn(enabled, 2);
            Grid.SetRow(enabled, 0);
            SensorDataGrid.Children.Add(enabled);

            TextBlock curValue = new();
            curValue.Text = "Cur Value";
            Grid.SetColumn(curValue, 3);
            Grid.SetRow(curValue, 0);
            SensorDataGrid.Children.Add(curValue);

            TextBlock prevValues = new();
            prevValues.Text = "Prev Value";
            Grid.SetColumn(prevValues, 4);
            Grid.SetRow(prevValues, 0);
            SensorDataGrid.Children.Add(prevValues);
        }

        private void CreateActuatorTableHeader()
        {
            ActuatorDataGrid.RowDefinitions.Add(new());
            TextBlock type = new();
            type.Text = "TYPE";
            Grid.SetColumn(type, 0);
            Grid.SetRow(type, 0);
            ActuatorDataGrid.Children.Add(type);

            TextBlock pin = new();
            pin.Text = "PIN";
            Grid.SetColumn(pin, 1);
            Grid.SetRow(pin, 0);
            ActuatorDataGrid.Children.Add(pin);

            TextBlock enabled = new();
            enabled.Text = "Enabled";
            Grid.SetColumn(enabled, 2);
            Grid.SetRow(enabled, 0);
            ActuatorDataGrid.Children.Add(enabled);

            TextBlock curValue = new();
            curValue.Text = "Cur Value";
            Grid.SetColumn(curValue, 3);
            Grid.SetRow(curValue, 0);
            ActuatorDataGrid.Children.Add(curValue);

            TextBlock timing = new();
            timing.Text = "Timing";
            Grid.SetColumn(timing, 4);
            Grid.SetRow(timing, 0);
            ActuatorDataGrid.Children.Add(timing);
        }
        
        private void TheTreeView_MouseEnter(object sender, MouseEventArgs e)
        {
            mouseInWindow = true;
            theGrid.Background = new SolidColorBrush(Colors.LightSteelBlue);
        }

        private void TheTreeView_MouseLeave(object sender, MouseEventArgs e)
        {
            mouseInWindow = false;
            theGrid.Background = null;
        }
    }
}