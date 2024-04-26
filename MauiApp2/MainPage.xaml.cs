namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            myLabel.Text = "Fred " + count;
            SemanticScreenReader.Announce(CounterBtn.Text);
            myStack.Children.Add(new Label { Text = "abc" + count });
        }
    }

}
