using LightsOutCube.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace LightsOutCube
{
    public partial class AboutWindow : Window
    {
        private readonly AboutViewModel _vm;
        private GridViewColumnHeader _lastHeader;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public AboutWindow()
        {
            InitializeComponent();
            _vm = new AboutViewModel();
            DataContext = _vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Refresh VM data; UI bindings will reflect the collections
                _vm.Refresh();
                _vm.RefreshHighScores();
            }
            catch { /* Ignore error */ }

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch { /* best-effort */ }
            e.Handled = true;
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is GridViewColumnHeader header) || header.Column == null)
                return;

            // determine sort property name: prefer Tag, fallback to binding path
            string sortBy = header.Tag as string;
            if (string.IsNullOrEmpty(sortBy) && header.Column.DisplayMemberBinding is Binding b && b.Path != null)
                sortBy = b.Path.Path;

            if (string.IsNullOrEmpty(sortBy)) return;

            // find the ListView that owns this header
            var listView = FindAncestor<ListView>(header);
            if (listView == null) return;

            var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view == null) return;

            // toggle direction if same column clicked twice
            var direction = ListSortDirection.Ascending;
            if (_lastHeader == header)
            {
                direction = _lastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(sortBy, direction));
            view.Refresh();

            _lastHeader = header;
            _lastDirection = direction;
        }

        // small visual-tree helper
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}