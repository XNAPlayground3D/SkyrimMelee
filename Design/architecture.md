# Architecture

## Layout

```text
SkyrimMelee.slnx
Design/                     5 docs: overview, architecture, combat, ui-and-ai, testing
README.md  AGENTS.md  TASKS.md  LICENSE
SkyrimMelee/
  Program.cs  SkyrimMeleeGame.cs
  Core/       GameClock, FixedStepLoop
  Graphics/   PrimitiveLibrary, ForwardRenderer, Camera, Arena, ScreenGradient, Vfx
  Fighters/   FighterRig, FighterRigBuilder, GreatswordRig, FighterVisuals
  Combat/     CombatTuning, AttackDefinition, SwordClip(s), CombatStateMachine,
              FighterStats, DamageModel, StaggerModel, DuelDirector, CombatIntent
  Physics/    CombatWorld, HurtboxSet, BladeSweep, HitResolver
  Input/      InputRouter, PlayerController
  Ai/         DuelistAi, AiProfile
  Ui/         MyraHost, Hud.xmmp, HudPanel, DebugOverlay.xmmp, DebugOverlay, UiSkin
  Debug/      HeadlessSelfTest
SkyrimMelee.Tests/
  FixedStepLoopTests.cs      xUnit tests for pure logic
```

`SkyrimMelee.Tests` covers pure logic with xUnit; `--self-test` runs full combat scenarios against the real state machines without a window.

## Loop

```text
Update (fixed 60 Hz):
  input -> round state -> state machines -> sample sword clip
  -> update guard shape -> sweep blade -> resolve hits -> AI intents -> camera/VFX

Draw:
  arena -> fighters -> swords -> VFX -> debug overlay -> Myra
```

- Simulation is always 1/60 s; render rate never affects outcomes.
- Catch-up is capped at 5 steps; excess time is dropped.
- Slow motion scales the number of steps, never the step size.
- No render interpolation in v1 (rigid poses, negligible gain).

**Determinism:** no RNG, wall clock, or frame-rate math in combat; single-threaded physics; stable iteration and contact ordering. Same inputs ⇒ identical state at tick _n_.

## Rendering

DigitalRiseModel builds `DrMesh`/`DrMeshPart` geometry and nothing else. We draw it with one `BasicEffect` pass: world/view/projection, one warm key light, one cool fill, per-part flat colour. Verified in T0.1: parts expose 32-byte `VertexPositionNormalTexture` buffers, so binding `VertexBuffer` + `IndexBuffer` and calling `DrawIndexedPrimitives` works without a declaration adapter.

| Element | Value |
| --- | --- |
| Coordinates | Right-handed, `+Y` up |
| Fighters | Player `(-1.0, 0, 0)` facing `+X`; opponent `(+1.0, 0, 0)` facing `-X` |
| Gap | 2.0 m; the grip sits ~0.42 m forward, so full extension reaches into the torso |
| Camera | `(0.3, 2.6, −5.2)` → `(0, 1.0, 0)`, FOV 44° |
| Arena | Shallow stone cylinder radius 6, four rings, two duelling-position markers, four pillars, gradient backdrop |

Meshes are cached in `PrimitiveLibrary` (unit box; sphere/cylinder/cone/disc/plane built from radius/length parameters; capsules keyed by length+radius; line versions for debug). Draw calls take a part, a `DrMaterial` (colour), and a world matrix. ~60 draw calls total; merging fighter parts is a later optimisation.

Two geometry quirks are handled in the library/renderer: DigitalRise line primitives are position-only, so the library builds PNT line lists for `BasicEffect`; and the double-sided disc winds opposite to the other primitives, so `ForwardRenderer.Draw` takes a per-instance cull mode (`CullClockwiseFace` for the disc).

The arena is a `DrModel`: a root bone plus one bone/mesh per item (floor, each ring, each marker, each pillar). `ForwardRenderer.Draw(DrModelInstance, Matrix)` applies bone transforms, traverses the meshes, and picks the lit or unlit pass from `PrimitiveType`. Mesh parts are clones that share the library's vertex/index buffers.

**Capsule fighter** (~1.8 m): one white capsule (1.24 m cylinder + two 0.28 m caps) plus scale-free `HandR` / `HandL` attachment bones; the right hand owns the sword pivot. Fighters are `DrModel` rigs in local space (+X forward, +Z side) placed by a root transform. The capsule has no joints: reaction clips apply one rigid `BodyTransform` (lean, topple, rise) before the root, and the sword pivot is animated independently.

**Greatsword** (1.47 m blade reach): grip cylinder, pommel sphere, crossguard box, two tapered blade boxes, cone tip, inset fuller. Local origin at the grip, blade along `+Y`. It attaches to the scale-free `HandR` bone; the grip-relative `Pivot` is the only animated sword transform.

### The two-handed grip

Both hands are attached to the sword rig and travel with the hilt; the capsule body only takes a rigid reaction pose ([rationale](overview.md#the-two-handed-grip)).

**Camera presets:** third-person duel (default, over the player's right shoulder at `(-6.0, 3.1, 2.4)` looking at `(-0.2, 1.0, 0)`), profile (`(0, 1.9, −7.5)`), top-down debug. Idle sway ±0.6°, impact shake 0.15 s decay, small FOV punch on power attacks.

**VFX:** pooled 256 box shards and expanding torus rings — amber sparks for hits, blue ring for blocks, red shards and flash for guard breaks, dark disc for grounding. Zero allocations in combat.

## Physics

Bepu is used as a collision **query** engine: no gravity, no dynamics, no `Timestep` in combat. Static shapes are added at fight start, plus a guard-blade capsule while guarding.

| Hurtbox | Shape | Radius / half-length | Centre |
| --- | --- | --- | --- |
| Head | Sphere | 0.115 | (0, 1.67, 0) |
| Torso | Capsule | 0.16 / 0.21 | (0, 1.26, 0) |
| Hips | Capsule | 0.14 / 0.09 | (0, 0.86, 0) |
| Legs | Capsule | 0.13 / 0.25 | (0, 0.42, 0) |

The blade is approximated by three spheres along the blade segment (`t = 0 / 0.5 / 1`, radii 0.055 / 0.040 / 0.025).

```text
per tick, while Active:
  substeps = clamp(ceil(tipTravel / 0.35 m), 1, 8)
  for each substep, sweep the 3 spheres from previous to current pose
  collect contacts -> earliest time-of-impact wins
```

Bepu call: `Sweep(shape, poseAtPrevious, (current − previous) / dt, dt, pool, handler)` — a pose plus a velocity over `maximumT`, not a from/to pair.

The 0.35 m substep cap prevents tunneling. Contacts are classified by shape ownership: guard blade → block, body → hit, floor → graze. Ties within 1e-4 s favour the defender. Each swing keeps a hit set so a hurtbox is damaged at most once.

The guard hitbox is frozen when the guard completes; the 1.2° visual breathing is not reflected in physics (~3 cm divergence, accepted).

## Debug and flags

`--debug` overlay + wireframes · `--god` · `--slowmo` (0.25×) · `--no-ui` · `--self-test` · `--help`

`GameClock` owns `FrameIndex`, `SimTime`, fixed `DeltaTime`, `TimeScale`, real/scaled time, accumulator and dropped steps. Slow motion scales incoming time, never the step duration.

`F1` toggles the debug overlay; `F2` toggles flat wireframe. The overlay draws hurtboxes, the blade sphere chain, the last 20 swept segments, contact points and normals, plus the rejection reason for any ignored contact.
