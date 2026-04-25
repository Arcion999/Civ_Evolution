using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class GameSceneController : Node2D
{
    private MapView _mapView = null!;
    private TopBarView _topBar = null!;
    private SelectionPanelView _selection = null!;
    private EventLogView _eventLog = null!;
    private VictoryScreenView _victory = null!;

    public UnitState? SelectedUnit { get; set; }
    public HexCoord? SelectedTile { get; set; }

    public override void _Ready()
    {
        _mapView = GetNode<MapView>("MapView");
        _topBar = GetNode<TopBarView>("CanvasLayer/TopBarView");
        _selection = GetNode<SelectionPanelView>("CanvasLayer/SelectionPanelView");
        _eventLog = GetNode<EventLogView>("CanvasLayer/EventLogView");
        _victory = GetNode<VictoryScreenView>("CanvasLayer/VictoryScreenView");
        RefreshAll();
    }

    public void RefreshAll()
    {
        _mapView.Render(Main.State, this);
        _topBar.Render(Main.State);
        _selection.Render(this);
        _eventLog.Render(Main.State);
        _victory.Render(Main.State);
    }

    public void EndTurn()
    {
        TurnSystem.EndTurn(Main.State, Main.Config);
        RefreshAll();
    }

    public void SaveGame()
    {
        SaveLoadSystem.Save(ProjectSettings.GlobalizePath("user://savegame.json"), Main.State);
        Main.State.EventLog.Add("Game saved.");
        RefreshAll();
    }

    public void LoadGame()
    {
        var path = ProjectSettings.GlobalizePath("user://savegame.json");
        if (!System.IO.File.Exists(path)) return;
        Main.State = SaveLoadSystem.Load(path);
        RefreshAll();
    }

    public void FoundCity()
    {
        if (SelectedUnit == null) return;
        if (!Main.Config.Units[SelectedUnit.UnitDefId].CanFoundCity) return;

        Main.State.Cities.Add(new CityState
        {
            Id = Main.State.NextCityId,
            OwnerPlayerId = SelectedUnit.OwnerPlayerId,
            Name = $"Horizon {Main.State.NextCityId}",
            Coord = SelectedUnit.Coord,
            CurrentProductionId = "worker"
        });
        SelectedUnit.Consumed = true;
        Main.State.EventLog.Add("City founded.");
        RefreshAll();
    }

    public void SelectTile(HexCoord coord)
    {
        SelectedTile = coord;
        SelectedUnit = Main.State.Units.FirstOrDefault(u => !u.Consumed && u.Coord.Equals(coord) && u.OwnerPlayerId == Main.State.ActivePlayerId);
        RefreshAll();
    }

    public void MoveSelectedUnit(HexCoord coord)
    {
        if (SelectedUnit == null) return;
        if (MovementSystem.TryMoveUnit(Main.State, Main.Config, SelectedUnit, coord))
        {
            FogOfWarSystem.UpdateVisibility(Main.State, Main.State.ActivePlayerId);
            foreach (var enemy in Main.State.Units.Where(u => u.OwnerPlayerId != SelectedUnit.OwnerPlayerId && !u.Consumed).ToList())
            {
                if (enemy.Coord.DistanceTo(SelectedUnit.Coord) == 1)
                    CombatSystem.ResolveAttack(Main.State, Main.Config, SelectedUnit, enemy);
            }
        }
        RefreshAll();
    }
}
