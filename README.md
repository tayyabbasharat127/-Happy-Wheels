# Happy Wheels Hill Climb

A 2D Unity driving game where you push a fragile car across rough terrain, manage fuel, and try to survive four distance-based levels.

## How the Game Works

Press **Play**, enter your player name, and start driving. The goal is to move as far to the right as possible without flipping the car or letting the driver's head hit the ground.

The car accelerates with the **Right Arrow** key. Holding the key spins the wheels, builds forward thrust, and consumes fuel. Fuel cans on the course help you keep going.

Your score increases as the car moves forward. Each level is completed when you reach **1000 m**. After completing a level, the game pauses and lets you continue to the next one.

## Controls

- **Right Arrow**: Accelerate and thrust forward
- **Play / Next Level**: Start the run or continue after a completed level
- **Try Again**: Restart the current level after a crash
- **Main Menu**: Return to the menu and reset run progress

## Rules

- Reach **1000 m** to clear a level.
- Clear **4 levels** to win the game.
- If the car stays flipped over too long, the run ends.
- If the driver's head touches the ground, the run ends.
- Running out of momentum or fuel makes it harder to reach the goal, so collect fuel cans whenever possible.

## Project

Built with Unity as a small 2D physics driving game. The main project files live in:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Unity-generated cache folders such as `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are intentionally ignored.
