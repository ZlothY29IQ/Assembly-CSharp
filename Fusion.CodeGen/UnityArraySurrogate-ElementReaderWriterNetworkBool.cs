using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityArraySurrogate_0040ElementReaderWriterNetworkBool : UnityArraySurrogate<NetworkBool, Fusion.ElementReaderWriterNetworkBool>
{
	[WeaverGenerated]
	public NetworkBool[] Data = Array.Empty<NetworkBool>();

	[WeaverGenerated]
	public override NetworkBool[] DataProperty
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
	public UnityArraySurrogate_0040ElementReaderWriterNetworkBool()
	{
	}
}
