using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace Cosmetics;

public class CosmeticFlickReactor : MonoBehaviour
{
	private enum AxisMode
	{
		X,
		Y,
		Z,
		CustomForward
	}

	[Header("Axis")]
	[Tooltip("Which single axis/direction to use for flick detection.\n- X/Y/Z use the axes defined by the Space settings below (Local vs World).\n- CustomForward uses axisReference.forward (ignores Space).")]
	[SerializeField]
	private AxisMode axisMode = AxisMode.Z;

	[Tooltip("Used only when AxisMode = CustomForward. The forward/back of this transform defines the direction.")]
	[SerializeField]
	private Transform axisReference;

	[Header("Space")]
	[Tooltip("If enabled, X/Y/Z use world axes, otherwise local axes.\nUse Local for movement relative to the object’s facing.\nUse World for absolute directions independent of rotation.")]
	[SerializeField]
	private bool useWorldAxes;

	[Tooltip("Optional transform to define a custom world frame for X/Y/Z.\nIf assigned and Space is World, this transform’s Right/Up/Forward act as the world axes.\nIf not assigned, Unity’s global axes are used.")]
	[SerializeField]
	private Transform worldSpace;

	[Header("Velocity Source")]
	[Tooltip("Primary velocity tracker.")]
	[SerializeField]
	private SimpleSpeedTracker speedTracker;

	[Tooltip("Fallback velocity source if speedTracker is missing.")]
	[SerializeField]
	private Rigidbody rb;

	[Header("Thresholds")]
	[Tooltip("Minimum absolute signed speed along the chosen axis required to consider a object movement (m/s).")]
	[SerializeField]
	private float minSpeedThreshold = 2f;

	[Tooltip("Optional upper bound for mapping flick strength to 0–1.\nSet <= 0 to disable onFlickStrength.")]
	[SerializeField]
	private float maxSpeedThreshold;

	[Tooltip("How much back-and-forth reversal is required to register a flick.\nExample: 2.5 means => +1.3 then -1.2 within the window (|1.3| + |1.2| = 2.5).")]
	[SerializeField]
	private float directionChangeRequired = 2f;

	[Header("Timing")]
	[Tooltip("Max time allowed between the initial peak and its reversal (seconds).")]
	[SerializeField]
	private float flickWindowSeconds = 0.2f;

	[Tooltip("Buffer time after a successful flick during which no new flicks are allowed (seconds).")]
	[SerializeField]
	private float retriggerBufferSeconds = 0.15f;

	[Header("Events")]
	public UnityEvent OnFlickShared;

	public UnityEvent OnFlickLocal;

	public UnityEvent<float> onFlickStrength;

	private Vector3 lastPosition;

	private bool hasLastPosition;

	private float lastPeakSpeed;

	private float lastPeakTime = -999f;

	private int lastPeakSign;

	private float blockUntilTime;

	private VRRig rig;

	private bool isLocal;

	private void Reset()
	{
		if (speedTracker == null)
		{
			speedTracker = GetComponent<SimpleSpeedTracker>();
		}
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
	}

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		if (rig == null && base.gameObject.GetComponentInParent<GTPlayer>() != null)
		{
			rig = GorillaTagger.Instance.offlineVRRig;
		}
		isLocal = rig != null && rig.isLocal;
		ResetState();
		blockUntilTime = 0f;
		hasLastPosition = false;
	}

	private void Update()
	{
		Vector3 axis = ResolveAxisDirection();
		if (axis.sqrMagnitude < 0.5f)
		{
			return;
		}
		float signedSpeedAlong = GetSignedSpeedAlong(axis);
		if (Mathf.Abs(signedSpeedAlong) >= minSpeedThreshold)
		{
			int num = ((signedSpeedAlong > 0f) ? 1 : (-1));
			if (num == lastPeakSign && !(Mathf.Abs(signedSpeedAlong) > Mathf.Abs(lastPeakSpeed)))
			{
				return;
			}
			if (lastPeakSign != 0 && num == -lastPeakSign)
			{
				float num2 = Time.time - lastPeakTime;
				float num3 = Mathf.Abs(lastPeakSpeed) + Mathf.Abs(signedSpeedAlong);
				bool flag = num2 <= flickWindowSeconds;
				bool flag2 = num3 >= directionChangeRequired;
				bool flag3 = Time.time >= blockUntilTime;
				if (flag && flag2 && flag3)
				{
					FireEvents(Mathf.Abs(signedSpeedAlong));
					blockUntilTime = Time.time + retriggerBufferSeconds;
					ResetState();
				}
				else
				{
					lastPeakSign = num;
					lastPeakSpeed = signedSpeedAlong;
					lastPeakTime = Time.time;
				}
			}
			else
			{
				lastPeakSign = num;
				lastPeakSpeed = signedSpeedAlong;
				lastPeakTime = Time.time;
			}
		}
		else if (Time.time - lastPeakTime > flickWindowSeconds)
		{
			ResetState();
		}
	}

	private Vector3 ResolveAxisDirection()
	{
		switch (axisMode)
		{
		case AxisMode.X:
			if (!useWorldAxes)
			{
				return base.transform.right;
			}
			if (!(worldSpace != null))
			{
				return Vector3.right;
			}
			return worldSpace.right;
		case AxisMode.Y:
			if (!useWorldAxes)
			{
				return base.transform.up;
			}
			if (!(worldSpace != null))
			{
				return Vector3.up;
			}
			return worldSpace.up;
		case AxisMode.Z:
			if (!useWorldAxes)
			{
				return base.transform.forward;
			}
			if (!(worldSpace != null))
			{
				return Vector3.forward;
			}
			return worldSpace.forward;
		case AxisMode.CustomForward:
			if (!(axisReference != null))
			{
				return Vector3.zero;
			}
			return axisReference.forward;
		default:
			return Vector3.zero;
		}
	}

	private float GetSignedSpeedAlong(Vector3 axis)
	{
		Vector3 lhs;
		if (speedTracker != null)
		{
			lhs = speedTracker.GetWorldVelocity();
		}
		else if (rb != null)
		{
			lhs = rb.linearVelocity;
		}
		else
		{
			if (!hasLastPosition)
			{
				lastPosition = base.transform.position;
				hasLastPosition = true;
				return 0f;
			}
			Vector3 vector = base.transform.position - lastPosition;
			float num = ((Time.deltaTime > Mathf.Epsilon) ? (1f / Time.deltaTime) : 0f);
			lhs = vector * num;
			lastPosition = base.transform.position;
		}
		return Vector3.Dot(lhs, axis.normalized);
	}

	private void FireEvents(float currentAbsSpeed)
	{
		if (isLocal)
		{
			OnFlickLocal?.Invoke();
		}
		OnFlickShared?.Invoke();
		if (maxSpeedThreshold > 0f)
		{
			float value = Mathf.InverseLerp(minSpeedThreshold, maxSpeedThreshold, currentAbsSpeed);
			onFlickStrength?.Invoke(Mathf.Clamp01(value));
		}
	}

	private void ResetState()
	{
		lastPeakSign = 0;
		lastPeakSpeed = 0f;
		lastPeakTime = -9999f;
	}
}
