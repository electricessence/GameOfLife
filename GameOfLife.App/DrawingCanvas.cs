using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading;
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

	readonly ConcurrentQueue<Point2D> _pointQueue = new();

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

		_next = grid.ScrambleRadialSymmetricAsync(ScrambleDensity)
			.ContinueWith(
				_ => _timer.Start(),
				TaskContinuationOptions.OnlyOnRanToCompletion);

		this.PointerPressed += PointerPressedHandler;
		this.PointerReleased += PointerReleasedHandler;
		this.PointerMoved += PointerMovedHandler;
	}

	private void UpdateDrawing(object? sender, EventArgs e)
	{
		_next.Wait();

		while (_pointQueue.TryDequeue(out var p))
		{
			var c = grid.GetCell(p);
			c.SetValueIfInBounds(true);
			c.GetOffset(0, 1).SetValueIfInBounds(true);
			c.GetOffset(1, 0).SetValueIfInBounds(true);
			c.GetOffset(-1, 0).SetValueIfInBounds(true);
			c.GetOffset(0, -1).SetValueIfInBounds(true);
		}

		InvalidateVisual(); // Invalidate to redraw
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		// Give the draw phase max CPU.
		Draw(context);

		_next = grid.NextAsync(grid2)
			.ContinueWith(
				_ => (grid2, grid) = (grid, grid2),
				TaskContinuationOptions.OnlyOnRanToCompletion);
	}

	private void Draw(DrawingContext context)
	{
		context.FillRectangle(Background, new Rect(0, 0, WidthInBoxes * BoxSize, HeightInBoxes * BoxSize));
		foreach (var row in grid.Rows)
		{
			foreach (var cell in row)
			{
				var (x, y) = cell.Location;
				if (cell)
				{
					var rect = new Rect(x * BoxSize, y * BoxSize, BoxSize, BoxSize);
					context.FillRectangle(Foreground, rect);
				}
			}
		}
	}

	private int _pressed;

	private void AddPointerEvent(object? sender, PointerEventArgs e)
	{
		var point = e.GetCurrentPoint(sender as Control);
		int x = (int)(point.Position.X / BoxSize);
		int y = (int)(point.Position.Y / BoxSize);
		_pointQueue.Enqueue(new(x, y));
	}

	private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
	{
		Interlocked.CompareExchange(ref _pressed, 1, 0);
		AddPointerEvent(sender, e);
	}

	private void PointerMovedHandler(object? sender, PointerEventArgs e)
	{
		if(_pressed == 1) AddPointerEvent(sender, e);
	}

	private void PointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
	{
		Interlocked.CompareExchange(ref _pressed, 0, 1);
		AddPointerEvent(sender, e);
	}
}
