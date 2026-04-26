using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AgeOfHorizons.Core;

public static class GameBootstrap
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static GameConfig LoadConfig(string projectRoot) => LoadConfigFromDirectory(Path.Combine(projectRoot, "data"));

    public static GameConfig LoadConfigFromDirectory(string dataDirectory)
    {
        T Load<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(dataDirectory, file)), Options)!;

        return new GameConfig
        {
            Terrains = Load<List<TerrainDef>>("terrain.json").ToDictionary(x => x.Id, x => x),
            Resources = Load<List<ResourceDef>>("resources.json").ToDictionary(x => x.Id, x => x),
            Units = Load<List<UnitDef>>("units.json").ToDictionary(x => x.Id, x => x),
            Buildings = Load<List<BuildingDef>>("buildings.json").ToDictionary(x => x.Id, x => x),
            Techs = Load<List<TechDef>>("technologies.json").ToDictionary(x => x.Id, x => x),
            Factions = Load<List<FactionDef>>("factions.json").ToDictionary(x => x.Id, x => x),
            AiPersonalities = Load<List<AiPersonalityDef>>("ai_personalities.json").ToDictionary(x => x.Id, x => x),
            Victory = Load<VictorySettings>("victory.json")
        };
    }

    public static GameState NewGame(GameConfig config, int width = 46, int height = 32, int aiPlayers = 2, int? seed = null)
    {
        var gameSeed = seed ?? Random.Shared.Next();
        var rng = new Random(gameSeed);
        var state = new GameState
        {
            MapSeed = gameSeed,
            Grid = MapGenerator.Generate(width, height, config, rng),
            Players = new List<PlayerState>()
        };

        state.Players.Add(new PlayerState { Id = 1, Name = "Aurora League", FactionId = config.Factions.Keys.First(), IsAI = false, Gold = 10 });
        for (var i = 0; i < aiPlayers; i++)
        {
            var id = i + 2;
            var factionId = config.Factions.Keys.Skip(i + 1).FirstOrDefault() ?? config.Factions.Keys.First();
            state.Players.Add(new PlayerState { Id = id, Name = $"AI {id}", FactionId = factionId, IsAI = true, Gold = 10 });
        }

        state.ActivePlayerId = 1;

        foreach (var player in state.Players)
        {
            HexCoord start;
            do
            {
                start = new HexCoord(rng.Next(width), rng.Next(height));
            } while (state.Grid.Get(start)!.TerrainId == "water" || state.Units.Any(u => u.Coord.Equals(start)));

            state.Units.Add(new UnitState { Id = state.NextUnitId, OwnerPlayerId = player.Id, UnitDefId = "settler", Coord = start, MovesRemaining = config.Units["settler"].Move });
            state.Units.Add(new UnitState { Id = state.NextUnitId, OwnerPlayerId = player.Id, UnitDefId = "scout", Coord = start, MovesRemaining = config.Units["scout"].Move });
            state.Units.Add(new UnitState { Id = state.NextUnitId, OwnerPlayerId = player.Id, UnitDefId = "warrior", Coord = start, MovesRemaining = config.Units["warrior"].Move });
        }

        FogOfWarSystem.UpdateVisibility(state, state.ActivePlayerId);
        state.EventLog.Add($"World seed {gameSeed} generated.");
        return state;
    }
}
