# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"비뢰도" (Bi-Ryeo-Do): a personal, single-player Android game in Unity. Genre: wuxia + **player-driven turn-based battles** + growth by levelling and learning martial arts (no reincarnation). The user explicitly does NOT want an idle/auto-battle game: every turn the player picks a command (attack, martial art, guard, meditate), chooses the target, and fights should feel fast. Visual mood only is borrowed from 수련협객 (ink-wash look, currency 두). Never published, so copyright/store concerns are out of scope. Design notes and the screenshot analysis live in `Docs/GAME_DESIGN.md`; the user supplies the story separately (goes into `StageDef.storyText`).

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
- `InteractiveBattle` (Core) is the player-driven battle for a party of 1-4: an action-gauge turn order (each tick units gain `Speed`; at 1000 they act). `Begin()` runs enemy turns until a party member's turn (`Current`), `PlayerAct(PlayerCommand)` resolves that member's command and then every enemy turn up to the next party turn, returning `BattleEvent`s for the UI to animate. Commands: use a martial art (single target, multi-hit, area, Evade, GuardParty, Stun, HealAlly), Guard (damage x0.4), Meditate (내공 + small heal). Companions (`CompanionData`: 효룡, 장홍, 윤준호, 임성진) join at fixed stages with fixed arts (`ArtData.OwnerId`); their stats follow the protagonist's level. Only `EarlyGameContent.MaxCompanions` (3) fight at once: `SelectCompanions(stage, SaveData.ActiveCompanions)` takes the player's picks (set in the 강호 tab's 파티 편성 row), then fills by join order; the balance bot uses the default (join order). Resources: HP, 내공 (qi, regen each turn), 기세 (spirit, fills from hitting/being hit; 비기 cost 100). Enemies follow a cyclic `EnemyMove` script; a `Charge` move telegraphs the following heavy move and its victim (`Combatant.Charging`, `ChargeTarget`, `PeekMove()` drive the intent display); Stun interrupts a charge (bosses are interrupted but never stun-locked). A story boss can have `EnemyData.RetreatHpPct` (stage 30's 대공자 비 = 0.3): at that HP fraction it leaves the fight (`BattleEvent.Retreated`), so the battle is won but nobody is slain. Story trials (시험) are normal fights with a `TrialRule` (`StageData.Trials`, parallel to `Encounters`): Survive (endure N enemy actions, 검후 십초지적), Deadline (win within N party actions, 목요관), Lifeline (each landed charged move cuts the rope, N cuts lose, 금요관), Harmony (an unbroken chain of attacks by the whole party powers the next hit x2.5, plus a deadline, 수화관); `InteractiveBattle.Trial` must be set before `Begin()` and `TrialStatus` feeds the HUD. The old auto `BattleEngine` remains for `CalcDamage` and balance tests.
- `BattleScreen` (Game) is a full-screen overlay opened from the 강호 tab: story intro, a chain of fights (rest between fights: +50% HP, full 내공; after each win a `ShowHub` pause lets the player challenge the next fight or step back; stepping back stores a resume point via Core `StageProgress` (one slot: next fight index + party HP), and the 강호 row shows 이어하기), rewards, outro. It only draws and animates the events; rules stay in Core. Speed toggle x1/x2/x3.
- Story-only scenes: `StageData.Prologue` (`StoryScene` list) plays before a stage's Intro until the stage is first cleared (used to bridge volumes: 아미산 합숙 before stage 2, 가출 before stage 5, 운향정의 소녀 before stage 9). Stage numbers are the identity for saves/rewards/unlocks, so add story via Prologue instead of renumbering.
- 스토리 대사 형식(Core `StoryLine`): `Intro`/`Outro`/`StoryScene.Lines`의 한 줄이 `화자|대사`이면 대화(초상화+이름표+타자 효과), 그냥 문장이면 지문이다. 비류연은 왼쪽, 상대는 오른쪽에 뜬다. 초상화는 `Characters/player`, 동료는 `Characters/<id>`, 그 외 인물은 이름이 일치하는 적의 `Enemies/<ArtKey>` 그림을 쓰고 없으면 이름 첫 글자 색 블록이다. 화자 이름은 8자 이하.
- Content is plain data in `Core/EarlyGameContent.cs` (arts, companions, enemies, stages 1-30 from volumes 1-16; rewards derive from each stage's `RecommendedLevel`/`DooTotal`). No ScriptableObjects. Tune balance with a throwaway console project that auto-plays stages with a bot against `InteractiveBattle` (see `Tests/EditMode/SimpleBot.cs`; `EarlyGameContentTests` asserts every stage is clearable by it at its recommended level).
- Items: `ItemData` (consumable / weapon / accessory / material / key) lives in `EarlyGameContent.Items` with drop tables (`Drops`), first-clear rewards (`StageRewards`) and a `StartKit`. `Inventory` (Core) is the logic over `SaveData` (bag stacks, equip rules: 1 weapon + 2 accessories for the protagonist only, `ApplyGear`, selling materials, `RollDrops`, `GrantStageReward`). Consumables are used in battle as a `PlayerCommand.UseItem` (heals the most wounded ally / restores the user's 내공 and 기세); the UI removes the item from the bag afterwards. The 행낭 tab (`BagScreen`) handles gear and selling. The balance bot ignores items on purpose, so items are pure upside.
- 승급 비무: `Promotion` (Core) 검룡위 1-10(`SaveData.SwordRank`, 레벨과 별개), 비무첩(`PromotionOffer`: 입장료·상금·상대 2명), 12턴 제한 무승부, 오검룡 이상만 스테이지 12-13 참가. `BattleScreen.OpenPromotion`이 주인공 단독으로 동적 `EnemyData`와 싸운다(`StageData.Number 0` = 동료 없음). `WorldScreen`은 스크롤되며 맨 위에 승급 시험장 행이 있다.
- `SaveData` (Core, plain `[Serializable]` public fields for `JsonUtility`; additive fields such as `Items` need no version bump, `ClearedStage` tracks progress, free arts are auto-learned by `GameManager`) is owned by `GameManager` (singleton, DontDestroyOnLoad) and written by `SaveService` to `persistentDataPath/save.json` (atomic temp-file swap), on battle win, pause, and quit. Bump `SaveData.Version` and migrate when changing the schema.
- 무공 숙련(`Mastery`, Core): 주인공 무공은 사용 횟수로 입문→소성(10)→대성(30)→극의(70), 단계마다 위력 +5%. 대성부터 변초(강/쾌/연)를 무공 탭에서 고른다(비기·보조기 제외). 전투에서 `Mastery.Apply`로 스킬을 만들고 `RecordUse`로 센다. 균형 봇은 무시한다.
- 합공·인연(Core `Bonds`, `CommandKind.Joint`): 동료와 같은 스테이지를 클리어할 때마다 인연 +1(0/3/8/15 → 서먹/친분/신뢰/의기투합). 친분부터 주인공 차례에 기세 50으로 합공(주인공+동료가 연속 타격, 동료는 다음 행동 소모, 동료별 부가효과: 효룡 방어, 장홍 점혈, 윤준호 치유, 임성진 관통). 인연 레벨당 위력 +10%. `InteractiveBattle.CanJoint`/`PlayerCommand.Joint`, 합공 이름은 `CompanionData.JointName`.
- 의뢰·기연(Core `Quests`, `Fortunes`): 강호 탭의 의뢰 게시판은 돌파한 스테이지의 전투 하나에 시간 제한/버티기 규칙을 붙인 일회성 퀘스트(`Quests.ToStage`가 `StageData.IsQuest`로 변환, 스테이지 진행도와 무관, 경험치는 `Quests.ExpReward`, 두는 `RewardDoo`). 기연은 스테이지 돌파 후 열리는 선택지 이벤트(두·경험·아이템·인연·숙련 보상). 완료 기록은 `SaveData.DoneQuests/DoneFortunes`. 문구는 게임 연출이며 원작 설정이 아니다.
- `Progression` holds the growth formulas (levelling and the stat curve); balance numbers are placeholders documented in `Docs/GAME_DESIGN.md`. Update that doc when changing rules.
- UI is built from code, not scenes/prefabs: `MainScreen` creates the Canvas (1080x1920 reference, safe-area aware), a three-tab bar (`WorldScreen` stage map, `MartialArtsScreen` learn/equip, `BagScreen` items) and the `BattleScreen` overlay. `UiFactory` holds the helpers and ink-wash palette. Text uses the legacy `UnityEngine.UI.Text` with `Font.CreateDynamicFontFromOSFont` (Noto CJK/Malgun) so Hangul renders without a TMP font asset.
- Artwork is optional and matched by file name via `ArtLibrary`: `Resources/Art/Backgrounds/stage_NN.png`, `Enemies/<enemy id>.png`, `Characters/player.png` (`Characters/<companion id>.png` for party members; missing files fall back to flat-colour placeholders). Names, sizes and AI prompts are in `Docs/ART_GUIDE.md`.
- To type-check `Game/` outside Unity: compile `Core/*.cs` + `Game/**/*.cs` in a throwaway netstandard2.1 project referencing NuGet `UnityEngine.Modules` 2021.3.33 plus a small hand-written stub for `UnityEngine.UI`/`EventSystems` (uGUI isn't on NuGet). This catches API/typo errors but not runtime layout problems.
- Navigation is a bottom `TabRouter` with three panels indexed by its `Tab` enum order (World, MartialArts, Bag).
