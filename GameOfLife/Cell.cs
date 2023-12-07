namespace GameOfLife;

public record Cell
{
	public Cell(Grid grid, Point2D location)
	{
		_grid = grid;
		Location = location;
		IsInBounds = grid.Bounds.IsInBounds(location);

		_localGrid = new Lazy<Cell[,]>(() =>
		{
			var lg = new Cell[3, 3];
			lg[0, 0] = grid.GetCell(location.Offset(-1, -1));
			lg[1, 0] = grid.GetCell(location.Offset(0, -1));
			lg[2, 0] = grid.GetCell(location.Offset(1, -1));
			lg[0, 1] = grid.GetCell(location.Offset(-1, 0));
			lg[1, 1] = this;
			lg[2, 1] = grid.GetCell(location.Offset(1, 0));
			lg[0, 2] = grid.GetCell(location.Offset(-1, 1));
			lg[1, 2] = grid.GetCell(location.Offset(0, 1));
			lg[2, 2] = grid.GetCell(location.Offset(1, 1));
			return lg;
		});
	}

	private readonly Grid _grid;

	private readonly Lazy<Cell[,]> _localGrid;

	public Point2D Location { get; }

	public bool IsInBounds { get; }

	public Cell GetOffset(int x, int y)
	{
		if (x < -1 || y < -1 || x > 1 || y > 1)
			return _grid.GetCell(Location.Offset(x, y));

		return _localGrid.Value[x + 1, y + 1];
	}

	public bool Value { get; set; }

	public int CountLivingNeighbors()
	{
		int count = 0;
		var lg = _localGrid.Value;
		if (lg[0, 0]) count++;
		if (lg[1, 0]) count++;
		if (lg[2, 0]) count++;
		if (lg[0, 1]) count++;
		if (lg[2, 1]) count++;
		if (lg[0, 2]) count++;
		if (lg[1, 2]) count++;
		if (lg[2, 2]) count++;
		return count;
	}

	public static implicit operator bool(Cell cell)
		=> cell.Value;
}
