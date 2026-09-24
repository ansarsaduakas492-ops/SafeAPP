# Progress log

Append compact entries; do not paste transcripts or full command output. Include date, task ID, actor, result, evidence, review, learning/manual status, and next action. Keep failed attempts only when they explain an unresolved issue or a decision.

## 2026-09-22 — planning baseline

- Actor: mentor and planning agent.
- Result: MAUI selected; two-week teaching workflow, Tuesday/Thursday checkpoints, early speech gate, student setup guide, and repository handoff prepared.
- Evidence: planning documents only. Application source has not been scaffolded; device and student learning checks have not occurred.
- Schedule: dates and student availability await kickoff confirmation. No delivery milestone is accepted.
- Next: M0-1, kickoff constraints and student-owned setup.
- Planning review: focused independent review identified ambiguous speech-session restart wording and a final acceptance dependency; both were corrected. Setup paths were made portable to the student's checkout.
- Planning validation: generator validated 6 milestones and 22 tasks; browser checks confirmed milestone selection, student-task filtering, expanded acceptance details, and desktop rendering. These are roadmap checks, not application or student acceptance checks.

## 2026-09-22 — calendar and messaging clarification

- Mentor correction: download September 23, begin work September 24, target final review October 8. Calls remain Tuesday/Thursday; offline speech gate moved to September 28.
- Updated current calendar, handoff, roadmap, and channel acceptance checks. WhatsApp plus SMS/copy fallback; manual sending in external apps, no recipient SafePhrase app or messaging backend. Other messengers deferred.
- These are planning changes only; delivery tasks remain pending. Final acceptance still requires the functional pilot, including real voice activation.

## 2026-09-23 — local repository preparation

- Task: preparation for M0-1; actor: coordinating agent, at the student's request to follow the mentor's documents.
- Result: fetched `origin/main` at `0737d02` into the empty local repository and checked out tracking branch `main`; no commit or push.
- Evidence: clean checkout before documentation updates; `src/` contains only `.gitkeep`. Read the handoff, delivery plan, and M0/M1 tasks.
- Manual/learning status: no student task, SDK/device check, or mentor acceptance is complete. Requested kickoff details: phone/Android version, speech language, availability, city/call times, IDE setup, and programming experience.
- Review: local review of preparation and tracking changes only; no application behavior exists to test.
- Forecast: `not_assessed` until availability and device are confirmed. Keep the approved September 24 start and October 8 target.
- Next: record the student's answers for M0-1, then guide naming and manual setup in small steps.

## 2026-09-23 — M0-1 partial student answers

- Actors: student report and coordinating agent. Recorded POCO C65, English speech, Visual Studio without MAUI, introductory competitive-programming C++, one hour daily and up to 3-4 hours on free weekends in `project-decisions.md`.
- M0-1 is doing, not done: Android version, city/timezone, meeting dates/times, and mentor confirmation remain pending. No device or learning result is inferred.
- Local IDE discovery: standard `vswhere.exe` path was not found; installed IDE version remains unverified. Microsoft installation guidance supports adding the MAUI workload through Visual Studio Installer with default optional components.
- Review: local review of tracking changes; source specification unchanged. Forecast remains `not_assessed`; delivery dates remain unchanged.
- Next: student adds the MAUI workload and reports the result and installed Android version; then verify tooling and finish kickoff/naming before creating the application.

## 2026-09-23 — IDE identification correction

- Student clarified that only Visual Studio Code is installed and Visual Studio Installer is absent; supplied screenshot shows Visual Studio Code. This supersedes the earlier report of Visual Studio without MAUI.
- Corrected decisions, roadmap evidence, and handoff. Toolchain setup is still incomplete; no student checkpoint is marked done.
- Next: student downloads and runs Visual Studio Community's official installer and reports reaching workload selection. Continue with at most three manual steps at a time.

## 2026-09-23 — student created the MAUI template

- Tasks: M0-3 and M1-1, both doing. Student chose SafePhrase, installed Visual Studio/MAUI, and manually created the .NET 10 project in the repository.
- Evidence: student screenshots and direct local inspection of `src/SafePhrase/SafePhrase.csproj`, solution, and counter code. Project is open with Windows Machine selected; Android build/deployment has not been observed.
- Review: read-only source inspection; no application code changes or builds. Opening the IDE does not establish restore, build, device, or learning completion.
- Updated tracking and regenerated the map. Forecast remains not_assessed pending first-device readiness; remaining M0 questions are open.
- Next: ask for installed Android and MIUI/HyperOS versions, then guide phone debugging and the first counter run in small steps.

## 2026-09-23 — M1-1 deployment restriction

- Evidence: student-provided Visual Studio log confirms successful Android build and signed debug APK; deployment fails with ADB0010 / INSTALL_FAILED_USER_RESTRICTED: Install canceled by user. No application launch is verified.
- Student screenshots confirm Android 15/API 35, OS 2.0.208.0, USB debugging enabled, and Install via USB disabled. The student reports authorizing the computer's USB debugging request.
- Next: student enables Install via USB, retries from Visual Studio, and confirms any SafePhrase installation prompt. Diagnose further only if the retry fails. No source-code change is indicated; no raw log or unique device identifier is recorded.

## 2026-09-24 — first physical Android run

- M1-1 done from student evidence: a screenshot shows the default SafePhrase MAUI template open on POCO C65 with `Clicked 25 times`. The student also reported successful installation. This verifies launch and counter interaction on Android 15/API 35; it does not verify future SOS behavior.
- M0-2 done: agent explained the starter structure in chat and recorded kickoff choices separately from the source specification. Local review confirms the existing .gitignore excludes generated bin/obj output; no additional ignore change is needed yet.
- M0-1 remains doing until city/timezone and call schedule are known; M0-3 remains doing until the student explains project name versus app title. Mentor acceptance remains pending.
- Forecast: not_assessed until the September 24 checkpoint time is known. Next: one manual label edit and teach-back; then agent Home/Settings increment after the M0 gate.

## 2026-09-24 — label learning check and Home/Settings increment

- M1-3 partial student evidence: `SafePhrase is ready` appeared on the phone after their XAML edit; the counter behavior remained unchanged. The student did not recognize the hidden `.csproj` file, so the agent explained that the IDE's project node corresponds to build settings and `ApplicationTitle` is the phone-facing title. M0-3 remains open until their explanation is heard.
- M1-2 doing: agent kept the student's heading, replaced the counter template with a Home page and added Settings with local phrase/message/channel persistence plus add/edit/delete for trusted contacts. The Safety Mode button is disabled until its behavior is implemented.
- Android Compile target passes with zero warnings/errors. Full package build failed twice with XALNS7024 because a shared `obj` assembly is locked by another process. Phone check of the new screens is pending; no settings or SOS acceptance is claimed.
- Local review: existing ignore rules cover `bin/obj`; no new dependency, backend, or package was added. Next: student launches the updated app from Visual Studio and checks settings persistence with test data, then reports results without sharing a private phone number.

## 2026-09-24 — Settings screen phone check

- Student reports the updated app works on the phone, settings can be saved, and contacts can be added. This confirms an initial physical-device check for Home/Settings and contact creation.
- Contact edit/delete and saved-value persistence after closing and reopening the app remain unverified. Do not mark M1-2 or M1-3 complete yet.
- The student briefly saw the old counter XAML while the code-behind already expected Settings controls. Local inspection found the mismatch; the agent restored the intended Home XAML, preserving `SafePhrase is ready`. `git diff --check` passes.
- Next: student adds a harmless test contact, edits it, deletes it, closes and reopens the app, then reports which saved settings remain. Do not share a real contact number.

## 2026-09-24 — settings confirmed and offline speech experiment prepared

- Student reports contact add/edit/delete and settings persistence across restart all work on the phone. M1-2 is recorded done from source and student-reported device evidence; mentor acceptance remains pending. M1-3 remains open for the student's Git diff review and explanation.
- Added an Android-only speech experiment page that explicitly uses Android's on-device recognizer, checks service availability, asks for microphone permission only after the student taps Listen, runs one English (US) recognition session, displays results/errors, supports cancellation, and destroys the recognizer on navigation away. No audio recording or message sending is implemented.
- Added the Android microphone permission and the recognition-service package visibility query. Official Android docs say `createOnDeviceSpeechRecognizer` is API 31+ and fails if the service is unavailable; API 33+ can additionally check/download language models. Our experiment will establish actual en-US support by running the phone test, including with network disabled.
- Android Compile target succeeds with zero warnings/errors; `git diff --check` passes. S1-1 done; S1-2 is now the active student task. Next: run the updated app and test availability, microphone permission, first/repeated en-US recognition, silence/end-of-speech, Stop, and offline operation.
- First phone speech result: student reports `LanguageUnavailable` for en-US through the explicit on-device recognizer. Android documents this result as a supported language that is currently unavailable, for example because its model has not been downloaded. Offline recognition has not passed yet; next the student checks/downloads English on-device speech in phone settings and retries.
- Settings follow-up: screenshots showed that HyperOS search opens Google text-to-speech and Assistant voice preferences, not an offline model manager. The app now queries Android 13+ for installed/downloadable en-US models and requests a model download through the system recognizer when available. Android Compile target passes with zero warnings/errors. Next: student runs the updated app, taps Check / prepare English offline model, follows any system prompt, and reports the status.
- Student now reports that English speech recognition works in the app. This verifies a first successful one-session recognition on the physical phone through the explicit on-device recognizer. Network-offline operation, repeated sessions, silence/unrelated speech, and Stop are still unverified; the speech gate remains active.
