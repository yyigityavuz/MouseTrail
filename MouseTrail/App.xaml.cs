using System.Threading;
using System.Windows;

namespace MouseTrail
{
    public partial class App : System.Windows.Application
    {
        private Mutex? singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            singleInstanceMutex = new Mutex(true, "MouseTrail.SingleInstance", out bool isFirstInstance);
            if (!isFirstInstance)
            {
                Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
