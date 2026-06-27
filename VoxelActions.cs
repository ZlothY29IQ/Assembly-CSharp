using PlayFab.Internal;
using UnityEngine;
using UnityEngine.Serialization;

public class VoxelActions : SingletonMonoBehaviour<VoxelActions>
{
	[SerializeField]
	private GameObject _hitFX;

	[FormerlySerializedAs("_digFX")]
	[SerializeField]
	private GameObject _dirtDigFX;

	[FormerlySerializedAs("_bigDigFX")]
	[SerializeField]
	private GameObject _dirtDigBigFX;

	[SerializeField]
	private GameObject _stoneDigFX;

	[SerializeField]
	private GameObject _stoneDigBigFX;

	public void PlayDigFX(Vector3 position, Vector3 normal, int dirtAmount, int stoneAmount)
	{
		if (dirtAmount > 0)
		{
			Object.Instantiate((dirtAmount >= 20) ? _dirtDigBigFX : _dirtDigFX, position, Quaternion.LookRotation(normal));
		}
		if (stoneAmount > 0)
		{
			Object.Instantiate((dirtAmount >= 20) ? _stoneDigBigFX : _stoneDigFX, position, Quaternion.LookRotation(normal));
		}
	}
}
