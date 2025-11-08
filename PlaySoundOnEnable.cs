using System.Collections;
using UnityEngine;

public class PlaySoundOnEnable : MonoBehaviour
{
	[SerializeField]
	private AudioSource _source;

	[SerializeField]
	private AudioClip[] _clips;

	[SerializeField]
	private bool _loop;

	[SerializeField]
	private Vector2 _loopDelay;

	private void Reset()
	{
		_source = GetComponent<AudioSource>();
		if ((bool)_source)
		{
			_source.playOnAwake = false;
		}
	}

	private void OnEnable()
	{
		Play();
	}

	private void OnDisable()
	{
		Stop();
	}

	public void Play()
	{
		if (_loop && _clips.Length == 1 && _loopDelay == Vector2.zero)
		{
			_source.clip = _clips[0];
			_source.loop = true;
			_source.GTPlay();
			return;
		}
		_source.loop = false;
		if (_loop)
		{
			StartCoroutine(DoLoop());
			return;
		}
		_source.clip = _clips[Random.Range(0, _clips.Length)];
		_source.GTPlay();
	}

	private IEnumerator DoLoop()
	{
		while (base.enabled)
		{
			_source.clip = _clips[Random.Range(0, _clips.Length)];
			_source.GTPlay();
			while (_source.isPlaying)
			{
				yield return null;
			}
			float num = Random.Range(_loopDelay.x, _loopDelay.y);
			if (num > 0f)
			{
				float waitEndTime = Time.time + num;
				while (Time.time < waitEndTime)
				{
					yield return null;
				}
			}
		}
	}

	public void Stop()
	{
		_source.GTStop();
		_source.loop = false;
	}
}
