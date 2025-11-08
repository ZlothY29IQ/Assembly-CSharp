using System;
using Fusion.Internal;

namespace Fusion.CodeGen;

[Serializable]
[WeaverGenerated]
internal class UnityDictionarySurrogate_0040ElementReaderWriterInt32_0040ElementReaderWriterInt32 : UnityDictionarySurrogate<int, Fusion.ElementReaderWriterInt32, int, Fusion.ElementReaderWriterInt32>
{
	[WeaverGenerated]
	public SerializableDictionary<int, int> Data = SerializableDictionary.Create<int, int>();

	[WeaverGenerated]
	public override SerializableDictionary<int, int> DataProperty
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
	public UnityDictionarySurrogate_0040ElementReaderWriterInt32_0040ElementReaderWriterInt32()
	{
	}
}
