# SkyrimMelee

A Skyrim-inspired greatsword duel between two humanoids — with a twist: **the body never moves.** No rig, no limbs, no walking. Only the sword does. Every attack, block, stagger and kill is expressed through the blade's arc, and all geometry is generated at runtime from 3D primitives.

**Hard rules**

- **No 3D models.** Everything is built from DigitalRiseModel primitives (box, capsule, sphere, cylinder, cone, disc, plane).
- **No skeletal animation.** The body is a statue; a keyframed hand pivot moves the sword and nothing else.

## Project state

**Last updated:** 2026-11-19 · **Phase:** 0 — toolchain verified · **Code:** none yet · **Next:** `T0.2` (after T0.1 confirmation)

| Area | State |
| --- | --- |
| Docs | ✅ README, `Design/`, TASKS, AGENTS |
| Toolchain | ✅ `T0.1` verified — awaiting confirmation |
| Solution & skeleton | ⬜ `T0.2` |
| Primitive rendering | ⬜ `T1.1` |
| Sword motion & combat | ⬜ `T2.1` / `T4.1` |
| Physics hit detection | ⬜ `T3.1` |
| Myra UI | ⬜ `T7.1` |

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
dotnet run --project SkyrimMelee/SkyrimMelee.csproj
dotnet run --project SkyrimMelee/SkyrimMelee.csproj -- --self-test   # headless combat tests
```

`--debug` (overlay + hitboxes) · `--god` (infinite health) · `--slowmo` · `--no-ui`

**Controls:** `LMB` slash · `Shift+LMB` power slash · `1`/`2`/`3` forced arcs · `RMB` hold to block · `Esc` pause · `F1` debug · `F2` slow motion · `F5` reset duel.

## License

See [LICENSE](LICENSE).
