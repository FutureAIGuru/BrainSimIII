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
    public partial class ModuleUI_ConversationDlg : ModuleBaseDlg
    {
        public ModuleUI_ConversationDlg()
        {
            InitializeComponent();
        }

        List<string> speechInQueue = new();
        List<string> speechOutQueue = new();

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

            UpdateConversation();
         
            for (int i = 0; i < speechInQueue.Count; i++) DrawInput(speechInQueue[i]);
            for (int i = 0; i < speechOutQueue.Count; i++) DrawOutput(speechOutQueue[i]);
            speechInQueue.Clear();
            speechOutQueue.Clear();

            //DrawAudioInputText();
            //DrawAudioOutputText();

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
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box on other pages
            //if (tabHome.IsSelected == false) return;

            if (SpeechEntryTextBox.IsKeyboardFocused) return;

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
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            //let the arrow keys be used for other things like navigating in a text box
            //if (tabHome.IsSelected == false) return;

            if (SpeechEntryTextBox.IsKeyboardFocused) return;

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
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((ModuleUserInterfaceDlg)Owner).ClosePopups();
        }

        //private void DrawAudioInputText()
        //{
        //    ModuleUI_Conversation parent = (ModuleUI_Conversation)base.ParentModule;
        //    if (parent == null) return;

        //    ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
        //    if (moduleSpeechInPlus == null) return;

        //    TextInputTextBlock.Text = moduleSpeechInPlus.GetInputText();
        //}

        //private void DrawAudioOutputText()
        //{
        //    ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
        //    if (parent == null) return;

        //    ModuleSpeechOut moduleSpeechOut = (ModuleSpeechOut)parent.FindModule(typeof(ModuleSpeechOut));
        //    if (moduleSpeechOut == null) return;

        //    TextOutputTextBlock.Text = moduleSpeechOut.toSpeak;
        //}

        private void UpdateConversation()
        {
            while (conversationStack.Children.Count > 30) conversationStack.Children.RemoveAt(0);

            if (conversationScroller.VerticalOffset == conversationScroller.ScrollableHeight)
            {
                conversationScroller.ScrollToEnd();
            }
        }

        public void SetInput(string speechIn)
        {
            speechInQueue.Add(speechIn);
        }

        public void SetOutput(string speechOut)
        {
            speechOutQueue.Add(speechOut);
        }

        private void DrawInput(string speechIn)
        {
            TextBlock theText = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10,10,10,10), Text = speechIn,
                FontSize = 16, HorizontalAlignment = HorizontalAlignment.Left, Foreground = new SolidColorBrush(Color.FromRgb(102,102,102))};

            Style textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, this.FindResource("OpenSans-Bold")));
            theText.Style = textStyle;

            Border theBorder = new() { CornerRadius = new CornerRadius(15), HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(0), MinWidth = 50, MaxWidth = 300};
            theBorder.Background = new SolidColorBrush(Color.FromRgb(206,206,206));
            theBorder.Child = theText;

            BitmapImage bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.UriSource = new System.Uri("/Resources/UserInterface/TALK-BUBBLES-Pointer-User.png", System.UriKind.Relative);
            bitImage.EndInit();
            Image hook = new() { Source = bitImage, Width = 20, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(5,0,0,0) };

            Grid wordBubble = new() { Margin = new Thickness(30,0,0,0)};
            RowDefinition row1 = new();
            RowDefinition row2 = new();
            wordBubble.RowDefinitions.Add(row1);
            wordBubble.RowDefinitions.Add(row2);

            Grid.SetRow(theBorder, 0);
            Grid.SetRow(hook, 1);
            wordBubble.Children.Add(theBorder);
            wordBubble.Children.Add(hook);

            conversationStack.Children.Add(wordBubble);
        }

        private void DrawOutput(string speechOut)
        {
            TextBlock theText = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10,10,10,10), Text = speechOut,
                FontSize = 16, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush(Colors.White)};

            Style textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, this.FindResource("OpenSans-Bold")));
            theText.Style = textStyle;

            Border theBorder = new() { CornerRadius = new CornerRadius(15), HorizontalAlignment = HorizontalAlignment.Right,
                BorderThickness = new Thickness(0), MinWidth = 50, MaxWidth = 300};
            theBorder.Background = new SolidColorBrush(Color.FromRgb(107,172,246));
            theBorder.Child = theText;

            BitmapImage hookBitImage = new();
            hookBitImage.BeginInit();
            hookBitImage.UriSource = new System.Uri("/Resources/UserInterface/TALK-BUBBLES-Pointer-Sallie.png", System.UriKind.Relative);
            hookBitImage.EndInit();
            Image hook = new() { Source = hookBitImage, Width = 20, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0) };

            BitmapImage podBitImage = new();
            podBitImage.BeginInit();
            podBitImage.UriSource = new System.Uri("/Resources/UserInterface/ICON-Sallie-Blue.png", System.UriKind.Relative);
            podBitImage.EndInit();
            Image podIcon = new() { Source = podBitImage, Width = 35, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(20,0,0,0)};

            Grid wordBubble = new();
            RowDefinition row1 = new();
            RowDefinition row2 = new();
            wordBubble.RowDefinitions.Add(row1);
            wordBubble.RowDefinitions.Add(row2);

            Grid.SetRow(theBorder, 0);
            Grid.SetRow(hook, 1);
            Grid.SetColumn(theBorder, 0);
            Grid.SetColumn(hook, 0);
            wordBubble.Children.Add(theBorder);
            wordBubble.Children.Add(hook);

            Grid wordsAndIcon = new() { Margin = new Thickness(0,0,30,0) };
            ColumnDefinition col1 = new();
            ColumnDefinition col2 = new() { Width = new GridLength(65) };
            wordsAndIcon.ColumnDefinitions.Add(col1);
            wordsAndIcon.ColumnDefinitions.Add(col2);

            Grid.SetColumn(wordBubble, 0);
            Grid.SetColumn(podIcon, 1);
            wordsAndIcon.Children.Add(wordBubble);
            wordsAndIcon.Children.Add(podIcon);

            conversationStack.Children.Add(wordsAndIcon);
        }

        private void SpeechOffSwitch_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.speechEnabled = true;

            SpeechOffSwitch.Visibility = Visibility.Collapsed;
            SpeechOnSwitch.Visibility = Visibility.Visible;
        }

        private void SpeechOnSwitch_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.speechEnabled = false;

            SpeechOnSwitch.Visibility = Visibility.Collapsed;
            SpeechOffSwitch.Visibility = Visibility.Visible;
        }

        private void SpeechPodSwitch_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.remoteMicEnabled = false;
            moduleSpeechInPlus.Initialize();

            SpeechPodSwitch.Visibility = Visibility.Collapsed;
            SpeechLocalSwitch.Visibility = Visibility.Visible;
        }

        private void SpeechLocalSwitch_Click(object sender, RoutedEventArgs e)
        {
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return;

            moduleSpeechInPlus.remoteMicEnabled = true;
            moduleSpeechInPlus.Initialize();

            SpeechLocalSwitch.Visibility = Visibility.Collapsed;
            SpeechPodSwitch.Visibility = Visibility.Visible;
        }

        private void InputText()
        {
            ModuleUI_Conversation parent = (ModuleUI_Conversation)ParentModule;
            if (parent == null) return;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            moduleSpeechInPlus.ReceiveInputFromText(SpeechEntryTextBox.Text);
        }

        private void SpeechEntryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                InputText();
        }

        private void TextSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            TextSubmitButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Med.png", System.UriKind.Relative));
            InputText();
        }

        private void TextSubmitButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            TextSubmitButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Med-Disabled.png", System.UriKind.Relative));
        }

        private void TextSubmitButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            TextSubmitButton.Source = new BitmapImage(new System.Uri("/Resources/UserInterface/Motion/DRIVE-CONTROLS-Up-Med-Disabled.png", System.UriKind.Relative));
        }

        private void CommandsButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CommandsButton.Background = new SolidColorBrush(Color.FromRgb(121, 70, 200));
            CommandsButtonLabel.Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235));
            ((ModuleUserInterfaceDlg)Owner).ShowAvailableCommands();
        }

        private void CommandsButton_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            CommandsButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            CommandsButtonLabel.Foreground = new SolidColorBrush(Color.FromRgb(121, 70, 200));
        }

        private void CommandsButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            CommandsButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            CommandsButtonLabel.Foreground = new SolidColorBrush(Color.FromRgb(121, 70, 200));
        }
    }
}