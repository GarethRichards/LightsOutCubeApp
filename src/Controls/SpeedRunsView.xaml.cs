using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LightsOutCube.ViewModels;
using System.Diagnostics;
using System.Windows.Media;

namespace LightsOutCube.Controls
{
    public partial class SpeedRunsView : UserControl
    {
        public SpeedRunsView()
        {
            InitializeComponent();
        }

        // Ensure the TreeView scrolls so the SpeedRunEntryWrapper with IsLatest==true is visible
        public void EnsureLatestVisible()
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (ItemsSource == null) return;
                    foreach (var item in ItemsSource)
                    {
                        if (item is SpeedRunEntryWrapper wrapper && wrapper.IsLatest)
                        {
                            if (PART_TreeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                            {
                                tvi.IsSelected = true;
                                tvi.BringIntoView();
                            }
                            break;
                        }
                    }
                }), DispatcherPriority.Background);
            }
            catch { /* ignore */ }
        }

        // Animate the TreeView so that it starts displaying the worst-ranked entry first
        // and scrolls until the SpeedRunEntryWrapper with IsLatest==true is the first entry
        // visible. The animation runs for the provided duration.
        public void AnimateLatestVisible(TimeSpan duration)
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (ItemsSource == null) return;

                    var items = new System.Collections.Generic.List<object>();
                    foreach (var it in ItemsSource) items.Add(it);
                    if (items.Count == 0) return;

                    int latestIndex = items.FindIndex(i => i is SpeedRunEntryWrapper w && w.IsLatest);
                    if (latestIndex < 0) return;

                    var generator = PART_TreeView.ItemContainerGenerator;
                    PART_TreeView.UpdateLayout();

                    object worstItem = items[items.Count - 1];
                    object latestItem = items[latestIndex];

                    if (!(generator.ContainerFromItem(worstItem) is TreeViewItem worstTvi)
                        || !(generator.ContainerFromItem(latestItem) is TreeViewItem latestTvi))
                    {
                        // If containers are not ready, fall back to immediate ensure
                        EnsureLatestVisible();
                        return;
                    }

                    // find the ScrollViewer inside the TreeView template
                    ScrollViewer scroll = null;
                    // try to find descendant ScrollViewer
                    scroll = FindVisualChild<ScrollViewer>(PART_TreeView);
                    if (scroll == null)
                    {
                        EnsureLatestVisible();
                        return;
                    }
                    ScrollToSelectedEntry(duration, worstTvi, latestTvi, scroll);

                }), DispatcherPriority.Background);
            }
            catch { /* ignore */ }
        }

        private void ScrollToSelectedEntry(TimeSpan duration, TreeViewItem worstTvi, TreeViewItem latestTvi, ScrollViewer scroll)
        {
            try
            {
            // compute offsets so item top aligns with viewport top

            // position worst item at top (start)
            var worstPoint = worstTvi.TransformToAncestor(scroll).Transform(new Point(0, 0));
                double startOffset = scroll.VerticalOffset + worstPoint.Y;
                // apply start offset immediately
                scroll.ScrollToVerticalOffset(startOffset);

                // ensure layout updated after jump
                PART_TreeView.UpdateLayout();

                // compute target offset for latest item
                var latestPoint = latestTvi.TransformToAncestor(scroll).Transform(new Point(0, 0));
                double targetOffset = scroll.VerticalOffset + latestPoint.Y;

                // run an interpolated animation using DispatcherTimer
                var sw = Stopwatch.StartNew();
                var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                timer.Tick += (s, e) =>
                {
                    double t = Math.Min(1.0, sw.Elapsed.TotalMilliseconds / Math.Max(1.0, duration.TotalMilliseconds));
                    double value = startOffset + (targetOffset - startOffset) * t;
                    scroll.ScrollToVerticalOffset(value);
                    if (t >= 1.0)
                    {
                        timer.Stop();
                        // final alignment
                        scroll.ScrollToVerticalOffset(targetOffset);
                        latestTvi.IsSelected = true;
                    }
                };
                timer.Start();
            }
            catch
            {
                EnsureLatestVisible();
            }
        }

        static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SpeedRunsView), new PropertyMetadata(null));
    }
}
