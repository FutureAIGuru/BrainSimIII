using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for NotesDialob.xaml
    /// </summary>
    public partial class NotesDialog : Window
    {
        private void OKbutton_Click(object sender, RoutedEventArgs e)
        {
            if (!mainRTB.IsReadOnly)
            {
                TextRange range;
                var a = mainRTB.Document;
                range = new TextRange(mainRTB.Document.ContentStart, mainRTB.Document.ContentEnd);
                MemoryStream stream = new MemoryStream();
                range.Save(stream, DataFormats.Xaml);
                string xamlText = Encoding.UTF8.GetString(stream.ToArray());

                //strip all the hot links back out again
                int beg = 0;
                while (xamlText.IndexOf("<Hyperlink", beg) != -1)
                {
                    beg = xamlText.IndexOf("<Hyperlink", beg);
                    int end = xamlText.IndexOf(">", beg);
                    xamlText = xamlText.Remove(beg, end - beg + 1);
                }
                xamlText = xamlText.Replace("</Hyperlink>", "");

                // MainWindow.networkNotes = xamlText;
            }
            // MainWindow.hideNotes = (bool)checkBox.IsChecked;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //we use the text showing, not the hyperlink address
        //You have to press ctrl to follow hyperlinks when editing
        private void MainRTB_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0 || (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) > 0 || rtb.IsReadOnly)
            {
                if (e.OriginalSource is Run r)
                {
                    if (r.Parent is Hyperlink hyperlink)
                    {
                        Uri innerText = new Uri(r.Text);
                        Process.Start(new ProcessStartInfo(innerText.AbsoluteUri));
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
