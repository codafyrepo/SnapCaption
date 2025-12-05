using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Diagnostics;

using SnapCaption.utils;

using Button = Wpf.Ui.Controls.Button;

namespace SnapCaption
{
    public partial class MainWindow : FluentWindow
    {
        public bool IsAutoHeight { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                RootNavigation.Navigate(typeof(CaptionPage));
                IsAutoHeight = true;
                CheckForFirstUse();
            };

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            var windowState = WindowHandler.LoadState(this, Translator.Setting);
            if (windowState.Left <= 0 || windowState.Left >= screenWidth || 
                windowState.Top <= 0 || windowState.Top >= screenHeight)
            {
                WindowHandler.RestoreState(this, new Rect(
                    (screenWidth - 775) / 2, screenHeight * 3 / 4 - 167, 775, 167));
            }
            else
                WindowHandler.RestoreState(this, windowState);

            ToggleTopmost(Translator.Setting.MainWindow.Topmost);
            UpdateOriginalCaptionButton(Translator.Setting.MainWindow.OriginalCaptionVisible);
            UpdateNativeCaptionsButton(Translator.Setting.MainWindow.NativeCaptionsVisible);
            UpdateHistoryLockButton(Translator.Setting.MainWindow.HistorySelectable);
        }

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!this.Topmost);
        }

        private void LogOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (Translator.LogOnlyFlag)
            {
                Translator.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                Translator.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }
        }

        private void OriginalCaptionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            Translator.Setting.MainWindow.OriginalCaptionVisible = !Translator.Setting.MainWindow.OriginalCaptionVisible;
            symbolIcon.Filled = Translator.Setting.MainWindow.OriginalCaptionVisible;

            CaptionPage.Instance?.UpdateOriginalCaptionVisibility(Translator.Setting.MainWindow.OriginalCaptionVisible);
        }

        private void NativeCaptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            Translator.Setting.MainWindow.NativeCaptionsVisible = !Translator.Setting.MainWindow.NativeCaptionsVisible;
            symbolIcon.Filled = Translator.Setting.MainWindow.NativeCaptionsVisible;

            if (Translator.Setting.MainWindow.NativeCaptionsVisible)
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
            else
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
        }

        private void HistoryLockButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            Translator.Setting.MainWindow.HistorySelectable = !Translator.Setting.MainWindow.HistorySelectable;
            symbolIcon.Filled = !Translator.Setting.MainWindow.HistorySelectable;

            CaptionPage.Instance?.UpdateHistoryCursorAndSelection(Translator.Setting.MainWindow.HistorySelectable);
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all captions and history
            Translator.ClearAllCaptions();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowHandler.SaveState(window, Translator.Setting);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainWindow_LocationChanged(sender, e);
            IsAutoHeight = false;
        }

        public void ToggleTopmost(bool enabled)
        {
            var button = TopmostButton as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            symbolIcon.Filled = enabled;
            this.Topmost = enabled;
            Translator.Setting.MainWindow.Topmost = enabled;
        }

        private void CheckForFirstUse()
        {
            if (!Translator.FirstUseFlag)
                return;

            RootNavigation.Navigate(typeof(SettingPage));
            LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
        }

        public void AutoHeightAdjust(int minHeight = -1, int maxHeight = -1)
        {
            if (minHeight > 0 && Height < minHeight)
            {
                Height = minHeight;
                IsAutoHeight = true;
            }

            if (IsAutoHeight && maxHeight > 0 && Height > maxHeight)
                Height = maxHeight;
        }

        public void ShowSnackbar(string title, string message, bool isError = false)
        {
            var snackbar = new Snackbar(SnackbarHost)
            {
                Title = title,
                Content = message,
                Appearance = isError ? ControlAppearance.Danger : ControlAppearance.Light,
                Timeout = TimeSpan.FromSeconds(5)
            };
            snackbar.Show();
        }

        private void UpdateOriginalCaptionButton(bool isVisible)
        {
            if (OriginalCaptionButton.Icon is SymbolIcon icon)
            {
                icon.Filled = isVisible;
            }
        }

        private void UpdateNativeCaptionsButton(bool isVisible)
        {
            if (NativeCaptionsButton.Icon is SymbolIcon icon)
            {
                icon.Filled = isVisible;
            }
        }

        private void UpdateHistoryLockButton(bool isSelectable)
        {
            if (HistoryLockButton.Icon is SymbolIcon icon)
            {
                icon.Filled = !isSelectable;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Translator.IsExiting = true;
            if (Translator.Window != null)
            {
                try
                {
                    LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to kill LiveCaptions: {ex.Message}");
                }
            }
        }
    }
}
