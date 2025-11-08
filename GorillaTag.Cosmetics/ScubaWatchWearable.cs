using GorillaLocomotion;
using UnityEngine;

namespace GorillaTag.Cosmetics;

[ExecuteAlways]
public class ScubaWatchWearable : MonoBehaviour
{
	public bool onLeftHand;

	[Tooltip("The transform that will be rotated to indicate the current depth.")]
	public Transform dialNeedle;

	[Tooltip("If your rotation is not zeroed out then click the Auto button to use the current rotation as 0.")]
	public Quaternion initialDialRotation;

	[Tooltip("The range of depth values that the dial will rotate between.")]
	public Vector2 depthRange = new Vector2(0f, 20f);

	[Tooltip("The range of rotation values that the dial will rotate between.")]
	public Vector2 dialRotationRange = new Vector2(0f, 360f);

	[Tooltip("The axis that the dial will rotate around.")]
	public Vector3 dialRotationAxis = Vector3.right;

	[Tooltip("The current depth of the player.")]
	[DebugOption]
	private float currentDepth;

	protected void Update()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (onLeftHand)
		{
			if (instance.LeftHandWaterVolume != null)
			{
				currentDepth = Mathf.Max(0f - instance.LeftHandWaterSurface.surfacePlane.GetDistanceToPoint(instance.LastLeftHandPosition), 0f);
			}
			else
			{
				currentDepth = 0f;
			}
		}
		else if (instance.RightHandWaterVolume != null)
		{
			currentDepth = Mathf.Max(0f - instance.RightHandWaterSurface.surfacePlane.GetDistanceToPoint(instance.LastRightHandPosition), 0f);
		}
		else
		{
			currentDepth = 0f;
		}
		float t = (currentDepth - depthRange.x) / (depthRange.y - depthRange.x);
		float angle = Mathf.Lerp(dialRotationRange.x, dialRotationRange.y, t);
		dialNeedle.localRotation = initialDialRotation * Quaternion.AngleAxis(angle, dialRotationAxis);
	}
}
