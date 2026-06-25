# Happy Wheels Hill Climb

A 2D Unity physics driving game inspired by hill-climb arcade gameplay. Drive a fragile vehicle across rough terrain, manage fuel and nitro, survive crashes, and complete four distance-based levels.

## Overview

Happy Wheels Hill Climb focuses on simple controls, readable physics, and progressively harder terrain. The player must keep the vehicle stable while climbing hills, using nitro at the right moments, collecting resources, and reaching the level distance goal.

The game includes a complete run loop with player name input, score tracking, lives, checkpoints, respawn handling, fuel management, nitro charges, pause/game-over/win states, and level progression.

## Features

- Four distance-based levels
- Progressive terrain difficulty with rougher hills and bumps on higher levels
- Fuel system with low-fuel warnings and fuel pickups
- Nitro boost system with limited charges
- Life system with checkpoint-based respawning
- Safe respawn logic that validates road surface and spawn clearance
- Flip and head-impact death detection
- Score tracking and level completion flow
- Dynamic camera follow and speed-based zoom
- In-game UI for lives, fuel, nitro, speed, score, pause, and level completion
- Procedural UI/audio/background managers created at runtime

## Controls

| Action | Key |
| --- | --- |
| Accelerate | `Right Arrow` / `D` |
| Brake / Reverse | `Left Arrow` / `A` |
| Nitro boost | `Space` |
| Recover to checkpoint | `R` |
| Pause / Resume | `P` / `Esc` |

## Gameplay Rules

- Reach `1000 m` to complete each level.
- Complete all four levels to win the run.
- The vehicle loses fuel while driving.
- Fuel pickups restore fuel.
- Nitro gives a strong forward and upward launch.
- If the driver head touches the ground, the player loses a life.
- If the vehicle remains flipped for too long, the player loses a life.
- When lives remain, the vehicle respawns at the latest safe checkpoint.
- When no lives remain, the game ends.

## Level Difficulty

Difficulty increases mainly through terrain:

- Level 1: introductory terrain
- Level 2: mild hills and bumps
- Level 3: rougher terrain with stronger slope variation
- Level 4: most aggressive terrain profile

Obstacle spawning is currently disabled so the difficulty comes from driving skill, terrain control, fuel, and nitro timing.

## Respawn System

The respawn system is designed to keep physics stable after crashes:

- Locks respawn so death cannot trigger multiple times
- Freezes the car during the respawn transition
- Clears velocity, angular velocity, thrust, nitro, and stuck state
- Resets the car body, wheels, and attached player rigidbodies
- Finds a valid road surface before spawning
- Checks that the spawn area is clear of blocking colliders
- Aligns the car with the road slope
- Falls back to the previous valid checkpoint if the latest checkpoint is unsafe
- Resets the camera target after the vehicle is placed

## Project Structure

```text
Assets/
  Images/              Game sprites and UI images
  Materials/           Physics and rendering materials
  Prefabs/             Particle and effect prefabs
  Scenes/              Main gameplay scene
  Scripts/             Gameplay, UI, audio, managers, vehicle logic

Packages/              Unity package manifest and lock file
ProjectSettings/       Unity project configuration
```

## Important Scripts

| Script | Purpose |
| --- | --- |
| `CarController.cs` | Vehicle movement, fuel, nitro, physics reset, respawn pose |
| `GameStateManager.cs` | Run state, score, level flow, pause, win/game-over logic |
| `LifeManager.cs` | Lives, respawn sequence, respawn lock |
| `CheckpointManager.cs` | Safe checkpoint creation and respawn validation |
| `TerrainDifficultyManager.cs` | Runtime terrain difficulty scaling by level |
| `UIManager.cs` | Runtime HUD, menus, overlays, score/lives/fuel/nitro display |
| `AudioManager.cs` | Runtime-generated sound effects and engine audio |
| `Follow.cs` | Camera follow, smoothing, speed zoom, respawn retargeting |

## Setup

1. Open the project in Unity.
2. Open the gameplay scene from `Assets/Scenes/SampleScene.unity`.
3. Press Play.
4. Enter a player name and start driving.

This repository contains Unity-generated project folders. If Unity needs to reimport assets, it may regenerate `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or project files.

## Development Notes

- The game uses Unity 2D physics and SpriteShape terrain.
- Managers are automatically created by `GameStateManager` when missing from the scene.
- Level completion is score/distance based rather than a physical finish object.
- Checkpoints are score-threshold based and validated against the current road surface.
- Runtime terrain changes refresh the current checkpoint so respawns match the modified road.

## License

No license has been specified for this project. Add one before distributing or publishing the game.
