using System.Collections.ObjectModel;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        public ObservableCollection<ModuleX> modules = new ();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            for (int i = 0; i < 10; i++)
            {
                modules.Add(new ModuleX(i.ToString()));
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            //if (count == 1)
            //    CounterBtn.Text = $"Clicked {count} time";
            //else
            //    CounterBtn.Text = $"Clicked {count} times";

            //SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
    public class ModuleX
    {
        public string name { get; set; }
        public ModuleX(string name)
        { this.name = name; }
    }

}
