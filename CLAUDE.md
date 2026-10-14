# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"비뢰도" (Bi-Ryeo-Do): a personal, single-player Android game in Unity. Genre: wuxia + **player-driven turn-based battles** + growth with reincarnation (제N생). The user explicitly does NOT want an idle/auto-battle game: every turn the player picks a command (attack, martial art, guard, meditate), chooses the target, and fights should feel fast. Visual mood only is borrowed from 수련협객 (ink-wash look, currency 두). Never published, so copyright/store concerns are out of scope. Design notes and the screenshot analysis live in `Docs/GAME_DESIGN.md`; the user supplies the story separately (goes into `StageDef.storyText`).

Reply to the user in Korean (see `.claude/settings.json`); keep code identifiers and comments in English.

## Story

Confirmed by the user: the protagonist is 비류연 and the heroine is 나예린 (the 빙백봉 of volume 3).

All lore lives in `Story/` (see `Story/README.md`). The user provides the original text as 28 volumes in `Story/raw/` (`vol01`…`vol28`); never edit those files.

- Before writing any story-dependent content (stage names/`storyText`, enemies, bosses, dialogue, 기화 events, martial-art lore), read `Story/INDEX.md`, then the relevant `Story/analysis/volXX.md`, `Story/CHARACTERS.md` and `Story/GAME_MAPPING.md`. Do not invent canon; if the analysis doesn't cover it, say so and ask.
- When asked to analyze a volume, follow the procedure in `Story/README.md`: one volume at a time, fill `analysis/_TEMPLATE.md`, split events into main story vs. substories, then update `MAIN_STORY.md`, `SUBSTORIES.md`, `CHARACTERS.md`, `GAME_MAPPING.md` and the `INDEX.md` status table.
- Record which game assets carry which story beats in `Story/GAME_MAPPING.md` so content stays traceable to the source volume.

## Environment

- Unity 2022.3 LTS (`ProjectSettings/ProjectVersion.txt` pins 2022.3.62f1; if the installed editor differs, Unity will offer to upgrade). Target: Android, portrait, IL2CPP, ARM64.
- The Unity Editor is NOT available in the Claude cloud container, so `Game/` and `Editor/` code cannot be compiled or run here. Only `Core/` is verifiable (see below). Say so when changes touch Unity-side code.
- First time in the editor: open the folder as a Unity project, then run menu **BiRyeoDo ▸ Setup Project** (`Scripts/Editor/ProjectSetup.cs`). It applies Android player settings and creates/registers `Scenes/Boot.unity` (GameManager + a UI object with `TabRouter` + `MainScreen`). Content needs no assets: it is plain data in `Core/EarlyGameContent.cs`. **BiRyeoDo ▸ Build Android APK** (also used by CI) runs the setup first.
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
- `Assets/_Project/Scripts/Game` (`BiRyeoDo.Game`): MonoBehaviours, UI, persistence. References Core and TextMeshPro.
- `Assets/_Project/Scripts/Editor` (`BiRyeoDo.Editor`, editor-only): setup tooling.
- `Assets/_Project/Tests/EditMode`: NUnit tests of Core (assembly references Core only).

Key flow:
- `InteractiveBattle` (Core) is the player-driven battle for a party of 1-4: an action-gauge turn order (each tick units gain `Speed`; at 1000 they act). `Begin()` runs enemy turns until a party member's turn (`Current`), `PlayerAct(PlayerCommand)` resolves that member's command and then every enemy turn up to the next party turn, returning `BattleEvent`s for the UI to animate. Commands: use a martial art (single target, multi-hit, area, Evade, GuardParty, Stun, HealAlly), Guard (damage x0.4), Meditate (내공 + small heal). Companions (`CompanionData`: 효룡, 장홍, 윤준호) join at fixed stages with fixed arts (`ArtData.OwnerId`); their stats follow the protagonist's level. Resources: HP, 내공 (qi, regen each turn), 기세 (spirit, fills from hitting/being hit; 비기 cost 100). Enemies follow a cyclic `EnemyMove` script; a `Charge` move telegraphs the following heavy move and its victim (`Combatant.Charging`, `ChargeTarget`, `PeekMove()` drive the intent display); Stun interrupts a charge (bosses are interrupted but never stun-locked). The old auto `BattleEngine` remains for `CalcDamage` and balance tests.
- `BattleScreen` (Game) is a full-screen overlay opened from the 강호 tab: story intro, a chain of fights (rest between fights: +50% HP, full 내공), rewards, outro. It only draws and animates the events; rules stay in Core. Speed toggle x1/x2/x3.
- Story-only scenes: `StageData.Prologue` (`StoryScene` list) plays before a stage's Intro until the stage is first cleared (used to bridge volumes: 아미산 합숙 before stage 2, 가출 before stage 5, 운향정의 소녀 before stage 9). Stage numbers are the identity for saves/rewards/unlocks, so add story via Prologue instead of renumbering.
- Content is plain data in `Core/EarlyGameContent.cs` (arts, companions, enemies, stages 1-13 from volumes 1-5; rewards derive from each stage's `RecommendedLevel`/`DooTotal`). No ScriptableObjects. Tune balance with a throwaway console project that auto-plays stages with a bot against `InteractiveBattle` (see `Tests/EditMode/SimpleBot.cs`; `EarlyGameContentTests` asserts every stage is clearable by it at its recommended level).
- Items: `ItemData` (consumable / weapon / accessory / material / key) lives in `EarlyGameContent.Items` with drop tables (`Drops`), first-clear rewards (`StageRewards`) and a `StartKit`. `Inventory` (Core) is the logic over `SaveData` (bag stacks, equip rules: 1 weapon + 2 accessories for the protagonist only, `ApplyGear`, selling materials, `RollDrops`, `GrantStageReward`). Consumables are used in battle as a `PlayerCommand.UseItem` (heals the most wounded ally / restores the user's 내공 and 기세); the UI removes the item from the bag afterwards. The 행낭 tab (`BagScreen`) handles gear and selling. The balance bot ignores items on purpose, so items are pure upside.
- `SaveData` (Core, plain `[Serializable]` public fields for `JsonUtility`; additive fields such as `Items` need no version bump, `ClearedStage` tracks progress, free arts are auto-learned by `GameManager`) is owned by `GameManager` (singleton, DontDestroyOnLoad) and written by `SaveService` to `persistentDataPath/save.json` (atomic temp-file swap), on battle win, pause, and quit. Bump `SaveData.Version` and migrate when changing the schema.
- `Progression` holds the growth formulas and reincarnation rule; balance numbers are placeholders documented in `Docs/GAME_DESIGN.md`. Update that doc when changing rules.
- UI is built from code, not scenes/prefabs: `MainScreen` creates the Canvas (1080x1920 reference, safe-area aware), a three-tab bar (`WorldScreen` stage map, `MartialArtsScreen` learn/equip, `BagScreen` items) and the `BattleScreen` overlay. `UiFactory` holds the helpers and ink-wash palette. Text uses the legacy `UnityEngine.UI.Text` with `Font.CreateDynamicFontFromOSFont` (Noto CJK/Malgun) so Hangul renders without a TMP font asset.
- Artwork is optional and matched by file name via `ArtLibrary`: `Resources/Art/Backgrounds/stage_NN.png`, `Enemies/<enemy id>.png`, `Characters/player.png` (`Characters/<companion id>.png` for party members; missing files fall back to flat-colour placeholders). Names, sizes and AI prompts are in `Docs/ART_GUIDE.md`.
- To type-check `Game/` outside Unity: compile `Core/*.cs` + `Game/**/*.cs` in a throwaway netstandard2.1 project referencing NuGet `UnityEngine.Modules` 2021.3.33 plus a small hand-written stub for `UnityEngine.UI`/`EventSystems` (uGUI isn't on NuGet). This catches API/typo errors but not runtime layout problems.
- Navigation is a bottom `TabRouter` with three panels indexed by its `Tab` enum order (World, MartialArts, Bag).
