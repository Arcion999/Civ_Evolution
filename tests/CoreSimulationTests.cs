using System;
using System.IO;
using AgeOfHorizons.Core;
using Xunit;

public class CoreSimulationTests
{
    private static string ProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "project.godot")))
            current = current.Parent;

        return current?.FullName ?? throw new InvalidOperationException("Could not locate project root.");
    }

    [Fact]
    public void NewGame_GeneratesPlayersAndUnits()
    {
        var config = GameBootstrap.LoadConfig(ProjectRoot());
        var state = GameBootstrap.NewGame(config, 8, 8, 1, seed: 42);

        Assert.Equal(2, state.Players.Count);
        Assert.True(state.Units.Count >= 4);
        Assert.Equal(1, state.ActivePlayerId);
    }

    [Fact]
    public void MovementSystem_MovesWithinRange()
    {
        var config = GameBootstrap.LoadConfig(ProjectRoot());
        var state = GameBootstrap.NewGame(config, 8, 8, 0, seed: 7);
        var unit = state.Units[0];
        var target = unit.Coord;
        foreach (var n in unit.Coord.Neighbors())
        {
            if (state.Grid.Contains(n) && state.Grid.Get(n)!.TerrainId != "water") { target = n; break; }
        }

        var moved = MovementSystem.TryMoveUnit(state, config, unit, target);
        Assert.True(moved);
    }
}
