//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

    public ModuleBaseDlg()
    {
        this.Loaded += ModuleBaseDlg_Loaded;
    }

    private void ModuleBaseDlg_Loaded(object sender, RoutedEventArgs e)
    {
        // Create a button and add it to the panel
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
    }

    private void SourceButton_Click(object sender, RoutedEventArgs e)
    {
        string theModuleType = this.GetType().Name.ToString();
        string cwd = System.IO.Directory.GetCurrentDirectory();
        cwd = cwd.ToLower().Replace("bin\\debug\\net8.0-windows", "");
        string fileName = cwd + @"modules\" + theModuleType;
        if (theModuleType.ToLower().Contains("dlg"))
            fileName += ".xaml.cs";
        else
            fileName += ".cs";



        if (!File.Exists(fileName))
        { }
        Process process = new Process();
        string taskFile = @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe";
        if (!File.Exists(taskFile))
            taskFile = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
        ProcessStartInfo startInfo = new ProcessStartInfo(taskFile, "/edit " + fileName);
        process.StartInfo = startInfo;
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

}
