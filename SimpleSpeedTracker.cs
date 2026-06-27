using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class SimpleSpeedTracker : MonoBehaviour, IGorillaSliceableSimple
{
	private enum AxisFilter
	{
		None,
		X,
		Y,
		Z
	}

	[Header("Settings")]
	[Tooltip("Transform whose movement speed is tracked. If left empty, uses this object’s transform.")]
	[SerializeField]
	private Transform target;

	[Tooltip("If enabled, speed and direction calculations use world (global) space, otherwise local space.\nUse Local Space when you want speed relative to the object’s facing direction (e.g., how fast a sword swings forward)")]
	[SerializeField]
	private bool useWorldAxes;

	[Tooltip("Optional transform defining a custom world reference.\nIf set, that transform’s Right/Up/Forward axes are treated as world axes.\nIf left empty, Unity’s global world axes are used.")]
	[SerializeField]
	private Transform worldSpace;

	[Tooltip("If true, uses raw instantaneous speed without smoothing.\nIf false, smooths speed using the Responsiveness setting below.")]
	[SerializeField]
	private bool useRawSpeed;

	[SerializeField]
	private float responsiveness = 10f;

	[SerializeField]
	private AnimationCurve postprocessCurve = AnimationCurve.Linear(0f, 0f, 10f, 10f);

	[Header("Axis Filter")]
	[Tooltip("Optionally restrict speed tracking to a single axis.\nWhen set, speed is signed: positive = moving along the axis, negative = moving against it.\nAxes are resolved using the Space settings above (Local vs World).")]
	[SerializeField]
	private AxisFilter trackAxis;

	[Header("Property Output")]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[Header("Events")]
	[Tooltip("Speed threshold used to trigger the Above/Below events.\nWhen an axis filter is set, this compares against absolute speed on that axis.")]
	[SerializeField]
	private float eventThreshold = 1f;

	public UnityEvent<float> onSpeedUpdated;

	public UnityEvent onSpeedAboveThreshold;

	public UnityEvent onSpeedBelowThreshold;

	[Tooltip("Signed speed along the positive axis direction required to fire onAbovePositiveThreshold / onBelowPositiveThreshold.")]
	[SerializeField]
	private float positiveThreshold = 2f;

	public UnityEvent onAbovePositiveThreshold;

	public UnityEvent onBelowPositiveThreshold;

	[Tooltip("Signed speed threshold for the negative axis direction. Enter as a negative number.\nFires onAboveNegativeThreshold / onBelowNegativeThreshold when signed speed crosses this value.")]
	[SerializeField]
	private float negativeThreshold = -2f;

	public UnityEvent onAboveNegativeThreshold;

	public UnityEvent onBelowNegativeThreshold;

	[Header("Debug")]
	[Tooltip("Current displayed speed value (raw or smoothed). Signed when Axis Filter is set.")]
	public float debugCurrentSpeed;

	private float lastSpeed;

	private float lastRawSpeed;

	private Vector3 lastVelocity;

	private Vector3 lastPos;

	private float lastSliceTime;

	private bool wasAboveThreshold;

	private bool wasMovingPositive;

	private bool wasMovingNegative;

	private bool HasAxisFilter => trackAxis != AxisFilter.None;

	public void OnEnable()
	{
		if (target == null)
		{
			target = base.transform;
		}
		lastPos = target.position;
		lastSliceTime = Time.time;
		lastVelocity = Vector3.zero;
		lastRawSpeed = 0f;
		lastSpeed = 0f;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		float num = Mathf.Max(1E-06f, Time.time - lastSliceTime);
		Vector3 position = target.position;
		Vector3 lhs = (position - lastPos) / num;
		float num2 = trackAxis switch
		{
			AxisFilter.X => Vector3.Dot(lhs, ResolveAxisRight()), 
			AxisFilter.Y => Vector3.Dot(lhs, ResolveAxisUp()), 
			AxisFilter.Z => Vector3.Dot(lhs, ResolveAxisForward()), 
			_ => lhs.magnitude, 
		};
		lastSpeed = (useRawSpeed ? num2 : Mathf.Lerp(lastSpeed, num2, 1f - Mathf.Exp((0f - responsiveness) * num)));
		float time = Mathf.Abs(lastSpeed);
		float f = postprocessCurve.Evaluate(time);
		continuousProperties.ApplyAll(f);
		float num3 = (useRawSpeed ? num2 : lastSpeed);
		onSpeedUpdated?.Invoke(num3);
		debugCurrentSpeed = num3;
		bool flag = Mathf.Abs(num3) >= eventThreshold;
		if (flag && !wasAboveThreshold)
		{
			onSpeedAboveThreshold?.Invoke();
		}
		else if (!flag && wasAboveThreshold)
		{
			onSpeedBelowThreshold?.Invoke();
		}
		wasAboveThreshold = flag;
		if (HasAxisFilter)
		{
			bool flag2 = num3 >= positiveThreshold;
			if (flag2 && !wasMovingPositive)
			{
				onAbovePositiveThreshold?.Invoke();
			}
			else if (!flag2 && wasMovingPositive)
			{
				onBelowPositiveThreshold?.Invoke();
			}
			wasMovingPositive = flag2;
			bool flag3 = num3 <= negativeThreshold;
			if (flag3 && !wasMovingNegative)
			{
				onAboveNegativeThreshold?.Invoke();
			}
			else if (!flag3 && wasMovingNegative)
			{
				onBelowNegativeThreshold?.Invoke();
			}
			wasMovingNegative = flag3;
		}
		lastVelocity = lhs;
		lastRawSpeed = num2;
		lastPos = position;
		lastSliceTime = Time.time;
	}

	public float GetPostProcessSpeed()
	{
		return postprocessCurve.Evaluate(Mathf.Abs(lastSpeed));
	}

	public float GetRawSpeed()
	{
		return lastRawSpeed;
	}

	public Vector3 GetWorldVelocity()
	{
		return lastVelocity;
	}

	public Vector3 GetLocalVelocity()
	{
		if (useWorldAxes)
		{
			return lastVelocity;
		}
		if (target != null)
		{
			return target.InverseTransformDirection(lastVelocity);
		}
		return base.transform.InverseTransformDirection(lastVelocity);
	}

	public float GetSignedSpeedAlongForward(Transform reference)
	{
		if (reference == null)
		{
			return 0f;
		}
		return Vector3.Dot(lastVelocity, reference.forward);
	}

	public float GetSignedSpeedX()
	{
		return Vector3.Dot(lastVelocity, ResolveAxisRight());
	}

	public float GetSignedSpeedY()
	{
		return Vector3.Dot(lastVelocity, ResolveAxisUp());
	}

	public float GetSignedSpeedZ()
	{
		return Vector3.Dot(lastVelocity, ResolveAxisForward());
	}

	public Vector3 GetVelocityInAxisSpace()
	{
		Vector3 rhs = ResolveAxisRight();
		Vector3 rhs2 = ResolveAxisUp();
		Vector3 rhs3 = ResolveAxisForward();
		return new Vector3(Vector3.Dot(lastVelocity, rhs), Vector3.Dot(lastVelocity, rhs2), Vector3.Dot(lastVelocity, rhs3));
	}

	private Vector3 ResolveAxisRight()
	{
		if (useWorldAxes)
		{
			if (worldSpace != null)
			{
				return worldSpace.right;
			}
			return Vector3.right;
		}
		if (!(target != null))
		{
			return base.transform.right;
		}
		return target.right;
	}

	private Vector3 ResolveAxisUp()
	{
		if (useWorldAxes)
		{
			if (worldSpace != null)
			{
				return worldSpace.up;
			}
			return Vector3.up;
		}
		if (!(target != null))
		{
			return base.transform.up;
		}
		return target.up;
	}

	private Vector3 ResolveAxisForward()
	{
		if (useWorldAxes)
		{
			if (worldSpace != null)
			{
				return worldSpace.forward;
			}
			return Vector3.forward;
		}
		if (!(target != null))
		{
			return base.transform.forward;
		}
		return target.forward;
	}
}
