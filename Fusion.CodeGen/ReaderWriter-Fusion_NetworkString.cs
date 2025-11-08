using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[WeaverGenerated]
internal struct ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E : IElementReaderWriter<NetworkString<_32>>
{
	[WeaverGenerated]
	public static IElementReaderWriter<NetworkString<_32>> Instance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe NetworkString<_32> Read(byte* data, int index)
	{
		return *(NetworkString<_32>*)(data + index * 132);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe ref NetworkString<_32> ReadRef(byte* data, int index)
	{
		return ref *(NetworkString<_32>*)(data + index * 132);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe void Write(byte* data, int index, NetworkString<_32> val)
	{
		*(NetworkString<_32>*)(data + index * 132) = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementWordCount()
	{
		return 33;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementHashCode(NetworkString<_32> val)
	{
		return val.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public static IElementReaderWriter<NetworkString<_32>> GetInstance()
	{
		if (Instance == null)
		{
			Instance = default(ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E);
		}
		return Instance;
	}
}
[StructLayout(LayoutKind.Sequential, Size = 1)]
[WeaverGenerated]
internal struct ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__128_003E : IElementReaderWriter<NetworkString<_128>>
{
	[WeaverGenerated]
	public static IElementReaderWriter<NetworkString<_128>> Instance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe NetworkString<_128> Read(byte* data, int index)
	{
		return *(NetworkString<_128>*)(data + index * 516);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe ref NetworkString<_128> ReadRef(byte* data, int index)
	{
		return ref *(NetworkString<_128>*)(data + index * 516);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe void Write(byte* data, int index, NetworkString<_128> val)
	{
		*(NetworkString<_128>*)(data + index * 516) = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementWordCount()
	{
		return 129;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementHashCode(NetworkString<_128> val)
	{
		return val.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public static IElementReaderWriter<NetworkString<_128>> GetInstance()
	{
		if (Instance == null)
		{
			Instance = default(ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__128_003E);
		}
		return Instance;
	}
}
