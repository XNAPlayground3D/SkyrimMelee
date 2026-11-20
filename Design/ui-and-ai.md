# UI and AI

## UI

Myra renders at the window's **native resolution**. The HUD anchors to the edges and centre, so it adapts to any window size without a virtual canvas or scaling layer. Minimum window 1280 × 720. World renders first, Myra last.

- Myra embeds Inter-Regular.ttf and exposes it through `DefaultAssets.DebugFontSystem`; any size comes from `GetFont(size)`. No content pipeline, no font files, no image assets.
- `MyraHost` is a thin owner of `MyraEnvironment` + `Desktop`. The desktop uses the window bounds directly, so mouse input needs no remapping.
- `UiSkin` builds a flat style from colours and runtime 1×1 textures; Myra defaults that need atlases are overridden.
- Palette: ink `#12141A`, panel `#1B1E26`, text `#E6DCC8`, accent `#8FA6C0`, health `#B03A34`, stamina `#6FA76F`, danger `#C4553F`.

**HUD markup.** `SkyrimMelee/Ui/Hud.xmmp` and `DebugOverlay.xmmp` are the source of truth. `myra-export <file>.xmmp` writes a manual partial (never overwritten) and a `.Generated.cs` designer file (always regenerated); both are committed. The fight HUD is the player name, a 420 × 18 health bar with a pale delayed **damage ghost** behind a transparent-track red bar, and a 340 × 10 stamina bar at the top-left; the opponent is mirrored at the top-right. Round pips and `ROUND N` sit at centre-top, with a banner below (`ROUND N` 1.4 s, `K.O.` 1.2 s, `VICTORY`/`DEFEAT` 2.6 s). The ghost holds 0.35 s, then drains at 90 HP/s while the red bar catches up at 420 HP/s; insufficient stamina blinks the bar at 3 Hz. `HudPanel.UpdateFight` runs every frame and allocates no strings; the round/pip text changes only when the score changes. The render-statistics and controls blocks are dev-only and appear with the F1 overlay. The F1 debug overlay is another exported `VerticalStackPanel` with one `Label` per row; `F2` toggles the flat wireframe. Do not construct HUD widgets in C#; edit the markup and re-export. Panel backgrounds use MML's `#RRGGBBAA` color format (e.g. `#00000066`). Controls that are not implemented yet are amber, with a muted legend line.

**Menu skin.** `UiSkin` builds four Inter sizes (46 / 30 / 21 / 15 px) from Myra's embedded font and one flat button style: ink panels, steel borders, parchment text, and an amber focus ring. `MainMenu.xmmp` holds the title, Duel / Practice / Options / Quit, the difficulty row, and the options sub-panel; `MainMenu.cs` owns the events, focus order, and label updates. Mouse clicks go through Myra; the game routes Up / Down / Enter / Space / Escape and Left / Right for difficulty. Myra's key handler is cleared while the menu is open so a key press cannot activate a button twice. Every screen change uses a 180 ms black fade drawn above the UI. `--play` skips the menu for gameplay captures and automation.

```text
Boot → MainMenu → Duel → Pause → Options
                   ↓         ↓
                Results ←────┘
       Practice (no rounds, no death)
```

| Screen | Contents |
| --- | --- |
| Main menu | Title, Duel, Practice, Options, Quit, difficulty |
| HUD | Health (with delayed damage ghost), stamina, names, round pips, centre banner |
| Pause | Resume, Options, Quit to menu; simulation fully frozen |
| Results | Outcome, score, hits, damage, blocks, timed blocks, guard breaks, Rematch |
| Options | Volume, screen shake, debug default, difficulty; session-only |

HUD layout: player bars anchored top-left (health at most 420 × 18, stamina at most 340 × 10, both in a 24 px inset), opponent mirrored top-right, round pips centre-top, banner centred. Bars animate; text is cached; zero allocations per frame. On narrow windows bars shrink with their anchor; they never overlap the centre.

The current dev HUD already has a separate **VITALS** block at the bottom-left (24 px inset): a `ROUND 1  0 - 0` line, then a label above each `HorizontalProgressBar` (`Health 150/150`, `Stamina 88/100`). The bars use the palette's health red and stamina green; a denied power attack appends `POWER DENIED` to the stamina label.

**Input ownership:** blocking screens give Myra all input; the HUD never captures. `MyraHost.CapturesInput` is true while a modal or keyboard-focused widget is open, and gameplay intent collection is skipped and the buffer cleared. Closing a screen sets a release latch so the click that resumed the game cannot swing. Losing focus auto-pauses, clears the intent buffer, and latches. `Esc` belongs to the UI.

**Accessibility:** no damage numbers by default; guard break and timed block have non-colour cues; shake toggle; no flashing above 3 Hz.

## AI

The AI plays by the same rules: it emits `CombatIntent`s and cannot shorten a wind-up, skip recovery, or see the player's buffered input. It perceives a state change after a reaction delay (jittered ±15 % with a seeded RNG), which is why a delayed read is a late block. It never presses a button the player could not: guard, attack, power attack, bash, and spacing all pass through the same state machine, stamina, `GuardResolver`, and `DefenseResolver`.

| Situation | Response |
| --- | --- |
| Player winding up | Start the guard after `reaction − GuardRaiseDuration`; a timed-block roll raises 40 ms later; roll `BlockChance` when the delay expires |
| Player in recovery, ≤ 2.2 m, no cooldown | Aggression roll → punish with a light; power only on recovery or player stamina < 20 |
| Player guarding, ≤ 2.2 m | `Aggression × 0.6` roll → `PowerSlash` with `0.5 + PowerChance` if stamina ≥ 30, otherwise a light |
| Player idle, ≤ 2.2 m | `Aggression × 0.5` roll → pressure with a light |
| Player winding up + mistake roll | Greedy swing into the committed attack instead of guarding |
| Nothing happening, self idle | Turtle roll → pre-guard; spacing closes past 2.3 m, backs off inside 1.35 m, braces while the player is committed |
| Self not idle | Keep the guard; no new intent until idle |

| Parameter | Novice | Adept | Master |
| --- | --- | --- | --- |
| Reaction | 220 ms | 100 ms | 60 ms |
| Block chance | 0.40 | 0.75 | 0.92 |
| Timed-block chance | 0.10 | 0.25 | 0.50 |
| Pre-guard chance | 0.10 | 0.45 | 0.70 |
| Aggression | 0.40 | 0.55 | 0.30 |
| Power chance | 0.10 | 0.25 | 0.45 |
| Mistake chance | 0.35 | 0.10 | 0.02 |

Profiles are data (`AiProfile`); adding a difficulty needs no new decision code. The menu starts on **Novice** (`AiProfile.Default`); `--profile=<name>` overrides it at launch.

**Telemetry.** `DuelistAi` publishes `LastDecision` (`action`, `reason`) plus a monotonically increasing `DecisionId`, the pending `ReactionRemaining`, `GuardRemaining`, and the current `Intent`/`Approach`. The game logs each new decision once with the simulation time and the FSM state it produced (`SlashRight (idle pressure) -> Windup SlashRight`). The F1 overlay shows the profile, intent, reaction countdown, guard timer, approach, the last decision and reason, per-round `AiFightStats` (attacks, hit rate, block rate, guard breaks), and the last two decision/state lines.
