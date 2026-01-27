using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BrainSimulator.Modules;

public partial class ModuleSpellDlg : ModuleBaseDlg
{
    public ModuleSpellDlg()
    {
        InitializeComponent();
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        AddCurrentWord();
    }

    private void txtWord_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddCurrentWord();
        }
    }

    private void AddCurrentWord()
    {
        string word = txtWord.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(word))
        {
            SetStatus("Please enter a word.");
            return;
        }

        var module = ParentModule as ModuleSpell;
        if (module != null)
        {
            module.AddWordSpelling(word);
            SetStatus($"Spelling added: {word}", Colors.Black);
            txtWord.Clear();
            txtWord.Focus();
        }
        else
        {
            SetStatus("Error: Module not found.", Colors.Red);
        }
    }

    private void btnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Word List File",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            txtFilePath.Text = openFileDialog.FileName;
        }
    }

    private void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        string filePath = txtFilePath.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(filePath))
        {
            SetStatus("Please select a file first.");
            return;
        }

        if (!File.Exists(filePath))
        {
            SetStatus("File not found.");
            return;
        }

        var module = ParentModule as ModuleSpell;
        if (module != null)
        {
            SetStatus("Loading words...");
            
            int count = module.LoadWordsFromFile(filePath);
            
            SetStatus($"Successfully loaded {count} word(s) from file.");
        }
        else
        {
            SetStatus("Error: Module not found.");
        }
    }
}