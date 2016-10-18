using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ePSXeQuitter
{
    public partial class App : Application
    {
        private MainWindow mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            mainWindow = new MainWindow();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (mainWindow.notifyIconWrapper != null)
            {
                mainWindow.notifyIconWrapper.Dispose();
            }
        }
    }
}
