using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameOfLife.App;

public class DrawingCanvas : Control
{
    private const int BoxSize = 7;
    private const int BoxSizePlus = BoxSize + 2;
    private const int WidthInBoxes = 160;
    private const int HeightInBoxes = 80;
    private const int Delay = 100;
    private const int ScrambleDensity = 3;

    private static readonly SolidColorBrush Background = new(Color.FromArgb(255, 16, 16, 16));
    private static readonly SolidColorBrush Foreground = new(Color.FromArgb(255, 200, 200, 200));
    private readonly DispatcherTimer _timer;
    private Grid _grid;
    private Grid _grid2;
    Task _next;

    readonly ConcurrentQueue<Point2D> _pointQueue = new();

    readonly ConcurrentQueue<List<Rect>> _pool = new();
    readonly ConcurrentQueue<List<Rect>> _rectQueue = new();

    public DrawingCanvas()
    {
        var bounds = new Bounds(WidthInBoxes, HeightInBoxes);

        _grid = new Grid(bounds);
        _grid2 = new Grid(bounds);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Delay)
        };

        _timer.Tick += UpdateDrawing;

        _next = _grid.ScrambleRadialSymmetricAsync(ScrambleDensity)
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
            var c = _grid.GetCell(p);
            c.SetValueIfInBounds(true);
            c.GetOffset(0, 1).SetValueIfInBounds(true);
            c.GetOffset(1, 0).SetValueIfInBounds(true);
            c.GetOffset(-1, 0).SetValueIfInBounds(true);
            c.GetOffset(0, -1).SetValueIfInBounds(true);
        }

        List<Rect> rects = _pool.TryDequeue(out var r) ? r : [];
        foreach (var row in _grid.Rows)
        {
            foreach (var cell in row)
            {
                var (x, y) = cell.Location;
                if (cell)
                {
                    var rect = new Rect(x * BoxSize - 1, y * BoxSize - 1, BoxSizePlus, BoxSizePlus);
                    rects.Add(rect);
                    //context.FillRectangle(Foreground, rect);
                }
            }
        }
        _rectQueue.Enqueue(rects);

        _next = _grid.NextAsync(_grid2)
            .ContinueWith(
                _ => (_grid2, _grid) = (_grid, _grid2),
                TaskContinuationOptions.OnlyOnRanToCompletion);

        InvalidateVisual(); // Invalidate to redraw
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Give the draw phase max CPU.
        Draw(context);
    }

    private void Draw(DrawingContext context)
    {
        List<Rect>? rects = null;
        while (_rectQueue.TryDequeue(out var r)) rects = r; // Skip any missed frames.
        if (rects is null) return;

        context.FillRectangle(Background, new Rect(0, 0, WidthInBoxes * BoxSize, HeightInBoxes * BoxSize));
        foreach (var rect in rects)
        {
            context.FillRectangle(Foreground, rect, 2);
        }

        rects.Clear();
        _pool.Enqueue(rects);
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
        if (_pressed == 1) AddPointerEvent(sender, e);
    }

    private void PointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
    {
        Interlocked.CompareExchange(ref _pressed, 0, 1);
        AddPointerEvent(sender, e);
    }
}
