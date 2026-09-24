using System.Collections.Generic;
using System.Runtime.Versioning;

namespace SafePhrase;

public partial class OfflineSpeechTestPage : ContentPage
{
#if ANDROID
    private Android.Speech.SpeechRecognizer? recognizer;
#endif

    public OfflineSpeechTestPage()
    {
        InitializeComponent();
#if !ANDROID
        StatusLabel.Text = "This experiment is available on Android only.";
        ListenButton.IsEnabled = false;
#endif
    }

    private async void OnCheckAvailabilityClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            StatusLabel.Text = "Checking and downloading speech models requires Android 13 or later.";
            return;
        }

        Android.Content.Context context = Android.App.Application.Context;
        if (!Android.Speech.SpeechRecognizer.IsOnDeviceRecognitionAvailable(context))
        {
            StatusLabel.Text = "No on-device recognition service is available on this phone.";
            return;
        }

        try
        {
            recognizer?.Destroy();
            recognizer = Android.Speech.SpeechRecognizer.CreateOnDeviceSpeechRecognizer(context);
            Android.Content.Intent intent = CreateEnglishIntent();
            StatusLabel.Text = "Checking whether English (US) is installed or can be downloaded...";
            Java.Util.Concurrent.IExecutor executor = context.MainExecutor
                ?? throw new InvalidOperationException("Android main-thread executor is unavailable.");
            recognizer.CheckRecognitionSupport(
                intent,
                executor,
                new RecognitionSupportCallback(support =>
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (OperatingSystem.IsAndroidVersionAtLeast(33))
                            HandleRecognitionSupport(support, intent);
                    }),
                    error => MainThread.BeginInvokeOnMainThread(() =>
                        StatusLabel.Text = $"Could not check language support (Android error {error}).")));
        }
        catch (Exception exception)
        {
            StatusLabel.Text = $"Could not check the English model: {exception.Message}";
        }
#else
        await DisplayAlertAsync("Android only", "Run this experiment on the Android phone.", "OK");
#endif
    }

#if ANDROID
    private static Android.Content.Intent CreateEnglishIntent()
    {
        Android.Content.Intent intent = new(Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguageModel,
            Android.Speech.RecognizerIntent.LanguageModelFreeForm);
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguage, "en-US");
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPartialResults, true);
        return intent;
    }

    [SupportedOSPlatform("android33.0")]
    private void HandleRecognitionSupport(Android.Speech.RecognitionSupport support,
        Android.Content.Intent intent)
    {
        const string englishTag = "en-US";
        if (support.InstalledOnDeviceLanguages?.Contains(englishTag) == true)
        {
            StatusLabel.Text = "English (US) offline model is installed. Tap Listen once in English.";
            return;
        }

        if (support.SupportedOnDeviceLanguages?.Contains(englishTag) == true)
        {
            StatusLabel.Text = "English (US) is supported. Requesting its offline model download...";
            try
            {
                recognizer?.TriggerModelDownload(intent);
                StatusLabel.Text = "Download requested. Keep Wi-Fi on; if Android asks, allow it. Tap Check again after the download finishes.";
            }
            catch (Exception exception)
            {
                StatusLabel.Text = $"Could not request the model download: {exception.Message}";
            }
            return;
        }

        if (support.PendingOnDeviceLanguages?.Contains(englishTag) == true)
        {
            StatusLabel.Text = "English (US) download is pending. Keep Wi-Fi on, then tap Check again later.";
            return;
        }

        string available = string.Join(", ", support.InstalledOnDeviceLanguages ?? []);
        string downloadable = string.Join(", ", support.SupportedOnDeviceLanguages ?? []);
        StatusLabel.Text = $"English (US) is not listed. Installed: {available}. Downloadable: {downloadable}.";
    }
#endif

    private async void OnListenClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            StatusLabel.Text = "On-device speech recognition requires Android 12 or later.";
            return;
        }

        if (!Android.Speech.SpeechRecognizer.IsOnDeviceRecognitionAvailable(Android.App.Application.Context))
        {
            StatusLabel.Text = "No on-device recognition service is available on this phone.";
            return;
        }

        PermissionStatus permission = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permission != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Microphone permission was not granted. No audio was processed.";
            return;
        }

        try
        {
            recognizer?.Destroy();
            recognizer = Android.Speech.SpeechRecognizer.CreateOnDeviceSpeechRecognizer(
                Android.App.Application.Context);
            recognizer.SetRecognitionListener(new OfflineRecognitionListener(
                text => MainThread.BeginInvokeOnMainThread(() =>
                {
                    ResultLabel.Text = string.IsNullOrWhiteSpace(text) ? "No words recognized." : text;
                    StatusLabel.Text = "Session finished. Tap Listen once in English to try again.";
                    SetListening(false);
                }),
                error => MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = $"Recognition stopped: {error}. Check the English speech model and try again.";
                    SetListening(false);
                }),
                partial => MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(partial))
                        ResultLabel.Text = partial;
                })));

            Android.Content.Intent intent = CreateEnglishIntent();

            ResultLabel.Text = "Listening...";
            StatusLabel.Text = "Speak a short English phrase. The recognizer will stop after silence.";
            SetListening(true);
            recognizer.StartListening(intent);
        }
        catch (Exception exception)
        {
            recognizer?.Destroy();
            recognizer = null;
            StatusLabel.Text = $"Could not start on-device recognition: {exception.Message}";
            SetListening(false);
        }
#else
        await DisplayAlertAsync("Android only", "Run this experiment on the Android phone.", "OK");
#endif
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
#if ANDROID
        recognizer?.Cancel();
        StatusLabel.Text = "Listening cancelled.";
        SetListening(false);
#endif
    }

    private void SetListening(bool isListening)
    {
        ListenButton.IsEnabled = !isListening;
        StopButton.IsEnabled = isListening;
    }

    protected override void OnDisappearing()
    {
#if ANDROID
        recognizer?.Cancel();
        recognizer?.Destroy();
        recognizer = null;
#endif
        base.OnDisappearing();
    }

#if ANDROID
    private sealed class OfflineRecognitionListener : Java.Lang.Object, Android.Speech.IRecognitionListener
    {
        private readonly Action<string?> onResult;
        private readonly Action<Android.Speech.SpeechRecognizerError> onError;
        private readonly Action<string?> onPartialResult;

        public OfflineRecognitionListener(
            Action<string?> onResult,
            Action<Android.Speech.SpeechRecognizerError> onError,
            Action<string?> onPartialResult)
        {
            this.onResult = onResult;
            this.onError = onError;
            this.onPartialResult = onPartialResult;
        }

        public void OnBeginningOfSpeech() { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() { }
        public void OnEvent(int eventType, Android.OS.Bundle? parameters) { }
        public void OnReadyForSpeech(Android.OS.Bundle? parameters) { }
        public void OnRmsChanged(float rmsdB) { }

        public void OnError(Android.Speech.SpeechRecognizerError error)
        {
            onError(error);
        }

        public void OnPartialResults(Android.OS.Bundle? partialResults)
        {
            onPartialResult(ReadText(partialResults));
        }

        public void OnResults(Android.OS.Bundle? results)
        {
            onResult(ReadText(results));
        }

        private static string? ReadText(Android.OS.Bundle? results)
        {
            IList<string>? candidates = results?.GetStringArrayList(
                Android.Speech.SpeechRecognizer.ResultsRecognition);
            return candidates is { Count: > 0 } ? candidates[0] : null;
        }
    }

    [SupportedOSPlatform("android33.0")]
    private sealed class RecognitionSupportCallback : Java.Lang.Object,
        Android.Speech.IRecognitionSupportCallback
    {
        private readonly Action<Android.Speech.RecognitionSupport> onSupport;
        private readonly Action<int> onError;

        public RecognitionSupportCallback(
            Action<Android.Speech.RecognitionSupport> onSupport,
            Action<int> onError)
        {
            this.onSupport = onSupport;
            this.onError = onError;
        }

        public void OnError(int errorCode) => onError(errorCode);

        public void OnSupportResult(Android.Speech.RecognitionSupport support) => onSupport(support);
    }
#endif
}
