# SafePhrase

SafePhrase is an educational Android app that listens for a saved English phrase and helps prepare an SOS message for trusted contacts. Messages can include a Google Maps location link and a timestamp. The recipient does not need to install SafePhrase.

## Download and install

1. Open [GitHub Releases](https://github.com/ansarsaduakas492-ops/SafeAPP/releases) and choose the newest APK release. Under **Assets**, download **SafePhrase-1.0.apk** to your Android phone. The current candidate is [v1.0.0-rc.1](https://github.com/ansarsaduakas492-ops/SafeAPP/releases/tag/v1.0.0-rc.1). You can also copy the supplied local APK to the phone using USB or a file transfer service. If you receive a ZIP bundle, extract it first. The APK is the installer; GitHub's **Source code** ZIP is not an Android app.
2. Open the APK on the phone using Files or your download notification. If Android asks, allow **Install unknown apps** for the browser or file manager you are using, then tap **Install**. Menu names vary by phone. You can turn this permission off after installation. Do not disable Play Protect.
3. Open **SafePhrase** from the app list. Visual Studio, a computer connection, and a SafePhrase account are not required to use the installed app.

Generated APKs are distributed as release assets and excluded from Git; downloading the repository alone does not include the installer. There is no Play Store listing. If GitHub requires access to this repository, sign in with an authorized account.

The candidate uses a local **test signing key**. If Android reports a signature conflict with an existing SafePhrase installation, use a build signed with the same key, or uninstall the old app before installing this one. Uninstalling removes saved settings and contacts; record what you need first. Do not uninstall solely because a generic installation error appears: check free storage and the full APK download first.

## What you need

- A 64-bit Android phone: this APK includes `arm64-v8a` and `x86_64`, not 32-bit ABIs. The package minimum is Android 5.0/API 21, but **voice Safety Mode requires Android 12/API 31 or later**, a compatible on-device speech service, and its English (US) model. The in-app model check/download requires Android 13/API 33 or later. Installation on an older phone does not establish voice compatibility.
- Microphone access for voice recognition, and notifications for listening status and background alerts.
- WhatsApp with an active account and internet access to use WhatsApp messages; or an SMS app, an active SIM, and mobile SMS service for SMS. Carrier charges may apply.
- Optional location permission and enabled device location for a map link. If location cannot be obtained, the message says it is unavailable.

## First setup

1. Open **Settings**. Enter an English **Safe phrase**, for example `please help me now`. Edit the **SOS message**, choose **WhatsApp** or **SMS**, and tap **Save settings**. The channel choice applies to the manual/foreground SOS flow.
2. In **Add contact**, enter a trusted person's name (optional) and their full international phone number, including `+` and the country code. Tap **Add contact**. Use **Edit** or **Delete** beside an existing contact to change it. Add at least one contact and agree on any test message with that person first.
3. Return Home, open **Details → Recognition setup**, and tap **Check / prepare English offline model**. If a download is requested, keep internet access on and check again after it finishes. Tap **Listen once in English** and say a phrase to check recognition. This setup screen does not trigger SOS.

## Using Safety Mode

Tap **Start** on Home while the app is visible. Follow the microphone and notification permission prompts. The app can also offer SMS permission and full-screen alert access. Read the visible status: wait until it reports that it is listening. Speak your saved phrase as a complete utterance; extra words can prevent a match. Matching ignores capitalization, punctuation, and repeated spaces; it does not use fuzzy matching.

Tap **Stop** on Home or in the microphone notification to end listening. Removing the app from recent apps also stops listening. After an error, process termination, or restart, check the status and explicitly start again. Background listening is experimental and can be interrupted by Android or phone settings.

### Foreground or manual SOS

Use **Prepare SOS** to try the message flow without voice recognition, or say the phrase while Safety Mode is listening in the foreground.

- Answer the location prompt. The five-second countdown starts after that prompt. Tap **Cancel SOS** to cancel, or **Open message now** to skip the remaining countdown.
- When the countdown ends, SafePhrase opens the selected channel with prepared text. **Review the recipients and tap Send yourself in WhatsApp or the SMS app.** Opening a composer does not send a message.
- WhatsApp opens one saved contact at a time. Return to SafePhrase and tap **Next WhatsApp contact** for another contact; confirm each send separately. The SMS composer is given the saved recipient list; check it before sending.
- If a channel cannot open, the app can offer **Open SMS for all contacts** or **Copy SOS message**, depending on availability. Paste copied text into your chosen app and send it yourself.

### Background or locked-screen SOS

A detected phrase can show an SOS notification or a lock-screen confirmation. Full-screen appearance depends on Android permission and phone settings. If no full-screen prompt appears, tap the SOS notification. The phone may require your normal unlock.

- **Cancel SOS** cancels that alert.
- **Send SOS SMS**, followed by the required unlock, attempts to send SMS directly to the saved contacts when SMS permission is granted. **There is no second Send tap in the SMS app in this path, even if WhatsApp is selected in Settings.**
- If SMS permission is unavailable, use the offered manual message-app route and confirm sending there.
- A submission status is not proof that a recipient received or read the message. An uncertain or partial SMS result is not retried automatically. Check before starting a new SOS to avoid duplicate messages.

## A first check (5–10 minutes)

Use a consenting test contact. Save settings, close/reopen the app to check persistence, then use **Prepare SOS → Cancel SOS**. Repeat and inspect the prepared text and recipients without sending. Next, use **Recognition setup** to check your English phrase. Perform a deliberate send only after agreeing with the recipient, and ask them whether it arrived. Test locked-screen/direct SMS separately because it can send immediately after confirmation.

The packaged APK still needs an installation and full scenario check on the actual phone. A successful build and logic checks do not establish microphone, lock-screen, SIM, carrier, or delivery behavior.

## Troubleshooting and privacy

| Problem | What to check |
| --- | --- |
| Start or Prepare SOS is unavailable | Save a phrase containing words and at least one valid contact; return Home. |
| Recognition is unavailable or misses the phrase | Check Android/API support, the installed English (US) model, microphone permission, and the recognized text in Details. Use the exact phrase and restart Safety Mode after resolving an error. |
| No background alert | Check notification and full-screen alert access in Settings. Restart Safety Mode while the app is visible. Device power management can interrupt the service. |
| No map link | Allow location and enable device location. A missing location does not create invented coordinates. |
| No outgoing message | In the manual flow, tap Send in the other app. For direct SMS, check permission, SIM/default SMS subscription, carrier service, and the reported result. |

Settings and contacts are stored locally. The app uses Android's on-device recognition API, has no app backend, and does not intentionally store audio recordings or upload audio. The English model may need an initial download. Phone backups and the speech provider are controlled by Android; the app declares `allowBackup=true`. Opening a message composer hands the prepared text/location to another app. The WhatsApp `wa.me` link includes that data in its URL and can pass it to the external service before you press Send; sending to the contact still requires confirmation. Direct SMS passes the message to Android/carrier after SOS confirmation. **Copy SOS message** puts the text on the system clipboard. Offline recognition does not mean offline WhatsApp delivery. The current voice language is English (US); Russian and Kazakh recognition are not implemented.

## Build and project evidence

See [APK build and signing notes](documents/apk-build.md) for reproduction commands and the candidate configuration, and [verification](documents/verification.md) for requirement coverage and remaining phone checks. The project uses C#, XAML, and .NET MAUI. Application code is in `src/`; project records are in `documents/`.
