# SafePhrase SOS App

An educational pilot project for a voice-triggered personal safety application.

- [Functional specification](documents/functional-specification.md)
- [Project decisions](documents/project-decisions.md)
- `documents/`: specifications, analyses, plans, and other project documents.
- `src/`: application source code.

The approved stack is **.NET MAUI, C#, and XAML**, targeting Android APK installation.

## Start here

1. Open this repository in Codex. Its root `AGENTS.md` defines the teaching and token-economy workflow.
2. Open [the interactive milestone map](documents/roadmap.html) in a browser. Select a milestone to see student activities, agent work, evidence, and checkpoint questions. It works offline.
3. Follow [student setup](documents/student-setup.md) to install the tools and create the initial project manually. No application has been scaffolded yet.
4. Agents resume from [the handoff](documents/handoff.md), using [the delivery plan](documents/delivery-plan.md) and [roadmap data](documents/roadmap.json).

Download on September 23; project work runs from September 24 to October 8, 2026, with Tuesday/Thursday calls. Confirm call times across Kyiv and Bishkek at kickoff. The map shows recorded evidence, not automatic completion; browser interaction does not modify repository progress.

The final target includes real voice activation, cancellation, location/time, and prepared messages in WhatsApp or SMS. The user confirms sending in that external application; the recipient does not need SafePhrase. No messaging backend or bot is planned.

Progress is recorded in [the work log](documents/progress-log.md). After editing roadmap data, rebuild its browser view from the repository root:

```powershell
powershell -NoProfile -File documents/tools/Build-Roadmap.ps1
```
