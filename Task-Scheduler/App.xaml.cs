using Microsoft.Extensions.DependencyInjection;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            TaskNotificationScheduler.Start();
            return new Window(new AppShell());
        }
    }
}