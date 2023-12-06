using System.Runtime.CompilerServices;

namespace GameOfLife;

public readonly record struct Bounds(int Width, int Height)
{
	public static implicit operator Bounds((int Width, int Height) b)
		=> new(b.Width, b.Height);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInBoundsX(int x)
		=> x >= 0 && x < Width;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInBoundsY(int y)
		=> y >= 0 && y < Height;

	public bool IsInBounds(int x, int y)
		=> IsInBoundsX(x) && IsInBoundsY(y);

	public bool IsInBounds(Point2D location)
		=> IsInBoundsX(location.X) && IsInBoundsY(location.Y);
}
