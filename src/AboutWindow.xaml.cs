using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;
using LightsOutCube.Model;
using LightsOutCube.ViewModels;

namespace LightsOutCube
{
    public partial class AboutWindow : Window
    {
        private readonly AboutViewModel _vm;

        public AboutWindow()
        {
            InitializeComponent();
            _vm = new AboutViewModel();
            DataContext = _vm;
        }

        // Other existing event handlers should remain unchanged (e.g. Window_Loaded, CloseButton_Click, RefreshSpeedRunsButton_Click)
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

        // RefreshScoresButton removed - high scores are bound to ViewModel

        // Speed runs refresh handled by ViewModel; no button handler required.
    }
}