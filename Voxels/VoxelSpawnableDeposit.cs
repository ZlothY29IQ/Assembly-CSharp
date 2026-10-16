using TMPro;
using UnityEngine;

namespace Voxels;

public class VoxelSpawnableDeposit : SpawnableEntityDeposit
{
	[SerializeField]
	private TMP_Text text;

	private void SetCounter(int count)
	{
		text.text = count.ToString();
	}

	public override void OnEntityStateChange(long prevState, long newState)
	{
		base.OnEntityStateChange(prevState, newState);
		SetCounter(Mathf.Max(0, (int)newState));
	}
}
