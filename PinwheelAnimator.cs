using UnityEngine;

public class PinwheelAnimator : MonoBehaviour
{
	public Transform spinnerTransform;

	[Tooltip("In revolutions per second.")]
	public float maxSpinSpeed = 4f;

	public float spinSpeedMultiplier = 5f;

	public float damping = 0.5f;

	private Vector3 oldPos;

	private float spinSpeed;

	protected void OnEnable()
	{
		oldPos = spinnerTransform.position;
		spinSpeed = 0f;
	}

	protected void LateUpdate()
	{
		Vector3 position = spinnerTransform.position;
		Vector3 forward = base.transform.forward;
		Vector3 vector = position - oldPos;
		float b = Mathf.Clamp(vector.magnitude / Time.deltaTime * Vector3.Dot(vector.normalized, forward) * spinSpeedMultiplier, 0f - maxSpinSpeed, maxSpinSpeed);
		spinSpeed = Mathf.Lerp(spinSpeed, b, Time.deltaTime * damping);
		spinnerTransform.Rotate(Vector3.forward, spinSpeed * 360f * Time.deltaTime);
		oldPos = position;
	}
}
