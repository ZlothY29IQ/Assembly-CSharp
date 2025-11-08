using UnityEngine;

public class BezierCurve : MonoBehaviour
{
	public Transform referenceTransform;

	public Vector3[] points;

	public Vector3 GetPoint(float t)
	{
		Vector3 vector = ((points.Length == 3) ? Bezier.GetPoint(points[0], points[1], points[2], t) : Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
		if (!referenceTransform)
		{
			return vector;
		}
		return referenceTransform.TransformPoint(vector);
	}

	public Vector3 GetVelocity(float t)
	{
		Vector3 vector = ((points.Length == 3) ? Bezier.GetFirstDerivative(points[0], points[1], points[2], t) : Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t));
		if (!referenceTransform)
		{
			return vector;
		}
		return referenceTransform.TransformPoint(vector) - referenceTransform.position;
	}

	public Vector3 GetDirection(float t)
	{
		return GetVelocity(t).normalized;
	}

	public void Reset()
	{
		referenceTransform = base.transform;
		points = new Vector3[4]
		{
			new Vector3(1f, 0f, 0f),
			new Vector3(2f, 0f, 0f),
			new Vector3(3f, 0f, 0f),
			new Vector3(4f, 0f, 0f)
		};
	}
}
