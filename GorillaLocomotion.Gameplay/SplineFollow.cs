using System.Collections.Generic;
using Photon.Pun;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace GorillaLocomotion.Gameplay;

public sealed class SplineFollow : MonoBehaviour
{
	private struct SplineNode(Vector3 position, Vector3 tangent, Vector3 up)
	{
		public readonly Vector3 Position = position;

		public readonly Vector3 Tangent = tangent;

		public readonly Vector3 Up = up;

		public static SplineNode Lerp(SplineNode a, SplineNode b, float t)
		{
			return new SplineNode(Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.Tangent, b.Tangent, t), Vector3.Lerp(a.Up, b.Up, t));
		}
	}

	[SerializeField]
	[Tooltip("If true, approximates the spline position. Only use when exact position does not matter.")]
	private bool _approximate;

	[SerializeField]
	private SplineContainer _unitySpline;

	[SerializeField]
	private float _duration;

	private double _secondsToCycles;

	[SerializeField]
	private float _smoothRotationTrackingRate = 0.5f;

	private float _smoothRotationTrackingRateExp;

	private float _progressPerFixedUpdate;

	[SerializeField]
	private float _splineProgressOffset;

	[SerializeField]
	private Quaternion _rotationFix = Quaternion.identity;

	private NativeSpline _nativeSpline;

	private float _progress;

	[Header("Approximate Spline Parameters")]
	[SerializeField]
	[Range(4f, 200f)]
	private int _approximationResolution = 100;

	private readonly List<SplineNode> _approximationNodes = new List<SplineNode>();

	public void Start()
	{
		base.transform.rotation *= _rotationFix;
		_smoothRotationTrackingRateExp = Mathf.Exp(_smoothRotationTrackingRate);
		_progress = _splineProgressOffset;
		_progressPerFixedUpdate = Time.fixedDeltaTime / _duration;
		_secondsToCycles = 1f / _duration;
		_nativeSpline = new NativeSpline(_unitySpline.Spline, _unitySpline.transform.localToWorldMatrix, Allocator.Persistent);
		if (_approximate)
		{
			CalculateApproximationNodes();
		}
	}

	private void CalculateApproximationNodes()
	{
		for (int i = 0; i < _approximationResolution; i++)
		{
			_nativeSpline.Evaluate((float)i / (float)_approximationResolution, out var position, out var tangent, out var upVector);
			SplineNode item = new SplineNode(position, tangent, upVector);
			_approximationNodes.Add(item);
		}
		if (_nativeSpline.Closed)
		{
			_approximationNodes.Add(_approximationNodes[0]);
		}
	}

	private void FixedUpdate()
	{
		if (!_approximate)
		{
			FollowSpline();
		}
	}

	private void Update()
	{
		if (_approximate)
		{
			FollowSpline();
		}
	}

	private void FollowSpline()
	{
		if (PhotonNetwork.InRoom)
		{
			double num = PhotonNetwork.Time * _secondsToCycles + (double)_splineProgressOffset;
			_progress = (float)(num % 1.0);
		}
		else
		{
			_progress = (_progress + _progressPerFixedUpdate) % 1f;
		}
		SplineNode splineNode = EvaluateSpline(_progress);
		base.transform.position = splineNode.Position;
		Quaternion a = Quaternion.LookRotation(splineNode.Tangent) * _rotationFix;
		base.transform.rotation = Quaternion.Slerp(a, base.transform.rotation, Mathf.Exp((0f - _smoothRotationTrackingRateExp) * Time.deltaTime));
	}

	private SplineNode EvaluateSpline(float t)
	{
		t %= 1f;
		if (_approximate)
		{
			float num = t * (float)_approximationNodes.Count;
			int num2 = (int)num;
			float t2 = num - (float)num2;
			num2 %= _approximationNodes.Count;
			SplineNode a = _approximationNodes[num2];
			SplineNode b = _approximationNodes[(num2 + 1) % _approximationNodes.Count];
			return SplineNode.Lerp(a, b, t2);
		}
		_nativeSpline.Evaluate(t, out var position, out var tangent, out var upVector);
		return new SplineNode(position, tangent, upVector);
	}

	private void OnDestroy()
	{
		_nativeSpline.Dispose();
	}
}
