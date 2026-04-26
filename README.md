# Age of Horizons (Godot 4 + C#)

Age of Horizons is an original turn-based 4X prototype (explore, expand, exploit, exterminate) built with **Godot 4** and **C#**.

## Setup Instructions

### Prerequisites
- Godot 4.6.2 Mono/.NET build (or newer 4.6.x)
- .NET 8 SDK

### Open the Project
1. Open Godot 4 (Mono build).
2. Import this folder (`Civ_Evolution`).
3. Build C# when prompted.
4. Run the project.

## How to Run
- Main scene: `res://scenes/Main.tscn`
- Starts in main menu (`New Game`, `Quit`).

## Controls
- **W/A/S/D**: pan camera across the world.
- **Middle Mouse Drag**: pan camera.
- **Mouse Wheel**: smooth zoom in/out.
- **Left Click** tile: select tile/unit/city tile.
- **Shift + Left Click** tile: move selected unit.
- **End Turn**: process yields, growth, production, research, AI turns.
- **Found City**: settle with settler on valid land tile.
- **Queue Warrior / Queue Builder**: add city production queue items.
- **Build Farm / Build Mine / Build Camp**: apply tile improvements.
- **Save / Load**: JSON save/load (`user://savegame.json`).

## Gameplay Loop (Prototype)
1. Start a new game.
2. Move settler and found city.
3. End turns to gain yields and complete production/research.
4. AI factions move/found cities automatically on their turns.
5. Win by:
   - controlling at least 3 cities (expansion victory), or
   - researching at least 3 technologies (knowledge victory).

## Architecture Overview

### Core Simulation Layer (plain C#)
Located in `src/AgeOfHorizons.Core` and does not depend on Godot nodes.

Key classes/systems:
- `GameState`, `GameConfig`, `TurnSystem`
- `HexCoord`, `HexGrid`, `HexPathfinder`
- `MapGenerator`, `TileState`, `PlayerState`, `CityState`, `UnitState`
- `TechTree`, `ProductionSystem`, `YieldSystem`
- `MovementSystem`, `CombatSystem`, `FogOfWarSystem`
- `AISystem`, `DiplomacySystem`, `VictorySystem`
- `SaveLoadSystem`, `EventLog`

### Godot Presentation Layer
Located in `scripts/presentation` and scenes in `scenes`.
- Rendering and interaction: `GameSceneController`, `MapView`, `CameraController`
- UI: `TopBarView`, `SelectionPanelView`, `EventLogView`, `VictoryScreenView`
- Menu: `MainMenuController`

## Data-Driven Content
JSON files in `data/` define:
- terrain
- resources
- units
- buildings
- technologies
- factions
- AI personalities
- victory settings

### Add New Content
- Add a new JSON row/object in the matching file (e.g., `data/units.json`).
- Ensure `id` is unique.
- Reference the new `id` from techs, production, or faction config where needed.
- Restart game to load updated data definitions.

## Tests
- Core tests are in `tests/` (`AgeOfHorizons.Core.Tests.csproj`).
- Run with `dotnet test tests/AgeOfHorizons.Core.Tests.csproj`.

## Known Limitations
- Minimal UI panels (city/tech/diplomacy placeholders).
- Basic combat and AI heuristics only.
- No advanced diplomacy, trade, culture, religion, or tactical layers.
- Rendering uses simple debug circles instead of final art.
- Map generation is simple random terrain.

## Recommended Next Improvements
1. Better procedural map generation (continents/biomes/resources).
2. Rich city management and building effects.
3. Full tech tree UI and player tech choice.
4. Expanded AI decision-making and diplomacy.
5. Better pathfinding (weighted terrain + zone of control).
6. Unit promotions and ranged combat.
7. Tooltips, production queue UI, and minimap.
8. Deterministic save/load compatibility versioning.


## Art / Assets
- To keep this repository binary-free, visual assets are generated at runtime by `MapView.cs`.
- The `assets/README.md` file documents this approach.
- No external art downloads are required for the prototype.
