using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SimpleSpeedTracker : MonoBehaviour, IGorillaSliceableSimple
{
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

	[Header("Property Output")]
	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[Header("Events")]
	[Tooltip("Speed threshold used to trigger events.")]
	[SerializeField]
	private float eventThreshold = 1f;

	public UnityEvent<float> onSpeedUpdated;

	public UnityEvent onSpeedAboveThreshold;

	public UnityEvent onSpeedBelowThreshold;

	[Header("Debug")]
	[Tooltip("Current displayed speed value (raw or smoothed).")]
	public float debugCurrentSpeed;

	private float lastSpeed;

	private float lastRawSpeed;

	private Vector3 lastVelocity;

	private Vector3 lastPos;

	private float lastSliceTime;

	private bool wasAboveThreshold;

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
		Vector3 vector = (position - lastPos) / num;
		float magnitude = vector.magnitude;
		lastSpeed = (useRawSpeed ? magnitude : Mathf.Lerp(lastSpeed, magnitude, 1f - Mathf.Exp((0f - responsiveness) * num)));
		float num2 = postprocessCurve.Evaluate(lastSpeed);
		continuousProperties.ApplyAll(num2);
		float num3 = (useRawSpeed ? magnitude : num2);
		onSpeedUpdated?.Invoke(num3);
		debugCurrentSpeed = num3;
		bool flag = num3 >= eventThreshold;
		if (flag && !wasAboveThreshold)
		{
			onSpeedAboveThreshold?.Invoke();
		}
		else if (!flag && wasAboveThreshold)
		{
			onSpeedBelowThreshold?.Invoke();
		}
		wasAboveThreshold = flag;
		lastVelocity = vector;
		lastRawSpeed = magnitude;
		lastPos = position;
		lastSliceTime = Time.time;
	}

	public float GetPostProcessSpeed()
	{
		return postprocessCurve.Evaluate(lastSpeed);
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
