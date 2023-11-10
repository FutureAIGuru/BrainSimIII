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
using System.Windows.Media.Media3D;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public partial class ModuleUserInterfaceDlg : ModuleBaseDlg
    {
        [XmlIgnore]
        public string PodName { get => podName; }

        private string podName = "Sallie";
        private string connectionStatus = "Not Connected";

        const int captionHeight = 45;

        List<WindowHelper> childWindows = new();

        bool wakeWordsSet = false;

        public string imageSaveLocation = Utils.GetOrAddDocumentsSubFolder(Utils.FolderUISavedImages);

        [XmlIgnore]
        public bool saveImage = false;

        int batteryCharge1 = 0, batteryCharge2 = 0;

        ModuleUI_SpeechCommandsDlg commandsWindow;
        int commandsWindowWidth = 400, commandsWindowHeight = 500;

        Window mainDevWindow;

        public ModuleUserInterfaceDlg()
        {
            InitializeComponent();

            try //this crashes if the window is opened a second time
            {
                FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
                {
                    DefaultValue = FindResource(typeof(Window))
                });
            }
            catch { }
            //DataContext = this;
        }

        //this function is also called when the dialog is opened
        //so we can initalize the dialog again here, otherwise things like slider maximums would go back to their default values when the dialog is opened 
        private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
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

            if (mainDevWindow == null) mainDevWindow = Owner;

            SetBatteryStatus();

            foreach (WindowHelper child in childWindows) child.Draw();
            Owner = null;

            if (wakeWordsSet == false && UpdateWakeWords())
                wakeWordsSet = true;

            return true;
        }

        private class WindowHelper
        {
            public WindowHelper(ModuleBaseDlg window, ModuleBaseDlg parent, int leftPos, int rightPos, int topPos, int bottomPos)
            {
                Window = window;
                Parent = parent;
                window.Owner = parent;
                LeftPos = Math.Clamp(leftPos, 0, 100);
                RightPos = Math.Clamp(rightPos, 0, 100);
                TopPos = Math.Clamp(topPos, 0, 100);
                BottomPos = Math.Clamp(bottomPos, 0, 100);
                Popup = false;

                UpdatePosition();
                Window.Show();
            }

            public WindowHelper(ModuleBaseDlg window, ModuleBaseDlg parent)
            {
                Window = window;
                Parent = parent;
                window.Owner = parent;

                Popup = true;
                UpdatePosition();
            }

            bool expanded = false;

            public ModuleBaseDlg Window { get; }
            ModuleBaseDlg Parent;

            //position of edge of window as a percentage of the main window
            private int LeftPos;
            private int RightPos;
            private int TopPos;
            private int BottomPos;

            public bool Popup { get; }

            public void UpdatePosition()
            {
                double parentLeft;
                double parentTop;
                if (Parent.WindowState == WindowState.Maximized)
                {
                    System.Windows.Forms.Screen mainWindowScreen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Parent).Handle);
                    System.Drawing.Rectangle screenArea = mainWindowScreen.WorkingArea;

                    parentLeft = screenArea.Left;
                    parentTop = screenArea.Top;
                }
                else
                {
                    parentLeft = Parent.Left;
                    parentTop = Parent.Top;
                }

                //popups stay docked to top right corner
                if (Popup)
                {
                    Window.Left = parentLeft + Parent.ActualWidth - Window.Width - 5;
                    Window.Top = parentTop + captionHeight;
                    return;
                }

                if (!expanded)
                {
                    Window.Left = parentLeft + (Parent.ActualWidth * (LeftPos / 100f));
                    Window.Width = parentLeft + (Parent.ActualWidth * (RightPos / 100f)) - Window.Left;
                    Window.Top = parentTop + captionHeight + ((Parent.ActualHeight - captionHeight) * (TopPos / 100f));
                    Window.Height = parentTop + captionHeight + ((Parent.ActualHeight - captionHeight) * (BottomPos / 100f)) - Window.Top;
                }
            }

            const float expandPercentage = 0.25f;
            public bool ExpandCollapse()
            {
                expanded = !expanded;
                if (expanded)
                {
                    //expand out from right corner where the expand button is located
                    Window.Left -= Window.Width * expandPercentage;

                    Window.Width *= 1 + expandPercentage;
                    Window.Height *= 1 + expandPercentage;
                }
                return expanded;
            }

            public Type WindowType()
            {
                return Window.GetType();
            }

            public void Draw()
            {
                Window.Draw(false);
            }
        }

        //returns true if a window was expanded
        public bool ExpandCollapseWindow(Type windowType)
        {
            WindowHelper matchingWindow = WindowOfType(windowType);
            if (matchingWindow == null) return false;
            else return matchingWindow.ExpandCollapse();
        }

        private WindowHelper WindowOfType(Type windowType)
        {
            foreach (WindowHelper child in childWindows)
            {
                if (child.WindowType() == windowType)
                    return child;
            }
            return null;
        }

        public void ClosePopups()
        {
            foreach(WindowHelper child in childWindows)
            {
                if (child.Popup)
                    child.Window.Hide();
            }

            SettingsEnabledButton.Visibility = Visibility.Collapsed;
            SettingsDisabledButton.Visibility = Visibility.Visible;

            HelpEnabledButton.Visibility = Visibility.Collapsed;
            HelpDisabledButton.Visibility = Visibility.Visible;
        }

        //set up anything on the display that needs to be set programaticaly
        public void Initialize(float minPan, float maxPan, float minTilt, float maxTilt, float minSpeed, float maxSpeed, out float setSpeed)
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
            CaptionBar.Height = captionHeight;

            UpdateMaximizeRestoreButton();

            //create child windows

            ModuleUI_CameraFeed cameraModule = new();
            cameraModule.ShowDialog();
            ModuleUI_CameraFeedDlg cameraWindow = cameraModule.GetDlg();
            cameraWindow.Initialize(minPan, maxPan, minTilt, maxTilt);
            childWindows.Add(new WindowHelper(cameraWindow, this, 2, 40, 2, 59));

            ModuleUI_Environment environmentModule = new();
            environmentModule.ShowDialog();
            ModuleUI_EnvironmentDlg environmentWindow = environmentModule.GetDlg();
            environmentWindow.Initialize();
            childWindows.Add(new WindowHelper(environmentWindow, this, 2, 40, 61, 98));

            ModuleUI_Motion motionModule = new();
            motionModule.ShowDialog();
            ModuleUI_MotionDlg motionWindow= motionModule.GetDlg();
            motionWindow.Initialize(minSpeed, maxSpeed, out setSpeed);
            childWindows.Add(new WindowHelper(motionWindow, this, 41, 61, 2, 49));

            ModuleUI_Knowledge knowledgeModule = new();
            knowledgeModule.ShowDialog();
            ModuleUI_KnowledgeDlg knowledgeWindow = knowledgeModule.GetDlg();
            childWindows.Add(new WindowHelper(knowledgeWindow, this, 41, 61, 51, 98));

            ModuleUI_Intercom intercomModule = new();
            intercomModule.ShowDialog();
            ModuleUI_IntercomDlg intercomWindow = intercomModule.GetDlg();
            intercomWindow.Initialize();
            childWindows.Add(new WindowHelper(intercomWindow, this, 62, 98, 2, 22));

            ModuleUI_Conversation conversationModule = new();
            conversationModule.ShowDialog();
            ModuleUI_ConversationDlg conversationWindow = conversationModule.GetDlg();
            childWindows.Add(new WindowHelper(conversationWindow, this, 62, 98, 24, 98));

            //popup windows
            //ModuleUI_Help helpModule = new();
            //helpModule.ShowDialog();
            //ModuleUI_HelpDlg helpWindow = helpModule.GetDlg();
            ModuleUI_HelpDlg helpWindow = new();
            childWindows.Add(new WindowHelper(helpWindow, this));

            //ModuleUI_Settings settingsModule = new();
            //settingsModule.ShowDialog();
            //ModuleUI_SettingsDlg settingsWindow = settingsModule.GetDlg();
            ModuleUI_SettingsDlg settingsWindow = new();
            childWindows.Add(new WindowHelper(settingsWindow, this));

            commandsWindow = new();
            commandsWindow.Width = commandsWindowWidth;
            commandsWindow.Height = commandsWindowHeight;
            commandsWindow.Owner = this;
            commandsWindow.ResizeMode = ResizeMode.NoResize;

            intercomWindow.PopulateSounds();
        }

        //returns false if the text will not fit
        public bool SetErrorMessage(string msg)
        {
            //if(WillTextClip(msg, ErrorLabel))
                return false;

            //ErrorLabel.Content = msg;
            //return true;
        }

        private bool WillTextClip(string text, Label label)
        {
            Typeface typeface = new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch);
            FormattedText formattedText = new FormattedText(text, System.Threading.Thread.CurrentThread.CurrentCulture,
                label.FlowDirection, typeface, label.FontSize, label.Foreground);

            return (formattedText.Width > (label.Width - 10));
        }

        //returns true if successfull
        private bool UpdateWakeWords()
        {
            ModuleUserInterface parent = (ModuleUserInterface)ParentModule;
            if (parent == null) return false;
            ModuleSpeechInPlus moduleSpeechInPlus = (ModuleSpeechInPlus)parent.FindModule(typeof(ModuleSpeechInPlus));
            if (moduleSpeechInPlus == null) return false;

            List<string> wakeWords = moduleSpeechInPlus.GetWakeWords();
            if (wakeWords.Count == 0) return false;

            WindowHelper settingsWindowHelper = WindowOfType(typeof(ModuleUI_SettingsDlg));
            if (settingsWindowHelper == null) return false;
            ModuleUI_SettingsDlg settingsWindow = (ModuleUI_SettingsDlg)settingsWindowHelper.Window;
            
            settingsWindow.UpdateComboBox(wakeWords);
            return true;
        }

        public void UpdateEnvironment(List<ModelUIElement3D> theObjects)
        {
            ModuleUI_EnvironmentDlg moduleUI_EnvironmentDlg = (ModuleUI_EnvironmentDlg)WindowOfType(typeof(ModuleUI_EnvironmentDlg)).Window;
            moduleUI_EnvironmentDlg.DrawObjects(theObjects);
        }

        private void UpdateTitle()
        {
            Title = podName + " - " + connectionStatus;
        }

        public void SaveName(string newName)
        {
            ModuleUserInterface parent = (ModuleUserInterface)base.ParentModule;
            if (parent == null) return;

            podName = newName;
            parent.SaveWakeWord(podName);
            podNameLabel.Content = podName;
            UpdateTitle();
        }

        public void CalibratePod()
        {
            ModuleUserInterface parent = (ModuleUserInterface)base.ParentModule;
            if (parent != null)
                parent.Recalibrate();
        }

        private void Dlg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ModuleUserInterface parent = (ModuleUserInterface)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (!e.IsRepeat) moduleInputControl.MoveForwardBackward(true);
                    //prevent arrow keys from doing anything else
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
            ModuleUserInterface parent = (ModuleUserInterface)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

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
            ModuleUserInterface parent = (ModuleUserInterface)ParentModule;
            if (parent == null) return;
            ModuleInputControl moduleInputControl = (ModuleInputControl)parent.FindModule(typeof(ModuleInputControl));
            if (moduleInputControl == null) return;

            moduleInputControl.Stop();
        }        

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if(WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void UpdateMaximizeRestoreButton()
        {
            if(WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }

        public void DrawSpeechIn(string speechIn)
        {
            WindowHelper conversationWindowHelper = WindowOfType(typeof(ModuleUI_ConversationDlg));
            if (conversationWindowHelper == null) return;
            ModuleUI_ConversationDlg conversationWindow = (ModuleUI_ConversationDlg)conversationWindowHelper.Window;
            conversationWindow.SetInput(speechIn);
        }

        public void DrawSpeechOut(string speechOut)
        {
            WindowHelper conversationWindowHelper = WindowOfType(typeof(ModuleUI_ConversationDlg));
            if (conversationWindowHelper == null) return;
            ModuleUI_ConversationDlg conversationWindow = (ModuleUI_ConversationDlg)conversationWindowHelper.Window;
            conversationWindow.SetOutput(speechOut);
        }

        public void SetConnectionStatus(bool connected)
        {
            if (connected)
            {
                connectionStatus = "Connected";
                NetworkDisconnectedImage.Visibility = Visibility.Collapsed;
                NetworkConnectedImage.Visibility = Visibility.Visible;
            }
            else
            {
                connectionStatus = "Not Connected";
                NetworkConnectedImage.Visibility = Visibility.Collapsed;
                NetworkDisconnectedImage.Visibility = Visibility.Visible;
            }
            UpdateTitle();
        }

        public void SetBatteryLevels(int charge1, int charge2)
        {
            batteryCharge1 = charge1;
            batteryCharge2 = charge2;
        }

        public void SetBatteryStatus()
        {
            float totalCharge = (batteryCharge1 + batteryCharge2) / 1000f;
            
            BatteryEmpty.Visibility = Visibility.Collapsed;
            Battery1.Visibility = Visibility.Collapsed;
            Battery2.Visibility = Visibility.Collapsed;
            Battery3.Visibility = Visibility.Collapsed;
            Battery4.Visibility = Visibility.Collapsed;
            BatteryFull.Visibility = Visibility.Collapsed;

            if (totalCharge > 4.2)
                BatteryFull.Visibility = Visibility.Visible;
            else if (totalCharge > 3.9)
                Battery4.Visibility = Visibility.Visible;
            else if (totalCharge > 3.6)
                Battery3.Visibility = Visibility.Visible;
            else if (totalCharge > 3.3)
                Battery2.Visibility = Visibility.Visible;
            else if (totalCharge > 3)
                Battery1.Visibility = Visibility.Visible;
            else
                BatteryEmpty.Visibility = Visibility.Visible;
        }

        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            UpdateMaximizeRestoreButton();
        }

        public void MoveChildren()
        {
            foreach (WindowHelper child in childWindows) child.UpdatePosition();
        }

        private void ModuleBaseDlg_LocationChanged(object sender, EventArgs e)
        {
            MoveChildren();
        }

        private void ModuleBaseDlg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MoveChildren();
        }

        private void ModuleBaseDlg_Loaded(object sender, RoutedEventArgs e)
        {
            ((ModuleUserInterface)ParentModule).InitializeDialog();
        }

        private void HelpDisabledButton_Click(object sender, RoutedEventArgs e)
        {
            HelpDisabledButton.Visibility = Visibility.Collapsed;
            HelpEnabledButton.Visibility = Visibility.Visible;

            SettingsEnabledButton.Visibility = Visibility.Collapsed;
            SettingsDisabledButton.Visibility = Visibility.Visible;

            WindowOfType(typeof(ModuleUI_HelpDlg)).Window.Show();
        }

        private void HelpEnabledButton_Click(object sender, RoutedEventArgs e)
        {
            HelpEnabledButton.Visibility = Visibility.Collapsed;
            HelpDisabledButton.Visibility = Visibility.Visible;

            WindowOfType(typeof(ModuleUI_HelpDlg)).Window.Hide();
        }

        private void SettingsDisabledButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDisabledButton.Visibility = Visibility.Collapsed;
            SettingsEnabledButton.Visibility = Visibility.Visible;

            HelpEnabledButton.Visibility = Visibility.Collapsed;
            HelpDisabledButton.Visibility = Visibility.Visible;

            WindowOfType(typeof(ModuleUI_SettingsDlg)).Window.Show();
        }

        private void SettingsEnabledButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsEnabledButton.Visibility = Visibility.Collapsed;
            SettingsDisabledButton.Visibility = Visibility.Visible;

            WindowOfType(typeof(ModuleUI_SettingsDlg)).Window.Hide();
        }

        private void ModuleBaseDlg_Closed(object sender, EventArgs e)
        {
#if !DEBUG
            mainDevWindow.Close();
#endif        
        }

        public void ShowAvailableCommands()
        {
            double mainWindowLeft;
            double mainWindowTop;
            if (WindowState == WindowState.Maximized)
            {
                System.Windows.Forms.Screen mainWindowScreen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
                System.Drawing.Rectangle screenArea = mainWindowScreen.WorkingArea;

                mainWindowLeft = screenArea.Left;
                mainWindowTop = screenArea.Top;
            }
            else
            {
                mainWindowLeft = Left;
                mainWindowTop = Top;
            }

            commandsWindow.Width = commandsWindowWidth;
            commandsWindow.Height = commandsWindowHeight;

            ModuleUI_HelpDlg helpWindow = (ModuleUI_HelpDlg)WindowOfType(typeof(ModuleUI_HelpDlg)).Window;
            commandsWindow.Left = mainWindowLeft + ActualWidth - commandsWindowWidth - 100;
            commandsWindow.Top = mainWindowTop + helpWindow.Height + 100;
            commandsWindow.Show();
            commandsWindow.Topmost = true;
        }

        private void ModuleBaseDlg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClosePopups();
        }
    }
}