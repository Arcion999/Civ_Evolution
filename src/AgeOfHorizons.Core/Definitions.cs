using System.Collections.Generic;

namespace AgeOfHorizons.Core;

public record TerrainDef(string Id, string Name, int MoveCost, int Food, int Production, int Science, bool IsWater);
public record ResourceDef(string Id, string Name, int Food, int Production, int Science);
public record UnitDef(string Id, string Name, int Move, int Strength, bool CanFoundCity, int Cost);
public record BuildingDef(string Id, string Name, int Cost, int Food, int Production, int Science);
public record TechDef(string Id, string Name, int Cost, List<string> Prereqs, List<string> UnlocksUnits, List<string> UnlocksBuildings);
public record FactionDef(string Id, string Name, string ColorHex, string AiPersonality);
public record AiPersonalityDef(string Id, string Name, float ExpansionBias, float MilitaryBias, float ScienceBias);

public class GameConfig
{
    public Dictionary<string, TerrainDef> Terrains { get; init; } = new();
    public Dictionary<string, ResourceDef> Resources { get; init; } = new();
    public Dictionary<string, UnitDef> Units { get; init; } = new();
    public Dictionary<string, BuildingDef> Buildings { get; init; } = new();
    public Dictionary<string, TechDef> Techs { get; init; } = new();
    public Dictionary<string, FactionDef> Factions { get; init; } = new();
    public Dictionary<string, AiPersonalityDef> AiPersonalities { get; init; } = new();
    public VictorySettings Victory { get; init; } = new();
}

public class VictorySettings
{
    public int ScienceTechsRequired { get; init; } = 3;
    public int DominationCityCount { get; init; } = 3;
}
