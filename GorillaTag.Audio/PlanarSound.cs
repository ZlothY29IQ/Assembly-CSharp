using UnityEngine;

namespace GorillaTag.Audio;

public class PlanarSound : MonoBehaviour
{
	private Transform cameraXform;

	private bool hasCamera;

	[SerializeField]
	private bool limitDistance;

	[SerializeField]
	private float maxDistance = 1f;

	protected void OnEnable()
	{
		if (Camera.main != null)
		{
			cameraXform = Camera.main.transform;
			hasCamera = true;
		}
	}

	protected void LateUpdate()
	{
		if (hasCamera)
		{
			Transform obj = base.transform;
			Vector3 localPosition = obj.parent.InverseTransformPoint(cameraXform.position);
			localPosition.y = 0f;
			if (limitDistance && localPosition.sqrMagnitude > maxDistance * maxDistance)
			{
				localPosition = localPosition.normalized * maxDistance;
			}
			obj.localPosition = localPosition;
		}
	}
}
