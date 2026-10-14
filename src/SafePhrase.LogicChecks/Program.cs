using SafePhrase;

var cases = new (string? Saved, string? Heard, bool Expected)[]
{
    ("Call Aunty Aigul", "call aunty aigul", true),
    ("Help me please", "  HELP,\tme   please!  ", true),
    ("Help me please", "help me", false),
    ("Help me please", "do not help me please", false),
    ("help", "shelf", false),
    ("", "", false),
    ("!!!", "...", false),
    (null, "help", false),
    ("help", null, false),
    ("Позови тётю", "ПОЗОВИ ТЁТЮ!", true),
    ("caf\u00e9", "cafe\u0301", true)
};

foreach (var test in cases)
{
    if (SafePhraseMatcher.Matches(test.Saved, test.Heard) != test.Expected)
        throw new InvalidOperationException($"Phrase matching failed: {test.Saved} / {test.Heard}");
}

Console.WriteLine($"Passed {cases.Length} phrase matching checks.");

int stateChecks = 0;
void Check(bool condition, string scenario)
{
    if (!condition)
        throw new InvalidOperationException($"Voice SOS state check failed: {scenario}");
    stateChecks++;
}

SafetyModeState.BeginListening();
SafetyModeState.IsAppVisible = false;
SafetyModeState.PhraseDetected();
int opens = 0;
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return Task.CompletedTask; })
    && opens == 0 && SafetyModeState.HasPendingSos, "background detection waits for the app");

SafetyModeState.IsAppVisible = true;
try
{
    await SafetyModeState.TryOpenPendingSosAsync(() => Task.FromException(new InvalidOperationException("Navigation unavailable")));
    throw new InvalidOperationException("Navigation failure was not propagated.");
}
catch (InvalidOperationException exception) when (exception.Message == "Navigation unavailable")
{
    Check(SafetyModeState.HasPendingSos, "failed navigation retains the detection");
}
Check(await SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return Task.CompletedTask; })
    && opens == 1 && !SafetyModeState.HasPendingSos, "retry opens and acknowledges the detection");
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return Task.CompletedTask; })
    && opens == 1, "acknowledged detection cannot open twice");

SafetyModeState.PhraseDetected();
var navigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Task<bool> firstOpen = SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return navigation.Task; });
Check(SafetyModeState.HasPendingSos, "detection stays pending while navigation is in flight");
SafetyModeState.PhraseDetected();
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return Task.CompletedTask; })
    && opens == 2, "duplicate detection cannot overlap navigation");
navigation.SetResult();
Check(await firstOpen && !SafetyModeState.HasPendingSos, "successful navigation acknowledges once");

SafetyModeState.PhraseDetected();
SafetyModeState.RequestStop();
SafetyModeState.PhraseDetected();
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => { opens++; return Task.CompletedTask; })
    && !SafetyModeState.HasPendingSos && opens == 2, "Stop discards pending and late detections");

int previewEvents = 0;
void OnPreviewChanged() => previewEvents++;
SafetyModeState.Changed += OnPreviewChanged;
SafetyModeState.UpdateRecognizedText("Example spoken phrase");
Check(previewEvents == 1 && SafetyModeState.LastRecognizedText == "Example spoken phrase",
    "recognized text updates the UI");
Check(!Microsoft.Maui.Storage.Preferences.Default.StoredKeys.Any(), "recognized text is not persisted");
SafetyModeState.BeginListening();
Check(SafetyModeState.LastRecognizedText.Length == 0 && previewEvents == 2 && !SafetyModeState.StopRequested,
    "new listening clears the preview and Stop request");
SafetyModeState.Changed -= OnPreviewChanged;

Console.WriteLine($"Passed {stateChecks} voice SOS state checks.");

int beforeFlowChecks = stateChecks;
SafetyModeState.Update(false, "Stopped");
Check(SafetyModeState.TryRequestStart(out int staleStart), "start request claimed");
Check(!SafetyModeState.TryRequestStart(out _), "queued start cannot overlap");
SafetyModeState.RequestStop();
Check(!SafetyModeState.AcceptStart(staleStart), "Stop invalidates a queued service start");
Check(SafetyModeState.TryRequestStart(out int currentStart) && SafetyModeState.AcceptStart(currentStart),
    "a fresh request can start after Stop");
SafetyModeState.FinishService(staleStart, "Old service stopped");
Check(SafetyModeState.OwnsService(currentStart) && !SafetyModeState.OwnsService(staleStart),
    "stale service cleanup and callbacks cannot affect a new start");
Check(SafetyModeState.IsActive && !SafetyModeState.IsStarting && !SafetyModeState.IsListening,
    "service acknowledgement does not claim microphone readiness");
SafetyModeState.SetListening(true, "Listening");
SafetyModeState.SetListening(false, "Restarting");
Check(SafetyModeState.IsActive && !SafetyModeState.IsListening && SafetyModeState.IsBusy,
    "restart keeps mode ownership and clears readiness");
SafetyModeState.RequestStop();
SafetyModeState.BeginListening();
SafetyModeState.SetSosFlowOpen(true);
SafetyModeState.PhraseDetected();
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => Task.CompletedTask) && SafetyModeState.HasPendingSos,
    "an existing countdown prevents a duplicate voice route");
SafetyModeState.SetSosFlowOpen(false);
SafetyModeState.RequestStop();

var cancelled = new SosFlowGuard();
cancelled.End();
Check(!cancelled.TryPrepare(true, true), "late Send Now cannot act after Cancel");
var flow = new SosFlowGuard();
Check(!flow.TryPrepare(false, true) && !flow.TryPrepare(true, false), "only the visible SOS page can prepare");
Check(flow.TryPrepare(true, true) && !flow.TryPrepare(true, true), "timer and Send Now prepare once");
Check(!flow.TryNext(true, true), "next contact cannot overlap an active handoff");
flow.CompletePreparation();
Check(!flow.TryNext(true, false) && flow.TryNext(true, true), "return to the foreground permits explicit next contact");
flow.End();
flow.CompletePreparation();
Check(!flow.CanAct(true, true) && !flow.TryNext(true, true), "leaving the page invalidates fallback and next-contact continuations");

var messageSettings = new SafetySettings { SosMessage = "Help & check on me" };
var timestamp = new DateTimeOffset(2026, 10, 13, 12, 30, 0, TimeSpan.FromHours(5));
var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
try
{
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    string message = SosMessageBuilder.Create(messageSettings,
        new Microsoft.Maui.Devices.Sensors.Location(43.25, 76.95), timestamp);
    Check(message.Contains("?q=43.25,76.95") && message.Contains("2026-10-13 12:30:00 +05:00"),
        "map coordinates and time are invariant across device languages");
    string link = SosMessageBuilder.CreateWhatsAppLink("+7 (701) 123-45-67", message);
    Check(link.StartsWith("https://wa.me/77011234567?text=")
        && Uri.UnescapeDataString(link.Split("?text=")[1]) == message,
        "WhatsApp preserves the message and international recipient");
    string noLocation = SosMessageBuilder.Create(messageSettings, null, timestamp);
    Check(noLocation.Contains("Location unavailable") && !noLocation.Contains("maps.google"),
        "missing location never invents coordinates");
    try
    {
        SosMessageBuilder.CreateWhatsAppLink("+٧٧٠١١٢٣٤٥٦٧", "Help");
        throw new InvalidOperationException("Invalid recipient was accepted.");
    }
    catch (ArgumentException)
    {
        Check(true, "unsupported phone digits cannot produce a broken WhatsApp recipient");
    }
}
finally
{
    System.Globalization.CultureInfo.CurrentCulture = oldCulture;
}
Console.WriteLine($"Passed {stateChecks - beforeFlowChecks} startup, cancellation and message checks.");

int beforeAlertChecks = stateChecks;
SafetyModeState.RequestStop();
SafetyModeState.BeginListening();
SafetyModeState.PhraseDetected();
string oldDetection = SafetyModeState.PendingSosId;
Check(SafetyModeState.IsCurrentDetection(oldDetection), "voice alert receives a pending detection identity");
SafetyModeState.PhraseDetected();
Check(SafetyModeState.PendingSosId == oldDetection, "duplicate detection preserves the alert identity");
SafetyModeState.IsAppVisible = true;
SafetyModeState.SetVoicePromptOpen(true);
Check(!await SafetyModeState.TryOpenPendingSosAsync(() => Task.CompletedTask) && SafetyModeState.HasPendingSos,
    "lock-screen confirmation prevents automatic countdown behind its buttons");
SafetyModeState.SetVoicePromptOpen(false);
Check(await SafetyModeState.TryOpenPendingSosAsync(() => Task.CompletedTask)
    && !SafetyModeState.IsCurrentDetection(oldDetection) && SafetyModeState.PendingSosId.Length == 0,
    "Continue acknowledges only after successful countdown navigation");
SafetyModeState.PhraseDetected();
string currentDetection = SafetyModeState.PendingSosId;
Check(currentDetection != oldDetection && !SafetyModeState.IsCurrentDetection(oldDetection)
    && SafetyModeState.IsCurrentDetection(currentDetection), "old alert intents cannot consume a new detection");
SafetyModeState.RequestStop();
Check(!SafetyModeState.IsCurrentDetection(currentDetection) && !Microsoft.Maui.Storage.Preferences.Default.StoredKeys.Any(),
    "Cancel clears the pending flag and detection identity");
Console.WriteLine($"Passed {stateChecks - beforeAlertChecks} lock-screen alert state checks.");

int beforeSmsChecks = stateChecks;
SafetyModeState.BeginListening();
SafetyModeState.PhraseDetected();
string smsDetection = SafetyModeState.PendingSosId;
Check(!SafetyModeState.TryClaimSmsConfirmation("stale", () => true) && SafetyModeState.HasPendingSos,
    "stale consent cannot consume a pending SMS alert");
SafetyModeState.SetSosFlowOpen(true);
Check(!SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => true), "an existing SOS screen prevents a competing SMS send");
SafetyModeState.SetSosFlowOpen(false);
SafetyModeState.IsAppVisible = true;
var smsNavigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Task<bool> openingSmsRoute = SafetyModeState.TryOpenPendingSosAsync(() => smsNavigation.Task);
Check(!SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => true), "an in-flight composer route cannot also send directly");
smsNavigation.SetException(new InvalidOperationException("Route failed"));
try { await openingSmsRoute; }
catch (InvalidOperationException exception) when (exception.Message == "Route failed") { }
Check(!SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => false) && SafetyModeState.HasPendingSos,
    "failed durable consent claim prevents dispatch and keeps the pending confirmation");
Check(SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => true) && SafetyModeState.IsSendingSms
    && !SafetyModeState.HasPendingSos, "consent is consumed before dispatch while keeping sending ownership");
Check(!SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => true)
    && !await SafetyModeState.TryOpenPendingSosAsync(() => Task.CompletedTask),
    "duplicate consent cannot send or open a second composer");
Check(!SafetyModeState.TryRequestStart(out _), "sending freezes the contact/settings snapshot and microphone startup");
SafetyModeState.RequestStop();
Check(SafetyModeState.IsSendingSms && SafetyModeState.IsBusy, "Stop cannot undo an already-confirmed carrier operation");
SafetyModeState.FinishSmsConfirmation("Sending status unknown; no automatic retry.");
Check(!SafetyModeState.IsSendingSms && !SafetyModeState.IsBusy && !SafetyModeState.HasPendingSos,
    "completion releases the mode without reviving a consumed detection");
Check(!SafetyModeState.TryClaimSmsConfirmation(smsDetection, () => true), "unknown outcome cannot auto-retry old consent");
Check(SafetyModeState.TryRequestStart(out int postSmsStart) && SafetyModeState.AcceptStart(postSmsStart),
    "student can manually start a fresh listening session after completion");
SafetyModeState.FinishSmsConfirmation("Late SMS status");
Check(SafetyModeState.OwnsService(postSmsStart), "late SMS status cannot stop a newer microphone session");
SafetyModeState.RequestStop();
Console.WriteLine($"Passed {stateChecks - beforeSmsChecks} direct-SMS consent state checks.");
