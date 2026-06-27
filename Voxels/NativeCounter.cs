using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Voxels;

public struct NativeCounter : IDisposable
{
	private readonly Allocator _allocator;

	[NativeDisableUnsafePtrRestriction]
	private unsafe readonly int* _counter;

	public unsafe int Count
	{
		get
		{
			return *_counter;
		}
		set
		{
			*_counter = value;
		}
	}

	public unsafe NativeCounter(Allocator allocator)
	{
		_allocator = allocator;
		_counter = (int*)UnsafeUtility.Malloc(4L, 4, _allocator);
		Count = 0;
	}

	public unsafe int Increment()
	{
		return Interlocked.Increment(ref *_counter) - 1;
	}

	public unsafe void Dispose()
	{
		UnsafeUtility.Free(_counter, _allocator);
	}
}
