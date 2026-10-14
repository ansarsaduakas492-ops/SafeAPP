namespace SafePhrase
{
    public partial class AppShell : Shell
    {
        private bool isHandlingVoiceSos;
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
                if (isHandlingVoiceSos)
                    return;
                isHandlingVoiceSos = true;
                try
                {
                    bool opened = await SafetyModeState.TryOpenPendingSosAsync(
                        async () =>
                        {
                            try
                            {
                                await GoToAsync(nameof(SosCountdownPage));
                            }
                            catch
                            {
                                SafetyModeState.SetSosFlowOpen(false);
                                throw;
                            }
                        });
                    if (!opened)
                        return;
#if ANDROID
                    if (OperatingSystem.IsAndroidVersionAtLeast(31))
                        VoiceSosAlert.Dismiss(Android.App.Application.Context);
#endif
                }
                catch (Exception exception)
                {
                    string nextStep = SafetyModeState.HasPendingSos
                        ? "The detection is saved. Return to Home and tap Open SOS, or reopen SafePhrase."
                        : "The countdown opened, but its notification could not be dismissed.";
                    await DisplayAlertAsync("Voice SOS", $"{nextStep}\n{exception.Message}", "OK");
                }
                finally
                {
                    isHandlingVoiceSos = false;
                }
            });
        }
    }
}
