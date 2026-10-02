using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Speech;
using System.Runtime.Versioning;

namespace SafePhrase;

[SupportedOSPlatform("android31.0")]
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMicrophone)]
public sealed class BackgroundSpeechService : Service
{
    public const string StopAction = "SafePhrase.STOP_LISTENING";
    private const string ChannelId = "safephrase_listening";
    private const string AlertChannelId = "safephrase_detection";
    private const int ListeningNotificationId = 7101;
    private const int ResultNotificationId = 7102;
    private SpeechRecognizer? recognizer;
    private Handler? handler;
    private int version;
    private int consecutiveErrors;
    private bool running;
    private string phrase = "";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == StopAction)
        {
            SafetyModeState.RequestStop();
            StopListening("Safety Mode stopped by you.");
            return StartCommandResult.NotSticky;
        }

        if (running)
            return StartCommandResult.NotSticky;

        try
        {
            NotificationManager manager = (NotificationManager)GetSystemService(NotificationService)!;
            manager.CreateNotificationChannel(new NotificationChannel(ChannelId,
                "SafePhrase microphone", NotificationImportance.Low));
            manager.CreateNotificationChannel(new NotificationChannel(AlertChannelId,
                "SafePhrase detection and errors", NotificationImportance.High));
            manager.Cancel(ResultNotificationId);
            StartForeground(ListeningNotificationId, BuildNotification(
                "Safety Mode active", "Listening for your English safe phrase. Tap Stop to end.", true),
                ForegroundService.TypeMicrophone);
            phrase = SafetySettingsStore.Load().SafePhrase;
            if (SafePhraseMatcher.Normalize(phrase).Length == 0)
                throw new InvalidOperationException("Save a phrase containing words first.");
            if (!SpeechRecognizer.IsOnDeviceRecognitionAvailable(this))
                throw new InvalidOperationException("On-device speech recognition is unavailable.");
            handler = new Handler(Looper.MainLooper!);
            running = true;
            consecutiveErrors = 0;
            SafetyModeState.BeginListening();
            StartSession();
        }
        catch (Exception exception)
        {
            StopListening($"Could not start Safety Mode: {exception.Message}", notify: true);
        }
        // Android must not silently restart microphone use after process death.
        return StartCommandResult.NotSticky;
    }

    private void StartSession()
    {
        if (!running || SafetyModeState.StopRequested)
            return;
        int session = ++version;
        try
        {
            recognizer?.Destroy();
            recognizer = SpeechRecognizer.CreateOnDeviceSpeechRecognizer(this);
            recognizer.SetRecognitionListener(new Listener(
                text => handler?.Post(() => CompleteSession(session, text)),
                error => handler?.Post(() => HandleError(session, error))));
            using Intent request = new(RecognizerIntent.ActionRecognizeSpeech);
            request.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            request.PutExtra(RecognizerIntent.ExtraLanguage, "en-US");
            request.PutExtra(RecognizerIntent.ExtraPartialResults, false);
            recognizer.StartListening(request);
            SafetyModeState.Update(true, "Listening in English (US). Background mode is experimental.");
            handler?.PostDelayed(() =>
            {
                if (running && version == session)
                    StopListening("Speech session stopped responding. Start Safety Mode again.", notify: true);
            }, 45000);
        }
        catch (Exception exception)
        {
            StopListening($"Recognition failed: {exception.Message}", notify: true);
        }
    }

    private void CompleteSession(int session, string? text)
    {
        if (!running || SafetyModeState.StopRequested || version != session)
            return;
        consecutiveErrors = 0;
        if (SafePhraseMatcher.Matches(phrase, text))
        {
            StopListening("Safe phrase detected.");
            ((NotificationManager)GetSystemService(NotificationService)!).Notify(ResultNotificationId,
                BuildNotification("Safe phrase detected", "Tap to open the SOS countdown. No message sent.", false));
            SafetyModeState.PhraseDetected();
            return;
        }
        ScheduleRestart(400);
    }

    private void HandleError(int session, SpeechRecognizerError error)
    {
        if (!running || SafetyModeState.StopRequested || version != session)
            return;
        if (error is SpeechRecognizerError.NoMatch or SpeechRecognizerError.SpeechTimeout)
        {
            consecutiveErrors = 0;
            ScheduleRestart(400);
            return;
        }
        if (error is SpeechRecognizerError.InsufficientPermissions or SpeechRecognizerError.LanguageNotSupported
            or SpeechRecognizerError.LanguageUnavailable || ++consecutiveErrors >= 3)
        {
            StopListening($"Listening stopped: {error}. Check permissions/model, then start again.", notify: true);
            return;
        }
        ScheduleRestart(1000 * consecutiveErrors);
    }

    private void ScheduleRestart(int delay)
    {
        // Invalidate duplicate terminal callbacks before scheduling another session.
        version++;
        int restart = version;
        recognizer?.Destroy();
        recognizer = null;
        SafetyModeState.Update(true, "Safety Mode active. Restarting listening after the session ended...");
        handler?.PostDelayed(() =>
        {
            if (running && !SafetyModeState.StopRequested && version == restart)
                StartSession();
        }, delay);
    }

    private Notification BuildNotification(string title, string text, bool ongoing)
    {
        Intent open = new(this, typeof(MainActivity));
        open.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        PendingIntent content = PendingIntent.GetActivity(this, 0, open,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        Notification.Builder builder = new Notification.Builder(this, ongoing ? ChannelId : AlertChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title).SetContentText(text).SetContentIntent(content)
            .SetVisibility(NotificationVisibility.Private).SetOngoing(ongoing).SetAutoCancel(!ongoing);
        if (ongoing)
        {
            Intent stop = new(this, typeof(BackgroundSpeechService));
            stop.SetAction(StopAction);
            PendingIntent stopIntent = PendingIntent.GetService(this, 1, stop,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
            builder.AddAction(new Notification.Action.Builder(
                (Android.Graphics.Drawables.Icon?)null, "Stop", stopIntent).Build());
        }
        return builder.Build();
    }

    private void StopListening(string status, bool notify = false)
    {
        running = false;
        version++;
        handler?.RemoveCallbacksAndMessages(null);
        recognizer?.Cancel();
        recognizer?.Destroy();
        recognizer = null;
        StopForeground(StopForegroundFlags.Remove);
        SafetyModeState.Update(false, status);
        if (notify)
            ((NotificationManager)GetSystemService(NotificationService)!).Notify(ResultNotificationId,
                BuildNotification("Safety Mode stopped", status, false));
        StopSelf();
    }

    public override void OnDestroy()
    {
        running = false;
        version++;
        handler?.RemoveCallbacksAndMessages(null);
        handler?.Dispose();
        handler = null;
        recognizer?.Cancel();
        recognizer?.Destroy();
        recognizer = null;
        if (SafetyModeState.IsActive)
            SafetyModeState.Update(false, "Safety Mode stopped by Android. Start again to listen.");
        base.OnDestroy();
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        StopListening("Safety Mode stopped when the app was removed from recent apps.");
        base.OnTaskRemoved(rootIntent);
    }

    private sealed class Listener(Action<string?> result, Action<SpeechRecognizerError> error)
        : Java.Lang.Object, IRecognitionListener
    {
        public void OnResults(Bundle? results) => result(results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition)?.FirstOrDefault());
        public void OnError(SpeechRecognizerError value) => error(value);
        public void OnBeginningOfSpeech() { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() { }
        public void OnEvent(int eventType, Bundle? parameters) { }
        public void OnPartialResults(Bundle? partialResults) { }
        public void OnReadyForSpeech(Bundle? parameters) { }
        public void OnRmsChanged(float rmsdB) { }
    }
}
