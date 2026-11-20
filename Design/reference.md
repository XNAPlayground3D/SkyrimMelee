# Skyrim melee reference

What Skyrim actually does when two humans with greatswords fight, limited to the
scope of this project. This is the **reference**, not the spec: the spec is
[combat.md](combat.md). Where SkyrimMelee differs on purpose, the difference is
listed in [Deliberate divergences](#deliberate-divergences) with a reason.

Everything below is summarised from the Unofficial Elder Scrolls Pages (UESP),
retrieved 2026-11-20:

- [Skyrim:Combat](https://en.uesp.net/wiki/Skyrim:Combat) — power attacks, staggering, damage.
- [Skyrim:Block](https://en.uesp.net/wiki/Skyrim:Block) — blocking maths, bash, cones, movement.
- [Skyrim:Two-handed](https://en.uesp.net/wiki/Skyrim:Two-handed) — greatsword speed, reach, stagger.
- [Skyrim:Controls](https://en.uesp.net/wiki/Skyrim:Controls) — inputs and view toggling.
- [Skyrim:Stamina](https://en.uesp.net/wiki/Skyrim:Stamina) — costs and regeneration.
- [Skyrim:Transport](https://en.uesp.net/wiki/Skyrim:Transport) — movement speeds.

Out of scope: magic, shouts, archery, sneaking, mounted combat, brawls, kill cams,
perks, smithing, potions, and anything with more than two combatants.

---

## 1. Inputs (PC, default bindings)

| Skyrim control | Binding | What it does |
| --- | --- | --- |
| Move | `W A S D` | Camera-relative movement; the body turns with the camera, not with the travel vector |
| Look | Mouse | Yaw/pitch the camera; the character faces the camera forward |
| Attack | `LMB` tap | One normal swing; repeated presses chain alternating left/right swings |
| Power attack | `LMB` hold | Charged swing, released on the button release; costs stamina |
| Block | `RMB` hold | Weapon block for two-handed weapons; movement slows while held |
| Bash | `LMB` tap **while blocking** | Short shove with the weapon; costs stamina |
| Power bash | `LMB` hold while blocking | Perk-gated heavier bash |
| Change view | `F` or mouse wheel | Toggles first/third person; holding `F` in third person orbits the camera |
| Zoom | Mouse wheel | Third-person camera distance |
| Sprint | `Alt` | Faster than running; drains stamina |

Notes:

- Attack direction (left/right/forward/side/back) comes from the **movement input
  held at the moment of the attack**, not from a separate attack key.
- Blocking is a hold, not a toggle. There is **no timed-block, parry, or riposte
  window** anywhere in Skyrim's melee system.
- The view toggle is `F` **or** the wheel, which is why `F` cannot stay bound to
  bash in this project; bash is on `B` (or `LMB` while blocking).

## 2. Normal attacks

| Property | Value |
| --- | --- |
| Greatsword speed | 0.7 (`0.75` with the Unofficial Patch — see the UESP bug note) |
| Base two-handed attack duration | 34/45 s ≈ **0.756 s** |
| Swings per second | speed / 0.756 ≈ **0.93** at speed 0.7 (≈ **1.08 s** per swing) |
| Reach | **1.3** (weapon record units; longer than one-handed weapons) |
| Stagger multiplier | **1.1** (battle axe 1.15, warhammer 1.25) |
| Damage | weapon base damage × skill/perk multipliers |
| Stamina | **free** for normal attacks |

- Greatswords and battle axes swing at the same speed; warhammers are slower
  (0.6) and hit harder.
- A normal attack is a committed animation: input during wind-up/active is
  ignored, and only stagger or death interrupt it.
- The swing direction follows the blade: from the resting right-side stance a
  tap always starts the same cut; pressing again while that cut is still playing
  answers from the other side (the blade finished there). It is **not** a fixed
  every-second-press alternation — pause and the next swing starts from rest.
- Combo hits do not wait for the weapon to return to idle: the next swing starts
  out of the current attack's recovery, from the follow-through. Only a pause
  lets the blade settle back to the rest stance.
- Holding the button does **not** repeat attacks — it charges a power attack.

## 3. Power attacks

| Property | Value |
| --- | --- |
| Trigger | Hold the attack button, then release |
| Damage | **×2** a normal attack |
| Stagger | ×1 (but a much higher stagger *chance*) |
| Stamina cost | `(40 + 2 × weapon weight) × cost multiplier` — a steel greatsword (weight 17) costs **74** |
| At 0 stamina | The stamina bar flashes green and a **normal attack** is performed instead |
| Block interaction | A blocked power attack reduces block effectiveness by a factor of **0.66** |

Directional variants, chosen by the movement input at the moment of the power
attack:

| Input held | Variant | Effect |
| --- | --- | --- |
| Forward | Dash strike | Steps into the target while swinging |
| Backward | Backstep strike | Steps away, then swings |
| Left / right | Side strike | Sidesteps the incoming attack; with the Sweep perk a two-handed side power attack hits the whole forward arc |
| None | Standing power attack | In place |

Two-handed power attacks are **much more likely to stagger** than one-handed
ones, whether or not the target blocks.

## 4. Blocking

| Property | Value |
| --- | --- |
| Input | Hold `RMB` (two-handed weapons can block) |
| Coverage | **35° cone** in front of the actor (`fCombatHitConeAngle`) |
| Weapon block base | **30%** reduction (`fBlockWeaponBase`), scaling with the attacker's weapon damage and the defender's Block skill |
| Block cap | **85%** damage reduction; stacks with armor up to ~97% |
| Power attacks | Blocked percentage × **0.66** |
| Stamina | Blocking an incoming hit consumes a **small** amount of stamina |
| Movement | Blocking forward ≈ walk (81 raw ≈ 3.8 ft/s ≈ **1.16 m/s**), blocking backward ≈ 71 raw ≈ 1.01 m/s |
| Block Runner perk | Removes most of the movement penalty |

There is no directional block, no perfect block, and no guard meter. A blocked
hit still costs stamina; a power attack still gets through for a fraction, and
can guard-break when stamina runs out.

## 5. Bash

| Property | Value |
| --- | --- |
| Input | Tap attack **while blocking** |
| Damage | ×0.5 of a normal attack (`Deadly Bash` perk ×5) |
| Stagger | ×0.5, chance-based |
| Stamina | **35** |
| Interrupt | Can stagger and can interrupt an opponent's power attack |
| Follow-up | You stay in the block stance; bashing does not lower the guard |

Power bash (perk): hold attack while blocking, 55 stamina, ×1.5 damage and ×1.5
stagger.

Bash damage scales with the weapon's base damage, not with Block enchantments.
A bash is a short, planted shove: the actor does not travel during it.

## 6. Staggering

- Power attacks are the main source; normal attacks stagger only via perks,
  shouts, or high stagger multipliers.
- A stagger **interrupts the interrupted action** (swing, block, bash) and
  stuns the actor for a short time.
- Staggered **NPCs/creatures stop moving**. The player can still move while
  staggered (flagged unverified on the UESP talk page).
- There is **no poise meter**: stagger is a chance per hit, not an accumulating
  resource.
- Ragdoll knockdowns (shouts, Warmaster) are outside this project's scope.

## 7. Stamina

| Action | Cost |
| --- | --- |
| Normal attack | 0 |
| Power attack | (40 + 2 × weight) × multiplier |
| Bash | 35 |
| Power bash | 55 |
| Sprint | 7/s (+2% per worn weight) |
| Blocking a hit | small, proportional to the blocked damage |

- Regeneration: **1.75% of max per second in combat**, 5%/s out of combat.
- Any stamina-consuming action can start while stamina is **above zero**; the
  action then drains the bar and imposes a regeneration delay proportional to
  the deficit (capped at 5 s). Only at **0** stamina does the action fail —
  power attacks degrade to a normal attack instead.
- The bar flashes green when the player tries to act at 0 stamina.

## 8. Movement speeds (for scale)

Raw units and conversions from the UESP table:

| State | Raw | ft/s | m/s | Fraction of run |
| --- | --- | --- | --- | --- |
| Sprint forward | 500 | 23.4 | 7.13 | 1.35× |
| **Run forward** | **370** | **17.3** | **5.27** | **1.00×** |
| Run backward | 205 | 9.6 | 2.93 | 0.55× |
| Walk forward | 80 | 3.8 | 1.16 | 0.22× |
| Walk backward | 72 | 3.3 | 1.01 | 0.19× |
| Block forward | 81 | 3.8 | 1.16 | 0.22× |
| Block backward | 71 | 3.3 | 1.01 | 0.19× |
| Sneak walk | 47 | 2.2 | 0.67 | 0.13× |

- Strafing is not listed separately by UESP; it is slower than the forward run
  and does not rotate the body.
- Attacking does not stop movement in Skyrim; the swing plays while the actor
  keeps moving at a reduced speed. A bash is the exception: it is planted.

## 9. Camera and view

- `F` or the mouse wheel toggles first/third person. Holding `F` in third person
  lets the mouse orbit without turning the character.
- The wheel zooms the third-person distance and (with Eagle Eye) the bow zoom;
  for melee it is purely a camera control.
- Third person is an over-the-shoulder offset, not a centered orbit.
- Losing focus, opening a menu, or dying (ragdoll) forces third person in some
  cases; the project already pauses on focus loss (T7.4).

## 10. Death and rounds

Skyrim has no rounds: a dead actor ragdolls and stays down. Finishing moves and
the kill cam are presentation only. SkyrimMelee uses best-of-three rounds for
arena pacing, which has no Skyrim equivalent.

---

## 11. How SkyrimMelee maps to the reference

| Mechanic | Skyrim | SkyrimMelee today | Status |
| --- | --- | --- | --- |
| Attack button | `LMB` tap; the arc follows the blade (rest = same cut, chained press = other side) | `LMB` tap; rest = `SlashLeft`, chained press = the other side (`SlashCombo`); `1`/`2` force a side | ✅ matches |
| Power attack | Hold `LMB` | Hold `LMB` for `PowerSlashHoldTime` (350 ms) | ✅ matches |
| Power damage | ×2 | ×2 | ✅ matches |
| Bash input | Tap `LMB` while blocking | Tap `LMB` (or `B`) while blocking | ✅ matches |
| Bash damage | ×0.5 | ×0.5 | ✅ matches |
| Bash movement | Planted | `LocksMovement` roots the fighter | ✅ matches |
| Attack movement | Reduced, not stopped | 0.25× walk | 🚧 tuned lower than Skyrim |
| Block movement | ≈0.22× run | 0.45× walk | 🚧 tuned higher than Skyrim |
| Block coverage | 35° frontal cone | Physical guard-blade capsule only | ⬜ T4.6 |
| Block maths | 30% base, 85% cap, ×0.66 vs power | Timed-block model (project invention) | ⚠️ deliberate |
| Stamina gate | Action allowed above 0 stamina; power degrades at 0 | Same (`StaminaRules`); power falls back to a slash at 0 | ✅ matches |
| Stamina costs | 74 power / 35 bash of a 100 pool with slow regen | 30 power / 15 bash of 100 with 25/s regen | ⚠️ deliberate (arena pacing) |
| Stagger | Chance-based, interrupt | Poise meter: light hits chip, power always staggers | ⚠️ deliberate (deterministic) |
| Directional power attacks | Forward dash, backstep, sidestep | Not implemented | ⬜ T5.6 |
| Camera toggle | `F` / wheel | `F` toggles view; wheel zooms (Skyrim's wheel toggle deferred) | ✅ matches (zoom) |
| First/third person | Both | Both: eye-level first person with the capsule hidden | ✅ matches |
| Death | Ragdoll, no rounds | Rigid topple + best-of-three rounds | ⚠️ deliberate |

## Deliberate divergences

These are choices, not gaps. Each one is kept because of a project constraint.

1. **Poise meter instead of chance stagger.** Determinism (AGENTS.md) forbids
   `Random` in combat resolution, and a chance-based stagger would make the
   self-test non-reproducible. Light hits chip poise, power attacks guarantee a
   stagger. [combat.md](combat.md#poise-and-stagger)
2. **Timed block.** Skyrim has no parry window. The project adds a 250 ms
   perfect-block window to give skilled play a reward and to make block timing
   legible in a duel. It is layered on top of, not instead of, the block damage
   model.
3. **Faster stamina economy.** Skyrim's 74-stamina power attack and 1.75%/s
   regen would make a 30-second arena round mostly walking. The project uses a
   30-cost power attack with 25/s regen so a round has several exchanges.
   Costs still live in `CombatTuning.cs`.
4. **Slower free movement.** Skyrim runs at 5.27 m/s; the arena is 5.5 m in
   radius, so the project walks at 3.2 m/s to keep spacing readable. Attack
   (0.25×) and guard (0.45×) multipliers are relative to that number.
5. **Rigid bodies.** No skeletal animation is allowed, so a stagger or death is
   one rigid transform of the primitive assembly, not a jointed reaction.
6. **Best-of-three rounds.** Skyrim has a single life; the project wants a
   repeatable duel, so [rounds and input](combat.md#rounds-and-input) exist.
7. **No kill cam, no finishing moves, no decapitations.** Presentation outside
   the primitive/rigid constraints.

## Known gaps (scheduled)

- **T4.6 — Guard coverage cone.** Skyrim blocks in a 35° cone; the project
  currently only blocks when the blade capsule intersects, so a visually correct
  block can still be a clean hit.
- **T5.6 — Directional power attacks.** Forward dash, backstep, and sidestep
  variants chosen by movement input, per §3.
- **Power bash** and **sprint power attacks** are listed in
  [TASKS.md](../TASKS.md#backlog-not-scheduled) until the base duel is tuned.
