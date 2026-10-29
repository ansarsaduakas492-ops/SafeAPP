# Android APK handoff

Candidate date: 2026-10-29. Display name: SafePhrase. Version: 1.0 (version code 1). Application ID: `com.companyname.safephrase`, retained to match the existing development app.

## Delivery files

The generated delivery folder is `artifacts/SafePhrase-1.0/`. It contains `SafePhrase-1.0.apk`, `README.md`, `BUILD.md`, `VERIFICATION.md`, and `SHA256SUMS.txt`. A ZIP of the folder is provided alongside it. APKs, bundles, build intermediates, and signing keys are not committed. The user authorized commit/push and repository distribution on 2026-10-29. Installer assets belong to the [v1.0.0-rc.1 release](https://github.com/ansarsaduakas492-ops/SafeAPP/releases/tag/v1.0.0-rc.1); this is a phone-test candidate, not store publication or mentor acceptance.

## Configuration and signing

This is a standalone **Release** APK with application assemblies embedded, not Visual Studio fast deployment. It uses the SDK's local **debug/test certificate** (`AndroidKeyStore=false`) for the educational phone trial. Release optimization does not turn that test certificate into a production signing identity. Do not use this candidate as a store release; production distribution requires a stable private signing key and separate publication work. No private key or password is included in the delivery folder.

An installed app with the same application ID can only be updated with a compatible signing identity/version. If the old installation uses a different key, Android will reject the update. Uninstalling removes its local data. Rebuilding on another computer can produce a different test certificate and different bytes, even with unchanged source.

Reference: [Microsoft's Android CLI publishing guide](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0).

## Rebuild from source

Prerequisites: .NET SDK 10.0.401 (used for this candidate), the .NET MAUI Android workload, Android SDK/build tools, and the workload-compatible Java JDK. Run from the repository root in PowerShell. The following command publishes only the Android project and keeps the ordinary Visual Studio outputs separate:

```powershell
dotnet publish src/SafePhrase/SafePhrase.csproj -f net10.0-android -c Release -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormats=apk -p:AndroidKeyStore=false -p:EmbedAssembliesIntoApk=true -p:BaseIntermediateOutputPath=obj/ApkCandidate/ -p:BaseOutputPath=bin/ApkCandidate/ -p:UseSharedCompilation=false -m:1 -nr:false -v:minimal
```

The project explicitly excludes all `bin/**` and `obj/**` files from default source globs so that changing the base paths does not compile old generated C# files. Base paths allow separate per-architecture intermediates; do not substitute one fixed global `IntermediateOutputPath` for all architectures.

Use the signed output under `src/SafePhrase/bin/ApkCandidate/Release/net10.0-android/publish/`, not an unsigned APK. Copy it to the delivery folder as `SafePhrase-1.0.apk`. The generated manifest and package contents, not just the command line, must confirm the resulting SDK levels, ABIs, and embedded assemblies.

Run the existing linked-logic checks:

```powershell
dotnet run --project src/SafePhrase.LogicChecks/SafePhrase.LogicChecks.csproj -c Release -p:UseSharedCompilation=false
```

Check an APK against the supplied hash:

```powershell
Get-FileHash artifacts/SafePhrase-1.0/SafePhrase-1.0.apk -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt`. The checksum identifies the candidate; it is not proof of phone behavior.

## Verification record

- Publish completed successfully using the command above, with no warning/error lines in the final build log. The first sandbox attempt could not start an MSBuild task host. Fixed global intermediate paths then collided between architecture builds; changing to Base paths exposed older generated C# files, resolved by explicitly excluding all bin/obj files. The successful build used per-architecture intermediate paths and did not delete or stop the IDE's outputs.
- APK identity: `com.companyname.safephrase`, version `1.0` / code `1`, minimum API `21`, target API `36`, ABIs `arm64-v8a` and `x86_64`. Both ABIs contain an embedded `libassembly-store.so` plus the .NET runtime; no fast-deployment assembly directory is needed.
- APK size: 29,642,434 bytes. SHA-256: `1c3070f67f4d0594870939c804a6981579771ceaedf45e3df4058ae2e31cebd0`.
- Android build-tools 36.0.0 `apksigner verify --verbose --print-certs` passed v1/v2/v3 verification. Signer: `CN=Android Debug, O=Android, C=US`; certificate SHA-256: `84ecebe1086bcdaf60b111926bf12b531532ca73c74f618ba96a736644187bf7`. The tool emitted v1 notices about META-INF dependency metadata; APK v2/v3 verification passed. `zipalign -c -P 16 -v 4` passed.
- All 58 existing linked-logic checks passed. Focused independent source/document review found no remaining blocking finding after output-glob and WhatsApp privacy fixes. Release JSON reflection is enabled in the generated runtime configuration.

See `verification.md` in the repository, or `VERIFICATION.md` in the delivery folder. Installation, Release runtime behavior, permission prompts, speech, actual messages, and mentor acceptance require separate evidence from the chosen physical device.
