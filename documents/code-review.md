# M3-4 source review and checkpoint evidence

Date: 2026-10-13. Scope: listening -> pending detection -> countdown -> manual SMS/WhatsApp handoff, with the student's compact visual refinement. Earlier uncommitted work is preserved.

This records the M3-4 snapshot. The subsequent M3-6 increment in lock-screen-sos.md supersedes notification-only background handling, adds a persisted random detection ID beside the pending flag, and brings current logic checks to 46; no speech/audio is persisted.

## Review findings and fixes

| Finding | Resolution |
| --- | --- |
| Settings could change the displayed phrase/recipients while the service retained its startup snapshot. | Home navigation and settings mutations are guarded during startup, listening, pending SOS and an open SOS flow. Delete confirmation rechecks the guard. |
| Home and the notification claimed listening before Android reported readiness. | Service ownership and microphone readiness are separate. Only a current-session OnReadyForSpeech sets Listening; startup/restart clear readiness and update the notification. Stop stays available during startup. |
| The local startup flag ended before Android acknowledged the queued command. | Shared start generation spans permissions and service acknowledgement. Stop invalidates queued requests; StopSelf(startId) preserves newer commands. Callback/watchdog/cleanup ownership protects a newer session. |
| Cancel/navigation and a delayed fallback could still reach preparation. | The SOS flow guard ends before navigation, claims preparation once, rejects overlapping contacts and rechecks foreground/page ownership after asynchronous choices. Shell navigation invalidates the page flow. |
| Location permission could overlap the timer and foreground cancellation. | Explicit permission precedes the countdown. Denial or location failure preserves SOS without fabricated coordinates. |
| Unicode digits could be accepted as a WhatsApp recipient. | Contact validation uses ASCII international digits and rejects a zero-leading country code; URL creation validates the normalized recipient. |

Independent read-only review of startup/service/settings and the integrated SOS flow identified the first three findings plus the stale-command cleanup edge. Findings were fixed and re-reviewed; final review reported no remaining actionable finding. Coordinator reviewed formatting, native UI/event wiring, manifest, notification/privacy configuration and recognition-helper cleanup.

## Retained behavior

- On-device en-US recognition, bounded errors, session version invalidation, silence/nonmatch restarts, non-sticky service and manual restart after process death/boot. Stop and recents removal cancel microphone work.
- Background detection persists only a pending boolean. Failed countdown navigation retains it; concurrent opening is guarded and successful navigation acknowledges it. Recognized text stays in memory, cleared at a new start; no audio/text journal or upload was added.
- Five-second countdown, Cancel and immediate preparation share the guarded flow. SMS opens all recipients; WhatsApp opens contacts sequentially. Failure offers explicit SMS/copy choices. User presses Send; opening a composer does not establish delivery.

## Validation performed

- `dotnet run --project src/SafePhrase.LogicChecks --no-restore -p:UseSharedCompilation=false`: 40 checks passed (11 phrase, 11 pending/preview, 18 startup/ownership/cancellation/message). Actual application logic is linked; only MAUI Preferences and Location are substituted in the console project.
- Final full Android Debug build passed with 0 warnings/errors: `dotnet build src/SafePhrase/SafePhrase.csproj -f net10.0-android --no-restore -m:1 -nr:false -v:minimal -p:UseSharedCompilation=false`.
- Sandboxed full build failed with AAPT2 pipe closure. Approved build outside that environment exposed damaged flat intermediates. Removed only the verified project obj/Debug/net10.0-android cache and rebuilt successfully. No source/user files were deleted.
- Native SVG icons are processed by MAUI. Source review checked transparent overlays, button descriptions/labels, Stop/SOS accents, disabled gates, fixed Cancel/message controls and light/dark styling. No rendered-phone result is claimed.

This is implementation/source evidence, not physical-device, student or mentor acceptance. Debug fast deployment is not standalone APK readiness; M4 packaging remains pending. No commit, push, device deployment or outgoing message performed.

## Tuesday checkpoint and next step

October 6 M3 and October 8 M4 targets remain overdue; dates unchanged. M3-4 code preparation is complete. Outstanding S1/M2 evidence, student M3-3 and mentor acceptance keep M3 active/late and M4 pending.

Student first runs from Visual Studio and reports the main screen's clarity. Remaining phone evidence covers delayed startup/Stop, silence/unrelated speech, lock-screen/minimize detection, notification return, failed-navigation retry, recents removal, permission/model errors, both channels and no automatic restart after process death. Use deliberate demo contacts and confirm any outgoing message personally.
