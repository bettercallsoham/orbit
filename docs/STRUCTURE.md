# Orbit project structure

This is one native .NET 10 WPF application. `Orbit.csproj` stays at the root so `dotnet build` works without extra arguments.

| Path | Responsibility |
| --- | --- |
| `src/Application/` | Startup, main window, system UI, and command-line entry |
| `src/Views/` | Radial overlay, dialogs, and notifications |
| `src/Controls/` | Reusable WPF drawing and input controls |
| `src/Wheel/` | Radial rendering API and renderer implementations |
| `src/Models/` | Settings, jobs, templates, and action data |
| `src/Services/` | Conversion, file operations, dependencies, persistence, and Windows integration |
| `resources/` | Orbit logo, icon, sound, font, and shared WPF styles |
| `tests/` | In-app verification and screenshot runner |
| `samples/` | Plugin and template examples |
| `docs/` | Current guides, changelog, and contributor information |

`Orbit.csproj` owns the application build. The `samples/` project is compiled separately. `tests/` contains the built-in verification commands invoked with `Orbit.dll --test`. Resources used at runtime are declared in `Orbit.csproj`; `LICENSE`, `COPYRIGHT`, and third-party notices stay at the repository root.
