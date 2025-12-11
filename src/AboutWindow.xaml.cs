using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        // Placeholder for the formatting helper used elsewhere in the class
        private static string FormatDuration(TimeSpan ts)
        {
            return $"{ts.TotalSeconds:0.###}s";
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

        // RefreshScoresButton removed - high scores are bound to ViewModel

        // Speed runs refresh handled by ViewModel; no button handler required.
    }
}