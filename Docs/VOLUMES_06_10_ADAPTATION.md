# Volumes 6–10 Main-Story Adaptation

## Scope and sources

Based on `Story/analysis/vol06.md` through `vol10.md`, cross-checked with the character and main-story notes. Original source files and analyses are untouched. All 31 stage IDs remain available, including the remotely added stage 31. Stages 14–20 now contain **29 main-story fights instead of 20**; these use the current party, growth and equipment, with ordinary rewards rather than fixed-stat side scenarios.

`Core/StoryMidgame.cs` centralizes prologues, introductions, conclusions, briefings, interludes, aftermaths and 29 encounter-adjacent scene groups. First-clear, resumed playback and Records replay share that data. Quest, tower and isolated-fight suppression remains in place.

## Playable sequence

| Stage | Fights | Adaptation |
|---|---|---|
| 14 | 3→8 | Azure/Vermilion team match, river assassins, Red Bear checkpoint, Heuk Sang, first Gal Hyobong survival clash, disguised raids, second rampage, twin killers |
| 15 | 3→4 | Added twin-spear vanguard, then scouts, cavalry/deputy and Gu Cheonhak's representative duel |
| 16 | 3 | Memorial after cavalry withdrawal, treaty-backed inspector and market escort |
| 17 | 3 | Pang Yukyeong, Sinung squad and Pang Hyeokseong replace the invented full Maharyeong boss duel |
| 18 | 2→3 | White-dragon sword clash, added rope sparring with Hyo Ryong, separate Ice Sword duel |
| 19 | 3→5 | Separate Yun Junho, Moyong Hwi, Hyo Ryong, Naye Rin and protagonist illusion scenes; added phantom Sword Saint and Gal Hyobong fights |
| 20 | 3 | Natural-cave survival, day-26 beast, day-44 escape/resuscitation, empty-coffin funeral and Mount Hua departure |

The first Gal clash requires surviving eight enemy actions; guarding and meditation are available. Its outcome remains a withdrawal, not the brother's death. New opponents use normal enemy curves, realm bonuses and difficulty scaling; existing battle art is reused. Pang Hyeokseong receives the final boss's phase and reward rules. New fight speed-star limits follow measured party-action counts.

## Continuity corrections

Hyeongeomja is distinct from punctual instructor Mujinja; the golden net is a clue, not a confirmed revelation of the master's identity. Twin killers' right-arm injuries follow volume 7. Gal's last words precede Hyo's farewell. The cavalry withdraws before the memorial rather than being exterminated. The inspector's double-agent explanation remains a lie told to Cheong Heun.

Maharyeong's guards and later opponents are kept distinct. Yong uses the white-dragon sword while the relic stays sheathed. Ice Sword consistently offers three requests, one spent on discipleship; phase dialogue agrees. Renamed 백빙신첨 retains Chill, with the old spelling as an alias. The inspector remains outside the private illusion trial. Naye Rin's father/daughter relationship, day-seven address, day-ten natural cave, day-44 rescue and zero final deaths are consistent. Memory IDs, payouts and unlocks remain; earlier Wudang memories are marked as flashbacks.

## Growth and rewards

Added fights pay normal experience, 두, stars and material drops. Original reward denominators are retained for expanded stages, so added fights increase the chapter total rather than splitting the old payout. Equipment, martial-art and companion unlocks keep their existing stage gates.

Actual normal first-run rewards, with no faction perks, including the existing income boosts and per-fight rounding:

| Stage | Experience before→after | 두 before→after |
|---|---:|---:|
| 14 | 1,796→3,056 | 18,250→31,000 |
| 15 | 1,969→2,407 | 19,650→24,020 |
| 17 | 2,294→2,289 | 22,460→22,440 |
| 18 | 2,439→3,049 | 23,860→29,820 |
| 19 | 2,631→3,289 | 25,280→31,600 |

Stages 16/20 retain their fights and payouts. Stage 17 redistributes its base budget across the replacement guards; small total differences come from rounding. Replays and higher difficulties still use their existing modifiers. More ordinary fights provide more material-roll opportunities; no new drop system is introduced.

## Existing saves

Story revision 2→3 remaps earned stars and final-fight completion across all difficulties, preserving first-boss material history. The removed mixed guard fight's earned stars become retired-star credit. Affected between-fight checkpoints restart the chapter; inventory, experience, currency, unlocked stages and other checkpoints remain. Lobby synchronization backfills only stages with no normal-fight records, preventing unplayed inserted fights from being awarded victory stars. Migration is idempotent.

## Validation and limits

Forty deterministic runs per expanded stage at the balance bot's prepared level, with start-kit equipment and no progression, produced 34/40 wins at stages 14/18 and 40/40 at 15/17/19. A continuous 14–21 run with actual experience, currency and mastery progression cleared 10/40 times with the additions versus 4/40 without those added fights (same revised stage-17 guards in both cases). Successful expanded runs ended at level 21 with about 65% party HP after stage 21. This deliberately limited bot ignores equipment upgrades, training and tactical items; it is a regression probe, not a player win-rate estimate. The existing Ice Sword fight remains the main bottleneck for that restricted build.

Final Core: 691 tests, 676 passed; the remaining 15 failures have the same names and messages as the 661-test pre-change baseline (646 passed). All 30 added tests pass. Game/UI and UI-test C# compilation succeeds with no errors. `git diff --check` passes. Unity rendering, pagination and hands-on difficulty require the Editor; .NET validation cannot exercise those.
