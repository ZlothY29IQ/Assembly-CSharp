using UnityEngine;

public class RaceCheckpointManager : MonoBehaviour
{
	[SerializeField]
	private RaceCheckpoint[] checkpoints;

	private RaceVisual visual;

	private void Start()
	{
		visual = GetComponent<RaceVisual>();
		for (int i = 0; i < checkpoints.Length; i++)
		{
			checkpoints[i].Init(this, i);
		}
		OnRaceEnd();
	}

	public void OnRaceStart()
	{
		for (int i = 0; i < checkpoints.Length; i++)
		{
			checkpoints[i].SetIsCorrectCheckpoint(i == 0);
		}
	}

	public void OnRaceEnd()
	{
		for (int i = 0; i < checkpoints.Length; i++)
		{
			checkpoints[i].SetIsCorrectCheckpoint(isCorrect: false);
		}
	}

	public void OnCheckpointReached(int index, SoundBankPlayer checkpointSound)
	{
		checkpoints[index].SetIsCorrectCheckpoint(isCorrect: false);
		checkpoints[(index + 1) % checkpoints.Length].SetIsCorrectCheckpoint(isCorrect: true);
		visual.OnCheckpointPassed(index, checkpointSound);
	}

	public bool IsPlayerNearCheckpoint(VRRig player, int checkpointIdx)
	{
		if (checkpointIdx >= 0 && checkpointIdx < checkpoints.Length)
		{
			return player.IsPositionInRange(checkpoints[checkpointIdx].transform.position, 6f);
		}
		return false;
	}
}
