using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FormatAllFiles2
{
    /// <summary>
    /// WPF user control for the Format All Files options page.
    /// </summary>
    public partial class OptionsControl : UserControl
    {
        private bool initialized;

        public OptionsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the control with current settings values.
        /// Must be called after the control is created and before it is displayed.
        /// </summary>
        public void LoadSettings(string fileExtensions, FormatCommandPreset preset, string customFormatCommands)
        {
            initialized = false;

            FileExtensionsTextBox.Text = fileExtensions ?? string.Empty;
            CustomFormatCommandsTextBox.Text = customFormatCommands ?? string.Empty;

            // Select the matching combo box item
            var presetTag = preset.ToString();
            foreach (ComboBoxItem item in FormatCommandComboBox.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == presetTag)
                {
                    FormatCommandComboBox.SelectedItem = item;
                    break;
                }
            }

            UpdateCustomCommandsVisibility();

            initialized = true;
        }

        /// <summary>
        /// Gets the current file extensions value from the text box.
        /// </summary>
        public string FileExtensionsValue => FileExtensionsTextBox.Text;

        /// <summary>
        /// Gets the selected format command preset.
        /// </summary>
        public FormatCommandPreset PresetValue
        {
            get
            {
                if (FormatCommandComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (Enum.TryParse<FormatCommandPreset>(item.Tag.ToString(), out var preset))
                        return preset;
                }
                return FormatCommandPreset.FormatDocument;
            }
        }

        /// <summary>
        /// Gets the custom format commands text.
        /// </summary>
        public string CustomFormatCommandsValue => CustomFormatCommandsTextBox.Text;

        /// <summary>
        /// Event raised when any setting value changes.
        /// </summary>
        public event EventHandler SettingsChanged;

        private void FileExtensionsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (initialized)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void FormatCommandComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCustomCommandsVisibility();
            if (initialized)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CustomFormatCommandsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (initialized)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCustomCommandsVisibility()
        {
            var isCustom = PresetValue == FormatCommandPreset.Custom;
            var visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            CustomCommandsLabel.Visibility = visibility;
            CustomFormatCommandsTextBox.Visibility = visibility;
            CustomCommandsHint.Visibility = visibility;
        }
    }
}
