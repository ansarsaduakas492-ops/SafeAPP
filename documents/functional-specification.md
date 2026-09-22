# Functional Specification

> Source: Functional Specification - SafePhrase SOS App (4).docx. Converted to Markdown on 2026-09-22. Source wording and requirements are preserved; formatting is adapted for Markdown. Technology suggestions below are not an approved project decision.

Project: SafePhrase SOS App — MVP

## 1. Purpose

SafePhrase is a personal safety application that allows a user to trigger an SOS alert using a predefined voice phrase. The application is designed for situations where the user may not be able to manually call or text someone.

The MVP will focus on a simple working prototype that can be demonstrated by a high school student.

## 2. Target Users

The application can be used by:

- teenagers walking alone
- women in unsafe situations
- elderly people
- any vulnerable person who may need quick assistance

## 3. MVP Scope

The MVP must include:

1. User can set a personal safe phrase.
2. User can add trusted contact phone numbers.
3. User can start and stop Safety Mode.
4. Application listens for the safe phrase only when Safety Mode is active.
5. When the safe phrase is detected, the app gets the user’s current location.
6. The app prepares an SOS message with location and time.
7. The app opens WhatsApp or SMS with the prepared message.
8. User can cancel the SOS within a short countdown period.

## 4. Out of Scope for MVP

The following features are not required for the first version:

- automatic background listening 24/7
- reading WhatsApp, Telegram, or other chats
- automatic sending without user confirmation
- voice identity recognition
- cloud storage
- login/registration
- police/emergency service integration
- complex AI/ML model

## 5. Main User Flow

### Use Case 1: Configure Safety Settings

**Actor**

User

**Preconditions**

Application is installed/opened.

**Flow**

1. User opens the app.
2. User navigates to Settings.
3. User enters a safe phrase, for example:
   “Call Aunty Aigul”
4. User enters one or more trusted contact phone numbers.
5. User enters or confirms default SOS message text.
6. User saves settings.

**Expected Result**

The app stores the safe phrase and trusted contacts locally on the device.

### Use Case 2: Start Safety Mode

**Actor**

User

**Preconditions**

Safe phrase and at least one trusted contact are saved.

**Flow**

1. User opens the app.
2. User clicks Start Safety Mode.
3. Application requests microphone permission if not already granted.
4. Application starts listening for the predefined safe phrase.
5. App displays status:
   Safety Mode Active / Listening

**Expected Result**

The app listens only while Safety Mode is active.

### Use Case 3: Trigger SOS by Voice

**Actor**

User

**Preconditions**

Safety Mode is active.

**Flow**

1. User says the predefined safe phrase.
2. Application converts speech to text.
3. Application compares recognized text with the saved safe phrase.
4. If phrase matches, app shows SOS countdown screen.
5. App gets current geolocation.
6. App prepares SOS message.

**Expected Result**

SOS message is prepared with user’s current location and timestamp.

### Use Case 4: Cancel False Alarm

**Actor**

User

**Preconditions**

Safe phrase was detected.

**Flow**

1. App displays countdown:
   SOS will be sent in 5 seconds
2. User clicks Cancel.
3. App cancels the SOS action.
4. App returns to Safety Mode screen.

**Expected Result**

No message is opened or sent.

### Use Case 5: Send SOS Message

**Actor**

User

**Preconditions**

Safe phrase was detected and user did not cancel.

**Flow**

1. Countdown completes.
2. App opens WhatsApp or SMS with pre-filled message.
3. User confirms sending manually.

**Expected Result**

Trusted contact receives SOS message with location.

## 6. Functional Requirements

| ID | Requirement |
| --- | --- |
| FR-01 | The system shall allow the user to enter and save a safe phrase. |
| FR-02 | The system shall allow the user to add at least one trusted contact phone number. |
| FR-03 | The system shall allow the user to update or delete trusted contacts. |
| FR-04 | The system shall allow the user to customize the SOS message text. |
| FR-05 | The system shall provide a Start Safety Mode button. |
| FR-06 | The system shall provide a Stop Safety Mode button. |
| FR-07 | The system shall request microphone permission before listening. |
| FR-08 | The system shall listen for speech only when Safety Mode is active. |
| FR-09 | The system shall convert detected speech to text. |
| FR-10 | The system shall compare recognized text with the saved safe phrase. |
| FR-11 | The system shall trigger SOS flow when the safe phrase is detected. |
| FR-12 | The system shall request geolocation permission before getting location. |
| FR-13 | The system shall generate Google Maps location link from GPS coordinates. |
| FR-14 | The system shall include current date and time in the SOS message. |
| FR-15 | The system shall show a 5-second cancel countdown before opening the message. |
| FR-16 | The system shall allow the user to cancel the SOS action. |
| FR-17 | The system shall open WhatsApp or SMS with pre-filled SOS message. |
| FR-18 | The system shall not store audio recordings. |
| FR-19 | The system shall not send audio to any server in MVP. |
| FR-20 | The system shall store settings locally on the device/browser. |

## 7. Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-01 | The app should be simple enough for a high school student to demonstrate. |
| NFR-02 | The app should work on mobile browser or mobile device. |
| NFR-03 | The app should not continuously listen in the background. |
| NFR-04 | The app should clearly show when Safety Mode is active. |
| NFR-05 | The app should protect user privacy by not storing audio. |
| NFR-06 | The app should use local storage for MVP settings. |
| NFR-07 | The app should provide clear error messages when microphone or location permission is denied. |

## 8. Data Fields

### User Settings

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| safePhrase | String | Yes | Example: “Call Aunty Aigul” |
| trustedContactName | String | Optional | Example: “Mom” |
| trustedContactPhone | String | Yes | International format preferred |
| sosMessage | String | Yes | Default message can be provided |
| preferredChannel | Enum | Yes | SMS / WhatsApp |

## 9. Default SOS Message

```text
SOS. I may be in danger. Please check on me.

My current location:
{location_link}

Time:
{current_time}
```

## 10. Error Handling

| Scenario | System Behavior |
| --- | --- |
| Microphone permission denied | Show message: “Microphone access is required to use Safety Mode.” |
| Location permission denied | Show message: “Location access is required to include your location in SOS message.” |
| No trusted contact saved | Disable Start Safety Mode and ask user to add contact. |
| No safe phrase saved | Disable Start Safety Mode and ask user to add safe phrase. |
| Speech not recognized | Continue listening. |
| Phrase does not match | Continue listening. |
| WhatsApp/SMS cannot be opened | Show fallback message with copy option. |

## 11. Phrase Matching Logic

For MVP, exact matching is not required.

The app should normalize both texts before comparison:

- convert to lowercase
- remove punctuation
- trim spaces

**Example:**

**Saved phrase:**

Call Aunty Aigul

**Recognized speech:**

call aunty aigul

Result: match.

**Optional enhancement:**

- allow partial match if similarity is above 80%.

## 12. Suggested Technology Stack

### Option 1 — Web MVP

Recommended for fastest implementation.

- Frontend: HTML/CSS/JavaScript or React
- Speech Recognition: Web Speech API
- Location: Browser Geolocation API
- Storage: Local Storage
- Messaging: WhatsApp deep link / SMS link
- Hosting: Vercel / Netlify

### Option 2 — Mobile App

Better if the project needs to look like a real mobile product.

- React Native with Expo
- Expo Location
- Speech-to-text library
- AsyncStorage
- SMS / WhatsApp link integration

For school project MVP, I recommend Web MVP first.

## 13. MVP Screens

### Screen 1: Home

**Elements:**

- App name: SafePhrase
- Start Safety Mode button
- Settings button
- Short instruction text

### Screen 2: Settings

**Elements:**

- Safe phrase input
- Trusted contact input
- SOS message input
- Preferred channel: SMS / WhatsApp
- Save button

### Screen 3: Safety Mode

**Elements:**

- Status: Listening
- Recognized speech preview
- Stop Safety Mode button
- Test SOS button

### Screen 4: SOS Countdown

**Elements:**

- Message: “SOS detected”
- Countdown timer: 5 seconds
- Cancel button
- Send Now button

## 14. Acceptance Criteria

### AC-01: Save Safe Phrase

Given user enters a safe phrase  
When user clicks Save  
Then the phrase is stored locally and can be used in Safety Mode.

### AC-02: Start Safety Mode

Given safe phrase and trusted contact are saved  
When user clicks Start Safety Mode  
Then app starts listening for speech.

### AC-03: Detect Safe Phrase

Given Safety Mode is active  
When user says the saved safe phrase  
Then app triggers SOS countdown.

### AC-04: Cancel SOS

Given SOS countdown is displayed  
When user clicks Cancel  
Then SOS is cancelled and no message is opened.

### AC-05: Prepare SOS Message

Given SOS countdown completes  
When location is available  
Then app opens SMS or WhatsApp with pre-filled message and location link.

### AC-06: Permission Error

Given microphone or location permission is denied  
When user starts Safety Mode or triggers SOS  
Then app displays a clear error message.

## 15. Demo Scenario for Competition

1. Student opens SafePhrase app.
2. Shows saved phrase: “Call Aunty Aigul”.
3. Clicks Start Safety Mode.
4. Says the phrase.
5. App detects it.
6. Countdown appears.
7. App gets location.
8. WhatsApp/SMS opens with prepared SOS message.
