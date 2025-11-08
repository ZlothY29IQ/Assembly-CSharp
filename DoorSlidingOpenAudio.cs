using UnityEngine;

public class DoorSlidingOpenAudio : MonoBehaviour, IBuildValidation, ITickSystemTick
{
	public GhostLabButton button;

	public AudioSource audioSource;

	bool ITickSystemTick.TickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	public bool BuildValidationCheck()
	{
		if (button == null)
		{
			Debug.LogError("reference button missing for doorslidingopenaudio", base.gameObject);
			return false;
		}
		if (audioSource == null)
		{
			Debug.LogError("missing audio source on doorslidingopenaudio", base.gameObject);
			return false;
		}
		return true;
	}

	void ITickSystemTick.Tick()
	{
		if (button.ghostLab.IsDoorMoving(button.forSingleDoor, button.buttonIndex))
		{
			if (!audioSource.isPlaying)
			{
				audioSource.time = 0f;
				audioSource.GTPlay();
			}
		}
		else if (audioSource.isPlaying)
		{
			audioSource.time = 0f;
			audioSource.GTStop();
		}
	}
}
