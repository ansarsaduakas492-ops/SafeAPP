# APK candidate verification

Date: 2026-10-29. Scope: locally packaged SafePhrase 1.0 candidate. This record separates source/logic/package checks from physical-device acceptance. Historical device reports do not validate this Release APK.

## Automated and source evidence

- The existing `SafePhrase.LogicChecks` executable passed all 58 checks: 11 phrase matching, 11 voice/pending state, 18 startup/cancellation/message, 6 lock-screen alert state, and 12 direct-SMS consent state checks. These link selected application logic with platform stubs; they do not run Android speech, permissions, notification UI, or `SmsManager`.
- Focused independent source review covered Release JSON persistence compatibility, speech prerequisites, foreground composer behavior, background direct-SMS consent, and instruction accuracy. Package metadata/signature evidence is recorded in `apk-build.md`.
- No phone installation, microphone recording, external message, or recipient delivery was performed during packaging.

## Requirement/check matrix

Every row still needs the described check on the packaged APK. Source availability does not mean a requirement has passed on a phone.

| Requirement | Source or linked-logic evidence | Remaining packaged-phone check |
| --- | --- | --- |
| FR-01, AC-01: save phrase | SettingsPage and SafetySettingsStore; nonempty normalized phrase guard | Save, close/reopen, verify saved phrase. |
| FR-02: trusted contacts | SettingsPage validates international numbers | Add multiple consenting test contacts; restart and check. |
| FR-03: update/delete contacts | SettingsPage Edit/Delete handlers | Edit and delete; check persistence and recipients. |
| FR-04: SOS text | SettingsPage; SosMessageBuilder | Save custom text and inspect both composers. |
| FR-05, AC-02: Start | MainPage; startup generation/ownership checks | Start from visible Home with configured phrase/contact. |
| FR-06: Stop | MainPage and BackgroundSpeechService cleanup; state checks | Stop from Home and notification, then speak phrase; no SOS. |
| FR-07, AC-06: microphone permission | MainPage permission gate | Deny and grant; inspect error/status and retry. |
| FR-08: controlled listening | Service ownership/session cleanup; SafetyModeState checks | Stop, recents removal, process termination and restart. Recognition setup is a separate explicit one-session diagnostic. |
| FR-09: speech to text | Android on-device en-US recognizer; no fake recognition | Compatible phone/model, silence, unrelated speech, repeated sessions. |
| FR-10: phrase comparison | 11 matching checks for normalized exact equality | Say exact/nonmatching phrases and inspect Details. |
| FR-11, AC-03: voice SOS | Pending/duplicate/startup/alert state checks | One detection opens one flow; repeat and verify no overlap. |
| FR-12, AC-06: location permission | SosCountdownPage permission request and unavailable fallback | Deny/grant/disable location; inspect message and status. |
| FR-13: Maps link | SosMessageBuilder; message checks for coordinate formatting | Check actual current location link; omit private coordinates from evidence. |
| FR-14: time | Message builder uses local offset timestamp; message checks | Compare prepared message to phone time. |
| FR-15: five-second countdown | SosCountdownPage countdown; cancellation guard | After location prompt, observe countdown and Open message now. Background direct SMS is an explicitly agreed alternative path. |
| FR-16, AC-04: cancel | SOS flow and alert identity/cancellation checks | Cancel countdown, lock-screen alert and Back; verify no message dispatch. |
| FR-17, AC-05: channel handoff | SMS ComposeAsync; encoded wa.me link per contact; copy fallback | WhatsApp, SMS, multiple contacts, missing channel, copy, and a deliberate agreed send. Ask recipient separately about receipt. |
| FR-18: no audio storage | Speech listener processes text; no application recording/file path in reviewed flow | Inspect behavior on chosen phone; do not claim a privacy audit. |
| FR-19: no server audio upload | CreateOnDeviceSpeechRecognizer; no application speech backend | Verify model readiness and offline recognition; Android/provider privacy remains a separate boundary. |
| FR-20: local settings | Preferences JSON; Release reflection enabled in generated configuration | Release settings save/load, app restart and contact editing. Android backup is enabled. |
| Background extension | User-started microphone foreground service; notification Stop | Lock/unlock, notification permissions, interruptions, power management and restart. Original NFR-03 background exclusion is superseded for this user-approved extension; source specification remains unchanged. |
| Direct-SMS extension | Explicit consent ledger; 12 consent state checks; permission/SIM validation | Permission grant/denial, default SIM, unlock, cancellation, partial/unknown results and actual recipient receipt with a consenting contact. No automatic retry. |

## Phone acceptance activity

Use the actual delivered APK and record phone model, Android/API version, candidate SHA-256, and results without phone numbers, exact locations, or audio. Begin with three steps:

1. Install/open the APK without Visual Studio; save phrase, channel and contact, then close/reopen and verify persistence.
2. Run Prepare SOS, cancel once, then inspect the composed message without sending. Check both channels and location denial.
3. Check English recognition; exercise Start/Stop, then separately test a locked-screen Cancel before any agreed direct-SMS send.

After these checks, record permissions, multiple-contact behavior, intentional recipient-confirmed delivery, interruptions, and the remaining matrix rows. Do not label a composer opening or Android submission callback as receipt.

## Learning and acceptance

Student teach-back: why does opening an SMS/WhatsApp composer require another Send tap, while confirming Send SOS SMS on the lock-screen path does not?

The student walkthrough, manual code-change exercise, S1/M2 evidence, M3-3 device work, and dated mentor acceptance remain open. M4 is not accepted by creating an APK. The October 8 target is overdue; dates and required evidence are unchanged. The next checkpoint is the installation/cancellation check above, followed by a smaller agreed phone test session.
