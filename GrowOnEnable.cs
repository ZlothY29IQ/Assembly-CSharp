using UnityEngine;

public class GrowOnEnable : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private float growDuration = 1f;

	[SerializeField]
	private AnimationCurves.EaseType easeType = AnimationCurves.EaseType.EaseOutBack;

	private AnimationCurve _curve;

	private Vector3 _targetScale;

	private float _lerpVal;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		_targetScale = base.transform.localScale;
	}

	private void OnEnable()
	{
		_lerpVal = 0f;
		_curve = AnimationCurves.GetCurveForEase(easeType);
		UpdateScale();
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		base.transform.localScale = _targetScale;
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		_lerpVal = Mathf.Clamp01(_lerpVal + Time.deltaTime / growDuration);
		UpdateScale();
		if (_lerpVal >= 1f)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	private void UpdateScale()
	{
		base.transform.localScale = _targetScale * _curve.Evaluate(_lerpVal);
	}
}
