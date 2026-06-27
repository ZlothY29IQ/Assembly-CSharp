using System;
using UnityEngine;

public class EventDialogueBank : MonoBehaviour
{
	[Serializable]
	public struct EventDialogueBankEntry
	{
		public AudioClip audioClip;

		public AudioSource audioSource;
	}

	[SerializeField]
	private EventDialogueBankEntry[] bank;

	[SerializeField]
	private AudioSource defaultAudioSource;

	[SerializeField]
	private float index;

	private int _index = -1;

	private void Awake()
	{
		for (int i = 0; i < bank.Length; i++)
		{
			if (bank[i].audioSource == null)
			{
				bank[i].audioSource = defaultAudioSource;
			}
			bank[i].audioSource.playOnAwake = false;
			bank[i].audioSource.gameObject.SetActive(value: true);
		}
	}

	private void LateUpdate()
	{
		if (_index == Mathf.FloorToInt(index) - 1)
		{
			return;
		}
		_index = Mathf.FloorToInt(index) - 1;
		if (_index >= 0 && _index < bank.Length && !(bank[_index].audioClip == null) && !(bank[_index].audioSource == null))
		{
			if (bank[_index].audioSource.isPlaying)
			{
				bank[_index].audioSource.Stop();
			}
			bank[_index].audioSource.clip = bank[_index].audioClip;
			bank[_index].audioSource.Play();
		}
	}
}
