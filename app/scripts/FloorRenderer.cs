using Godot;
using IE.Controller;
using IE.Controller.Views;
using IE.Model.Diagnostics;

namespace IE.App;

public partial class FloorRenderer : Node2D
{
    public const int CellSize = 32;

    private IReadOnlyList<PlacedItemViewModel> _items = [];
    private int _floorWidth;
    private int _floorHeight;

    public void UpdateFloor(int floorWidth, int floorHeight, IReadOnlyList<PlacedItemViewModel> items)
    {
        _floorWidth = floorWidth;
        _floorHeight = floorHeight;
        _items = items;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int pixelWidth = _floorWidth * CellSize;
        int pixelHeight = _floorHeight * CellSize;

        DrawRect(new Rect2(0, 0, pixelWidth, pixelHeight), new Color(0.08f, 0.08f, 0.08f));

        Color gridColor = new(0.2f, 0.2f, 0.2f);
        for (int x = 0; x <= _floorWidth; x++)
        {
            DrawLine(new Vector2(x * CellSize, 0), new Vector2(x * CellSize, pixelHeight), gridColor);
        }
        for (int y = 0; y <= _floorHeight; y++)
        {
            DrawLine(new Vector2(0, y * CellSize), new Vector2(pixelWidth, y * CellSize), gridColor);
        }

        Color auColor = new(0.3f, 0.8f, 0.4f);
        foreach (PlacedItemViewModel item in _items)
        {
            Rect2 rect = new(item.X * CellSize, item.Y * CellSize, item.Width * CellSize, item.Height * CellSize);
            DrawRect(rect, auColor, false, 2.0f);

            Vector2 labelPos = new(rect.Position.X + 4, rect.Position.Y + 14);
            DrawString(ThemeDB.FallbackFont, labelPos, item.TypeLabel, HorizontalAlignment.Left, -1, 12, auColor);
        }

        Log.Trace(LogCategory.View, $"floor draw; grid={_floorWidth}x{_floorHeight}; items={_items.Count}");
    }
}
