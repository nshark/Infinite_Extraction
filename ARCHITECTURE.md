# ARCHITECTURE

Infinite Extraction is a 2D game: a C# core with a Godot 4 view layer. The core holds
all gameplay logic and is framework-free; Godot supplies rendering, input, and timing
and nothing else.

## Layers

```
IE.Model  <--  IE.Controller  <--  IE.App (Godot)
   ^                 ^                 ^
   |                 |                 |
   +--  IE.ViewMath  +-----------------+
                     |
                  IE.Tests
```

Every arrow is one-way and compiler-enforced. `IE.Model` sits at the bottom and
references nothing. `IE.Controller` and `IE.ViewMath` reference `IE.Model` only.
`IE.App` is the sole project that references the engine. `IE.Tests` references the
three libraries and has no engine on its graph — that the suite compiles at all is
the purity proof.

| Project | Path | May reference | Holds |
|---|---|---|---|
| `IE.Model` | `src/IE.Model/` | nothing | domain state, static rules, primitives, logging system |
| `IE.Controller` | `src/IE.Controller/` | `IE.Model` | input handling, mutation orchestration, undo, view/pacer interfaces |
| `IE.ViewMath` | `src/IE.ViewMath/` | `IE.Model` | grid↔pixel math (the only core assembly where floats are legal) |
| `IE.App` | `app/` | all three + Godot | scene roots, view implementations, platform adapters |
| `IE.Tests` | `tests/IE.Tests/` | all three | xUnit suite, fakes, architecture guardrails |

## Invariants

- **No floating-point in `IE.Model` or `IE.Controller`.** Floats are not bit-identical
  across platforms, so they are a desync risk on the state path. Fractional values are
  fixed-point integers. `NoFloatingPointTests` fails the suite on any float or double
  in a field, property, parameter, return type, or method-body local of those two
  assemblies. `IE.ViewMath` is the escape valve.
- **Views never mutate the model.** `IMainView.Render` receives `MainViewModel`, a
  record struct of primitives. The view has no handle on live state to write through.
- **The controller never touches an engine type.** It sees `IMainView` and `IPacer`.
- **One refresh per state change.** Every handler ends in `GameController.Refresh`,
  the single UI update path.
- **One undo entry per real change.** `GameController.Track` captures a `Snapshot`
  before the body, compares after, and pushes only if something moved. Exceptions
  propagate and push nothing.
- **Snapshots cover core state plus the user-intent slice of session state.**
  `SessionState.AutoExtractEnabled` is snapshotted; `StatusText` and
  `TicksThisSession` are transient and are not.

## Call flow: a key press

1. `MainScene._UnhandledInput` maps the key to a controller handler. The scene does
   not touch the model.
2. `GameController.Handle*Pressed` runs inside `Track`, which snapshots pre-state.
3. The handler body calls a static rules method (`ExtractionRules.Extract`, …) — the
   only place gameplay decisions live.
4. `Track` compares snapshots, pushes one undo entry if state changed, then calls
   `Refresh`.
5. `Refresh` projects state into a `MainViewModel` and hands it to `IMainView.Render`.
6. `MainScene.Render` writes labels and positions the drill via `PixelLayout`.

## Timing

`IPacer` is the controller's only view of time. `EnginePacer` defers to
`SceneTree.CreateTimer`; `SynchronousPacer` queues callbacks so tests advance time
explicitly. Auto-extract reschedules itself one tick at a time and stops when the seam
empties.

## Instrumentation

`IE.Model.Diagnostics.Log` is per-category × level. `Warn` and `Error` are always
compiled in; `Info`/`Debug`/`Trace` are gated on `IE_LOG_VERBOSE`, which
`Directory.Build.props` defines in every configuration so shipped builds still log.
Gate log-only precomputation on `Log.VerboseCompiled`. `GodotLogSink` is the only
place in the codebase that calls an engine print API.

## Toolchain

`scripts/setup-toolchain.sh` installs Godot 4.7.1 (.NET build) into `.toolchain/` and
a .NET 8 SDK into `$HOME/.dotnet`, then generates both solution files. The repo-root
`InfiniteExtraction.sln` drives `dotnet build`/`dotnet test`; `app/IE.App.sln` is the
one the Godot editor expects beside `project.godot`. `TargetFramework` is set once, in
`Directory.Build.props`.
