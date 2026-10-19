# Repository Guidelines

## Project Structure & Module Organization

Bi-Ryeo-Do is a single-player Android game using Unity 2022.3.62f1. Source lives under `Assets/_Project/Scripts/`:

- `Core/`: Unity-independent battle, progression, inventory, and story data. Keep this assembly free of Unity references.
- `Game/`: Unity components, UI, and persistence.
- `Editor/`: project setup and Android build tooling.

NUnit tests live in `Assets/_Project/Tests/EditMode/`. Consult `Docs/` for design and art guidance, `Story/` for source material, and `CLAUDE.md` for detailed development rules. `Packages/manifest.json` declares Unity dependencies.

## Build, Test, and Development Commands

Use the pinned Unity editor with Android build support. Open the checkout and run **BiRyeoDo > Setup Project** to generate the Boot scene and configure Android settings; enter Play mode to run locally.

From the repository root:

```bash
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults /tmp/biryeodo-tests.xml -logFile -
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod BiRyeoDo.Editor.BuildScript.BuildAndroid -logFile -
```

The build produces `build/Android/BiRyeoDo.apk`. Android CI uses GameCI and requires Unity licensing credentials.

Without Unity, follow `CLAUDE.md` to create an external .NET 8 test project compiling Core and EditMode sources with NUnit 3.14. Run `dotnet test <project.csproj>`; add `--filter Name~SkillRespectsCooldownAndQi` for a focused check. This validates Core only.

## Coding Style & Naming Conventions

Use four-space indentation, braces on separate lines, PascalCase types/methods/properties, and `_camelCase` private fields. Match adjacent code. Keep Core compatible with C# 9; avoid records and nullable annotations. No formatter or linter configuration is checked in.

Keep identifiers and comments in English. Write player-facing guidance in polite Korean; preserve narrative dialogue style.

## Testing Guidelines

Name fixtures `*Tests.cs` and tests descriptively. Add regression tests for changed Core behavior and use injectable randomness/clocks for determinism. Run relevant tests, then the Core suite. No coverage percentage is specified. Check Unity UI changes in Play mode; .NET tests cannot validate rendering.

## Commit & Pull Request Guidelines

History uses descriptive English subjects, often imperative, without a fixed prefix scheme. Keep commits focused. PRs should explain behavior changes, validation and limitations, link relevant issues, and include screenshots for UI changes.

## Content & Asset Rules

Never edit `Story/raw/`. Before story-dependent changes, read `Story/INDEX.md` and relevant analyses; maintain `Story/GAME_MAPPING.md`. Commit Unity `.meta` files with assets. Keep generated caches and credentials out of Git.
