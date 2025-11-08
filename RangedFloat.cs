using UnityEngine;

public class RangedFloat : MonoBehaviour, IRangedVariable<float>, IVariable<float>, IVariable
{
	[SerializeField]
	private float _value = 0.5f;

	[SerializeField]
	private float _min;

	[SerializeField]
	private float _max = 1f;

	[SerializeField]
	private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve Curve => _curve;

	public float Range => _max - _min;

	public float Min
	{
		get
		{
			return _min;
		}
		set
		{
			_min = value;
		}
	}

	public float Max
	{
		get
		{
			return _max;
		}
		set
		{
			_max = value;
		}
	}

	public float normalized
	{
		get
		{
			if (!Range.Approx0())
			{
				return (_value - _min) / (_max - Min);
			}
			return 0f;
		}
		set
		{
			_value = _min + Mathf.Clamp01(value) * (_max - _min);
		}
	}

	public float curved => _min + _curve.Evaluate(normalized) * (_max - _min);

	public float Get()
	{
		return _value;
	}

	public void Set(float f)
	{
		_value = Mathf.Clamp(f, _min, _max);
	}
}
