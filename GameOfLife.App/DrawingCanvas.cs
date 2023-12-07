using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace GameOfLife.App;

public class DrawingCanvas : Control
{
	private const int BoxSize = 5;
	private const int WidthInBoxes = 160;
	private const int HeightInBoxes = 80;
	private const int Delay = 100;
	private const int ScrambleDensity = 3;

	private static readonly SolidColorBrush Background = new(Color.FromArgb(255, 16, 16, 16));
	private static readonly SolidColorBrush Foreground = new(Color.FromArgb(255, 200, 200, 200));
	private readonly DispatcherTimer _timer;
	private Grid grid;
	private Grid grid2;
	Task _next;

	public DrawingCanvas()
	{
		var bounds = new Bounds(WidthInBoxes, HeightInBoxes);

		grid = new Grid(bounds);
		grid2 = new Grid(bounds);

		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(Delay)
		};

		_timer.Tick += UpdateDrawing;

		_next = grid.ScrambleAsync(ScrambleDensity)
			.ContinueWith(
				_ => _timer.Start(),
				TaskContinuationOptions.OnlyOnRanToCompletion);
	}

	private void UpdateDrawing(object? sender, EventArgs e)
	{
		InvalidateVisual(); // Invalidate to redraw
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		_next.Wait();
		_next = grid.NextAsync(grid2)
			.ContinueWith(
				_ => (grid2, grid) = (grid, grid2),
				TaskContinuationOptions.OnlyOnRanToCompletion);

		Draw(context);
	}

	private void Draw(DrawingContext context)
	{
		context.FillRectangle(Background, new Rect(0, 0, WidthInBoxes * BoxSize, HeightInBoxes * BoxSize));
		foreach (var row in grid.Rows)
		{
			foreach (var cell in row)
			{
				var (x, y) = cell.Location;
				if(cell)
				{
					var rect = new Rect(x * BoxSize, y * BoxSize, BoxSize, BoxSize);
					context.FillRectangle(Foreground, rect);
				}
			}
		}
	}
}
