using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pastomatic.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly IConfiguration? _configuration;
        private readonly string _settingsFilePath;

        private const string DefaultSystemPrompt =
            "You are an expert image analyst. Provide an extremely detailed description of what you see in this image. Describe:\n" +
            "- All text content (transcribe exactly as shown)\n" +
            "- UI elements, buttons, menus, dialogs\n" +
            "- Code snippets (format as markdown code blocks with language)\n" +
            "- Diagrams, charts, graphs (describe structure and data)\n" +
            "- Colors, layout, positioning\n" +
            "- Any error messages or notifications\n\n" +
            "Format your response as clean markdown. Be thorough - the description will be used by someone who cannot see the image.";

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }

        public SettingsWindow(IConfiguration configuration) : this()
        {
            _configuration = configuration;
            LoadSettingsFromConfiguration();
        }

        private void LoadSettingsFromConfiguration()
        {
            if (_configuration == null) return;

            // General tab
            SelectComboBoxByTag(HotkeyComboBox, _configuration.GetValue<string>("Hotkey:Key", "Insert")!);
            SaveFolderTextBox.Text = _configuration.GetValue<string>("Image:SaveFolder", @"E:\claude\images");
            MaxMegapixelsSlider.Value = _configuration.GetValue<double>("Image:MaxMegapixels", 2.0);
            SelectComboBoxByTag(ClipboardFormatComboBox, _configuration.GetValue<string>("Image:ClipboardFormat", "Read {wslpath}")!);
            CloseOnFocusLossCheckBox.IsChecked = _configuration.GetValue<bool>("UI:CloseOnFocusLoss", true);

            // Vision LLM tab
            EndpointTextBox.Text = _configuration.GetValue<string>("Vision:Endpoint", "http://10.0.0.244:8000/v1");
            ModelTextBox.Text = _configuration.GetValue<string>("Vision:Model", "qwen2.5-vl-72b-instruct");
            ApiKeyPasswordBox.Password = _configuration.GetValue<string>("Vision:ApiKey", "");
            MaxTokensTextBox.Text = _configuration.GetValue<int>("Vision:MaxTokens", 4096).ToString();
            TemperatureSlider.Value = _configuration.GetValue<double>("Vision:Temperature", 0.1);
            TimeoutTextBox.Text = _configuration.GetValue<int>("Vision:TimeoutSeconds", 120).ToString();
            SystemPromptTextBox.Text = _configuration.GetValue<string>("Vision:SystemPrompt", DefaultSystemPrompt);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveSettingsAsync();
                MessageBox.Show("Settings saved. Restart for hotkey changes to take effect.",
                    "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveSettingsAsync()
        {
            string jsonString = File.Exists(_settingsFilePath)
                ? await File.ReadAllTextAsync(_settingsFilePath)
                : "{}";

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement.Clone();

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                var propertiesToSkip = new HashSet<string>
                {
                    "Hotkey", "Image", "Vision", "UI", "Logging"
                };

                foreach (var property in root.EnumerateObject())
                {
                    if (!propertiesToSkip.Contains(property.Name))
                        property.WriteTo(writer);
                }

                // Hotkey
                writer.WritePropertyName("Hotkey");
                writer.WriteStartObject();
                writer.WriteString("Key", (HotkeyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Insert");
                writer.WriteBoolean("Suppress", true);
                writer.WriteEndObject();

                // Image
                writer.WritePropertyName("Image");
                writer.WriteStartObject();
                writer.WriteNumber("MaxMegapixels", MaxMegapixelsSlider.Value);
                writer.WriteString("SaveFolder", SaveFolderTextBox.Text);
                writer.WriteString("ClipboardFormat", (ClipboardFormatComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Read {wslpath}");
                writer.WriteEndObject();

                // Vision
                writer.WritePropertyName("Vision");
                writer.WriteStartObject();
                writer.WriteString("Endpoint", EndpointTextBox.Text);
                writer.WriteString("Model", ModelTextBox.Text);
                writer.WriteString("ApiKey", ApiKeyPasswordBox.Password);
                writer.WriteNumber("MaxTokens", int.TryParse(MaxTokensTextBox.Text, out int maxTokens) ? maxTokens : 4096);
                writer.WriteNumber("Temperature", TemperatureSlider.Value);
                writer.WriteNumber("TimeoutSeconds", int.TryParse(TimeoutTextBox.Text, out int timeout) ? timeout : 120);
                writer.WriteString("SystemPrompt", SystemPromptTextBox.Text);
                writer.WriteEndObject();

                // UI
                writer.WritePropertyName("UI");
                writer.WriteStartObject();
                writer.WriteBoolean("CloseOnFocusLoss", CloseOnFocusLossCheckBox.IsChecked ?? true);
                writer.WriteNumber("SuccessDisplayMs", 1500);
                writer.WriteEndObject();

                // Logging (preserve)
                writer.WritePropertyName("Logging");
                writer.WriteStartObject();
                writer.WriteString("MinimumLevel", "Information");
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Use COM-based folder picker (no WinForms dependency)
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Image Save Folder",
                InitialDirectory = SaveFolderTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                SaveFolderTextBox.Text = dialog.FolderName;
            }
        }

        private void ResetPromptButton_Click(object sender, RoutedEventArgs e)
        {
            SystemPromptTextBox.Text = DefaultSystemPrompt;
        }

        private void SelectComboBoxByTag(ComboBox comboBox, string tagValue)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), tagValue, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }
    }
}
