# Project Guidelines

## Overview

**来吧宝可梦 (PokeballShuffler)** — A .NET MAUI mobile app that simulates a Pokéball shuffle game. Players draw balls across 4 rounds (4, 3, 2, 1) from a pool of 15 balls, with animated cascade reveals.

## Architecture

- **Pattern**: MVVM with `CommunityToolkit.Mvvm` (v8.4.2)
- **UI approach**: Code-first — all UI is built programmatically in `MainPage.cs`, NOT in XAML. The `MainPage.xaml` is excluded from build via `<MauiXaml Remove="MainPage.xaml" />`.
- **Animations**: Event-driven (`BallAddedToBasket` event) with async Scale animations (150ms per ball cascade)
- **State management**: `ObservableCollection<Pokeball>` for reactive UI; `OnPropertyChanged` + `NotifyCanExecuteChanged` for command state

### Key Files

| File | Role |
|------|------|
| `MainPage.cs` | Full UI construction (Grid, Border, StackLayout), animations, event handling |
| `ViewModels/MainViewModel.cs` | Game logic: shuffle (Fisher-Yates), reset, round state, command bindings |
| `Models/Pokeball.cs` | Ball model with `BallType`, computed `AccentColor`, `DisplayName`, image path |
| `Models/BallType.cs` | Enum: PokeBall, PremierBall, GreatBall, UltraBall, MasterBall |
| `MauiProgram.cs` | MAUI builder config, font registration |

### Communication Flow

```
MainViewModel ──(event: BallAddedToBasket)──▸ MainPage.cs (triggers animation)
MainViewModel ──(data binding)──▸ UI elements (labels, button states)
MainPage.cs ──(command binding)──▸ MainViewModel (Shuffle, Reset)
```

## Build and Test

```bash
# Debug build (Android)
dotnet build PokeballShuffler/PokeballShuffler.csproj -f net9.0-android -c Debug

# Release build (Android)
dotnet build PokeballShuffler/PokeballShuffler.csproj -f net9.0-android -c Release

# Install to connected Android device
adb install -r "PokeballShuffler/bin/Release/net9.0-android/com.companyname.pokeballshuffler-Signed.apk"
```

VS Code tasks are configured for all of the above — use the task runner.

**Target frameworks**: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`. Android is the primary development target.

## Code Style

- **C# with nullable enabled**, implicit usings enabled
- Private fields: `_camelCase`; properties/methods/events: `PascalCase`
- Use **switch expressions** for type-based mappings (see `Pokeball.AccentColor`)
- Use **XML doc comments** (`/// <summary>`) on public members
- Write **code comments in English** (app display name is Chinese, code is English)
- `[RelayCommand]` attribute generates commands — method `Shuffle()` becomes `ShuffleCommand`
- Async commands use `Task`-returning methods with `await` for animations

## Conventions

- **No XAML for pages** — build UI in C# code-behind. XAML is only used for `App.xaml` (resource dictionaries) and `Resources/Styles/`.
- **Dark theme**: background `#1a1a2e`, accent `#e94560`. Respect this palette when adding UI.
- **Landscape-only**: Android is locked to landscape orientation via `AndroidManifest.xml`.
- **Minimal dependencies**: Only `CommunityToolkit.Mvvm` beyond base MAUI. Avoid adding packages unless necessary.
- **Animation pattern**: Fire events from ViewModel, handle animation in code-behind with `async`/`await` and `ScaleTo`/delay choreography.
