# Testing

## Pyramid

| Layer | What | Gate |
| --- | --- | --- |
| Pure functions | Damage, poise, clip sampling, tuning validation | Every task touching them |
| Simulation | Full combat rules headless, scripted intents, fixed seeds | Every combat/physics task |
| Visual | Rendering, camera, UI, VFX, feel | Every rendering/UI task |
| Balance | AI vs AI and self-play statistics | T6.2, T8.5 |

A behaviour without a scenario is not tested. New combat behaviour ships with its scenario in the same task.

## Determinism

Given identical initial state and intents, tick _n_ must be byte-identical: same health, stamina, poise, states, clip times, blade endpoints, contacts. Run every scenario twice and compare a rolling state hash; print the first diverging tick on failure. No RNG (except seeded AI), no wall clock, no threads, no dictionary-order decisions.

## Harness

`--self-test` runs without a window using the real state machines, tuning, clips, and physics.

```csharp
record Scenario(string Id, string Description, FighterSetup Player, FighterSetup Opponent,
    IntentTrack PlayerIntents, IntentTrack OpponentIntents, float Duration,
    Action<SimSnapshot>[] Assertions);
```

Intent tracks use absolute sim times. Output is one `PASS/FAIL` line per scenario and a non-zero exit code on failure. `--filter ST-04 --verbose` dumps per-tick state.

## Scenarios

| ID | Checks |
| --- | --- |
| `DM-01/02` | Damage table and rounding across armor, block skill, timed/guard-break |
| `CL-01/02` | Clip sampling endpoints, loop continuity |
| `CL-03/04` | Telegraph hold ≥ 100 ms, active window contains the centreline crossing |
| `CL-05` | Blade never intersects the fighter's own body (120 Hz sampling) |
| `ST-01/02` | Clean hit lands; attack with no hurtboxes whiffs and recovers |
| `ST-03/04` | Normal block reduction and drain; timed block at 180 ms after raise |
| `ST-05` | Power overhead into 5 stamina ⇒ guard break, guard removed, 1000 ms stagger |
| `ST-06/07` | Off-axis hit is not blocked; hitting a wind-up interrupts the attack |
| `ST-08/09` | One hit per hurtbox per swing; poses 1.2 m apart in one tick still connect |
| `ST-10/11` | Double KO draws; equal-TOI guard beats body |
| `ST-12/13` | Reset reproduces state; power attack at 10 stamina is rejected with no cost |
| `ST-14/15` | Poise empties on the fourth fast hit; 120 ms buffer fires on the first legal tick |
| `ST-16/17` | Guard released during raise does not block; `--god` clamps at 1 health |
| `AI-01` | Master beats Novice in ≥ 8/10 fixed-seed duels; every pairing shows blocks, timed blocks, guard breaks |

## Manual checklist

- [ ] Both fighters framed in every camera preset; arcs read left / right / overhead.
- [ ] Telegraphs visible in real time, not just slow motion.
- [ ] Hurtbox wireframes match the statue; VFX appear at contact points.
- [ ] Alt-tab mid-swing pauses and resumes; resize keeps HUD and aspect.
- [ ] Timed block, guard break, and stagger are distinguishable in one glance.
- [ ] Every menu works by mouse and keyboard; Resume never swings.
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
dotnet run --project SkyrimMelee -- --self-test
```

Visual and feel checks cannot be automated. They must be reported as "manual" or "not verified on this machine" — never claimed without running.
