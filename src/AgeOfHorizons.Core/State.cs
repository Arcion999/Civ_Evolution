using System;
using System.Collections.Generic;
using System.Linq;

namespace AgeOfHorizons.Core;

public class TileState
{
    public HexCoord Coord { get; set; }
    public string TerrainId { get; set; } = "plains";
    public string? ResourceId { get; set; }
    public int? OwnerPlayerId { get; set; }
    public int? CityId { get; set; }
}

public class UnitState
{
    public int Id { get; set; }
    public int OwnerPlayerId { get; set; }
    public string UnitDefId { get; set; } = "scout";
    public HexCoord Coord { get; set; }
    public int MovesRemaining { get; set; }
    public int Health { get; set; } = 100;
    public bool Consumed { get; set; }
}

public class CityState
{
    public int Id { get; set; }
    public int OwnerPlayerId { get; set; }
    public string Name { get; set; } = "New City";
    public HexCoord Coord { get; set; }
    public int StoredProduction { get; set; }
    public string CurrentProductionId { get; set; } = "worker";
    public int Population { get; set; } = 1;
}

public class PlayerState
{
    public int Id { get; set; }
    public string Name { get; set; } = "Player";
    public string FactionId { get; set; } = "dawn_unity";
    public bool IsAI { get; set; }
    public int Science { get; set; }
    public string CurrentResearchTechId { get; set; } = "surveying";
    public HashSet<string> ResearchedTechs { get; set; } = new();
    public HashSet<string> VisibleTiles { get; set; } = new();
}

public class HexGrid
{
    public int Width { get; init; }
    public int Height { get; init; }
    public Dictionary<string, TileState> Tiles { get; init; } = new();

    public bool Contains(HexCoord c) => c.Q >= 0 && c.R >= 0 && c.Q < Width && c.R < Height;
    public TileState? Get(HexCoord c) => Tiles.GetValueOrDefault(c.ToString());
}

public class GameState
{
    public int Turn { get; set; } = 1;
    public int ActivePlayerId { get; set; }
    public HexGrid Grid { get; set; } = new();
    public List<PlayerState> Players { get; set; } = new();
    public List<UnitState> Units { get; set; } = new();
    public List<CityState> Cities { get; set; } = new();
    public EventLog EventLog { get; set; } = new();
    public string? VictoryWinner { get; set; }

    public PlayerState ActivePlayer => Players.First(p => p.Id == ActivePlayerId);
    public int NextUnitId => Units.Count == 0 ? 1 : Units.Max(u => u.Id) + 1;
    public int NextCityId => Cities.Count == 0 ? 1 : Cities.Max(c => c.Id) + 1;
}

public class EventLog
{
    public List<string> Entries { get; set; } = new();

    public void Add(string message)
    {
        Entries.Add($"T{DateTime.UtcNow:HH:mm:ss} {message}");
        if (Entries.Count > 100) Entries.RemoveAt(0);
    }
}
