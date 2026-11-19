# UI and AI

## UI

Myra renders at the window's **native resolution**. The HUD anchors to the edges and centre, so it adapts to any window size without a virtual canvas or scaling layer. Minimum window 1280 × 720. World renders first, Myra last.

- Myra embeds Inter-Regular.ttf and exposes it through `DefaultAssets.DebugFontSystem`; any size comes from `GetFont(size)`. No content pipeline, no font files, no image assets.
- `MyraHost` is a thin owner of `MyraEnvironment` + `Desktop`. The desktop uses the window bounds directly, so mouse input needs no remapping.
- `UiSkin` builds a flat style from colours and runtime 1×1 textures; Myra defaults that need atlases are overridden.
- Palette: ink `#12141A`, panel `#1B1E26`, text `#E6DCC8`, accent `#8FA6C0`, health `#B03A34`, stamina `#6FA76F`, danger `#C4553F`.

**HUD markup.** `SkyrimMelee/Ui/Hud.xmmp` and `DebugOverlay.xmmp` are the source of truth. `myra-export <file>.xmmp` writes a manual partial (never overwritten) and a `.Generated.cs` designer file (always regenerated); both are committed. The HUD is two `VerticalStackPanel` blocks — render statistics (FPS, draw calls, primitives, current clip, speed, progress) and controls — with one `Label` per row. The F1 debug overlay is another exported `VerticalStackPanel` with one `Label` per row; `F2` toggles the flat wireframe. Do not construct HUD widgets in C#; edit the markup and re-export. Panel backgrounds use MML's `#RRGGBBAA` color format (e.g. `#00000066`). Controls that are not implemented yet are amber, with a muted legend line.

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

The current dev HUD already has a separate **VITALS** block at the bottom-left (24 px inset): a label above each `HorizontalProgressBar` (`Health 150/150`, `Stamina 88/100`). The bars use the palette's health red and stamina green; a denied power attack appends `POWER DENIED` to the stamina label.

**Input ownership:** blocking screens give Myra all input; the HUD never captures. Closing a screen sets a release latch so the click that resumed the game cannot swing. Losing focus auto-pauses, clears the intent buffer, and latches. `Esc` belongs to the UI.

**Accessibility:** no damage numbers by default; guard break and timed block have non-colour cues; shake toggle; no flashing above 3 Hz.

## AI

The AI plays by the same rules: it emits `CombatIntent`s and cannot shorten a wind-up, skip recovery, or see the player's buffered input. It perceives a state change after a reaction delay (jittered ±15 % with a seeded RNG), which is why a delayed read is a late block.

| Situation | Response |
| --- | --- |
| Opponent winding up, time-to-active > reaction + 100 ms | Guard (timed-block chance per profile) |
| Opponent winding up, too close to react | Guard anyway; trade if the opponent is low |
| Opponent in recovery (≥ 300 ms left) | Punish with light, or power if affordable |
| Opponent guarding, stamina below a power block cost | Power slash for the guard break |
| Opponent guarding, healthy stamina | Chip with lights to drain block stamina |
| Opponent staggered | Power slash if it lands in time, else light |
| Own stamina < 25 or poise < 40 | Stop attacking, recover, block only when needed |

| Parameter | Novice | Adept | Master |
| --- | --- | --- | --- |
| Reaction | 450 ms | 300 ms | 180 ms |
| Decision interval | 150 ms | 100 ms | 75 ms |
| Block chance | 0.40 | 0.70 | 0.90 |
| Timed-block chance | 0.05 | 0.25 | 0.50 |
| Aggression | 0.30 | 0.60 | 0.85 |
| Mistake chance | 0.25 | 0.10 | 0.03 |

Profiles are data; adding a difficulty needs no new decision code. A hidden practice profile never attacks and guards on a fixed cycle.

The AI tracks decisions, hit/block rates, guard breaks, and staggers for the results screen, the overlay (`"punish recovery (410 ms left)"`), and the balance pass. Seeded per duel, drawn only on decision ticks, reset each round.
