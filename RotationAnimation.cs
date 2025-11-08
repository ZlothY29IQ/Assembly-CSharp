using UnityEngine;

public class RotationAnimation : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private AnimationCurve x;

	[SerializeField]
	private AnimationCurve y;

	[SerializeField]
	private AnimationCurve z;

	[SerializeField]
	private AnimationCurve attack;

	[SerializeField]
	private AnimationCurve release;

	[SerializeField]
	private Vector3 amplitude = Vector3.one;

	[SerializeField]
	private Vector3 period = Vector3.one;

	private Quaternion baseRotation;

	private float baseTime;

	private float releaseTime;

	private bool releaseSet;

	public bool TickRunning { get; set; }

	public void Tick()
	{
		Vector3 zero = Vector3.zero;
		zero.x = amplitude.x * x.Evaluate((Time.time - baseTime) * period.x % 1f);
		zero.y = amplitude.y * y.Evaluate((Time.time - baseTime) * period.y % 1f);
		zero.z = amplitude.z * z.Evaluate((Time.time - baseTime) * period.z % 1f);
		if (releaseSet)
		{
			float num = release.Evaluate(Time.time - releaseTime);
			zero *= num;
			if (num < Mathf.Epsilon)
			{
				base.enabled = false;
			}
		}
		base.transform.localRotation = Quaternion.Euler(zero) * baseRotation;
	}

	private void Awake()
	{
		baseRotation = base.transform.localRotation;
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
		releaseSet = false;
		baseTime = Time.time;
	}

	public void ReleaseToDisable()
	{
		releaseSet = true;
		releaseTime = Time.time;
	}

	public void CancelRelease()
	{
		releaseSet = false;
	}

	private void OnDisable()
	{
		base.transform.localRotation = baseRotation;
		TickSystem<object>.RemoveTickCallback(this);
	}
}
