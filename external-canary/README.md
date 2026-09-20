# BUE External Canary

This is a repository-top-level consumer sample, not an official feature and not a release artifact.

- FeatureId: `com.bue.canary.hello` (outside the reserved `io.github.yu80rice.bue` segment).
- BepInEx GUID: `com.bue.canary.plugin`.
- The project references exactly one published player assembly: `BetterUnturnedExperience.dll`.
- `CopyLocal` is disabled; the BUE DLL is supplied by the runtime deployment, never copied into this sample output.
- The sample deliberately does not use the NoOp seven-seam probe.

Build from the repository root with a repack output:

```powershell
dotnet msbuild external-canary/ExternalCanary.csproj -t:Rebuild -p:Configuration=Release -p:BuePublishedDll="$PWD/artifacts/repack/external-canary/BetterUnturnedExperience.dll"
```

This sample proves only independent compilation and headless discovery/registration. It does not prove that the BUE ecosystem exists, and binary upgrade compatibility is deferred by DEV-V6-09.
