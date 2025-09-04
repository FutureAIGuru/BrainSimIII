﻿//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace BrainSimulator.Modules;

public class ModuleBaseDlg : Window
{
    public ModuleBase ParentModule;
    private DateTime dt;
    private DispatcherTimer timer;
    public int UpdateMS = 100;
    public Label statusLabel;

    public ModuleBaseDlg()
    {
        this.Loaded += ModuleBaseDlg_Loaded;
    }

    private void ModuleBaseDlg_Loaded(object sender, RoutedEventArgs e)
    {
        // Create a help button and add it to the panel
        Button helpButton = new Button
        {
            Content = "?",
            FontSize = 24,
            Width = 20,
            Height = 25,
            Margin = new Thickness(5),
            Padding = new Thickness(0, -6, 0, 0),
            Name = "helpButton",
            ToolTip = "Show dialog help",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        helpButton.Click += HelpButton_Click;


        //get the image for the source button icon
        var img = new Image
        {
            Source = new BitmapImage(
                new Uri("pack://application:,,,/Resources/icons/textFileIcon.png",
                        UriKind.Absolute)),
            Stretch = Stretch.Uniform
        };

        //create a button to show the source code
        Button sourceButton = new Button
        {
            Content = img,
            Width = 20,
            Height = 25,
            Margin = new Thickness(5, 5, 30, 5),
            Padding = new Thickness(1),
            Name = "sourceButton",
            ToolTip = "Show dialog source",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        sourceButton.Click += SourceButton_Click;

        // Add the button to the dialog
        ((Panel)this.Content).Children.Add(helpButton);
        ((Panel)this.Content).Children.Add(sourceButton);

        statusLabel = new()
        {
            Content = "OK",
            Name = "statusLabel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 0, 60, 3),
            FontSize = 18
        };
        ((Panel)this.Content).Children.Add(statusLabel);
    }

    private void SourceButton_Click(object sender, RoutedEventArgs e)
    {
        string theModuleType = this.GetType().Name.ToString();
        string cwd = System.IO.Directory.GetCurrentDirectory();
        cwd = cwd.ToLower().Replace("bin\\debug\\net8.0-windows", "");
        string fileNameDlg = cwd + @"modules\" + theModuleType + ".xaml.cs";
        string fileName = cwd + @"modules\" + theModuleType.Substring(0,theModuleType.Length-3) + ".cs";

        //find visiaul studio
        string taskFile = @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe";
        if (!File.Exists(taskFile))
            taskFile = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
        if (!File.Exists(taskFile))
            return;

        //fire up the processes
        Process processDlg = new();
        ProcessStartInfo startInfo = new ProcessStartInfo(taskFile, "/edit " + fileNameDlg);
        processDlg.StartInfo = startInfo;
        processDlg.Start();
        Process process = new();
        ProcessStartInfo startInfo2 = new ProcessStartInfo(taskFile, "/edit " + fileName);
        process.StartInfo = startInfo2;
        process.Start();
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        string theModuleType = this.GetType().Name.ToString();
        theModuleType = theModuleType.Replace("Dlg", "");
        ModuleDescriptionDlg md = new ModuleDescriptionDlg(theModuleType);
        md.Show();
    }

    virtual public bool Draw(bool checkDrawTimer)
    {
        if (!checkDrawTimer) return true;
        return true;
    }

    public void Timer_Tick(object sender, EventArgs e)
    {
        timer.Stop();
        if (Application.Current == null) return;
        if (this != null)
            Draw(false);

    }

    //this picks up a final draw after 1/4 second 
    public void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        timer.Stop();
        if (Application.Current == null) return;
        if (this != null)
            Draw(false);
    }

    /// <summary>
    /// Sets a status message at the bottom of the dialog. Seets the background yellow if the color is red or null
    /// </summary>
    /// <param name="message"></param>
    /// <param name="c">Defaults to red</param>
    public void SetStatus(string message, Color? c = null)
    {
        if (c == null) c = Colors.Red;
        statusLabel.Background = new SolidColorBrush(Colors.Gray);
        if (c == Colors.Red && (message != "OK" && message != "" ))
            statusLabel.Background = new SolidColorBrush(Colors.LemonChiffon);
        if (message == "OK" || message == "")
            statusLabel.Foreground = new SolidColorBrush(Colors.Black);
        else
            statusLabel.Foreground = new SolidColorBrush((Color)c);

        statusLabel.Content = message;
    }
}
