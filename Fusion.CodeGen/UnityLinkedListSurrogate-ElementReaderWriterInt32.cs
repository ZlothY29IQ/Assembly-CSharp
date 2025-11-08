using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityLinkedListSurrogate_0040ElementReaderWriterInt32 : UnityLinkedListSurrogate<int, Fusion.ElementReaderWriterInt32>
{
	[WeaverGenerated]
	public int[] Data = Array.Empty<int>();

	[WeaverGenerated]
	public override int[] DataProperty
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
	public UnityLinkedListSurrogate_0040ElementReaderWriterInt32()
	{
	}
}
