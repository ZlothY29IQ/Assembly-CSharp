using System;
using System.Collections.Generic;
using UnityEngine;

public class BetterBakerPositionOverrides : MonoBehaviour
{
	[Serializable]
	public struct OverridePosition
	{
		public GameObject go;

		public Transform bakingTransform;

		public Transform gameTransform;
	}

	public List<OverridePosition> overridePositions;
}
