using System.Collections.Concurrent;

namespace GameOfLife;

public class Grid(Bounds bounds)
{
	public Grid(int width, int height)
		: this(new Bounds(width, height)) { }

	public Bounds Bounds { get; } = bounds;

	readonly ConcurrentDictionary<Point2D, Cell> _cells = new();

	public Cell GetCell(Point2D location)
		=> _cells.GetOrAdd(location, p => new Cell(this, p));

	public Cell GetCell(int x, int y)
		=> GetCell(new(x, y));

	public static int CountLivingNeighbors(bool[,] grid, Bounds bounds, Point2D location)
	{
		int count = 0;
		var (width, height) = bounds;
		var (x, y) = location;

		int left = x - 1;
		int right = x + 1;
		int top = y - 1;
		int bottom = y + 1;

		bool leftIn = left >= 0;
		bool rightIn = right < width;
		bool topIn = top >= 0;
		bool bottomIn = bottom < height;

		if (leftIn)
		{
			if (topIn && grid[left, top]) count++;
			if (grid[left, y]) count++;
			if (bottomIn && grid[left, bottom]) count++;
		}

		if (topIn && grid[x, top]) count++;
		if (bottomIn && grid[x, bottom]) count++;

		if (rightIn)
		{
			if (topIn && grid[left, top]) count++;
			if (grid[left, y]) count++;
			if (bottomIn && grid[left, bottom]) count++;
		}

		return count;
	}

	public static int CountLivingNeighbors(bool[,] grid, Point2D location)
		=> CountLivingNeighbors(grid, new(grid.GetLength(0), grid.GetLength(1)), location);

	public Task ScrambleAsync(Random random, int density, CancellationToken cancellationToken = default)
	{
		var (width, height) = Bounds;
		return Parallel.ForAsync(0, height, cancellationToken, (row, ct) =>
		{
			for (int col = 0; col < width; col++)
			{
				ct.ThrowIfCancellationRequested();
				var cell = GetCell(col, row);
				cell.Value = random.Next(density) == 0;
			}

			return ValueTask.CompletedTask;
		});
	}

	/* Any live cell with fewer than two live neighbours dies, as if by underpopulation.
     * Any live cell with two or three live neighbours lives on to the next generation.
     * Any live cell with more than three live neighbours dies, as if by overpopulation.
     * Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction. */

	public Task NextAsync(Grid target, CancellationToken cancellationToken = default)
	{
		var (width, height) = Bounds;
		return Parallel.ForAsync(0, height, cancellationToken, (row, ct) =>
		{
			for (int col = 0; col < width; col++)
			{
				ct.ThrowIfCancellationRequested();
				var cell = GetCell(col, row);
				var count = cell.CountLivingNeighbors();
				target.GetCell(col, row).Value = cell ? count is 2 or 3 : count is 3;
			}

			return ValueTask.CompletedTask;
		});
	}

	public IEnumerable<Cell> GetRow(int y)
	{
		int width = Bounds.Width;
		for (var col = 0; col < width; col++)
			yield return GetCell(col, y);
	}

	public IEnumerable<IEnumerable<Cell>> Rows
	{
		get
		{
			int height = Bounds.Height;
			for (var row = 0; row < height; row++)
				yield return GetRow(row);
		}
	}
}
