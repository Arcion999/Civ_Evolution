using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AgeOfHorizons.Core;

public static class MapGenerator
{
    public static HexGrid Generate(int width, int height, GameConfig config, Random rng)
    {
        var grid = new HexGrid { Width = width, Height = height };
        var terrainIds = config.Terrains.Keys.Where(x => x != "water").ToList();
        for (var q = 0; q < width; q++)
        for (var r = 0; r < height; r++)
        {
            var terrainId = terrainIds[rng.Next(terrainIds.Count)];
            if (rng.NextDouble() < 0.15) terrainId = "water";
            var tile = new TileState { Coord = new HexCoord(q, r), TerrainId = terrainId };
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
        var cameFrom = new Dictionary<HexCoord, HexCoord?>();
        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in current.Neighbors().Where(state.Grid.Contains))
            {
                if (cameFrom.ContainsKey(next)) continue;
                var tile = state.Grid.Get(next);
                if (tile == null || tile.TerrainId == "water") continue;
                cameFrom[next] = current;
                if (next.Equals(goal))
                {
                    var path = new List<HexCoord> { goal };
                    var cursor = current;
                    while (!cursor.Equals(start))
                    {
                        path.Add(cursor);
                        cursor = cameFrom[cursor]!.Value;
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }
                frontier.Enqueue(next);
            }
        }

        return new List<HexCoord>();
    }
}

public static class MovementSystem
{
    public static bool TryMoveUnit(GameState state, GameConfig config, UnitState unit, HexCoord target)
    {
        var path = HexPathfinder.FindPath(state, unit.Coord, target);
        if (path.Count < 2) return false;
        var def = config.Units[unit.UnitDefId];
        var steps = path.Count - 1;
        if (steps > Math.Min(unit.MovesRemaining, def.Move)) return false;

        unit.Coord = target;
        unit.MovesRemaining -= steps;
        return true;
    }

    public static void RefreshMoves(GameState state, GameConfig config)
    {
        foreach (var unit in state.Units.Where(u => !u.Consumed && u.OwnerPlayerId == state.ActivePlayerId))
        {
            unit.MovesRemaining = config.Units[unit.UnitDefId].Move;
        }
    }
}

public static class YieldSystem
{
    public static (int Food, int Production, int Science) CalculateCityYield(GameState state, GameConfig config, CityState city)
    {
        var tile = state.Grid.Get(city.Coord)!;
        var terrain = config.Terrains[tile.TerrainId];
        return (terrain.Food + city.Population, terrain.Production + 1, terrain.Science + 1);
    }
}

public static class ProductionSystem
{
    public static void ProcessCityProduction(GameState state, GameConfig config, CityState city)
    {
        var yields = YieldSystem.CalculateCityYield(state, config, city);
        city.StoredProduction += yields.Production;
        var unitDef = config.Units.GetValueOrDefault(city.CurrentProductionId);
        if (unitDef != null && city.StoredProduction >= unitDef.Cost)
        {
            city.StoredProduction -= unitDef.Cost;
            state.Units.Add(new UnitState
            {
                Id = state.NextUnitId,
                OwnerPlayerId = city.OwnerPlayerId,
                UnitDefId = unitDef.Id,
                Coord = city.Coord,
                MovesRemaining = unitDef.Move
            });
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
        player.Science += scienceGain;
        if (!config.Techs.TryGetValue(player.CurrentResearchTechId, out var tech)) return;
        if (player.ResearchedTechs.Contains(tech.Id)) return;
        if (!CanResearch(player, tech)) return;
        if (player.Science >= tech.Cost)
        {
            player.Science -= tech.Cost;
            player.ResearchedTechs.Add(tech.Id);
            state.EventLog.Add($"{player.Name} researched {tech.Name}.");
            var next = config.Techs.Values.FirstOrDefault(t => !player.ResearchedTechs.Contains(t.Id) && CanResearch(player, t));
            if (next != null) player.CurrentResearchTechId = next.Id;
        }
    }
}

public static class CombatSystem
{
    public static bool ResolveAttack(GameState state, GameConfig config, UnitState attacker, UnitState defender)
    {
        if (attacker.Coord.DistanceTo(defender.Coord) > 1) return false;
        var attack = config.Units[attacker.UnitDefId].Strength;
        var defense = config.Units[defender.UnitDefId].Strength;
        defender.Health -= Math.Max(10, attack - defense + 20);
        attacker.MovesRemaining = 0;
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
        {
            AddVision(player.VisibleTiles, unit.Coord, 2, state.Grid);
        }
        foreach (var city in state.Cities.Where(c => c.OwnerPlayerId == playerId))
        {
            AddVision(player.VisibleTiles, city.Coord, 2, state.Grid);
        }
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
            var candidates = u.Coord.Neighbors()
                .Where(state.Grid.Contains)
                .Where(c => state.Grid.Get(c)?.TerrainId != "water")
                .ToList();

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
                    CurrentProductionId = "worker"
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
                    ProductionSystem.ProcessCityProduction(state, config, city);
                TechTree.ProcessResearch(state, config, current);
                processed.Add(current.Id);
            }

            var currentIndex = state.Players.FindIndex(p => p.Id == state.ActivePlayerId);
            var nextIndex = (currentIndex + 1) % state.Players.Count;
            state.ActivePlayerId = state.Players[nextIndex].Id;
            if (nextIndex == 0) state.Turn++;

            MovementSystem.RefreshMoves(state, config);
            FogOfWarSystem.UpdateVisibility(state, state.ActivePlayerId);

            var active = state.ActivePlayer;
            if (!active.IsAI) break;

            AISystem.RunTurn(state, config, active, rng);
        }

        state.Units.RemoveAll(u => u.Consumed);
        state.VictoryWinner = VictorySystem.CheckVictory(state, config);
    }
}

public static class SaveLoadSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(string path, GameState state)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static GameState Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameState>(json, JsonOptions)!;
    }
}
