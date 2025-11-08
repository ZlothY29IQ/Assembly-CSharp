using System;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class AOEReceiver : MonoBehaviour
{
	[Serializable]
	public struct AOEContext
	{
		public Vector3 origin;

		public float radius;

		public GameObject instigator;

		public float baseStrength;

		public float finalStrength;

		public float distance;

		public float normalizedDistance;
	}

	public AOEContextEvent OnAOEReceived;

	[Tooltip("Quick toggle to disable receiving without disabling the GameObject.")]
	[SerializeField]
	private bool enabledForAOE = true;

	public void ReceiveAOE(in AOEContext AOEContext)
	{
		if (enabledForAOE)
		{
			OnAOEReceived?.Invoke(AOEContext);
		}
	}
}
