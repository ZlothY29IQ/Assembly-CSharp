using UnityEngine;

public class CrossFadeAudioSources : MonoBehaviour, IRangedVariable<float>, IVariable<float>, IVariable
{
	[SerializeField]
	private float _lerp;

	[SerializeField]
	private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Space]
	[SerializeField]
	private AudioSource source1;

	[SerializeField]
	private AudioSource source2;

	[Space]
	public bool lerpByClipLength;

	public bool tween;

	public float tweenSpeed = 16f;

	private float _lastT;

	public float Min
	{
		get
		{
			return 0f;
		}
		set
		{
		}
	}

	public float Max
	{
		get
		{
			return 1f;
		}
		set
		{
		}
	}

	public float Range => 1f;

	public AnimationCurve Curve => _curve;

	public void Play()
	{
		if ((bool)source1)
		{
			source1.Play();
		}
		if ((bool)source2)
		{
			source2.Play();
		}
	}

	public void Stop()
	{
		if ((bool)source1)
		{
			source1.Stop();
		}
		if ((bool)source2)
		{
			source2.Stop();
		}
	}

	private void Update()
	{
		if ((bool)source1 && (bool)source2)
		{
			float num = _curve.Evaluate(_lerp);
			float num2 = (_lastT = ((!tween) ? (lerpByClipLength ? _curve.Evaluate((float)source1.timeSamples / (float)source1.clip.samples) : num) : MathUtils.Xlerp(_lastT, num, Time.deltaTime, tweenSpeed)));
			source2.volume = num2;
			source1.volume = 1f - num2;
		}
	}

	public float Get()
	{
		return _lerp;
	}

	public void Set(float f)
	{
		_lerp = Mathf.Clamp01(f);
	}
}
