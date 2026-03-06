using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag.Reactions;

public class SpawnWorldEffects : MonoBehaviour
{
	private enum TransformAxis
	{
		Forward,
		Back,
		Right,
		Left,
		Up,
		Down
	}

	[Tooltip("The defaults are numbers for the flamethrower hair dryer.")]
	private readonly float _maxParticleHitReactionRate = 2f;

	[Tooltip("Must be in the global object pool and have a tag.")]
	[SerializeField]
	private GameObject _prefabToSpawn;

	[Tooltip("When enabled, a short raycast is fired from the spawn position to find the exact surface normal. The spawned object's Up vector will be aligned to that normal instead of world Up.")]
	[SerializeField]
	private bool _useNormalOrientation;

	[SerializeField]
	private float _normalRaycastDistance = 0.3f;

	[SerializeField]
	private LayerMask _normalRaycastLayers = 134218241;

	[Header("Raycast Direction Override")]
	[Tooltip("Optional. When assigned, the raycast used for normal-orientation will shoot along this transform's forward axis instead of along the incoming hit normal.")]
	[SerializeField]
	private Transform _raycastDirectionSource;

	[Tooltip("If true, uses -forward instead of forward from Raycast Direction Source.")]
	[SerializeField]
	private bool _raycastDirectionUseNegativeForward;

	[Header("Forward Orientation")]
	[Tooltip("Optional. When assigned, the spawned object's forward vector will be aligned to the chosen axis of this transform, projected onto the spawn surface.")]
	[SerializeField]
	private Transform _forwardOrientationSource;

	[Tooltip("Which local axis of the Forward Orientation Source to use as the spawned object's forward.")]
	[SerializeField]
	private TransformAxis _forwardSourceAxis;

	private bool _hasPrefabToSpawn;

	private bool _isPrefabInPool;

	private double _lastCollisionTime;

	private SinglePool _pool;

	protected void OnEnable()
	{
		if (GorillaComputer.instance == null)
		{
			Debug.LogError("SpawnWorldEffects: Disabling because GorillaComputer not found! Hierarchy path: " + base.transform.GetPath(), this);
			base.enabled = false;
			return;
		}
		if (_prefabToSpawn != null && !_isPrefabInPool)
		{
			if (_prefabToSpawn.CompareTag("Untagged"))
			{
				Debug.LogError("SpawnWorldEffects: Disabling because Spawn Prefab has no tag! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_isPrefabInPool = ObjectPools.instance.DoesPoolExist(_prefabToSpawn);
			if (!_isPrefabInPool)
			{
				Debug.LogError("SpawnWorldEffects: Disabling because Spawn Prefab not in pool! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_pool = ObjectPools.instance.GetPoolByObjectType(_prefabToSpawn);
		}
		_hasPrefabToSpawn = _prefabToSpawn != null && _isPrefabInPool;
	}

	public void RequestSpawn(Vector3 worldPosition)
	{
		RequestSpawn(worldPosition, Vector3.up);
	}

	public void RequestSpawn(Vector3 worldPosition, Vector3 normal)
	{
		if (_maxParticleHitReactionRate < 1E-05f || !FireManager.hasInstance)
		{
			return;
		}
		double num = GTTime.TimeAsDouble();
		if ((float)(num - _lastCollisionTime) < 1f / _maxParticleHitReactionRate)
		{
			return;
		}
		if (_hasPrefabToSpawn && _isPrefabInPool && _pool.GetInactiveCount() > 0)
		{
			Vector3 vector = normal;
			if (_useNormalOrientation)
			{
				vector = GetSurfaceNormal(worldPosition, normal);
			}
			Quaternion? rotationOverride = null;
			if (_forwardOrientationSource != null)
			{
				Vector3 vector2 = Vector3.ProjectOnPlane(GetAxisVector(_forwardOrientationSource, _forwardSourceAxis), vector);
				if (vector2.sqrMagnitude < 0.0001f)
				{
					vector2 = Vector3.ProjectOnPlane((Mathf.Abs(vector.y) < 0.99f) ? Vector3.up : Vector3.right, vector);
				}
				rotationOverride = Quaternion.LookRotation(vector2.normalized, vector);
			}
			FireManager.SpawnFire(_pool, worldPosition, vector, base.transform.lossyScale.x, rotationOverride);
		}
		_lastCollisionTime = num;
	}

	private static Vector3 GetAxisVector(Transform source, TransformAxis axis)
	{
		return axis switch
		{
			TransformAxis.Forward => source.forward, 
			TransformAxis.Back => -source.forward, 
			TransformAxis.Right => source.right, 
			TransformAxis.Left => -source.right, 
			TransformAxis.Up => source.up, 
			TransformAxis.Down => -source.up, 
			_ => source.forward, 
		};
	}

	private Vector3 GetSurfaceNormal(Vector3 worldPosition, Vector3 hitNormal)
	{
		Vector3 vector2;
		if (_raycastDirectionSource != null)
		{
			Vector3 vector = _raycastDirectionSource.forward;
			if (_raycastDirectionUseNegativeForward)
			{
				vector = -vector;
			}
			vector2 = ((vector.sqrMagnitude > 1E-06f) ? vector.normalized : Vector3.down);
		}
		else
		{
			vector2 = -((hitNormal.sqrMagnitude > 1E-06f) ? hitNormal.normalized : Vector3.up);
		}
		Vector3 origin = worldPosition + -vector2 * 0.05f;
		Vector3 direction = vector2;
		if (Physics.Raycast(origin, direction, out var hitInfo, _normalRaycastDistance + 0.05f, _normalRaycastLayers, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.normal;
		}
		return hitNormal;
	}
}
