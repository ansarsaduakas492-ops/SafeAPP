# Confirmed background SOS SMS

Agreed: 2026-10-14. After a background voice trigger, SafePhrase presents a generic Cancel / Send SOS SMS confirmation. If the user confirms while the phone is locked, Android requires its normal unlock. SafePhrase then sends the prepared SOS text to each saved contact directly through Android SMS; there is no second Send button in a composer.

On Start, SafePhrase explains and requests Android's SMS permission. If permission is denied or later unavailable, the confirmation offers the existing message-app route, where the user sends manually. The saved manual SMS/WhatsApp setting still governs foreground and manual SOS flows. SMS charges may apply; users can cancel before confirming.

The app builds one message from the saved SOS text, timestamp and a fresh available location. Location permission is not requested by the lock-screen prompt; missing or stale location is omitted according to the existing message builder. Phone numbers are read from the saved trusted-contact list when the user confirms.

Before calling SmsManager, the app synchronously records the confirmed detection ID in private app storage and consumes the pending alert. The same alert cannot automatically dispatch again after process interruption. This deliberately prefers avoiding duplicate SOS messages; if the process stops after claiming consent but before Android accepts the SMS, the user must start a new SOS. Partial native outcomes or missing sent-intent callbacks are reported as unknown/incomplete and are never automatically retried. A successful Android sent callback reports submission status, not recipient delivery or reading. Check the SMS app before retrying an unknown result.

The app does not simulate taps in another app or press the system Send button. Android's `SmsManager` API is the direct SMS mechanism and requires `SEND_SMS`; platform policy, SIM availability, carrier service, notification settings and device behavior still apply. Direct SMS has not been exercised on the student's phone, no message was sent, and Google Play approval is not claimed. Store publishing policy is a separate M4 decision.

Sources: [Android SmsManager](https://developer.android.com/reference/android/telephony/SmsManager), [Android SubscriptionManager](https://developer.android.com/reference/android/telephony/SubscriptionManager), [Google Play SMS/Call Log permissions](https://support.google.com/googleplay/android-developer/answer/10208820).
