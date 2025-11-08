using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[WeaverGenerated]
internal struct ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus : IElementReaderWriter<GorillaPaintbrawlManager.PaintbrawlStatus>
{
	[WeaverGenerated]
	public static IElementReaderWriter<GorillaPaintbrawlManager.PaintbrawlStatus> Instance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe GorillaPaintbrawlManager.PaintbrawlStatus Read(byte* data, int index)
	{
		return *(GorillaPaintbrawlManager.PaintbrawlStatus*)(data + index * 4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe ref GorillaPaintbrawlManager.PaintbrawlStatus ReadRef(byte* data, int index)
	{
		return ref *(GorillaPaintbrawlManager.PaintbrawlStatus*)(data + index * 4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe void Write(byte* data, int index, GorillaPaintbrawlManager.PaintbrawlStatus val)
	{
		*(GorillaPaintbrawlManager.PaintbrawlStatus*)(data + index * 4) = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementWordCount()
	{
		return 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementHashCode(GorillaPaintbrawlManager.PaintbrawlStatus val)
	{
		return val.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public static IElementReaderWriter<GorillaPaintbrawlManager.PaintbrawlStatus> GetInstance()
	{
		if (Instance == null)
		{
			Instance = default(ReaderWriter_0040GorillaPaintbrawlManager__PaintbrawlStatus);
		}
		return Instance;
	}
}
