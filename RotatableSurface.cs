using UnityEngine;

public class RotatableSurface : MonoBehaviour
{
	public ManipulatableSpinner spinner;

	public float rotationScale = 1f;

	private void LateUpdate()
	{
		float angle = spinner.angle;
		base.transform.localRotation = Quaternion.Euler(0f, angle * rotationScale, 0f);
	}
}
