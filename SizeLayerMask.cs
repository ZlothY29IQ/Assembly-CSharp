using System;
using UnityEngine;

[Serializable]
public class SizeLayerMask
{
	[SerializeField]
	private bool affectLayerA = true;

	[SerializeField]
	private bool affectLayerB = true;

	[SerializeField]
	private bool affectLayerC = true;

	[SerializeField]
	private bool affectLayerD = true;

	public int Mask
	{
		get
		{
			int num = 0;
			if (affectLayerA)
			{
				num |= 1;
			}
			if (affectLayerB)
			{
				num |= 2;
			}
			if (affectLayerC)
			{
				num |= 4;
			}
			if (affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}
}
