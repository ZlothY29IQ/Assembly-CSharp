using System;
using Fusion.Internal;
using UnityEngine;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityValueSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion : UnityValueSurrogate<Quaternion, ReaderWriter_0040UnityEngine_Quaternion>
{
	[WeaverGenerated]
	public Quaternion Data;

	[WeaverGenerated]
	public override Quaternion DataProperty
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
	public UnityValueSurrogate_0040ReaderWriter_0040UnityEngine_Quaternion()
	{
	}
}
