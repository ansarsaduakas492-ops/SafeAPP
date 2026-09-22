# Student setup: first .NET MAUI Android run

This guide supports the agreed two-week educational project: C#, XAML, .NET MAUI, and an Android APK demonstration. You install the tools and create the first project manually. Agents will then help implement the interface and logic, explaining each small change in beginner-friendly terms. Project decisions are recorded in [project-decisions.md](project-decisions.md).

## 1. Install Visual Studio on Windows

1. Open Microsoft's [MAUI installation guide](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation?tabs=vswin) and select the **Visual Studio** instructions. Use the current stable Visual Studio release that supports the chosen MAUI/.NET version and your Windows edition. Check the linked system requirements before downloading. This project does not pin an IDE or SDK version yet.
2. Run Visual Studio Installer yourself. Install Visual Studio, or choose **Modify** for an existing installation.
3. Select **.NET Multi-platform App UI development**, retaining its default optional components for Android development. Let the installer supply the matching .NET, Android, and Java tooling.
4. Finish installation and restart Windows if requested. Launch Visual Studio.
5. Record the IDE version from **Help > About**. In a fresh terminal, run `dotnet --version` and `dotnet workload list`; record versions, not complete machine diagnostics. Workload names may differ between Visual Studio and command-line installations; a missing literal `maui` entry alone does not prove failure.

Visual Studio is the IDE used here. Installation and first-run success remain manual checkpoints; this document does not assert that either has happened.

## 2. Choose your project name

Choose the name yourself before creating files. The repository folder name `SafeApp` does not force the project or launcher name.

| Name | Meaning | Optional example |
| --- | --- | --- |
| Solution | Container that groups projects in Visual Studio | `SafePhrase` |
| Project | Buildable application; its name normally becomes the `.csproj` filename | `SafePhrase` |
| Root namespace | Prefix organizing C# types; initially usually based on the project name | `SafePhrase` |
| Display name | Text shown to the phone user; MAUI property `ApplicationTitle` | `Safe Phrase` |
| Application ID | Android package identity; MAUI property `ApplicationId` | `com.example.safephrase` |

For this first project, use a short descriptive PascalCase name: capitalize each word and omit spaces, such as `SafePhrase`. Prefer Latin letters and optional digits, beginning with a letter. Avoid hyphens, punctuation, and C# keywords. This is a simple project convention that also works comfortably as a C# namespace; it is not a claim that every other filename is forbidden. C# identifiers are case-sensitive. Microsoft documents the distinction between language rules and naming conventions in [C# identifier names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names).

The application ID should use a stable lowercase reverse-domain-style value. The example above is a placeholder, not an assigned project identity. Agree the real value before distributing builds. Changing the display name does not change the installed application's identity. Review these properties in Microsoft's [Android publishing reference](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli).

## 3. Create the project manually

1. Locate your own checkout folder (called `<repo>` below), then inspect `<repo>\src` first. The mentor's checkout path is not a required path on your computer. If a project or solution already exists, open it and report its name instead of creating another. Do not overwrite, delete, or move existing files. A `.vs` folder is local IDE state, not proof that a usable project exists; leave it alone.
2. In Visual Studio, choose **Create a new project**, search for MAUI, and select **.NET MAUI App** using C#. Follow Microsoft's [first app walkthrough](https://dotnet.microsoft.com/en-us/learn/maui/first-app-tutorial/create?initial-os=windows) for the current screens.
3. Enter your chosen project name. Set **Location** to `<repo>\src`, substituting your actual checkout path. If offered, select **Place solution and project in the same directory**. Check the resulting path before pressing Create: the intended project file is `src\<ChosenName>\<ChosenName>.csproj`, not `src\<ChosenName>\<ChosenName>\<ChosenName>.csproj`. Do not append the project name to Location if the wizard already adds it.
4. Choose a stable .NET version supported by the installed IDE and MAUI workload. Record the selection; do not copy a version from an old tutorial screenshot.
5. Create the project and allow dependency restore to finish. Keep the generated template for the first run. Do not create another Git repository inside `src`.

Before feature work, locate `MainPage.xaml` (page layout), `MainPage.xaml.cs` (page C# behavior), and the `.csproj` (build settings). C# classes, methods, and conditions will feel partly familiar from C++; XAML describes the visual elements that those classes control.

## 4. Run on a physical Android phone

1. On your phone, enable developer options, typically by tapping **Build number** seven times under **About phone**. Then enable **USB debugging**. Menus vary by manufacturer.
2. Connect the unlocked phone with a data-capable USB cable. Accept the USB debugging trust prompt for your development computer.
3. In Visual Studio, select **Debug**, choose the Android target and your connected phone in the run-target selector, then press **F5**. Wait for build, installation, and launch.
4. Use the template's counter button and confirm that its displayed count changes.

If the phone is absent, check the cable, trust prompt, and manufacturer's Windows USB driver. If needed, inspect `adb devices` locally using Visual Studio's Android ADB command prompt; share only whether the device is authorized, unauthorized, or absent. Never paste the device serial number. Microsoft's [physical-device setup guide](https://learn.microsoft.com/en-us/dotnet/maui/android/device/setup) covers these steps and driver links.

If the build fails, capture the first relevant error code and a short sanitized message, plus the chosen target framework. Do not repeatedly recreate the project to troubleshoot it.

Debug deployment proves the development loop. The eventual demonstration also needs a signed Release APK that installs and starts independently of Visual Studio. Packaging, signing-key storage, and installation testing are a later milestone; keep keys and passwords out of source control and chat. See [Microsoft's Android publishing guide](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli).

## 5. Understand the Git checkpoint

From your repository root (`<repo>`), `git status --short` shows changed and untracked files, and `git diff` shows changes to tracked files. Review untracked source files separately: ordinary `git diff` does not display their content.

A commit saves a local snapshot; a push uploads commits to a remote. Neither is part of this setup checkpoint. Before any future commit, review the ignore rules for `.vs`, `bin`, and `obj`, and keep generated files, credentials, and signing material out. Do not use **Commit all** or initialize a nested repository. Branch creation and pushing follow the repository's separate naming and approval rules.

## 6. Report the manual checkpoint

Send a short report with:

- Chosen solution/project name and project path.
- Visual Studio version, .NET SDK version, and Android target framework from the project.
- Confirmation that the phone is recognized, the app opens, and the counter changes.
- A cropped screenshot of the running template, if convenient, or a concise description of the visible result.
- Any first blocking error, with device IDs, usernames, personal paths, credentials, and private content removed.

Do not report full logs or screenshots containing phone identifiers or notifications. After this checkpoint, agents can start small UI and logic changes against the actual project and explain what each change does and how you can test it.
