using System;
using UnityEngine;

public class LerpTask<T>
{
	public float elapsed;

	public float duration;

	public T lerpFrom;

	public T lerpTo;

	public Action<T, T, float> onLerp;

	public Action onLerpEnd;

	public bool active;

	public void Reset()
	{
		onLerp(lerpFrom, lerpTo, 0f);
		active = false;
		elapsed = 0f;
	}

	public void Start(T from, T to, float duration)
	{
		lerpFrom = from;
		lerpTo = to;
		this.duration = duration;
		elapsed = 0f;
		active = true;
	}

	public void Finish()
	{
		onLerp(lerpFrom, lerpTo, 1f);
		onLerpEnd?.Invoke();
		active = false;
		elapsed = 0f;
	}

	public void Update()
	{
		if (active)
		{
			float deltaTime = Time.deltaTime;
			if (elapsed < duration)
			{
				float arg = ((elapsed + deltaTime >= duration) ? 1f : (elapsed / duration));
				onLerp(lerpFrom, lerpTo, arg);
				elapsed += deltaTime;
			}
			else
			{
				Finish();
			}
		}
	}
}
