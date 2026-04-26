using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AgeOfHorizons.Core
{

public static class MapGenerator
{
    public static HexGrid Generate(int width, int height, GameConfig config, Random rng)
    {
        var grid = new HexGrid { Width = width, Height = height };
        var terrainIds = config.Terrains.Keys.Where(x => x != "water").ToList();
        var resourceIds = config.Resources.Keys.ToList();

        for (var q = 0; q < width; q++)
        for (var r = 0; r < height; r++)
        {
            var terrainId = terrainIds[rng.Next(terrainIds.Count)];
            if (rng.NextDouble() < 0.18) terrainId = "water";

            var tile = new TileState { Coord = new HexCoord(q, r), TerrainId = terrainId };
            if (terrainId != "water" && rng.NextDouble() < 0.20)
                tile.ResourceId = resourceIds[rng.Next(resourceIds.Count)];

            grid.Tiles[tile.Coord.ToString()] = tile;
        }

        return grid;
    }
}

public static class HexPathfinder
{
    public static List<HexCoord> FindPath(GameState state, HexCoord start, HexCoord goal)
    {
        if (start.Equals(goal)) return new List<HexCoord> { start };
        var frontier = new Queue<HexCoord>();
        var cameFrom = new Dictionary<HexCoord, HexCoord?> { [start] = null };
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in current.Neighbors().Where(state.Grid.Contains))
            {
                if (cameFrom.ContainsKey(next)) continue;
                var tile = state.Grid.Get(next);
                if (tile == null || tile.TerrainId == "water") continue;
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal)) return new List<HexCoord>();

        var path = new List<HexCoord>();
        var cursor = goal;
        while (true)
        {
            path.Add(cursor);
            var prev = cameFrom[cursor];
            if (prev == null) break;
            cursor = prev.Value;
        }
        path.Reverse();
        return path;
    }
}

public static class MovementSystem
{
    public static bool TryMoveUnit(GameState state, GameConfig config, UnitState unit, HexCoord target)
    {
        if (unit.ActionPointsRemaining <= 0) return false;
        var path = HexPathfinder.FindPath(state, unit.Coord, target);
        if (path.Count < 2) return false;

        var maxMove = config.Units[unit.UnitDefId].Move;
        var steps = path.Count - 1;
        if (steps > Math.Min(unit.MovesRemaining, maxMove)) return false;

        unit.Coord = target;
        unit.MovesRemaining -= steps;
        if (unit.MovesRemaining == 0) unit.ActionPointsRemaining = 0;
        return true;
    }

    public static void RefreshMoves(GameState state, GameConfig config)
    {
        foreach (var unit in state.Units.Where(u => !u.Consumed && u.OwnerPlayerId == state.ActivePlayerId))
        {
            unit.MovesRemaining = config.Units[unit.UnitDefId].Move;
            unit.ActionPointsRemaining = 1;
        }
    }
}

public static class YieldSystem
{
    public static (int Food, int Production, int Science, int Gold, int Culture, int Influence) CalculateCityYield(GameState state, GameConfig config, CityState city)
    {
        var tile = state.Grid.Get(city.Coord)!;
        var terrain = config.Terrains[tile.TerrainId];
        var food = terrain.Food + city.Population;
        var production = terrain.Production + 1;
        var science = terrain.Science + 1;
        var gold = 1 + (tile.ResourceId != null ? 1 : 0);
        var culture = 1;
        var influence = 1;

        if (tile.ImprovementId == "farm") food += 1;
        if (tile.ImprovementId == "mine") production += 1;
        if (tile.ImprovementId == "camp") gold += 1;
        if (tile.ImprovementId == "road") influence += 1;

        return (food, production, science, gold, culture, influence);
    }
}

public static class ProductionSystem
{
    public static void ProcessCityProduction(GameState state, GameConfig config, CityState city)
    {
        var yields = YieldSystem.CalculateCityYield(state, config, city);
        city.StoredProduction += yields.Production;
        city.StoredFood += yields.Food;

        if (city.StoredFood >= 8 + city.Population * 4)
        {
            city.StoredFood = 0;
            city.Population++;
            state.EventLog.Add($"{city.Name} grew to pop {city.Population}.");
        }

        var buildId = city.ProductionQueue.FirstOrDefault() ?? city.CurrentProductionId;
        var unitDef = config.Units.GetValueOrDefault(buildId);
        if (unitDef != null && city.StoredProduction >= unitDef.Cost)
        {
            city.StoredProduction -= unitDef.Cost;
            state.Units.Add(new UnitState
            {
                Id = state.NextUnitId,
                OwnerPlayerId = city.OwnerPlayerId,
                UnitDefId = unitDef.Id,
                Coord = city.Coord,
                MovesRemaining = unitDef.Move,
                ActionPointsRemaining = 1
            });
            if (city.ProductionQueue.Count > 0)
                city.ProductionQueue.RemoveAt(0);
            state.EventLog.Add($"{city.Name} trained {unitDef.Name}.");
        }
    }
}

public static class TechTree
{
    public static bool CanResearch(PlayerState player, TechDef tech) => tech.Prereqs.All(player.ResearchedTechs.Contains);

    public static void ProcessResearch(GameState state, GameConfig config, PlayerState player)
    {
        var scienceGain = state.Cities.Where(c => c.OwnerPlayerId == player.Id)
            .Select(c => YieldSystem.CalculateCityYield(state, config, c).Science).Sum();

        player.Science += Math.Max(1, scienceGain);

        if (!config.Techs.TryGetValue(player.CurrentResearchTechId, out var tech)) return;
        if (player.ResearchedTechs.Contains(tech.Id)) return;
        if (!CanResearch(player, tech)) return;

        if (player.Science >= tech.Cost)
        {
            player.Science -= tech.Cost;
            player.ResearchedTechs.Add(tech.Id);
            player.Era = EraSystem.FromTechCount(player.ResearchedTechs.Count);
            state.EventLog.Add($"{player.Name} researched {tech.Name} ({player.Era}).");

            var next = config.Techs.Values.FirstOrDefault(t => !player.ResearchedTechs.Contains(t.Id) && CanResearch(player, t));
            if (next != null) player.CurrentResearchTechId = next.Id;
        }
    }
}

public static class CombatSystem
{
    public static bool ResolveAttack(GameState state, GameConfig config, UnitState attacker, UnitState defender)
    {
        if (attacker.Coord.DistanceTo(defender.Coord) > 1 || attacker.ActionPointsRemaining <= 0) return false;
        var attack = config.Units[attacker.UnitDefId].Strength;
        var defense = config.Units[defender.UnitDefId].Strength;
        defender.Health -= Math.Max(8, attack - defense + 18);
        attacker.ActionPointsRemaining = 0;

        if (defender.Health <= 0)
        {
            defender.Consumed = true;
            state.EventLog.Add($"{attacker.OwnerPlayerId} defeated unit {defender.Id}.");
            return true;
        }
        return false;
    }
}

public static class FogOfWarSystem
{
    public static void UpdateVisibility(GameState state, int playerId)
    {
        var player = state.Players.First(p => p.Id == playerId);
        player.VisibleTiles.Clear();
        foreach (var unit in state.Units.Where(u => !u.Consumed && u.OwnerPlayerId == playerId))
            AddVision(player.VisibleTiles, unit.Coord, 2, state.Grid);
        foreach (var city in state.Cities.Where(c => c.OwnerPlayerId == playerId))
            AddVision(player.VisibleTiles, city.Coord, 3, state.Grid);
    }

    private static void AddVision(HashSet<string> visible, HexCoord center, int radius, HexGrid grid)
    {
        for (var q = center.Q - radius; q <= center.Q + radius; q++)
        for (var r = center.R - radius; r <= center.R + radius; r++)
        {
            var c = new HexCoord(q, r);
            if (grid.Contains(c) && center.DistanceTo(c) <= radius)
                visible.Add(c.ToString());
        }
    }
}

public static class DiplomacySystem
{
    public static string GetRelation(PlayerState a, PlayerState b) => a.Id == b.Id ? "Self" : "Neutral";
}

public static class EraSystem
{
    private static readonly string[] Eras =
    {
        "Stone Age", "Bronze Age", "Iron Age", "Classical Era", "Medieval Era", "Early Modern Era", "Modern Era",
        "Atomic Era", "Information Era", "Singularity Era", "Planetary Era", "Stellar Era", "Interstellar Era", "Intergalactic Era", "Multiverse Era"
    };

    public static string FromTechCount(int count)
    {
        var idx = Math.Clamp(count / 2, 0, Eras.Length - 1);
        return Eras[idx];
    }
}

public static class VictorySystem
{
    public static string? CheckVictory(GameState state, GameConfig config)
    {
        foreach (var p in state.Players)
        {
            var cityCount = state.Cities.Count(c => c.OwnerPlayerId == p.Id);
            if (cityCount >= config.Victory.DominationCityCount) return $"{p.Name} wins by expansion.";
            if (p.ResearchedTechs.Count >= config.Victory.ScienceTechsRequired) return $"{p.Name} wins by knowledge.";
        }
        return null;
    }
}

public static class AISystem
{
    public static void RunTurn(GameState state, GameConfig config, PlayerState ai, Random rng)
    {
        var aiUnits = state.Units.Where(u => !u.Consumed && u.OwnerPlayerId == ai.Id).ToList();
        foreach (var u in aiUnits)
        {
            var candidates = u.Coord.Neighbors().Where(state.Grid.Contains)
                .Where(c => state.Grid.Get(c)?.TerrainId != "water").ToList();

            if (candidates.Count > 0)
            {
                var target = candidates[rng.Next(candidates.Count)];
                MovementSystem.TryMoveUnit(state, config, u, target);
            }

            if (config.Units[u.UnitDefId].CanFoundCity && !state.Cities.Any(c => c.Coord.Equals(u.Coord)))
            {
                state.Cities.Add(new CityState
                {
                    Id = state.NextCityId,
                    OwnerPlayerId = ai.Id,
                    Name = $"{ai.Name} Hold {state.NextCityId}",
                    Coord = u.Coord,
                    CurrentProductionId = "warrior"
                });
                u.Consumed = true;
                state.EventLog.Add($"{ai.Name} founded a city.");
            }
        }
    }
}

public static class TurnSystem
{
    public static void EndTurn(GameState state, GameConfig config)
    {
        var rng = new Random();
        var processed = new HashSet<int>();

        while (true)
        {
            var current = state.ActivePlayer;
            if (!processed.Contains(current.Id))
            {
                foreach (var city in state.Cities.Where(c => c.OwnerPlayerId == current.Id))
                {
                    var yields = YieldSystem.CalculateCityYield(state, config, city);
                    current.Gold += yields.Gold;
                    current.Culture += yields.Culture;
                    current.Influence += yields.Influence;
                    ProductionSystem.ProcessCityProduction(state, config, city);
                }
                TechTree.ProcessResearch(state, config, current);
                processed.Add(current.Id);
            }

            var currentIndex = state.Players.FindIndex(p => p.Id == state.ActivePlayerId);
            var nextIndex = (currentIndex + 1) % state.Players.Count;
            state.ActivePlayerId = state.Players[nextIndex].Id;
            if (nextIndex == 0) state.Turn++;

            MovementSystem.RefreshMoves(state, config);
            FogOfWarSystem.UpdateVisibility(state, state.ActivePlayerId);

            if (!state.ActivePlayer.IsAI) break;
            AISystem.RunTurn(state, config, state.ActivePlayer, rng);
        }

        state.Units.RemoveAll(u => u.Consumed);
        state.VictoryWinner = VictorySystem.CheckVictory(state, config);
    }
}

public static class SaveLoadSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

    public static void Save(string path, GameState state) => File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));

    public static GameState Load(string path) => JsonSerializer.Deserialize<GameState>(File.ReadAllText(path), JsonOptions)!;
}
}
