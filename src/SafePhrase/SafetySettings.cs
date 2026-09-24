using System.Text.Json;
using Microsoft.Maui.Storage;

namespace SafePhrase;

public sealed class TrustedContact
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
}

public sealed class SafetySettings
{
    public const string DefaultMessage = "SOS. I may be in danger. Please check on me.";

    public string SafePhrase { get; set; } = "";
    public string SosMessage { get; set; } = DefaultMessage;
    public string PreferredChannel { get; set; } = "WhatsApp";
    public List<TrustedContact> Contacts { get; set; } = [];
}

public static class SafetySettingsStore
{
    private const string SettingsKey = "safety_settings";

    public static SafetySettings Load()
    {
        string json = Preferences.Default.Get(SettingsKey, string.Empty);
        return string.IsNullOrEmpty(json)
            ? new SafetySettings()
            : JsonSerializer.Deserialize<SafetySettings>(json) ?? new SafetySettings();
    }

    public static void Save(SafetySettings settings)
    {
        Preferences.Default.Set(SettingsKey, JsonSerializer.Serialize(settings));
    }
}
