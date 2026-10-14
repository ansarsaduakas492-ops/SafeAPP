using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Runtime.Versioning;
using AColor = Android.Graphics.Color;
using AButton = Android.Widget.Button;
using View = Android.Views.View;
using Microsoft.Maui.Devices.Sensors;

namespace SafePhrase;

// Only this generic confirmation is shown over keyguard; contacts/message stay behind unlock.
[SupportedOSPlatform("android31.0")]
[Activity(Exported = false, ExcludeFromRecents = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public sealed class VoiceSosActivity : Activity
{
    private AButton? continueButton;
    private TextView? explanation;
    private bool openingMain;
    private bool resumed;
    private string detectionId = "";
    private string? unlockedDetectionToContinue;
    private BackCallback? backCallback;
    private AButton? cancelButton;
    private bool sendingSms;
    private bool sendFinished;
    private bool directSmsOffered;
    private bool confirmedDirectSms;
    private Task<Location?>? locationTask;
    private readonly CancellationTokenSource locationCancellation = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        detectionId = Intent?.GetStringExtra(VoiceSosAlert.DetectionIdExtra) ?? "";
        if (DirectSmsSender.HasClaimedConfirmation(this, detectionId))
        {
            if (SafetyModeState.IsCurrentDetection(detectionId))
                SafetyModeState.ClearPendingSos();
            VoiceSosAlert.Dismiss(this);
            Finish();
            return;
        }
        if (!SafetyModeState.IsCurrentDetection(detectionId) || SafetyModeState.IsSosFlowOpen)
        {
            Finish();
            return;
        }
        SetShowWhenLocked(true);
        SetTurnScreenOn(true);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            backCallback = new BackCallback(CancelOrClose);
            OnBackInvokedDispatcher!.RegisterOnBackInvokedCallback(0, backCallback);
        }
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(AColor.ParseColor("#F4F7F6"));
        root.SetOnApplyWindowInsetsListener(new InsetsListener(Dp(24)));
        var scroll = new Android.Widget.ScrollView(this);
        var content = new LinearLayout(this) { Orientation = Orientation.Vertical };
        content.SetGravity(GravityFlags.Center);
        scroll.FillViewport = true;
        scroll.AddView(content);
        root.AddView(scroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        TextView symbol = Label("SOS", 36, "#FFFFFF");
        symbol.Gravity = GravityFlags.Center;
        symbol.Background = Shape("#B43C47", 56);
        content.AddView(symbol, new LinearLayout.LayoutParams(Dp(112), Dp(112))
            { Gravity = GravityFlags.Center, BottomMargin = Dp(24) });
        content.AddView(Label("Safe phrase detected", 26, "#183735"));
        explanation = Label("Send an SOS SMS to all your saved contacts?\nNothing has been sent.", 17, "#526B67");
        content.AddView(explanation, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(16), BottomMargin = Dp(24) });
        cancelButton = ActionButton("Cancel SOS", "#176E59");
        cancelButton.Click += (_, _) => CancelOrClose();
        root.AddView(cancelButton, ButtonLayout());
        directSmsOffered = DirectSmsSender.HasPermission(this);
        continueButton = ActionButton(directSmsOffered ? "Send SOS SMS" : "Open SOS message", "#B43C47");
        continueButton.Click += (_, _) => ContinueSos();
        root.AddView(continueButton, ButtonLayout());
        root.AddView(Label(DirectSmsSender.HasPermission(this)
            ? "Unlock to send. SMS charges may apply."
            : "SMS permission is off. You will confirm sending in the message app.", 13, "#526B67"));
        SetContentView(root);
        locationTask = TryGetLocationAsync(locationCancellation.Token);
    }

    protected override void OnResume()
    {
        base.OnResume();
        resumed = true;
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.Changed += OnSafetyChanged;
        if ((!sendingSms && !sendFinished && !SafetyModeState.IsCurrentDetection(detectionId)) || SafetyModeState.IsSosFlowOpen)
        {
            Finish();
            return;
        }
        SafetyModeState.SetVoicePromptOpen(true);
        if (!openingMain && !sendingSms && !sendFinished)
        {
            directSmsOffered = DirectSmsSender.HasPermission(this);
            continueButton!.Text = directSmsOffered ? "Send SOS SMS" : "Open SOS message";
        }
        if (unlockedDetectionToContinue is string id)
        {
            unlockedDetectionToContinue = null;
            OpenMain(id);
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        sendFinished = false;
        directSmsOffered = DirectSmsSender.HasPermission(this);
        continueButton!.Text = directSmsOffered ? "Send SOS SMS" : "Open SOS message";
        unlockedDetectionToContinue = null;
        detectionId = intent?.GetStringExtra(VoiceSosAlert.DetectionIdExtra) ?? "";
        if (!SafetyModeState.IsCurrentDetection(detectionId))
        {
            Finish();
            return;
        }
        RetryUnlock("Send an SOS SMS to all your saved contacts? Nothing has been sent.");
    }

    protected override void OnPause()
    {
        resumed = false;
        SafetyModeState.Changed -= OnSafetyChanged;
        SafetyModeState.SetVoicePromptOpen(false);
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        unlockedDetectionToContinue = null;
        locationCancellation.Cancel();
        locationCancellation.Dispose();
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && backCallback is not null)
            OnBackInvokedDispatcher!.UnregisterOnBackInvokedCallback(backCallback);
        SafetyModeState.Changed -= OnSafetyChanged;
        Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        base.OnDestroy();
    }

    public override void OnBackPressed() => CancelOrClose();

    private void CancelOrClose()
    {
        if (sendingSms || sendFinished)
            Finish();
        else
            VoiceSosAlert.Cancel(this, detectionId);
    }

    [SupportedOSPlatform("android33.0")]
    private sealed class BackCallback(Action cancel) : Java.Lang.Object, Android.Window.IOnBackInvokedCallback
    {
        public void OnBackInvoked() => cancel();
    }

    private void OnSafetyChanged()
    {
        if (resumed && !sendingSms && !sendFinished && !SafetyModeState.IsCurrentDetection(detectionId))
            Finish();
    }

    private void ContinueSos()
    {
        if (sendFinished)
        {
            Finish();
            return;
        }
        if (openingMain || !resumed || !SafetyModeState.IsCurrentDetection(detectionId))
            return;
        openingMain = true;
        confirmedDirectSms = directSmsOffered;
        continueButton!.Enabled = false;
        KeyguardManager keyguard = (KeyguardManager)GetSystemService(KeyguardService)!;
        if (keyguard.IsKeyguardLocked)
        {
            explanation!.Text = confirmedDirectSms
                ? "Unlock to send the confirmed SOS SMS to your saved contacts."
                : "Unlock to open your message app. You will press Send there.";
            keyguard.RequestDismissKeyguard(this, new UnlockCallback(this, detectionId));
        }
        else
        {
            OpenMain(detectionId);
        }
    }

    private void OpenMain(string expectedDetectionId)
    {
        if (IsFinishing || IsDestroyed || expectedDetectionId != detectionId
            || !SafetyModeState.IsCurrentDetection(expectedDetectionId))
            return;
        if (!resumed)
        {
            // Keyguard success can precede OnResume; never launch from a paused activity.
            unlockedDetectionToContinue = expectedDetectionId;
            return;
        }
        if (((KeyguardManager)GetSystemService(KeyguardService)!).IsKeyguardLocked)
        {
            RetryUnlock("Unlock your phone, then tap Continue SOS.");
            return;
        }
        if (confirmedDirectSms)
        {
            _ = SendConfirmedSmsAsync(expectedDetectionId);
            return;
        }
        try
        {
            using Intent open = new(this, typeof(MainActivity));
            open.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            StartActivity(open);
            SafetyModeState.SetVoicePromptOpen(false);
            // Pending SOS is acknowledged only by successful MAUI countdown navigation.
            Finish();
        }
        catch (Exception)
        {
            RetryUnlock("Could not open SafePhrase. Tap Continue SOS to retry.");
        }
    }

    private static async Task<Location?> TryGetLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
                return null;
            Location? location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), cancellationToken);
            if (location is null)
                return null;
            return IsFreshLocation(location) ? location : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsFreshLocation(Location location)
    {
        TimeSpan age = DateTimeOffset.Now - location.Timestamp;
        return age >= TimeSpan.Zero && age <= TimeSpan.FromSeconds(30) && location.Accuracy is > 0 and <= 500;
    }

    private async Task SendConfirmedSmsAsync(string expectedDetectionId)
    {
        try
        {
            using var manager = DirectSmsSender.GetManager(this);
            SafetySettings settings = SafetySettingsStore.Load();
            string[] phones = settings.Contacts.Select(contact => contact.Phone)
                .Where(phone => !string.IsNullOrWhiteSpace(phone)).Distinct().ToArray();
            if (phones.Length == 0)
                throw new InvalidOperationException("No contacts are saved. No SMS was sent.");
            Location? location = locationTask is { IsCompletedSuccessfully: true } ? locationTask.Result : null;
            if (location is not null && !IsFreshLocation(location))
                location = null;
            string message = SosMessageBuilder.Create(settings, location, DateTimeOffset.Now);
            sendingSms = true;
            if (!SafetyModeState.TryClaimSmsConfirmation(expectedDetectionId,
                    () => DirectSmsSender.PersistConfirmationClaim(this, expectedDetectionId)))
            {
                sendingSms = false;
                Finish();
                return;
            }
            VoiceSosAlert.Dismiss(this);
            VoiceSosAlert.ShowSmsStatus(this, "Confirmed SMS attempt started. If interrupted, check your SMS app; sending status may be unknown.");
            continueButton!.Text = "Sending...";
            explanation!.Text = "Sending the confirmed SOS SMS. You can close this screen; sending will continue.";
            cancelButton!.Text = "Close";
            string status = await DirectSmsSender.SendAsync(ApplicationContext!, manager, message, phones);
            sendingSms = false;
            sendFinished = true;
            SafetyModeState.FinishSmsConfirmation(status);
            VoiceSosAlert.ShowSmsStatus(ApplicationContext!, status);
            if (!IsFinishing && !IsDestroyed)
            {
                explanation.Text = status;
                continueButton.Text = "Close";
                continueButton.Enabled = true;
            }
        }
        catch (Exception exception)
        {
            if (sendingSms)
            {
                string status = "SMS attempt interrupted; sending status is unknown. Check your SMS app before trying again.";
                sendingSms = false;
                sendFinished = true;
                SafetyModeState.FinishSmsConfirmation(status);
                VoiceSosAlert.ShowSmsStatus(ApplicationContext!, status);
                if (!IsFinishing && !IsDestroyed)
                {
                    explanation!.Text = status;
                    continueButton!.Text = "Close";
                    continueButton.Enabled = true;
                }
            }
            else if (!IsFinishing && !IsDestroyed)
            {
                directSmsOffered = DirectSmsSender.HasPermission(this);
                continueButton!.Text = directSmsOffered ? "Send SOS SMS" : "Open SOS message";
                RetryUnlock(exception.Message);
            }
        }
    }

    private void RetryUnlock(string text)
    {
        if (IsFinishing || IsDestroyed)
            return;
        unlockedDetectionToContinue = null;
        openingMain = false;
        continueButton!.Enabled = true;
        explanation!.Text = text;
    }

    private sealed class UnlockCallback(VoiceSosActivity owner, string detectionId) : KeyguardManager.KeyguardDismissCallback
    {
        public override void OnDismissSucceeded() => owner.OpenMain(detectionId);
        public override void OnDismissCancelled()
        {
            if (owner.detectionId == detectionId)
                owner.RetryUnlock("Unlock cancelled. Cancel SOS or tap Continue to try again.");
        }
        public override void OnDismissError()
        {
            if (owner.detectionId == detectionId)
                owner.RetryUnlock("Unlock your phone, then tap Continue SOS.");
        }
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);
    private TextView Label(string text, int size, string color)
    {
        var label = new TextView(this) { Text = text, TextSize = size, Gravity = GravityFlags.Center };
        label.SetTextColor(AColor.ParseColor(color));
        label.SetPadding(0, Dp(8), 0, Dp(8));
        return label;
    }
    private AButton ActionButton(string text, string color)
    {
        var button = new AButton(this) { Text = text, TextSize = 17, ContentDescription = text };
        button.SetAllCaps(false);
        button.SetTextColor(AColor.White);
        button.Background = Shape(color, 16);
        return button;
    }
    private Android.Graphics.Drawables.GradientDrawable Shape(string color, int radius)
    {
        var shape = new Android.Graphics.Drawables.GradientDrawable();
        shape.SetColor(AColor.ParseColor(color));
        shape.SetCornerRadius(Dp(radius));
        return shape;
    }
    private LinearLayout.LayoutParams ButtonLayout() =>
        new(ViewGroup.LayoutParams.MatchParent, Dp(60)) { BottomMargin = Dp(12) };

    private sealed class InsetsListener(int padding) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View? view, WindowInsets? insets)
        {
            var bars = insets!.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
            view!.SetPadding(padding + bars.Left, padding + bars.Top, padding + bars.Right, padding + bars.Bottom);
            return insets;
        }
    }
}
