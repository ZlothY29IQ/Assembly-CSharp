using UnityEngine;

public class MusicManagerEventTargets : MonoBehaviour
{
	public void StopAllMusic()
	{
		StopAllMusic(null);
	}

	public void StopAllMusic(AudioClip clip)
	{
		MusicManager.StopAllMusic(clip);
	}

	public void Lock(bool v)
	{
		MusicManager.LockMusic(v);
	}
}
