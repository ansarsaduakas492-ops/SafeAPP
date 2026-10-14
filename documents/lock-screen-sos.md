# Background voice SOS confirmation

Agreed: 2026-10-13; direct SMS follow-up 2026-10-14. The student requested a trigger in background listening to wake the screen and offer Cancel or Send SOS SMS. This supersedes notification-only handling where the platform allows full-screen alerts. Sending still requires an explicit tap on the confirmation.

## Behavior

1. Student starts Safety Mode in the foreground. Recognition remains experimental, on-device en-US. Stop/recents removal and manual restart after process death/boot remain.
2. A matched phrase stops microphone recognition and persists a pending flag plus a random detection ID. No spoken phrase or audio is persisted. Foreground detection still opens the existing countdown.
3. Background detection posts a separate high-importance voice SOS alarm notification. With full-screen access, Android may open the non-exported VoiceSosActivity, which uses showWhenLocked/turnScreenOn. It displays a generic SOS confirmation, no recipient/phrase/location details.
4. Cancel or device Back clears the matching pending detection and notification before closing. A stale notification or callback cannot cancel/consume a newer detection.
5. Continue SOS requests ordinary keyguard dismissal. Cancelled unlock stays at confirmation; failure permits retry. Only a live, resumed, unlocked activity with the same pending ID can open MainActivity. Success arriving before OnResume waits for resume.
6. If SMS permission is available, the button says Send SOS SMS. After an ordinary phone unlock, SafePhrase sends to the saved contacts directly; no second composer button is required. If permission is unavailable, the button opens the existing manual message flow. Foreground/manual channel selection remains unchanged. The confirmed-SMS behavior and its limits are in confirmed-sms.md.

Errors use their existing notification channel and ID; they never wake the SOS confirmation. Full-screen alerts use ID 7103 and their own channel. No background StartActivity workaround, overlay permission, accessibility service, credential bypass or boot receiver was added.

## Permission and platform boundary

When Start is tapped on Android 14+ and full-screen access is missing, SafePhrase explains why it is useful and offers Allow / Use notification. Allow opens Android's app-specific special-access screen directly; the user still turns on permission there. The current startup request waits for the app to leave and resume, then checks permission again and continues. If access remains off, the user chooses notification-only operation or Cancel. Already-granted access skips this step. Microphone recognition does not start while waiting, and Stop invalidates the startup request.

Settings -> Lock-screen SOS remains a secondary management entry and uses the same intent helper. Denial/revocation preserves a tappable notification with Cancel/Review SOS actions. Android special access cannot be granted by the app itself.

Android documents full-screen launch on locked/off screens when permitted, and heads-up notification behavior on unlocked screens. Notification importance, user/device settings and available permission affect the result. The app cannot promise forced opening while the user is in another app, or guaranteed wake on every phone. Google Play automatic full-screen access is limited to calling/alarm functionality; this local educational increment does not establish Play eligibility or publish anything.

Sources: [Full-screen intent limits](https://source.android.com/docs/core/permissions/fsi-limits), [Time-sensitive notifications](https://developer.android.com/develop/ui/views/notifications/time-sensitive), [Keyguard dismissal](https://developer.android.com/reference/android/app/KeyguardManager#requestDismissKeyguard(android.app.Activity,%20android.app.KeyguardManager.KeyguardDismissCallback)).

## Evidence

- Full Android Debug build passed with 0 warnings/errors in isolated obj/SmsConfirmation and bin/SmsConfirmation directories; generated manifest includes full-screen/SMS permissions and non-exported confirmation/receipt components. Debug is still fast deployment, not M4 standalone packaging.
- Earlier 46 linked logic checks predate the direct-SMS claim callback change; no new test run is recorded for this increment.
- Independent read-only review found no remaining actionable issue after ownership/resume/keyguard guards. Coordinator additionally identified Back revealing MainActivity and unintentionally starting the countdown; both Back APIs now cancel first and the fix was independently re-reviewed.
- Device wake, rendered native layout, SMS permission, SIM selection, outgoing SMS, permission-denied fallback, cancelled unlock, Back, rotation and stopped/removed app behavior remain unconfirmed. No device deployment or external message was performed.

## Student next activity (5 minutes)

Run the current project from Visual Studio and tap Start. If access is missing, follow the app's Allow offer, enable permission on the opened Android page and return. Then lock the phone, say the phrase and choose Cancel first. Report whether the offer/return worked and the screen showed both actions. Continue/unlock/message demonstration follows that result; sending remains deliberate.

## Guided permission follow-up

Student explicitly requested the app propose the needed permission instead of requiring manual settings discovery. The startup offer retains its generation across the system-settings round trip, rechecks visibility/permission before service dispatch and unsubscribes the temporary resume listener in finally. Stop/cancellation never dispatches an old request. Focused independent review reported no actionable issue. Phone permission/return behavior remains unconfirmed.

Full Android Debug follow-up build passed with 0 warnings/errors in separate obj/PermissionPrompt and bin/PermissionPrompt paths. Ordinary output encountered XALNS7024 because Visual Studio held the generated Android assembly; verification used isolated outputs without closing the IDE or deleting its files. Earlier 46 pure-logic results are reused because linked logic did not change. No current phone deployment or standalone M4 readiness is claimed.
