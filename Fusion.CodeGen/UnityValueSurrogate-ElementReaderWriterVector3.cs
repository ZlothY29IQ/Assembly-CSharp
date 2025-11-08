using System;
using Fusion.Internal;
using UnityEngine;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityValueSurrogate_0040ElementReaderWriterVector3 : UnityValueSurrogate<Vector3, Fusion.ElementReaderWriterVector3>
{
	[WeaverGenerated]
	public Vector3 Data;

	[WeaverGenerated]
	public override Vector3 DataProperty
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
	public UnityValueSurrogate_0040ElementReaderWriterVector3()
	{
	}
}
