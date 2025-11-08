using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[WeaverGenerated]
internal struct ReaderWriter_0040BarrelCannon__BarrelCannonState : IElementReaderWriter<BarrelCannon.BarrelCannonState>
{
	[WeaverGenerated]
	public static IElementReaderWriter<BarrelCannon.BarrelCannonState> Instance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe BarrelCannon.BarrelCannonState Read(byte* data, int index)
	{
		return *(BarrelCannon.BarrelCannonState*)(data + index * 4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe ref BarrelCannon.BarrelCannonState ReadRef(byte* data, int index)
	{
		return ref *(BarrelCannon.BarrelCannonState*)(data + index * 4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe void Write(byte* data, int index, BarrelCannon.BarrelCannonState val)
	{
		*(BarrelCannon.BarrelCannonState*)(data + index * 4) = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementWordCount()
	{
		return 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementHashCode(BarrelCannon.BarrelCannonState val)
	{
		return val.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public static IElementReaderWriter<BarrelCannon.BarrelCannonState> GetInstance()
	{
		if (Instance == null)
		{
			Instance = default(ReaderWriter_0040BarrelCannon__BarrelCannonState);
		}
		return Instance;
	}
}
