using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BezierSpline : MonoBehaviour
{
	[SerializeField]
	private Vector3[] points;

	[SerializeField]
	private BezierControlPointMode[] modes;

	[SerializeField]
	private bool loop;

	private float _totalArcLength;

	private float[] _timesTable;

	private float[] _lengthsTable;

	public bool Loop
	{
		get
		{
			return loop;
		}
		set
		{
			loop = value;
			if (value)
			{
				modes[modes.Length - 1] = modes[0];
				SetControlPoint(0, points[0]);
			}
		}
	}

	public int ControlPointCount => points.Length;

	public int CurveCount => (points.Length - 1) / 3;

	private void Awake()
	{
		float num = 0f;
		for (int i = 1; i < points.Length; i++)
		{
			num += (points[i] - points[i - 1]).magnitude;
		}
		int subdivisions = Mathf.RoundToInt(num / 0.1f);
		buildTimesLenghtsTables(subdivisions);
	}

	private void buildTimesLenghtsTables(int subdivisions)
	{
		_totalArcLength = 0f;
		float num = 1f / (float)subdivisions;
		_timesTable = new float[subdivisions];
		_lengthsTable = new float[subdivisions];
		Vector3 b = GetPoint(0f);
		for (int i = 1; i <= subdivisions; i++)
		{
			float num2 = num * (float)i;
			Vector3 point = GetPoint(num2);
			_totalArcLength += Vector3.Distance(point, b);
			b = point;
			_timesTable[i - 1] = num2;
			_lengthsTable[i - 1] = _totalArcLength;
		}
	}

	private float getPathFromTime(float t)
	{
		if (float.IsNaN(_totalArcLength) || _totalArcLength == 0f)
		{
			return t;
		}
		if (t > 0f && t < 1f)
		{
			float num = _totalArcLength * t;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			int num6 = _lengthsTable.Length;
			for (int i = 0; i < num6; i++)
			{
				if (_lengthsTable[i] > num)
				{
					num4 = _timesTable[i];
					num5 = _lengthsTable[i];
					if (i > 0)
					{
						num3 = _lengthsTable[i - 1];
					}
					break;
				}
				num2 = _timesTable[i];
			}
			t = num2 + (num - num3) / (num5 - num3) * (num4 - num2);
		}
		if (t > 1f)
		{
			t = 1f;
		}
		else if (t < 0f)
		{
			t = 0f;
		}
		return t;
	}

	public void BuildSplineFromPoints(Vector3[] newPoints, BezierControlPointMode[] newModes, bool isLoop)
	{
		points = newPoints;
		modes = newModes;
		loop = isLoop;
		float num = 0f;
		for (int i = 1; i < points.Length; i++)
		{
			num += (points[i] - points[i - 1]).magnitude;
		}
		int subdivisions = Mathf.RoundToInt(num / 0.1f);
		buildTimesLenghtsTables(subdivisions);
	}

	public Vector3 GetControlPoint(int index)
	{
		return points[index];
	}

	public void SetControlPoint(int index, Vector3 point)
	{
		if (index % 3 == 0)
		{
			Vector3 vector = point - points[index];
			if (loop)
			{
				if (index == 0)
				{
					points[1] += vector;
					points[points.Length - 2] += vector;
					points[points.Length - 1] = point;
				}
				else if (index == points.Length - 1)
				{
					points[0] = point;
					points[1] += vector;
					points[index - 1] += vector;
				}
				else
				{
					points[index - 1] += vector;
					points[index + 1] += vector;
				}
			}
			else
			{
				if (index > 0)
				{
					points[index - 1] += vector;
				}
				if (index + 1 < points.Length)
				{
					points[index + 1] += vector;
				}
			}
		}
		points[index] = point;
		EnforceMode(index);
	}

	public BezierControlPointMode GetControlPointMode(int index)
	{
		return modes[(index + 1) / 3];
	}

	public void SetControlPointMode(int index, BezierControlPointMode mode)
	{
		int num = (index + 1) / 3;
		modes[num] = mode;
		if (loop)
		{
			if (num == 0)
			{
				modes[modes.Length - 1] = mode;
			}
			else if (num == modes.Length - 1)
			{
				modes[0] = mode;
			}
		}
		EnforceMode(index);
	}

	private void EnforceMode(int index)
	{
		int num = (index + 1) / 3;
		BezierControlPointMode bezierControlPointMode = modes[num];
		if (bezierControlPointMode == BezierControlPointMode.Free || (!loop && (num == 0 || num == modes.Length - 1)))
		{
			return;
		}
		int num2 = num * 3;
		int num3;
		int num4;
		if (index <= num2)
		{
			num3 = num2 - 1;
			if (num3 < 0)
			{
				num3 = points.Length - 2;
			}
			num4 = num2 + 1;
			if (num4 >= points.Length)
			{
				num4 = 1;
			}
		}
		else
		{
			num3 = num2 + 1;
			if (num3 >= points.Length)
			{
				num3 = 1;
			}
			num4 = num2 - 1;
			if (num4 < 0)
			{
				num4 = points.Length - 2;
			}
		}
		Vector3 vector = points[num2];
		Vector3 vector2 = vector - points[num3];
		if (bezierControlPointMode == BezierControlPointMode.Aligned)
		{
			vector2 = vector2.normalized * Vector3.Distance(vector, points[num4]);
		}
		points[num4] = vector + vector2;
	}

	public Vector3 GetPoint(float t, bool ConstantVelocity)
	{
		if (ConstantVelocity)
		{
			return GetPoint(getPathFromTime(t));
		}
		return GetPoint(t);
	}

	public Vector3 GetPoint(float t)
	{
		int num;
		if (t >= 1f)
		{
			t = 1f;
			num = points.Length - 4;
		}
		else
		{
			t = Mathf.Clamp01(t) * (float)CurveCount;
			num = (int)t;
			t -= (float)num;
			num *= 3;
		}
		return base.transform.TransformPoint(Bezier.GetPoint(points[num], points[num + 1], points[num + 2], points[num + 3], t));
	}

	public Vector3 GetPointLocal(float t)
	{
		int num;
		if (t >= 1f)
		{
			t = 1f;
			num = points.Length - 4;
		}
		else
		{
			t = Mathf.Clamp01(t) * (float)CurveCount;
			num = (int)t;
			t -= (float)num;
			num *= 3;
		}
		return Bezier.GetPoint(points[num], points[num + 1], points[num + 2], points[num + 3], t);
	}

	public Vector3 GetVelocity(float t)
	{
		int num;
		if (t >= 1f)
		{
			t = 1f;
			num = points.Length - 4;
		}
		else
		{
			t = Mathf.Clamp01(t) * (float)CurveCount;
			num = (int)t;
			t -= (float)num;
			num *= 3;
		}
		return base.transform.TransformPoint(Bezier.GetFirstDerivative(points[num], points[num + 1], points[num + 2], points[num + 3], t)) - base.transform.position;
	}

	public Vector3 GetDirection(float t, bool ConstantVelocity)
	{
		if (ConstantVelocity)
		{
			return GetDirection(getPathFromTime(t));
		}
		return GetDirection(t);
	}

	public Vector3 GetDirection(float t)
	{
		return GetVelocity(t).normalized;
	}

	public void AddCurve()
	{
		Vector3 vector = points[points.Length - 1];
		Array.Resize(ref points, points.Length + 3);
		vector.x += 1f;
		points[points.Length - 3] = vector;
		vector.x += 1f;
		points[points.Length - 2] = vector;
		vector.x += 1f;
		points[points.Length - 1] = vector;
		Array.Resize(ref modes, modes.Length + 1);
		modes[modes.Length - 1] = modes[modes.Length - 2];
		EnforceMode(points.Length - 4);
		if (loop)
		{
			points[points.Length - 1] = points[0];
			modes[modes.Length - 1] = modes[0];
			EnforceMode(0);
		}
	}

	public void RemoveLastCurve()
	{
		if (points.Length > 4)
		{
			Array.Resize(ref points, points.Length - 3);
			Array.Resize(ref modes, modes.Length - 1);
		}
	}

	public void RemoveCurve(int index)
	{
		if (points.Length > 4)
		{
			List<Vector3> list = points.ToList();
			int i;
			for (i = 4; i < points.Length && index - 3 > i; i += 3)
			{
			}
			for (int j = 0; j < 3; j++)
			{
				list.RemoveAt(i);
			}
			points = list.ToArray();
			int index2 = (i - 4) / 3;
			List<BezierControlPointMode> list2 = modes.ToList();
			list2.RemoveAt(index2);
			modes = list2.ToArray();
		}
	}

	public void Reset()
	{
		points = new Vector3[4]
		{
			new Vector3(0f, -1f, 0f),
			new Vector3(0f, -1f, 2f),
			new Vector3(0f, -1f, 4f),
			new Vector3(0f, -1f, 6f)
		};
		modes = new BezierControlPointMode[2];
	}
}
