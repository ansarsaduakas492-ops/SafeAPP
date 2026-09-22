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
