/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for ModuleDescription.xaml
    /// </summary>
    public partial class ModuleDescriptionDlg : Window
    {
        string moduleType = "";
        public ModuleDescriptionDlg(string theModuleType)
        {
            InitializeComponent();
            moduleType = theModuleType;
            string fileName = Path.GetFullPath(".").ToLower();
            var modules = Utils.GetListOfExistingCSharpModuleTypes();

            foreach (var v in modules)
            {
                moduleSelector.Items.Add(v.Name.Replace("Module", ""));
            }
            moduleSelector.SelectedItem = theModuleType.Replace("Module", "");

            Owner = Application.Current.MainWindow;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleDescriptionFile.SetDescription(moduleType, Description.Text);
            ModuleDescriptionFile.Save();
        }

        private void moduleSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                moduleType = "Module" + cb.SelectedItem.ToString();
                Description.Text = ModuleDescriptionFile.GetDescription(moduleType);
            }
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ModuleDescriptionFile
    {
        public class ModuleDescription
        {
            public string moduleName;
            public string description;
        }
        public static List<ModuleDescription> theModuleDescriptions = null;

        public static string GetDescription(string moduleName)
        {
            if (theModuleDescriptions is null) Load();
            ModuleDescription desc = theModuleDescriptions.Find(t => t.moduleName == moduleName);
            if (desc is not null) return desc.description;
            return "";
        }
        public static void SetDescription(string moduleName, string theDescription)
        {
            ModuleDescription desc = theModuleDescriptions.Find(t => t.moduleName == moduleName);
            if (desc is not null)
                desc.description = theDescription;
            else
            {
                desc = new ModuleDescription { moduleName = moduleName, description = theDescription };
                theModuleDescriptions.Add(desc);
            }
        }

        public static bool Load()
        {
            Stream file;
            string location = AppDomain.CurrentDomain.BaseDirectory;
            location += "ModuleDescriptions.xml";
            file = File.Open(location, FileMode.Open, FileAccess.Read);
            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<ModuleDescription>));
                theModuleDescriptions = (List<ModuleDescription>)reader.Deserialize(file);
            }
            catch (Exception e)
            {
                MessageBox.Show("Module CvStageDescription Xml file read failed because: " + e.Message);
                return false;
            }
            file.Close();
            return true;
        }

        public static bool Save()
        {
            Stream file;
            string fileName = Path.GetFullPath(".").ToLower();
            //we're running with source...save to the source version
            int index = fileName.IndexOf("bin\\");
            if (index != -1)
            {
                fileName = fileName.Substring(0, index);
            }
            fileName += "ModuleDescriptions.xml";
            try
            {
                file = File.Create(fileName);
                XmlSerializer writer = new XmlSerializer(typeof(List<ModuleDescription>));
                writer.Serialize(file, theModuleDescriptions);
            }
            catch (Exception e)
            {
                MessageBox.Show("Module CvStageDescription Xml file write failed because: " + e.Message);
                return false;
            }
            file.Position = 0; ;

            file.Close();

            return true;
        }


    }
}
