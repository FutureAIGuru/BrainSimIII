//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public partial class Module3DSimModelDlg : ModuleBaseDlg
    {

        public static string currentFileName = "";

        private bool greenSize = false;
        private bool greenX = false;
        private bool greenY = false;
        private bool greenZ = false;
        private bool greenRotate = false;

        public bool isAllGreen()
        {
            ComboBoxItem cbi = (ComboBoxItem)ShapeBox.SelectedItem;
            ComboBoxItem cbi2 = (ComboBoxItem)ColorBox.SelectedItem;
            if (cbi == null || cbi2 == null) return false;
            string shape = cbi.Content.ToString();
            string color = cbi2.Content.ToString();
            if (string.IsNullOrEmpty(shape) || string.IsNullOrEmpty(color))
            {
                return false;
            }
            if (!greenRotate || !greenSize || !greenX || !greenY || !greenZ)
            {
                return false;
            }
            return true;
        }

        private bool editInProgress = false;

        public Module3DSimModelDlg()
        {
            InitializeComponent();
            ClearEntryValues();
            Constructor3D construct = new();
            construct.PopulateShapeBox(ShapeBox);
            construct.PopulateColorBox(ColorBox);
        }

        public override bool Draw(bool checkDrawTimer)
        {
            if (!base.Draw(checkDrawTimer)) return false;
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
            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (parent != null && parent.ourThings.Count > 0)
            {
                ShapeCollection.Items.Clear();
                foreach (var thing in parent.ourThings)
                {
                    ShapeCollection.Items.Add(new ListBoxItem()
                    {
                        Content = thing,
                        ToolTip = "a Shape in the 3D Environment",
                    });
                }
            }
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(false);
        }


        private void SizeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (SizeBox.Text == "")
                {
                    SizeBox.Background = new SolidColorBrush(Colors.Yellow);
                    greenSize = false;
                    return;
                }
                if (double.Parse(SizeBox.Text) < -0 || double.Parse(SizeBox.Text) > 500)
                {
                    SizeBox.Background = new SolidColorBrush(Colors.Red);
                    greenSize = false;
                }
                else
                {
                    SizeBox.Background = new SolidColorBrush(Colors.Green);
                    if (ShapeCollection.SelectedIndex == -1)
                    {
                        Z_Box.Text = (double.Parse(SizeBox.Text) / 2).ToString();
                    }
                    greenSize = true;
                    greenZ = true;
                }
            }
            catch
            {
                SizeBox.Background = new SolidColorBrush(Colors.Red);
                greenSize = false;
            }
        }

        private void X_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (X_Box.Text == "")
                {
                    X_Box.Background = new SolidColorBrush(Colors.Yellow);
                    greenX = false;
                    return;
                }
                if (double.Parse(X_Box.Text) < -500 || double.Parse(X_Box.Text) > 500)
                {
                    X_Box.Text = ((double)(0)).ToString();
                    X_Box.Background = new SolidColorBrush(Colors.Red);
                    greenX = false;
                }
                else
                {
                    X_Box.Background = new SolidColorBrush(Colors.Green);
                    greenX = true;
                }
            }
            catch
            {
                X_Box.Background = new SolidColorBrush(Colors.Red);
                greenX = false;
            }
        }

        private void Y_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Y_Box.Text == "")
                {
                    Y_Box.Background = new SolidColorBrush(Colors.Yellow);
                    greenY = false;
                    return;
                }
                if (double.Parse(Y_Box.Text) < -500 || double.Parse(Y_Box.Text) > 500)
                {
                    Y_Box.Text = ((double)(0)).ToString();
                    Y_Box.Background = new SolidColorBrush(Colors.Red);
                    greenY = false;
                }
                else
                {
                    Y_Box.Background = new SolidColorBrush(Colors.Green);
                    greenY = true;
                }
            }
            catch
            {
                Y_Box.Background = new SolidColorBrush(Colors.Red);
                greenY = false;
            }
        }
        private void Z_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Z_Box.Text == "")
                {
                    Z_Box.Background = new SolidColorBrush(Colors.Yellow);
                    greenZ = false;
                    return;
                }
                if (double.Parse(Z_Box.Text) < -500 || double.Parse(Z_Box.Text) > 500)
                {
                    Z_Box.Text = ((double)(0)).ToString();
                    Z_Box.Background = new SolidColorBrush(Colors.Red);
                    greenZ = false;
                }
                else
                {
                    Z_Box.Background = new SolidColorBrush(Colors.Green);
                    greenZ = true;
                }
            }
            catch
            {
                Z_Box.Background = new SolidColorBrush(Colors.Red);
                greenZ = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isAllGreen())
            {
                MessageBox.Show("Not all parameters are set to valid values.");
                return;
            }
            editInProgress = true;
            ComboBoxItem cbi = (ComboBoxItem)ShapeBox.SelectedItem;
            ComboBoxItem cbi2 = (ComboBoxItem)ColorBox.SelectedItem;
            if (cbi == null || cbi2 == null) return;
            string shape = cbi.Content.ToString();
            string color = cbi2.Content.ToString();

            EnvironmentObject newShape = null;
            if (shape == "Cube")
            {
                newShape = new Cube
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Sphere")
            {
                newShape = new Sphere
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Cylinder")
            {
                newShape = new Cylinder
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Cone")
            {
                newShape = new Cone
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Wall")
            {
                newShape = new Wall
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if ( shape == "Triangle2D")
            {
                newShape = new Triangle2D
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }

            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (parent == null) return;
            parent.ourThings.Add(newShape);
            ShapeCollection.Items.Insert(0, new ListBoxItem()
            {
                Content = newShape,
                ToolTip = "a Shape in the 3D Environment",
            });
            ShapeCollection.SelectedIndex = 0;
            NotifyViewersOfListChange();
            editInProgress = false;
        }


        private void ShapeCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShapeCollection.SelectedIndex == -1)
            {
                RemoveButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                AddButton.IsEnabled = true;
            }
            else
            {
                RemoveButton.IsEnabled = true;
                EditButton.IsEnabled = true;
                AddButton.IsEnabled = true;
            }
            if (ShapeCollection.SelectedIndex != -1 && !editInProgress)
            {
                int shapeIndex = ShapeCollection.SelectedIndex;
                ListBoxItem selectedShape = (ListBoxItem)ShapeCollection.ItemContainerGenerator.ContainerFromIndex(shapeIndex);
                EnvironmentObject editThisShape = selectedShape.Content as EnvironmentObject;
                if (editThisShape == null)
                {
                    MessageBox.Show("Could not get shape from ListBox.");
                    return;
                }
                ShapeBox.Text = editThisShape.shape.ToString();
                ColorBox.Text = editThisShape.color.ToString();
                SizeBox.Text = editThisShape.size.ToString();
                Point3DPlus thisPosition = editThisShape.position;
                X_Box.Text = thisPosition.X.ToString();
                Y_Box.Text = thisPosition.Y.ToString();
                Z_Box.Text = thisPosition.Z.ToString();
                RotateBox.Text = editThisShape.rotation.ToString();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedShape();
        }

        private void RemoveSelectedShape()
        {
            if (ShapeCollection.SelectedIndex != -1)
            {
                ListBoxItem toBeRemoved = (ListBoxItem)ShapeCollection.Items.GetItemAt(ShapeCollection.SelectedIndex);
                if (toBeRemoved == null) return;
                ShapeCollection.Items.RemoveAt(ShapeCollection.SelectedIndex);
                EnvironmentObject theObject = toBeRemoved.Content as EnvironmentObject;
                Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
                if (parent != null && parent.ourThings.Count > 0)
                {
                    foreach (var thing in parent.ourThings)
                    {
                        if (thing.ToString() == theObject.ToString())
                        {
                            parent.ourThings.Remove(thing);
                            break;
                        }
                    }
                }
                NotifyViewersOfListChange();
            }
        }

        public void NotifyViewersOfListChange()
        {
            Module3DSimModel theModule = (Module3DSimModel)base.ParentModule;
            if (theModule == null) return;
            Module3DSimControl theSimulatorControl = (Module3DSimControl)theModule.FindModule("3DSimControl");
            if (theSimulatorControl != null)
            {
                theSimulatorControl.recreateWorld = true;
            }
            Module3DSimView theSimulatorView = (Module3DSimView)theModule.FindModule("3DSimView");
            if (theSimulatorView != null)
            {
                theSimulatorView.recreateWorld = true;
            }
            Sallie.VideoQueue.Clear();
        }

        public void ClearEntryValues()
        {
            // We need to actually change all boxes to force validation
            SizeBox.Text = " ";
            X_Box.Text = " ";
            Y_Box.Text = " ";
            Z_Box.Text = " ";
            RotateBox.Text = " ";

            // but we want to start with empty values
            SizeBox.Text = "";
            X_Box.Text = "";
            Y_Box.Text = "";
            Z_Box.Text = "";
            RotateBox.Text = "";
        }

        private void RotateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (RotateBox.Text == "")
                {
                    RotateBox.Background = new SolidColorBrush(Colors.Yellow);
                    greenRotate = false;
                    return;
                }
                if (double.Parse(RotateBox.Text) < -90 || double.Parse(Z_Box.Text) > 90)
                {
                    RotateBox.Text = ((double)(0)).ToString();
                    RotateBox.Background = new SolidColorBrush(Colors.Red);
                    greenRotate = false;
                }
                else
                {
                    RotateBox.Background = new SolidColorBrush(Colors.Green);
                    greenRotate = true;
                }
            }
            catch
            {
                RotateBox.Background = new SolidColorBrush(Colors.Red);
                greenRotate = false;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            editInProgress = true;
            int shapeIndex = ShapeCollection.SelectedIndex;
            if (shapeIndex == -1) return;
            RemoveSelectedShape();
            if (!isAllGreen())
            {
                MessageBox.Show("Not all parameters are set to valid values.");
                return;
            }

            ComboBoxItem cbi = (ComboBoxItem)ShapeBox.SelectedItem;
            string shape = cbi.Content.ToString();
            ComboBoxItem cbi2 = (ComboBoxItem)ColorBox.SelectedItem;
            string color = cbi2.Content.ToString();

            EnvironmentObject newShape = null;
            if (shape == "Cube")
            {
                newShape = new Cube
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Sphere")
            {
                newShape = new Sphere
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Cylinder")
            {
                newShape = new Cylinder
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            else if (shape == "Cone")
            {
                newShape = new Cone
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            if (shape == "Wall")
            {
                newShape = new Wall
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            if (shape == "Triangle2D")
            {
                newShape = new Triangle2D
                {
                    color = color,
                    size = float.Parse(SizeBox.Text),
                    position = new Point3DPlus(float.Parse(X_Box.Text), float.Parse(Y_Box.Text), float.Parse(Z_Box.Text)),
                    rotation = float.Parse(RotateBox.Text),
                };
            }
            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (parent == null) return;
            parent.ourThings.Add(newShape);
            ShapeCollection.Items.Insert(shapeIndex, new ListBoxItem()
            {
                Content = newShape,
                ToolTip = "a Shape in the 3D Environment",
            });
            ShapeCollection.SelectedIndex = shapeIndex;
            // ClearEntryValues();
            NotifyViewersOfListChange();
            editInProgress = false;
        }

        private void Set_in_UKS_Click(object sender, RoutedEventArgs e)
        {
            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (parent != null)
            {
                parent.SetObjectsInUKS();
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (MessageBox.Show("ARE YOU SURE???", "CLEAR ALL", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                parent.ourThings.Clear();
                ShapeCollection.Items.Clear();
                NotifyViewersOfListChange();
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            //open a filestream of 3dsim object files and load selected file
            loadHandler();
        }

        private bool loadHandler()
        {
            MainWindow.SuspendEngine();
            Sallie.VideoQueue.Clear();
            string defaultPath = Utils.GetOrAddLocalSubFolder(Utils.FolderModelObjects);
            using System.Windows.Forms.OpenFileDialog openFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleModelLoad,
                Multiselect = false,
                InitialDirectory = defaultPath
            };
            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (LoadFile(openFileDialog.FileName))
                {
                    openFileDialog.Dispose();
                }
                MainWindow.ResumeEngine();
                return true;
            }
            openFileDialog.Dispose();
            MainWindow.ResumeEngine();
            return false;
        }

        private bool LoadFile(string fileName)
        {
            bool fromClipboard = fileName == "ClipBoard";
            Stream file;
            List<EnvironmentObject> loadedObjects;
            try
            {
                file = File.Open(fileName, FileMode.Open, FileAccess.Read);
            }
            catch (Exception e)
            {
                return false;
            }
            XmlSerializer reader1 = new XmlSerializer(typeof(List<EnvironmentObject>), XmlFile.GetModuleTypes());
            try
            {
                loadedObjects = (List<EnvironmentObject>)reader1.Deserialize(file);
            }
            catch (Exception e)
            {
                file.Close();
                MessageBox.Show("file load failed \r\n\r\n" + e.InnerException, "File Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return false;
            }

            file.Position = 0;
            //the above automatically loads the content of the neuronArray object but can't load the neurons themselves
            //because of formatting changes
            XmlDocument xmldoc = new XmlDocument();
            XmlNodeList neuronNodes;
            xmldoc.Load(file);
            file.Close();

            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            ShapeCollection.Items.Clear();
            parent.ourThings = loadedObjects;
            for (int i = 0; i < parent.ourThings.Count; i++)
            {
                ShapeCollection.Items.Insert(i, new ListBoxItem()
                {
                    Content = parent.ourThings[i],
                    ToolTip = "a Shape in the 3D Environment",
                });
            }
            NotifyViewersOfListChange();

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //save all current objects to an xml file
            SaveHandler();
        }

        private bool SaveHandler()
        {
            string defaultPath = Utils.GetOrAddLocalSubFolder(Utils.FolderModelObjects);
            System.Windows.Forms.SaveFileDialog saveFileDialog = new()
            {
                Filter = Utils.FilterXMLs,
                Title = Utils.TitleModelSave,
                InitialDirectory = defaultPath
            };
            System.Windows.Forms.DialogResult result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (SaveFile(saveFileDialog.FileName))
                {
                    saveFileDialog.Dispose();
                    return true;
                }
            }
            saveFileDialog.Dispose();
            return false;
        }

        private bool SaveFile(string fileName)
        {
            Stream file;
            string tempFile = "";
            file = File.Create(fileName);

            Module3DSimModel parent = (Module3DSimModel)base.ParentModule;
            if (parent.ourThings != null || parent.ourThings.Count != 0)
            {

                Type[] extraTypes = XmlFile.GetModuleTypes();
                XmlSerializer writer = new XmlSerializer(typeof(List<EnvironmentObject>), extraTypes);
                writer.Serialize(file, parent.ourThings);

                file.Close();

                currentFileName = fileName;
                return true;

            }
            return false;
        }

        public static bool CanWriteTo(string fileName, out string message)
        {
            FileStream file1;
            message = "";
            if (File.Exists(fileName))
            {
                try
                {
                    file1 = File.Open(fileName, FileMode.Open);
                    file1.Close();
                    return true;
                }
                catch (Exception e)
                {
                    message = e.Message;
                    return false;
                }
            }
            return true;

        }
    }
}
