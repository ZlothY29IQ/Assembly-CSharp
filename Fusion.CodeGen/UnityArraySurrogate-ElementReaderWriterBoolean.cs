using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityArraySurrogate_0040ElementReaderWriterBoolean : UnityArraySurrogate<bool, global::ElementReaderWriterBoolean>
{
	[WeaverGenerated]
	public bool[] Data = Array.Empty<bool>();

	[WeaverGenerated]
	public override bool[] DataProperty
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
	public UnityArraySurrogate_0040ElementReaderWriterBoolean()
	{
	}
}
