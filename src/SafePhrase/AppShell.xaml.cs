namespace SafePhrase
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(OfflineSpeechTestPage), typeof(OfflineSpeechTestPage));
        }
    }
}
