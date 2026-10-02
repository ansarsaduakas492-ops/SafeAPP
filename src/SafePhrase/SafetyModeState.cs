namespace SafePhrase;

// Service and UI updates are serialized on the Android main thread.
public static class SafetyModeState
{
    private const string PendingSosKey = "pending_voice_sos";
    public static bool IsActive { get; private set; }
    public static bool IsAppVisible { get; set; }
    public static bool StopRequested { get; private set; }
    public static bool HasPendingSos => Preferences.Default.Get(PendingSosKey, false);
    public static string Status { get; private set; } = "Safety Mode is stopped.";
    public static event Action? Changed;

    public static void Update(bool active, string status)
    {
        IsActive = active;
        Status = status;
        Changed?.Invoke();
    }

    public static void PhraseDetected()
    {
        if (StopRequested)
            return;
        Preferences.Default.Set(PendingSosKey, true);
        Update(false, "Safe phrase detected. Open SafePhrase to start the SOS countdown.");
    }

    public static bool TakePendingSos()
    {
        if (!HasPendingSos)
            return false;
        ClearPendingSos();
        return true;
    }

    public static void ClearPendingSos() => Preferences.Default.Remove(PendingSosKey);

    public static void BeginListening()
    {
        StopRequested = false;
        ClearPendingSos();
    }

    public static void RequestStop()
    {
        StopRequested = true;
        ClearPendingSos();
    }
}
