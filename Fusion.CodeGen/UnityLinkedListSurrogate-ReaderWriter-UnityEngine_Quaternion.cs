using System;
using Fusion.Internal;
using UnityEngine;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityLinkedListSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion : UnityLinkedListSurrogate<Quaternion, ReaderWriter_0040UnityEngine_Quaternion>
{
	[WeaverGenerated]
	public Quaternion[] Data = Array.Empty<Quaternion>();

	[WeaverGenerated]
	public override Quaternion[] DataProperty
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
	public UnityLinkedListSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion()
	{
	}
}
