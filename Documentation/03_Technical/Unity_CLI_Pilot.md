# Unity CLI pilot

Verified October 28, 2026, against CityForge V4 using Unity Editor **6000.1.12f1**, Unity CLI **1.0.0-beta.14**, and Unity Pipeline **0.9.1-exp.1**. This is an experimental local Editor automation pilot, not a completed CI build pipeline.

## Installed dependency

`Packages/manifest.json` pins `com.unity.pipeline`. Unity resolved the matching lockfile dependencies; this includes Mono.Cecil 1.11.6 and Newtonsoft.Json 3.2.1. Editor version, URP version, visual assets, project rendering settings and player saves were not changed.

The package provides a local Editor command server. Run commands from the project directory, or pass `--project-path` explicitly when multiple projects are open. Keep Pipeline descriptors, authentication material, Library contents, logs, licensed assets and builds outside public Git.

## Verified commands

```sh
unity --version
unity --json status
unity --json command --project-path . script eval --code 'return UnityEngine.Application.unityVersion;'
unity --json command --project-path . capture screenshot --view game --output /absolute/output/MainMenu.png --width 1600 --height 1000
```

Group and command are separate shell arguments (`script eval`, `capture screenshot`). CLI beta.8 interpreted a single quoted group-and-command string incorrectly. Discover the connected package's actual command schemas with:

```sh
unity --json command --project-path . --query screenshot --detail full
```

The pilot inspected the active scene, roots, dirty state, Play state and active color space through live C# evaluation. It captured the MainMenu Game view at 1600×1000. It invoked the existing `CityForge.Editor.InitialProjectSetup.Validate()` routine, which checks URP, the mass-study prefab, the orthographic camera rig and five URP Lit materials. The wrapper refused unsaved scenes and restored the original scene setup in `finally`. All passed with structured JSON results. No temporary script was imported into Assets and no project domain reload was needed for those evaluations.

This scaffold check is a limited smoke test. It does not qualify gameplay, current native-district appearance, tree LODs, water, performance or a player build. Historical forest/asset evaluation menus remain inappropriate gameplay baselines.

## macOS Keychain issue encountered

The pre-existing CLI beta.8 repeatedly requested access to the shared `unity` Keychain item. Unity documented a beta.8 analytics credential-read timeout that could prevent Always Allow from persisting. Do not reset the user's Keychain, remove credentials, broaden access to all apps, disable macOS protections or repeatedly ask for passwords to work around this.

The pilot stopped the old pending CLI processes; the user dismissed remaining prompts with **Deny**. The CLI was updated from Unity's official installer to beta.14, checksum checked by the installer and verified as signed by Unity Technologies SF. Editor and Hub versions were not upgraded. Subsequent status, evaluation, screenshot and validation commands completed normally, and the user confirmed the prompts were gone.

The installer initially replaced the executable in place and macOS killed the first new invocation (exit 137), despite its signature verifying on disk. Atomically replacing it with the same signed bytes on a fresh inode resolved that launch issue; no signing or Gatekeeper protection was disabled.

References:
- https://docs.unity.com/en-us/unity-cli/use-unity-cli
- https://docs.unity.com/en-us/unity-cli/release-notes
- https://discussions.unity.com/t/unity-cli-1-0-0-beta-10-is-rolling-out/1736729/5

## Follow-up

Prefer this local CLI interface for observation and controlled Editor operations while retaining existing tooling as a fallback. Pin the Editor version. Before adopting a build command, review the existing build method: `PlayerBuildVerification.Run()` invokes `RegionPbrKitBuilder.BuildGraph()` and saves assets, so it is not a read-only validation. A player build was not run in this pilot. Do not automatically update the Editor, accept legal terms, change visual settings, or publish licensed assets as part of CLI setup.

## Subsequent forest-study use

After the initial pilot, controlled ARM64 release players were built through Pipeline `script run` and `BuildPipeline.BuildPlayer`, with MainMenu as the sole build scene and temporary architecture/shader-inclusion settings restored in `finally`. The local forest replay tool preserves this workflow; see `Approved_Forest_Canopy.md`. This subsequent work does not turn the initial scaffold check into a gameplay regression suite or production CI pipeline.

On macOS, launch a test player once through `open -W -n --env ...`. Starting its executable and immediately calling `open -a` left duplicate menu players in an early study. Record focus per frame and use a single foreground player for performance claims.
