using UnityEngine;

namespace PerformanceSystems;

public abstract class ATimeSliceBehaviour : MonoBehaviour, ITimeSlice
{
	[SerializeField]
	protected TimeSliceControllerAsset _timeSliceControllerAsset;

	[SerializeField]
	protected bool _updateIfDisabled = true;

	protected float _lastUpdateTime;

	protected void Awake()
	{
		_timeSliceControllerAsset.AddTimeSliceBehaviour(this);
	}

	protected void OnDestroy()
	{
		_timeSliceControllerAsset.RemoveTimeSliceBehaviour(this);
	}

	public void SliceUpdate()
	{
		float deltaTime = Time.realtimeSinceStartup - _lastUpdateTime;
		_lastUpdateTime = Time.realtimeSinceStartup;
		SliceUpdateAlways(deltaTime);
		if (_updateIfDisabled || base.gameObject.activeSelf)
		{
			SliceUpdate(deltaTime);
		}
	}

	public abstract void SliceUpdate(float deltaTime);

	public abstract void SliceUpdateAlways(float deltaTime);
}
