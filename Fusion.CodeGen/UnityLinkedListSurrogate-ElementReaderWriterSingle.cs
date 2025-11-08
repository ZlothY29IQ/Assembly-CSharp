using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityLinkedListSurrogate_0040ElementReaderWriterSingle : UnityLinkedListSurrogate<float, Fusion.ElementReaderWriterSingle>
{
	[WeaverGenerated]
	public float[] Data = Array.Empty<float>();

	[WeaverGenerated]
	public override float[] DataProperty
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
	public UnityLinkedListSurrogate_0040ElementReaderWriterSingle()
	{
	}
}
