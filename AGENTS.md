# Project Guidelines

## Overview

**来吧宝可梦 (PokeballShuffler)** — A .NET MAUI mobile app that simulates a Pokeball shuffle game.

Current gameplay includes two modes:

- **Normal mode**: Standard 4-round draw flow (4, 3, 2, 1) from a pool of 15 balls.
- **Extended mode**: Adds two character skills on top of the same 4-round flow:
	- **Char1**: The 4th ball of Basket 1 is hidden and can be revealed by tapping the Char1 slot.
	- **Char2**: After round 4, rerolls Basket 4 by swapping with a random ball from Undrawn (one-time use).

Draws are animated with staggered ball reveals, and the Undrawn panel is animated when final results are shown.

## Architecture

- **Pattern**: MVVM with `CommunityToolkit.Mvvm` (v8.4.2)
- **UI approach**: Code-first — all UI is built programmatically in `MainPage.cs`, NOT in XAML. The `MainPage.xaml` is excluded from build via `<MauiXaml Remove="MainPage.xaml" />`.
- **Animations**: Event-driven (`BallAddedToBasket`, `Basket4Rerolled`) with async fade/scale choreography in code-behind.
- **State management**: `ObservableCollection<Pokeball>` for basket/undrawn state; `OnPropertyChanged` + `NotifyCanExecuteChanged` for command state.
- **Mode/Theming**: Runtime mode switch (`GameMode.Normal` / `GameMode.Extended`) with light/dark palette switching in `MainPage.cs`.

### Key Files

| File | Role |
|------|------|
| `MainPage.cs` | Full UI construction (inventory panel, 4 baskets, undrawn panel, controls), animation choreography, character avatar interactions, theme application |
| `ViewModels/MainViewModel.cs` | Game logic: shuffle draws, hidden-ball handling (Extended), Char2 reroll, reset, command state |
| `Models/GameMode.cs` | Enum for mode state (`Normal`, `Extended`) |
| `Models/Pokeball.cs` | Ball model with `BallType`, computed `AccentColor`, `DisplayName`, image path |
| `Models/BallType.cs` | Enum: PokeBall, PremierBall, GreatBall, UltraBall, MasterBall |
| `MauiProgram.cs` | MAUI builder config, font registration |

### Communication Flow

```
MainViewModel ──(event: BallAddedToBasket)──▸ MainPage.cs (triggers animation)
MainViewModel ──(event: Basket4Rerolled)──▸ MainPage.cs (swap animation + undrawn rebuild)
MainViewModel ──(data binding)──▸ UI elements (labels, button states)
MainPage.cs ──(command binding)──▸ MainViewModel (Shuffle, Reset, ToggleGameMode, Char2Reroll)
```

## Build and Test

```bash
# Debug build (Android)
dotnet build PokeballShuffler/PokeballShuffler.csproj -f net9.0-android -c Debug

# Release build (Android)
dotnet build PokeballShuffler/PokeballShuffler.csproj -f net9.0-android -c Release

# Install to connected Android device
adb install -r "PokeballShuffler/bin/Release/net9.0-android/com.jansen611.pokeballshuffler-Signed.apk"
```

VS Code tasks are configured for all of the above — use the task runner.

Additional task is available for release flow:

```bash
# Build and install sequence
Build and Install APK (Release)
```

**Target frameworks**: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`. Android is the primary development target.

## Code Style

- **C# with nullable enabled**, implicit usings enabled
- Private fields: `_camelCase`; properties/methods/events: `PascalCase`
- **UI field naming (MainPage.cs code-behind exception)**: use `type_name` for UI element fields (for example `btn_modeToggle`, `btn_reset`, `lbl_roundLabel`, `vsl_basketContainers`, `brd_inventoryPanel`) to make control roles immediately recognizable.
- Use **switch expressions** for type-based mappings (see `Pokeball.AccentColor`)
- Use **XML doc comments** (`/// <summary>`) on public members
- Write **code comments in English** (app display name is Chinese, code is English)
- `[RelayCommand]` attribute generates commands — method `Shuffle()` becomes `ShuffleCommand`
- Async commands use `Task`-returning methods with `await` for animation pacing

## Conventions

- **No XAML for pages** — build UI in C# code-behind. XAML is only used for `App.xaml` (resource dictionaries) and `Resources/Styles/`.
- **Theme behavior**:
	- Normal mode uses a light beige palette.
	- Extended mode uses the dark palette (background `#1a1a2e`, accent `#e94560`).
	- Keep both palettes in sync when introducing new controls.
- **Landscape-only**: Android is locked to landscape orientation via `AndroidManifest.xml`.
- **Minimal dependencies**: Only `CommunityToolkit.Mvvm` beyond base MAUI. Avoid adding packages unless necessary.
- **Animation pattern**: Fire events from ViewModel, handle animation in code-behind with `async`/`await` and Fade/Scale choreography.
- **Extended mode UX rules**:
	- Do not add the hidden 4th ball directly into `Basket1`; keep it in `HiddenBall` and reveal via Char1 slot.
	- Char2 reroll is one-time per game and only available after all rounds complete.
	- Keep Char2 disabled while Undrawn reveal animation is running (`_isUndrawnAnimating` gate in UI).
	- Keep character availability visuals (`Opacity`/`IsEnabled`) synchronized with ViewModel state.
