using UnityEngine;
using UnityEngine.Serialization;

public class SpinParametricAnimation : MonoBehaviour
{
	[Tooltip("Axis to rotate around.")]
	public Vector3 axis = Vector3.up;

	[Tooltip("Whether rotation is in World Space or Local Space")]
	public bool WorldSpaceRotation = true;

	[FormerlySerializedAs("speed")]
	[Tooltip("Speed of rotation.")]
	public float revolutionsPerSecond = 0.25f;

	[Tooltip("Affects the progress of the animation over time.")]
	public AnimationCurve timeCurve;

	private float _animationProgress;

	private float _oldAngle;

	protected void OnEnable()
	{
		axis = axis.normalized;
	}

	protected void LateUpdate()
	{
		Transform transform = base.transform;
		_animationProgress = (_animationProgress + Time.deltaTime * revolutionsPerSecond) % 1f;
		float num = timeCurve.Evaluate(_animationProgress) * 360f;
		float angle = num - _oldAngle;
		_oldAngle = num;
		if (WorldSpaceRotation)
		{
			transform.rotation = Quaternion.AngleAxis(angle, axis) * transform.rotation;
		}
		else
		{
			transform.localRotation = Quaternion.AngleAxis(angle, axis) * transform.localRotation;
		}
	}
}
