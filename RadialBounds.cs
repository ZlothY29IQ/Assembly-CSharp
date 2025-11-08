using UnityEngine;
using UnityEngine.Events;

public class RadialBounds : MonoBehaviour
{
	[SerializeField]
	private Vector3 _localCenter;

	[SerializeField]
	private float _localRadius = 1f;

	[Space]
	public UnityEvent<RadialBounds> onOverlapEnter;

	public UnityEvent<RadialBounds> onOverlapExit;

	public UnityEvent<RadialBounds, float> onOverlapStay;

	public Vector3 localCenter
	{
		get
		{
			return _localCenter;
		}
		set
		{
			_localCenter = value;
		}
	}

	public float localRadius
	{
		get
		{
			return _localRadius;
		}
		set
		{
			_localRadius = value;
		}
	}

	public Vector3 center => base.transform.TransformPoint(_localCenter);

	public float radius => MathUtils.GetScaledRadius(_localRadius, base.transform.lossyScale);
}
