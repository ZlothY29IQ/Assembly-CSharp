using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fusion.CodeGen;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[WeaverGenerated]
internal struct ReaderWriter_0040BatteryChargerState__FusionCrankData : IElementReaderWriter<BatteryChargerState.FusionCrankData>
{
	[WeaverGenerated]
	public static IElementReaderWriter<BatteryChargerState.FusionCrankData> Instance;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe BatteryChargerState.FusionCrankData Read(byte* data, int index)
	{
		return *(BatteryChargerState.FusionCrankData*)(data + index * 12);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe ref BatteryChargerState.FusionCrankData ReadRef(byte* data, int index)
	{
		return ref *(BatteryChargerState.FusionCrankData*)(data + index * 12);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public unsafe void Write(byte* data, int index, BatteryChargerState.FusionCrankData val)
	{
		*(BatteryChargerState.FusionCrankData*)(data + index * 12) = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementWordCount()
	{
		return 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public int GetElementHashCode(BatteryChargerState.FusionCrankData val)
	{
		return val.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[WeaverGenerated]
	public static IElementReaderWriter<BatteryChargerState.FusionCrankData> GetInstance()
	{
		if (Instance == null)
		{
			Instance = default(ReaderWriter_0040BatteryChargerState__FusionCrankData);
		}
		return Instance;
	}
}
