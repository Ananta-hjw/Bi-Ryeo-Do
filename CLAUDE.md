# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"비뢰도" (Bi-Ryeo-Do): a personal, single-player Android game in Unity. Genre: wuxia + turn-based battles + incremental growth with reincarnation (제N생). Inspired by the mobile game 수련협객 (ink-wash art, 5-tab layout: 무공/경영/전투/기화/행낭, currency 두). Never published, so copyright/store concerns are out of scope. Design notes and the screenshot analysis live in `Docs/GAME_DESIGN.md`; the user supplies the story separately (goes into `StageDef.storyText`).

Reply to the user in Korean (see `.claude/settings.json`); keep code identifiers and comments in English.

## Story

All lore lives in `Story/` (see `Story/README.md`). The user provides the original text as 28 volumes in `Story/raw/` (`vol01`…`vol28`); never edit those files.

- Before writing any story-dependent content (stage names/`storyText`, enemies, bosses, dialogue, 기화 events, martial-art lore), read `Story/INDEX.md`, then the relevant `Story/analysis/volXX.md`, `Story/CHARACTERS.md` and `Story/GAME_MAPPING.md`. Do not invent canon; if the analysis doesn't cover it, say so and ask.
- When asked to analyze a volume, follow the procedure in `Story/README.md`: one volume at a time, fill `analysis/_TEMPLATE.md`, split events into main story vs. substories, then update `MAIN_STORY.md`, `SUBSTORIES.md`, `CHARACTERS.md`, `GAME_MAPPING.md` and the `INDEX.md` status table.
- Record which game assets carry which story beats in `Story/GAME_MAPPING.md` so content stays traceable to the source volume.

## Environment

- Unity 2022.3 LTS (`ProjectSettings/ProjectVersion.txt` pins 2022.3.62f1; if the installed editor differs, Unity will offer to upgrade). Target: Android, portrait, IL2CPP, ARM64.
- The Unity Editor is NOT available in the Claude cloud container, so `Game/` and `Editor/` code cannot be compiled or run here. Only `Core/` is verifiable (see below). Say so when changes touch Unity-side code.
- First time in the editor: open the folder as a Unity project, then run menu **BiRyeoDo ▸ Setup Project** (`Scripts/Editor/ProjectSetup.cs`). It applies Android player settings, creates sample ScriptableObjects in `Assets/_Project/Resources/Data`, and creates/registers `Scenes/Boot.unity` (GameManager + a UI object with `TabRouter` + `MainScreen`).
- Unity creates `.meta` files on first import; commit them together with their assets. `Library/`, `Temp/`, `Builds/` are gitignored.

## Commands

Unity-free logic tests (works in the container; `dotnet-sdk-8.0` via `apt-get update && apt-get install -y dotnet-sdk-8.0`). Compile Core + tests with a throwaway csproj outside the repo:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework><LangVersion>9.0</LangVersion></PropertyGroup>
  <ItemGroup>
    <Compile Include="/path/to/repo/Assets/_Project/Scripts/Core/*.cs" />
    <Compile Include="/path/to/repo/Assets/_Project/Tests/EditMode/*.cs" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
  </ItemGroup>
</Project>
```

Run `dotnet test` (add `--filter Name~SkillRespectsCooldownAndQi` for one test). Don't commit that csproj (`*.csproj` is gitignored).

In the Unity editor: Window ▸ General ▸ Test Runner ▸ EditMode. Headless: `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -logFile -`. Android build: File ▸ Build Settings ▸ Android (or `Unity -batchmode -buildTarget Android ...`).

## Architecture

Two layers, split by assembly definition so game logic stays testable without Unity:

- `Assets/_Project/Scripts/Core` (`BiRyeoDo.Core`, `noEngineReferences: true`): pure C#, no `UnityEngine`. Battle engine, stats, progression, save data model. Must not reference Unity types; keep it compilable as plain .NET (C# 9, no records/nullable annotations since Unity's compiler is older).
- `Assets/_Project/Scripts/Game` (`BiRyeoDo.Game`): MonoBehaviours, ScriptableObjects, UI, persistence. References Core and TextMeshPro.
- `Assets/_Project/Scripts/Editor` (`BiRyeoDo.Editor`, editor-only): setup tooling.
- `Assets/_Project/Tests/EditMode`: NUnit tests of Core (assembly references Core only).

Key flow:
- `BattleEngine` (Core) is an action-gauge turn system: each tick units gain `Speed`; at 1000 they act (pick highest-power usable `SkillSpec`, target lowest-HP enemy). It runs the whole fight deterministically from an `IRandom` and returns a `BattleResult` containing a list of `BattleEvent`s.
- `BattleController` (Game) calls the engine once, then plays the events back with delays and fires `EventPlayed`/`BattleFinished` for the UI. The engine never touches UI or time; keep animation concerns in the controller/UI.
- Content is data-driven: `MartialArtDef`/`EnemyDef`/`StageDef` ScriptableObjects convert to Core types (`ToSpec()`, `Create()`). Add content as assets, not code.
- `SaveData` (Core, plain `[Serializable]` public fields for `JsonUtility`) is owned by `GameManager` (singleton, DontDestroyOnLoad) and written by `SaveService` to `persistentDataPath/save.json` (atomic temp-file swap), on battle win, pause, and quit. Bump `SaveData.Version` and migrate when changing the schema.
- `Progression` holds the growth formulas and reincarnation rule; balance numbers are placeholders documented in `Docs/GAME_DESIGN.md`. Update that doc when changing rules.
- Content is loaded at runtime by `ContentDatabase` via `Resources.LoadAll` from `Assets/_Project/Resources/Data`, so every StageDef/MartialArtDef/EnemyDef asset must live there (stages are ordered by `number`; arts are looked up by `id`; `bido_throw` is the starter art equipped by `GameManager`). The early-game content (7 stages, arts, enemies from volumes 1-3) is defined as plain data in `Core/EarlyGameContent.cs`; **BiRyeoDo ▸ Rebuild Story Content** regenerates the ScriptableObject assets from it (deleting everything in `Resources/Data`), so edit that file, not the assets. Tune balance with a throwaway console project that auto-plays the stages against `BattleEngine`.
- UI is built from code, not scenes/prefabs: `MainScreen` creates the Canvas (1080x1920 reference, safe-area aware), the bottom tab bar and one panel per tab; `BattleScreen.Build(panel)` creates the 전투 tab widgets and runs the auto-battle loop (normal enemy → fight → rewards → repeat; `StageProgress` unlocks the 보스 도전 button; beating the boss loads the next stage). `UiFactory` holds the helpers and ink-wash palette. Text uses the legacy `UnityEngine.UI.Text` with `Font.CreateDynamicFontFromOSFont` (Noto CJK/Malgun) so Hangul renders without a TMP font asset; switch to a TMP font asset only if a stylized font is imported.
- To type-check `Game/` outside Unity: compile `Core/*.cs` + `Game/**/*.cs` in a throwaway netstandard2.1 project referencing NuGet `UnityEngine.Modules` 2021.3.33 plus a small hand-written stub for `UnityEngine.UI`/`EventSystems` (uGUI isn't on NuGet). This catches API/typo errors but not runtime layout problems.
- Navigation is a bottom `TabRouter` with five panels indexed by its `Tab` enum order (MartialArts, Management, Battle, Fortune, Inventory).
