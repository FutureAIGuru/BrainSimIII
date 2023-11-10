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
using System.IO;

namespace BrainSimulator.Modules
{
    public partial class ModulePodAudioDlg : ModuleBaseDlg
    {
        public ModulePodAudioDlg()
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
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            //float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            //if (scale == 0) return false;
            double scaleY = windowSize.Y / 10000000f;

            ModulePodAudio parent = (ModulePodAudio)base.ParentModule;

            polyline.Stroke = new SolidColorBrush(Colors.Black);
            polyline.Points.Clear();
            polyline.Points.Add(new Point(0, windowCenter.Y));
            polyline.Points.Add(new Point(windowSize.X, windowCenter.Y));
            polyline.Points.Add(new Point(0, windowCenter.Y));
            for (int i = 0; i < parent.waveFormBuffer.Length; i++)
            {
                double x = i * windowSize.X / parent.waveFormBuffer.Length;
                double y = windowCenter.Y + parent.waveFormBuffer[i] * scaleY;
                polyline.Points.Add(new Point(x, y));
            }
            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModulePodAudio podAudio = (ModulePodAudio)base.ParentModule;

            if (podAudio == null) return;
            if (cbAudioFiles.SelectedItem == null) return;
            podAudio.PlaySoundEffect(cbAudioFiles.SelectedItem.ToString());
        }

        private void Record_ButtonClick(object sender, RoutedEventArgs e)
        {
            ModulePodAudio podAudio = (ModulePodAudio)base.ParentModule;
            if (RecordMic.Content == "Record Remote Mic")
            {
                podAudio.recordMic = true;
                RecordMic.Content = "Stop Recording";
            } else
            {
                podAudio.recordMic = false;
                RecordMic.Content = "Record Remote Mic";
            }
        }

        private void ComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
            Set_cbAudioFiles();
        }

        private void Set_cbAudioFiles()
        {
            ModulePodAudio parent = (ModulePodAudio)base.ParentModule;
            if (parent == null) return;
            string filePath = Utils.GetOrAddLocalSubFolder(Utils.FolderAudioFiles) + "\\";

            cbAudioFiles.Items.Clear();
            foreach (var file in Directory.GetFiles(filePath))
            {
                cbAudioFiles.Items.Add(file.ToString().Replace(filePath, ""));
            }
        }
        
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            ModulePodAudio parent = (ModulePodAudio)base.ParentModule;
            if (parent == null) return;
            parent.ClearAudioQueue();
        }
    }
}