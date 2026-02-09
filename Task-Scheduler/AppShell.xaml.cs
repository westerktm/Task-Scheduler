namespace Task_Scheduler
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        }
    }
}
