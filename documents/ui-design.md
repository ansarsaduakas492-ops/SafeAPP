# SafePhrase interface refresh

Requested: 2026-10-12. This is a presentation update of existing behavior, not a new messaging or tracking system.

## Reference and choices

The student's supplied image is a comparison table, not an app screenshot. Apply its principles: one dominant action (Noonlight), organized settings (Google Personal Safety), contact cards (Life360), and distinct help actions (bSafe). No external product features or visual assets are imported.

- Use a light canvas, teal primary action and a restrained red SOS accent, with corresponding system-dark colors. Retain English UI. After the student's density feedback, omit the extra readiness subtitle and marketing headings.
- Keep styling in a small MAUI resource dictionary. Reuse Open Sans and native XAML controls; no new packages, bitmap assets or UI frameworks.
- Use scrollable pages, wrapping text, minimum 44-unit action targets and contact initials without claiming live location or online status.

## Screen behavior

- Home: a flat screen with one central Start/Stop/Open SOS action, short heading, service status and manual SOS footer. Show missing-setup instructions only when needed. Move phrase/channel/contact summary, recognition preview and preparation into a plain Details disclosure. No summary cards, contact names, repeated badges or marketing copy on the default view.
- Settings: use compact field labels for phrase, SOS text and channel, followed by contacts and the add/edit form. Remove numbered headings, hero text and large section cards. Contact cards use reduced padding and actions with at least 44-unit targets. Editing still scrolls to the form; validation, persistence and deletion confirmation remain.
- SOS: display the remaining seconds in a small plain dial and label the immediate action "Open message now." Keep Cancel and message actions fixed below scrolling content; move manual-send explanation into the content. Hide the dial when preparation begins or finishes. Preserve sequential WhatsApp contacts, SMS/copy alternatives and manual sending.
- Recognition setup: apply the same visual language to the existing one-session helper without changing its recognition logic.

## Evidence boundary

Android compilation and source review can confirm XAML/event wiring, not the rendered layout on the POCO or Android background behavior. Student device/learning and mentor acceptance remain separate; delivery dates are unchanged.

The final ordinary Debug build uses fast deployment: view the updated UI by running from Visual Studio. A standalone embedded-assembly APK attempt failed on AAPT2 pipe closure, and elevated retry is unavailable due to the approval-review usage limit. Do not treat the existing signed APK as proof of the latest self-contained package.

## 2026-10-12 simplification

Student described the first refresh as overwhelming and requested a change. The compact revision supersedes its decorative card-heavy presentation without changing the existing SOS or background behavior. Final Compile evidence and the remaining device/packaging boundaries are recorded separately in the roadmap/handoff.

## 2026-10-13 action clarity

The student requested more pleasant, immediately understandable controls alongside the integrated flow review. Keep the compact hierarchy; add native SVG microphone/stop/SOS/message/settings/cancel icons, a circular action surround and outlined countdown. The microphone is teal, Stop and SOS use stronger red, and startup/restart has a small progress indicator. Icons retain short labels and accessible descriptions; decorative overlays pass taps to actual buttons. Details remains collapsed. No new feature, dependency or bitmap asset was added.

Final full Android Debug build passed after rebuilding damaged AAPT2 intermediates outside the sandbox. The older packaging failure above is historical; standalone APK and device appearance acceptance remain separate M4/student work.

## 2026-10-13 lock-screen confirmation

Student requested immediate Cancel/Continue after a background phrase. A small native Android screen uses the same light/teal/red palette, large SOS symbol, two fixed buttons and scrolling generic explanation. It wakes/shows over keyguard only through the platform's full-screen notification mechanism; recipient and message details require unlock. Back cancels. Settings adds one compact special-access section, without Home density changes. Platform behavior and outstanding phone evidence are recorded in lock-screen-sos.md.
