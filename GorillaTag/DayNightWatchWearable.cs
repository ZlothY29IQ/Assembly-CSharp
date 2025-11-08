using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag;

public class DayNightWatchWearable : MonoBehaviour
{
	[Tooltip("The transform that will be rotated to indicate the current time.")]
	public Transform clockNeedle;

	[FormerlySerializedAs("dialRotationAxis")]
	[Tooltip("The axis that the needle will rotate around.")]
	public Vector3 needleRotationAxis = Vector3.right;

	private BetterDayNightManager dayNightManager;

	[DebugOption]
	private float rotationDegree;

	private string currentTimeOfDay;

	private Quaternion initialRotation;

	private void Start()
	{
		if (!dayNightManager)
		{
			dayNightManager = BetterDayNightManager.instance;
		}
		rotationDegree = 0f;
		if ((bool)clockNeedle)
		{
			initialRotation = clockNeedle.localRotation;
		}
	}

	private void Update()
	{
		currentTimeOfDay = dayNightManager.currentTimeOfDay;
		double currentTimeInSeconds = ((ITimeOfDaySystem)dayNightManager).currentTimeInSeconds;
		double totalTimeInSeconds = ((ITimeOfDaySystem)dayNightManager).totalTimeInSeconds;
		rotationDegree = (float)(360.0 * currentTimeInSeconds / totalTimeInSeconds);
		rotationDegree = Mathf.Floor(rotationDegree);
		if ((bool)clockNeedle)
		{
			clockNeedle.localRotation = initialRotation * Quaternion.AngleAxis(rotationDegree, needleRotationAxis);
		}
	}
}
