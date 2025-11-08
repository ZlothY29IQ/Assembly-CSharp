using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityArraySurrogate_0040ElementReaderWriterInt64 : UnityArraySurrogate<long, Fusion.ElementReaderWriterInt64>
{
	[WeaverGenerated]
	public long[] Data = Array.Empty<long>();

	[WeaverGenerated]
	public override long[] DataProperty
	{
		[WeaverGenerated]
		get
		{
			return Data;
		}
		[WeaverGenerated]
		set
		{
			Data = value;
		}
	}

	[WeaverGenerated]
	public UnityArraySurrogate_0040ElementReaderWriterInt64()
	{
	}
}
