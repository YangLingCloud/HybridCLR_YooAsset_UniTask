# AGENTS.md — com.yanglingyun.hyu v3.1.0

## Project Overview

This repository is a **pure Unity Package (UPM) root**, not a Unity project.

- Package name: `com.yanglingyun.hyu`
- Version: 3.1.0
- Minimum Unity: 2022.3
- Domain: HybridCLR + YooAsset hot-update build toolchain
- Target platforms: Windows, Android, iOS
- License: MIT

This package provides an all-in-one editor build pipeline for Unity hot-update scenarios, integrating HybridCLR (C# hot-update), YooAsset (resource management), and UniTask (async programming).

## Repository Structure

```
.
├── package.json                          # UPM package definition (dependencies, samples)
├── CHANGELOG.md                          # Changelog
├── README.md / README_EN.md              # Bilingual documentation (CN / EN)
├── LICENSE                               # MIT
├── README/                               # Documentation assets (.png, .xmind, .pdf, .docx)
│
├── Editor/                               # Editor assembly: com.yanglingyun.hyu.Editor
│   ├── HybridEditor.asmdef               # Editor-only asmdef
│   ├── BuildHelper.cs                    # AOT metadata check, DLL copy, APK build, link.xml supplement
│   ├── HybridBuilderWindow.cs            # UI Toolkit build window controller
│   ├── HybridBuilderWindow.uxml          # Window UI layout
│   ├── HybridBuilderSettings.cs          # Build config ScriptableObject + HybridBuildOption enum
│   ├── HybridBuildPipeViewerBase.cs      # Build pipeline viewer base class
│   ├── HybridBuildPipeViewerBase.uxml    # Viewer UI layout
│   ├── HybridScriptableBuildPipelineViewer.cs  # SBP build pipeline viewer
│   ├── SceneHelper.cs                    # Scene utilities
│   ├── BuildPipelineTask/
│   │   └── TaskBuildScript_SBP.cs        # SBP custom build task (script packaging)
│   └── ScriptableBuildPipeline/
│       ├── HybridScriptableBuildPipeline.cs      # SBP pipeline impl
│       └── HybridScriptableBuildParameters.cs    # SBP build parameters
│
├── Runtime/                              # Runtime assembly: com.yanglingyun.hyu.Runtime
│   ├── com.yanglingyun.hyu.Runtime.asmdef
│   └── HybridRuntimeSettings.cs          # Runtime config (HostServerIP, ReleaseBuildVersion, Packages)
│
└── Samples~/                             # Importable samples (UPM convention, not compiled)
    ├── HotUpdateSample/                  # Full hot-update sample
    │   ├── AOTScripts/                   # AOT runtime scripts (AOTPublic.asmdef)
    │   ├── Editor/                       # Sample editor tools (com.yanglingyun.hyu.Sample.Editor.asmdef)
    │   │   ├── HybridSettingsImporter.cs           # Auto/manual settings importer
    │   │   └── HybridCLRSettingsSnapshot.json      # HybridCLR preset config snapshot
    │   ├── EventDefine/                  # UniEvent event definitions (Battle/Patch/Scene/User)
    │   ├── HotUpdateAssets/              # Assets to be packaged (Prefabs/Scenes/Textures/Materials etc.)
    │   │   ├── HotUpdateDll/             # Compiled hot-update DLL output directory
    │   │   └── PatchedAOTDLL/            # AOT supplementary metadata DLLs (.bytes)
    │   ├── HotUpdateScripts/             # Hot-update assembly (HotUpdate.asmdef)
    │   ├── PatchLogic/                   # YooAsset patch download state machine (8 FSM nodes)
    │   ├── Resources/                    # Built-in resources (PatchWindow prefab etc.)
    │   ├── Scripts/                      # Main scene AOT scripts (GameManager, HybridLauncher)
    │   ├── Settings/                     # Pre-configured ScriptableObject assets
    │   └── ThirdParty/                   # Lightweight dependencies (UniEvent/UniMachine/UniUtility)
    └── BuildTests/                       # Build pipeline tests
        └── Editor/
            ├── com.yanglingyun.hyu.Tests.Editor.asmdef
            └── HybridBuildPipelineTests.cs   # NUnit EditMode tests
```

## Assembly Structure

| Assembly | Type | Location | Description |
|----------|------|----------|-------------|
| `com.yanglingyun.hyu.Editor` | Editor | `Editor/` | Core editor build tools, Editor-only |
| `com.yanglingyun.hyu.Runtime` | Runtime | `Runtime/` | Runtime config types, all platforms |
| `com.yanglingyun.hyu.Sample.Editor` | Editor | `Samples~/HotUpdateSample/Editor/` | Sample editor tools |
| `com.yanglingyun.hyu.Tests.Editor` | Editor | `Samples~/BuildTests/Editor/` | Test assembly, requires `UNITY_INCLUDE_TESTS` |
| `AOTPublic` | Runtime | `Samples~/HotUpdateSample/AOTScripts/` | Sample AOT scripts |
| `HotUpdate` | Runtime | `Samples~/HotUpdateSample/HotUpdateScripts/` | Sample hot-update assembly |

## Package Dependencies

Declared in `package.json` (resolved automatically by UPM):

| Package | Version | Purpose |
|---------|---------|---------|
| `com.code-philosophy.hybridclr` | 8.2.0 | HybridCLR hot-update core |
| `com.tuyoogame.yooasset` | 2.3.9 | YooAsset resource management |
| `com.cysharp.unitask` | 2.5.10 | UniTask async programming |
| `com.unity.scriptablebuildpipeline` | 1.21.21 | SBP build pipeline |
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | JSON serialization |

## Installation

```json
"com.yanglingyun.hyu": "https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask.git"
```

No `?path=` suffix needed — the repo root is the package root.

## Editor Menus

### Package Menus (`HybridTool/`)

| Menu Item | Function |
|-----------|----------|
| `Check AOT Metadata` | Validate whether AOT metadata needs supplementation |
| `Build APK` | Build APK package |
| `Get Patched AOT Assembly List` | Get list of AOT assemblies requiring supplementation |
| `Generate AOT DLLs and Copy` | Generate AOT DLLs and copy to resource directory |
| `Generate Hot-Update DLLs and Copy` | Compile hot-update DLLs and copy to resource directory |
| `Supplement Prefab Dependencies` | Supplement prefab dependencies to link.xml |
| `Hybrid Builder` | Open UI Toolkit build window |

### Sample Menus (`HybridTool/Sample-HotUpdateSample/`)

| Menu Item | Function |
|-----------|----------|
| `Export HybridCLR Settings Snapshot` | Export current HybridCLR config snapshot |
| `Restore HybridCLR Settings from Snapshot` | Restore HybridCLR config from snapshot |
| `Normalize Collector Paths` | Normalize YooAsset collector paths |

## Key Classes & Responsibilities

### Editor/

- **`BuildHelper`** — Static utility class, core methods:
  - `GetBuildScenes()` — Get build scene list
  - `EnsureAOTStripDirExists(aotDir)` — Check AOT strip directory; if missing, prompt to trigger `PrebuildCommand.GenerateAll()`
  - `CheckAccessMissingMetadata()` — Compare AOT vs hot-update DLLs to determine if APK rebuild is needed
  - `SupplementPrefabDependent` methods — Supplement link.xml (handles Missing Script safely, Set-based dedup)
  - `ProjectPath` — Project root directory (parent of `Application.dataPath`)

- **`HybridBuilderSettings`** — Build config ScriptableObject, fields include:
  - `RuntimeSettings` — Associated runtime config
  - `AssetPackages` — Asset package name list
  - `ScriptPackageName` — Script package name
  - `PatchedAOTDLLFolder` / `HotUpdateDLLFolder` — DLL directory references
  - `ReleaseBuildVersion` / `AssetBuildVersion` / `ScriptBuildVersion` — Version numbers
  - `buildOutputPath` — Build output path (supports relative paths, resolved via `ResolveBuildOutputPath()`)
  - `isClearBuildCache` / `isUseAssetDependDB` / `isUseSelfIncrementingVersions` — Build options
  - `assetCompressOption` / `assetFileNameStyle` / `assetEncyptionClassName` — YooAsset packaging options
  - `assetBuildinFileCopyOption` / `assetBuildinFileCopyParams` — Built-in file copy options
  - `hybridBuildOption` — Hybrid build option (`HybridBuildOption` enum: None/BuildAll/BuildAsset/BuildScript/BuildApplication)

- **`HybridRuntimeSettings`** (Runtime/) — Runtime config ScriptableObject:
  - `HostServerIP` — Resource server address
  - `ReleaseBuildVersion` — Release version
  - `Packages` — Package names and version info

- **`HybridBuilderWindow`** — UI Toolkit build window controller
- **`HybridBuildPipeViewerBase`** — Build pipeline viewer base class
- **`HybridScriptableBuildPipelineViewer`** — SBP build pipeline viewer, distinguishes Asset/Script packaging
- **`TaskBuildScript_SBP`** — SBP custom build task (script packaging flow)
- **`HybridScriptableBuildPipeline`** — SBP pipeline implementation
- **`HybridScriptableBuildParameters`** — SBP build parameter definition
- **`SceneHelper`** — Scene utilities

### Samples~/HotUpdateSample/Editor/

- **`HybridSettingsImporter`** — Auto-detects HybridCLR config on import, prompts snapshot restore
- **`HybridCLRSettingsSnapshot.json`** — Preset config snapshot (hotUpdateAssemblyDefinitions, patchAOTAssemblies, etc.)

## Build Pipeline Architecture

```
Base Package Build (low frequency)          Hot-Update Build (high frequency)
├── PrebuildCommand.GenerateAll()           ├── CompileDllCommand.CompileDllActiveBuildTarget()
│   ├── Compile hot-update DLLs             ├── Copy hot-update DLLs to HotUpdateAssets/
│   ├── Il2CppDefGeneratorCommand           ├── YooAsset asset packaging (Asset packages)
│   ├── LinkGeneratorCommand                ├── YooAsset script packaging (Script package, RawFile)
│   └── StripAOTDllCommand                  └── Generate version info
├── Copy AOT supplementary metadata
└── Build APK
```

### First-Build Prerequisite Chain

When the AOT strip directory does not exist, `BuildHelper.EnsureAOTStripDirExists()` automatically prompts and triggers `PrebuildCommand.GenerateAll()`, which executes the full chain: compile hot-update DLLs → generate IL2CPP definitions → generate link.xml → generate stripped AOT DLLs → generate bridge functions.

### link.xml Defensive Handling

`SupplementPrefabDependent` automatically creates a valid XML document when `HybridCLRData/Generated/link.xml` is missing. It handles null components (Missing Script) safely and uses Set-based deduplication to prevent duplicate `<type>` nodes.

## Build & Test

This repository is package source code. Build and test execution happens inside a host Unity project after importing the package and samples.

### Tests

- Location: `Samples~/BuildTests/Editor/HybridBuildPipelineTests.cs`
- Type: NUnit EditMode tests
- Assembly: `com.yanglingyun.hyu.Tests.Editor` (requires `UNITY_INCLUDE_TESTS`)
- Coverage:
  - BuildConfig — HybridCLR config, assembly lists, scene list, path validity
  - BuilderSettings — Asset existence, RuntimeSettings association, output path resolution, version format
  - RuntimeSettings — Asset existence, HostServerIP config
  - Platform Tests — Parameterized (Windows/Android/iOS): DLL paths, AOT strip paths, cross-platform uniqueness
  - FirstBuildPrerequisites — AOT strip directory, GenerateAll completeness, MetadataCheck pass
- Tests marked `[Category("SlowTest")]` execute actual build commands and take longer

## Code Conventions

### Language Rules

- **Code identifiers**: English
- **UI text / dialogs / menu labels**: English
- **Code comments**: Chinese (project convention)
- **Documentation**: Bilingual (README.md Chinese, README_EN.md English)

### C# Style

- Allman brace style
- 4-space indentation
- Prefer explicit guard clauses and defensive checks
- Error logging: `Debug.unityLogger.LogError(tag, message)`
- ScriptableObject fields: `[SerializeField] private` + public property wrapper, setter calls `EditorUtility.SetDirty(this)`
- XML doc comments use Chinese `<summary>`
- `CreateAssetMenu` attribute for ScriptableObject creation menus

### Known Filename Typos & Naming Inconsistencies

The following filenames have spelling inconsistencies. When modifying, keep `.meta` files in sync:

- `Samples~/HotUpdateSample/HotUpdateScripts/animate/` — lowercase directory name (should be PascalCase `Animate/`)

## Safety Rules

- **NEVER** let AOT assemblies reference the `HotUpdate` asmdef (would cause hot-update DLLs to be processed by IL2CPP)
- **NEVER** execute script packaging without generating AOT prerequisites first
- Sample-only logic must stay in `Samples~/HotUpdateSample/Editor/` — do not promote to package-level `Editor/`
- `HotUpdate` asmdef must have `auto reference` disabled to prevent accidental reference by `Assembly-CSharp`
- Pre-build must pass `BuildHelper.CheckAccessMissingMetadata()` to verify hot-update code does not access stripped types

## Agent Modification Guidelines

### When Modifying Snapshot Import/Export Logic

- At runtime, prefer the imported sample local path: `Assets/Samples/com.yanglingyun.hyu/<version>/Hot Update Sample/Editor/`
- Use package-internal path `Samples~/HotUpdateSample/Editor/` only as development fallback
- Note: sample display name is `Hot Update Sample` (with spaces), directory name is `HotUpdateSample` (no spaces)

### When Modifying Build Prerequisite Checks

- Must validate both AOT strip directory and hot-update DLL generation chain
- First-build bootstrap path should prefer `PrebuildCommand.GenerateAll()`
- `EnsureAOTStripDirExists()` is the prerequisite check entry point

### When Modifying link.xml Logic

- Must handle `HybridCLRData/Generated/link.xml` not existing (auto-create)
- Must handle null components (Missing Script) during safe traversal
- Use Set for type deduplication to prevent duplicate `<type>` nodes

### When Modifying Build Output Paths

- `HybridBuilderSettings.buildOutputPath` supports relative paths (relative to project root)
- Resolved to absolute path via `ResolveBuildOutputPath()`
- Full output path obtained via `GetBuildOutputPath()` (includes version subdirectory)

### When Modifying YooAsset Collector Paths

- Sample Collector paths are stored as sample-relative paths
- After import, converted to absolute paths by `Normalize Collector Paths` menu
- Path normalization logic lives in `Samples~/HotUpdateSample/Editor/`

### When Adding New Editor Menus

- Package-level menus go under `HybridTool/`
- Sample-level menus go under `HybridTool/Sample-HotUpdateSample/`
- Menu labels must be in English

### Version Number Format

- Three-segment: `ReleaseBuildVersion_AssetBuildVersion_ScriptBuildVersion`
- Display format: `Realse:{r} AssetPakcage:{a} ScriptPackge:{s}` (note existing typos in code: `Realse`, `Pakcage`, `Packge`)

### Known Assembly Definition Issues

- `HybridEditor.asmdef` and `com.yanglingyun.hyu.Sample.Editor.asmdef` both reference a placeholder GUID `a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6` — this is a template/invalid reference that may cause assembly resolution warnings. Replace with actual GUID or remove if unused.

### When Modifying README/ Documentation Assets

- `README/` contains supplementary documentation files (.png diagrams, .xmind mind maps, .pdf, .docx)
- These are NOT part of the UPM package distribution — consider adding to `.npmignore` if publishing to registry
- Do not confuse with `README.md` / `README_EN.md` (the actual package documentation)
