using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using LightsOutCube.Model;
using LightsOutCube.ViewModels;
using System.Globalization;
using LightsOutCube.Utilities;

namespace LightsOutCube.Controls
{
    public partial class CelebrationView : UserControl
    {
        public CelebrationView()
        {
            InitializeComponent();
        }

        public void Show(SpeedRunSummary summary, IEnumerable runs)
        {
            try
            {
                Visibility = Visibility.Visible;
                SubtitleText.Text = $"You completed {summary.SolvedCount} puzzles";
                var ts = TimeSpan.FromMilliseconds(summary.TotalElapsedMs);
                var timeStr = ts.TotalMinutes >= 1 ? ts.ToString(@"mm\:ss\.fff") : ts.ToString(@"s\.fff") + "s";
                TimeText.Text = $"in {timeStr}!";
                SpeedRunsList.ItemsSource = runs;

                // Highlight the matching run in the list: match by timestamp formatted the same way used in AboutViewModel
                var formatted = summary.Timestamp.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
                int rank = 1;
                foreach (var item in runs)
                {
                    if (item is SpeedRunEntryWrapper wrapper)
                    {
                        wrapper.IsLatest = string.Equals(wrapper.Timestamp, formatted, StringComparison.CurrentCulture);
                        rank = wrapper.Rank;
                    }
                }
                if (rank == 1) 
                {
                    HighScoreText.Visibility = Visibility.Visible; 
                }
                else
                {
                    HighScoreText.Visibility = Visibility.Collapsed;
                }
                // ensure UI scrolls to the latest entry
                try { SpeedRunsList.AnimateLatestVisible(TimeSpan.FromSeconds(5)); } catch { /* Ignore */ }
                // start confetti and ribbons using centralized Effects helper, routing shapes into CelebrationView
                Effects.StartConfetti(this, (elem) => AddConfetti(elem), (elem) => RemoveConfetti(elem));
                Effects.StartRibbons(this, (elem) => AddRibbon(elem));
                // also start fireworks particle bursts
                Effects.StartFireworks(this, (elem) => AddConfetti(elem), (elem) => RemoveConfetti(elem));
            }
            catch { /* best-effort */ }
        }

        public void Hide()
        {
            Visibility = Visibility.Collapsed;
        }

        // Methods to allow outer code to add/remove visual elements (confetti/ribbons)
        public void AddConfetti(UIElement element)
        {
            try { Dispatcher.Invoke(() => ConfettiCanvas.Children.Add(element)); } catch { /* best-effort */ }
        }

        public void RemoveConfetti(UIElement element)
        {
            try { Dispatcher.Invoke(() => ConfettiCanvas.Children.Remove(element)); } catch { /* best-effort */ }
        }

        public void AddRibbon(UIElement element)
        {
            try { Dispatcher.Invoke(() => RibbonCanvas.Children.Add(element)); } catch { /* best-effort */ }
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
