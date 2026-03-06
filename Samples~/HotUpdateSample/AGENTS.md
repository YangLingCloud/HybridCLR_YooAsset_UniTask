# AGENTS.md — HotUpdateSample

Complete hot-update sample demonstrating HybridCLR + YooAsset + UniTask integration. Importable via UPM Package Manager.

## Structure

```
HotUpdateSample/
├── AOTScripts/           # AOT assembly (ships with APK, NOT hot-updatable)
│   ├── AOTPublic.asmdef
│   ├── HttpHelper.cs
│   └── SampleBundleEncryption.cs
├── Editor/               # Sample editor tools (snapshot import, path normalization)
├── EventDefine/          # UniEvent message types (Battle/Patch/Scene/User)
├── HotUpdateAssets/      # Packaged into AssetBundles by YooAsset
│   ├── HotUpdateDll/     # OUTPUT: compiled hot-update DLLs land here
│   └── PatchedAOTDLL/    # OUTPUT: AOT metadata .bytes files
├── HotUpdateScripts/     # Hot-update assembly (loaded at runtime by HybridCLR)
│   ├── HotUpdate.asmdef  # autoReferenced: false (CRITICAL)
│   └── animate/          # Note: lowercase (known inconsistency)
├── PatchLogic/           # YooAsset download FSM (8 states)
├── Resources/            # Built-in prefabs (PatchWindow)
├── Scripts/              # Main scene AOT entry points
├── Settings/             # ScriptableObject config assets
└── ThirdParty/           # Vendored utilities (no external deps)
    ├── UniEvent/         # Publish/subscribe event bus
    ├── UniMachine/       # Finite state machine framework
    └── UniUtility/       # BitMask, Timer, StringFormat helpers
```

## Assembly Separation (CRITICAL)

| Assembly | Type | Hot-Updatable | Key Rule |
|----------|------|---------------|----------|
| `AOTPublic` | Runtime | NO | Ships with APK. Interfaces/utilities for hot-update code to call. |
| `HotUpdate` | Runtime | YES | `autoReferenced: false`. NEVER referenced by AOT assemblies. |
| `com.yanglingyun.hyu.Sample.Editor` | Editor | N/A | Editor-only. Contains snapshot importer. |

## Runtime Flow

```
HybridLauncher (AOT, Scripts/)
  → GameManager (AOT, Scripts/)
    → PatchOperation (8-step FSM, PatchLogic/)
      1. FsmInitializePackage    — Init YooAsset packages
      2. FsmRequestPackageVersion — Query remote version
      3. FsmUpdatePackageManifest — Update manifest
      4. FsmCreateDownloader     — Create download task
      5. FsmDownloadPackageFiles — Download assets
      6. FsmDownloadPackageOver  — Verify downloads
      7. FsmClearCacheBundle     — Clean stale cache
      8. FsmEndPatch             — Load AOT metadata + hot-update DLL
    → HotUpdateLauncher (HotUpdate assembly, loaded via HybridCLR)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add new hot-update script | `HotUpdateScripts/` | Must be under HotUpdate.asmdef scope |
| Add new AOT utility | `AOTScripts/` | Must NOT reference HotUpdate assembly |
| Add FSM state | `PatchLogic/FsmNode/` | Implement `IStateNode`, register in `PatchOperation` |
| Define new event | `EventDefine/` | Implement `IEventMessage` |
| Modify download UI | `PatchLogic/PatchWindow.cs` | Driven by `PatchEventDefine` events |
| Change startup flow | `Scripts/HybridLauncher.cs` | AOT entry point, initializes YooAsset |
| Edit build config | `Settings/HybridBuilderSettings.asset` | ScriptableObject, not code |
| Add encryption | `AOTScripts/SampleBundleEncryption.cs` | YooAsset IDecryptionServices impl |

## FSM Architecture (PatchLogic/)

- Framework: `ThirdParty/UniMachine/` (`StateMachine` + `IStateNode`)
- Orchestrator: `PatchOperation.cs` — registers all 8 nodes, drives transitions
- UI: `PatchWindow.cs` — listens to `PatchEventDefine` events for progress display
- Each `FsmNode` is a self-contained state: `OnEnter` → async work → `machine.ChangeState<NextNode>()`
- Uses UniTask for async operations within each state

## Event System (EventDefine/)

- Framework: `ThirdParty/UniEvent/` (publish/subscribe, `IEventMessage` interface)
- 4 event categories: `BattleEventDefine`, `PatchEventDefine`, `SceneEventDefine`, `UserEventDefine`
- `PatchEventDefine` drives FSM ↔ UI communication (download progress, errors, completion)

## CONVENTIONS (sample-specific)

- Hot-update entry point is always `HotUpdateLauncher.cs` — instantiated via reflection after DLL load
- AOT scripts in `Scripts/` handle bootstrap only — no game logic
- `HotUpdateAssets/` subdirectories are YooAsset collector targets — paths stored in `Settings/AssetBundleCollectorSetting.asset`
- After sample import, run `Normalize Collector Paths` to fix relative → absolute path conversion

## ANTI-PATTERNS

- NEVER add AOTScripts/ references to HotUpdate.asmdef (breaks hot-update isolation)
- NEVER put game logic in Scripts/ (AOT bootstrap only)
- NEVER skip FsmEndPatch's AOT metadata loading step (causes MissingMethodException at runtime)
- NEVER assume HotUpdateDll/ or PatchedAOTDLL/ have content in source — they are build output directories
