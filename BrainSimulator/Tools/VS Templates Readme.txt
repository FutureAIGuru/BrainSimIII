Visual Studio Module Creation Templates

You can Create a New Module more easily using the Visual Studio templates.  

To install the templates in Visual Studio:

Copy the two zip files to the user item template directory. 
	The default location is:
	%USERPROFILE%\Documents\Visual Studio <version>\Templates\ItemTemplates\Visual C#
	Examples:
	C:\Users\Nicholas\Documents\Visual Studio 2022\Templates\ItemTemplates\Visual C#
	or 
	C:\Users\Nicholas\OneDrive - futureai.guru\Documents\Visual Studio 2022\Templates

Close Visual Studio and then reopen it.

Now under Add | New Item | Visual C#, you should see the option to create a new BrainSimulator Module and a new BrainSimulator Module Dlg.
Be sure however to name the new Module in the format Module{XYZ}.cs, and the matching ModuleDlg in the format Module{XYZ}Dlg.xaml!
Also notice that the Actual 'Show Dialog' option in the menu only shows up after a new build once you added the dialog.

To Change the templates:
Edit the Module.cs template 
Edit the ModuleDlg.xaml & ModuleDlg.xaml.cs template
Copy these files into the appropriate zip files.
Follow the installation procedure above, overwrite any existing zip files.