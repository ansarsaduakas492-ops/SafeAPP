namespace SafePhrase;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafetySettings settings = SafetySettingsStore.Load();

        if (string.IsNullOrWhiteSpace(settings.SafePhrase))
            SetupStatusLabel.Text = "Add a safe phrase in Settings.";
        else if (settings.Contacts.Count == 0)
            SetupStatusLabel.Text = "Add a trusted contact in Settings.";
        else
            SetupStatusLabel.Text = $"Settings saved for {settings.Contacts.Count} trusted contact(s).";
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnOfflineSpeechTestClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OfflineSpeechTestPage));
    }
}
