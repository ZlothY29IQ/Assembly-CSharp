using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

public class StayOutsideOfGeo : MonoBehaviour
{
	[SerializeField]
	private Transform _target;

	[SerializeField]
	private Vector3 _targetOffset = Vector3.zero;

	[SerializeField]
	private float _threshold = 0.4f;

	[SerializeField]
	private bool _pauseOnPenetration = true;

	[SerializeField]
	private Collider _disableOnMove;

	private VoxelWorld _voxelWorld;

	private List<int3> _positionHistory = new List<int3>();

	private int _maxHistorySize = 10;

	private int _historyIndex;

	private float _maxDensity;

	private float _minDensity;

	private void Reset()
	{
		_target = base.transform;
	}

	private void Start()
	{
		if ((object)_target == null)
		{
			_target = base.transform;
		}
		_voxelWorld = VoxelWorld.GetFor(base.gameObject);
		if (_voxelWorld == null)
		{
			Debug.LogError("VoxelWorld not found in the scene. Please ensure there is a VoxelWorld component present.");
			base.enabled = false;
		}
	}

	private void Update()
	{
		int3 voxelForWorldPosition = _voxelWorld.GetVoxelForWorldPosition(_target.position + _targetOffset);
		if (TestPosition(voxelForWorldPosition).match)
		{
			AddPositionToHistory(voxelForWorldPosition);
			return;
		}
		if (ResolvePenetration(voxelForWorldPosition))
		{
			Debug.Log($"Successfully resolved penetration for {_target.name} at position {voxelForWorldPosition}", this);
			return;
		}
		for (int i = 10; i < 100; i += 10)
		{
			for (int j = 0; j < 10; j++)
			{
				int x = voxelForWorldPosition.x + UnityEngine.Random.Range(-i, i);
				int y = voxelForWorldPosition.y + UnityEngine.Random.Range(-i, i);
				int z = voxelForWorldPosition.z + UnityEngine.Random.Range(-i, i);
				int3 int5 = new int3(x, y, z);
				if (IsOutsideGeo(int5))
				{
					Debug.Log($"Found valid random position {int5} outside geo for {_target.name}", this);
					SetPosition(_voxelWorld.GetWorldPosition(int5));
					return;
				}
			}
		}
	}

	private bool ResolvePenetration(int3 pos)
	{
		Debug.LogWarning($"{base.name} inside geo in {_voxelWorld.GetChunkForLocalPosition(pos)} [{_target.position.RoundToInt()}={_voxelWorld.GetVoxelForWorldPosition(_target.position)}->{_maxDensity:F2}]", this);
		for (int i = 0; i < _positionHistory.Count; i++)
		{
			int3 int5 = PopMostRecentPosition();
			if (!int5.Equals(int3.zero))
			{
				(bool, int3) tuple = TestPosition(int5, useThreshold: false);
				if (tuple.Item1)
				{
					Debug.Log($"Found valid position {tuple.Item2} near recent position {int5} at index {i}");
					SetPosition(_voxelWorld.GetWorldPosition(tuple.Item2));
					return true;
				}
			}
		}
		return false;
	}

	private void SetPosition(Vector3 worldPosition)
	{
		Debug.Log($"Moving {_target} from {_target.position} to {worldPosition}", this);
		Debug.DrawLine(_target.position, worldPosition, Color.red, 5f);
		if ((bool)_disableOnMove)
		{
			_disableOnMove.enabled = false;
		}
		_target.position = worldPosition;
		if ((bool)_disableOnMove)
		{
			_disableOnMove.enabled = true;
		}
	}

	private (bool match, int3 position) TestPosition(int3 position, bool useThreshold = true)
	{
		float a = _voxelWorld.GetDensityAt(position, 0).ToFloat();
		_maxDensity = Mathf.Max(a, float.MinValue);
		_minDensity = Mathf.Min(a, float.MaxValue);
		float num = (useThreshold ? _threshold : 0f);
		if (_voxelWorld.GetDensityAt(position, 0).ToFloat() < num)
		{
			for (int i = position.x - 1; i <= position.x + 1; i++)
			{
				for (int j = position.y - 1; j <= position.y + 1; j++)
				{
					for (int k = position.z - 1; k <= position.z + 1; k++)
					{
						a = _voxelWorld.GetDensityAt(new int3(i, j, k), 0).ToFloat();
						_maxDensity = Mathf.Max(a, _maxDensity);
						_minDensity = Mathf.Min(a, _minDensity);
						if (a < 0f)
						{
							return (match: true, position: new int3(i, j, k));
						}
					}
				}
			}
		}
		return (match: false, position: position);
	}

	private bool IsOutsideGeo(int3 position)
	{
		return _voxelWorld.GetDensityAt(position, 0).ToFloat() < 0f;
	}

	private void AddPositionToHistory(int3 position)
	{
		if (!_positionHistory.Contains(position))
		{
			if (_positionHistory.Count >= _maxHistorySize)
			{
				_positionHistory[_historyIndex] = position;
			}
			else
			{
				_positionHistory.Add(position);
			}
			_historyIndex = (_historyIndex + 1) % _maxHistorySize;
		}
	}

	private int3 GetMostRecentPosition()
	{
		if (_positionHistory.Count == 0)
		{
			return int3.zero;
		}
		return _positionHistory[(_historyIndex - 1 + _positionHistory.Count) % _positionHistory.Count];
	}

	private int3 PopMostRecentPosition()
	{
		if (_positionHistory.Count == 0)
		{
			return int3.zero;
		}
		_historyIndex = (_historyIndex - 1 + _maxHistorySize) % _maxHistorySize;
		return _positionHistory[_historyIndex];
	}
}
