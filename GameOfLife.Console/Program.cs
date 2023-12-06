using GameOfLife;
using Spectre.Console;

var bounds = new Bounds(40, 25);
var grid = new GameOfLife.Grid(bounds);
var grid2 = new GameOfLife.Grid(bounds);
var random = new Random();
var canvas = new Canvas(grid.Bounds.Width, grid.Bounds.Height);
const int delay = 200;
const int density = 3;
await grid.ScrambleAsync(random, density);

await AnsiConsole.Live(canvas)
	.StartAsync(async context =>
	{
	loop:
		await RenderAsync(grid, canvas);
		context.Refresh();
		var next = grid.NextAsync(grid2);
		await Task.Delay(delay);
		await next;
		(grid2, grid) = (grid, grid2);
		goto loop;
	});

static Task RenderAsync(GameOfLife.Grid grid, Canvas canvas, CancellationToken cancellationToken = default)
	=> Parallel.ForEachAsync(grid.Rows, cancellationToken, (row, ct) =>
	{
		foreach (var cell in row)
		{
			ct.ThrowIfCancellationRequested();
			var (x, y) = cell.Location;
			canvas.SetPixel(x, y, cell ? Color.Grey : Color.Black);
		}

		return ValueTask.CompletedTask;
	});