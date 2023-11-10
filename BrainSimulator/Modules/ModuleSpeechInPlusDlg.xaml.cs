//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System;
using Microsoft.Win32;

namespace BrainSimulator.Modules
{
    public partial class ModuleSpeechInPlusDlg : ModuleBaseDlg
    {
        private bool FirstTimeDrawn = true;

        public ModuleSpeechInPlusDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return false;

            if (parent.text2dialog.Length > 0)
            {
                TypedEntryTextBox.Text = parent.text2dialog;
                parent.text2dialog = "";
            }

            if (!parent.speechEnabled || parent.speechRecognitionPaused)
            {
                listenStatusImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/RedCircle.png"));
                listenStatusLabel.Content = "Not Listening";
            }
            else if (parent.keywordRecognition)
            {
                listenStatusImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/YellowCircle.png"));
                listenStatusLabel.Content = "Wake Word";
            }
            else
            {
                listenStatusImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/GreenCircle.png"));
                listenStatusLabel.Content = "Open Mic";
            }

            cbWakeWords.SelectedItem = parent.wakeWord;

            if (!parent.speechEnabled)
            {
                cbEnabled.IsChecked = false;
            }
            else cbEnabled.IsChecked = true;

            if (!parent.remoteMicEnabled)
            {
                rbLocal.IsChecked = true;
            }
            else
                rbRemote.IsChecked = true;

            if (FirstTimeDrawn)
            {
                Set_cbWakeWords();
                FirstTimeDrawn = false;
            }

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
            //(false);
        }

        public string GetEntryText()
        {
            return TypedEntryTextBox.Text;
        }

        List<string> history = new();
        int historyPointer = -1;

        public void AddToHistory(string s)
        {
            history.Add(s);
            historyPointer = history.Count;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //this.Cursor = Cursors.Wait;
                //System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
                parent.ReceiveInputFromText(TypedEntryTextBox.Text);
                history.Add(TypedEntryTextBox.Text);
                TypedEntryTextBox.Text = "";
                historyPointer = history.Count;
            }
            if (e.Key == Key.Up)
            {
                if (historyPointer <= 0)
                    return;
                TypedEntryTextBox.Text = history[historyPointer - 1];
                historyPointer--;
            }
            if (e.Key == Key.Down)
            {
                if (historyPointer >= history.Count - 1)
                    return;
                historyPointer++;
                TypedEntryTextBox.Text = history[historyPointer];
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;
            parent.speechEnabled = true;
            parent.ResumeRecognition();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;
            parent.speechEnabled = false;
            parent.PauseRecognition();
        }

        private void RadioButton_Local_Click(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;
            parent.remoteMicEnabled = false;
            parent.Initialize();
        }

        private void RadioButton_Remote_Click(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;
            parent.remoteMicEnabled = true;
            parent.Initialize();
        }

        private void ComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
            Set_cbWakeWords();
        }

        public void Set_cbWakeWords()
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;
            string filePath = parent.FilePath;
            string fileSuffix = parent.FileSuffix;

            cbWakeWords.Items.Clear();
            foreach (var file in Directory.GetFiles(filePath))
            {
                string fileName = file.ToString().Replace(filePath, "").Replace(fileSuffix, "");
                cbWakeWords.Items.Add(file.ToString().Replace(filePath, "").Replace(fileSuffix, ""));
                parent.UKS?.GetOrAddThing(fileName, "WakeWord");
            }
        }

        public List<string> GetCBWakeWords()
        {
            List<string> wakewords = new();
            foreach (string name in cbWakeWords.Items)
            {
                wakewords.Add(name);
            }
            return wakewords;
        }

        private void cbWakeWords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            if (parent == null) return;

            if (e.AddedItems.Count == 0)
            {
                return;
            }
            string name = e.AddedItems[0].ToString();
            if (parent.wakeWord != name)
                parent.SetWakeWord(name);
            parent.Initialize();
        }
        //OPEN COMMAND FILE
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SuspendEngine();
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                InitialDirectory = Utils.GetOrAddLocalSubFolder(Utils.FolderCommandFiles),
            };
            if (openFileDialog.ShowDialog() != true)
            {
                MainWindow.ResumeEngine();
                return;
            }
            string[] lines = File.ReadAllLines(openFileDialog.FileName);

            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;

            parent.SetLinesFromFile(lines);
            //foreach ( string line in lines )
            //{
            //    if ( line.StartsWith("//") ) continue;
            //    parent.ReceiveInputFromText(line);
            //}
            MainWindow.ResumeEngine();
            if (cbRun.IsChecked == true)
                parent.run = 2;
            else
                parent.run = 1;
        }


        private void TypedEntryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void TypedEntryTextBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

        }
        //STEP
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            parent.run = 1;
        }
        //RUN
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            parent.run = 2;
        }
        //CANCEL
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
            parent.SetLinesFromFile(new string[0]);
        }
        //cbRUN
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                ModuleSpeechInPlus parent = (ModuleSpeechInPlus)base.ParentModule;
                if (cb.IsChecked == true)
                {
                    parent.run = 2;
                }
                else
                    parent.run = 0;
            }
        }
    }
}
