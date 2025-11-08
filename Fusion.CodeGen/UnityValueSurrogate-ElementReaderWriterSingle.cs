using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityValueSurrogate_0040ElementReaderWriterSingle : UnityValueSurrogate<float, Fusion.ElementReaderWriterSingle>
{
	[WeaverGenerated]
	public float Data;

	[WeaverGenerated]
	public override float DataProperty
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
	public UnityValueSurrogate_0040ElementReaderWriterSingle()
	{
	}
}
