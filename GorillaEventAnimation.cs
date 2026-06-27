using System;
using UnityEngine;

public class GorillaEventAnimation : MonoBehaviour
{
	public Animation _animation;

	public float offsetTime;

	public int animationClipIndex;

	public AnimationClip[] clips;

	[NonSerialized]
	public int _clipIndex;

	private void Awake()
	{
		if (_animation == null)
		{
			_animation = GetComponentInChildren<Animation>();
		}
		_animation.playAutomatically = false;
		for (int i = 0; i < clips.Length; i++)
		{
			clips[i].legacy = true;
		}
	}

	private void OnDisable()
	{
		_animation.enabled = false;
	}

	public void PlayClipByIndex(int index, float startTime)
	{
		if (index >= 0 && index < clips.Length)
		{
			if (!_animation.enabled)
			{
				_animation.enabled = true;
			}
			AnimationClip animationClip = clips[index];
			if (_animation.GetClip(animationClip.name) == null)
			{
				_animation.AddClip(animationClip, animationClip.name);
			}
			_animation.Play(animationClip.name);
			_animation[animationClip.name].time = startTime;
			_clipIndex = index;
		}
	}
}
