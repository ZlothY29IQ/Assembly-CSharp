using UnityEngine;

namespace GorillaTag.Rendering;

public class RendererCullerByTriggers : MonoBehaviour, IBuildValidation
{
	[Tooltip("These renderers will be enabled/disabled depending on if the main camera is the colliders.")]
	public Renderer[] renderers;

	public Collider[] colliders;

	private bool camWasTouching;

	private const float cameraRadiusSq = 0.010000001f;

	private Transform mainCameraTransform;

	protected void OnEnable()
	{
		camWasTouching = false;
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			if (renderer != null)
			{
				renderer.enabled = false;
			}
		}
		if (mainCameraTransform == null)
		{
			mainCameraTransform = Camera.main.transform;
		}
	}

	protected void LateUpdate()
	{
		if (mainCameraTransform == null)
		{
			mainCameraTransform = Camera.main.transform;
		}
		Vector3 position = mainCameraTransform.position;
		bool flag = false;
		Collider[] array = colliders;
		foreach (Collider collider in array)
		{
			if (!(collider == null) && (collider.ClosestPoint(position) - position).sqrMagnitude < 0.010000001f)
			{
				flag = true;
				break;
			}
		}
		if (camWasTouching == flag)
		{
			return;
		}
		camWasTouching = flag;
		Renderer[] array2 = renderers;
		foreach (Renderer renderer in array2)
		{
			if (renderer != null)
			{
				renderer.enabled = flag;
			}
		}
	}

	public bool BuildValidationCheck()
	{
		return true;
	}
}
