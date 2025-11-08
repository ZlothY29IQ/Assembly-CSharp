using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityArraySurrogate_0040ElementReaderWriterSingle : UnityArraySurrogate<float, Fusion.ElementReaderWriterSingle>
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
	public UnityArraySurrogate_0040ElementReaderWriterSingle()
	{
	}
}
