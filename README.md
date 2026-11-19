# SkyrimMelee

A Skyrim-inspired greatsword duel between two white primitive fighters — with a twist: **the bodies never articulate.** No rig, no limbs, no walking. Every attack, block, stagger and kill is expressed through the blade's arc; reactions tip or topple the whole body as one rigid piece. All geometry is generated at runtime from 3D primitives.

**Hard rules**

- **No 3D models.** Everything is built from DigitalRiseModel primitives (box, capsule, sphere, cylinder, cone, disc, plane).
- **No skeletal animation.** The body is a rigid white primitive assembly (head, torso, hips, legs) with no joints. Reaction clips may tip or topple it as a whole; a keyframed hand pivot moves the sword.

## Project state

**Last updated:** 2026-11-20 · **Phase:** 2 — sword motion · **Code:** solution, clock, Myra, renderer, arena, third-person camera, primitive fighters, swords, HUD, clip player, attack tuning, guard clips, shared blade geometry, combat FSM (top-level Tuning, Animation, Physics, Attacks, Combat, SelfTest), four attack clips (SlashLeft, SlashRight, PowerSlash overhead, Bash), reaction clips with rigid body motion, Bepu hurtboxes, swept blade cast, per-swing hit events, guard classification, poise/stagger rules, block bash, headless self-test scenarios, health/stamina bars, stamina economy, damage model, defense outcomes, duel director, input buffer, WASD movement and follow camera, move-while-attacking/guarding (bash roots the fighter), alternating slash combo, Skyrim stamina gate, player-only sword motion, opponent hit/death reactions, whole-body hit flash, pause status · **Next:** `T5.3`

| Area | State |
| --- | --- |
| Docs | ✅ README, `Design/`, TASKS, AGENTS |
| Toolchain | ✅ `T0.1` |
| Solution & skeleton | ✅ `T0.2` |
| Tests | ✅ xUnit project, 205 passing |
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
| Code layout (top-level folders) | ✅ `T5.7` |
| HUD & menus | ⬜ `T7.1` |

## Stack

| Layer | Library |
| --- | --- |
| Runtime | MonoGame 3.8.6.1 (DesktopGL), `net10.0` |
| Geometry | DigitalRiseModel 0.7.6 (primitives, no renderer) |
| UI | Myra 1.6.8 |
| Collision | BepuPhysics2 2.4.0 (static hurtboxes + swept blade) |

## Documentation

Detailed specifications live in [`Design/`](Design/overview.md): [overview](Design/overview.md), [architecture](Design/architecture.md), [combat](Design/combat.md), [Skyrim reference](Design/reference.md), [UI & AI](Design/ui-and-ai.md), [testing](Design/testing.md). Start there for anything beyond the summary above. The work plan is in [TASKS.md](TASKS.md); the agent workflow is in [AGENTS.md](AGENTS.md).

## Build & run

Requires .NET SDK 10.0.4xx+ and a GPU with OpenGL 3.3.

```bash
dotnet build SkyrimMelee.slnx
dotnet test SkyrimMelee.slnx
dotnet run --project SkyrimMelee/SkyrimMelee.csproj
dotnet run --project SkyrimMelee/SkyrimMelee.csproj -- --self-test   # headless combat tests
```

`--debug` (overlay on launch) · `--god` · `--slowmo` · `--no-ui` · `--self-test` · `--help`

UI markup lives in `SkyrimMelee/Ui/Hud.xmmp` and `DebugOverlay.xmmp`; after editing, run `myra-export <file>.xmmp`.

**Controls:** `WASD` move · mouse look · `LMB` tap slash combo / hold power slash · `F`/LMB while blocking bash · `1`/`2` forced arcs · `RMB` block · `4`–`8` reaction previews · `F1` debug overlay · `F2` wireframe · `Esc` quit.

## License

See [LICENSE](LICENSE).
