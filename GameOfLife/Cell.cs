namespace GameOfLife;

public record Cell
{
	public Cell(Grid grid, Point2D location)
	{
		_grid = grid;
		Location = location;
		IsInBounds = grid.Bounds.IsInBounds(location);

		_localGrid = new Lazy<Cell>[3, 3];
		_localGrid[0, 0] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(-1, -1)));
		_localGrid[1, 0] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(0, -1)));
		_localGrid[2, 0] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(1, -1)));
		_localGrid[0, 1] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(-1, 0)));
		_localGrid[1, 1] = new Lazy<Cell>(this);
		_localGrid[2, 1] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(1, 0)));
		_localGrid[0, 2] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(-1, 1)));
		_localGrid[1, 2] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(0, 1)));
		_localGrid[2, 2] = new Lazy<Cell>(() => grid.GetCell(Location.Offset(1, 1)));
	}

	private readonly Grid _grid;

	private readonly Lazy<Cell>[,] _localGrid;

	public Point2D Location { get; }

	public bool IsInBounds { get; }

	public Cell GetOffset(int x, int y)
	{
		if (x < -1 || y < -1 || x > 1 || y > 1)
			return _grid.GetCell(Location.Offset(x, y));

		return _localGrid[x + 1, y + 1].Value;
	}

	public bool Value { get; set; }

	public int CountLivingNeighbors()
	{
		int count = 0;
		if (_localGrid[0, 0].Value) count++;
		if (_localGrid[1, 0].Value) count++;
		if (_localGrid[2, 0].Value) count++;
		if (_localGrid[0, 1].Value) count++;
		if (_localGrid[2, 1].Value) count++;
		if (_localGrid[0, 2].Value) count++;
		if (_localGrid[1, 2].Value) count++;
		if (_localGrid[2, 2].Value) count++;
		return count;
	}

	public static implicit operator bool(Cell cell)
		=> cell.Value;
}
