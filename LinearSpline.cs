using System.Collections.Generic;
using UnityEngine;

public class LinearSpline : MonoBehaviour
{
	public struct CurveBoundary
	{
		public Vector3 start;

		public Vector3 end;
	}

	public Transform[] controlPointTransforms = new Transform[0];

	public Transform debugTransform;

	public List<Vector3> controlPoints = new List<Vector3>();

	public List<float> distances = new List<float>();

	public List<CurveBoundary> curveBoundaries = new List<CurveBoundary>();

	public bool roundCorners;

	public float cornerRadius = 1f;

	public bool looping;

	public float testFloat;

	public int gizmoResolution = 128;

	public float totalDistance;

	private void RefreshControlPoints()
	{
		controlPoints.Clear();
		for (int i = 0; i < controlPointTransforms.Length; i++)
		{
			controlPoints.Add(controlPointTransforms[i].position);
		}
		totalDistance = 0f;
		distances.Clear();
		for (int j = 1; j < controlPoints.Count; j++)
		{
			float num = Vector3.Distance(controlPoints[j - 1], controlPoints[j]);
			distances.Add(num);
			totalDistance += num;
		}
		float num2 = Vector3.Distance(controlPoints[controlPoints.Count - 1], controlPoints[0]);
		distances.Add(num2);
		if (looping)
		{
			totalDistance += num2;
		}
		curveBoundaries.Clear();
		if (roundCorners)
		{
			for (int k = 0; k < controlPoints.Count; k++)
			{
				int num3 = ((k > 0) ? (k - 1) : (controlPoints.Count - 1));
				int index = (k + 1) % controlPoints.Count;
				float num4 = Mathf.Min(Mathf.Min(cornerRadius, distances[num3 % distances.Count] * 0.5f), distances[k % distances.Count] * 0.5f);
				curveBoundaries.Add(new CurveBoundary
				{
					start = Vector3.Lerp(controlPoints[num3], controlPoints[k], 1f - num4 / distances[num3 % distances.Count]),
					end = Vector3.Lerp(controlPoints[k], controlPoints[index], num4 / distances[k])
				});
			}
		}
	}

	private void Awake()
	{
		RefreshControlPoints();
	}

	public Vector3 Evaluate(float t)
	{
		if (controlPoints.Count < 1)
		{
			return Vector3.zero;
		}
		if (controlPoints.Count < 2)
		{
			return controlPoints[0];
		}
		if (controlPoints.Count < 3)
		{
			return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
		}
		float num = Mathf.Clamp01(t) * totalDistance;
		int num2 = 0;
		float num3 = 0f;
		float num4 = 0f;
		for (int i = 0; i < distances.Count; i++)
		{
			if (looping || i != distances.Count - 1)
			{
				num2 = i;
				if (num - num4 <= distances[i])
				{
					num3 = Mathf.Clamp01((num - num4) / distances[i]);
					break;
				}
				num3 = 1f;
				num4 += distances[i];
			}
		}
		num2 %= controlPoints.Count;
		int num5 = (num2 + 1) % controlPoints.Count;
		if (roundCorners)
		{
			if (num3 > 0.5f && (looping || num2 < controlPoints.Count - 2))
			{
				_ = (num5 + 1) % controlPoints.Count;
				float num6 = Mathf.Min(Mathf.Min(cornerRadius, distances[num2] * 0.5f), distances[num5 % distances.Count] * 0.5f);
				float num7 = 1f - num6 / distances[num2];
				if (num3 > num7)
				{
					Vector3 start = curveBoundaries[num5].start;
					Vector3 end = curveBoundaries[num5].end;
					float t2 = 0.5f * Mathf.Clamp01((num3 - num7) / (1f - num7));
					Vector3 a = Vector3.Lerp(start, controlPoints[num5], t2);
					Vector3 b = Vector3.Lerp(controlPoints[num5], end, t2);
					return Vector3.Lerp(a, b, t2);
				}
			}
			else if (num3 <= 0.5f && (looping || num2 > 0))
			{
				int num8 = ((num2 > 0) ? (num2 - 1) : (controlPoints.Count - 1));
				float num9 = Mathf.Min(Mathf.Min(cornerRadius, distances[num2] * 0.5f), distances[num8 % distances.Count] * 0.5f) / distances[num2];
				if (num3 < num9)
				{
					Vector3 start2 = curveBoundaries[num2].start;
					Vector3 end2 = curveBoundaries[num2].end;
					float t3 = 0.5f + 0.5f * Mathf.Clamp01(num3 / num9);
					Vector3 a2 = Vector3.Lerp(start2, controlPoints[num2], t3);
					Vector3 b2 = Vector3.Lerp(controlPoints[num2], end2, t3);
					return Vector3.Lerp(a2, b2, t3);
				}
			}
		}
		return Vector3.Lerp(controlPoints[num2], controlPoints[num5], num3);
	}

	public Vector3 GetForwardTangent(float t, float step = 0.01f)
	{
		t = Mathf.Clamp(t, 0f, 1f - step - Mathf.Epsilon);
		Vector3 vector = Evaluate(t);
		return (Evaluate(t + step) - vector).normalized;
	}

	private void OnDrawGizmosSelected()
	{
		RefreshControlPoints();
		Gizmos.color = Color.yellow;
		int num = gizmoResolution;
		Vector3 vector = Evaluate(0f);
		for (int i = 1; i <= num; i++)
		{
			float t = (float)i / (float)num;
			Vector3 vector2 = Evaluate(t);
			Gizmos.DrawLine(vector, vector2);
			vector = vector2;
		}
		Vector3 to = Evaluate(1f);
		Gizmos.DrawLine(vector, to);
	}
}
