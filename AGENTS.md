# AGENTS.md — how work happens in this repo

This file is the contract for any agent (human or AI) contributing to SkyrimMelee. It defines the loop, the guardrails, and the definition of done. If something here conflicts with an explicit instruction from the developer, the developer wins — but say so out loud before deviating.

---

## Non-negotiable guardrails

Read [README.md](README.md) → **Hard rules** first. In short:

1. **Only the approved asset set may be imported.** The shipping build still uses primitives; once T9.8 lands, only `Content/Models/fighter.glb` and `sword.glb` are imported. Arena, VFX, and UI remain generated.
2. **Skeletal animation is fighter-only.** Clips have no root motion and no IK; the sword is a static attachment on `weapon_r`. Combat timing stays authoritative in `CombatTuning` and deterministic at the fixed 60 Hz step.
3. **Combat numbers live in `CombatTuning.cs`**, never inline in logic.
4. **Combat must stay deterministic and headless-testable.**
5. **`dotnet build SkyrimMelee.slnx` must be green** before you report anything as finished.
6. **README stays brief.** It is a one-screen snapshot. Detailed behavior, numbers, and diagrams belong in [`Design/`](Design/overview.md), never in README.

Don't add new NuGet dependencies, replace the renderer, or change the architecture without asking first. Don't "fix" the constraints because they made something harder — that's the project.

---

## The workflow (mandatory, one task at a time)

### 1. Get the next task from TASKS.md

- Open [TASKS.md](TASKS.md) and take the **top-most unchecked task** (`[ ]`).
- If the developer named a specific task, do that one instead.
- Read its **Goal** and **Done when**, plus any task it depends on, and the `Design/` docs it links.
- Announce the task ID and title before starting.
- If the task is too large to finish and verify in one pass, **split it in TASKS.md first**, then do the first half. Editing TASKS.md is expected and encouraged; the roadmap is not etched in stone.

### 2. Do it

- Implement the task to its stated **Done when** — not approximately, not "close enough".
- Follow the conventions and folder layout in [Design/architecture.md](Design/architecture.md) and [Design/combat.md](Design/combat.md). New files go where the layout says; if no place fits, propose a layout change in TASKS.md.
- Keep the diff focused on this task. No drive-by refactors, no unrelated formatting churn, no speculative abstractions.
- If you discover a needed change outside the task's scope: write it into TASKS.md (new task or Backlog), then continue.
- If you get blocked (missing API, broken toolchain, contradictory requirements): stop, state the blocker, and ask. Do not silently work around a blocker in a way that undermines a guardrail.

### 3. Test thoroughly

Every task, minimum bar:

```powershell
dotnet build SkyrimMelee.slnx          # zero warnings introduced
dotnet test SkyrimMelee.slnx           # unit tests
dotnet run --project SkyrimMelee/SkyrimMelee.csproj -- --self-test   # combat tasks (stub until T3.5)
```

Then, depending on what the task touched:

| Task touches | Required verification |
| --- | --- |
| Rendering / visuals | Run the game, look at it. Check the new visuals from every camera preset. Check resize and alt-tab. |
| Combat logic | Run `--self-test`; add or extend scenarios so the new behavior is asserted. Run it twice — results must match. |
| Pure logic / math | Add or extend xUnit tests in `SkyrimMelee.Tests`; `dotnet test` must pass. |
| Physics | Verify with `F2` wireframe that physics shapes match rendered geometry. Test at 60 FPS and with a simulated low frame rate. |
| UI | Exercise every new screen with mouse **and** keyboard. Check that clicks don't leak into gameplay. |
| Input | Test the rejection paths (insufficient stamina, mid-recovery, UI open), not just the happy path. |
| Timings / tuning | Verify slow motion (`--slowmo`) and that constants match `CombatTuning.cs`. |

Rules for test claims:

- Never claim something works that you did not run.
- If a test cannot run in this environment (GPU, audio), say exactly that in the confirmation request.
- Update or add self-test scenarios **in the same task** that introduces the behavior. A behavior without a scenario is not tested.
- If a test fails, fix it — don't report the task as finished with "known failing test" unless the developer explicitly accepts it.

### 4. Update the docs with the new project state

After the task is implemented and tested:

**README.md** — rewrite the **Project state** section so it is true right now, and keep the whole file under ~100 lines:

- `Last updated` date (use the real current date).
- `Phase`, `Code`, and `Next task` (the next unchecked task ID).
- The status table: mark the relevant area ✅/🚧/⬜.
- Update the slim summary only if the task changed it (e.g. a new CLI flag or a control).

**Design/** — update the documents the task actually changed:

- New or changed behavior → the matching `Design/*.md` section, with tables and numbers matching `CombatTuning.cs`.
- A retired risk → mark it in [Design/overview.md](Design/overview.md#risks).
- A new architectural decision → a new row in [the decision table](Design/overview.md#decisions).

Write both as documentation of the **current, working** project — never as a plan or a wish list.

### 5. Ask the developer whether the task is finished

Present a short, verifiable report and then ask explicitly. Use this shape:

```
Task: T3.2 — Swept blade cast
Status: implemented, tests green

What changed
- Physics/BladeSweep.cs: per-substep swept capsule queries
- Combat/Tuning: sweep distance cap 0.35 m
- TASKS: no changes (within scope)

Verified
- dotnet build: 0 warnings, 0 errors
- --self-test: 9/9 scenarios pass (twice, identical output)
- Manual: max-speed power swing registers exactly one first contact at 60 FPS and at simulated 10 FPS

Not verified
- (nothing, or: audio path not exerciseable on this machine)

Is T3.2 finished? Reply "yes" to confirm, or tell me what to change.
```

**Do not act as if the task is done before an explicit confirmation.** Silence, "looks fine I guess", or a question about something else is not confirmation — ask again if it's ambiguous. "Yes", "done", "approved", "commit it" are confirmation.

If the developer asks for changes: go back to step 2, re-test, update the docs again, and ask again. Never mark the task complete in the meantime.

### 6. Only after explicit confirmation: update TASKS.md, commit, push

Once the developer confirms:

1. Change the task from `[ ]` to `[x]` in TASKS.md (or `[~]` → `[x]`).
2. Add a row to **Done log**: task ID, date, commit hash (fill after committing), "confirmed by: <developer>".
3. Move the **Next up** marker to the next unchecked task.
4. **Review and curate TASKS.md freely.** Now that the task is confirmed, add, remove, reword, split, merge, or reorder tasks as reality demands — including cutting scope. Keep the **Invariants** intact, keep dependencies coherent, and leave `Next up` on the top-most unchecked task. Mention board changes in the commit body. This is expected work, not something to ask permission for.
5. If the task changed the plan, adjust dependent tasks and note it in the commit body.
6. Commit, then push:

```powershell
git add -A
git commit -m "T3.2: swept blade cast" -m "Per-substep swept capsule queries with a per-swing hit set.
Self-test scenarios: clean hit, whiff, tunneling stress.
README project state updated; T3.2 marked done."
git push origin HEAD
```

Commit rules:

- **One task per commit.** Don't squash two tasks together, don't commit half a task.
- Subject: `T<id>: <imperative summary>` ≤ 72 chars. Body: what/why/tests.
- Include the TASKS.md, README.md, and affected `Design/` changes in the same commit as the code.
- Never commit `bin/`, `obj/`, `.vs/`, local saves, logs, or generated content caches.
- Never force-push. If push is rejected, fetch/rebase (never merge-commit) and push again.
- If the repo has no configured remote or push fails for permissions: stop and ask. Do not create a remote.

After pushing, report the commit hash and the next task.

---

## Definition of Done (per task)

A task is done only when **all** of these are true:

- [ ] The behavior in **Done when** exists and was demonstrated.
- [ ] `dotnet build SkyrimMelee.slnx` is green with no new warnings.
- [ ] `dotnet test SkyrimMelee.slnx` passes, with new xUnit coverage for pure logic.
- [ ] `--self-test` passes, and the task's behavior has a scenario (if it is combat logic).
- [ ] Visual/UI changes were actually looked at, not just compiled.
- [ ] No guardrail was violated: no imported models outside the approved M9 set, no skeletal animation outside the fighter, no magic numbers in logic.
- [ ] README **Project state** reflects reality, including the new next task, and README is still brief.
- [ ] The affected `Design/` docs describe the new behavior and match `CombatTuning.cs`.
- [ ] The developer explicitly confirmed the task.
- [ ] TASKS.md is updated (checkbox, Done log, next-up).
- [ ] Exactly one commit exists for the task, and it has been pushed.

---

## Code conventions

- C# latest, nullable enabled, file-scoped namespaces, one public type per file.
- **Tabs for indentation in C#** (`.editorconfig` enforces it); project/XML files keep the SDK's two-space style.
- **HUD and debug layouts live in `.xmmp`** and are exported with `myra-export`; never build HUD widgets in C#. Re-export after markup changes and commit both `*.cs` and `*.Generated.cs`.
- Namespace root `SkyrimMelee`; folder structure mirrors namespaces.
- Gameplay data as `record`/`readonly struct`; avoid mutable static state.
- `CombatTuning.cs` is the single source of truth for every timing, cost, multiplier, and threshold. If you need a new number, add it there with a comment.
- Fixed 60 Hz simulation. Never let rendering frame rate affect combat outcomes.
- No `Random` in combat resolution. AI may use a seeded RNG, stored per duel so runs are reproducible.
- No per-frame allocations in combat/rendering hot paths. Pool particles and UI strings where it matters.
- Comments explain *why*, not *what*. Public types get one-line XML docs only when the name isn't obvious.
- Delete dead code instead of commenting it out.

---

## Communication

- Be concise and concrete. Report task ID, what changed, what was tested, what wasn't.
- When you disagree with the plan, say so with a reason and a proposal, then follow the developer's call.
- When you change TASKS.md, mention it — the roadmap is shared state.
- Never inflate progress. "Compiles" is not "works"; "works" is not "tested".
- Ask one clear question at a time; batch only tightly related questions.
