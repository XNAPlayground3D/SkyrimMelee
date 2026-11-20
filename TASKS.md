# TASKS — SkyrimMelee roadmap

> **This document is a living board, not a contract.**
> The agent may split, merge, reorder, reword, or delete tasks whenever reality disagrees with the plan — as long as the result is still moving toward the goal described in [README.md](README.md) and [Design/](Design/overview.md). When you change the plan, say so in the commit message.
>
> **Next up:** `T7.4`

---

## How to read this file

- `[ ]` not started · `[~]` in progress · `[x]` done and confirmed by the developer
- The **next task is the top-most unchecked task**, unless the developer names a different one.
- A task is only `[x]` after the developer explicitly confirms it (see [AGENTS.md](AGENTS.md)).
- After a task is confirmed, the agent curates this board without asking: adding, removing, rewording, splitting, merging, or reordering tasks as the project needs.
- Each task lists **Goal** and **Done when**. "Done when" is the test plan, not a suggestion.
- **Specs live in [`Design/`](Design/overview.md), not here.** Tasks reference the relevant doc; the doc wins on details.
- [`Design/reference.md`](Design/reference.md) records what Skyrim does. When a task changes feel, check it there first; new divergences go in its divergence list.
- Completed tasks are logged at the bottom with date and commit hash.
- The **Invariants** section must survive every rewrite.

## Invariants (do not delete or weaken)

1. No 3D model files, ever. Everything is built from `MeshPrimitives` primitives or hand-written vertex data.
2. No skeletal animation, no skinning, no rig hierarchy with animated joints. The body is a rigid white primitive assembly (head, torso, hips, legs); reaction clips may tip or topple it as one piece. The sword moves on its hand pivot.
3. Combat numbers live in `CombatTuning.cs`, not inline in logic.
4. Combat resolution is deterministic and runnable headless (`--self-test`).
5. `dotnet build SkyrimMelee.slnx` is green before any task is reported finished.
6. Pure logic has xUnit coverage in `SkyrimMelee.Tests`; `dotnet test SkyrimMelee.slnx` is green.

---

## M0 — Foundation

### [x] T0.1 — Verify the toolchain before writing game code
**Goal:** retire the technical unknowns with a throwaway spike, then delete the spike.
- Confirm `MonoGame.Framework.DesktopGL` 3.8.6.1 runs a window on this machine with SDK 10.0.4xx.
- Add `DigitalRiseModel.MonoGame` 0.7.6 and confirm `MeshPrimitives.CreateBoxMesh` produces a `DrMesh` whose `DrMeshPart` vertex/index buffers can be bound and drawn with `BasicEffect` (checking the vertex declaration actually matches `VertexPositionNormalTexture`).
- Confirm `Myra` 1.6.8 initializes and renders text (it uses FontStashSharp `SpriteFontBase`, not XNA `SpriteFont`/content pipeline).
- Confirm `BepuPhysics` 2.4.0 compiles and a `Simulation` with one static box can be stepped.
- Record exact package versions, TFM (`net10.0` vs `net8.0` fallback), and any gotchas in [Design/overview.md](Design/overview.md#risks).

**Done when:** a spike project (deleted afterwards) shows one lit box, one Myra label, and one Bepu sweep result; findings are written into `Design/overview.md`; no spike leftovers in the repo.

### [x] T0.2 — Create the real solution skeleton
**Goal:** the repo builds, tests, and opens a black window with a title and clean shutdown.
- `SkyrimMelee.slnx` at root, `SkyrimMelee/SkyrimMelee.csproj` (`net10.0`, nullable on, four pinned packages).
- `SkyrimMelee.Tests` (xUnit) in the solution, referencing the game project; `dotnet test SkyrimMelee.slnx` green.
- `Program.cs`, `SkyrimMeleeGame.cs` with fixed 60 Hz update, variable-rate draw, 1600×900 window, easy resize.
- `.editorconfig` (tabs for C#, two-space XML) and `.gitignore` for `bin/`, `obj/`, `.vs/`, `*.user`, content cache.
- `dotnet build SkyrimMelee.slnx` and `dotnet run --project SkyrimMelee` both work.

**Done when:** build and tests are warning-free, window opens and closes cleanly, `.gitignore` keeps the working tree clean.

### [x] T0.3 — Myra host and UI text
**Goal:** text can be drawn on screen.
- `Ui/MyraHost.cs`: a `MyraEnvironment` + root `Desktop` at the native window resolution; anchored layout survives resizing without a virtual canvas.
- Use Myra's embedded FontStashSharp font. No `.spritefont`, no content pipeline; add a `FontSystem` + one TTF only if a screen needs sizes the embedded font cannot provide.
- Top-left debug label showing FPS, build config, and fixed-step drift.

**Done when:** the label renders crisply at the default size and after resizing; no content pipeline or font files are required.

### [x] T0.4 — Debug flags and the run loop
**Goal:** the developer can inspect the game from day one.
- CLI flags: `--debug`, `--god`, `--slowmo`, `--no-ui`, `--self-test` (stub that exits non-zero if assertions aren't implemented yet).
- Deterministic clock: `GameClock` exposes `FrameIndex`, `SimTime`, `DeltaTime`, `TimeScale`.
- Debug overlay scaffold (toggle with `F1`).

**Done when:** each flag changes behavior observably; `--self-test` exits 0 with "no tests yet"; README documents the flags.

---

## M1 — Primitive scene

### [x] T1.1 — Primitive mesh cache
**Goal:** one place that owns every generated mesh.
- `Graphics/PrimitiveLibrary.cs` wrapping `MeshPrimitives`: unit box, cylinder, cone, sphere, capsule, disc, plane, plus line versions for debugging.
- Draw calls take a `DrMeshPart`, a `DrMaterial`, and a world matrix; materials are created through `PrimitiveLibrary.CreateMaterial`.
- Tessellation tuned low (sphere 16, cylinder 16) — this is a stylized game, not a CAD viewer.

**Done when:** a test scene draws all primitives at once with correct winding, correct lighting, and no leaked GPU resources across restarts.

### [x] T1.2 — Renderer, camera, arena
**Goal:** a believable place to duel.
- `ForwardRenderer` with `BasicEffect`: per-part color, one directional light, ambient fill, fog disabled.
- `Camera`: fixed 3/4 side view framing both fighters, slight sway on idle; hooks for shake later.
- `Arena`: stone disc, concentric grid rings (line primitives), four pillars, dark background, subtle vignette via clear color gradient (two-pass quad).
- `Arena` is a `DrModel`: one bone per item, one mesh part per bone. `ForwardRenderer.Draw(DrModelInstance, Matrix)` traverses the model and renders each part.

**Done when:** the arena renders from the gameplay camera with stable 60 FPS and no z-fighting; camera framing shows both fighter slots without clipping.

### [x] T1.3 — Fighter statue from primitives
**Goal:** two recognizable humanoids, built only from boxes/capsules/spheres.
- `FighterRigBuilder`: head (sphere), neck + torso + pelvis (boxes/capsule), upper/lower arms (capsules), hands (boxes), thighs/shins (capsules), boots (boxes).
- One permanent combat stance pose, mirrored by the root transform, both facing each other at ±1.0 m on X (2.0 m gap).
- Built as `DrModel` rigs and drawn with `ForwardRenderer.Draw(DrModelInstance, Matrix)`.
- `FighterVisuals`: distinct palettes (steel-blue vs rust-red), so the two are readable at a glance.
- Wireframe toggle: `F2` switches every mesh to flat, unlit wireframe; `F1` toggles the overlay.

**Done when:** silhouettes read as humanoids from the gameplay camera; no part intersects the floor; toggling wireframe shows the exact primitive decomposition.

### [x] T1.4 — The greatsword
**Goal:** the star of the show, and its pivot.
- `GreatswordRig`: blade (long box, slightly tapered by two stacked boxes), fuller (thin inset box), crossguard (box), grip (cylinder), pommel (sphere). Local origin at the grip.
- Hand pivot: the sword attaches to the scale-free `HandR` bone (the hand box is a scaled child bone), so the sword does not inherit the box scale.
- Dev handle: arrows or `IJKL` rotate yaw/pitch, `Q`/`E` roll; the grip/mid/tip gizmo appears with the `F1` debug overlay.
- Off-hand compromise reviewed in the default guard pose: accepted. The fallback (hands attached to the sword rig) remains documented.

**Done when:** sword looks like a two-handed greatsword, sits naturally in the right hand, and the dev rotation can sweep it through every pose without clipping the body.

### [x] T1.5 — HUD from Myra markup
**Goal:** the HUD is authored in Myra markup and exported, not built in C#.
- `SkyrimMelee/Ui/Hud.xmmp`: two `VerticalStackPanel` blocks — render statistics (FPS, draw calls, primitives) and controls usage; every row is a `Label`.
- `SkyrimMelee/Ui/DebugOverlay.xmmp`: the F1 debug panel follows the same pattern (a `VerticalStackPanel`, one `Label` per row).
- Export with `myra-export SkyrimMelee/Ui/Hud.xmmp` and `myra-export SkyrimMelee/Ui/DebugOverlay.xmmp`; commit the manual and generated files.
- `ForwardRenderer` exposes `DrawCalls` and `PrimitivesDrawn`; the stats block updates live.
- Panel backgrounds are markup brushes with alpha (`Background="#00000066"`).
- Unimplemented controls are amber; a muted legend line explains the colour.
- The F1 debug overlay follows the same markup/export pattern.

**Done when:** both blocks render at native resolution, the stats update live, and no HUD widget is constructed in C#.

---

## M2 — Sword motion ("animation")

### [x] T2.1 — Sword clip system
**Goal:** replace hard-coded rotations with authored keyframe clips.
- `SwordClip` (duration, `SwordKey` list, per-segment easing), sampler with lerp + slerp.
- Clip registry with ID lookup, looping for idle/guard, one-shot for attacks.
- 100 ms cross-fade between outgoing and incoming clip poses.
- Unit-level check: sampling t=0 and t=1 returns the first/last key exactly.
- HUD playback rows show the current clip name, speed multiplier, and progress (e.g. `50ms/120ms`); the F1 overlay also shows clip time.

**Done when:** dev harness can play any clip at any speed and the sword motion looks continuous (no snapping) across clip changes.

### [x] T2.2 — SlashLeft clip (perfection pass)
**Goal:** author `SlashLeft` until the developer explicitly approves it.
- Only `SlashLeft` is in `SwordClips` / `AttackCatalog` for this task; every other attack has its own task (T2.6–T2.9).
- Fighters are four white primitives (head, torso, hips, legs) plus invisible `HandR` / `HandL` pivots; the visible hands stay on the sword rig.
- Idle/guard uses the Skyrim standing greatsword pose: hand at the right hip, blade nearly vertical with a slight backward lean.
- Camera is a third-person view over the player's right shoulder; the idle sword stands on that right side and the slash sweeps across the screen in the fighter's Z axis.
- Timing matches `CombatTuning.Slash` (250 / 220 / 350 ms); 100 ms telegraph hold.
- The cut is a body orbit: the hands circle a chest-height pivot. The wind-up carries the whole sword right and behind the body (hands ~0.9 m back, ~0.6 m right) with the blade near-vertical and the idle roll unchanged, so the sword translates rather than twisting in the wrist; the tip ends almost behind the fighter's back. The 220 ms active sweep carries the hilt ~1.5 m forward/left and the tip cuts through the opponent's space. The blade rolls edge-first during the cut.
- Iterate on arc, hilt travel, hand placement, and readability from developer feedback.
- Do not mark done until the developer says the clip is perfect.

**Done when:** developer approval of the SlashLeft arc, timing, and hilt/hand motion.

### [x] T2.3 — Guard clips
**Goal:** blocking looks like a greatsword guard, not a T-pose.
- `GuardRaise` / `GuardHold` (loop, subtle 1–2° breathing) / `GuardLower`.
- Guard pose physically covers the torso from the front and places the blade between the fighters.
- Dev harness: hold RMB to raise/hold, release to lower.

**Done when:** raising/lowering guard has visible anticipation and follow-through; guard blade genuinely occludes the torso from the opponent's direction.

### [x] T2.4 — Clip -> blade segment
**Goal:** one function that turns a pose into world-space blade geometry.
- `BladeGeometry.FromPose` / `FromWorld`: grip, blade base, blade tip, base/tip radius, length.
- F1 overlay draws the capsule axis and base/tip rings; the same values will feed the T3.2 sweep.
- Tests sample every clip at 20 steps and keep the blade rigid.

**Done when:** debug draw of the physics blade capsule overlaps the rendered blade within 1 cm at every sampled time across every clip.

### [x] T2.5 — Combat state machine (motion only)
**Goal:** input-less, damage-less sequencing: idle → wind-up → active → recovery → idle; idle → guard → idle; interruption by stagger.
- `CombatStateMachine` with explicit states, timers, and a transition table; updates on the fixed 60 Hz step.
- Commit lock: no transition out of an attack except stagger/death.
- Guard raise/held/lower with 200 ms raise and 150 ms lower.
- F1 overlay shows state, state time, and active/block/facing-lock flags; dev controls pause/reset/scale the simulation.

**Done when:** FSM debug view shows correct state/timer transitions; forced transitions cannot skip wind-up or active phases.

### [x] T2.6 — SlashRight clip
**Goal:** add the mirrored slash as its own deliverable.
- Author `SlashRight` as the mirror of the approved `SlashLeft`: grip Z and Euler pitch/yaw negated, same 250 / 220 / 350 ms timing and 110 ms telegraph hold.
- Add it to `SwordClips` / `AttackCatalog`; tests assert the mirror and the generic rules.
- Dev harness: `1` / LMB plays `SlashLeft`, `2` plays `SlashRight`.

**Done when:** developer approval of the SlashRight arc, timing, and hilt/hand motion.

### [x] T2.7 — Overhead clip
**Goal:** add the vertical chop as its own deliverable.
- Author `Overhead` with the 400 / 140 / 450 ms phases, 100 ms hold, hilt rising above the head then chopping forward/down.
- Add it to `SwordClips` / `AttackCatalog`; tests assert the raise/chop and the generic rules.
- Dev harness: `3` plays Overhead.

**Done when:** developer approval of the Overhead arc, timing, and hilt/hand motion.

### [x] T2.8 — PowerSlash clip
**Goal:** add the wide power cleave as its own deliverable.
- Author `PowerSlash` with 600 / 160 / 700 ms, 100 ms hold, ~2 m hilt travel, and a bigger follow-through than SlashLeft.
- Add it to `SwordClips` / `AttackCatalog`; test `RequiresStamina`, the 30 stamina cost, 1.8× damage, 45 stagger, and the larger travel.
- Dev harness: Shift+LMB plays it.

**Done when:** developer approval of the PowerSlash arc, timing, and hilt/hand motion.

### [x] T2.9 — Three-attack moveset
**Goal:** keep exactly three attacks — `SlashLeft`, `SlashRight`, `PowerSlash` — with `PowerSlash` as the single overhead power slam.
- Move the former power-overhead poses and timing into `PowerSlash`: 600 / 160 / 700 ms, 100 ms hold, high overhead raise, deep follow-through.
- Remove `Overhead` and `PowerOverhead` from `SwordClips`, `CombatTuning`, `AttackCatalog`, input, HUD, tests, and docs.
- Dev harness: `1` / LMB plays `SlashLeft`, `2` plays `SlashRight`, Shift+LMB plays `PowerSlash`.

**Done when:** developer approval of the three-attack set and of the `PowerSlash` overhead arc, timing, and hilt/hand motion.

### [x] T2.10 — Reaction clips
**Goal:** add the non-guard reaction animations as their own deliverable (split out of T2.3).
- `StaggerSmall` (600 ms), `StaggerLarge` (1000 ms), `Death` (1200 ms), `Victory` (1600 ms); durations in `CombatTuning`.
- Sword side: `SwordClip` keys for each reaction.
- Body side: a rigid `BodyClip` per reaction applied to the capsule (no joints); `StaggerSmall`/`Large` rock and recover, `Death` topples onto the floor, `Victory` leans back and holds.
- Tests: durations/names, staggers recover, death rests on the floor, victory holds; `BodyClips` match their sword clips.
- Dev harness: `4`–`7` play the clips, `R` resets to idle.

**Done when:** each reaction reads clearly from the back camera and the developer approves the set.

---

## M3 — Physics & hit detection

### [x] T3.1 — Bepu world with static hurtboxes
**Goal:** collision shapes exist and can be queried.
- `CombatWorld` owns one `Simulation` (no gravity), one static floor, and per-fighter static shapes: torso capsule, head sphere, hips capsule.
- Guard blade registers/unregisters its static capsule as the guard state changes.
- `HurtboxSet` maps Bepu collidable handles back to `FighterId` + `HurtboxKind`.
- Hurtboxes follow the fighter's `BodyTransform`, so they match the capsule during reaction clips.
- `F2` wireframe draws every registered shape in world space.
- Tests: four hurtboxes per fighter, handle mapping, body transform application, guard add/remove, guard pose alignment.

**Done when:** debug shapes match the rendered statue parts; unregistering the guard removes its collidable without leaking.

### [x] T3.2 — Swept blade cast
**Goal:** fast swings cannot tunnel.
- `BladeSweep`: swept capsule from previous blade segment to current segment, executed per simulation substep with a custom Bepu hit handler.
- Collect all hits per substep: shape id, contact point, normal, time-of-impact.
- Hard cap on sweep distance per substep; assert if exceeded.
- `BladeSweep` reuses its collections and deduplicates per hurtbox per call; zero-TOI contacts are recorded without a point/normal.
- Test: `PowerSlash` against a torso at 60 Hz and with six fixed steps per simulated 10 FPS frame; the torso is the first hit and is reported once.

**Done when:** a scripted max-speed power swing against a torso always registers exactly one first-contact event, at 60 Hz and at simulated 10 FPS.

### [x] T3.3 — Swing damage bookkeeping
**Goal:** one hit per target per swing, with correct contact data.
- Per-swing `HashSet` of already-hit hurtboxes; independent set per attack (not per fighter).
- `HitEvent` record: attacker, target, hurtbox kind, world point, normal, attack definition, active-window progress.
- A confirmed hit tints the hit body primitive (and its `F2` outline) for `HitFlashDuration`.
- `SwingHitSet` begins on each attack and deduplicates `(fighter, hurtbox)`.
- Tests: torso-then-head yields two events, repeats are ignored, a new swing clears, different targets are separate, progress clamps.
- Contact feedback is the hit-part tint; no spark particles.

**Done when:** a swing that passes through torso then head produces two hits (different hurtboxes) but only one per hurtbox; the hit set resets on the next attack.

### [x] T3.4 — Blade-vs-blade block detection
**Goal:** an active guard genuinely intercepts the incoming blade.
- While guarding, the guard blade capsule is a query target (not just a visual); each fighter can register its own guard.
- Sweep classification: guard-blade contact → block candidate; body contact → clean-hit candidate; earliest time-of-impact wins, with the guard winning ties within 1e-4 s.
- Facing cone check (±60° around forward) and guard-held age determine block validity; younger than 250 ms is a timed block.
- The blade sweep adds a full-blade capsule alongside the three spheres so a thin guard cannot slip between samples.
- Tests: front block, behind, lowering, body-first, tie, no-guard, cone edges, and a swept guard contact.

**Done when:** scripted swings at a guarding opponent in front always classify as block; swings from behind or during guard lowering classify as clean hits; earliest-TOI arbitration is verified with a deliberate double contact.

### [x] T3.5 — Headless self-test harness
**Goal:** the whole combat sim is verifiable without a window.
- `--self-test` runs named, deterministic scenarios on a fixed timestep with a scripted track (no RNG, no wall clock).
- Scenarios: clean hit, blocked hit, timed block, guard break, stagger interrupt, whiff, one-hit-per-swing, tunneling stress.
- Prints PASS/FAIL per scenario, exits non-zero on failure.
- Guard break currently verifies the consequences (guard removed, 1000 ms stagger); the stamina trigger lands in T4.4.
- Tests: all scenarios pass and 10 consecutive runs are identical.

**Done when:** all scenarios pass on a cold machine; results are identical across 10 consecutive runs.

---

## M4 — Combat rules

### [x] T4.1 — Stats and stamina economy
**Goal:** stamina is a resource you feel.
- `FighterStats`: health, max health, stamina, armor rating, two-handed skill, block skill, poise.
- Regen: 25/s stamina after 0.6 s delay; disabled while blocking; poise regen 25/s after 1 s.
- Costs charged at wind-up; power attack rejected below cost (with a UI cue); light attacks always allowed.
- HUD vitals block: a label above each `HorizontalProgressBar` (`Health 150/150`, `Stamina 100/100`); denial appends `POWER DENIED` to the stamina label. The self-test gains a power-rejection scenario.
- Tests: spend/clamp, regen delay, guard freeze, poise regen/damage reset, reset, rejection.

**Done when:** stamina math is covered by xUnit or self-test assertions; a rejected power attack consumes no stamina and plays no clip.

### [x] T4.2 — Damage pipeline
**Goal:** the [Design/combat](Design/combat.md#damage) formula, implemented once, tested in isolation.
- `DamageModel` pure functions: base → armor → block → timed block → guard break.
- Return a breakdown struct (raw, armored, final, reduction %, stamina cost) for the debug overlay.
- Deterministic rounding rules (no banker's rounding surprises).
- Tuning constants for skills, armor/block curves, timed bonus, stamina multipliers, and the minimum.
- Tests: armor 0/300/567/900, block skill 0/50/100, timed block, guard break, stamina costs, rounding, minimum 1.

**Done when:** xUnit table-driven tests cover armor 0/300/567/900, block skill 0/50/100, timed/non-timed/guard-break, and match the documented formula exactly.

### [x] T4.3 — Poise, stagger, and block bash
**Goal:** heavy hits interrupt; light hits chip; the guard has a quick shove.
- Poise damage from `AttackDefinition.Stagger` applied on hit; poise zero → stagger state with duration from attack weight.
- Stagger cancels wind-up/active/recovery and guard; it cannot be cancelled.
- Power attacks always stagger on clean hit.
- `StaggerRules` applies poise damage, restores poise on stagger, and returns the duration. The opponent FSM receives the stagger; its separate animation is deferred to the AI/duel pass.
- Tests: ten light hits break poise, power always staggers, non-breaking hits do not, FSM lock/expiry, windup/guard cancellation, no re-stagger; 7 xUnit + 2 self-test scenarios.
- Block bash (`T4.6`, folded into this batch): 160 / 80 / 300 ms, 15 stamina, 0.4× damage, 40 poise, `RequiresStamina`, no `AlwaysStagger`; starts/ends in the guard pose; only the sword moves (the body stays static); F or LMB while blocking; resumes `GuardHold` directly if RMB is held, otherwise lowers. Sword clip, catalog, HUD, and a self-test bash scenario.

**Done when:** scripted trades produce the expected interrupt windows, a staggered fighter cannot act until the timer expires, and the bash reads clearly from the back camera.

### [x] T4.4 — Timed block and guard break
**Goal:** the two skill-expressive defensive outcomes.
- Timed block: +0.15 reduction (cap 0.85), half stamina drain, 250 ms attacker micro-stagger (`StaggerMicro`).
- Guard break: stamina cost > remaining stamina → full damage, guard force-lowered, long stagger, distinct VFX/sound cue.
- Guard cannot be re-raised while staggered.
- `DefenseResolver` applies the incoming attack to the defender's stamina and returns the damage, stamina spend, both stagger durations, and the break flag.
- Tests: clean/block/timed/guard-break from the same attack, half-stamina timed block, guard-break lock, micro-stagger lock; `StaggerMicro` clip/body 250 ms; self-test defense outcomes.

**Done when:** all three defensive outcomes (clean hit / block / guard break) are reproducible from the same incoming attack by varying guard timing and stamina.

### [x] T4.5 — Death, rounds, reset
**Goal:** a complete duel loop without UI.
- Health ≤ 0 → death clip, 3 s hold, round scored, fighters reset (stats, clips, shapes, hit sets).
- Best of three; `DuelDirector` exposes round number, score, phase, and match winner.
- A double KO replays the round without score. `R` resets the match.
- Reset must leave no state behind (verified by running the same scenario twice).
- The game applies clean-hit damage to the opponent and reports its death; the opponent's independent death animation is deferred to the AI/duel presentation.
- Tests: best-of-three, draw replay, 3 s hold, reset, health/death clamp; self-test best of three.
- HUD: round/score line in the VITALS block.

**Done when:** a scripted best-of-three completes correctly; back-to-back duels produce byte-identical self-test output.

### [x] T4.6 — Guard coverage cone
**Goal:** a visually correct block is actually a block, as in Skyrim.
- Skyrim blocks inside a **35°** frontal cone regardless of the exact weapon pose; the project previously required the guard blade capsule to intersect the incoming sweep.
- `GuardResolver` now keeps the blade-contact path as the primary test and falls back to a body hit inside `GuardBlockConeDegrees` (35° half-angle) of the defender's forward direction.
- Tuning: `GuardBlockConeDegrees` in `CombatTuning.cs`; docs in [reference.md](Design/reference.md#4-blocking) and [combat.md](Design/combat.md#blocking).
- Tests: body hit inside the cone blocks without a blade contact, the timed window still applies, and a hit outside the cone stays clean.

**Done when:** a guard held straight ahead blocks a chest-height sweep that misses the blade by ≤ 0.15 m, and a flanking hit still lands.

### [x] T4.7 — Bash interrupts, it does not stun
**Goal:** a bash is an interrupt/punish tool, not a stun-lock.
- `CombatTuning.Bash` no longer sets `AlwaysStagger`; it keeps 40 poise and 0.5× damage.
- `BashRules.ForcesStagger(targetState, guarding)` is the single rule: true only for a held guard or a target in wind-up/active.
- `SkyrimMeleeGame.ResolveBashHit` forces the stagger only for those cases; otherwise the bash goes through `StaggerRules` (poise chip, stagger on poise break).
- An interrupt cancels the opponent's attack and staggers `StaggerSmallDuration`; a held guard is shoved (large stagger on guard break).
- Tests: `ForcesStagger` state table, bash chips 40 poise per hit and staggers on the third, attack definition no longer always-staggers; self-test `bash` asserts the idle hit does not stagger and the interrupt table is right.
- Docs: `combat.md` and `reference.md` bash rows.

**Done when:** bashing an idle, recovering, or raising opponent deals damage/poise but does not stagger, while a bash into wind-up/active or a held guard still staggers.

### [x] T4.8 — One damage instance per swing
**Goal:** a power slash should not delete a full health bar in one swing.
- `ResolveOpponentHits` was missing `TryMarkDamagingHit`; a single overhead could pass through head, torso, hips, and legs and apply full damage for each unique hurtbox (up to 4 × 50 = 200).
- The opponent path now claims the swing's one damage instance after the first registered contact; later hurtboxes in the same swing cannot add damage.
- AI difficulty follow-up: power use now uses `PowerChance` directly instead of `0.5 + chance`; Novice aggression 0.40 (was 0.55) and power 10%, Master power 45%.
- Self-test `one hit per swing` now registers two different hurtboxes and asserts only the first damage claim succeeds.
- Docs/profile tables updated.

**Done when:** one enemy swing deals damage at most once regardless of how many hurtboxes it crosses; Novice no longer chains power slashes.

---

## M5 — Player, camera, feel

### [x] T5.1 — Player input and intent buffering
**Goal:** inputs feel responsive without breaking commitment.
- `InputRouter` with UI-first capture (`MyraHost.CapturesInput` while a modal/focused widget is open).
- 120 ms input buffer for attacks; one pending intent, newest wins; it fires on the first legal frame. Guard is level-triggered.
- Rejected actions (stamina, bash without guard, expired buffer) surface in the F1 overlay.
- Tests: newest wins, peek/clear, 120 ms expiry, buffered attack fires on the first legal frame, guard available on the first legal frame; self-test input buffer.

**Done when:** mashing during recovery yields exactly one queued attack; holding block during recovery raises guard on the first legal frame.

### [x] T5.2 — Player locomotion and follow camera
**Goal:** move the player around the arena with Skyrim-style camera-relative control.
- Camera-relative WASD, mouse look, walk speed in `CombatTuning`, arena radius clamp, and a minimum separation from the opponent.
- The rigid capsule slides and keeps facing the camera while strafing/backpedaling (Skyrim-style); attacks turn to the camera forward. No walk animation, no rig. Hurtboxes and sword follow the player root automatically.
- Movement continues through attacks (0.25× speed, facing locked) and guard (0.45×), Skyrim-style. `Bash` is the exception: `LocksMovement` roots the fighter for the whole swing. Stagger/Death lock the root too. The guard hitbox follows the moving hand.
- Third-person camera follows the player; the opponent stays static until AI movement (T6).
- Only the player's sword/body reacts to input; the opponent holds its idle sword pose (hit tint is still feedback).
- Hit feedback flashes the defender's whole body coral for 300 ms, and one swing deals damage once even if the sweep crosses hips and legs. The opponent plays its own stagger/death reaction when hit, so the end of a round is visible; the F1 overlay shows `hits N`, the last hurtbox/time, and the opponent's health.
- LMB tap starts `SlashLeft` from rest; a tap chained during the swing answers with the other side (`SlashCombo` follows the blade, not a counter) and starts during `Recovery` after `ComboCancelTime` (80 ms), without waiting for the blade to return to idle. `1` / `2` force a side. Holding LMB for 350 ms fires `PowerSlash` (real-time charge, even in slow motion). `Shift+LMB` is no longer used.
- Stamina follows the Skyrim gate: any stamina starts the attack; at zero a power attack degrades to `SlashLeft` with a stamina flash. Power ×2 and bash ×0.5 damage now match the reference.
- Tests: arena clamp, facing math, camera follow, zoom/pitch clamps, tap/hold charge, blade-side slash selection, stamina gate.
- The F1 overlay shows the move input, player position, and camera yaw.

**Done when:** WASD + mouse look move smoothly at 60 Hz, attacks still land, and the opponent stays framed.

### [x] T5.3 — First/third person and zoom
**Goal:** the Skyrim camera feel.
- `F` toggles third/first person; bash moved from `F` to `B` (LMB tap while blocking still bashes). HUD shows `F First / third`, `Wheel Zoom`, `B / LMB (block) Bash`.
- Mouse wheel zooms: third-person distance steps 0.4 m per notch, clamped 2.5–7 m; first-person FOV steps 2° per notch, clamped 55–90° (default 70°). Third person keeps its 44° FOV.
- First person sits at the player's head (1.60 m), uses the same yaw/pitch look, hides the player capsule but keeps the sword and hands visible.
- Tests: mode toggle, first-person eye/look-at, both zoom clamps (xUnit).
- Visual: both modes, both zoom extremes, bash on `B`, and no capsule in the first-person view.

**Done when:** `F` switches modes cleanly and the wheel zooms without clipping or losing the opponent.

### [x] T5.4 — Camera feel
**Goal:** impact is felt in the camera, not just seen.
- Impact shake on a landed hit: amplitude `0.012 + 0.10 × damage/maxHealth` (≈0.027 m for a slash, ≈0.045 m for a power slash), 90 ms decay (5 frames); `--no-shake` disables it. Block-scaled shake arrives with opponent attacks (T6).
- Power slash adds a 2.5° FOV punch over 180 ms.
- Standing still applies a slow ±1.2 cm look drift; movement and attacks stop it.
- `F3` cycles presets: gameplay 3/4 → side profile (6 m) → top-down debug (7 m, pitch -1.45) → gameplay. Presets force third person, ignore mouse look, and restore the saved yaw/pitch/distance.
- Tests: shake moves then decays, FOV punch rises then clears, preset cycle/restore, presets ignore look, first person leaves on preset, idle drift, `--no-shake` parse.
- Visual: side and top-down presets frame both fighters.

**Done when:** shake is noticeable but never hides a wind-up; presets all frame both fighters.

### [x] T5.5 — Debug overlay v2
**Goal:** tune combat without a debugger.
- Live: clock, state + timer, health/stamina/poise, clip + normalized time, active-window flag, last hit/guard outcome.
- Collision: the last 20 blade segments drawn as a trail, plus the contact point and normal for 1 s.
- Tuning: `;` selects `HitFlashDuration` / `StaggerMicroDuration` / `StaggerSmallDuration` / `StaggerLargeDuration`; `[` / `]` nudge ±10 ms (floor 10 ms, ceiling 3 s). Each nudge logs `Name = 0.00f` to stdout and shows it in the overlay for paste-back.
- Overrides are session-only and live for the four tunables; attack phase timings are excluded because they rebuild clips.
- Tests: default values, ±10 ms step and clamp, key cycling, paste-ready log format.
- Visual: trail during a swing, contact marker on a hit, `;` / `[` / `]` update the tune line.

**Done when:** an entire tuning session can be done in-game with the overlay, and the logged values can be pasted into the tuning file unchanged.

### [x] T5.7 — Split the game code out of Combat
**Goal:** `Combat/` stops being a 26-file grab bag; each top-level folder owns one concern.
- `Tuning/` — `CombatTuning` (every number, single source of truth).
- `Animation/` — clip formats, builders, and players (`SwordClip(s)`, `SwordMotion`, `BodyClip(s)`, `BodyMotion`).
- `Physics/` — blade geometry, sweeps, hurtbox world (`BladeGeometry`, `BladeSweep`, `CombatWorld`, `HurtboxSet`).
- `Attacks/` — attack data and per-swing hit records (`AttackDefinition`, `AttackCatalog`, `HitEvent`, `SwingHitSet`).
- `Combat/` — only the rules: state machine, guard classification, stats, damage/defense/stagger/stamina, rounds, locomotion.
- `SelfTest/` — `SelfTestRunner`.
- Namespaces mirror the folders; dependencies point `SelfTest → Combat → Attacks → Physics → Animation → Tuning` with no cycles.
- Tests import the namespaces once via `<Using>` items; no behavior changes.

**Done when:** the solution builds warning-free, all tests and self-test scenarios pass, and `Design/architecture.md` shows the new tree.

---

## M6 — Opponent AI

### [x] T6.1 — Reactive duelist AI
**Goal:** a believable sparring partner that closes distance and fights back.
- `Ai/DuelistAi`: seeded, deterministic policy. It sees only the player/self state, stamina, and distance; it emits one `CombatIntent`, a guard wish, and an approach value.
- Movement: walks toward the player when farther than 2.3 m, gives ground inside 1.35 m, and braces while the player's attack is committed. `MoveOpponent` uses the same `PlayerWalkSpeed × Locomotion.MovementMultiplier` as the player (0.25× attacking, 0.45× guarding), clamped by the arena radius and the 1.15 m separation, and faces the player.
- Behavior: reacts to a player wind-up after a profile delay to guard (timed if late), occasionally pre-guards (normal blocks), punishes recovery, presses a held guard with a power attack, attacks an idle player, and sometimes makes a greedy trade.
- `AiProfile.Adept`: 100 ms reaction, 75% block, 45% pre-guard, 55% aggression, 25% power, 10% mistake. Presets live in T6.2.
- Both directions use the same rules: the opponent's attacks sweep the player's hurtboxes, are classified by `GuardResolver` (blade contact or the 35° cone), and run `DefenseResolver` for damage/stamina/guard breaks; the player's hits are classified against the opponent's guard the same way.
- Incoming clean hits/staggers/death apply to the player; blocked hits add the smaller block shake from T5.4.
- F1 shows both sides' guard state, clip, positions, and block/timed/break/hit counters.
- Tests: guard after the reaction delay, punish recovery, no action while committed, approach/retreat/brace spacing, same seed ⇒ identical decision sequence.

**Done when:** a 2-minute fight shows all of: clean hits, blocks, timed blocks, guard breaks, and staggers from both sides; AI never uses an illegal transition.

### [x] T6.2 — Difficulty profiles
**Goal:** three opponents worth beating.
- `AiProfile`: reaction, block chance, timed-block chance, pre-guard (turtle), aggression, power frequency, mistake chance.
- Presets: `AiProfile.Novice`, `Adept`, `Master` (data only; `AiProfile.All`, `ByName`). `--profile=<name>` selects one at launch; the main-menu selector lands in T7.1.
- The AI schedules its guard including the raise time, and a timed-block read delays the raise slightly without finishing after the active window.
- `AiDuelSimulator` runs headless AI-vs-AI duels at 1.6 m with the real FSMs, stamina, guard/defense and stagger rules; it reports winner, hits, blocks, damage, duration.
- Tests: profile data, Master beats Novice ≥8/10 seeds, Novice lands hits, deterministic duels. Self-test scenario `ai duel`.
- Balance: Master 0.06 s reaction / 0.92 block / 0.70 pre-guard / 0.30 aggression / 0.45 power / 0.02 mistake; Novice 0.22 / 0.40 / 0.10 / 0.40 / 0.10 / 0.35.

**Done when:** Master beats Novice in ≥8 of 10 headless AI-vs-AI duels; Novice still lands occasional hits.

### [x] T6.3 — AI visualization
**Goal:** understand (and debug) the opponent's decisions.
- `DuelistAi` publishes `LastDecision` (action + reason), a monotonically increasing `DecisionId`, `ReactionRemaining`, `GuardRemaining`, `Intent`, `Approach`, and `Profile`.
- The game logs each new decision once with sim time and the FSM state it produced: `16.20 SlashRight (idle pressure) -> Windup SlashRight`; last two shown in the overlay.
- F1 overlay adds an AI block: profile, intent, reaction countdown, guard timer, approach, last decision/reason, and per-round `AiFightStats` (attacks, hit rate, block rate, guard breaks).
- AI telemetry updated for every choice: block read / timed block / failed roll / pre-guard / punish recovery / greedy trade / guard pressure / idle pressure.
- Fix found while wiring the log: the opponent attack path required `Idle` and ran after the guard was lowered, so punish intents issued from a held guard were dropped. It now starts the wind-up before the guard releases, using the same `TryStartAttack` transitions as the player.
- Overlay layout: merged the frame/sim/dt, scale/acc/dropped, clip/time, and fsm/flags rows so the panel sits below the render HUD without clipping.
- Tests: decision action/reason for guard success, failed roll, punish, and reset; `AiFightStats` rates and reset; F1 checked visually in a live run.

**Done when:** every AI decision shown in the overlay corresponds to a state-machine event in the log.

### [x] T6.4 — Arena health potions
**Goal:** a risk/reward pickup that breaks up pure sword trades.
- `Combat/HealthPotions.cs`: seeded spawn field. First potion after 4 s, then one every 8 s while fewer than two are active; spawns on a 3.5–5 m ring, retrying up to six times to stay 1 m away from both fighters.
- Pickup radius 0.65 m; the nearest fighter takes it (ties to the player). Instant `Heal(35)` capped at max health. Both fighters can pick up.
- Deterministic under the fixed 60 Hz step; resets each round. No potion lifetime.
- `Fighters/PotionRig.cs`: red sphere, glass neck, cork, glow ring; bobs/spins, no hurtbox. Rigs are created lazily and never disposed mid-fight: their cloned meshes share the primitive library buffers, so disposing one on pickup would take the arena with it.
- Tuning in `CombatTuning`; HUD health bar shows the instant heal. F1 debug line includes the active potion count.
- Tests: spawn delay/cap/ring/determinism, 5.5 s at 60 Hz, heal cap, nearest/tie/out-of-range pickup; self-test `health potion`. 285 xUnit, 17/17 self-test.

**Done when:** potions appear at the seeded times inside the ring, cap at two, and restore 35 health on pickup for either fighter.

---

## M7 — Myra UI

### [x] T7.1 — Skin + main menu
**Goal:** it looks like a game, not a debug harness.
- `UiSkin`: parchment/steel palette, four Inter sizes (46 / 30 / 21 / 15), and one flat `ButtonStyle` with ink panels, steel borders, and an amber focus ring.
- `MainMenu` lives in `Ui/MainMenu.xmmp` + `MainMenu.cs`: title, Duel / Practice / Options / Quit, difficulty row (default Novice), and an options sub-panel (difficulty, screen shake, debug overlay, back).
- Mouse clicks use Myra; Up / Down move focus with wrap-around, Enter / Space activate, Escape backs out of options, Left / Right change difficulty. Myra's key handler is cleared while the menu is open so a press cannot activate twice.
- Practice starts the same fight without `DuelDirector` rounds: deaths do not score and both fighters reset to spawn after 1.6 s; the HUD shows `PRACTICE`.
- Launch boots to the menu; `Esc` in a fight returns to it. Screen changes use a 180 ms black fade drawn over the UI. Starting a fight resets the camera to the default third-person orbit and both fighters to spawn; the scroll-wheel baseline is captured at load so a stale OS wheel value cannot zoom the camera on launch.
- `--play` skips the menu for captures and automation. `ResetRound` now returns both fighters to spawn, so rounds and menu-started fights begin from the same state.
- Options shell only: real pause/options/results stay in T7.3.
- Verified visually at 1600×900 and 1280×720; mouse navigation through Duel / Practice / Options / Quit; keyboard focus movement (Up/Down) and the amber ring.

**Done when:** every menu item is reachable by keyboard and mouse; no text renders blurry at 1600×900 or after resize.

### [x] T7.2 — HUD
**Goal:** read the fight at a glance, Skyrim-style.
- Player top-left and opponent top-right: name, 420 × 18 health with a pale delayed damage ghost, 340 × 10 stamina. Ghost holds 0.35 s then drains at 90/s; the red bar catches up at 420/s; stamina blinks at 3 Hz when denied.
- Centre-top round pips and `ROUND N`; banner below for `ROUND N` (1.4 s), `K.O.` (1.2 s), `VICTORY`/`DEFEAT` (2.6 s). Practice shows `PRACTICE` and `RESET`.
- Per-frame `HudPanel.UpdateFight` (no strings); round/pip labels update only when the score changes. Render stats and controls move behind F1.
- HudPanel markup rewritten and regenerated; `HudPanel.cs` owns ghost/health/stamina animation, pips, banner, and debug visibility.
- Tests: `HudMath.StaminaFlashActive` blink table; build 0/0, 276 tests, self-test 16/16.
- Verified live: HUD at 1600×900, ghost chunk after a hit, round pips/banner on a new round. Stamina flash logic unit-tested; live depletion was not captured because the opponent won the round first.

**Done when:** HUD updates match stats exactly at 60 FPS; damage ghost and stamina flash are visibly correct in a scripted trade.

### [x] T7.3 — Pause, options, results
**Goal:** the loop is complete.
- `Ui/PauseMenu.xmmp` + `PauseMenu.cs`: PAUSED overlay with Resume / Options / Quit to menu; options sub-panel with Volume ±10%, Screen shake, Debug overlay, Difficulty (Novice/Adept/Master), Back.
- `GameSettings` is the session store shared by the main menu and pause screen (volume, shake, debug default, difficulty). `ApplySettings` pushes shake/debug/volume (via `SoundEffect.MasterVolume`) every frame; `--profile` seeds the initial difficulty.
- Pause sets `_paused` so the fixed-step simulation, clips, and physics freeze; the world keeps rendering under a dark overlay. Resume clears focus and latches the click so the menu click cannot swing.
- Results appear when `DuelDirector` reaches `MatchOver`: VICTORY/DEFEAT, score, `R1 WIN · R2 LOSS` history, total hits/damage, blocks/timed blocks/breaks, Rematch and Menu. Esc from results returns to the menu.
- Match totals are accumulated across rounds; per-round winners are recorded at each `ReportDeath`. Round results and totals reset on a new match.
- Tests: `GameSettingsTests` defaults; visual pause, options, results, and resume at 1600×900.

**Done when:** pausing during an active swing freezes the blade mid-air and resumes exactly; options persist for the session.

### [ ] T7.4 — UI/gameplay input ownership
**Goal:** no click leaks into a swing.
- `InputRouter` gives Myra exclusive input while any blocking screen is open.
- Losing window focus pauses the game and drops buffered intents.
- Clicking a menu button can never also trigger an attack on the frame the menu closes.

**Done when:** a click on "Resume" does not produce a slash; alt-tabbing mid-swing pauses safely.

---

## M8 — Polish & release

### [x] T8.0 — Even arena columns
**Goal:** the arena reads as a ring from every camera preset.
- Replace the four hand-placed pillars (clustered on +Z) with eight pillars at equal angles on a 5.7 m radius.
- `Arena.cs` keeps the count and radius as named constants; no other geometry changes.
- Verified in gameplay and the F3 preset.

**Done when:** the eight columns are equally spaced around the ring and visible from the gameplay and F3 presets.

### [ ] T8.1 — Hit VFX from primitives
**Goal:** contact reads clearly without art assets.
- Block ring (torus flash), guard-break shatter (deformed box shards).
- Pooled, capped at ~256 particles, zero allocations during combat.

**Done when:** clean hit / block / guard break are distinguishable from a still frame; particle count never exceeds the cap.

### [ ] T8.2 — Audio hooks
**Goal:** placeholder feedback, swappable later.
- `IAudio` abstraction with procedural beeps/clicks for swing, hit, block, guard break, death.
- Volume from options; no audio files committed (generated tones only).

**Done when:** every combat event has distinct feedback; muting in options silences everything.

### [ ] T8.3 — Duel presentation
**Goal:** a duel has a beginning, a middle, and an end.
- Intro: camera push-in, "Round 1 — Fight!" banner, weapons raised.
- Victory: winner's sword raised, loser's dropped, 2 s hold, results transition.
- Rematch without returning to menu.

**Done when:** a full best-of-three plays start to finish with no dead frames or stuck screens.

### [ ] T8.4 — Performance pass
**Goal:** a locked 60 FPS on modest hardware.
- Merge static fighter parts into cached `DrMesh` groups where possible (fewer draw calls, same visuals).
- Profile: update, physics sweeps, draw. Budget: 2 ms sim, 6 ms draw.
- Kill per-frame allocations in combat paths (verify with `dotnet-counters`).

**Done when:** steady 60 FPS with overlay + particles in the worst-case power-attack exchange; allocated bytes per frame near zero.

### [ ] T8.5 — Balance and feel pass
**Goal:** the numbers in [Design/combat](Design/combat.md) are actually fun.
- Tune timings/damage/stamina so a competent player wins ~50% vs Adept and loses to Master.
- No dominant strategy: block-turtling must lose to guard breaks; mashing must lose to timed blocks.
- Record final numbers back into `Design/combat.md` and `CombatTuning.cs`.

**Done when:** 20 self-played duels show varied outcomes and both defensive and offensive play are viable.

### [ ] T8.6 — Release checklist
**Goal:** ship it.
- README build/run section verified on a clean clone and clean machine.
- `dotnet build -c Release SkyrimMelee.slnx` warning-free; `dotnet test` and `--self-test` green.
- Version tag, release notes in the tag message, screenshots/GIF generated from the game itself.

**Done when:** a fresh clone builds and plays with only the documented prerequisites; tag pushed.

---

## Backlog (not scheduled)

Ideas that fit the constraints but are out of scope for v1. Promote into a milestone only with developer agreement.

- Directional power attacks (forward dash / backstep / side sweep): cut for scope; the standing overhead stays.
- Greatsword variants with different reach/speed/damage (iron, steel, ebony) — visuals via primitive proportions + palette.
- Power bash (hold LMB while blocking): 55 stamina, ×1.5 damage/stagger, needs a new bash clip variant.
- Sprinting power attacks (Skyrim's `Great Critical Charge`): needs sprint state + stamina drain.
- Kill cam / finishing move presentation for the last blow of a round.
- Armor tiers that visibly change the statue (pauldrons as boxes/capsules) and armor rating.
- Arena variants (ruined courtyard, snowy pass) built from the same primitives.
- Parry → riposte window with a dedicated punish clip.
- Replay / ghost of the last duel, driven by the deterministic input track.
- Gamepad support.
- Procedural audio replacing placeholder tones.
- Two-on-one encounters (multiple static fighters, AI target selection).
- Photo mode with free camera and clip scrubbing.
- First-person weapon presentation: offset/scale the sword rig so the hilt sits further from the eye.
- Native AOT / trimming experiment.
- Implement `--god` (documented clamp at 1 health; currently parsed but unused).

## Risks & unknowns

The live risk register (R1–R6, with retirement tasks) lives in [Design/overview.md](Design/overview.md#risks). Update it there; do not duplicate it here.

## Done log

| Task | Date | Commit | Confirmed by |
| --- | --- | --- | --- |
| T0.1 — toolchain verification | 2026-11-19 | `8b19835` | developer |
| T0.2 — solution skeleton + xUnit | 2026-11-19 | `9bd499d` | developer |
| T0.3 — Myra host + debug label | 2026-11-19 | `e927166` | developer |
| T0.4 — clock, flags, overlay | 2026-11-19 | `7e66931` | developer |
| T1.1 — primitive cache + showcase | 2026-11-19 | `80917e4` | developer |
| T1.2 — arena, camera, forward renderer | 2026-11-19 | `648c0a9` | developer |
| T1.3 — fighter rigs & palettes | 2026-11-19 | `fa3ce24` | developer |
| T1.4 — greatswords & hand pivot | 2026-11-19 | `6b5a248` | developer |
| T1.5 — HUD + debug markup | 2026-11-19 | `6b5a248` | developer |
| T2.1 — sword clip system | 2026-11-19 | `266c883` | developer |
| T2.2 — SlashLeft clip | 2026-11-19 | `5602692` | developer |
| T2.3 — guard clips | 2026-11-19 | `01819f2` | developer |
| T2.4 — clip → blade segment | 2026-11-19 | `71c3ac4` | developer |
| T2.5 — combat state machine | 2026-11-19 | `a7954ae` | developer |
| T2.6 — SlashRight clip | 2026-11-19 | `0667ba5` | developer |
| T2.7 — Overhead clip | 2026-11-19 | `66b0bd8` | developer |
| T2.8 — PowerSlash clip | 2026-11-19 | `0960141` | developer |
| T2.9 — Three-attack moveset | 2026-11-19 | `2243f3d` | developer |
| T2.10 — Reaction clips | 2026-11-19 | `d9f4d14` | developer |
| T3.1 — Bepu hurtboxes + primitive body | 2026-11-19 | `588a8f4` | developer |
| T3.2 — Swept blade cast + hit tint | 2026-11-19 | `12c6f98` | developer |
| T3.3 — Swing hit events | 2026-11-19 | `c08fb04` | developer |
| T3.4 — Blade-vs-blade blocks | 2026-11-19 | `28ff76a` | developer |
| T3.5 — Headless self-test | 2026-11-19 | `0ee29e0` | developer |
| T4.1 — Stats, stamina, vitals HUD | 2026-11-19 | `0bc4c85` | developer |
| T4.2 — Damage pipeline | 2026-11-19 | `0c4d6d4` | developer |
| T4.3 — Poise, stagger, block bash | 2026-11-20 | `521ebce` | developer |
| T4.4 — Timed block & guard break | 2026-11-20 | `8f5fc89` | developer |
| T4.5 — Death, rounds, reset | 2026-11-20 | `b47d3ee` | developer |
| T5.1 — Input intent buffer | 2026-11-20 | `e220f9a` | developer |
| T5.2 — Skyrim locomotion, camera, input feel, hit feedback | 2026-11-20 | `0b6c643` | developer |
| T5.3 — First/third person, zoom, bash key (and blade-side slash fix) | 2026-11-20 | `1808e37` | developer |
| T5.4 — Camera shake, FOV punch, idle drift, F3 presets | 2026-11-20 | `c9091ea` | developer |
| T5.5 — Debug overlay v2 (trail, contact, tuning, bash guard fixes) | 2026-11-20 | `fc45e65` | developer |
| T6.1 — Reactive duelist AI (movement, two-way combat, bash) | 2026-11-20 | `7a93a4a` | developer |
| T4.6 — Guard coverage cone | 2026-11-20 | `7a93a4a` | developer |
| T5.7 — Split game code into top-level folders | 2026-11-20 | `0b6c643` | developer |
| T6.2 — Difficulty profiles | 2026-11-20 | `55dcf9c` | developer |
| T4.7 — Bash interrupts, it does not stun | 2026-11-20 | `bf5742b` | developer |
| T6.3 — AI visualization | 2026-11-20 | `3712217` | developer |
| T7.1 — Skin + main menu | 2026-11-20 | `ebcffdb` | developer |
| T7.2 — HUD | 2026-11-20 | `247ee68` | developer |
| T6.4 — Arena health potions | 2026-11-20 | `c8199c6` | developer |
| T4.8 — One damage instance per swing | 2026-11-20 | `8e6e01b` | developer |
| T7.3 — Pause, options, results | 2026-11-20 | PENDING | developer |
| T8.0 — Even arena columns | 2026-11-20 | `0e90144` | developer |


