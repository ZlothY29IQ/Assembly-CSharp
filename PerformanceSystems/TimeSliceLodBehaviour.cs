using UnityEngine;
using UnityEngine.Events;

namespace PerformanceSystems;

public class TimeSliceLodBehaviour : ATimeSliceBehaviour, ILod
{
	[Space]
	[SerializeField]
	protected int _currentLod = -1;

	[SerializeField]
	protected float[] _lodRanges;

	[Space]
	[SerializeField]
	protected UnityEvent[] _onLodRangeEvents;

	[Space]
	[SerializeField]
	protected UnityEvent _onCulledEvent;

	protected Transform _transform;

	public Vector3 Position => _transform.position;

	public float[] LodRanges => _lodRanges;

	public UnityEvent[] OnLodRangeEvents => _onLodRangeEvents;

	public UnityEvent OnCulledEvent => _onCulledEvent;

	public int CurrentLod => _currentLod;

	protected void Start()
	{
		_updateIfDisabled = true;
		_transform = base.transform;
	}

	protected void SetLod(int newLod)
	{
		if (newLod != _currentLod)
		{
			_currentLod = newLod;
			if (newLod < _onLodRangeEvents.Length)
			{
				_onLodRangeEvents[newLod].Invoke();
			}
			else if (newLod == _onLodRangeEvents.Length)
			{
				_onCulledEvent.Invoke();
			}
			else
			{
				Debug.LogWarning($"No event for LOD [{newLod}]", this);
			}
		}
	}

	public void UpdateLod(Vector3 refPos)
	{
		Vector3 position = _transform.position;
		float num = Vector3.Distance(refPos, position);
		for (int i = 0; i < _lodRanges.Length; i++)
		{
			float num2 = _lodRanges[i];
			if (num <= num2)
			{
				SetLod(i);
				return;
			}
		}
		SetLod(_lodRanges.Length);
	}

	public override void SliceUpdate(float deltaTime)
	{
	}

	public override void SliceUpdateAlways(float deltaTime)
	{
		UpdateLod(_timeSliceControllerAsset.ReferenceTransform.position);
	}
}
