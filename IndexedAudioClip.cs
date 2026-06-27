using System;
using UnityEngine;

[Serializable]
public class IndexedAudioClip
{
	[SerializeField]
	private int intVal = 67;

	public static implicit operator int(IndexedAudioClip a)
	{
		return a.intVal;
	}

	public static implicit operator IndexedAudioClip(int a)
	{
		return new IndexedAudioClip(a);
	}

	public IndexedAudioClip(int a)
	{
		intVal = a;
	}
}
