# ARCHITECTURE

Infinite Extraction is a 2D game: a C# core with a Godot 4 view layer. The core holds
all gameplay logic and is framework-free; Godot supplies rendering, input, and timing
and nothing else.

The core is currently a shell: the layering, the logging system, the view boundary,
and the pacer are in place, and there is no gameplay on top of them yet. Domain state
and rules go in `IE.Model`, orchestration in `IE.Controller` — see Extension points.

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
| `IE.Model` | `src/IE.Model/` | nothing | logging system; domain state, static rules, primitives |
| `IE.Controller` | `src/IE.Controller/` | `IE.Model` | input handling, mutation orchestration, view/pacer interfaces |
| `IE.ViewMath` | `src/IE.ViewMath/` | `IE.Model` | viewport/DPI math (the only core assembly where floats are legal) |
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
  the single UI update path. `Refresh` is private; nothing else repaints.
- **`SessionState` is split by snapshot eligibility.** User-intent fields belong in an
  undo snapshot, transient ones (`StatusText`, `RendersThisSession`) never do.

## Call flow: boot

1. Godot loads `res://scenes/Main.tscn`; `MainScene._Ready` runs.
2. `MainScene` installs `GodotLogSink`, applies `IE_LOG`, and logs the boot line —
   instrumentation first, so the rest of boot is visible.
3. It constructs `SessionState`, then `GameController`, injecting itself as
   `IMainView` and an `EnginePacer` as `IPacer`.
4. `GameController.Start` sets the status line and calls `Refresh`.
5. `Refresh` projects state into a `MainViewModel`, counts the paint in
   `SessionState.RendersThisSession`, and hands the model to `IMainView.Render`.
6. `MainScene.Render` writes the status label.

`MainScene._UnhandledInput` maps Escape to quit and forwards nothing else. Key
handling belongs in the controller: the scene maps a key to a handler and never
touches state itself.

## Extension points

- A domain type and a static rules class in `IE.Model`, integer-only, no I/O.
- A `Handle*Pressed` method on `GameController` per user intent, wrapped in a
  tracking helper that snapshots pre-state, runs the body, and pushes one undo entry
  iff state changed — exceptions propagate and push nothing.
- Fields on `MainViewModel` for whatever the view must paint, never a reference to
  live state.
- A key case in `MainScene._UnhandledInput` forwarding to the handler.

## Timing

`IPacer` is the controller's only view of time, exposed as `GameController.Pacer`.
`EnginePacer` defers to `SceneTree.CreateTimer`; `SynchronousPacer` queues callbacks
so tests advance time explicitly. Timed behaviour goes through this and never through
an engine timer.

## Instrumentation

`IE.Model.Diagnostics.Log` is per-category × level. `Warn` and `Error` are always
compiled in; `Info`/`Debug`/`Trace` are gated on `IE_LOG_VERBOSE`, which
`Directory.Build.props` defines in every configuration so shipped builds still log.
Gate log-only precomputation on `Log.VerboseCompiled`. `GodotLogSink` is the only
place in the codebase that calls an engine print API.

`IE_HEADLESS_SELFTEST=1` makes `MainScene` assert the boot path reached the real view
and quit with the matching exit code, so a headless run proves the engine-side wiring
executes. Extend the assertion as gameplay lands.

## Toolchain

`scripts/setup-toolchain.sh` installs Godot 4.7.1 (.NET build) into `.toolchain/` and
a .NET 8 SDK into `$HOME/.dotnet`, then generates both solution files. The repo-root
`InfiniteExtraction.sln` drives `dotnet build`/`dotnet test`; `app/IE.App.sln` is the
one the Godot editor expects beside `project.godot`. `TargetFramework` is set once, in
`Directory.Build.props`.
