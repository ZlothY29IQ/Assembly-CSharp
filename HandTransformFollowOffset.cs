using System;
using GorillaLocomotion;
using UnityEngine;

[Serializable]
internal class HandTransformFollowOffset
{
	internal Transform followTransform;

	[SerializeField]
	private Transform[] targetTransforms;

	[SerializeField]
	internal Vector3 positionOffset;

	[SerializeField]
	internal Quaternion rotationOffset;

	private Vector3 position;

	private Quaternion rotation;

	internal void UpdatePositionRotation()
	{
		if (!(followTransform == null) && targetTransforms != null)
		{
			position = followTransform.position + followTransform.rotation * positionOffset * GTPlayer.Instance.scale;
			rotation = followTransform.rotation * rotationOffset;
			Transform[] array = targetTransforms;
			foreach (Transform obj in array)
			{
				obj.position = position;
				obj.rotation = rotation;
			}
		}
	}
}
