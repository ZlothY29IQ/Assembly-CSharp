using System.Collections.Generic;
using UnityEngine;

namespace PerformanceSystems;

[CreateAssetMenu(menuName = "PerformanceTools/TimeSlicer/TimeSliceController", fileName = "TimeSliceController")]
public class TimeSliceControllerAsset : ScriptableObject
{
	private readonly List<ITimeSlice> _currentTimeSliceBehaviours = new List<ITimeSlice>();

	private readonly HashSet<ITimeSlice> _timeSliceBehavioursToAdd = new HashSet<ITimeSlice>();

	private readonly HashSet<ITimeSlice> _timeSliceBehavioursToRemove = new HashSet<ITimeSlice>();

	private Transform _referenceTransform;

	[Range(1f, 150f)]
	[SerializeField]
	private int _timeSlices = 1;

	private int _currentSlice;

	private bool _isActive;

	private int _sliceSize;

	public Transform ReferenceTransform => _referenceTransform;

	private void RemovePendingObjects()
	{
		_currentTimeSliceBehaviours.FastRemove(_timeSliceBehavioursToRemove);
		_timeSliceBehavioursToRemove.Clear();
	}

	private void AddPendingObjects()
	{
		foreach (ITimeSlice item in _timeSliceBehavioursToAdd)
		{
			if (!_currentTimeSliceBehaviours.Contains(item))
			{
				_currentTimeSliceBehaviours.Add(item);
			}
		}
		_timeSliceBehavioursToAdd.Clear();
	}

	private void UpdateCurrentSliceObjects()
	{
		int count = _currentTimeSliceBehaviours.Count;
		if (count == 0)
		{
			return;
		}
		int num = Mathf.Max(1, _timeSlices);
		_sliceSize = Mathf.CeilToInt((float)count / (float)num);
		if (_sliceSize <= 0)
		{
			_sliceSize = 1;
		}
		int num2 = _sliceSize * _currentSlice;
		if (num2 >= count)
		{
			num2 = Mathf.Max(0, count - _sliceSize);
		}
		int num3 = Mathf.Min(_sliceSize, count - num2);
		if (num3 <= 0)
		{
			return;
		}
		for (int i = 0; i < num3; i++)
		{
			int num4 = num2 + i;
			if (num4 >= 0 && num4 < _currentTimeSliceBehaviours.Count)
			{
				_currentTimeSliceBehaviours[num4]?.SliceUpdate();
				continue;
			}
			break;
		}
	}

	public void SetRefTransform(Transform refTransform)
	{
		_referenceTransform = refTransform;
		_isActive = _referenceTransform != null;
	}

	public void AddTimeSliceBehaviour(ITimeSlice timeSlice)
	{
		if (!_currentTimeSliceBehaviours.Contains(timeSlice))
		{
			_timeSliceBehavioursToAdd.Add(timeSlice);
		}
	}

	public void RemoveTimeSliceBehaviour(ITimeSlice timeSlice)
	{
		if (!_currentTimeSliceBehaviours.Contains(timeSlice))
		{
			_timeSliceBehavioursToRemove.Remove(timeSlice);
		}
		else
		{
			_timeSliceBehavioursToRemove.Add(timeSlice);
		}
	}

	public void Update()
	{
		InitializeReferenceTransformWithMainCam();
		if (_isActive)
		{
			if (_currentSlice == 0)
			{
				RemovePendingObjects();
				AddPendingObjects();
			}
			UpdateCurrentSliceObjects();
			_currentSlice = (_currentSlice + 1) % Mathf.Max(1, _timeSlices);
		}
	}

	public void InitializeReferenceTransformWithMainCam()
	{
		if (_referenceTransform == null)
		{
			_referenceTransform = Camera.main?.transform;
		}
		_isActive = _referenceTransform != null;
	}

	private void OnDisable()
	{
		ClearAsset();
	}

	public void ClearAsset()
	{
		_currentTimeSliceBehaviours.Clear();
		_timeSliceBehavioursToAdd.Clear();
		_timeSliceBehavioursToRemove.Clear();
		_referenceTransform = null;
	}
}
