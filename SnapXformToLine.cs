using UnityEngine;
using UnityEngine.Events;

public class SnapXformToLine : MonoBehaviour
{
	public bool apply = true;

	public bool snapOrientation = true;

	public bool resetOnDisable = true;

	[Space]
	public Transform target;

	[Space]
	public Transform from;

	public Transform to;

	private Vector3 _closest;

	private float _linear;

	public Ref<IRangedVariable<float>> output;

	public UnityEvent<float> onLinearDistanceChanged;

	public UnityEvent<Vector3> onPositionChanged;

	public Vector3 linePoint => _closest;

	public float linearDistance => _linear;

	public void SnapTarget(bool applyToXform = true)
	{
		Snap(target);
	}

	public void SnapTarget(Vector3 point)
	{
		if ((bool)target)
		{
			target.position = GetSnappedPoint(target.position);
		}
	}

	public void SnapTargetLinear(float t)
	{
		if ((bool)target && (bool)from && (bool)to)
		{
			target.position = Vector3.Lerp(from.position, to.position, t);
		}
	}

	public Vector3 GetSnappedPoint(Transform t)
	{
		return GetSnappedPoint(t.position);
	}

	public Vector3 GetSnappedPoint(Vector3 point)
	{
		if (!apply)
		{
			return point;
		}
		if (!from || !to)
		{
			return point;
		}
		return GetClosestPointOnLine(point, from.position, to.position);
	}

	public void Snap(Transform xform, bool applyToXform = true)
	{
		if (!apply || !xform || !from || !to)
		{
			return;
		}
		Vector3 position = xform.position;
		Vector3 position2 = from.position;
		Vector3 position3 = to.position;
		Vector3 closestPointOnLine = GetClosestPointOnLine(position, position2, position3);
		float num = Vector3.Distance(position2, position3);
		float num2 = Vector3.Distance(closestPointOnLine, position2);
		Vector3 closest = _closest;
		Vector3 closest2 = closestPointOnLine;
		float linear = _linear;
		float num3 = (Mathf.Approximately(num, 0f) ? 0f : (num2 / (num + Mathf.Epsilon)));
		_closest = closest2;
		_linear = num3;
		if ((bool)output)
		{
			IRangedVariable<float> asT = output.AsT;
			asT.Set(asT.Min + _linear * asT.Range);
		}
		if (applyToXform)
		{
			xform.position = _closest;
			if (!Mathf.Approximately(closest.x, closest2.x) || !Mathf.Approximately(closest.y, closest2.y) || !Mathf.Approximately(closest.z, closest2.z))
			{
				onPositionChanged?.Invoke(_closest);
			}
			if (!Mathf.Approximately(linear, num3))
			{
				onLinearDistanceChanged?.Invoke(_linear);
			}
			if (snapOrientation)
			{
				xform.forward = (position3 - position2).normalized;
				xform.up = Vector3.Lerp(from.up.normalized, to.up.normalized, _linear);
			}
		}
	}

	private void OnDisable()
	{
		if (resetOnDisable)
		{
			SnapTargetLinear(0f);
		}
	}

	private void LateUpdate()
	{
		SnapTarget();
	}

	private static Vector3 GetClosestPointOnLine(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 lhs = p - a;
		Vector3 vector = b - a;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = Mathf.Clamp(Vector3.Dot(lhs, vector) / sqrMagnitude, 0f, 1f);
		return a + vector * num;
	}
}
