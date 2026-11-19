# Overview

SkyrimMelee is a duel between two humanoids with greatswords, with free player movement and a Skyrim-style camera. The bodies never articulate — no rig, no limbs, no walk cycle. Every attack, block, stagger, and kill is expressed through the blade's arc, and reaction clips may tip or topple the whole primitive body as one rigid piece.

The game recreates Skyrim's **melee rules**, not its world: stamina economy, attack commitment, front-facing blocks, timed blocks, guard breaks, and poise. Combat depth comes from timing, resource management, spacing, and camera control.

## Pillars

- **The sword is the actor.** All expression, threat, and feedback live in one moving object.
- **Commitment over cancellation.** A swing cannot be feinted or cancelled; mistakes have a recovery window.
- **Readable in slow motion.** Every attack is identifiable from its wind-up alone.
- **Stamina paces the fight.** Attacking and blocking compete for one resource.
- **Defense is skillful, not free.** Blocking costs stamina and has a punishing failure state.

## Hard constraints

| # | Constraint | Consequence |
| --- | --- | --- |
| 1 | **No 3D model files.** Everything is generated at runtime from primitives. | Fighters, sword, arena, and VFX are boxes, capsules, spheres, cylinders, cones, discs, and lines. No non-code assets are required: Myra embeds its default UI font. |
| 2 | **No skeletal animation.** The body is a rigid white primitive assembly (head, torso, hips, legs) with no joints. | The sword is keyframed on a hand pivot; reactions apply one rigid whole-body transform. No joints, no skinning, no IK. |

### The two-handed grip

The body is a small rigid assembly of white primitives, so there are no static arms for a moving hilt to disconnect from. The hands are still **part of the sword rig**: both hand boxes are attached to the grip and travel with it. The assembly only moves as one rigid piece for reaction clips (lean, topple, rise); it never bends or articulates. This keeps the sword readable without pretending the body can swing it.

## Scope

**In v1:** one arena, one weapon class, player vs AI, best of three, four attacks (SlashLeft, SlashRight, PowerSlash, Bash), free player movement with mouse look, first/third-person camera with wheel zoom, block / timed block / guard break / stagger / death, health–stamina–armor–skills–poise, primitive VFX, full Myra UI.

**Out:** dodging, jumping, shields, one-handed weapons, ranged, magic, perks, inventory, save files, multiplayer, gamepad.

## From Skyrim

The full research summary is in [reference.md](reference.md). Short version:

| Mechanic | Skyrim | Ours |
| --- | --- | --- |
| Movement | Camera-relative, body faces the camera; strafe/backpedal do not rotate | Same |
| Attacks | Tap alternates left/right arcs | Same (`SlashCombo`) |
| Power attacks | Hold attack, ×2 damage, stamina cost | Same (350 ms charge, ×2) |
| Bash | Tap attack while blocking, ×0.5, rooted | Same (`LocksMovement`) |
| Block | Hold block, weapon block, 35° cone | Blade capsule must actually intercept |
| Timed block | None | 250 ms window, core skill expression |
| Stagger | Chance-based | Poise meter, deterministic |
| Stamina | Any stamina starts the action; power falls back at 0 | Same (`StaminaRules`) |
| Camera | `F`/wheel toggle + zoom, over-the-shoulder | Third-person follow now, first person in T5.3 |

Deliberate divergences and their reasons are listed in
[reference.md](reference.md#deliberate-divergences).

## Stack

| Layer | Library |
| --- | --- |
| Runtime | MonoGame 3.8.6.1 DesktopGL, `net10.0` |
| Geometry | DigitalRiseModel 0.7.6 — primitives only, no renderer |
| UI | Myra 1.6.8 |
| Collision | BepuPhysics2 2.4.0 — static shapes + sweeps, no dynamics |

Verified end-to-end on 2026-11-19 with .NET SDK 10.0.401 (T0.1): window + render, primitive box, Myra label, Bepu sweep.

## Decisions

| Decision | Why |
| --- | --- |
| Reference-first fidelity | [reference.md](reference.md) records what Skyrim does; every divergence is deliberate and listed with a reason |
| DigitalRiseModel for geometry, custom `BasicEffect` renderer | Library has no renderer; a small renderer is easier to control than an engine |
| Bepu as a query engine only | The blade sweeps and the capsule only changes rigid pose; statics + `Sweep` are all we need |
| Keyframed clips as the only animation | One animated sword transform plus one rigid body pose, each with a single source of truth |
| Fixed 60 Hz deterministic simulation | Reproducible tests, fair timing, AI-vs-AI balance runs |
| xUnit for pure logic, `--self-test` for combat | Unit tests stay GPU-free; combat scenarios exercise the real loop and state machines |
| Myra with its embedded FontStashSharp font and a runtime flat skin | No content pipeline, no font files, no image assets |
| Best of three, no round timer | Stamina makes stalemates self-resolving |

## Risks

| ID | Risk | Status |
| --- | --- | --- |
| R1 | `DrMeshPart` vertex layout incompatible with `BasicEffect` | ✅ Retired: parts are 32-byte `VertexPositionNormalTexture` and draw directly |
| R2 | Bepu `Sweep` API differs from assumptions | ✅ Retired: `Sweep(shape, pose, velocity, maxT, pool, handler)` verified |
| R3 | Myra font/content pipeline on .NET 10 | ✅ Retired: Myra ships a `DynamicSpriteFont`; no content pipeline needed |
| R4 | Blade tunneling at high swing speed | Open — T3.2 / T3.5 |
| R5 | Primitive statue reads as a pile of boxes | ✅ Retired: the body is four clean white primitives matched to the hurtboxes |
| R6 | Off-hand gap visible during wide arcs | ✅ Retired: the capsule has no arms to gap against |

## Open questions (defaults already assumed)

| # | Question | Default |
| --- | --- | --- |
| Q1 | Player on the left or selectable side? | Left, facing right |
| Q2 | Best of three or single rounds? | Best of three |
| Q3 | Accept the off-hand compromise? | Try it at T1.4, then decide |
| Q4 | Arena flavour? | Plain stone circle, dark backdrop |
| Q5 | Audio in v1? | Placeholder procedural tones in T8.2 |

## Success for v1

- A newcomer can tell a light slash from a power attack before it lands.
- Blocking on reaction works; turtling loses to guard breaks; mashing loses to timed blocks.
- A full duel runs start to finish with no stuck states or leaked input.
- Combat is reproducible headless and the game holds 60 FPS.
