using System;
using System.Windows;

namespace ClipMaster
{
    public partial class App : Application
    {
        private System.Threading.Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure single instance
            const string mutexName = "ClipMaster_SingleInstance_Mutex";
            _mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                MessageBox.Show("ClipMaster is already running!", "ClipMaster", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
