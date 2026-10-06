# CLAUDE.md

This file provides guidance to any Claude agent working in this repository — Claude Code in the terminal, Cowork, and any subagent either of them spawns.

**Read this file in full before your first action in a session.** It is loaded automatically as project instructions; treat it as read, not as something to go fetch. Its rules take precedence over your defaults. If a rule here conflicts with a habit you'd otherwise follow, this file wins. After a context compaction, re-read it before continuing.

## Session start

At the very start of a new session — before exploring code, running commands, or making assumptions about what to do — ask the user what they'd like to work on. Keep the prompt to a single line (e.g. "What would you like to work on?"). Skip this only if the user's first message already contains a concrete task. Always read through the obsidian folder at `/Users/noah/Void_Sea/Infinite_Extraction`.

## Project Overview

Infinite Extraction ("IE") is a 2D game built as a framework-free C# core with a Godot 4 view layer. All gameplay logic lives in plain .NET libraries that know nothing about the engine; Godot supplies rendering, input, and timing.

**Start here**: `ARCHITECTURE.md` has the current layered view, key contracts, invariants, and call flows. Read it before making non-trivial changes.

## Tech Stack

- **Engine**: Godot 4.7.1 stable, **.NET/Mono build** (the plain build cannot run C#). Pinned in `scripts/setup-toolchain.sh` as `GODOT_VERSION`.
- **Language/runtime**: C# on .NET 8. `TargetFramework` is declared once, in `Directory.Build.props`, and inherited by every project including the Godot app. net8.0 is the floor Godot.NET.Sdk requires — bump it there and nowhere else.
- **Tests**: xUnit, run by `dotnet test`. No engine on the test graph.
- **Toolchain location**: `.toolchain/` at the repo root (gitignored) holds Godot; the .NET SDK goes to `$HOME/.dotnet` if one is not already on `PATH`. Both are installed per-user, no sudo.
- **Env vars**: `GODOT_BIN` and `DOTNET_ROOT` are exported by `.toolchain/env.sh`, generated during setup. `IE_LOG` and `IE_HEADLESS_SELFTEST` control runtime behavior (see Diagnostic modes). `IE_TOOLCHAIN_DIR` overrides where the toolchain is installed.

**First run in a fresh clone or a fresh machine:**

```bash
bash scripts/setup-toolchain.sh     # downloads Godot + .NET, generates both .sln files, restores
source .toolchain/env.sh            # puts godot + dotnet on PATH for this shell
```

The setup script is idempotent — re-run it any time; it skips whatever is already present.

**Sandboxed agents cannot run it.** The Cowork sandbox's egress proxy blocks NuGet, Microsoft, and the Godot download hosts, and has no sudo, so `dotnet` cannot be installed or restored there. Any session that needs to actually build, test, or launch this project must run on a machine with real network access — Claude Code in a terminal on the developer's Mac. A sandboxed session can still read and edit source, but must not claim a change is verified.

## Commands

All scripts source `scripts/_env.sh`, which resolves the toolchain and fails with instructions if setup has not been run.

```bash
scripts/build.sh                      # build everything (Debug); pass Release for a release build
scripts/test.sh                       # full xUnit suite
scripts/test.sh 'FullyQualifiedName~NoFloatingPoint'   # single test / class filter
scripts/run.sh                        # build, then launch the game windowed (logs to logs/run.log)
scripts/run.sh --editor               # open the Godot editor on app/
scripts/headless-selftest.sh          # scripted boot + interaction, self-exits (see below)
scripts/verify-guardrails.sh          # proves the architecture tests still bite
```

Raw equivalents, if a script is in the way:

```bash
dotnet build InfiniteExtraction.sln
dotnet test  InfiniteExtraction.sln
"$GODOT_BIN" --path app                       # run
"$GODOT_BIN" --headless --path app --import   # reimport resources only
```

**Two solution files, both intentional.** `InfiniteExtraction.sln` at the repo root is what `dotnet build`/`dotnet test` use; `app/IE.App.sln` is the one the Godot editor expects to find beside `project.godot`, and Godot regenerates it if deleted. Neither is committed as hand-written XML — `setup-toolchain.sh` generates both with the `dotnet` CLI. Godot's `dotnet/project/solution_directory` setting is known-buggy; do not try to collapse these into one with it.

**Test-first rule (strict TDD)**: For changes to pure-logic code, follow the full red-green loop:

1. **Write the test(s) first** — expressing the new intended behavior.
2. **Run them and see them fail** — and confirm the failure is for the expected reason (not a compile error, not a typo). A test that was never seen red proves nothing.
3. **Then change the implementation** to make them pass.
4. **Re-run the full suite** to confirm no regressions.

Show the diff and get buy-in on the plan when behavior changes have test fallout (e.g., existing assertions need to flip). Don't silently rewrite tests to match new behavior without flagging what's changing and why. Untestable layers (see the exclusions below) mean tests are the main safety net for logic changes; strict TDD keeps that net honest.

**Rebuild-before-launch rule**: The runtime does not always rebuild after a source edit. After editing source, run the build explicitly before relaunching or you'll be running stale code.

**Manual-test-after-every-change rule**: The user is not running the app themselves. After any change whose unit tests pass, rebuild and launch the app yourself, then wait for the user to confirm the feature works before moving on or pushing. Unit tests don't cover the view/UI layer, so passing tests aren't enough evidence that a visual/interaction change actually works. Every manual test run is followed by a **log-verification step** (see the Instrumentation rule) — launch with the relevant log categories enabled and read the captured output to confirm the code path actually executed and did what you expected. "The window opened and the user didn't complain" is not verification; the logs are.

- **An interactive (non-headless) launch is long-running — never treat its "completion" or a short log as exit/crash.** When you launch the app for the user to test, the process keeps running until the *user* closes it, which can be many minutes. Specific false positives to avoid:
  - **A background-task "completed" notification means the *launching shell command* returned, NOT that the app exited.** Do not conclude the user closed the window or that it crashed. Wait for the user's verbal report.
  - **A short stdout log means the log is still buffered, NOT that startup failed.** Output is block-buffered when redirected to a file, so lines flush in bursts and lag the user's actions. Re-read the file after the user reports doing something; don't infer "exited early" from line count.
  - **Shutdown noise is not a crash.** Allocator/teardown warnings that only appear when the process is *killed* (your `kill`/SIGTERM, or the user quitting) are normal teardown artifacts, never evidence of a code fault. To sanity-check a headless boot without them, use a path that self-exits rather than `sleep && kill`.
  - Bottom line: for an interactive launch, the only authoritative signals are (a) the user's report and (b) the flushed log lines once they've acted — not the task-completion notification, the launcher's exit code, or the line count.

**Instrumentation rule**: Instrumentation is a permanent, first-class part of this codebase, not scaffolding to rip out once a bug is fixed. It is the primary way the agent inspects the results of its own work in a layer it cannot otherwise observe — leave it in, and add more.

- **Every plan includes instrumentation.** When planning a change, decide up front which log calls (which category, which level) prove the new code path ran and behaved correctly, and include adding them as an explicit step — same standing as writing tests. A plan with no instrumentation step is incomplete.
- **Use the project's logging system**, not ad-hoc prints. It is per-category × level, quiet by default in local runs and verbose in shipped builds, so a user's bug report carries a real log. Warnings and errors are never compiled out. If a helper pre-computes summary data purely for logging, gate it with the same compile-time constant the logging uses — not a debug-only constant that would strip it from shipping builds. Never reach for raw console printing for diagnostics; route through the logger with the right category so it can be enabled/disabled per subsystem. Engine-level warning/error dialogs are for user-facing failures, not instrumentation.
- **Verify via the logs after manual testing.** The verification step of the manual-test rule is: relaunch with the relevant log categories enabled, then read the captured output and assert the expected lines are present (the path ran) *and* unrelated categories stayed silent (no leakage / correct gating). Treat this log check as a required deliverable of the change, reported alongside the test results.

**Architecture-doc-before-push rule**: When the user asks to push code changes to the remote, ask first whether they want `ARCHITECTURE.md` updated to reflect those changes — and wait for an answer before pushing. Triggers on any push whose diff (committed or about-to-commit) touches code. Skip the question if the push is documentation-only — there's no code drift to capture. Don't ask on plain commits, only when the push itself is requested.

**Docs describe what is, not what used-to-be**: `ARCHITECTURE.md` (and any reference doc) must read as a present-tense snapshot of the current design — never a changelog. When you update it, **replace** the stale description with the new reality; do not append the change as history. Concretely, do not write:

- Evolution narration — "used to", "previously", "no longer", "the old X was removed", "this replaced", "originally", "legacy", "is gone", "now does Y instead of X". If a sentence exists only to narrate a change, delete it; don't flip it to a negated present ("has no X", "does not skip Y") that still plants the ghost of the old design in a future reader's head.
- References to closed issues — `(issue #45)`, `(#72)`, `pre-#70`, "the #20 baseline", etc. The code and `git log` hold that provenance; an issue number in the doc is noise that misleads agents into chasing closed history. (Real code identifiers that happen to contain "Legacy" are fine — that's the current API.)
- War-story rationale — "we tried X but it broke, so now Y". Keep the *current* constraint ("Y, because Z"), drop the failed-attempt story.

Prefer lean over exhaustive: cut edge-case minutiae and redundant restatement, keep invariants, contracts, code identifiers, and call-flow steps. Drift toward shorter. Git history is the changelog; the doc is the map.

**Tech-debt-capture rule**: When you discover a problem in your own work — a flaky test, a known-broken edge case, a shortcut taken to unblock something, a TODO you'd otherwise drop into a comment — and you are not fixing it right now in this change, **propose an issue with the `tech-debt` label and wait for user confirmation before filing** (see the Issue-tracking rule below). Include enough context in your proposal that future-you could act on it: file/line, symptom, suspected cause, and candidate fixes. The bar is "did I notice it and choose to defer it" — if yes, surface it; the user decides whether it gets an issue. Don't silently move on, and don't auto-file.

**Issue-tracking rule**: Bugs and features are tracked as issues in the project's tracker. **Never create, update, label, close, reopen, comment on, or delete issues without explicit user confirmation** — propose the action (title, labels, body, or the specific change), then act only on the user's "yes." This applies to tech-debt capture too: surface, don't auto-file. Before planning or implementing any non-trivial fix or feature, prefer to have an associated issue — check the existing list first; if none exists, propose filing one (or ask the user to point at the right ticket) before changing code. The user can override on any given task ("just fix it", "no ticket needed") — when they do, proceed without one and don't keep asking for that task. Issue body format — bugs: **Context / Repro / Expected / Notes**; features: **Context / Proposed behavior / Acceptance / Notes**; goals-only design tickets (when the solution is still TBD): **Context / Goals / Notes**.

## Diagnostic / headless modes

**`IE_HEADLESS_SELFTEST=1`** — `MainScene` boots normally against the *real* views and the real `EnginePacer`, asserts the boot path reached the view, logs pass/fail, and calls `GetTree().Quit()` with the matching exit code. Extend the scripted interaction as gameplay lands. It self-exits by design: `sleep && kill` produces allocator/teardown warnings that look like a crash but are not.

**`IE_LOG=<spec>`** — sets per-category log levels at boot. Spec is a comma-separated list of `Category:Level`, e.g. `Controller:debug,View:trace`, or `all:trace`. Categories: `Boot`, `Model`, `Controller`, `View`, `Input`, `Undo`, `Save`. Levels: `off`, `error`, `warn`, `info`, `debug`, `trace`. Everything defaults to `warn`, so a plain run is quiet. A typo is ignored rather than thrown.

```bash
scripts/headless-selftest.sh                                  # fast: boot + one interaction, exits itself
IE_LOG=Boot:debug,Controller:debug scripts/headless-selftest.sh   # full: with instrumentation
IE_LOG=Undo:trace scripts/run.sh                              # windowed, one subsystem turned up
scripts/verify-guardrails.sh                                  # red/green proof of the no-floats test
```

`scripts/verify-guardrails.sh` plants a `float` field in `IE.Model`, asserts the suite goes **red**, removes it, and asserts it goes **green**. Run it after touching the architecture tests — a guardrail nobody has watched fail is not a guardrail.

Runs are deterministic today because the core is integer-only and nothing is seeded from a clock. Any randomness added later must come from an explicitly seeded generator held in `IE.Model`, so two runs with the same seed stay byte-identical.

Use these for automated debugging — do **not** use them as a substitute for the manual-test rule above when the change affects anything a human would see.

## Code Style

Enforced by `.editorconfig` with `EnforceCodeStyleInBuild` on. The first three are warnings (and therefore build errors in the libraries); the rest are suggestions:

- Types, methods, properties, events, namespaces, local functions: `PascalCase`
- Interfaces: `IPascalCase`
- Private and private-protected fields: `_camelCase`
- Locals and parameters: `camelCase` — **suggestion only**, because positional record parameters are PascalCase by design and would otherwise fail every build that declares a record
- Prefer explicit types over `var` — suggestion only, same reasoning
- Constants: `PascalCase` (not `SCREAMING_CASE`)
- File-scoped namespaces; one public type per file, named after the file. Godot script classes **must** match their filename or the engine will not bind the script.
- The three libraries build with `TreatWarningsAsErrors`. `IE.App` does not — Godot's source generators emit warnings outside our control.

- Nullability/strictness settings are enabled — annotate intent explicitly
- Boundaries between layers are expressed with plain language primitives, not framework-specific types, so the core stays framework-free and unit-testable with mocks
- Type everything; avoid inferred types when the type isn't obvious from the right-hand side
- Prefer DRY: if you find yourself writing the same logic twice, extract a helper. The codebase has plenty of small reusable helpers already — reuse before re-deriving. Watch for it especially when adding new tests, scoring terms, or rule predicates — those are where copy-paste creeps in fastest.

## Architecture rules (do not violate)

- **Views never mutate the model.** The view layer only reads state and renders; it doesn't write to it.
- **The controller never touches framework/engine node types directly.** It talks to views through interfaces, and to the event loop through an injected pacer/scheduler abstraction. This is what makes controller unit tests possible.
- **Every state change funnels through a single refresh call** at the end of the handler — one UI update path, no drift.
- **Snapshots capture core state plus the user-intent slice of session state**; transient fields and the undo stack itself stay out. Top-level user event handlers are wrapped in a tracking helper that captures pre-state, runs the body, and pushes a single undo entry iff state actually changed — de-duping no-op interactions. Exceptions inside a handler propagate; no push happens (a half-mutated state means the invariants are broken — crash, don't paper over).
- Pure rules classes are static and framework-free. Keep them that way.
- **No circular dependencies.** Across projects the layering is one-way and compiler-enforced: `Model` ← `Controller` ← (app + tests); `ViewMath` ← `Model` (one-way), referenced by the app and tests. Model must never gain a reference back to Controller or ViewMath; neither library may reference the engine/framework SDK. The same one-way discipline applies *within* a project: no class A → B → A cycles between rules classes, between helpers, between view-side scripts, etc. When two pieces want to call each other, the fix is one of: extract the shared logic into a lower-level helper that both call, invert the dependency with an interface or delegate the upper layer injects down, or merge them if they were never really separable. A new cycle is a design smell — stop and resolve it before continuing.
- **No floating-point in `Model` or `Controller`.** Floats and doubles are not deterministic across platforms, compilers, and JIT levels, so any floating-point on the core state code path is a desync time bomb for networked multiplayer. All math in those two assemblies must be integer (or fixed-point if fractional precision is genuinely needed). View-math that needs floats (DPI scaling, layout, pixel geometry) lives in `ViewMath` — a separate framework-free library that the app and tests reference but that Model + Controller do not. The rule is enforced by a reflection test that fails the suite if any field, property, parameter, return type, or method-body local is float/double. If you legitimately need a fractional value in Model or Controller, the answer is fixed-point integers (percent, basis points, scaled-by-1000), not a new exception to the rule.

## Project Structure

- `src/IE.Model/` — the **framework-free core library**. No engine SDK reference, no reference to the controller layer. Holds the pure domain model, static rule classes, serialization, and primitives. It is *physically incapable* of depending on the framework — an engine import will not compile. New testable domain logic goes HERE.
- `src/IE.Controller/` — the **framework-free orchestration library**, referencing **only** the model (one-way). Holds input handling, mutation/orchestration helpers shared between live and replayed paths, session state, and the view-boundary interfaces (`Views/IMainView.cs`, `Pacing/IPacer.cs`). New testable orchestration logic goes HERE.
- `src/IE.ViewMath/` — the **framework-free view-math library** (one-way reference to the model for primitives). The float-allowed pressure-relief valve for the no-floats rule. Anything view-side that needs sub-pixel precision and is also worth unit-testing belongs here.
- `app/` — the Godot project (`project.godot` lives here, so `res://` is this directory) and framework-side code only: `scripts/` scene roots and view implementations, `platform/` adapters (`GodotLogSink`, `EnginePacer`), `scenes/` .tscn files. Not unit-testable (needs engine types) — test the model logic they call instead.
- `tests/IE.Tests/` — the test project. It references all three core libraries, has **no engine SDK reference and no explicit file list** — a new source file in any library is seen automatically. `Fakes/` holds `FakeMainView`, `SynchronousPacer`, and `RecordingLogSink`; use them for controller flows. `Architecture/` holds the guardrails that fail the suite on a float in the core or a new dependency edge. That the suite compiles and passes with zero framework on its graph is the compile-time purity proof.
- `scripts/` — toolchain setup and every build/test/run entry point. Prefer adding a script here over documenting a long command line.
- `.toolchain/`, `bin/`, `obj/`, `app/.godot/`, `logs/` — generated. All gitignored; never commit or hand-edit them.

## Notes

- **Pinned entry points.** `app/project.godot` names `res://scenes/Main.tscn` as the main scene and `IE.App` as the assembly. `Main.tscn` binds `res://scripts/MainScene.cs` by path. Renaming any of the three, or the `MainScene` class, silently breaks the boot path — the window opens empty rather than erroring. The scene also depends on the node name `UI/Status`; `GetNode` throws at runtime if it moves.
- **Godot rewrites files on open.** The editor reformats `project.godot` and reorders `.tscn`/`.tres` keys, and generates `.import` sidecars for every asset. Commit what it produces; do not fight the formatting. `.editorconfig` already exempts these from trimming and final-newline rules.
- **`.import` sidecars are sources.** Every file under `app/` that Godot imports gets a `.import` file next to it. It must be committed alongside the asset or the project fails to load on another machine.
- **The Godot app project must stay in `app/`.** `Godot.NET.Sdk` globs `**/*.cs` beneath the `.csproj`. Moving `IE.App.csproj` to the repo root would swallow `src/` and `tests/` into the app assembly and produce duplicate-symbol errors; the fix would be `<Compile Remove>` exclusions that quietly rot. Keeping the Godot project in its own directory avoids the problem entirely.
- **Two `.sln` files are correct** — see Commands. Deleting `app/IE.App.sln` makes Godot regenerate one; deleting the root one breaks every script.
- **No `--` inside an XML comment.** `.csproj` and `.props` comments cannot contain a double hyphen or end in one — MSBuild fails the whole restore with `MSB4025` before it does anything else. Use a single hyphen or a colon when writing prose in a build file.
- **`Directory.Build.props` is the only place `TargetFramework` appears.** Adding one to an individual `.csproj` will drift from the app and produce confusing restore errors.
- **A core-state clone must copy every new field.** Cloning backs the undo stack; a field added to the state but not to its clone produces undo bugs that no test currently catches. Add the field to the snapshot's equality check too if it is player-visible intent.
- **A sandboxed session cannot verify anything here.** No .NET, no NuGet, no engine. Edit source freely, but say plainly that the change is unverified rather than implying it built.
