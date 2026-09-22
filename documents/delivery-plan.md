# Guided two-week delivery

## Outcome and scope

Deliver an understandable educational Android APK using .NET MAUI, C#, and XAML. The student should be able to run it, explain its main flow, change a small behavior, and describe its limitations. Codex writes most code; learning and physical-device checks remain explicit work.

The reference is `functional-specification.md`; later agreed decisions are in `project-decisions.md`. The structured source of milestones, dates, dependencies, ownership, acceptance, and current status is `roadmap.json`. `roadmap.html` is a generated, offline, read-only projection. Do not edit it directly or maintain a second independent checklist.

Approved calendar: download September 23; project work September 24 through October 8, 2026. Calls are September 24/29 and October 1/6/8. Confirm call times across Europe/Kyiv and Asia/Bishkek. The September 28 speech gate is an internal checkpoint, not an extra call. Assume roughly 60-90 minutes of student participation on most working days, plus setup time as needed; confirm availability before treating this as a forecast. Agent speed does not eliminate device testing or learning time. If the kickoff moves, explicitly rebaseline all dates while preserving the Tuesday/Thursday rhythm.

## Session loop

1. Read `handoff.md`, inspect `git status --short`, and retrieve only the active milestone and its dependencies. Read this plan once per fresh workflow and the relevant specification sections, not every document on every turn.
2. State one small outcome, identify agent/student ownership, and check unresolved decisions that affect this task. Continue with safe independent work while awaiting essential answers.
3. Implement one coherent increment. For student-owned steps, explain at most three actions at a time and wait for evidence. Never replace manual setup with silent scaffolding.
4. Validate the relevant behavior and review the diff. Fix material findings. Use a focused independent reviewer for the integrated SOS state flow and final candidate; a local review is enough for small UI changes. If delegation is unavailable, document that review was local.
5. Explain what changed / how it works / why / how to check, using ordinary language. Link up to three relevant files. Ask one short question requiring the student to explain a concept or predict behavior; give a small manual activity, then wait for their answer before recording learning completion. A missing learning check does not prevent unrelated implementation.
6. Update task status and evidence, the short work log, and the handoff. Rebuild the map. State the next task and any schedule risk. At a natural boundary, recommend a fresh chat with the supplied restart prompt.

## A small architecture

- Start with one MAUI application project under `src/<StudentChosenName>/`. Do not create separate domain/application/infrastructure projects.
- Use the existing four logical screens: Home, Settings, Safety Mode, and Countdown. Small page code-behind is acceptable initially. Extract a modest view model only when growing state or binding warrants it; do not introduce MVVM everywhere solely as a ritual.
- Use a small settings model and local persistence. A JSON contact list in Preferences is sufficient initially; no database is needed just to store a few settings.
- Extract speech/device integration and SOS state coordination when implemented. Keep message formatting and phrase normalization as plain functions that are easy to explain and test.
- Keep external operations asynchronous; explain `async`/`await` when first used. Cancel pending work on Stop/Cancel and prevent duplicate SOS triggers. Avoid blocking UI calls and swallowed errors. Dispose/unsubscribe when appropriate.
- Use built-in dependency injection if useful for these few services, not a service locator or an extra DI framework. Introduce interfaces only at boundaries that need substitution or testing.
- Prefer maintained built-in APIs and the MAUI Community Toolkit where they satisfy requirements. Verify current stable supported tooling at setup and pin chosen SDK/package versions once verified. Do not adopt previews for this pilot by default.
- Teach names in context: PascalCase for types/public members and camelCase for local variables; clear names over abbreviations. Explain the distinction between a project name, namespace, Android application ID, and visible app title during manual setup.

## The early speech gate

Run a minimal device experiment by September 28 before investing in voice integration. Test on-device recognition for the agreed language; record phone model, Android/API version, package version, language/model availability, and reproducible outcomes without identifiers or audio recordings. Test a silence interval, unrelated speech followed by the phrase, repeated sessions, Stop, and denied microphone permission. Verify offline operation with network disabled after any model provisioning, and inspect the selected API/configuration for online fallback. An airplane-mode success alone is not proof of all privacy behavior.

The Toolkit documents `OfflineSpeechToText` as requiring Android API 33+. Android's platform on-device recognizer starts at API 31, but availability is device-dependent. The standard recognizer is session-oriented and is not intended for continuous recognition. Distinguish a short functioning experiment from reliable ongoing Safety Mode.

If the gate fails, record exact evidence and timebox diagnosis to one focused session. Consider a compatible test device or a small Android C# adapter before a large library change. Give the mentor options and their time/privacy tradeoffs. Do not silently switch to online recognition, remove voice, or claim a Test SOS button satisfies voice acceptance. The manual SOS path can progress independently while the voice decision is pending.

## Decisions to resolve during implementation

- Kickoff: available time, physical Android device/API, spoken language, selected project name, and WhatsApp availability for the test recipient. Do not introduce recipient-app messaging by inference.
- Speech gate: normal recognition completion after silence or unrelated speech must preserve Safety Mode and resume listening safely, as required by the reference flow. Decide how to restart sessions and handle repeated recognition errors. Separately, the proposed pilot rule for leaving the foreground, locking the screen, or a call interrupting the microphone is to stop Safety Mode and clearly require a restart on return. Confirm and record lifecycle behavior before implementation.
- SOS flow: define what happens if location is denied, unavailable, or arrives after the countdown. Never invent coordinates or show a message as sent merely because a composer opened.
- Contacts/channels: define recipient selection for multiple contacts and WhatsApp. Do not imply one link sends separate messages to all saved contacts. Multiple-contact CRUD remains in scope.
- Phrase matching: use normalized exact comparison first, as described in the specification. The optional 80% similarity enhancement is deferred unless time and explicit agreement justify it.

## Validation proportional to the change

### Minimal messaging integration

WhatsApp is the selected messenger; retain SMS as the second channel and the copy fallback. The flow is: SafePhrase detects the phrase, offers cancellation, prepares location/time/text, opens an external composer, and the user presses Send. The recipient needs only that channel, not SafePhrase. No bot, Business API credentials, server, polling, or SignalR is needed for this handoff.

For M3-2, use the official WhatsApp click-to-chat format `https://wa.me/<international-number>?text=<encoded-text>`. Convert a validated international phone number to digits without the plus sign, spaces, brackets, or hyphens; URL-encode message text. Test the agreed contact-selection behavior. Do not assume a link broadcasts separately to all contacts or that a successfully opened link proves delivery. Show an explicit SMS/copy alternative if WhatsApp is unavailable or the handoff cannot be completed; do not silently switch channels.

Device checks include a test +996 contact, the agreed message language (including Cyrillic if selected), line breaks, map link, timestamp, manual confirmation, cancellation before opening, and app/account availability. Coordinate one deliberate test send to a consenting test recipient and have the student report receipt separately from composer-opening success. Do not record private phone numbers or exact personal locations in repository evidence. WhatsApp messaging needs connectivity; offline speech does not imply offline message delivery. Confirm speech language independently rather than assuming Kyrgyz recognition is available on the phone.

Telegram is a possible later addition, not a requirement for this pilot. The regional rationale is supported by the [ENC March 2023 report, chart 12](https://encouncil.org/wp-content/uploads/2023/05/2023-03-ENC-Report-RUS-V5.pdf), which records WhatsApp and Telegram use in Kyrgyzstan; this is historical evidence, not a current market-share claim. Integration behavior follows the [official WhatsApp click-to-chat guide](https://faq.whatsapp.com/5913398998672934).

### Checks and packaging

Build after a coherent change; avoid repeated builds without new evidence. Add a small test project only when meaningful pure logic is available. Cover normalization, message content/time/map link, and state transitions for cancellation and duplicate triggers. Do not add tests mirroring trivial property assignments.

Physical Android checks are mandatory for speech, microphone/location permissions, external message composition, and the final APK. An emulator or successful build is not substitute evidence. Device testing may be performed by the student; record their reported result explicitly rather than claiming direct observation.

Before final acceptance, verify AC-01 through AC-06 and FR-01 through FR-20, plus the four screens, Stop, Send Now, multiple-contact editing/deletion, the copy fallback, and audio privacy. Record relevant checks in milestone evidence or a compact `documents/verification.md` created when testing starts. Use test contacts; the student deliberately confirms any real outgoing message.

An APK candidate must install and run on the chosen phone from the packaged output. Explain the build configuration and test signing; keep signing secrets, private numbers, precise personal locations, generated builds, `.vs`, `bin`, and `obj` out of Git. Publishing to an app store is not part of the plan.

## Progress, ownership, and review

Task statuses: `pending`, `doing`, `blocked`, `done`. Every done task requires at least one concise evidence item. Prefix student reports as such. A cancelled or deferred requirement remains visible and needs a recorded mentor decision; it is not marked done.

Milestone statuses: `planned`, `active`, `blocked`, `ready_for_review`, `accepted`. `ready_for_review` requires all tasks done, acceptance evidence, and a recorded code review. `accepted` additionally requires a dated mentor acceptance note (the mentor can accept the internal speech gate asynchronously). Agent completion never implies mentor acceptance.

Dependencies require the prior milestone to be technically ready (`ready_for_review` or `accepted`), unless the task explicitly permits independent progress. Mentor acceptance can follow at the next call. S1 speech experimentation may proceed after the first device launch while M1 settings work finishes. M2 can proceed independently of S1; voice integration in M3 requires S1 and M2 technically ready.

At session end, record forecast as `not_assessed`, `on_track`, `at_risk`, or `late`, plus a short reason and next action. Compare outstanding evidence to the next checkpoint, not generated line counts or agent promises. A missed target is reported immediately on the next active session. Agents do not monitor the clock between sessions and no automation is implied.

If behind: first drop optional similarity matching, visual polish, extra platforms, and speculative architecture. Split remaining work into smaller verifiable increments. Required speech, cancellation, privacy, and message behavior cannot be silently removed to meet the date; ask the mentor to rebaseline scope or time with concrete alternatives.

Call format (10-15 minutes): student runs the agreed checkpoint, explains one code path in their own words, shows one manual change, then discusses blockers and the next target. Do not spend the call reading generated code line by line.

## Context and delegation protocol

Keep one active task per chat where practical. Delegate a genuine independent component or focused review only if this avoids substantial context or improves verification. A scoped brief contains: outcome, owned paths, necessary references, constraints, checks, and expected summary. Request fresh context; no recursive delegation. Do not give two agents ownership of the same files. The coordinator owns tracking and integration.

Handoff content: current task, last verified result, files to read next, relevant commands, incomplete manual checks, blocker/decision, and next action. Target roughly 150-250 words; keep old history in the work log. Never paste a whole conversation into a new chat or into a subagent brief.

Recommend a fresh chat after a milestone or lengthy detour, or based on actual context telemetry. Without telemetry, say that the conversation has accumulated unrelated history, not that a specific token budget has been consumed. Token economy must not suppress necessary explanations or verification. Subagents consume their own tokens and are not automatically cheaper.

## Sources checked for this plan

- [MAUI speech and offline recognition](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/essentials/speech-to-text)
- [Android SpeechRecognizer limitations and on-device APIs](https://developer.android.com/reference/android/speech/SpeechRecognizer)
- [MAUI SMS composition](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/communication/sms?view=net-maui-10.0)
- [Codex repository instructions](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Codex subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents)

The fresh-context briefs, handoff length, teaching loop, and schedule are project conventions, not guarantees about token savings or product behavior.
