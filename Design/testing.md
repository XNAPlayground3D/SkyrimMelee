# Testing

## Pyramid

| Layer | What | Gate |
| --- | --- | --- |
| Pure functions | Damage, poise, clip sampling, tuning validation | xUnit in `SkyrimMelee.Tests`; every task touching them |
| Simulation | Full combat rules headless, scripted intents, fixed seeds | Every combat/physics task |
| Visual | Rendering, camera, UI, VFX, feel | Every rendering/UI task |
| Balance | AI vs AI and self-play statistics | T6.2, T8.5 |

Pure logic gets xUnit coverage; combat scenarios run through `--self-test` because they need the real game loop. A behaviour without a scenario is not tested — new combat behaviour ships with its scenario in the same task.

## Determinism

Given identical initial state and intents, tick _n_ must be byte-identical: same health, stamina, poise, states, clip times, blade endpoints, contacts. Run every scenario twice and compare a rolling state hash; print the first diverging tick on failure. No RNG (except seeded AI), no wall clock, no threads, no dictionary-order decisions.

## Harness

`--self-test` runs without a window using the real state machines, tuning, clips, and physics. `SelfTestRunner` builds a fresh `CombatWorld` per scenario and scripts blade sweeps / FSM transitions directly, so every run starts from identical state. Output is one `PASS/FAIL` line per scenario; a failure exits non-zero. The xUnit project asserts all scenarios pass and that 10 consecutive runs are identical.

Current scenarios: clean hit, blocked hit, timed block, no stamina power→slash, power stagger, poise stagger, bash, defense outcomes, best of three, input buffer, ai duel, health potion, guard break, stagger interrupt, whiff, one hit per swing, tunneling stress.

## Scenarios

| ID | Checks |
| --- | --- |
| `DM-01/02` | Damage table and rounding across armor, block skill, timed/guard-break |
| `CL-01/02` | Clip sampling endpoints, loop continuity |
| `CL-03/04` | Telegraph hold ≥ 100 ms, active window contains the centreline crossing |
| `CL-05` | Blade never intersects the fighter's own body (120 Hz sampling) |
| `CL-06` | Reaction body clips are jointless; staggers recover, the death capsule rests on the floor |
| `PH-01` | Static hurtboxes map to fighter parts; the guard capsule adds/removes with no leftover collidable |
| `PH-02` | `PowerSlash` max-speed sweep hits the target torso first, once, at 60 Hz and 10 FPS; no tunneling |
| `ST-18` | `HitEvent` carries attacker/target/kind/point/normal/attack/progress; one hit per hurtbox per swing; a new swing resets the set |
| `ST-19` | Guard in front blocks; behind/lowering/body-first are clean; ties favour the guard; a fresh guard is a timed block |
| `ST-20` | Stamina spend/regen delay, guard freeze, poise regen, and power-attack rejection with no cost or state change |
| `ST-21` | Same attack yields clean, block, timed block, and guard break by timing/stamina; micro-stagger locks the attacker |
| `ST-01/02` | Clean hit lands; attack with no hurtboxes whiffs and recovers |
| `ST-03/04` | Normal block reduction and drain; timed block at 180 ms after raise |
| `ST-05` | Power slash into 5 stamina ⇒ guard break, guard removed, 1000 ms stagger |
| `ST-06/07` | Off-axis hit is not blocked; hitting a wind-up interrupts the attack |
| `ST-08/09` | One hit per hurtbox per swing; poses 1.2 m apart in one tick still connect |
| `ST-10/11` | Double KO draws; equal-TOI guard beats body |
| `ST-12/13` | Reset reproduces state; power attack at 10 stamina is rejected with no cost |
| `ST-14/15` | Poise empties after ten light hits; 120 ms buffer fires on the first legal tick |
| `ST-16/17` | Guard released during raise does not block; `--god` clamps at 1 health |
| `AI-01` | Master beats Novice in ≥ 8/10 fixed-seed duels; every pairing shows blocks, timed blocks, guard breaks |
| `AI-02` | Decision telemetry carries action/reason and a stable id; the reaction countdown is exposed; `AiFightStats` hit/block rates and reset are correct |
| `IT-01` | Health potions spawn inside the 3.5–5 m ring after the seeded delays, stop at two, heal 35 with a max clamp, and reset each round |

## Manual checklist

- [ ] Both fighters framed in every camera preset; arcs read left / right / overhead.
- [ ] Telegraphs visible in real time, not just slow motion.
- [ ] Hurtbox wireframes match the capsule; VFX appear at contact points.
- [ ] F1 blade capsule axis and rings overlap the rendered blade at every sampled clip time.
- [ ] Alt-tab mid-swing pauses and resumes; resize re-anchors the HUD without clipping.
- [ ] Timed block, guard break, and stagger are distinguishable in one glance.
- [ ] Every menu works by mouse and keyboard; Resume never swings.
- [ ] Main menu fits at 1600×900 and 1280×720; the focus ring follows Up/Down and text stays crisp.
- [ ] HUD health ghost lags behind a hit, round pips change with the score, and a denied attack blinks the stamina bar.
- [ ] Pausing mid-swing freezes the blade and resumes exactly; volume/shake/debug/difficulty persist between the menu and pause options.
- [ ] Results show score, round history, hits/damage/blocks/breaks; Rematch resets and Menu returns to the main menu.
- [ ] Pause freezes the blade mid-air; results numbers match debug counters.

## Performance

| Metric | Target |
| --- | --- |
| Simulation | ≤ 2 ms per tick |
| Draw | ≤ 6 ms per frame |
| Allocations | ≈ 0 bytes/frame in combat and draw |
| Frame rate | 60 FPS in a worst-case double power-attack exchange |

Measure with `dotnet-counters` and the overlay frame graph; report percentiles, not averages.

## Gate

```powershell
dotnet build SkyrimMelee.slnx -c Release   # 0 warnings
dotnet test SkyrimMelee.slnx               # unit tests
dotnet run --project SkyrimMelee -- --self-test
```

Visual and feel checks cannot be automated. They must be reported as "manual" or "not verified on this machine" — never claimed without running.
