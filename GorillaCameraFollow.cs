using GorillaLocomotion;
using Unity.Cinemachine;
using UnityEngine;

public class GorillaCameraFollow : MonoBehaviour
{
	public Transform playerHead;

	public GameObject cameraParent;

	public Vector3 headOffset;

	public Vector3 eulerRotationOffset;

	public CinemachineVirtualCamera cinemachineCamera;

	private Cinemachine3rdPersonFollow cinemachineFollow;

	private float baseCameraRadius = 0.2f;

	private float baseFollowDistance = 2f;

	private float baseVerticalArmLength = 0.4f;

	private Vector3 baseShoulderOffset = new Vector3(0.5f, -0.4f, 0f);

	private void Start()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			cameraParent.SetActive(value: false);
		}
		if (cinemachineCamera != null)
		{
			cinemachineFollow = cinemachineCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
			baseCameraRadius = cinemachineFollow.CameraRadius;
			baseFollowDistance = cinemachineFollow.CameraDistance;
			baseVerticalArmLength = cinemachineFollow.VerticalArmLength;
			baseShoulderOffset = cinemachineFollow.ShoulderOffset;
		}
	}

	private void LateUpdate()
	{
		if (cinemachineFollow != null)
		{
			float scale = GTPlayer.Instance.scale;
			cinemachineFollow.CameraRadius = baseCameraRadius * scale;
			cinemachineFollow.CameraDistance = baseFollowDistance * scale;
			cinemachineFollow.VerticalArmLength = baseVerticalArmLength * scale;
			cinemachineFollow.ShoulderOffset = baseShoulderOffset * scale;
		}
	}
}
