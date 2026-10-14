namespace SafePhrase;

public partial class MainPage : ContentPage
{
    private bool isVisible;
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

        if (SafePhraseMatcher.Normalize(settings.SafePhrase).Length == 0)
            SetupStatusLabel.Text = "Add a safe phrase in Settings.";
        else if (settings.Contacts.Count == 0)
            SetupStatusLabel.Text = "Add a trusted contact in Settings.";
        else
            SetupStatusLabel.Text = $"Settings saved for {settings.Contacts.Count} trusted contact(s).";

        TestSosButton.IsEnabled = settings.Contacts.Count > 0;
        SetupStatusLabel.IsVisible = SafePhraseMatcher.Normalize(settings.SafePhrase).Length == 0
            || settings.Contacts.Count == 0;
        TestSosButton.Text = "Prepare SOS";
        SavedPhraseLabel.Text = string.IsNullOrWhiteSpace(settings.SafePhrase)
            ? "Add your safe phrase" : $"\u201c{settings.SafePhrase}\u201d";
        ChannelSummaryLabel.Text = $"Manual SOS: {settings.PreferredChannel} \u00b7 Background confirmation: SMS";
        ContactSummaryLabel.Text = settings.Contacts.Count == 1
            ? "1 saved contact" : $"{settings.Contacts.Count} saved contacts";
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
            StartSafetyButton.IsEnabled = !SafetyModeState.IsBusy
                && settings.Contacts.Count > 0 && SafePhraseMatcher.Normalize(settings.SafePhrase).Length > 0;
            bool running = SafetyModeState.IsActive || SafetyModeState.IsStarting;
            StopSafetyButton.IsEnabled = running;
            StartSafetyButton.IsVisible = !running && !SafetyModeState.HasPendingSos;
            StopSafetyButton.IsVisible = running;
            StartActionVisual.IsVisible = StartSafetyButton.IsVisible;
            StartActionVisual.Opacity = StartSafetyButton.IsEnabled ? 1 : 0.45;
            StopActionVisual.IsVisible = running;
            RetryActionVisual.IsVisible = SafetyModeState.HasPendingSos;
            ListeningIndicator.IsRunning = running && !SafetyModeState.IsListening;
            ListeningIndicator.IsVisible = ListeningIndicator.IsRunning;
            ModeTitleLabel.Text = SafetyModeState.HasPendingSos ? "Voice SOS"
                : SafetyModeState.IsStarting ? "Starting..."
                : SafetyModeState.IsListening ? "Listening"
                : SafetyModeState.IsActive ? "Reconnecting..." : "Safety Mode";
            SpeechTestButton.IsEnabled = !SafetyModeState.IsBusy;
            TestSosButton.IsEnabled = settings.Contacts.Count > 0 && !SafetyModeState.IsBusy;
            SafetyStatusLabel.Text = SafetyModeState.Status;
            RecognizedTextLabel.Text = string.IsNullOrWhiteSpace(SafetyModeState.LastRecognizedText)
                ? "Last recognized phrase: none yet."
                : $"Last recognized phrase: {SafetyModeState.LastRecognizedText}";
            RetryVoiceSosButton.IsVisible = SafetyModeState.HasPendingSos;
        });
    }

    private async void OnStartSafetyClicked(object? sender, EventArgs e)
    {
        if (!SafetyModeState.TryRequestStart(out int requestId))
            return;
        bool dispatched = false;
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
            if (!SafetyModeState.IsCurrentStart(requestId))
                return;
            if (OperatingSystem.IsAndroidVersionAtLeast(33)
                && await Permissions.RequestAsync<NotificationPermission>() != PermissionStatus.Granted)
                throw new InvalidOperationException("Allow notifications so background phrase detection can be shown.");
            if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
                return;
            if (!await OfferLockScreenAccessAsync(requestId))
                return;
            if (!await OfferSmsSendingAsync(requestId))
                return;
            if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
                return;
            Android.Content.Intent start = new(Android.App.Application.Context, typeof(BackgroundSpeechService));
            start.PutExtra(BackgroundSpeechService.StartRequestId, requestId);
            Android.App.Application.Context.StartForegroundService(start);
            dispatched = true;
#else
            throw new InvalidOperationException("Safety Mode is available on Android only.");
#endif
        }
        catch (Exception exception)
        {
            if (SafetyModeState.IsCurrentStart(requestId))
            {
                SafetyModeState.FinishService(requestId, $"Could not start: {exception.Message}");
                await DisplayAlertAsync("Safety Mode not started", exception.Message, "OK");
            }
        }
        finally
        {
            if (!dispatched && SafetyModeState.IsCurrentStart(requestId))
                SafetyModeState.RequestStop();
            OnSafetyChanged();
        }
    }

#if ANDROID
    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private async Task<bool> OfferSmsSendingAsync(int requestId)
    {
        if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
            return false;
        if (DirectSmsSender.HasPermission(Android.App.Application.Context))
            return true;
        bool allow = await DisplayAlertAsync("Send SOS after your confirmation",
            "Allow SMS sending so the background alert can send your SOS to all saved contacts when you tap Send SOS SMS. SMS charges may apply. Nothing sends until you confirm an alert.",
            "Allow", "Use message app");
        if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
            return false;
        if (!allow)
            return true;
        if (await Permissions.RequestAsync<SendSmsPermission>() == PermissionStatus.Granted)
            return true;
        if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
            return false;
        return await DisplayAlertAsync("SMS permission is off",
            "The alert can open your message app instead. You will press Send there. Continue?", "Continue", "Cancel");
    }

    private sealed class SendSmsPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [(Android.Manifest.Permission.SendSms, true)];
    }
#endif

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
    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private async Task<bool> OfferLockScreenAccessAsync(int requestId)
    {
        Android.Content.Context context = Android.App.Application.Context;
        if (!OperatingSystem.IsAndroidVersionAtLeast(34) || VoiceSosAlert.CanOpenFullScreen(context))
            return true;

        bool openPermission = await DisplayAlertAsync("Wake the screen for SOS?",
            "Allow full-screen alerts so SafePhrase can show Cancel / Continue SOS when your phone is locked. Tap Allow, then turn on the permission on the next screen.",
            "Allow", "Use notification");
        if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
            return false;
        if (!openPermission)
            return true;

        var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool leftApp = false;
        void OnPermissionReturn()
        {
            if (!SafetyModeState.IsCurrentStart(requestId))
                returned.TrySetCanceled();
            else if (!SafetyModeState.IsAppVisible)
                leftApp = true;
            else if (leftApp)
                returned.TrySetResult();
        }

        SafetyModeState.Changed += OnPermissionReturn;
        try
        {
            SafetyModeState.Update(false, "Waiting for SOS screen permission. Listening has not started.");
            VoiceSosAlert.OpenFullScreenSettings(context);
            await returned.Task;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception)
        {
            if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
                return false;
            return await DisplayAlertAsync("Permission page unavailable",
                "SafePhrase can still show a notification when your phrase is detected. Continue with notifications?",
                "Continue", "Cancel");
        }
        finally
        {
            SafetyModeState.Changed -= OnPermissionReturn;
        }

        if (!SafetyModeState.IsCurrentStart(requestId) || !isVisible || !SafetyModeState.IsAppVisible)
            return false;
        if (VoiceSosAlert.CanOpenFullScreen(context))
            return true;
        return await DisplayAlertAsync("Screen permission is still off",
            "The screen may stay locked. You can tap the SOS notification instead. Continue with notifications?",
            "Continue", "Cancel");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private sealed class NotificationPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [(Android.Manifest.Permission.PostNotifications, true)];
    }
#endif

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.IsBusy)
        {
            await DisplayAlertAsync("Settings locked", "Stop Safety Mode or finish SOS before changing settings.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnOfflineSpeechTestClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.IsBusy)
            return;
        await Shell.Current.GoToAsync(nameof(OfflineSpeechTestPage));
    }

    private async void OnTestSosClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.IsBusy)
            return;
        if (SafetySettingsStore.Load().Contacts.Count == 0)
        {
            await DisplayAlertAsync("Add a contact", "Add at least one trusted contact before testing SOS.", "OK");
            return;
        }

        SafetyModeState.SetSosFlowOpen(true);
        try
        {
            await Shell.Current.GoToAsync(nameof(SosCountdownPage));
        }
        catch (Exception exception)
        {
            SafetyModeState.SetSosFlowOpen(false);
            await DisplayAlertAsync("Could not open SOS", exception.Message, "OK");
        }
    }

    private void OnRetryVoiceSosClicked(object? sender, EventArgs e)
    {
        if (SafetyModeState.HasPendingSos)
            SafetyModeState.Update(SafetyModeState.IsActive, SafetyModeState.Status);
    }

    private void OnToggleListeningDetailsClicked(object? sender, EventArgs e)
    {
        ListeningDetailsPanel.IsVisible = !ListeningDetailsPanel.IsVisible;
        if (sender is Button button)
            button.Text = ListeningDetailsPanel.IsVisible ? "Hide details" : "Details";
    }
}
