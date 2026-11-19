# Combat

## Stats

| Stat | Default | Notes |
| --- | --- | --- |
| Health | 150 | ~6–8 landed hits |
| Stamina | 100 | +25/s after 0.6 s; no regen while guarding |
| Armor | 0 | 0.12 % reduction per point, cap 80 % (667) |
| Two-handed skill | 60 | Damage ×(1 + 0.4 × skill/100) |
| Block skill | 50 | Block reduction 0.30 + 0.45 × skill/100 |
| Poise | 100 | +25/s after 1 s unhit; empty ⇒ stagger |

Weapon: Iron Greatsword, base damage 20, reach 1.47 m.

## Actions

| Action | Input | Wind-up / Active / Recovery | Stamina | Damage × | Stagger |
| --- | --- | --- | --- | --- | --- |
| Slash | LMB | 250 / 220 / 350 | 12 | 1.00 | 10 |
| Overhead | `3` | 400 / 140 / 450 | 18 | 1.20 | 20 |
| Power slash | Shift+LMB | 600 / 160 / 700 | 30 | 1.80 | 45 |
| Power overhead | Shift+`3` | 750 / 180 / 850 | 40 | 2.20 | 70 |
| Guard raise / hold / lower | hold RMB | 100 / — / 150 | 0 | — | — |

`1` and `2` force left/right arcs for practice. LMB alternates arcs and starts with right-to-left.

- Stamina is charged at wind-up start. Insufficient stamina ⇒ power attack rejected (no state change, no cost).
- `LightSlash` from `Idle` alternates the arc; the clip determines the visual, the definition determines the phases.

## Sword motion

One animated transform: `swordWorld = fighterRoot × handRest × Pose(clipTime)`.

```csharp
record struct SwordPose(Vector3 GripOffset, Quaternion Rotation);
record struct SwordKey(float Time, SwordPose Pose, Easing EaseToNext);
record SwordClip(string Name, float Duration, bool Loop, SwordKey[] Keys);
```

Clips: `Idle`, `GuardRaise`, `GuardHold`, `GuardLower`, `SlashLeft`, `SlashRight`, `Overhead`, `PowerSlash`, `PowerOverhead`, `StaggerMicro`, `StaggerSmall`, `StaggerLarge`, `Death`, `Victory`.

Each attack clip covers its full wind-up + active + recovery and holds the telegraph pose for ≥ 100 ms. The active window is the only time the blade can hit. Poses are sampled at simulation time, so mesh and hitbox are identical. Clips cross-fade over 100 ms when the state changes.

T2.2's dev harness plays the single authored clip: `Space` / `P` pauses, `R` restarts, `Z` / `X` (or `-` / `=`) changes speed. The HUD shows the current clip name, speed multiplier, and progress (`50ms/120ms`); the F1 overlay shows the clip name and normalized time.

T2.2 authors **only** `SlashLeft` and its `AttackDefinition`. `SlashRight`, `Overhead`, `PowerSlash`, and `PowerOverhead` are separate tasks (T2.6–T2.9) so each can be perfected on its own. All phase timings, stamina costs, damage multipliers, and stagger values live in `CombatTuning.cs` and mirror the action table above. Each attack holds its telegraph pose for at least 100 ms before the active window; the validator in `AttackDefinitionTests` enforces it.

`SlashLeft` starts and ends in the idle pose: the hand sits at the right hip and the blade stands nearly vertical, matching Skyrim's standing greatsword idle. The clip is a body-orbit cut: the hands orbit a chest-height pivot at ~0.75 m radius, and the blade points radially outward, so the hands move tangentially while the tip cuts a wide arc through the opponent's space. The wind-up draws the hands behind the right shoulder with the tip high and right, then holds for 100 ms. The blade rolls on the way back so the **cutting edge** leads the sweep, not the flat. The active window is 220 ms so the cut is readable in slow motion; it carries the hands forward and across the chest, the tip drives ~1.0 m forward, and the hilt travels ~1.5 m to the left. The cut finishes low-left and returns to idle. The cut sweeps across the body in the fighter's Z axis, which is the screen plane from the third-person back camera. The blade's broad face is rolled toward the camera on every key so the sweep stays readable. Both hands are part of the sword rig and travel with the hilt; the white capsule body stays static.

| State | Clip | Exits |
| --- | --- | --- |
| `Idle` | `Idle` | attack, guard |
| `Windup` | attack clip | `Active`, `Stagger`, `Dead` |
| `Active` | attack clip | `Recovery`, `Stagger`, `Dead` |
| `Recovery` | attack clip | `Idle`, `Stagger`, `Dead` |
| `GuardRaise` | `GuardRaise` | `GuardHold` |
| `GuardHold` | `GuardHold` | `GuardLower`, attack |
| `GuardLower` | `GuardLower` | `Idle` |
| `Stagger` | `StaggerMicro/Small/Large` | `Idle`, `Dead` |
| `Dead` | `Death` | reset only |

Rules: wind-up/active/recovery can only be interrupted by stagger or death; facing is locked from wind-up to recovery end; only `GuardHold` blocks; guard may attack directly out of `GuardRaise/Hold`; death beats stagger.

Blade hitbox: local `(0, 0.18, 0)` → `(0, 1.47, 0)`, transformed to world, approximated by three spheres (r 0.055 / 0.040 / 0.025). See [architecture](architecture.md#physics) for the sweep.

## Damage

```text
raw        = 20 × damageMultiplier × (1 + 0.4 × skill / 100)
armored    = raw × (1 − min(0.80, armor × 0.0012))
blocked    = armored × (1 − blockReduction)        // 0.30 + 0.45 × blockSkill/100
timed      = armored × (1 − min(0.85, blockReduction + 0.15))
guardBreak = armored
```

Rounded once at the end (`AwayFromZero`), minimum 1 on any connecting hit. Example at skill 60 (`raw 24.8`):

| Situation (armor 300, skill 60) | Damage |
| --- | --- |
| Clean hit | 16 |
| Blocked (block skill 50) | 8 |
| Timed block (block skill 50) | 5 |
| Guard break | 16 |
| Power overhead clean | 35 |

## Blocking

A hit is blocked only if the defender is in `GuardHold`, the guard shape is registered, contact is with the guard blade, and the attacker is inside the front 120° cone.

| Timing / stamina | Outcome |
| --- | --- |
| Guard held > 250 ms | Block: reduced damage, stamina cost `0.6 × armored` |
| Guard raised 100–250 ms before impact | Timed block: stronger reduction, half stamina, attacker gets 250 ms `StaggerMicro` |
| Block cost > remaining stamina | Guard break: full damage, guard removed, 1000 ms stagger |
| From outside the cone, or while raising/lowering | Clean hit |

Stamina drain on a blocked hit is paid by the defender. A timed block is the reward for reading, not reacting.

## Poise and stagger

Every hit subtracts `Stagger`, and poise is restored at 100 on entering `Stagger`.

| Source | Duration | Clip |
| --- | --- | --- |
| Poise emptied (light/overhead) | 600 ms | `StaggerSmall` |
| Power attack clean hit, or poise emptied by one | 1000 ms | `StaggerLarge` |
| Timed-block recoil | 250 ms | `StaggerMicro` |
| Guard break | 1000 ms | `StaggerLarge` |
| Death | 1200 ms, terminal | `Death` |

Stagger cancels any action and blocks all input, guard included. Damage can still be taken.

## Edge cases

| Case | Rule |
| --- | --- |
| Both land on the same tick | Both resolve; trades are legal |
| Double KO | Round draw: no score, replay the round |
| Guard and body at equal time-of-impact | Block wins |
| Guard contact first, body later in the same swing | Swing consumed by the block |
| Same hurtbox twice in one swing | Ignored (per-swing hit set) |
| Attack connects on the last active tick | Resolves before entering recovery |
| Attacker dies mid-swing | Attack stops, no further hits |
| Stamina exactly equals cost | Allowed |

## Rounds and input

- Best of three, 5 s intro, 3 s post-death hold, full reset between rounds.
- Attack intents buffer for 120 ms; one pending intent, newest wins; blocked/rejected intents are consumed.
- Guard is level-triggered, never buffered. Input is snapshotted once per simulation tick; UI captures first.

All values live in `Combat/CombatTuning.cs`. Changing one requires updating the tables above in the same commit.
