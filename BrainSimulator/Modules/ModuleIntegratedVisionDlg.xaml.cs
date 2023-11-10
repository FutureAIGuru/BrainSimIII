//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using Emgu.CV;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

using MessageBox = System.Windows.Forms.MessageBox;

namespace BrainSimulator.Modules
{
    public partial class ModuleIntegratedVisionDlg : ModuleBaseDlg
    {
        [XmlIgnore]
        public string ParametersOutputFolder { get; } = Utils.GetOrAddLocalSubFolder(Utils.FolderImageRecognitionParameters);
        [XmlIgnore]
        public string TestsInputImage { get; set; }
        
        public ModuleIntegratedVisionDlg()
        {
            InitializeComponent();
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return false;
            if (parent.FileToRecognize != null && parent.FileToRecognize.Length != 0)
            {
                FilenameTextBox.Text = Path.GetFileName(parent.FileToRecognize);
            }
            if (parent.CurrentImageProcessed && 
                parent.UpdateDialogImage && 
                parent.ImageToRecognize != null)
            {
                TimingTextBox.Text = ModuleIntegratedVision.StopwatchResult.ToString();
                Mat output = parent.ImgToShowOnDialog.Clone();

                ImageWindow.Source = ImgUtils.ToBitmapImage(output);

                // Make sure we don't process multiple times...      
                parent.UpdateDialogImage = false;
            }

            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }

        private void SaveDebugImagesCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            parent.SaveDebugImages = true;
        }

        private void SaveDebugImagesCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            parent.SaveDebugImages = false;
        }

        private void LoadTestImageButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            using OpenFileDialog openFileDialog = new()
            {
                Filter = Utils.FilterImages,
                Title = Utils.TitleImagesLoad,
                Multiselect = false,
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                TestsInputImage = openFileDialog.FileName;
                LoadImageToTestWith(TestsInputImage);
                openFileDialog.Dispose();
            }
        }
        public void LoadImageToTestWith(string fullImageFileSpec)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            Sallie.TestImage = CvInvoke.Imread(TestsInputImage);
            System.Drawing.Size resize = ImgUtils.ProcessingSize;
            CvInvoke.Resize(Sallie.TestImage, Sallie.TestImage, resize, 0, 0, Emgu.CV.CvEnum.Inter.Cubic);
            Sallie.TestImageFilename = Path.GetFileNameWithoutExtension(TestsInputImage);
            Sallie.TestFolderName = "";
        }

        private void LoadImageFolderButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            DialogResult result;
            using (var fbd = new FolderBrowserDialog())
            {
                result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Sallie.TestFolderName = fbd.SelectedPath;
                    Sallie.TestFolderFilenames = Directory.GetFiles(Sallie.TestFolderName, "*.jpg");
                    if (Sallie.TestFolderFilenames.Length == 0)
                    {
                        System.Windows.MessageBox.Show("Folder is empty");
                        return;
                    }
                    FilenameTextBox.Text = Sallie.TestFolderName;
                }
            }
        }

        private void ClearTestImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sallie.TestImage == null) return;
            Sallie.TestImageFilename = "";
            Sallie.TestImage.Dispose();
            Sallie.TestImage = null;
            Sallie.TestFolderName = "";
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            int index = ProcessingSizeComboBox.SelectedIndex;
            ListBoxItem lbi = (ListBoxItem)ProcessingSizeComboBox.ItemContainerGenerator.ContainerFromIndex(index);
            if (lbi == null) return;
            string resolution = lbi.Content as string;
            if (resolution == null) 
                ImgUtils.ProcessingSize = new System.Drawing.Size(512, 384);
            else if (resolution == "1024x768")
                ImgUtils.ProcessingSize = new System.Drawing.Size(1024, 768);
            else if (resolution == "768x576")
                ImgUtils.ProcessingSize = new System.Drawing.Size(768, 576);
            else if (resolution == "512x384")
                ImgUtils.ProcessingSize = new System.Drawing.Size(512, 384);
            else if (resolution == "341x256")
                ImgUtils.ProcessingSize = new System.Drawing.Size(341, 256);
            else if (resolution == "256x192")
                ImgUtils.ProcessingSize = new System.Drawing.Size(256, 192);
            else
                ImgUtils.ProcessingSize = new System.Drawing.Size(512, 384);
        }

        private void ViewInputRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            parent.ViewSelect = 1;
        }

        private void ViewMaskRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            parent.ViewSelect = 2;
        }

        private void ViewOutputRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            parent.ViewSelect = 3;
        }

        private void SensitivityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(SensitivityTextBox.Text, out Pixel.Sensitivity);
        }

        private void ThresholdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(ThresholdTextBox.Text, out Pixel.Threshold);
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            OpenFileDialog openFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleParamLoad,
                Multiselect = false,
                InitialDirectory = ParametersOutputFolder,
            };
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                int resolution = 2;
                XElement doc = XElement.Load(openFileDialog.FileName);
                if (doc == null) return;
                foreach (var parameter in doc.Descendants())
                {
                    if (parameter.Name.LocalName == "Sensitivity" &&
                        !int.TryParse(parameter.Value, out Pixel.Sensitivity)) Pixel.Sensitivity = 700;
                    if (parameter.Name.LocalName == "Threshold" &&
                        !int.TryParse(parameter.Value, out Pixel.Threshold)) Pixel.Threshold = 100;
                    if (parameter.Name.LocalName == "Resolution" &&
                        !int.TryParse(parameter.Value, out resolution)) resolution = 2;
                }

                SensitivityTextBox.Text = Pixel.Sensitivity.ToString();
                ThresholdTextBox.Text = Pixel.Threshold.ToString();
                ProcessingSizeComboBox.SelectedIndex = resolution;

            }
            openFileDialog.Dispose();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            SaveFileDialog saveFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleParamSave,
                InitialDirectory = ParametersOutputFolder,
            };
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                SaveParameterFile(saveFileDialog.FileName);
            }
            saveFileDialog.Dispose();
        }

        public void SaveParameterFile(string filename)
        {
            ModuleIntegratedVision parent = (ModuleIntegratedVision)base.ParentModule;
            if (parent == null) return;
            int resolution = ProcessingSizeComboBox.SelectedIndex;
            XElement res = new("IntegratedVisionSettings");
            res.Add(new XElement("Sensitivity", Pixel.Sensitivity),
                    new XElement("Threshold", Pixel.Threshold),
                    new XElement("Resolution", resolution));
            res.Save(filename);
        }
    }
}
