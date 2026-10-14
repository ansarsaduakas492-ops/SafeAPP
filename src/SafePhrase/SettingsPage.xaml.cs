namespace SafePhrase;

public partial class SettingsPage : ContentPage
{
    private SafetySettings settings = new();
    private TrustedContact? editingContact;
    private bool isVisible;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        isVisible = true;
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.Changed += OnSafetyChanged;
        settings = SafetySettingsStore.Load();
        PhraseEntry.Text = settings.SafePhrase;
        MessageEditor.Text = settings.SosMessage;
        ChannelPicker.SelectedIndex = settings.PreferredChannel == "SMS" ? 1 : 0;
        ResetContactForm();
        RenderContacts();
        UpdateVoiceAlertAccess();
    }

    protected override void OnDisappearing()
    {
        isVisible = false;
        SafetyModeState.Changed -= OnSafetyChanged;
        base.OnDisappearing();
    }

    private void OnSafetyChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (isVisible)
            UpdateVoiceAlertAccess();
    });

    private void UpdateVoiceAlertAccess()
    {
#if ANDROID
        VoiceAlertSection.IsVisible = OperatingSystem.IsAndroidVersionAtLeast(31);
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return;
        bool allowed = VoiceSosAlert.CanOpenFullScreen(Android.App.Application.Context);
        VoiceAlertStatusLabel.Text = allowed
            ? "Full-screen access allowed. Android can wake the locked screen for Cancel / Continue SOS; device settings still apply."
            : "Full-screen access is off. You will receive a notification to tap instead.";
        VoiceAlertPermissionButton.IsVisible = OperatingSystem.IsAndroidVersionAtLeast(34);
        VoiceAlertPermissionButton.Text = allowed ? "Manage full-screen alerts" : "Allow full-screen alerts";
#else
        VoiceAlertSection.IsVisible = false;
#endif
    }

    private async void OnVoiceAlertPermissionClicked(object? sender, EventArgs e)
    {
        if (!await CanChangeSettingsAsync())
            return;
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            return;
        try
        {
            VoiceSosAlert.OpenFullScreenSettings(Android.App.Application.Context);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Full-screen alerts", "Open Android Settings, Special app access, Full-screen notifications, then allow SafePhrase. If this option is unavailable, SOS still uses a notification.", "OK");
        }
#endif
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        if (!await CanChangeSettingsAsync())
            return;
        string phrase = PhraseEntry.Text?.Trim() ?? "";
        string message = MessageEditor.Text?.Trim() ?? "";

        if (SafePhraseMatcher.Normalize(phrase).Length == 0 || string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlertAsync("Check settings", "Enter a safe phrase and SOS message.", "OK");
            return;
        }

        settings.SafePhrase = phrase;
        settings.SosMessage = message;
        settings.PreferredChannel = ChannelPicker.SelectedIndex == 1 ? "SMS" : "WhatsApp";
        SafetySettingsStore.Save(settings);
        await DisplayAlertAsync("Saved", "Your settings are stored on this phone.", "OK");
    }

    private async void OnSaveContactClicked(object? sender, EventArgs e)
    {
        if (!await CanChangeSettingsAsync())
            return;
        string phone = PhoneEntry.Text?.Trim() ?? "";
        string digits = new(phone.Where(char.IsAsciiDigit).ToArray());

        if (!phone.StartsWith('+') || digits.Length is < 8 or > 15 || digits[0] == '0' ||
            phone.Skip(1).Any(character => !char.IsAsciiDigit(character) && character is not (' ' or '-' or '(' or ')')))
        {
            await DisplayAlertAsync("Check phone number", "Use an international number such as +996...", "OK");
            return;
        }

        string normalizedPhone = "+" + digits;
        if (settings.Contacts.Any(contact => contact.Phone == normalizedPhone && contact != editingContact))
        {
            await DisplayAlertAsync("Duplicate contact", "This phone number is already saved.", "OK");
            return;
        }

        if (editingContact is null)
        {
            settings.Contacts.Add(new TrustedContact
            {
                Name = NameEntry.Text?.Trim() ?? "",
                Phone = normalizedPhone
            });
        }
        else
        {
            editingContact.Name = NameEntry.Text?.Trim() ?? "";
            editingContact.Phone = normalizedPhone;
        }

        SafetySettingsStore.Save(settings);
        ResetContactForm();
        RenderContacts();
    }

    private void OnCancelEditClicked(object? sender, EventArgs e)
    {
        ResetContactForm();
    }

    private async Task<bool> CanChangeSettingsAsync()
    {
        if (!SafetyModeState.IsBusy)
            return true;
        await DisplayAlertAsync("Settings locked", "Stop Safety Mode or finish SOS before changing settings.", "OK");
        return false;
    }

    private void ResetContactForm()
    {
        editingContact = null;
        NameEntry.Text = "";
        PhoneEntry.Text = "";
        ContactSaveButton.Text = "Add contact";
        ContactFormTitleLabel.Text = "Add contact";
        CancelEditButton.IsVisible = false;
    }

    private void RenderContacts()
    {
        ContactsLayout.Children.Clear();
        EmptyContactsLabel.IsVisible = settings.Contacts.Count == 0;

        foreach (TrustedContact contact in settings.Contacts)
        {
            var row = new VerticalStackLayout { Spacing = 8 };
            var identity = new Grid { ColumnSpacing = 12 };
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            string initial = string.IsNullOrWhiteSpace(contact.Name) ? "+"
                : System.Globalization.StringInfo.GetNextTextElement(contact.Name.Trim()).ToUpperInvariant();
            var avatar = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                WidthRequest = 44, HeightRequest = 44,
                Content = new Label { Text = initial, FontFamily = "OpenSansSemibold",
                    HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };
            avatar.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#E5F3ED"), Color.FromArgb("#233F37"));
            var details = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            details.Children.Add(new Label { Text = string.IsNullOrWhiteSpace(contact.Name) ? "Trusted contact" : contact.Name,
                FontFamily = "OpenSansSemibold", FontSize = 16 });
            details.Children.Add(new Label { Text = contact.Phone, Style = (Style)Application.Current!.Resources["MutedText"] });
            identity.Add(avatar);
            identity.Add(details, 1);
            row.Children.Add(identity);

            var buttons = new Grid { ColumnSpacing = 10 };
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var editButton = new Button { Text = "Edit", Style = (Style)Application.Current!.Resources["SecondaryAction"],
                MinimumHeightRequest = 44, FontSize = 13, Padding = new Thickness(10, 6) };
            editButton.Clicked += async (_, _) =>
            {
                if (!await CanChangeSettingsAsync())
                    return;
                editingContact = contact;
                NameEntry.Text = contact.Name;
                PhoneEntry.Text = contact.Phone;
                ContactSaveButton.Text = "Save contact";
                ContactFormTitleLabel.Text = "Edit contact";
                CancelEditButton.IsVisible = true;
                await SettingsScrollView.ScrollToAsync(ContactFormSection, ScrollToPosition.Start, true);
            };
            var deleteButton = new Button { Text = "Delete", Style = (Style)Application.Current!.Resources["AlertAction"],
                MinimumHeightRequest = 44, FontSize = 13, Padding = new Thickness(10, 6) };
            deleteButton.Clicked += async (_, _) =>
            {
                if (!await CanChangeSettingsAsync())
                    return;
                bool confirmed = await DisplayAlertAsync("Delete contact?", contact.Phone, "Delete", "Cancel");
                if (!confirmed || !await CanChangeSettingsAsync())
                    return;

                settings.Contacts.Remove(contact);
                SafetySettingsStore.Save(settings);
                if (editingContact == contact)
                    ResetContactForm();
                RenderContacts();
            };

            buttons.Add(editButton);
            buttons.Add(deleteButton, 1);
            row.Children.Add(buttons);
            ContactsLayout.Children.Add(new Border { Style = (Style)Application.Current!.Resources["SafetyCard"],
                Padding = new Thickness(12), Content = row });
        }
    }
}
