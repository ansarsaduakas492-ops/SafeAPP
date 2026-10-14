using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace SafePhrase;

public partial class SosCountdownPage : ContentPage
{
    private const int CountdownSeconds = 5;
    private const int LocationTimeoutSeconds = 5;

    private readonly SafetySettings settings;
    private CancellationTokenSource? countdownCancellation;
    private CancellationTokenSource? locationCancellation;
    private Task<Location?>? locationTask;
    private readonly SosFlowGuard flow = new();
    private bool isVisible;
    private bool requestingLocationPermission;
    private bool hasStarted;
    private int nextWhatsAppContactIndex;
    private string? pendingMessageText;

    public SosCountdownPage()
    {
        InitializeComponent();
        settings = SafetySettingsStore.Load();
        SosChannelLabel.Text = $"{settings.PreferredChannel} \u00b7 {settings.Contacts.Count} trusted contact(s)";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isVisible = true;
        Shell.Current.Navigating -= OnShellNavigating;
        Shell.Current.Navigating += OnShellNavigating;
        SafetyModeState.SetSosFlowOpen(true);
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.Changed += OnSafetyChanged;
        if (hasStarted)
            return;

        hasStarted = true;
        CancelButton.IsEnabled = true;
        SendNowButton.IsEnabled = false;
        DoneButton.IsVisible = false;
        NextWhatsAppButton.IsVisible = false;
        DoneButton.Text = "Return to Home";
        countdownCancellation = new CancellationTokenSource();
        _ = StartCountdownAsync(countdownCancellation.Token);
    }

    private async Task StartCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            CountdownLabel.Text = "Location for your message";
            CountdownNumberLabel.Text = "\u2014";
            StatusLabel.Text = "You can allow location or continue without it.";
            requestingLocationPermission = true;
            PermissionStatus permission;
            try
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            finally
            {
                requestingLocationPermission = false;
            }
            if (cancellationToken.IsCancellationRequested || !CanOpenChannel())
                return;
            if (permission == PermissionStatus.Granted)
            {
                locationCancellation = new CancellationTokenSource();
                locationTask = TryGetLocationAsync(locationCancellation.Token);
                StatusLabel.Text = "Getting your location...";
            }
            else
            {
                StatusLabel.Text = "Location unavailable. Your message can still open.";
            }
            SendNowButton.IsEnabled = true;
            await RunCountdownAsync(cancellationToken);
        }
        catch (Exception)
        {
            if (flow.IsEnded)
                return;
            // Location permission failure must not prevent an SOS without coordinates.
            StatusLabel.Text = "Location unavailable. Your message can still open.";
            SendNowButton.IsEnabled = true;
            await RunCountdownAsync(cancellationToken);
        }
    }

    private async Task RunCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (int secondsLeft = CountdownSeconds; secondsLeft > 0; secondsLeft--)
            {
                CountdownNumberLabel.Text = secondsLeft.ToString();
                CountdownLabel.Text = $"SOS message opens in {secondsLeft} seconds";
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            await OpenSelectedChannelAsync();
        }
        catch (OperationCanceledException)
        {
            // The user cancelled or chose Send Now.
        }
        catch (Exception exception)
        {
            if (!CanOpenChannel())
                return;
            await DisplayAlertAsync("Could not prepare SOS", exception.Message, "OK");
            ShowDoneState("Could not prepare the SOS message. No message was sent.");
        }
    }

    private async Task<Location?> TryGetLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            GeolocationRequest request = new(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(LocationTimeoutSeconds));
            return await Geolocation.Default.GetLocationAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (flow.IsPreparing)
            return;
        flow.End();
        countdownCancellation?.Cancel();
        locationCancellation?.Cancel();
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSendNowClicked(object? sender, EventArgs e)
    {
        await OpenSelectedChannelAsync();
    }

    private async Task OpenSelectedChannelAsync()
    {
        if (!flow.TryPrepare(isVisible, SafetyModeState.IsAppVisible))
            return;
        countdownCancellation?.Cancel();
        CountdownDial.IsVisible = false;
        CancelButton.IsEnabled = false;
        SendNowButton.IsEnabled = false;
        CountdownLabel.Text = "Preparing the SOS message...";

        Location? location = null;
        if (locationTask is { IsCompleted: true })
        {
            try
            {
                location = await locationTask;
            }
            catch (OperationCanceledException)
            {
                location = null;
            }
        }

        locationCancellation?.Cancel();
        if (!CanOpenChannel())
            return;
        string messageText = SosMessageBuilder.Create(settings, location, DateTimeOffset.Now);
        string[] recipients = settings.Contacts
            .Select(contact => contact.Phone)
            .Where(phone => !string.IsNullOrWhiteSpace(phone))
            .ToArray();

        if (recipients.Length == 0)
        {
            await DisplayAlertAsync("Add a contact", "No trusted contacts are saved. No message was opened.", "OK");
            ShowDoneState("Add a trusted contact before testing SOS.");
            return;
        }

        if (string.Equals(settings.PreferredChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            pendingMessageText = messageText;
            nextWhatsAppContactIndex = 0;
            flow.CompletePreparation();
            await OpenNextWhatsAppContactAsync();
            return;
        }

        await OpenSmsComposerAsync(messageText, recipients);
    }

    private async Task OpenSmsComposerAsync(string messageText, string[] recipients)
    {
        if (!CanOpenChannel())
            return;
        if (!Sms.Default.IsComposeSupported)
        {
            await OfferAlternateChannelAsync(messageText, recipients, "SMS composition is not available on this device.", offerSmsOption: false);
            return;
        }

        try
        {
            SmsMessage message = new(messageText, recipients);
            await Sms.Default.ComposeAsync(message);
            ShowDoneState("SMS composer opened. Review the recipients and message, then press Send yourself. SafePhrase cannot tell whether it was sent.");
        }
        catch (Exception exception)
        {
            await OfferAlternateChannelAsync(messageText, recipients, $"Could not open the SMS composer: {exception.Message}", offerSmsOption: false);
        }
    }

    private async Task OpenNextWhatsAppContactAsync()
    {
        if (!flow.TryNext(isVisible, SafetyModeState.IsAppVisible))
            return;

        string[] recipients = settings.Contacts
            .Where(contact => !string.IsNullOrWhiteSpace(contact.Phone))
            .Select(contact => contact.Phone)
            .ToArray();
        string[] names = settings.Contacts
            .Where(contact => !string.IsNullOrWhiteSpace(contact.Phone))
            .Select(contact => string.IsNullOrWhiteSpace(contact.Name) ? contact.Phone : contact.Name)
            .ToArray();

        if (pendingMessageText is null || nextWhatsAppContactIndex >= recipients.Length)
        {
            ShowDoneState("All saved WhatsApp chats have been opened. Review each message and press Send in WhatsApp; SafePhrase cannot confirm delivery.");
            return;
        }

        NextWhatsAppButton.IsEnabled = false;
        string phone = recipients[nextWhatsAppContactIndex];
        string name = names[nextWhatsAppContactIndex];
        CountdownLabel.Text = "Opening WhatsApp...";
        StatusLabel.Text = $"Opening contact {nextWhatsAppContactIndex + 1} of {recipients.Length}: {name}";

        try
        {
            string link = SosMessageBuilder.CreateWhatsAppLink(phone, pendingMessageText);
            bool opened = await Launcher.Default.TryOpenAsync(link);
            if (!opened)
            {
                await OfferAlternateChannelAsync(pendingMessageText, recipients, "Could not open the WhatsApp chat link.", offerSmsOption: true);
                return;
            }

            nextWhatsAppContactIndex++;
            CountdownLabel.Text = "WhatsApp chat link opened";
            StatusLabel.Text = nextWhatsAppContactIndex < recipients.Length
                ? $"Review the prefilled message for {name} and press Send in WhatsApp. Return here and open the next contact. SafePhrase cannot confirm delivery."
                : $"Review the prefilled message for {name} and press Send in WhatsApp. SafePhrase cannot confirm delivery.";
            CancelButton.IsVisible = false;
            SendNowButton.IsVisible = false;
            NextWhatsAppButton.IsVisible = nextWhatsAppContactIndex < recipients.Length;
            NextWhatsAppButton.IsEnabled = true;
            DoneButton.IsVisible = true;
            DoneButton.Text = "Return to Home";
        }
        catch (Exception exception)
        {
            await OfferAlternateChannelAsync(pendingMessageText, recipients, $"Could not open WhatsApp: {exception.Message}", offerSmsOption: true);
        }
        finally
        {
            flow.CompletePreparation();
        }
    }

    private async Task OfferAlternateChannelAsync(string messageText, string[] recipients, string reason, bool offerSmsOption)
    {
        if (!CanOpenChannel())
            return;
        string choice = offerSmsOption
            ? await DisplayActionSheetAsync($"SOS channel unavailable: {reason}", "Cancel", null, "Open SMS for all contacts", "Copy SOS message")
            : await DisplayActionSheetAsync($"SOS channel unavailable: {reason}", "Cancel", null, "Copy SOS message");

        if (!CanOpenChannel())
            return;

        if (choice == "Open SMS for all contacts")
        {
            await OpenSmsComposerAsync(messageText, recipients);
        }
        else if (choice == "Copy SOS message")
        {
            await Clipboard.Default.SetTextAsync(messageText);
            ShowDoneState("SOS message copied. No message was sent.");
        }
        else
        {
            ShowDoneState($"{reason} No message was sent.");
        }
    }

    private void ShowDoneState(string status)
    {
        flow.End();
        if (Shell.Current.CurrentPage != this)
            return;
        StatusLabel.Text = status;
        CountdownLabel.Text = "SOS preparation finished";
        CountdownDial.IsVisible = false;
        CancelButton.IsVisible = false;
        SendNowButton.IsVisible = false;
        NextWhatsAppButton.IsVisible = false;
        DoneButton.IsVisible = true;
        DoneButton.Text = "Return to Home";
    }

    private bool CanOpenChannel()
    {
        if (flow.CanAct(isVisible, SafetyModeState.IsAppVisible))
            return true;
        if (flow.IsEnded || !isVisible)
            return false;
        ShowDoneState("SOS opening cancelled because SafePhrase is not visible. No message was sent.");
        return false;
    }

    private void OnSafetyChanged()
    {
        if (!SafetyModeState.IsAppVisible && !requestingLocationPermission && !flow.HasPrepared && !flow.IsEnded)
        {
            countdownCancellation?.Cancel();
            locationCancellation?.Cancel();
            ShowDoneState("Countdown cancelled when the app left the foreground. Start SOS again when ready.");
        }
    }

    private async void OnNextWhatsAppClicked(object? sender, EventArgs e)
    {
        await OpenNextWhatsAppContactAsync();
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        flow.End();
        await Shell.Current.GoToAsync("..");
    }

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (Shell.Current.CurrentPage != this)
            return;
        flow.End();
        countdownCancellation?.Cancel();
        locationCancellation?.Cancel();
        SafetyModeState.SetSosFlowOpen(false);
        if (!SafetyModeState.HasPendingSos && !SafetyModeState.IsActive && !SafetyModeState.IsStarting)
            SafetyModeState.Update(false, "Safety Mode is stopped. Start again to listen.");
    }

    protected override void OnDisappearing()
    {
        isVisible = false;
        if (flow.IsEnded)
        {
            Shell.Current.Navigating -= OnShellNavigating;
            SafetyModeState.SetSosFlowOpen(false);
        }
        SafetyModeState.Changed -= OnSafetyChanged;
        countdownCancellation?.Cancel();
        locationCancellation?.Cancel();
        countdownCancellation?.Dispose();
        locationCancellation?.Dispose();
        countdownCancellation = null;
        locationCancellation = null;
        locationTask = null;
        base.OnDisappearing();
    }
}
