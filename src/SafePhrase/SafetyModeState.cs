using Microsoft.Maui.Storage;

namespace SafePhrase;

// Service and UI updates are serialized on the Android main thread.
public static class SafetyModeState
{
    private const string PendingSosKey = "pending_voice_sos";
    private const string PendingSosIdKey = "pending_voice_sos_id";
    private static bool isOpeningPendingSos;
    private static int startGeneration;
    public static bool IsActive { get; private set; }
    public static bool IsStarting { get; private set; }
    public static bool IsListening { get; private set; }
    public static bool IsSosFlowOpen { get; private set; }
    public static bool IsVoicePromptOpen { get; private set; }
    public static bool IsSendingSms { get; private set; }
    public static bool IsBusy => IsActive || IsStarting || HasPendingSos || IsSosFlowOpen || IsVoicePromptOpen || IsSendingSms || isOpeningPendingSos;
    public static bool IsAppVisible { get; set; }
    public static bool StopRequested { get; private set; }
    public static bool HasPendingSos => Preferences.Default.Get(PendingSosKey, false);
    public static string PendingSosId => Preferences.Default.Get(PendingSosIdKey, "");
    public static bool IsCurrentDetection(string id) =>
        !string.IsNullOrEmpty(id) && HasPendingSos && id == PendingSosId;

    public static bool TryClaimSmsConfirmation(string id, Func<bool> persistClaim)
    {
        if (!IsCurrentDetection(id) || isOpeningPendingSos || IsSosFlowOpen || IsSendingSms)
            return false;
        // The Android caller synchronously persists consent consumption before dispatch.
        if (!persistClaim())
            return false;
        IsSendingSms = true;
        // Consume consent before dispatch. An interrupted attempt must never resend automatically.
        ClearPendingSos();
        Update(false, "Sending the confirmed voice SOS by SMS...");
        return true;
    }
    public static string Status { get; private set; } = "Safety Mode is stopped.";
    public static string LastRecognizedText { get; private set; } = "";
    public static event Action? Changed;

    public static void Update(bool active, string status)
    {
        IsActive = active;
        if (!active)
            IsListening = false;
        Status = status;
        Changed?.Invoke();
    }

    public static void UpdateRecognizedText(string? text)
    {
        LastRecognizedText = text ?? "";
        Changed?.Invoke();
    }

    public static void PhraseDetected()
    {
        if (StopRequested)
            return;
        if (!HasPendingSos || string.IsNullOrEmpty(PendingSosId))
            Preferences.Default.Set(PendingSosIdKey, Guid.NewGuid().ToString("N"));
        Preferences.Default.Set(PendingSosKey, true);
        Update(false, "Safe phrase detected. Open SafePhrase to start the SOS countdown.");
    }

    internal static async Task<bool> TryOpenPendingSosAsync(Func<Task> openCountdown)
    {
        if (!IsAppVisible || isOpeningPendingSos || IsSosFlowOpen || IsVoicePromptOpen || !HasPendingSos)
            return false;
        isOpeningPendingSos = true;
        try
        {
            await openCountdown();
            // A failed navigation must leave the detection available for retry.
            ClearPendingSos();
            return true;
        }
        finally
        {
            isOpeningPendingSos = false;
        }
    }

    public static void ClearPendingSos()
    {
        Preferences.Default.Remove(PendingSosKey);
        Preferences.Default.Remove(PendingSosIdKey);
    }

    public static void BeginListening()
    {
        StopRequested = false;
        ClearPendingSos();
        UpdateRecognizedText(null);
    }

    public static bool TryRequestStart(out int requestId)
    {
        requestId = startGeneration;
        if (IsBusy)
            return false;
        requestId = ++startGeneration;
        IsStarting = true;
        BeginListening();
        Update(false, "Starting microphone...");
        return true;
    }

    public static bool IsCurrentStart(int requestId) =>
        IsStarting && !StopRequested && requestId == startGeneration;

    public static bool OwnsService(int requestId) =>
        IsActive && !StopRequested && requestId == startGeneration;

    public static bool AcceptStart(int requestId)
    {
        if (!IsCurrentStart(requestId))
            return false;
        IsStarting = false;
        Update(true, "Starting recognition...");
        return true;
    }

    public static void FinishService(int requestId, string status)
    {
        if (requestId != startGeneration)
            return;
        IsStarting = false;
        Update(false, status);
    }

    public static void SetListening(bool ready, string status)
    {
        IsListening = ready;
        Update(true, status);
    }

    public static void SetSosFlowOpen(bool open)
    {
        IsSosFlowOpen = open;
        Changed?.Invoke();
    }

    public static void SetVoicePromptOpen(bool open)
    {
        IsVoicePromptOpen = open;
        Changed?.Invoke();
    }

    public static void FinishSmsConfirmation(string status)
    {
        IsSendingSms = false;
        if (!IsActive && !IsStarting && !HasPendingSos)
            Update(false, status);
        else
            Changed?.Invoke();
    }

    public static void RequestStop(string status = "Safety Mode stopped by you.")
    {
        StopRequested = true;
        startGeneration++;
        IsStarting = false;
        ClearPendingSos();
        Update(false, status);
    }
}
