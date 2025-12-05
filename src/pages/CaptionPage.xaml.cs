using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using SnapCaption.utils;

namespace SnapCaption
{
    public partial class CaptionPage : Page
    {
        public const int CARD_HEIGHT = 110;

        private static CaptionPage instance;
        public static CaptionPage Instance => instance;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            // Subscribe to events immediately
            Translator.Caption.PropertyChanged += TranslatedChanged;
            Translator.Caption.PropertyChanged += DisplayContextsChanged;
            if (Translator.Setting?.MainWindow != null)
            {
                Translator.Setting.MainWindow.PropertyChanged += MainWindow_PropertyChanged;
                Translator.Setting.MainWindow.PropertyChanged += MainWindow_HistorySelectableChanged;
            }

            Loaded += (s, e) =>
            {
                // Page loaded
            };

            Unloaded += (s, e) =>
            {
                // Page unloaded
            };

            // Initialize visibility based on setting
            if (Translator.Setting?.MainWindow != null)
            {
                UpdateOriginalCaptionVisibility(Translator.Setting.MainWindow.OriginalCaptionVisible);
                UpdateHistoryCursorAndSelection(Translator.Setting.MainWindow.HistorySelectable);
            }

            // Always attach copy handler to history
            HistoryTextBox.MouseLeftButtonDown += HistoryTextBox_CopyAll;
        }

        // Destructor or Unloaded to unsubscribe
        ~CaptionPage()
        {
             Translator.Caption.PropertyChanged -= TranslatedChanged;
             Translator.Caption.PropertyChanged -= DisplayContextsChanged;
             if (Translator.Setting?.MainWindow != null)
             {
                Translator.Setting.MainWindow.PropertyChanged -= MainWindow_PropertyChanged;
                Translator.Setting.MainWindow.PropertyChanged -= MainWindow_HistorySelectableChanged;
             }
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    textBlock.ToolTip = "Copied!";
                }
                catch
                {
                    textBlock.ToolTip = "Error to Copy";
                }
                await Task.Delay(500);
                textBlock.ToolTip = "Click to Copy";
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            // Font size is now fixed at 18, no dynamic resizing
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // This can be used for future property change handlers if needed
        }

        private void DisplayContextsChanged(object sender, PropertyChangedEventArgs e)
        {
            // Auto-scroll to bottom when AccumulatedHistory changes
            if (e.PropertyName == nameof(Translator.Caption.AccumulatedHistory) || 
                e.PropertyName == "AccumulatedHistory")
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Move caret to end and scroll to end
                    HistoryTextBox.CaretIndex = HistoryTextBox.Text.Length;
                    HistoryTextBox.ScrollToEnd();
                }), DispatcherPriority.Background);
            }
        }

        private void MainWindow_HistorySelectableChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Translator.Setting.MainWindow.HistorySelectable) || 
                e.PropertyName == "HistorySelectable")
            {
                UpdateHistoryCursorAndSelection(Translator.Setting.MainWindow.HistorySelectable);
            }
        }

        public void UpdateOriginalCaptionVisibility(bool isVisible)
        {
            var converter = new GridLengthConverter();

            if (isVisible)
            {
                // Show original caption: 75px for caption, 400px for history
                OriginalCaption_Row.Height = (GridLength)converter.ConvertFromString("75");
                CaptionLogCard_Row.Height = (GridLength)converter.ConvertFromString("400");
                CaptionLogCard.Height = 400;
                HistoryTextBox.Height = 384;
            }
            else
            {
                // Hide original caption: 0px for caption, 475px for history (400 + 75)
                OriginalCaption_Row.Height = (GridLength)converter.ConvertFromString("0");
                CaptionLogCard_Row.Height = (GridLength)converter.ConvertFromString("475");
                CaptionLogCard.Height = 475;
                HistoryTextBox.Height = 459;
            }
        }

        private async void HistoryTextBox_CopyAll(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(HistoryTextBox.Text))
            {
                try
                {
                    Clipboard.SetText(HistoryTextBox.Text);
                    var originalToolTip = HistoryTextBox.ToolTip;
                    HistoryTextBox.ToolTip = "Copied!";
                    await Task.Delay(1000);
                    HistoryTextBox.ToolTip = originalToolTip;
                }
                catch
                {
                    var originalToolTip = HistoryTextBox.ToolTip;
                    HistoryTextBox.ToolTip = "Error to Copy";
                    await Task.Delay(1000);
                    HistoryTextBox.ToolTip = originalToolTip;
                }
            }
        }

        public void UpdateHistoryCursorAndSelection(bool isSelectable)
        {
            if (isSelectable)
            {
                // Unlocked: Allow text selection, show IBeam cursor
                HistoryTextBox.Cursor = System.Windows.Input.Cursors.IBeam;
                HistoryTextBox.ToolTip = "Select text to copy";
            }
            else
            {
                // Locked: Copy all on click, show Hand cursor
                HistoryTextBox.Cursor = System.Windows.Input.Cursors.Hand;
                HistoryTextBox.ToolTip = "Click to Copy All";
            }
        }
    }
}
