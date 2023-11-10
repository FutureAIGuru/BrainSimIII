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
using System.IO;
using System;

namespace BrainSimulator.Modules
{
    public partial class ModuleUI_IntercomDlg : ModuleBaseDlg
    {

        public const int sliderNumTicks = 4;
        public const int startingVolume = 4;

        bool speechWasEnabled = false;

        public ModuleUI_IntercomDlg()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            volumeSlider.Maximum = sliderNumTicks;

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModulePodAudio modulePodAudio = (ModulePodAudio)parent.FindModule(typeof(ModulePodAudio));
            if (modulePodAudio == null) return;

            volumeSlider.Value = startingVolume;
            modulePodAudio.volume = startingVolume == 0 ? 0 : 1 / Math.Pow(2, Math.Abs(volumeSlider.Value - (sliderNumTicks + 1)));
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

            SetMicLevel();

            return true;
        }

        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        bool expanded = false;
        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            expanded = ((ModuleUserInterfaceDlg)Owner).ExpandCollapseWindow(this.GetType());
            if (expanded)
            {
                theChrome.ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
                theChrome.CaptionHeight = 40;
                ExpandButton.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else
            {
                WindowState = WindowState.Normal;
                ((ModuleUserInterfaceDlg)Owner).MoveChildren();
                theChrome.ResizeBorderThickness = new Thickness(0);
                theChrome.CaptionHeight = 0;
                CollapseButton.Visibility = Visibility.Collapsed;
                ExpandButton.Visibility = Visibility.Visible;
            }
        }

        private void Dlg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box on other pages
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(true);
                    //prevent arrow keys from doing anything else on the home page
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(false);
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = true;
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = true;
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box
            //if (tabHome.IsSelected == false) return;

            switch (e.Key)
            {
                case Key.Up:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Down:
                    moduleInputControl.StopForwardBack();
                    e.Handled = true;
                    break;
                case Key.Left:
                    moduleInputControl.turnLeft = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
                case Key.Right:
                    moduleInputControl.turnRight = false;
                    moduleInputControl.StopTurn();
                    e.Handled = true;
                    break;
            }
        }

        private void Dlg_Deactivated(object sender, System.EventArgs e)
        {
            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        private void IntercomOffSwitch_Click(object sender, RoutedEventArgs e)
        {
            IntercomOffSwitch.Visibility = Visibility.Collapsed;
            IntercomOnSwitch.Visibility = Visibility.Visible;

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleIntercom moduleIntercom = (ModuleIntercom)parent.FindModule(typeof(ModuleIntercom));
            if (moduleIntercom == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            speechWasEnabled = moduleSpeechInPlus.speechEnabled;
            moduleSpeechInPlus.speechEnabled = false;
            moduleSpeechInPlus.PauseRecognition();
            moduleIntercom.SetListenToPod(true);
        }

        private void IntercomOnSwitch_Click(object sender, RoutedEventArgs e)
        {
            IntercomOnSwitch.Visibility = Visibility.Collapsed;
            IntercomOffSwitch.Visibility = Visibility.Visible;

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleIntercom moduleIntercom = (ModuleIntercom)parent.FindModule(typeof(ModuleIntercom));
            if (moduleIntercom == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.speechEnabled = speechWasEnabled;
            moduleSpeechInPlus.ResumeRecognition();
            moduleIntercom.SetListenToPod(false);
        }

        private void PushTalkButton_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            PushTalkButton.Background = new SolidColorBrush(Color.FromRgb(121,70,200));
            PushTalkLabel.Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235));

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleIntercom moduleIntercom = (ModuleIntercom)parent.FindModule(typeof(ModuleIntercom));
            if (moduleIntercom == null) return;

            if (IntercomOnSwitch.Visibility == Visibility.Visible)
            {
                moduleIntercom.SetListenToPod(false);
                moduleIntercom.SendMicrophoneToPod(true);
            }
        }

        private void PushTalkButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            DisablePushTalk();
        }

        private void PushTalkButton_MouseLeave(object sender, MouseEventArgs e)
        {
            DisablePushTalk();
        }

        private void DisablePushTalk()
        {
            PushTalkButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            PushTalkLabel.Foreground = new SolidColorBrush(Color.FromRgb(121, 70, 200));

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModuleIntercom moduleIntercom = (ModuleIntercom)parent.FindModule(typeof(ModuleIntercom));
            if (moduleIntercom == null) return;

            if (IntercomOnSwitch.Visibility == Visibility.Visible)
            {
                moduleIntercom.SendMicrophoneToPod(false);
                moduleIntercom.SetListenToPod(true);
            }
        }

        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            PlaySoundButton.Background = new SolidColorBrush(Color.FromRgb(121, 70, 200));
            PlaySoundLabel.Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235));

            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;

            ModulePodAudio modulePodAudio = (ModulePodAudio)parent.FindModule(typeof(ModulePodAudio));
            object selectedSound = SoundsComboBox.SelectedItem;
            if (selectedSound != null)
                modulePodAudio.PlaySoundEffect(selectedSound.ToString(), Utils.FolderUIAudioFiles);
        }

        private void PlaySoundButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            PlaySoundButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            PlaySoundLabel.Foreground = new SolidColorBrush(Color.FromRgb(121, 70, 200));
        }

        private void PlaySoundButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            PlaySoundButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            PlaySoundLabel.Foreground = new SolidColorBrush(Color.FromRgb(121, 70, 200));
        }

        public void PopulateSounds()
        {
            string filePath = Utils.GetOrAddLocalSubFolder(Utils.FolderUIAudioFiles);

            foreach (var file in Directory.GetFiles(filePath))
            {
                SoundsComboBox.Items.Add(file.ToString().Replace(filePath + "\\", ""));
            }
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ModuleUI_Intercom parent = (ModuleUI_Intercom)ParentModule;
            if (parent == null) return;
            ModulePodAudio modulePodAudio = (ModulePodAudio)parent.FindModule(typeof(ModulePodAudio));
            if (modulePodAudio == null) return;

            modulePodAudio.volume = volumeSlider.Value == 0 ? 0 : 1 / Math.Pow(2, Math.Abs(volumeSlider.Value - (sliderNumTicks + 1)));
        }

        private void SetMicLevel()
        {
            ModuleUI_Intercom parent = (ModuleUI_Intercom)base.ParentModule;
            if (parent == null) return;
            ModulePodAudio modulePodAudio = (ModulePodAudio)parent.FindModule(typeof(ModulePodAudio));
            if (modulePodAudio == null) return;

            int maxVolume = 0;
            for (int i = 0; i < modulePodAudio.waveFormBuffer.Length; i++)
            {
                if (Math.Abs(modulePodAudio.waveFormBuffer[i]) > maxVolume)
                    maxVolume = Math.Abs(modulePodAudio.waveFormBuffer[i]);
            }

            List<Rectangle> volumeBars = new() { Volume1, Volume2, Volume3, Volume4, Volume5, Volume6, Volume7, Volume8 };

            for(int i = 0; i < volumeBars.Count; i++)
            {
                //if(avgVolume > Math.Pow(4, i + 1) / 2)
                if(maxVolume > 100000 + (i * 40000))
                    volumeBars[i].Visibility = Visibility.Visible;
                else
                    volumeBars[i].Visibility = Visibility.Collapsed;
            }
        }
    }
}