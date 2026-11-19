# TASKS — SkyrimMelee roadmap

> **This document is a living board, not a contract.**
> The agent may split, merge, reorder, reword, or delete tasks whenever reality disagrees with the plan — as long as the result is still moving toward the goal described in [README.md](README.md) and [Design/](Design/overview.md). When you change the plan, say so in the commit message.
>
> **Next up:** `T1.3`

---

## How to read this file

- `[ ]` not started · `[~]` in progress · `[x]` done and confirmed by the developer
- The **next task is the top-most unchecked task**, unless the developer names a different one.
- A task is only `[x]` after the developer explicitly confirms it (see [AGENTS.md](AGENTS.md)).
- Each task lists **Goal** and **Done when**. "Done when" is the test plan, not a suggestion.
- **Specs live in [`Design/`](Design/overview.md), not here.** Tasks reference the relevant doc; the doc wins on details.
- Completed tasks are logged at the bottom with date and commit hash.
- The **Invariants** section must survive every rewrite.

## Invariants (do not delete or weaken)

1. No 3D model files, ever. Everything is built from `MeshPrimitives` primitives or hand-written vertex data.
2. No skeletal animation, no skinning, no rig hierarchy with animated joints. The body is a statue; only the sword (and its hand pivot) moves.
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
- `Debug/DebugOverlay.cs` scaffold (toggle with `F1`).

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

### [ ] T1.3 — Fighter statue from primitives
**Goal:** two recognizable humanoids, built only from boxes/capsules/spheres.
- `FighterRigBuilder`: head (sphere), neck + torso + pelvis (boxes/capsule), upper/lower arms (capsules), hands (boxes), thighs/shins (capsules), boots (boxes).
- One permanent combat stance pose, mirrored correctly, both facing each other at ±1.4 m on X.
- `FighterVisuals`: distinct palettes (steel-blue vs rust-red), so the two are readable at a glance.
- Wireframe toggle that outlines each primitive.

**Done when:** silhouettes read as humanoids from the gameplay camera; no part intersects the floor; toggling wireframe shows the exact primitive decomposition.

### [ ] T1.4 — The greatsword
**Goal:** the star of the show, and its pivot.
- `GreatswordRig`: blade (long box, slightly tapered by two stacked boxes), fuller (thin inset box), crossguard (box), grip (cylinder), pommel (sphere). Local origin at the grip.
- Hand pivot node parented to the right hand; sword's local transform is the only animated transform in the whole game.
- Dev handle: rotate the pivot with arrow keys/`IJKL` to sanity-check reach and framing; `F3` shows a debug gizmo at grip, mid-blade, and tip.
- Review the off-hand compromise from [Design/architecture](Design/architecture.md#the-two-handed-grip) and either accept it or implement the documented fallback.

**Done when:** sword looks like a two-handed greatsword, sits naturally in the right hand, and the dev rotation can sweep it through every pose without clipping the body.

---

## M2 — Sword motion ("animation")

### [ ] T2.1 — Sword clip system
**Goal:** replace hard-coded rotations with authored keyframe clips.
- `SwordClip` (duration, `SwordKey` list, per-segment easing), sampler with lerp + slerp.
- Clip registry with ID lookup, looping for idle/guard, one-shot for attacks.
- 100 ms cross-fade between outgoing and incoming clip poses.
- Unit-level check: sampling t=0 and t=1 returns the first/last key exactly.

**Done when:** dev harness can play any clip at any speed and the sword motion looks continuous (no snapping) across clip changes.

### [ ] T2.2 — Attack clips and definitions
**Goal:** the five attacks from [Design/combat](Design/combat.md#actions) exist, are readable, and are tunable.
- Clips: `SlashLeft`, `SlashRight`, `Overhead`, `PowerSlash`, `PowerOverhead` authored to match the timings table.
- `AttackDefinition`: clip, wind-up/active/recovery split, stamina cost, damage multiplier, stagger power, allowed hit windows.
- Wind-up must visually sit at the end of a telegraph pose for ≥100 ms before the active window opens.

**Done when:** slow-motion playback makes every attack's wind-up readable; timing constants match `CombatTuning.cs` exactly.

### [ ] T2.3 — Guard clips
**Goal:** blocking looks like a greatsword guard, not a T-pose.
- `GuardRaise` / `GuardHold` (loop, subtle 1–2° breathing) / `GuardLower`.
- Guard pose physically covers the torso from the front and places the blade between the fighters.
- `StaggerSmall` / `StaggerLarge` / `Death` / `Victory` clips.

**Done when:** raising/lowering guard has visible anticipation and follow-through; guard blade genuinely occludes the torso from the opponent's direction.

### [ ] T2.4 — Clip -> blade segment
**Goal:** one function that turns a pose into world-space blade geometry.
- `SwordClip` pose → grip world position + blade base/tip world positions (blade capsule: base radius, tip radius, length).
- Same values feed rendering, debug draw, and physics sweeps — no duplicate math.

**Done when:** debug draw of the physics blade capsule overlaps the rendered blade within 1 cm at every sampled time across every clip.

### [ ] T2.5 — Combat state machine (motion only)
**Goal:** input-less, damage-less sequencing: idle → wind-up → active → recovery → idle; idle → guard → idle; interruption by stagger.
- `CombatStateMachine` with explicit states, timers, and a transition table.
- Commit lock: no transition out of an attack except stagger/death.
- Guard raise/held/lower with 100 ms raise and 150 ms lower.

**Done when:** FSM debug view shows correct state/timer transitions; forced transitions cannot skip wind-up or active phases.

---

## M3 — Physics & hit detection

### [ ] T3.1 — Bepu world with static hurtboxes
**Goal:** collision shapes exist and can be queried.
- `CombatWorld` owns one `Simulation` (no gravity), one static floor, and per-fighter static shapes: torso capsule, head sphere, hips capsule.
- Guard blade registers/unregisters its static capsule as the guard state changes.
- `HurtboxSet` maps Bepu collidable handles back to `FighterId` + `HurtboxKind`.
- `F1` wireframe draws every registered shape in world space.

**Done when:** debug shapes match the rendered statue parts; unregistering the guard removes its collidable without leaking.

### [ ] T3.2 — Swept blade cast
**Goal:** fast swings cannot tunnel.
- `BladeSweep`: swept capsule from previous blade segment to current segment, executed per simulation substep with a custom Bepu hit handler.
- Collect all hits per substep: shape id, contact point, normal, time-of-impact.
- Hard cap on sweep distance per substep; assert if exceeded.

**Done when:** a scripted max-speed power swing against a torso always registers exactly one first-contact event, at 60 Hz and at simulated 10 FPS.

### [ ] T3.3 — Swing damage bookkeeping
**Goal:** one hit per target per swing, with correct contact data.
- Per-swing `HashSet` of already-hit hurtboxes; independent set per attack (not per fighter).
- `HitEvent` record: attacker, target, hurtbox kind, world point, normal, attack definition, active-window progress.
- Sparks spawn at contact point and orient to the normal.

**Done when:** a swing that passes through torso then head produces two hits (different hurtboxes) but only one per hurtbox; the hit set resets on the next attack.

### [ ] T3.4 — Blade-vs-blade block detection
**Goal:** an active guard genuinely intercepts the incoming blade.
- While guarding, the guard blade capsule is a query target (not just a visual).
- Sweep classification: guard-blade contact → block candidate; body contact → clean-hit candidate; earliest time-of-impact wins.
- Facing cone check (±60° around forward) and guard-raised timestamp determine block validity; timed block = within 250 ms of raise.

**Done when:** scripted swings at a guarding opponent in front always classify as block; swings from behind or during guard lowering classify as clean hits; earliest-TOI arbitration is verified with a deliberate double contact.

### [ ] T3.5 — Headless self-test harness
**Goal:** the whole combat sim is verifiable without a window.
- `--self-test` runs named, deterministic scenarios on a fixed timestep with a scripted input track (no RNG, no wall clock).
- Scenarios: clean hit, blocked hit, timed block, guard break, stagger interrupt, whiff, one-hit-per-swing, tunneling stress.
- Prints PASS/FAIL per scenario, exits non-zero on failure.

**Done when:** all scenarios pass on a cold machine; results are identical across 10 consecutive runs.

---

## M4 — Combat rules

### [ ] T4.1 — Stats and stamina economy
**Goal:** stamina is a resource you feel.
- `FighterStats`: health, max health, stamina, armor rating, two-handed skill, block skill, poise.
- Regen: 25/s stamina after 0.6 s delay; disabled while blocking; poise regen 25/s after 1 s.
- Costs charged at wind-up; power attack rejected below cost (with a UI cue); light attacks always allowed.

**Done when:** stamina math is covered by xUnit or self-test assertions; a rejected power attack consumes no stamina and plays no clip.

### [ ] T4.2 — Damage pipeline
**Goal:** the [Design/combat](Design/combat.md#damage) formula, implemented once, tested in isolation.
- `DamageModel` pure functions: base → armor → block → timed block → guard break.
- Return a breakdown struct (raw, armored, final, reduction %, stamina cost) for the debug overlay.
- Deterministic rounding rules (no banker's rounding surprises).

**Done when:** xUnit table-driven tests cover armor 0/300/567/900, block skill 0/50/100, timed/non-timed/guard-break, and match the documented formula exactly.

### [ ] T4.3 — Poise and stagger
**Goal:** heavy hits interrupt; light hits chip.
- Poise damage from `AttackDefinition.Stagger` applied on hit; poise zero → stagger state with duration from attack weight.
- Stagger cancels wind-up/active/recovery and guard; it cannot be cancelled.
- Power attacks always stagger on clean hit.

**Done when:** scripted trades produce the expected interrupt windows; a staggered fighter cannot act until the timer expires.

### [ ] T4.4 — Timed block and guard break
**Goal:** the two skill-expressive defensive outcomes.
- Timed block: +0.15 reduction (cap 0.85), half stamina drain, 150 ms attacker micro-stagger.
- Guard break: stamina cost > remaining stamina → full damage, guard force-lowered, long stagger, distinct VFX/sound cue.
- Guard cannot be re-raised while staggered.

**Done when:** all three defensive outcomes (clean hit / block / guard break) are reproducible from the same incoming attack by varying guard timing and stamina.

### [ ] T4.5 — Death, rounds, reset
**Goal:** a complete duel loop without UI.
- Health ≤ 0 → death clip, 3 s hold, round scored, fighters reset (stats, clips, shapes, hit sets).
- Best of three; `DuelDirector` exposes round number, score, and phase.
- Reset must leave no state behind (verified by running the same scenario twice).

**Done when:** a scripted best-of-three completes correctly; back-to-back duels produce byte-identical self-test output.

---

## M5 — Player, camera, feel

### [ ] T5.1 — Player input and intent buffering
**Goal:** inputs feel responsive without breaking commitment.
- `InputRouter` with UI-first capture (`MyraEnvironment` gets first claim when a screen is open).
- 120 ms input buffer for attacks and guard raise; buffered intents fire on the first legal frame.
- Rejected actions (insufficient stamina, mid-swing) surface in the debug overlay, not as silence.

**Done when:** mashing during recovery yields exactly one queued attack; holding block during recovery raises guard on the first legal frame.

### [ ] T5.2 — Camera feel
**Goal:** impact is felt in the camera, not just seen.
- Impact shake (health- and block-scaled), 3–5 frame decay, optional disable in settings.
- Slight FOV punch on power attacks; slow drift during idle.
- `F3` cycles presets: gameplay 3/4, side profile, top-down debug.

**Done when:** shake is noticeable but never hides a wind-up; presets all frame both fighters.

### [ ] T5.3 — Debug overlay v2
**Goal:** tune combat without a debugger.
- Live: state + timer, stamina/health bars in text, current clip and normalized time, active-window flag, poise.
- Collision: swept capsule trail for the last 20 substeps, contact points, block classification reason.
- Tuning hotkeys: `[`/`]` to nudge a selected timing value by ±10 ms (session-only, logged for copy-paste into `CombatTuning.cs`).

**Done when:** an entire tuning session can be done in-game with the overlay, and the logged values can be pasted into the tuning file unchanged.

---

## M6 — Opponent AI

### [ ] T6.1 — Reactive duelist AI
**Goal:** a believable sparring partner with no movement.
- Behavior: observe wind-up → choose block / trade / punish recovery / power attack; guard-break opportunism when player stamina is low.
- Reaction delay per profile; mistake chance (late block, greedy attack).
- Outputs `CombatIntent` only — AI cannot cheat the state machine.

**Done when:** a 2-minute fight shows all of: clean hits, blocks, timed blocks, guard breaks, and staggers from both sides; AI never uses an illegal transition.

### [ ] T6.2 — Difficulty profiles
**Goal:** three opponents worth beating.
- `AiProfile`: reaction ms, block accuracy, timed-block chance, aggression, power-attack frequency.
- Presets: Novice, Adept, Master. Selectable from the main menu.
- Data-only: profiles must not require new code to tune.

**Done when:** Master beats Novice in ≥8 of 10 headless AI-vs-AI duels; Novice still lands occasional hits.

### [ ] T6.3 — AI visualization
**Goal:** understand (and debug) the opponent's decisions.
- Overlay of current AI intent, reaction timer, and chosen response with its reason.
- Per-fight stats: attacks thrown, hit rate, block rate, guard breaks suffered.

**Done when:** every AI decision shown in the overlay corresponds to a state-machine event in the log.

---

## M7 — Myra UI

### [ ] T7.1 — Skin + main menu
**Goal:** it looks like a game, not a debug harness.
- `UiSkin`: flat, low-contrast, parchment/steel palette; one custom font size set (title, body, small).
- Main menu: Duel / Practice / Options / Quit, keyboard + mouse navigation, focus ring.
- Screen transitions with a short fade.

**Done when:** every menu item is reachable by keyboard and mouse; no text renders blurry at 1600×900 or after resize.

### [ ] T7.2 — HUD
**Goal:** read the fight at a glance, Skyrim-style.
- Player + opponent: health bar (delayed "damage ghost" chunk), stamina bar (flash on insufficient stamina), name label, round pips.
- Center-top round banner with intro/outro timers.
- Bars animate on change; never allocate per frame.

**Done when:** HUD updates match stats exactly at 60 FPS; damage ghost and stamina flash are visibly correct in a scripted trade.

### [ ] T7.3 — Pause, options, results
**Goal:** the loop is complete.
- Pause (`Esc`): resume, options, quit to menu; simulation fully frozen.
- Options: master volume, screen shake on/off, debug overlay default, difficulty preset.
- Results: victory/defeat, score, per-round stats, rematch / menu.

**Done when:** pausing during an active swing freezes the blade mid-air and resumes exactly; options persist for the session.

### [ ] T7.4 — UI/gameplay input ownership
**Goal:** no click leaks into a swing.
- `InputRouter` gives Myra exclusive input while any blocking screen is open.
- Losing window focus pauses the game and drops buffered intents.
- Clicking a menu button can never also trigger an attack on the frame the menu closes.

**Done when:** a click on "Resume" does not produce a slash; alt-tabbing mid-swing pauses safely.

---

## M8 — Polish & release

### [ ] T8.1 — Hit VFX from primitives
**Goal:** contact reads clearly without art assets.
- Spark burst (small boxes with velocity + gravity + lifetime), block ring (torus flash), guard-break shatter (deformed box shards), subtle hit-flash on the struck fighter's material color.
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

- Greatsword variants with different reach/speed/damage (iron, steel, ebony) — visuals via primitive proportions + palette.
- Armor tiers that visibly change the statue (pauldrons as boxes/capsules) and armor rating.
- Arena variants (ruined courtyard, snowy pass) built from the same primitives.
- Parry → riposte window with a dedicated punish clip.
- Replay / ghost of the last duel, driven by the deterministic input track.
- Gamepad support.
- Procedural audio replacing placeholder tones.
- Two-on-one encounters (multiple static fighters, AI target selection).
- Photo mode with free camera and clip scrubbing.
- Native AOT / trimming experiment.

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
| T1.2 — arena, camera, forward renderer | 2026-11-19 | _pending_ | developer |
