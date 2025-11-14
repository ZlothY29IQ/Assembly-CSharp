using System;
using UnityEngine;

[Serializable]
public struct SerializableBSPNode
{
	public enum Axis
	{
		X,
		Y,
		Z,
		MatrixChain,
		MatrixFinal,
		Zone
	}

	[SerializeField]
	public Axis axis;

	[SerializeField]
	public float splitValue;

	[SerializeField]
	public short leftChildIndex;

	[SerializeField]
	public short rightChildIndex;

	public int matrixIndex => leftChildIndex;

	public int outsideChildIndex => rightChildIndex;

	public int zoneIndex => leftChildIndex;
}
