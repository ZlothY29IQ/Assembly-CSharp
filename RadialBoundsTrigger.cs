using UnityEngine;

public class RadialBoundsTrigger : MonoBehaviour
{
	[SerializeField]
	private Id32 _triggerID;

	[Space]
	public RadialBounds object1 = new RadialBounds();

	[Space]
	public RadialBounds object2 = new RadialBounds();

	[Space]
	public float hysteresis = 0.5f;

	[SerializeField]
	private bool _raiseEvents = true;

	[Space]
	private bool _overlapping;

	private float _timeSpentInOverlap;

	[Space]
	private float _timeOverlapStarted;

	private float _timeOverlapStopped;

	public void TestOverlap()
	{
		TestOverlap(_raiseEvents);
	}

	public void TestOverlap(bool raiseEvents)
	{
		if (!object1 || !object2)
		{
			_overlapping = false;
			_timeOverlapStarted = -1f;
			_timeOverlapStopped = -1f;
			_timeSpentInOverlap = 0f;
			return;
		}
		float time = Time.time;
		float num = object1.radius + object2.radius;
		bool flag = (object2.center - object1.center).sqrMagnitude <= num * num;
		if (_overlapping && flag)
		{
			_overlapping = true;
			_timeSpentInOverlap = time - _timeOverlapStarted;
			if (raiseEvents)
			{
				object1.onOverlapStay?.Invoke(object2, _timeSpentInOverlap);
				object2.onOverlapStay?.Invoke(object1, _timeSpentInOverlap);
			}
		}
		else if (!_overlapping && flag)
		{
			if (!(time - _timeOverlapStopped < hysteresis))
			{
				_overlapping = true;
				_timeOverlapStarted = time;
				_timeOverlapStopped = -1f;
				_timeSpentInOverlap = 0f;
				if (raiseEvents)
				{
					object1.onOverlapEnter?.Invoke(object2);
					object2.onOverlapEnter?.Invoke(object1);
				}
			}
		}
		else if (!flag && _overlapping)
		{
			_overlapping = false;
			_timeOverlapStarted = -1f;
			_timeOverlapStopped = time;
			_timeSpentInOverlap = 0f;
			if (raiseEvents)
			{
				object1.onOverlapExit?.Invoke(object2);
				object2.onOverlapExit?.Invoke(object1);
			}
		}
	}

	private void FixedUpdate()
	{
		TestOverlap();
	}

	private void OnDisable()
	{
		if (_raiseEvents && (bool)object1 && (bool)object2 && _overlapping)
		{
			object1.onOverlapExit?.Invoke(object2);
			object2.onOverlapExit?.Invoke(object1);
		}
		_timeOverlapStarted = -1f;
		_timeSpentInOverlap = 0f;
		_overlapping = false;
	}
}
