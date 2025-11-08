using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(BezierSpline))]
public class ManipulatableSpinner : ManipulatableObject
{
	public float breakDistance = 0.2f;

	public bool applyReleaseVelocity;

	public float releaseDrag = 1f;

	public float lowSpeedThreshold = 0.12f;

	public float lowSpeedDrag = 3f;

	private BezierSpline spline;

	private float previousHandT;

	private float currentHandT;

	private float tVelocity;

	public float angle { get; private set; }

	private void Awake()
	{
		spline = GetComponent<BezierSpline>();
	}

	protected override void OnStartManipulation(GameObject grabbingHand)
	{
		Vector3 position = grabbingHand.transform.position;
		float num = FindPositionOnSpline(position);
		previousHandT = num;
	}

	protected override void OnStopManipulation(GameObject releasingHand, Vector3 releaseVelocity)
	{
	}

	protected override bool ShouldHandDetach(GameObject hand)
	{
		if (!spline.Loop && (currentHandT >= 0.99f || currentHandT <= 0.01f))
		{
			return true;
		}
		Vector3 position = hand.transform.position;
		Vector3 point = spline.GetPoint(currentHandT);
		if (Vector3.SqrMagnitude(position - point) > breakDistance * breakDistance)
		{
			return true;
		}
		return false;
	}

	protected override void OnHeldUpdate(GameObject hand)
	{
		float num = angle;
		Vector3 position = hand.transform.position;
		currentHandT = FindPositionOnSpline(position);
		float num2 = currentHandT - previousHandT;
		if (spline.Loop)
		{
			if (num2 > 0.5f)
			{
				num2 -= 1f;
			}
			else if (num2 < -0.5f)
			{
				num2 += 1f;
			}
		}
		angle += num2;
		previousHandT = currentHandT;
		if (applyReleaseVelocity && currentHandT <= 0.99f && currentHandT >= 0.01f)
		{
			tVelocity = (angle - num) / Time.deltaTime;
		}
	}

	protected override void OnReleasedUpdate()
	{
		if (tVelocity != 0f)
		{
			angle += tVelocity * Time.deltaTime;
			if (Mathf.Abs(tVelocity) < lowSpeedThreshold)
			{
				tVelocity *= 1f - lowSpeedDrag * Time.deltaTime;
			}
			else
			{
				tVelocity *= 1f - releaseDrag * Time.deltaTime;
			}
		}
	}

	private float FindPositionOnSpline(Vector3 grabPoint)
	{
		int i = 0;
		int num = 200;
		float num2 = 0.001f;
		float num3 = 1f / (float)num;
		float3 y = base.transform.InverseTransformPoint(grabPoint);
		float result = 0f;
		float num4 = float.PositiveInfinity;
		for (; i < num; i++)
		{
			float num5 = math.distancesq(spline.GetPointLocal(num2), y);
			if (num5 < num4)
			{
				num4 = num5;
				result = num2;
			}
			num2 += num3;
		}
		return result;
	}

	public void SetAngle(float newAngle)
	{
		angle = newAngle;
	}

	public void SetVelocity(float newVelocity)
	{
		tVelocity = newVelocity;
	}
}
