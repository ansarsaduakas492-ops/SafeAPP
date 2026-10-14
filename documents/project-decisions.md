# Project decisions

## 2026-09-22 Initial demonstration scope

- The project is an educational proof of concept.
- The application will be demonstrated on Android, with APK installation as the intended distribution method for the demonstration.
- Push notification infrastructure is excluded from the current proof of concept scope.
- SignalR and polling are candidates if in-app message reception is required. Neither has been selected, and a backend has not been approved.
- At this point the framework and technology stack were undecided; the later decision below supersedes this item.

## Open messaging question

The supplied specification prepares an SOS message and opens SMS or WhatsApp for the user to send it manually. It does not define a recipient application or an application messaging backend.

Resolved by the messaging clarification below: retain external WhatsApp/SMS composition and manual sending. A recipient SafePhrase application is not part of this pilot.

## 2026-09-22 MAUI and guided two-week delivery (approved)

- Use .NET MAUI with C# and XAML for the Android APK application.
- Aim to deliver the educational prototype in two weeks. Hold short progress calls every Tuesday and Thursday.
- Codex implements code and UI in small stages, explains each increment in beginner-friendly language, reviews changes, and records progress.
- The student performs selected activities manually, starting with IDE installation, choosing a project name, and creating the project with step-by-step guidance.
- Favor understandable best practices and a small application structure over extensive decomposition.
- Token economy is the first agent workflow rule. Use targeted reads, bounded fresh-context delegation where useful, short result summaries, and explicit handoffs to new chats.

## Working planning assumptions (not additional approved requirements)

- The original September 22 kickoff assumption is superseded by the approved calendar below.
- Plan against the existing specification: manual sending through SMS/WhatsApp. A recipient SafePhrase app and messaging backend remain unapproved and outside this delivery plan.
- Proposed demonstration condition: application visible and screen awake. Agree explicit behavior on leaving the app or locking the screen before implementing Safety Mode. Background listening remains outside the supplied specification.
- Start with SMS, then add WhatsApp before final acceptance. An intermediate SMS-only milestone does not remove the WhatsApp requirement.
- Resolve missing-location behavior, contact selection, phrase language, and restart behavior at the tasks identified in the roadmap. Record agreed answers here; do not silently rewrite the reference specification.

## 2026-09-22 Calendar correction and messaging clarification

- The student downloads the repository on September 23; this is preparation only. Work starts Thursday September 24.
- Keep the two-week target: calls September 24, September 29, October 1, October 6, and final acceptance Thursday October 8, 2026. Confirm clock times for the mentor in Kyiv and the student in Bishkek.
- Internal offline-speech gate: September 28, after first device launch and before full voice integration. It is not an extra call.
- Final acceptance targets the functional pilot, including real voice activation. A UI-only prototype or Test SOS-only path is not equivalent. Unmet required behavior blocks a claim of full completion unless the mentor explicitly approves a scope change.
- Keep integration small: WhatsApp is the selected messenger, with SMS as the second channel and copy fallback. SMS may be implemented first to exercise the shared SOS flow; development order does not change final scope.
- Open the external composer with a selected recipient and SOS text; the user confirms sending. The recipient uses ordinary WhatsApp/SMS and does not install SafePhrase. No SafePhrase-to-SafePhrase messaging, SignalR, polling, backend, bots, Business API, or delivery tracking is required.
- Other messengers, including Telegram, are deferred rather than added to the two-week acceptance scope.

## 2026-09-27 Student request for behavior changes

- Recipient preference: prepare SOS for all saved trusted contacts. External composers still require the user to press Send; the app must not claim delivery. WhatsApp click-to-chat addresses one recipient per link, so the all-contact flow needs an explicit sequential/manual design before implementation.
- Missing-location preference: continue the SOS flow without a map link and clearly state that location is unavailable. This changes the reference specification's location-denial behavior and needs mentor review before acceptance evidence is finalized.
- Screen-lock/background preference: keep Safety Mode active until the student explicitly stops it, including while the screen is locked. This conflicts with `functional-specification.md` scope/NFR-03, which excludes continuous background listening, and requires mentor approval before implementation. Android microphone use in the background also requires additional platform work and has runtime restrictions; feasibility is not established by the one-session offline speech experiment.

## 2026-09-27 Student-directed workflow change

- Student explicitly asked the agent to perform remaining source/diff reviews, update task tracking, and continue implementation without waiting for file-by-file explanations. The agent may complete source-review tasks on the student's behalf, while recording who performed each action. Do not claim unreported phone checks or learning explanations as completed. Physical-device results remain student-reported evidence.

## 2026-09-23 Student kickoff information

- Student-selected initial test device: their current POCO C65. Installed Android version and on-device speech support remain unverified. This is a test-device choice, not a restriction to one phone model.
- Student-selected spoken phrase language: English. This does not independently select the UI or SOS message language.
- Student clarified with a screenshot that the installed editor is Visual Studio Code, not Visual Studio, and Visual Studio Installer is absent. The previous IDE report was a naming misunderstanding. Visual Studio Community with MAUI still needs manual installation. Student has some C++ experience from competitive programming; toolchain readiness remains unverified.
- Student reports about one hour of study daily, with up to 3-4 hours when free on weekends.
- City/timezone, call times, explicit confirmation of meeting dates, project name, and test-recipient WhatsApp availability remain pending. The approved delivery dates are unchanged.

## 2026-09-23 Manual project creation

- Superseding the earlier setup state: the student installed Visual Studio Community 2026 with MAUI and manually created `SafePhrase` under `src/SafePhrase/`. Screenshots show installer version 18.10.2, the MAUI template, and .NET 10 selected.
- Local files confirm `SafePhrase.csproj`, `SafePhrase.slnx`, namespace/title `SafePhrase`, Android target `net10.0-android`, and the default counter. The application ID remains the template placeholder `com.companyname.safephrase`; a distribution identity is not agreed yet.
- Opening the project is verified; successful restore/build, SDK version, Android deployment, and learning checks remain unverified. Keep the template for the first device run.

## 2026-09-27 WhatsApp recipient behavior

- Student clarified that SOS should be prepared for every saved trusted contact. WhatsApp click-to-chat supports one recipient per link, so SafePhrase opens one prefilled chat at a time and the student returns to open the next. The student presses Send in WhatsApp for each contact. SMS continues to use one composer addressed to all saved contacts. The app cannot confirm delivery.
- The student reported that the WhatsApp message was blank while SMS contained the message. The agent added encoded message text to the WhatsApp click-to-chat URL; phone verification remains pending.

## 2026-10-01 — user-authorized background listening extension

- The student explicitly requested background Safety Mode and instructed the agent not to wait for mentor approval. This supersedes the earlier approval prerequisite for implementation; it does not constitute device verification or mentor acceptance.
- This is an agreed extension beyond functional-specification.md NFR-03/out-of-scope. Keep the source transcription unchanged. Implement a user-started microphone foreground service with a visible notification and Stop action. Start it while the app is visible; no boot auto-start, hidden recording, or online fallback.
- Experimental en-US recognition restarts sessions after silence/nonmatching speech. Android documents SpeechRecognizer as unsuitable for continuous recognition; the POCO locked-screen experiment determines feasibility, battery impact and interruptions. Do not claim dependable continuous protection.
- Foreground phrase detection opens the existing SOS countdown. Background/locked-screen detection stops listening and posts a notification; tap/unlock/return opens the countdown. Android background-activity restrictions and manual sending remain in effect; no unattended WhatsApp/SMS delivery is implemented.
- Stop and service destruction invalidate callbacks. Fatal/repeated recognizer errors stop visibly. Removing the app from recent apps stops listening; process death does not automatically restart the microphone. A pending detection persists without audio or transcript storage.
- This scope increases October 8 delivery risk. Required acceptance checks remain open; dates are unchanged.

Sources: https://developer.android.com/develop/background-work/services/fgs/service-types ; https://developer.android.com/develop/background-work/services/fgs/restrictions-bg-start ; https://developer.android.com/guide/components/activities/secure-bal ; https://developer.android.com/reference/android/speech/SpeechRecognizer

## 2026-10-08 — background voice completion plan confirmed

- The student explicitly included Background Voice in the project and approved implementation of the completion plan. Retain user-started, on-device en-US recognition while minimized or locked, with the existing visible microphone notification and Stop action.
- The student selected notification-and-return for background detection: stop listening, retain the pending SOS, and open the five-second countdown after the app returns. Sending remains manual through SMS/WhatsApp. No background message sending or automatic activity launch is requested.
- The student selected stopping when SafePhrase is removed from recent apps. No microphone restart after process death or boot is added. Android/device feasibility and mentor acceptance remain separate from code completion.
- A pending detection is acknowledged only after successful countdown navigation; failed navigation can be retried from Home. The last recognized text is displayed in memory only and cleared on a new listening session. Audio and transcripts are not persisted or logged.
- Keep .NET MAUI/C#/XAML, one application project, and the existing session restart/error limits. Complete M3-1 first; M4 packaging and the student walkthrough follow milestone readiness. The October 6 checkpoint is overdue and October 8 delivery remains at risk; no dates are changed.

## 2026-10-12 — student-requested interface refresh

- The student explicitly requested a new interface using their supplied app-comparison image as a reference. Apply one dominant Safety Mode action, clear state, grouped settings, contact cards and a distinct manual SOS action. This authorizes the UI increment despite the prior deferral of optional polish.
- Keep the existing background notification-and-return flow, manual SMS/WhatsApp sending, contact CRUD and local persistence. No map, live contact status, route accompaniment, audio/video recording or additional delivery channel is requested.
- Use a light teal palette with system-dark support, native MAUI/XAML controls and existing fonts. Retain English UI and the student-written readiness label. Record this as M3-5, one bounded agent task; source specification and milestone due dates stay unchanged.
- The October 6 and October 8 targets have passed without recorded final acceptance. The UI increment does not establish device readiness, learning completion or mentor acceptance. Design rationale is in ui-design.md.

## 2026-10-12 — student requested less UI density

- Student described the initial refresh as overwhelming. Revise the existing M3-5 interface into a flat main screen with only the principal action/status, Settings, manual SOS and Details. Saved setup/contact/recognition information remains available through Details.
- Remove marketing/numbered headings, duplicate readiness text and large section wrappers. Use compact settings fields/contact cards and a plain countdown; preserve pinned cancellation and manual-send explanation.
- This supersedes the initial presentation choices, including retaining the extra readiness subtitle. It changes no background, contact storage or message behavior and is not evidence of device or mentor acceptance.

## 2026-10-13 — integrated source review and action clarity

- Student explicitly authorized the listening-to-SOS-to-message code review/fixes and a more pleasant, understandable interface. Complete M3-4, preserving the compact M3-5 layout and earlier uncommitted work.
- Lock settings during startup/listening/pending/open SOS; stop before changing the service's phrase/recipient snapshot. Separate startup ownership and recognizer readiness; stale commands/callbacks cannot affect a newer start.
- Request location consent before the five-second timer. Denial continues without coordinates; Cancel/navigation invalidate pending preparation/fallback. Manual sending and experimental background behavior remain.
- Use native SVG action icons and short accessible labels, a circular action and clear Stop/SOS colors. No backend/framework/channel added. Dates and device/student/mentor gates unchanged.

## 2026-10-13 — student-requested lock-screen voice confirmation

- Student authorized waking/opening the application after a background trigger and offering Cancel or continuation. Add M3-6 using permission-gated Android full-screen notification and a small native C# activity. This replaces notification-only handling where Android permits it; otherwise retain notification fallback.
- The lock-screen view is a generic confirmation. Continue requires normal unlock before the existing countdown and manual SMS/WhatsApp; Cancel and Back discard only the matching detection. Persist a random detection ID beside the flag to reject stale intents, never speech/audio.
- Add an explicit full-screen special-access route to Settings. Respect user notification/permission controls; no direct background activity launch, overlay or credential bypass. Android may show heads-up instead when unlocked. Full-screen Play eligibility and phone wake behavior are not established by code generation.
- Keep .NET MAUI/C#/XAML, existing foreground flow, experimental en-US recognition, Stop/recents rules and manual sending. Dates remain overdue and unchanged. Details/evidence: lock-screen-sos.md.

## 2026-10-13 — guided permission offer

- Student asked SafePhrase to explain and propose needed permissions instead of making the user find settings. Add the missing full-screen access offer to Start; microphone/notification runtime requests remain in that same startup flow.
- Allow opens the precise Android special-access page, waits for return and rechecks permission. Already-granted access skips the offer; denial keeps an explicit notification-only/Cancel choice. Android still requires the user's system confirmation; the app does not grant its own access.
- Preserve startup ownership and Stop/foreground guards, and reuse the same permission intent from Settings. This refines M3-6 without new dates or device/acceptance claims.

## 2026-10-14 — direct SMS after background confirmation

- Student clarified that pressing the background SOS confirmation should send the SOS immediately, without opening a composer for a second Send tap. Implement permission-gated Android SmsManager sending to saved trusted contacts after explicit confirmation and ordinary unlock. Label the action Send SOS SMS.
- If SMS permission is denied/unavailable, retain the manual message-app route. Foreground/manual channel selection and WhatsApp remain unchanged. A confirmed carrier attempt cannot be canceled after dispatch; Cancel remains available before confirmation.
- Consume the detection with a synchronous private claim before dispatch; never retry an unknown/partial outcome automatically. Sent callbacks do not prove recipient delivery. Record SMS charges, default SIM, platform/store policy, and no phone evidence separately. See confirmed-sms.md.
- No message was sent, physical-device behavior is unverified, and dates/mentor acceptance remain unchanged.
