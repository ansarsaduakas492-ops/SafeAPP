using Android.App;
using Android.Content;
using System.Runtime.Versioning;

namespace SafePhrase;

[SupportedOSPlatform("android31.0")]
internal static class VoiceSosAlert
{
    public const int NotificationId = 7103;
    public const string DetectionIdExtra = "SafePhrase.DETECTION_ID";
    private const string ChannelId = "safephrase_voice_alarm";
    private const string SmsStatusChannelId = "safephrase_sms_status";

    public static bool CanOpenFullScreen(Context context)
    {
        NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        return !OperatingSystem.IsAndroidVersionAtLeast(34) || manager.CanUseFullScreenIntent();
    }

    [SupportedOSPlatform("android34.0")]
    public static void OpenFullScreenSettings(Context context)
    {
        using Intent manage = new(Android.Provider.Settings.ActionManageAppUseFullScreenIntent);
        manage.SetData(Android.Net.Uri.Parse("package:" + context.PackageName));
        manage.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(manage);
    }

    public static void Show(Context context, bool fullScreen)
    {
        string detectionId = SafetyModeState.PendingSosId;
        if (!SafetyModeState.IsCurrentDetection(detectionId))
            return;
        NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        // Users retain control of this channel's sound, visibility and importance.
        using NotificationChannel channel = new(ChannelId, "Voice SOS alerts", NotificationImportance.High);
        channel.LockscreenVisibility = NotificationVisibility.Private;
        manager.CreateNotificationChannel(channel);
        using Intent open = new(context, typeof(VoiceSosActivity));
        open.SetAction("SafePhrase.VOICE_SOS." + detectionId);
        open.PutExtra(DetectionIdExtra, detectionId);
        open.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        PendingIntent content = PendingIntent.GetActivity(context, 20, open,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        using Intent cancel = new(context, typeof(CancelVoiceSosReceiver));
        cancel.SetAction("SafePhrase.CANCEL_VOICE_SOS." + detectionId);
        cancel.PutExtra(DetectionIdExtra, detectionId);
        PendingIntent cancelIntent = PendingIntent.GetBroadcast(context, 21, cancel,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        Notification.Builder builder = new Notification.Builder(context, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
            .SetContentTitle("Safe phrase detected")
            .SetContentText("Cancel or confirm sending SOS by SMS. Nothing has been sent.")
            .SetVisibility(NotificationVisibility.Private)
            .SetCategory(Notification.CategoryAlarm)
            .SetContentIntent(content).SetAutoCancel(false)
            .AddAction(new Notification.Action.Builder((Android.Graphics.Drawables.Icon?)null,
                "Cancel", cancelIntent).Build())
            .AddAction(new Notification.Action.Builder((Android.Graphics.Drawables.Icon?)null,
                "Review SOS", content).Build());
        if (fullScreen && CanOpenFullScreen(context))
            builder.SetFullScreenIntent(content, true);
        manager.Notify(NotificationId, builder.Build());
    }

    public static void Dismiss(Context context)
    {
        try { ((NotificationManager)context.GetSystemService(Context.NotificationService)!).Cancel(NotificationId); }
        catch (Exception) { }
    }

    public static void ShowSmsStatus(Context context, string status)
    {
        try
        {
            NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
            using NotificationChannel channel = new(SmsStatusChannelId, "SOS SMS status", NotificationImportance.Default);
            manager.CreateNotificationChannel(channel);
            using Intent open = new(context, typeof(MainActivity));
            open.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            PendingIntent content = PendingIntent.GetActivity(context, 30, open,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
            manager.Notify(7104, new Notification.Builder(context, SmsStatusChannelId)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo).SetContentTitle("SOS SMS status")
                .SetContentText(status).SetStyle(new Notification.BigTextStyle().BigText(status))
                .SetVisibility(NotificationVisibility.Private).SetContentIntent(content).SetAutoCancel(true).Build());
        }
        catch (Exception) { }
    }

    public static void Cancel(Context context, string detectionId)
    {
        if (!SafetyModeState.IsCurrentDetection(detectionId))
            return;
        SafetyModeState.RequestStop("Voice SOS cancelled. Start Safety Mode to listen again.");
        Dismiss(context);
    }
}

[SupportedOSPlatform("android31.0")]
[BroadcastReceiver(Exported = false)]
public sealed class CancelVoiceSosReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is not null)
            VoiceSosAlert.Cancel(context, intent?.GetStringExtra(VoiceSosAlert.DetectionIdExtra) ?? "");
    }
}
