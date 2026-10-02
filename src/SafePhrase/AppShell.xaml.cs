namespace SafePhrase
{
    public partial class AppShell : Shell
    {
        private bool isOpeningVoiceSos;
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(OfflineSpeechTestPage), typeof(OfflineSpeechTestPage));
            Routing.RegisterRoute(nameof(SosCountdownPage), typeof(SosCountdownPage));
            SafetyModeState.Changed += OnSafetyChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            OnSafetyChanged();
        }

        private void OnSafetyChanged()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!SafetyModeState.IsAppVisible || isOpeningVoiceSos || !SafetyModeState.HasPendingSos)
                    return;
                isOpeningVoiceSos = true;
                SafetyModeState.TakePendingSos();
                try
                {
#if ANDROID
                    ((Android.App.NotificationManager)Android.App.Application.Context
                        .GetSystemService(Android.Content.Context.NotificationService)!).Cancel(7102);
#endif
                    await GoToAsync(nameof(SosCountdownPage));
                }
                catch (Exception exception)
                {
                    await DisplayAlertAsync("Could not open voice SOS", exception.Message, "OK");
                }
                finally
                {
                    isOpeningVoiceSos = false;
                }
            });
        }
    }
}
