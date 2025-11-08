using GorillaLocomotion;
using UnityEngine;

public class RaceCheckpoint : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer banner;

	[SerializeField]
	private Material activeCheckpointMat;

	[SerializeField]
	private Material wrongCheckpointMat;

	[SerializeField]
	private SoundBankPlayer checkpointSound;

	[SerializeField]
	private SoundBankPlayer wrongCheckpointSound;

	private RaceCheckpointManager manager;

	private int checkpointIndex;

	private bool isCorrect;

	public void Init(RaceCheckpointManager manager, int index)
	{
		this.manager = manager;
		checkpointIndex = index;
		SetIsCorrectCheckpoint(index == 0);
	}

	public void SetIsCorrectCheckpoint(bool isCorrect)
	{
		this.isCorrect = isCorrect;
		banner.sharedMaterial = (isCorrect ? activeCheckpointMat : wrongCheckpointMat);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(other != GTPlayer.Instance.headCollider))
		{
			if (isCorrect)
			{
				manager.OnCheckpointReached(checkpointIndex, checkpointSound);
			}
			else
			{
				wrongCheckpointSound.Play();
			}
		}
	}
}
