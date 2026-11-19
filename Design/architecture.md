# Architecture

## Layout

```text
SkyrimMelee.slnx
Design/                     overview, architecture, reference, combat, ui-and-ai, testing
README.md  AGENTS.md  TASKS.md  LICENSE
SkyrimMelee/
  Program.cs  SkyrimMeleeGame.cs
  Tuning/     CombatTuning (every number)
  Core/       GameClock, FixedStepLoop, GameOptions, GeometryMath
  Graphics/   PrimitiveLibrary, ForwardRenderer, Camera, Arena, ScreenGradient
  Fighters/   FighterRig, FighterRigBuilder, GreatswordRig, FighterVisuals
  Animation/  SwordClip(s), SwordMotion, BodyClip(s), BodyMotion
  Physics/    BladeGeometry, BladeSweep, CombatWorld, HurtboxSet
  Attacks/    AttackDefinition, AttackCatalog, HitEvent, SwingHitSet
  Combat/     CombatState, CombatStateMachine, GuardResolver, FighterStats,
              DamageModel, DefenseResolver, StaggerRules, StaminaRules,
              DuelDirector, Locomotion
  Input/      InputRouter, AttackCharge, SlashCombo
  SelfTest/   SelfTestRunner
  Ui/         MyraHost, Hud.xmmp, HudPanel, DebugOverlay.xmmp, DebugOverlay
SkyrimMelee.Tests/          xUnit tests for pure logic
```

Dependencies point one way: `SelfTest → Combat → Attacks → Physics → Animation → Tuning`. `Graphics`, `Fighters`, `Input`, and `Ui` sit beside `Combat` and talk to the same lower layers. `Combat` keeps only the rules; models, clips, geometry, attack data, and the harness live in their own top-level folders.

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

**Primitive fighter** (~1.8 m): four white primitives — head sphere, torso, hips, and legs capsules — plus scale-free `HandR` / `HandL` attachment bones; the right hand owns the sword pivot. `FighterRigBuilder` reads the same `HurtboxDefinition` table the colliders use, so every rendered part is also its hurtbox. Fighters are `DrModel` rigs in local space (+X forward, +Z side) placed by a root transform. The parts have no joints: reaction clips apply one rigid `BodyTransform` (lean, topple, rise) before the root, and the sword pivot is animated independently.

**Greatsword** (1.47 m blade reach): grip cylinder, pommel sphere, crossguard box, two tapered blade boxes, cone tip, inset fuller. Local origin at the grip, blade along `+Y`. Each fighter owns its own sword and hand pivot. The player's pivot follows input clips; the opponent holds its idle pivot until the AI drives it, but plays its own stagger/death reaction clips when hit.

### The two-handed grip

Both hands are attached to the sword rig and travel with the hilt; the primitive body only takes a rigid reaction pose ([rationale](overview.md#the-two-handed-grip)).

**Camera:** third-person orbit over the player's right shoulder. Mouse look drives yaw/pitch, the target follows the player at chest height with a 1 m look-ahead, and the default distance is 5 m. `F` toggles first person and the wheel zooms (T5.3); `F3` presets and impact shake are T5.4.

**VFX:** a hit flashes the defender's whole body coral for 300 ms; pooled 256 box shards and expanding torus rings add the blue block ring, red guard-break shards, and dark grounding disc. Zero allocations in combat.

## Physics

Bepu is used as a collision **query** engine: no gravity, no dynamics, no `Timestep` in combat. `CombatWorld` owns one `Simulation`, a floor box, and four static hurtboxes per fighter. Hurtbox poses are rewritten from `BodyTransform * fighterRoot` whenever the rigid body reaction changes, so the statues and their collidables stay aligned. A guard-blade capsule is added when the guard completes and removed as soon as the guard drops. `HurtboxSet` maps each `StaticHandle` back to `FighterId` + `HurtboxKind`, and `F2` draws every registered shape (green body, amber guard blade).

| Hurtbox | Shape | Radius / half-length | Centre |
| --- | --- | --- | --- |
| Head | Sphere | 0.115 | (0, 1.67, 0) |
| Torso | Capsule | 0.16 / 0.21 | (0, 1.26, 0) |
| Hips | Capsule | 0.14 / 0.09 | (0, 0.86, 0) |
| Legs | Capsule | 0.13 / 0.25 | (0, 0.38, 0) |

The blade is approximated by three spheres along the blade segment (`t = 0 / 0.5 / 1`, radii 0.055 / 0.040 / 0.025) plus a full-blade capsule (radius 0.045, half-length 0.645). The capsule closes the gaps between the spheres, so a thin guard blade cannot slip between samples. `BladeSweep` sweeps both with Bepu:

```text
per tick, while Active:
  substeps = clamp(ceil(tipTravel / 0.35 m), 1, 8)
  for each substep, sweep the 3 spheres from previous to current pose
  collect contacts -> earliest time-of-impact wins
```

Bepu call: `Sweep(shape, poseAtPrevious, current − previous, 1, pool, handler)` — a pose plus a velocity over `maximumT`, not a from/to pair. The handler leaves `maximumT` untouched so every hurtbox on the path is reported, then `BladeSweep` deduplicates per hurtbox and sorts by time-of-impact. Contacts carry `Hurtbox` (fighter + kind), world point, normal, and TOI. A contact that starts inside a shape (`OnHitAtZeroT`) is recorded without a point or normal.

The 0.35 m substep cap prevents tunneling and is asserted in debug builds. Contacts are classified by shape ownership: guard blade → block candidate, body → clean-hit candidate, floor → graze. `GuardResolver` takes the defender's earliest guard and body contacts: the guard wins ties within 1e-4 s, but only when the defender is in `GuardHold`, the attacker is inside the 120° front cone, and the guard blade is registered. A guard younger than `GuardTimedBlockWindow` (250 ms) is a timed block; older is a normal block. Behind the cone or during the raise/lower is a clean hit.

`SwingHitSet` owns the per-attack hit set. It turns the first `BladeHit` for each `(fighter, hurtbox)` into a `HitEvent` carrying the attacker, target, hurtbox kind, point, normal, attack definition, and active-window progress; later contacts in the same swing are ignored, and the next attack clears the set. `TryMarkDamagingHit` guarantees one damage instance per swing even when the sweep crosses several hurtboxes. A confirmed hit flashes the whole defender (and its `F2` outline) for `HitFlashDuration`.

The guard hitbox follows the hand while guarding, including while the fighter moves; the small visual breathing is sampled each frame.

## Debug and flags

`--debug` overlay + wireframes · `--god` · `--slowmo` (0.25×) · `--no-ui` · `--self-test` · `--help`

`GameClock` owns `FrameIndex`, `SimTime`, fixed `DeltaTime`, `TimeScale`, real/scaled time, accumulator and dropped steps. Slow motion scales incoming time, never the step duration.

`F1` toggles the debug overlay; `F2` toggles flat wireframe. The overlay draws hurtboxes, the blade sphere chain, the last 20 swept segments, contact points and normals, plus the rejection reason for any ignored contact.
