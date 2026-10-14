using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Telephony;
using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace SafePhrase;

[SupportedOSPlatform("android31.0")]
internal static class DirectSmsSender
{
    public const string ReceiptIdExtra = "SafePhrase.SMS_RECEIPT_ID";
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> receipts = new();
    private const string ClaimsStore = "safephrase_sms_claims";
    private const string ClaimedDetectionKey = "last_claimed_detection_id";

    public static bool HasClaimedConfirmation(Context context, string detectionId) =>
        !string.IsNullOrEmpty(detectionId)
        && context.GetSharedPreferences(ClaimsStore, FileCreationMode.Private)?.GetString(ClaimedDetectionKey, "") == detectionId;

    public static bool PersistConfirmationClaim(Context context, string detectionId)
    {
        try
        {
            if (string.IsNullOrEmpty(detectionId) || HasClaimedConfirmation(context, detectionId))
                return false;
            return context.GetSharedPreferences(ClaimsStore, FileCreationMode.Private)?.Edit()?
                .PutString(ClaimedDetectionKey, detectionId)?.Commit() == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool HasPermission(Context context) =>
        context.CheckSelfPermission(Android.Manifest.Permission.SendSms) == Permission.Granted;

    public static SmsManager GetManager(Context context)
    {
        if (!HasPermission(context))
            throw new InvalidOperationException("SMS permission is off. Use the message app or allow SMS sending.");
        string feature = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? PackageManager.FeatureTelephonyMessaging
            : PackageManager.FeatureTelephony;
        if (context.PackageManager?.HasSystemFeature(feature) != true)
            throw new InvalidOperationException("This device cannot send SMS.");
        int subscription = SubscriptionManager.DefaultSmsSubscriptionId;
        if (!SubscriptionManager.IsValidSubscriptionId(subscription))
            throw new InvalidOperationException("Choose a default SIM for SMS in Android Settings, then try again. No SMS was sent.");
        var manager = (SmsManager?)context.GetSystemService(Java.Lang.Class.FromType(typeof(SmsManager)))
            ?? throw new InvalidOperationException("SMS service is unavailable.");
        return manager.CreateForSubscriptionId(subscription);
    }

    public static async Task<string> SendAsync(Context context, SmsManager manager, string message, string[] recipients)
    {
        // Queue each recipient once. Receipt callbacks confirm submission, not recipient delivery.
        var outcomes = new List<Task<int>>();
        foreach (string phone in recipients)
            outcomes.Add(SendToContactAsync(context, manager, phone, message));
        int[] results = await Task.WhenAll(outcomes);
        int sent = results.Count(result => result == 1);
        int failed = results.Count(result => result == 0);
        int unknown = results.Length - sent - failed;
        return $"SMS sent: {sent}/{results.Length} contacts. Failed or incomplete: {failed}. Status unknown: {unknown}. "
            + "Recipient delivery is not confirmed. No automatic retry; check your SMS app before sending again.";
    }

    private static async Task<int> SendToContactAsync(Context context, SmsManager manager, string phone, string message)
    {
        var pending = new List<PendingIntent>();
        var ids = new List<string>();
        var completion = new List<Task<bool>>();
        bool dispatchEntered = false;
        try
        {
            var parts = manager.DivideMessage(message)!;
            foreach (string part in parts)
            {
                string id = Guid.NewGuid().ToString("N");
                var receipt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                receipts[id] = receipt;
                ids.Add(id);
                completion.Add(receipt.Task);
                using Intent sent = new(context, typeof(SmsSentReceiver));
                sent.SetAction("SafePhrase.SMS_SENT." + id);
                sent.PutExtra(ReceiptIdExtra, id);
                pending.Add(PendingIntent.GetBroadcast(context, 0, sent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!);
            }
            dispatchEntered = true;
            manager.SendMultipartTextMessage(phone, null, parts, pending, null);
            bool[] sentParts = await Task.WhenAll(completion).WaitAsync(TimeSpan.FromSeconds(45));
            return sentParts.All(success => success) ? 1 : 0;
        }
        catch (TimeoutException)
        {
            // A late carrier response is not evidence that sending failed; do not repeat it.
            return completion.Any(task => task.IsCompletedSuccessfully && !task.Result) ? 0 : 2;
        }
        catch (Exception)
        {
            // Once Android's send API is entered, an exception cannot prove that no part was queued.
            return dispatchEntered ? 2 : 0;
        }
        finally
        {
            foreach (string id in ids)
                receipts.TryRemove(id, out _);
            foreach (PendingIntent intent in pending)
            {
                try { intent.Cancel(); }
                catch (Exception) { }
                try { intent.Dispose(); }
                catch (Exception) { }
            }
        }
    }

    public static void RecordReceipt(string id, bool success)
    {
        if (receipts.TryGetValue(id, out var receipt))
            receipt.TrySetResult(success);
    }
}

[SupportedOSPlatform("android31.0")]
[BroadcastReceiver(Exported = false)]
public sealed class SmsSentReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) =>
        DirectSmsSender.RecordReceipt(intent?.GetStringExtra(DirectSmsSender.ReceiptIdExtra) ?? "",
            (int)ResultCode == (int)Result.Ok);
}
