# HybridCLR + YooAsset + UniTask Integrated Solution

<div align="center">

[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3-brightgreen)](https://unity.com/) [![HybridCLR](https://img.shields.io/badge/HybridCLR-v8.2.0-blue)](https://github.com/focus-creative-games/hybridclr) [![YooAsset](https://img.shields.io/badge/YooAsset-v2.3.9-orange)](https://github.com/tuyoogame/YooAsset) [![UniTask](https://img.shields.io/badge/UniTask-v2.5.10-purple)](https://github.com/Cysharp/UniTask) [![License](https://img.shields.io/badge/License-MIT-green)](LICENSE) [![中文](https://img.shields.io/badge/中文-文档-red)](./README.md)

**Professional Unity Hot-Update & Resource Management Integrated Solution (Pure UPM Package)**

*Enterprise-grade hot-update framework · High-performance resource management · Modern async programming*

</div>

---

## Table of Contents

- [Overview](#overview)
- [Major Changes](#major-changes)
- [Core Concepts](#core-concepts)
- [Installation & Dependencies](#installation--dependencies)
- [Quick Start](#quick-start)
- [Integration Tools](#integration-tools)
- [Build Workflow](#build-workflow)
- [Editor Menus](#editor-menus)
- [Project Structure](#project-structure)
- [Sample Guide](#sample-guide)
- [Testing](#testing)
- [FAQ](#faq)
- [Best Practices](#best-practices)

---

## Overview

**HybridCLR + YooAsset + UniTask Integrated Solution** is a high-performance hot-update and resource management framework designed for Unity developers. By combining three industry-leading frameworks into a unified toolchain, it provides enterprise-grade hot-update capabilities:

- Hot-update DLL compilation and copy
- AOT metadata validation and supplement flow
- Coordinated asset and script packaging
- Sample snapshot import and path normalization
- All-in-one editor build window

Built on **Unity 2022.3, HybridCLR 8.2.0, YooAsset 2.3.9, UniTask 2.5.10**.

### Framework Comparison

| Component | Description | Key Advantage |
|-----------|-------------|---------------|
| **HybridCLR** | Complete C# hot-update solution | Dynamic code execution under IL2CPP |
| **YooAsset** | Professional resource management system | Efficient AssetBundle management and loading |
| **UniTask** | High-performance async programming framework | Zero-allocation async operations |

---

## Major Changes

### Repository Upgraded to Pure Package Layout

This repository has been migrated from "Unity project + embedded package" to "pure UPM package root":

```text
.
├── package.json
├── Editor/
├── Runtime/
└── Samples~/
```

> It no longer contains Unity project folders such as `Assets/`, `ProjectSettings/`, or `Packages/`.

### Installation Change

Now you can use the git URL directly:

```json
"https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask.git"
```

### Migration Guide from 2.x

1. Update package reference to root URL (remove `?path=` usage)
2. Re-import `HotUpdateSample` in host projects
3. Run sample setup menus:
   - `Restore HybridCLR Settings from Snapshot`
   - `Normalize Collector Paths`
4. On first build, allow automatic `GenerateAll` prerequisite chain when prompted

---

## Core Concepts

### Assembly-CSharp.dll

`Assembly-CSharp` is the DLL automatically assembled by Unity. Any code in a Unity project that is not separately compiled will be included in this `Assembly-CSharp.dll`.

### Assembly Definition

`Assembly Definition` is a feature introduced in Unity 2017.3, primarily designed to address compilation time issues with large assemblies.

Creating an `Assembly Definition` in any folder under Assets causes all code in that folder to be compiled into a separate DLL. When modifying code in that folder, only that DLL is recompiled, rather than the entire `Assembly-CSharp.dll`.

### AOT and Hot-Update Assemblies

#### Hot-Update Assemblies

While the hot-update assembly could theoretically be the `Assembly-CSharp` assembly, this framework uses `AssemblyDefinition` to create separate DLLs as hot-update assemblies for clearer project structure and easier resource management. Hot-update assemblies should not be processed by IL2CPP or compiled into the final build.

HybridCLR handles the `IFilterBuildAssemblies` callback to remove hot-update DLLs from the `build assemblies` list.

#### AOT Assemblies

AOT assemblies are shipped with the build and are not updated at runtime. In this framework, `Assembly-CSharp` serves as the main AOT assembly, with other AOT assemblies separated using `AssemblyDefinition`.

When using `Assembly-CSharp` as an AOT assembly, it is strongly recommended to disable the `auto reference` option on hot-update assemblies, because `Assembly-CSharp` is the top-level assembly and automatically references all remaining assemblies, which can lead to accidental references to hot-update assemblies.

### UniTask

UniTask is an open-source library on GitHub that provides a high-performance async solution for Unity. It can replace coroutines for async operations while remaining compatible with the Unity lifecycle, allowing methods like Awake, Start, and coroutines to execute asynchronously — all while still running on the main thread.

### Hot-Update DLL Loading

HybridCLR officially recommends attaching scripts directly to prefabs and loading them via AssetBundle for hot-update loading. Alternatively, you can reflect hot-update classes from the loaded DLL and use `AddComponent` to attach them to GameObjects. Either way, the hot-update DLL must be loaded before loading prefabs or classes.

### HybridCLR First-Build Prerequisite Chain

When running the first build in a fresh project via `HybridBuilder`, the full prerequisite chain is required:

1. Compile hot-update DLLs
2. Generate IL2CPP definitions
3. Generate link.xml
4. Generate stripped AOT DLLs

`HybridBuilder` will trigger `PrebuildCommand.GenerateAll()` automatically when needed.

---

## Installation & Dependencies

### Requirements

- **Unity**: 2022.3 LTS or higher
- **Target Platforms**: Windows, Android, iOS
- **IDE**: Visual Studio 2019+ or Rider

### Installation

Add via `Package Manager → Add Package From URL`:

```
https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask.git
```

### Package Dependencies

Declared in `package.json` (installed automatically):

- `com.code-philosophy.hybridclr` — HybridCLR hot-update core
- `com.tuyoogame.yooasset` — YooAsset resource management
- `com.cysharp.unitask` — UniTask async programming
- `com.unity.scriptablebuildpipeline` — SBP build pipeline
- `com.unity.nuget.newtonsoft-json` — JSON serialization

---

## Quick Start

### 1. Install Package

Install via Package Manager (see installation steps above).

### 2. Import Samples

Find `com.yanglingyun.hyu` in Package Manager and click the **Samples** tab to import:

- **Hot Update Sample** — Complete hot-update example
- **Build Pipeline Tests** — Build pipeline tests

Imported path: `Assets/Samples/com.yanglingyun.hyu/<version>/Hot Update Sample/`

### 3. Initialize Sample Settings

Run menus:

1. `HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot` — Quickly import HybridCLR configuration
2. `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths` — Quickly configure AssetBundleCollectorSetting

### 4. Configure Runtime Parameters

Open the `HybridRuntimeSettings` asset and fill in `HostServerIP` (CDN / resource server address).

### 5. Build

1. Run first build via `HybridTool/` menu (automatically triggers `PrebuildCommand.GenerateAll()` to generate stripped AOT DLLs)
2. Subsequent iterations only require hot-update DLL compilation and resource packaging

---

## Integration Tools

### HybridTool

Since both YooAsset and HybridCLR are loaded via Unity Package Manager, many parts of their code are not convenient to use and cannot be modified. This integration toolset was written as editor code to make the two third-party libraries work better together.

#### Key Features

| Module | Description | Use Case |
|--------|-------------|----------|
| **Metadata Validation** | Compare AOT and hot-update DLLs | Pre-build check |
| **APK Build Flow** | Automated build and dependency analysis | Full build workflow |
| **AOT Metadata Generation** | Auto-generate supplementary files | Resolve stripping issues |
| **Hot-Update DLL Compilation** | Compile hot-update code | Development phase |
| **Prefab Dependency Supplement** | Auto-complete link.xml | Resolve component reference issues |

### HybridBuilderWindow

A modern build tool window based on **UI Toolkit**, rewriting YooAsset.AssetBundleBuilderWindow with additional code packaging settings.

#### Core Components

- **HybridBuilderWindow** — Main window controller
- **HybridBuilderWindow.uxml** — UI layout definition
- **HybridBuildPipeViewerBase** — Core functionality base class

#### Using HybridBuilderWindow

In Unity Editor, open via menu: `HybridTool/Hybrid Builder`.

**Configure Build Settings:**

1. Select HybridBuilderSetting: the window lists all HybridBuilderSetting files in the project
2. Select HybridRuntimeSetting: choose the runtime settings file that defines resource packages and version info
3. Select build options: choose to build assets, scripts, or all

**Execute Build:**

Click the "Build" button. The build process automatically handles:
- Metadata supplement validation
- Hot-update DLL compilation
- AOT metadata generation
- AssetBundle resource packaging

### HybridScriptableBuildPipeline

The main build logic is implemented in HybridScriptableBuildPipelineViewer, distinguishing between Asset and Script packaging at runtime.

#### Modifications to YooAsset Build Pipeline

1. **Runtime build type distinction** — Assets and Scripts use different build pipelines
2. **Enhanced RawFileBuildPipeline** — Added TaskBuildScript_SBP pipeline step
3. **Batch build support** — Configure multiple packages at once via package name list
4. **APK build optimization** — Optimized build flow and error checking
5. **Stripping check** — Pre-build check for hot-update code accessing stripped code

### HybridBuilderSettings Configuration

```csharp
public class HybridBuilderSettings : ScriptableObject
{
    public HybridRuntimeSettings RuntimeSettings;  // Associated runtime config
    public List<string> AssetPackages;              // Asset package name list
    public string ScriptPackageName;                // Script package name
    public DefaultAsset PatchedAOTDLLFolder;        // AOT supplementary metadata DLL directory
    public DefaultAsset HotUpdateDLLFolder;         // Hot-update DLL directory
    public int ReleaseBuildVersion;                 // Release version number
    public int AssetBuildVersion;                   // Asset build version number
    public int ScriptBuildVersion;                  // Script build version number
    public string buildOutputPath;                  // Build output path (supports relative paths)
    public bool isClearBuildCache;                  // Whether to clear build cache
    public bool isUseAssetDependDB;                 // Use asset dependency DB (speeds up build)
    public bool isUseSelfIncrementingVersions;      // Use auto-incrementing version numbers
    public ECompressOption assetCompressOption;     // AB compression option
    public EFileNameStyle assetFileNameStyle;       // AB file naming style
    public string assetEncyptionClassName;          // AB encryption class name
    public EBuildinFileCopyOption assetBuildinFileCopyOption; // Built-in file copy option
    public string assetBuildinFileCopyParams;       // Copy option parameters
    public HybridBuildOption hybridBuildOption;     // Hybrid build option
}
```

### HybridRuntimeSettings Configuration

```csharp
public class HybridRuntimeSettings : ScriptableObject
{
    public string HostServerIP;
    public int ReleaseBuildVersion;
    public string Packages;
}
```

---

## Build Workflow

The build workflow for HybridCLR + YooAsset + UniTask is divided into two main stages: **Base Package Build** and **Hot-Update Package Build**. This separation enables efficient incremental update mechanisms.

### Build Flow Diagram

```
Base Package Build (Low frequency — first release or major updates)
├── Compile AOT assemblies
├── Generate bridge functions
├── Generate stripped AOT DLLs
├── Generate AOT supplementary metadata
└── Build final APK

Hot-Update Package Build (High frequency — daily updates)
├── Compile hot-update assemblies
├── Package hot-update DLLs
├── Package resource files
└── Generate version info
```

### Stage 1: Base Package Build

**Applicable scenarios**: First release, AOT code changes, bridge function changes

1. **Environment Setup**
   - Run `HybridCLR-Installer` to install the HybridCLR environment
   - Run `Generate-All` to generate bridge functions and initialization files

2. **AOT Metadata Generation**
   ```csharp
   // Automatically executed flow
   Il2CppDefGeneratorCommand.GenerateIl2CppDef();
   LinkGeneratorCommand.GenerateLinkXml();
   StripAOTDllCommand.GenerateStripedAOTDlls();
   ```

3. **APK Build**
   - Build the APK containing AOT code
   - Generate stripped AOT DLLs for subsequent hot-updates

### Stage 2: Hot-Update Package Build

**Applicable scenarios**: Hot-update code changes, resource file updates

1. **Hot-Update DLL Compilation**
   ```csharp
   CompileDllCommand.CompileDllActiveBuildTarget();
   ```

2. **Resource Package Build**
   - Package hot-update DLLs as RawFiles
   - Package art assets, configuration files, etc.
   - Generate version control info

3. **Incremental Build Optimization**
   - Leverage YooAsset's incremental packaging mechanism
   - Only rebuild changed resource packages, avoiding full rebuilds
   - `Clear Build Cache` option controls whether to clear the build cache

### Build Decision Mechanism

Determined via `BuildHelper.CheckAccessMissingMetadata()`:

- **Cases requiring APK rebuild**:
  - Hot-update code references stripped types
  - Bridge functions have changed
  - Major AOT code changes

- **Cases requiring only hot-update package update**:
  - Only hot-update logic code modified
  - Resource file updates
  - Hot-update layer bug fixes

#### Bridge Function Stability

Based on how bridge functions work, for a fixed AOT portion, the bridge function set is deterministic. No matter what hot-updates are applied afterward, no additional bridge functions will be needed. **Therefore, there is no risk of bridge function shortages after going live with hot-updates.**

---

## Editor Menus

### Package Menus (`HybridTool/`)

- `Check AOT Metadata` — Validate whether AOT metadata needs supplementation
- `Build APK` — Build APK package
- `Get Patched AOT Assembly List` — Get the list of AOT assemblies requiring supplementation
- `Generate AOT DLLs and Copy` — Generate AOT DLLs and copy to resource directory
- `Generate Hot-Update DLLs and Copy` — Compile hot-update DLLs and copy to resource directory
- `Supplement Prefab Dependencies` — Supplement prefab dependencies to link.xml
- `Hybrid Builder` — Open the UI Toolkit all-in-one build window

### Sample Menus (`HybridTool/Sample-HotUpdateSample/`)

- `Export HybridCLR Settings Snapshot` — Export current HybridCLR configuration snapshot
- `Restore HybridCLR Settings from Snapshot` — Restore HybridCLR configuration from snapshot
- `Normalize Collector Paths` — Normalize YooAsset collector paths

---

## Project Structure

```text
.
├── package.json                # UPM package definition (dependencies, samples)
├── CHANGELOG.md                # Changelog
├── README.md / README_EN.md    # Bilingual documentation (CN / EN)
├── LICENSE                     # MIT
├── README/                     # Documentation assets (.png, .xmind, .pdf, .docx)
│
├── Editor/                     # Editor assembly: com.yanglingyun.hyu.Editor
│   ├── HybridEditor.asmdef     # Editor-only asmdef
│   ├── BuildHelper.cs          # AOT metadata check, DLL copy, APK build, link.xml supplement
│   ├── HybridBuilderWindow.cs  # UI Toolkit build window controller
│   ├── HybridBuilderWIndow.uxml # Window UI layout (note casing: WIndow)
│   ├── HybridBuilderSettings.cs # Build config ScriptableObject + HybridBuildOption enum
│   ├── HybridBuildPipeViewerBase.cs  # Build pipeline viewer base class
│   ├── HybridBuildPipeViewerBase.uxml # Viewer UI layout
│   ├── HybridScriptableBuildPipelineViewer.cs # SBP build pipeline viewer
│   ├── SceneHelper.cs          # Scene utilities
│   ├── BuildPipelineTask/      # Rewritten build pipeline tasks
│   │   └── TaskBuildScript_SBP.cs  # SBP custom build task (script packaging)
│   └── ScriptableBuildPipeline/ # Rewritten build pipeline
│       ├── HybrdiScriptableBuildPipeline.cs     # SBP pipeline impl (note typo: Hybrdi)
│       └── HybridScriptableBuildParameters.cs   # SBP build parameters
│
├── Runtime/                    # Runtime assembly: com.yanglingyun.hyu.Runtime
│   ├── com.yanglingyun.hyu.Runtime.asmdef
│   └── HybridRuntimeSettings.cs # Runtime config (CDN address, version, packages)
│
└── Samples~/                   # Importable samples (UPM convention, not compiled)
    ├── HotUpdateSample/        # Complete hot-update sample
    │   ├── AOTScripts/         # AOT runtime scripts (AOTPublic.asmdef)
    │   ├── Editor/             # Sample editor tools (snapshot import, path normalization)
    │   ├── EventDefine/        # UniEvent event definitions (Battle/Patch/Scene/User)
    │   ├── HotUpdateAssets/    # Assets to be packaged (Prefabs/Scenes/Textures etc.)
    │   ├── HotUpdateScripts/   # Hot-update assembly (HotUpdate.asmdef)
    │   ├── PatchLogic/         # YooAsset patch download state machine (8 FSM nodes)
    │   ├── Resources/          # Built-in resources (PatchWindow prefab etc.)
    │   ├── Scripts/            # Main scene AOT scripts (GameManager, HybridLauncher)
    │   ├── Settings/           # Pre-configured ScriptableObject assets
    │   └── ThirdParty/         # Lightweight dependencies (UniEvent/UniMachine/UniUtility)
    └── BuildTests/             # Build pipeline tests
        └── Editor/
            ├── com.yanglingyun.hyu.Tests.Editor.asmdef
            └── HybridBuildPipelineTests.cs  # NUnit EditMode tests
```

---

## Sample Guide

This package provides two importable samples: **HotUpdateSample** (hot-update example) and **BuildTests** (build tests).

### HotUpdateSample — Complete Hot-Update Example

A ready-to-use hot-update demo project covering the complete flow from resource download to hot-update code execution.

#### Directory Structure

```text
HotUpdateSample/
├── AOTScripts/                # AOT runtime scripts (shipped with base build, not hot-updatable)
│   ├── AOTPublic.asmdef
│   ├── HttpHelper.cs          # HTTP utility class
│   └── SampleBundleEncryption.cs  # YooAsset bundle encryption example
├── Editor/                    # Editor import tools
│   ├── HybridCLRSettingsSnapshot.json  # HybridCLR preset configuration snapshot
│   └── HybridSettingsImporter.cs       # Auto/manual settings importer
├── EventDefine/               # UniEvent event definitions
│   ├── BattleEventDefine.cs
│   ├── PatchEventDefine.cs    # Hot-update flow events
│   ├── SceneEventDefine.cs
│   └── UserEventDefine.cs
├── HotUpdateAssets/           # Resources to be packed into AssetBundles
│   ├── HotUpdateDll/          # Compiled hot-update DLL directory
│   ├── PatchedAOTDLL/         # AOT supplementary metadata DLLs (.bytes format)
│   ├── Prefabs/               # Prefabs
│   ├── Scenes/                # Hot-update scenes
│   ├── Textures/ Materials/ UIPrefabs/ audios/
├── HotUpdateScripts/          # Hot-update assembly (loaded at runtime by HybridCLR)
│   ├── HotUpdate.asmdef
│   ├── HotUpdateLauncher.cs   # Hot-update entry script
│   ├── LoadImage.cs           # YooAsset texture loading example
│   ├── ModelRotate.cs         # YooAsset model loading example
│   └── animate/Rotating.cs    # Rotation animation component
├── PatchLogic/                # YooAsset hot-update download state machine
│   ├── FsmNode/               # 8 FSM state nodes
│   ├── PatchOperation.cs      # State machine dispatcher
│   └── PatchWindow.cs         # Download progress UI controller
├── Scripts/                   # Main scene AOT scripts
│   ├── GameManager.cs         # Game startup manager
│   └── HybridLauncher.cs     # HybridCLR + YooAsset launcher
├── Settings/                  # Configuration files
│   ├── AssetBundleCollectorSetting.asset
│   ├── HybridBuilderSettings.asset
│   └── HybridRuntimeSettings.asset
└── ThirdParty/                # Built-in lightweight utility libraries
    ├── UniEvent/              # Event bus
    ├── UniMachine/            # Finite state machine
    └── UniUtility/            # General utilities
```

#### Usage Steps

**Step 1: Import Sample**

Find `com.yanglingyun.hyu` in Package Manager, click the **Samples** tab, and import **Hot Update Sample**.

**Step 2: Automatic Initialization**

When the editor is opened for the first time after import, `HybridSettingsImporter` will automatically detect the HybridCLR configuration status:
- If the hot-update assembly list in HybridCLR Settings is empty, a dialog asks whether to restore from snapshot
- Clicking **Restore from Snapshot** will automatically configure:
  - `hotUpdateAssemblyDefinitions` → `[HotUpdate]`
  - `patchAOTAssemblies` → `[UniTask, UnityEngine.CoreModule, YooAsset, mscorlib]`
- Settings assets are automatically created and collector paths are normalized

**Step 3: Manual Initialization (Optional)**

If automatic initialization was not triggered, manually run the menus:

1. `HybridTool/Sample-HotUpdateSample/Restore HybridCLR Settings from Snapshot`
2. `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`

**Step 4: Configure and Build**

1. Fill in `HostServerIP` in `HybridRuntimeSettings`
2. Execute build via `HybridTool/` menu

#### Runtime Flow

```text
HybridLauncher → GameManager → PatchOperation (8-step state machine)
    → Initialize YooAsset packages
    → Request remote version number
    → Update resource manifest
    → Download resource packages
    → Load AOT metadata (to support hot-update generic functions)
    → Load hot-update DLLs (HybridCLR)
    → Instantiate HotUpdateLauncher, enter hot-update logic
```

### BuildTests — Build Pipeline Tests

An NUnit EditMode test suite for validating build configuration and pipeline correctness.

#### Usage Steps

1. Import **Build Pipeline Tests** sample from Package Manager
2. Open Unity Test Runner (`Window > General > Test Runner`)
3. Test assembly `com.yanglingyun.hyu.Tests.Editor` only compiles when `UNITY_INCLUDE_TESTS` is defined

#### Test Coverage

| Test Category | Description |
|---|---|
| **BuildConfig** | HybridCLR configuration existence, hot-update/AOT assembly lists, build scene list, project path validity |
| **BuilderSettings** | `HybridBuilderSettings` asset existence, `RuntimeSettings` association, build output path resolution, version format |
| **RuntimeSettings** | `HybridRuntimeSettings` asset existence, `HostServerIP` configuration |
| **Platform Tests** | Platform-parameterized tests (Windows / Android / iOS): DLL output paths, AOT stripping paths, cross-platform path uniqueness |
| **FirstBuildPrerequisites** | First-build prerequisite validation: AOT stripping directory, `GenerateAll` completeness, `MetadataCheck` pass |
| **VersionLogic** | Version auto-increment correctness, `GetCurrentVersion` build/display dual format, output path sync after version bump |
| **PipelineTypeValidation** | `HybrdiScriptableBuildPipeline` throws exception when passed illegal parameter types |
| **CopyDllEdgeCases** | `CopyPatchedAOTDll` / `CopyHotUpdateDll` empty path defense, `CopyDllFileToByte` returns empty list when source directory missing |
> Tests marked with `[Category("SlowTest")]` actually execute build commands and take longer; they are automatically skipped when the active platform does not match.

#### Test Boundary Notes

Tests only verify this package's own functionality (environment config, Editor methods, version logic, DLL compilation/copy, etc.). YooAsset resource packaging (`ScriptableBuildPipeline.Run()`), APK building (`BuildHelper.BuildAPK()`), and similar operations are the responsibility of their respective third-party frameworks or Unity build pipelines, covered by their own tests and not within this test scope.

---

## FAQ

### Q1: Hot-update code cannot access generic methods in AOT code?

This is because generic methods require additional metadata support. Solutions:

1. **Explicit invocation** — Explicitly call the generic method in hot-update code
2. **Manual configuration** — Add type preservation settings in link.xml
3. **Tool assistance** — Use the `HybridTool/Supplement Prefab Dependencies` feature

### Q2: "Missing AOT metadata" error during build?

1. Use `HybridTool/Check AOT Metadata` to validate whether metadata needs supplementation
2. Run `HybridTool/Generate AOT DLLs and Copy` to generate AOT supplementary files
3. Rebuild the APK

### Q3: "Method not found" error at runtime in hot-update code?

Possible causes and solutions:
- **Version mismatch** — Ensure hot-update DLLs and AOT metadata versions are consistent
- **Configuration issue** — Check if link.xml configuration is correct
- **Rebuild** — Rebuild the APK to update metadata

### Q4: MetadataCheck fails on first build?

Run the sample menu's Snapshot restore and Collector normalization first, then trigger the `GenerateAll` prerequisite chain before performing the hot-update build.

### Q5: Collector paths are wrong after importing sample?

Run: `HybridTool/Sample-HotUpdateSample/Normalize Collector Paths`

---

## Best Practices

### Assembly Partitioning Recommendations

#### AOT Assemblies (Stable, infrequently changed)
- Core business logic
- Third-party library wrappers
- Unity API abstraction layer
- Interface definitions and data structures
- Event systems

#### Hot-Update Assemblies (Frequently updated)
- Gameplay logic
- UI implementation
- Configuration data parsing

### Build Optimization Tips

- Leverage YooAsset's incremental packaging — leaving `Clear Build Cache` unchecked significantly speeds up builds
- Once the AOT portion is stable, daily iterations only require hot-update package builds
- Bridge functions are deterministic for a fixed AOT portion — hot-updates will not introduce new bridge function requirements

---

## License

MIT

---

<div align="center">

*If you have questions, please submit an [Issue](https://github.com/YangLingCloud/HybridCLR_YooAsset_UniTask/issues)*

*Happy Coding!*

</div>
