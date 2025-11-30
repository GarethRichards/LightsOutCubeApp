using System;
using System.Reflection;
using System.Windows;

namespace LightsOutCube
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            try
            {
                var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
                              Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
                              "n/a";
                VersionText.Text = $"Version {version}";
            }
            catch
            {
                VersionText.Text = "Version n/a";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}