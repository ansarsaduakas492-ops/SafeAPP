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
    private bool isPreparingMessage;
    private bool hasStarted;
    private int nextWhatsAppContactIndex;
    private string? pendingMessageText;

    public SosCountdownPage()
    {
        InitializeComponent();
        settings = SafetySettingsStore.Load();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.Changed += OnSafetyChanged;
        if (hasStarted)
            return;

        hasStarted = true;
        isPreparingMessage = false;
        CancelButton.IsEnabled = true;
        SendNowButton.IsEnabled = true;
        DoneButton.IsVisible = false;
        NextWhatsAppButton.IsVisible = false;
        DoneButton.Text = "Return to Home";
        countdownCancellation = new CancellationTokenSource();
        locationCancellation = new CancellationTokenSource();
        locationTask = TryGetLocationAsync(locationCancellation.Token);
        _ = RunCountdownAsync(countdownCancellation.Token);
    }

    private async Task RunCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (int secondsLeft = CountdownSeconds; secondsLeft > 0; secondsLeft--)
            {
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
        if (isPreparingMessage)
            return;

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
        if (isPreparingMessage)
            return;
        if (!CanOpenChannel())
            return;

        isPreparingMessage = true;
        countdownCancellation?.Cancel();
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
            isPreparingMessage = false;
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
        if (isPreparingMessage)
            return;
        if (!CanOpenChannel())
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

        isPreparingMessage = true;
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
            isPreparingMessage = false;
        }
    }

    private async Task OfferAlternateChannelAsync(string messageText, string[] recipients, string reason, bool offerSmsOption)
    {
        if (!CanOpenChannel())
            return;
        string choice = offerSmsOption
            ? await DisplayActionSheetAsync($"SOS channel unavailable: {reason}", "Cancel", null, "Open SMS for all contacts", "Copy SOS message")
            : await DisplayActionSheetAsync($"SOS channel unavailable: {reason}", "Cancel", null, "Copy SOS message");

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
        StatusLabel.Text = status;
        CountdownLabel.Text = "Test finished";
        CancelButton.IsVisible = false;
        SendNowButton.IsVisible = false;
        NextWhatsAppButton.IsVisible = false;
        DoneButton.IsVisible = true;
        DoneButton.Text = "Return to Home";
    }

    private bool CanOpenChannel()
    {
        if (SafetyModeState.IsAppVisible)
            return true;
        ShowDoneState("SOS opening cancelled because SafePhrase is not visible. No message was sent.");
        return false;
    }

    private void OnSafetyChanged()
    {
        if (!SafetyModeState.IsAppVisible && !isPreparingMessage && !DoneButton.IsVisible)
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
        await Shell.Current.GoToAsync("..");
    }

    protected override void OnDisappearing()
    {
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
