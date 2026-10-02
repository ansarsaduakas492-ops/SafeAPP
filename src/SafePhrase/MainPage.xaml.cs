namespace SafePhrase;

public partial class MainPage : ContentPage
{
    private bool isVisible;
    private bool isStarting;
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isVisible = true;
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.Changed += OnSafetyChanged;
        SafetySettings settings = SafetySettingsStore.Load();

        if (string.IsNullOrWhiteSpace(settings.SafePhrase))
            SetupStatusLabel.Text = "Add a safe phrase in Settings.";
        else if (settings.Contacts.Count == 0)
            SetupStatusLabel.Text = "Add a trusted contact in Settings.";
        else
            SetupStatusLabel.Text = $"Settings saved for {settings.Contacts.Count} trusted contact(s).";

        TestSosButton.IsEnabled = settings.Contacts.Count > 0;
        TestSosButton.Text = $"Test SOS ({settings.PreferredChannel})";
        OnSafetyChanged();
    }

    protected override void OnDisappearing()
    {
        isVisible = false;
        SafetyModeState.Changed -= OnSafetyChanged;
        base.OnDisappearing();
    }

    private void OnSafetyChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!isVisible)
                return;
            SafetySettings settings = SafetySettingsStore.Load();
            StartSafetyButton.IsEnabled = !isStarting && !SafetyModeState.IsActive && !SafetyModeState.HasPendingSos
                && settings.Contacts.Count > 0 && SafePhraseMatcher.Normalize(settings.SafePhrase).Length > 0;
            StopSafetyButton.IsEnabled = SafetyModeState.IsActive;
            SpeechTestButton.IsEnabled = !SafetyModeState.IsActive && !isStarting;
            TestSosButton.IsEnabled = settings.Contacts.Count > 0 && !SafetyModeState.IsActive && !isStarting;
            SafetyStatusLabel.Text = SafetyModeState.Status;
        });
    }

    private async void OnStartSafetyClicked(object? sender, EventArgs e)
    {
        if (isStarting || SafetyModeState.IsActive)
            return;
        isStarting = true;
        OnSafetyChanged();
        try
        {
#if ANDROID
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                throw new InvalidOperationException("Safety Mode requires Android 12 or later.");
            SafetySettings settings = SafetySettingsStore.Load();
            if (settings.Contacts.Count == 0 || SafePhraseMatcher.Normalize(settings.SafePhrase).Length == 0)
                throw new InvalidOperationException("Save a safe phrase and trusted contact first.");
            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
                throw new InvalidOperationException("Microphone permission is required. Listening has not started.");
            if (OperatingSystem.IsAndroidVersionAtLeast(33)
                && await Permissions.RequestAsync<NotificationPermission>() != PermissionStatus.Granted)
                throw new InvalidOperationException("Allow notifications so background phrase detection can be shown.");
            if (!isVisible || !SafetyModeState.IsAppVisible)
                return;
            Android.Content.Intent start = new(Android.App.Application.Context, typeof(BackgroundSpeechService));
            Android.App.Application.Context.StartForegroundService(start);
#else
            throw new InvalidOperationException("Safety Mode is available on Android only.");
#endif
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Safety Mode not started", exception.Message, "OK");
        }
        finally
        {
            isStarting = false;
            OnSafetyChanged();
        }
    }

    private void OnStopSafetyClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            SafetyModeState.RequestStop();
            Android.App.Application.Context.StopService(new Android.Content.Intent(
                Android.App.Application.Context, typeof(BackgroundSpeechService)));
        }
#endif
    }

#if ANDROID
    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private sealed class NotificationPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [(Android.Manifest.Permission.PostNotifications, true)];
    }
#endif

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnOfflineSpeechTestClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.IsActive || isStarting)
            return;
        await Shell.Current.GoToAsync(nameof(OfflineSpeechTestPage));
    }

    private async void OnTestSosClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.IsActive || isStarting)
            return;
        if (SafetySettingsStore.Load().Contacts.Count == 0)
        {
            await DisplayAlertAsync("Add a contact", "Add at least one trusted contact before testing SOS.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(SosCountdownPage));
    }
}
