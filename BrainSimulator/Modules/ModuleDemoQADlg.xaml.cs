using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrainSimulator.Modules
{
    public partial class ModuleDemoQADlg : ModuleBaseDlg
    {
        private enum ProviderType { openai, ollama }
        private ProviderType _provider = ProviderType.openai;

        public ModuleDemoQADlg()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                providerCombo.SelectedIndex = 0; // fires after all named elements exist
            };
        }


        private void AppendMessage(string speaker, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            chatTranscript.AppendText($"{speaker}: {message.Trim()}\n");
            chatTranscript.CaretIndex = chatTranscript.Text.Length;
            chatTranscript.ScrollToEnd();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendCurrentInputAsync();
        }

        private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true; // prevent newline
                await SendCurrentInputAsync();
            }
        }

        private async Task SendCurrentInputAsync()
        {
            var userText = chatInput.Text;
            if (string.IsNullOrWhiteSpace(userText)) return;

            chatInput.IsEnabled = false;
            sendButton.IsEnabled = false;

            AppendMessage("You", userText);
            chatInput.Clear();

            try
            {
                // Replace this with your actual chatbot call
                var botReply = await CallChatbotAsync(userText);
                AppendMessage("Bot", botReply);
            }
            catch (Exception ex)
            {
                AppendMessage("System", $"Error: {ex.Message}");
            }
            finally
            {
                chatInput.IsEnabled = true;
                sendButton.IsEnabled = true;
                chatInput.Focus();
            }
        }

        /// <summary>
        /// Return a string reply to show in the transcript.
        /// TODO: Integrate with UKS, for now it just is pure LLMs.
        /// </summary>
        private async Task<string> CallChatbotAsync(string userText)
        {
            var reply = await GPT.RunTextAsync(userText, "You are the QA Demo Bot. ", _provider.ToString());
            return reply?.Trim() ?? "(no reply)";
        }

        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _provider = providerCombo.SelectedIndex == 1 ? ProviderType.ollama : ProviderType.openai;
            AppendMessage("System", $"Provider set to {_provider.ToString()}.");
        }
    }

}
