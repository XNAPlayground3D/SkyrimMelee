# Architecture

## Layout

```text
SkyrimMelee.slnx
Design/                     5 docs: overview, architecture, combat, ui-and-ai, testing
README.md  AGENTS.md  TASKS.md  LICENSE
SkyrimMelee/
  Program.cs  SkyrimMeleeGame.cs
  Core/       GameClock, FixedStepLoop
  Graphics/   PrimitiveLibrary, PrimitiveRenderer, MeshInstance, Camera, Arena, Vfx
  Fighters/   FighterRig, FighterRigBuilder, GreatswordRig, FighterVisuals
  Combat/     CombatTuning, AttackDefinition, SwordClip(s), CombatStateMachine,
              FighterStats, DamageModel, StaggerModel, DuelDirector, CombatIntent
  Physics/    CombatWorld, HurtboxSet, BladeSweep, HitResolver
  Input/      InputRouter, PlayerController
  Ai/         DuelistAi, AiProfile
  Ui/         MyraHost, Menu/Pause/Result screens, HudScreen, UiSkin
  Debug/      DebugOverlay, HeadlessSelfTest
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
| Camera | `(0.45, 2.3, −5.8)` → `(0, 1.15, 0)`, FOV 45° |
| Arena | Stone disc, radius 6, rings, four pillars, dark backdrop |

Meshes are cached in `PrimitiveLibrary` (unit box; sphere/cylinder/cone/disc/plane built from radius/length parameters; capsules keyed by length+radius; line versions for debug). A `MeshInstance` is mesh part + colour + local transform. ~60 draw calls total; merging fighter parts is a later optimisation.

**Statue** (~1.78 m, 14 parts): head sphere, neck/chest/abdomen/pelvis boxes, arm and leg capsules, hand and boot boxes, permanent bladed stance, right hand owns the sword pivot.

**Greatsword** (1.64 m): grip cylinder, pommel sphere, crossguard box, two tapered blade boxes, cone tip, inset fuller. Local origin at the grip, blade along `+Y`, reach 1.47 m.

### The two-handed grip

The off-hand does not follow the hilt ([rationale](overview.md#the-two-handed-grip)).

**Camera presets:** duel (default), profile (`(0, 1.9, −7.5)`), top-down debug. Idle sway ±0.6°, impact shake 0.15 s decay, small FOV punch on power attacks.

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

`F1` draws hurtboxes, the blade sphere chain, the last 20 swept segments, contact points and normals, plus the rejection reason for any ignored contact.
