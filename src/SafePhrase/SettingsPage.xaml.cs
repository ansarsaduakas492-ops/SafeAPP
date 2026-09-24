namespace SafePhrase;

public partial class SettingsPage : ContentPage
{
    private SafetySettings settings = new();
    private TrustedContact? editingContact;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        settings = SafetySettingsStore.Load();
        PhraseEntry.Text = settings.SafePhrase;
        MessageEditor.Text = settings.SosMessage;
        ChannelPicker.SelectedIndex = settings.PreferredChannel == "SMS" ? 1 : 0;
        ResetContactForm();
        RenderContacts();
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        string phrase = PhraseEntry.Text?.Trim() ?? "";
        string message = MessageEditor.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(phrase) || string.IsNullOrWhiteSpace(message))
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
        string phone = PhoneEntry.Text?.Trim() ?? "";
        string digits = new(phone.Where(char.IsDigit).ToArray());

        if (!phone.StartsWith('+') || digits.Length is < 8 or > 15 ||
            phone.Skip(1).Any(character => !char.IsDigit(character) && character is not (' ' or '-' or '(' or ')')))
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

    private void ResetContactForm()
    {
        editingContact = null;
        NameEntry.Text = "";
        PhoneEntry.Text = "";
        ContactSaveButton.Text = "Add contact";
        CancelEditButton.IsVisible = false;
    }

    private void RenderContacts()
    {
        ContactsLayout.Children.Clear();

        foreach (TrustedContact contact in settings.Contacts)
        {
            var row = new VerticalStackLayout { Spacing = 4 };
            row.Children.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(contact.Name)
                    ? contact.Phone
                    : $"{contact.Name}: {contact.Phone}"
            });

            var buttons = new HorizontalStackLayout { Spacing = 12 };
            var editButton = new Button { Text = "Edit" };
            editButton.Clicked += (_, _) =>
            {
                editingContact = contact;
                NameEntry.Text = contact.Name;
                PhoneEntry.Text = contact.Phone;
                ContactSaveButton.Text = "Save contact";
                CancelEditButton.IsVisible = true;
            };
            var deleteButton = new Button { Text = "Delete" };
            deleteButton.Clicked += async (_, _) =>
            {
                bool confirmed = await DisplayAlertAsync("Delete contact?", contact.Phone, "Delete", "Cancel");
                if (!confirmed)
                    return;

                settings.Contacts.Remove(contact);
                SafetySettingsStore.Save(settings);
                if (editingContact == contact)
                    ResetContactForm();
                RenderContacts();
            };

            buttons.Children.Add(editButton);
            buttons.Children.Add(deleteButton);
            row.Children.Add(buttons);
            ContactsLayout.Children.Add(row);
        }
    }
}
