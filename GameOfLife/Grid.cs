using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

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

	public Task ScrambleAsync(int density, Random? random = null, CancellationToken cancellationToken = default)
	{
		random ??= new();
		var (width, height) = Bounds;
		return Parallel.ForAsync(0, height, cancellationToken, async (row, ct) =>
		{
			for (int col = 0; col < width; col++)
			{
				ct.ThrowIfCancellationRequested();
				var cell = GetCell(col, row);
				bool value;
				lock(random)
					value = random.Next(density) == 0;
				cell.Value = value;
				await Task.Yield();
			}
		});
	}

	public Task ScrambleQuadSymmetricAsync(int density, Random? random = null, CancellationToken cancellationToken = default)
	{
		random ??= new();
		var (width, height) = Bounds;
		var halfWidth = width / 2;
		var halfHeight = height / 2;
		var lastX = width - 1;
		var lastY = height - 1;

		return Parallel.ForAsync(0, halfHeight, cancellationToken, async (row, ct) =>
		{
			for (int col = 0; col < halfWidth; col++)
			{
				ct.ThrowIfCancellationRequested();

				var rCol = lastX - col;
				var cellTL = GetCell(col, row);
				var cellTR = GetCell(rCol, row);
				var bRow = lastY - row;
				var cellBL = GetCell(col, bRow);
				var cellBR = GetCell(rCol, bRow);
				bool value;
				lock (random)
					value = random.Next(density) == 0;
				cellTL.Value = cellTR.Value = cellBL.Value = cellBR.Value = value;
				await Task.Yield();
			}
		});
	}


	public Task ScrambleRadialSymmetricAsync(int density, Random? random = null, CancellationToken cancellationToken = default)
	{
		random ??= new();
		var (width, height) = Bounds;
		var halfWidth = width / 2;
		var halfHeight = height / 2;
		var lastX = width - 1;
		var lastY = height - 1;

		return Parallel.ForAsync(0, halfHeight, cancellationToken, async (row, ct) =>
		{
			for (int col = 0; col < width; col++)
			{
				ct.ThrowIfCancellationRequested();

				var rCol = lastX - col;
				var bRow = lastY - row;
				var cellT = GetCell(col, row);
				var cellB = GetCell(rCol, bRow);
				bool value;
				lock (random)
					value = random.Next(density) == 0;
				cellT.Value = cellB.Value = value;
				await Task.Yield();
			}
		});
	}

	/* Any live cell with fewer than two live neighbours dies, as if by underpopulation.
     * Any live cell with two or three live neighbours lives on to the next generation.
     * Any live cell with more than three live neighbours dies, as if by overpopulation.
     * Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction. */

	public Task NextAsync(Grid target, CancellationToken cancellationToken = default)
		// Guarantee deferred execution.
		=> Task.Run(async () =>
		{
			var (width, height) = Bounds;
			await Parallel.ForAsync(0, height, cancellationToken, async (row, ct) =>
			{
				for (int col = 0; col < width; col++)
				{
					ct.ThrowIfCancellationRequested();
					var cell = GetCell(col, row);
					var count = cell.CountLivingNeighbors();
					target.GetCell(col, row).Value = cell ? count is 2 or 3 : count is 3;
					await Task.Yield();
				}
			});
		}, cancellationToken);

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
