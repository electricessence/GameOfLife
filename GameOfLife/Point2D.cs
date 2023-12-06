namespace GameOfLife;

public readonly record struct Point2D(int X, int Y)
{
	public static implicit operator Point2D((int X, int Y) b)
		=> new(b.X, b.Y);

	public Point2D Offset(int x, int y)
		=> new(X + x, Y + y);

	public static Point2D operator +(Point2D a, Point2D b)
		=> new(a.X + b.X, a.Y + b.Y);
}
