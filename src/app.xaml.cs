using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;

namespace LightsOutCube
{
    /// <summary>
    /// Interaction logic for app.xaml
    /// </summary>

    public partial class App : Application
    {
        void AppStartingUp(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();            
        }

    }
}