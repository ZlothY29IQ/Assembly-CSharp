using System;
using Fusion.Internal;
using UnityEngine;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityLinkedListSurrogate_0040ElementReaderWriterVector3 : UnityLinkedListSurrogate<Vector3, Fusion.ElementReaderWriterVector3>
{
	[WeaverGenerated]
	public Vector3[] Data = Array.Empty<Vector3>();

	[WeaverGenerated]
	public override Vector3[] DataProperty
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
	public UnityLinkedListSurrogate_0040ElementReaderWriterVector3()
	{
	}
}
