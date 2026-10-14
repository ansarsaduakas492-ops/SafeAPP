namespace Microsoft.Maui.Storage;

// Only this standalone executable uses the stub; the app uses MAUI Preferences.
internal static class Preferences
{
    public static InMemoryPreferences Default { get; } = new();
}

internal sealed class InMemoryPreferences
{
    private readonly Dictionary<string, bool> values = new();
    private readonly Dictionary<string, string> strings = new();

    public bool Get(string key, bool defaultValue) => values.GetValueOrDefault(key, defaultValue);
    public void Set(string key, bool value) => values[key] = value;
    public string Get(string key, string defaultValue) => strings.GetValueOrDefault(key, defaultValue);
    public void Set(string key, string value) => strings[key] = value;
    public void Remove(string key)
    {
        values.Remove(key);
        strings.Remove(key);
    }
    public IEnumerable<string> StoredKeys => values.Keys.Concat(strings.Keys);
}
