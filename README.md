# SkyrimMelee

A Skyrim-inspired greatsword duel. The current build uses white primitive fighters and a keyframed sword; M9 replaces both with authored glTF assets and skeletal animation. Until T9.8 flips the default, the primitive build remains the shipping path.

Combat stays deterministic at 60 Hz: authored clips supply poses only; attack phases, stamina, damage, and hit resolution remain in the existing rules.

**Hard rules**

- **Only the approved asset set may be imported.** The shipping build still uses primitives; once T9.8 lands, only `Content/Models/fighter.glb` and `sword.glb` are imported. Arena, VFX, and UI remain generated.
- **Skeletal animation is fighter-only.** Clips have no root motion and no IK; the sword is a static attachment on `weapon_r`. Combat timing remains authoritative in `CombatTuning`.

## Project state

**Last updated:** 2026-11-20 · **Phase:** M9 — authored models · **Code:** solution, clock, Myra, renderer, arena, third-person camera, primitive fighters, swords, HUD, clip player, attack tuning, guard clips, shared blade geometry, combat FSM (top-level Tuning, Animation, Physics, Attacks, Combat, SelfTest), four attack clips (SlashLeft, SlashRight, PowerSlash overhead, Bash), reaction clips with rigid body motion, Bepu hurtboxes, swept blade cast, per-swing hit events, guard classification, poise/stagger rules, block bash (interrupt-only stagger), headless self-test scenarios, health/stamina bars, stamina economy, damage model, defense outcomes, duel director, input buffer, WASD movement and follow camera, move-while-attacking/guarding (bash roots the fighter), alternating slash combo, Skyrim stamina gate, player-only sword motion, opponent hit/death reactions, whole-body hit flash, first/third person with wheel zoom, damage-scaled camera shake, F3 presets, debug overlay v2 (blade trail, contact marker, live tuning), reactive opponent AI (chase, guard, attack, punish), incoming attacks, 35° guard cone, difficulty profiles (Novice default, Adept, Master), headless AI duels, AI decision log and fight stats, main menu (Duel/Practice/Options/Rules/Quit), UiSkin with focus ring, practice mode, fade transitions, fight HUD (health ghost, stamina, round pips, banners), arena health potions, pause/options/results with session settings, rules screen, focus-loss pause and click latch, DrModel validation harness (`--validate-models`), pause status · **Next:** `T9.3`

| Area | State |
| --- | --- |
| Docs | ✅ README, `Design/`, TASKS, AGENTS |
| Toolchain | ✅ `T0.1` |
| Solution & skeleton | ✅ `T0.2` |
| Tests | ✅ xUnit project, 286 passing |
| UI host & debug label | ✅ `T0.3` |
| Loop, clock & flags | ✅ `T0.4` |
| Primitive library & showcase | ✅ `T1.1` |
| Arena & camera | ✅ `T1.2` |
| Fighter body & hand pivots | ✅ `T1.3` / `T3.1` (primitive assembly) |
| Greatswords & hand pivot | ✅ `T1.4` |
| HUD from Myra markup | ✅ `T1.5` |
| Sword clip system | ✅ `T2.1` |
| SlashLeft clip | ✅ `T2.2` |
| Guard clips | ✅ `T2.3` |
| Blade geometry | ✅ `T2.4` |
| Combat state machine | ✅ `T2.5` |
| SlashRight clip | ✅ `T2.6` |
| Three-attack moveset (PowerSlash overhead) | ✅ `T2.9` |
| Reaction clips (rigid capsule) | ✅ `T2.10` |
| Combat rules | ⬜ `T4.1` |
| Physics hurtboxes | ✅ `T3.1` |
| Swept blade cast | ✅ `T3.2` |
| Swing hit bookkeeping | ✅ `T3.3` |
| Blade-vs-blade blocks | ✅ `T3.4` |
| Headless self-test | ✅ `T3.5` |
| Stats & stamina | ✅ `T4.1` |
| Damage pipeline | ✅ `T4.2` |
| Poise, stagger & bash | ✅ `T4.3` |
| Timed block & guard break | ✅ `T4.4` |
| Death, rounds, reset | ✅ `T4.5` |
| Input buffering | ✅ `T5.1` |
| Player movement & camera modes | ✅ `T5.2` |
| First/third person & zoom | ✅ `T5.3` |
| Camera feel (shake, presets) | ✅ `T5.4` |
| Debug overlay v2 (trail, tuning) | ✅ `T5.5` |
| Opponent AI | ✅ `T6.1` |
| Difficulty profiles | ✅ `T6.2` |
| AI visualization | ✅ `T6.3` |
| Guard coverage cone | ✅ `T4.6` |
| Code layout (top-level folders) | ✅ `T5.7` |
| HUD & menus | ✅ `T7.1` |
| Fight HUD | ✅ `T7.2` |
| Arena potions | ✅ `T6.4` |
| Pause, options & results | ✅ `T7.3` |
| Rules screen | ✅ `T7.5` |
| Input ownership | ✅ `T7.4` |
| Authored models (M9) | 🚧 `T9.3` waiting for external fighter/sword assets |

## Stack

| Layer | Library |
| --- | --- |
| Runtime | MonoGame 3.8.6.1 (DesktopGL), `net10.0` |
| Geometry | DigitalRiseModel 0.7.6 (primitives, glTF importer, skinning/animation data; M9 adds stock `SkinnedEffect`) |
| UI | Myra 1.6.8 |
| Collision | BepuPhysics2 2.4.0 (static hurtboxes + swept blade) |

## Documentation

Detailed specifications live in [`Design/`](Design/overview.md): [overview](Design/overview.md), [architecture](Design/architecture.md), [combat](Design/combat.md), [Skyrim reference](Design/reference.md), [UI & AI](Design/ui-and-ai.md), [testing](Design/testing.md), [models](Design/models/overview.md). Start there for anything beyond the summary above. The work plan is in [TASKS.md](TASKS.md); the agent workflow is in [AGENTS.md](AGENTS.md).

## Build & run

Requires .NET SDK 10.0.4xx+ and a GPU with OpenGL 3.3.

```bash
dotnet build SkyrimMelee.slnx
dotnet test SkyrimMelee.slnx
dotnet run --project SkyrimMelee/SkyrimMelee.csproj
dotnet run --project SkyrimMelee/SkyrimMelee.csproj -- --self-test   # headless combat tests
dotnet run --project SkyrimMelee/SkyrimMelee.csproj -- --validate-models   # needs Content/Models assets
```

`--debug` (overlay on launch) · `--god` · `--slowmo` · `--no-ui` · `--play` (skip menu) · `--profile=<novice|adept|master>` · `--self-test` · `--validate-models` (`--models-dir=<path>`) · `--help`

UI markup lives in `SkyrimMelee/Ui/Hud.xmmp` and `DebugOverlay.xmmp`; after editing, run `myra-export <file>.xmmp`.

**Controls:** `WASD` move · mouse look · wheel zoom · `F` first/third person · `LMB` tap slash combo / hold power slash · `B`/LMB while blocking bash · `1`/`2` forced arcs · `RMB` block · `4`–`8` reaction previews · `F1` debug overlay · `F2` wireframe · `F3` camera presets · `[` `]` `;` tune timings (overlay) · `Esc` quit.

## License

See [LICENSE](LICENSE).
