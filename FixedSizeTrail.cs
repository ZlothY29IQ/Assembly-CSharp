using System;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FixedSizeTrail : MonoBehaviour
{
	[SerializeField]
	private Transform _transform;

	[SerializeField]
	private LineRenderer _lineRenderer;

	[SerializeField]
	[Range(1f, 128f)]
	private int _segments = 8;

	[SerializeField]
	private float _length = 8f;

	public bool manualUpdate;

	[Space]
	public bool applyPhysics;

	public Vector3 gravity = new Vector3(0f, -9.8f, 0f);

	public AnimationCurve gravityCurve = AnimationCurves.EaseInCubic;

	[Space]
	private Vector3[] _points = new Vector3[8];

	public LineRenderer renderer => _lineRenderer;

	public float length
	{
		get
		{
			return _length;
		}
		set
		{
			_length = Math.Clamp(value, 0f, 128f);
		}
	}

	public Vector3[] points => _points;

	private void Reset()
	{
		Setup();
	}

	private void Awake()
	{
		Setup();
	}

	private void OnEnable()
	{
		Setup();
	}

	public void Setup()
	{
		_transform = base.transform;
		if (_lineRenderer == null)
		{
			_lineRenderer = GetComponent<LineRenderer>();
		}
		if ((bool)_lineRenderer)
		{
			_lineRenderer.useWorldSpace = true;
			Vector3 position = _transform.position;
			Vector3 forward = _transform.forward;
			int num = _segments + 1;
			_points = new Vector3[num];
			float num2 = _length / (float)_segments;
			for (int i = 0; i < num; i++)
			{
				_points[i] = position - forward * num2 * i;
			}
			_lineRenderer.positionCount = num;
			_lineRenderer.SetPositions(_points);
			Update();
		}
	}

	private void Update()
	{
		if (!manualUpdate)
		{
			Update(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (applyPhysics)
		{
			float deltaTime = Time.deltaTime;
			int num = _points.Length - 1;
			_ = _length / (float)num;
			for (int i = 1; i < num; i++)
			{
				float time = (float)(i - 1) / (float)num;
				float num2 = gravityCurve.Evaluate(time);
				Vector3 vector = gravity * (num2 * deltaTime);
				_points[i] += vector;
				_points[i + 1] += vector;
			}
		}
	}

	public void Update(float dt)
	{
		float num = _length / (float)(_segments - 1);
		Vector3 position = _transform.position;
		_points[0] = position;
		float num2 = Vector3.Distance(_points[0], _points[1]);
		float num3 = num - num2;
		if (num2 > num)
		{
			Array.Copy(_points, 0, _points, 1, _points.Length - 1);
		}
		for (int i = 0; i < _points.Length - 1; i++)
		{
			Vector3 vector = _points[i];
			Vector3 vector2 = _points[i + 1] - vector;
			if (vector2.sqrMagnitude > num * num)
			{
				_points[i + 1] = vector + vector2.normalized * num;
			}
		}
		if (num3 > 0f)
		{
			int num4 = _points.Length - 1;
			int num5 = num4 - 1;
			Vector3 vector3 = _points[num4] - _points[num5];
			Vector3 vector4 = vector3.normalized;
			if (applyPhysics)
			{
				Vector3 normalized = (_points[num5] - _points[num5 - 1]).normalized;
				vector4 = Vector3.Lerp(vector4, normalized, 0.5f);
			}
			_points[num4] = _points[num5] + vector4 * Math.Min(vector3.magnitude, num3);
		}
		_lineRenderer.SetPositions(_points);
	}

	private static float CalcLength(in Vector3[] positions)
	{
		float num = 0f;
		for (int i = 0; i < positions.Length - 1; i++)
		{
			num += Vector3.Distance(positions[i], positions[i + 1]);
		}
		return num;
	}
}
