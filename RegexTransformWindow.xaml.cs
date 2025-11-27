using System;
using System.Windows;
using System.Windows.Input;
using ClipMaster.Models;
using ClipMaster.Services;

namespace ClipMaster
{
    public partial class RegexTransformWindow : Window
    {
        private readonly RegexLibraryService _regexService;
        private string _originalText;
        private RegexPattern? _currentPattern;
        private bool _isLoadingPattern;
        private bool _isInitialized;

        public string TransformedText { get; private set; } = string.Empty;
        public bool Applied { get; private set; } = false;

        public RegexTransformWindow(string inputText)
        {
            _regexService = new RegexLibraryService();
            _originalText = inputText;

            InitializeComponent();

            InputTextBox.Text = inputText;
            LoadPatternLibrary();
            _isInitialized = true;
        }

        private void LoadPatternLibrary()
        {
            PatternListBox.ItemsSource = _regexService.Patterns;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PatternListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PatternListBox.SelectedItem is RegexPattern pattern)
            {
                _isLoadingPattern = true;
                _currentPattern = pattern;

                PatternNameTextBox.Text = pattern.Name;
                PatternTextBox.Text = pattern.Pattern;
                ReplacementTextBox.Text = pattern.Replacement;
                DescriptionTextBox.Text = pattern.Description;
                GlobalCheckBox.IsChecked = pattern.IsGlobal;
                CaseSensitiveCheckBox.IsChecked = pattern.IsCaseSensitive;
                MultilineCheckBox.IsChecked = pattern.IsMultiline;

                _isLoadingPattern = false;
                UpdatePreview();
            }
        }

        private void NewPatternButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPattern = null;
            PatternListBox.SelectedItem = null;

            PatternNameTextBox.Text = "New Pattern";
            PatternTextBox.Text = "";
            ReplacementTextBox.Text = "";
            DescriptionTextBox.Text = "";
            GlobalCheckBox.IsChecked = true;
            CaseSensitiveCheckBox.IsChecked = true;
            MultilineCheckBox.IsChecked = false;

            PatternTextBox.Focus();
        }

        private void DeletePatternButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPattern != null)
            {
                var result = MessageBox.Show(
                    $"Delete pattern '{_currentPattern.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _regexService.DeletePattern(_currentPattern.Id);
                    _currentPattern = null;
                    LoadPatternLibrary();
                    NewPatternButton_Click(sender, e);
                }
            }
        }

        private void SavePatternButton_Click(object sender, RoutedEventArgs e)
        {
            var name = PatternNameTextBox.Text.Trim();
            var pattern = PatternTextBox.Text;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a pattern name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(pattern))
            {
                MessageBox.Show("Please enter a regex pattern.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (isValid, error) = _regexService.ValidatePattern(pattern);
            if (!isValid)
            {
                MessageBox.Show($"Invalid regex pattern: {error}", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_currentPattern != null)
            {
                // Update existing
                _currentPattern.Name = name;
                _currentPattern.Pattern = pattern;
                _currentPattern.Replacement = ReplacementTextBox.Text;
                _currentPattern.Description = DescriptionTextBox.Text;
                _currentPattern.IsGlobal = GlobalCheckBox.IsChecked ?? true;
                _currentPattern.IsCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? true;
                _currentPattern.IsMultiline = MultilineCheckBox.IsChecked ?? false;

                _regexService.UpdatePattern(_currentPattern);
            }
            else
            {
                // Create new
                var newPattern = new RegexPattern
                {
                    Name = name,
                    Pattern = pattern,
                    Replacement = ReplacementTextBox.Text,
                    Description = DescriptionTextBox.Text,
                    IsGlobal = GlobalCheckBox.IsChecked ?? true,
                    IsCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? true,
                    IsMultiline = MultilineCheckBox.IsChecked ?? false
                };

                _regexService.AddPattern(newPattern);
                _currentPattern = newPattern;
            }

            LoadPatternLibrary();
            MessageBox.Show("Pattern saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            TransformedText = OutputTextBox.Text;
            Applied = true;
            DialogResult = true;
            Close();
        }

        private void PatternTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isLoadingPattern)
            {
                ValidateAndUpdatePreview();
            }
        }

        private void ReplacementTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isLoadingPattern)
            {
                UpdatePreview();
            }
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void OptionsChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitialized && !_isLoadingPattern)
            {
                UpdatePreview();
            }
        }

        private void ValidateAndUpdatePreview()
        {
            var pattern = PatternTextBox.Text;

            if (string.IsNullOrEmpty(pattern))
            {
                ValidationTextBlock.Text = "Enter a regex pattern";
                ValidationTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                OutputTextBox.Text = InputTextBox.Text;
                MatchCountTextBlock.Text = "";
                return;
            }

            var (isValid, error) = _regexService.ValidatePattern(pattern);

            if (isValid)
            {
                ValidationTextBlock.Text = "✓ Valid regex pattern";
                ValidationTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                UpdatePreview();
            }
            else
            {
                ValidationTextBlock.Text = $"✗ {error}";
                ValidationTextBlock.Foreground = System.Windows.Media.Brushes.OrangeRed;
                OutputTextBox.Text = InputTextBox.Text;
                MatchCountTextBlock.Text = "";
            }
        }

        private void UpdatePreview()
        {
            var input = InputTextBox.Text;
            var pattern = PatternTextBox.Text;
            var replacement = ReplacementTextBox.Text;

            if (string.IsNullOrEmpty(pattern))
            {
                OutputTextBox.Text = input;
                MatchCountTextBlock.Text = "";
                return;
            }

            var (isValid, _) = _regexService.ValidatePattern(pattern);
            if (!isValid)
            {
                return;
            }

            var isGlobal = GlobalCheckBox.IsChecked ?? true;
            var isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? true;
            var isMultiline = MultilineCheckBox.IsChecked ?? false;

            // Get match count
            var matches = _regexService.GetMatches(input, pattern, isCaseSensitive, isMultiline);
            MatchCountTextBlock.Text = $"({matches.Count} match{(matches.Count != 1 ? "es" : "")})";

            // Apply transformation
            var result = _regexService.ApplyCustomRegex(input, pattern, replacement, isGlobal, isCaseSensitive, isMultiline);
            OutputTextBox.Text = result;
        }
    }
}
