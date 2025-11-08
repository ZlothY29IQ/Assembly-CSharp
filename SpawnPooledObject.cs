using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Serialization;

public class SpawnPooledObject : MonoBehaviour
{
	[SerializeField]
	private Transform _spawnLocation;

	[SerializeField]
	private GameObject _pooledObject;

	[FormerlySerializedAs("_offset")]
	public Vector3 offset;

	[FormerlySerializedAs("_upright")]
	public bool upright;

	[FormerlySerializedAs("_facePlayer")]
	public bool facePlayer;

	[FormerlySerializedAs("_chanceToSpawn")]
	[Range(0f, 100f)]
	public int chanceToSpawn = 100;

	private int _pooledObjectHash;

	private void Awake()
	{
		if (!(_pooledObject == null))
		{
			_pooledObjectHash = PoolUtils.GameObjHashCode(_pooledObject);
		}
	}

	public void SpawnObject()
	{
		if (ShouldSpawn() && !(_pooledObject == null) && !(_spawnLocation == null))
		{
			GameObject obj = ObjectPools.instance.Instantiate(_pooledObjectHash);
			obj.transform.position = SpawnLocation();
			obj.transform.rotation = SpawnRotation();
			obj.transform.localScale = base.transform.lossyScale;
		}
	}

	private Vector3 SpawnLocation()
	{
		return _spawnLocation.transform.position + offset;
	}

	private Quaternion SpawnRotation()
	{
		Quaternion result = _spawnLocation.transform.rotation;
		if (facePlayer)
		{
			result = Quaternion.LookRotation(GTPlayer.Instance.headCollider.transform.position - _spawnLocation.transform.position);
		}
		if (upright)
		{
			result.eulerAngles = new Vector3(0f, result.eulerAngles.y, 0f);
		}
		return result;
	}

	private bool ShouldSpawn()
	{
		return Random.Range(0, 100) < chanceToSpawn;
	}
}
