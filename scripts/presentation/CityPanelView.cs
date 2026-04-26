using AgeOfHorizons.Core;
using Godot;
using System.Linq;

public partial class CityPanelView : RichTextLabel
{
    public void Render(GameState state, GameSceneController controller)
    {
        var city = controller.SelectedTile.HasValue ? state.Cities.FirstOrDefault(c => c.Coord.Equals(controller.SelectedTile.Value)) : null;
        if (city == null)
        {
            Text = "[b]City Panel[/b]\nSelect a city to inspect production, growth, and queue.";
            return;
        }

        var y = YieldSystem.CalculateCityYield(state, Main.Config, city);
        Text = $"[b]{city.Name}[/b] (Pop {city.Population})\nFood:{y.Food} Prod:{y.Production} Sci:{y.Science} Gold:{y.Gold}\nStored Food: {city.StoredFood} | Stored Prod: {city.StoredProduction}\nQueue: {(city.ProductionQueue.Count == 0 ? city.CurrentProductionId : string.Join(", ", city.ProductionQueue))}";
    }
}
