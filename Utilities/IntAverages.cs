using System.Runtime.CompilerServices;

namespace Utilities;

public class IntAverages : AverageCalculator<int>
{
	public IntAverages(int sampleCount)
		: base(sampleCount)
	{
		Reset();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override int PlusEquals(int value, int samples)
	{
		return value + samples;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override int MinusEquals(int value, int samples)
	{
		return value - samples;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override int Divide(int value, int samples)
	{
		return value / samples;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override int Multiply(int value, int samples)
	{
		return value * samples;
	}
}
