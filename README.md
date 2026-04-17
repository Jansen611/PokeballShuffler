# PokeballShuffler

[中文说明 / Chinese Version](README_CN.md)

A .NET MAUI mobile app that simulates a Pokeball shuffle game with animated draws, two gameplay modes, and character skills.

## Origin Story

This app started during a camping trip with kids.
We brought the "Come on! Pokemon" board game, but a few Pokeball tokens went missing in the middle of play, so we could not continue properly.

So I had a quick idea: build a digital version on the spot with AI.
In about 1.5 hours, the standard version was up and running.

When I got home, I kept going and built the extended mode too.

If this project helps other families, players, or makers in the open-source community, that would make me super happy.

## Features

- 4-round draw flow from a 15-ball pool: `4 -> 3 -> 2 -> 1`
- Two game modes:
  - **Normal Mode**: standard shuffle and reveal
  - **Extended Mode**:
    - **Character 1**: the 4th ball in Basket 1 is hidden and can be revealed by tapping the Character 1 icon
    - **Character 2**: after round 4, reroll Basket 4 once by swapping with a random ball from Undrawn
- Animated staggered reveals for each draw
- Animated Undrawn panel reveal for final results
- Runtime mode switching with light/dark themed palettes
- Landscape-focused gameplay on Android

## Tech Stack

- .NET MAUI
- C#
- Code-first UI

## Project Structure

- `PokeballShuffler/MainPage.cs`: UI construction, theme application, animation choreography
- `PokeballShuffler/ViewModels/MainViewModel.cs`: game flow, hidden-ball logic, Char2 reroll, command state
- `PokeballShuffler/Models/`: game enums/models (`BallType`, `GameMode`, `Pokeball`)

## Quick Start

1. Install .NET SDK (with MAUI workload) and Android development prerequisites.
2. Build the Android target.
3. Install the generated APK on a connected Android device.
4. Launch and play in landscape mode.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
