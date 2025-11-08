using System;
using UnityEngine;

[Serializable]
internal class SoundIdRemapping
{
	[GorillaSoundLookup]
	[SerializeField]
	private int soundIn = 1;

	[GorillaSoundLookup]
	[SerializeField]
	private int soundOut = 2;

	public int SoundIn => soundIn;

	public int SoundOut => soundOut;
}
