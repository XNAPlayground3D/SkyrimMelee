# SkyrimMelee

A Skyrim-inspired greatsword duel between two white capsule fighters — with a twist: **the body never moves.** No rig, no limbs, no walking. Only the sword does. Every attack, block, stagger and kill is expressed through the blade's arc, and all geometry is generated at runtime from 3D primitives.

**Hard rules**

- **No 3D models.** Everything is built from DigitalRiseModel primitives (box, capsule, sphere, cylinder, cone, disc, plane).
- **No skeletal animation.** The body is a static white capsule; a keyframed hand pivot moves the sword and nothing else.

## Project state

**Last updated:** 2026-11-19 · **Phase:** 2 — sword motion · **Code:** solution, clock, Myra, renderer, arena, third-person camera, capsule fighters, swords, HUD, clip player, attack tuning, orbit-cut SlashLeft, guard clips, shared blade geometry, combat FSM, SlashLeft/Right, Overhead, PowerSlash, pause status · **Next:** `T2.9`

| Area | State |
| --- | --- |
| Docs | ✅ README, `Design/`, TASKS, AGENTS |
| Toolchain | ✅ `T0.1` |
| Solution & skeleton | ✅ `T0.2` |
| Tests | ✅ xUnit project, 70 passing |
| UI host & debug label | ✅ `T0.3` |
| Loop, clock & flags | ✅ `T0.4` |
| Primitive library & showcase | ✅ `T1.1` |
| Arena & camera | ✅ `T1.2` |
| Capsule fighter & hand pivots | ✅ `T1.3` (capsule placeholder) |
| Greatswords & hand pivot | ✅ `T1.4` |
| HUD from Myra markup | ✅ `T1.5` |
| Sword clip system | ✅ `T2.1` |
| SlashLeft clip | ✅ `T2.2` |
| Guard clips | ✅ `T2.3` |
| Blade geometry | ✅ `T2.4` |
| Combat state machine | ✅ `T2.5` |
| SlashRight clip | ✅ `T2.6` |
| Overhead clip | ✅ `T2.7` |
| PowerSlash clip | ✅ `T2.8` |
| PowerOverhead clip | ⬜ `T2.9` |
| Reaction clips | ⬜ `T2.10` |
| Combat rules | ⬜ `T4.1` |
| Physics hit detection | ⬜ `T3.1` |
| HUD & menus | ⬜ `T7.1` |

## Stack

| Layer | Library |
| --- | --- |
| Runtime | MonoGame 3.8.6.1 (DesktopGL), `net10.0` |
| Geometry | DigitalRiseModel 0.7.6 (primitives, no renderer) |
| UI | Myra 1.6.8 |
| Collision | BepuPhysics2 2.4.0 (static hurtboxes + swept blade) |

## Documentation

Detailed specifications live in [`Design/`](Design/overview.md): [overview](Design/overview.md), [architecture](Design/architecture.md), [combat](Design/combat.md), [UI & AI](Design/ui-and-ai.md), [testing](Design/testing.md). Start there for anything beyond the summary above. The work plan is in [TASKS.md](TASKS.md); the agent workflow is in [AGENTS.md](AGENTS.md).

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

**Controls:** `LMB` slash · `Shift+LMB` power slash · `1`/`2`/`3` forced arcs · `RMB` block · `F1` debug overlay · `F2` wireframe · `Esc` quit.

## License

See [LICENSE](LICENSE).
