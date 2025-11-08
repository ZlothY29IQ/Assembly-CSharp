using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class ChargeableCosmeticEffects : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private float maxChargeSeconds = 1f;

	[SerializeField]
	private float chargeGainSpeed = 1f;

	[SerializeField]
	private float chargeLossSpeed = 1f;

	[Tooltip("This will remap the internal charge output to whatever you set. The remapped value will be output by 'whileCharging' and the 'continuousProperties' (keep in mind that the remapped value will then be used as an INPUT for the curves on each ContinuousProperty).\n\nIt should start at (0,0) and end at (1,1).\n\nDisabled if there are no ContinuousProperties and no whileCharging event callbacks.")]
	[SerializeField]
	private AnimationCurve masterChargeRemapCurve = AnimationCurves.Linear;

	[SerializeField]
	private bool isCharging;

	[SerializeField]
	private ContinuousPropertyArray continuousProperties;

	[SerializeField]
	private UnityEvent<float> whileCharging;

	[SerializeField]
	private UnityEvent onMaxCharge;

	[SerializeField]
	private UnityEvent onNoCharge;

	private float chargeTime;

	private float inverseMaxChargeSeconds;

	private bool hasFractionalsCached;

	public bool TickRunning { get; set; }

	private bool HasFractionals()
	{
		if (continuousProperties.Count <= 0)
		{
			return whileCharging.GetPersistentEventCount() > 0;
		}
		return true;
	}

	private void Awake()
	{
		inverseMaxChargeSeconds = 1f / maxChargeSeconds;
		hasFractionalsCached = HasFractionals();
	}

	public void SetMaxChargeSeconds(float s)
	{
		maxChargeSeconds = s;
		inverseMaxChargeSeconds = 1f / maxChargeSeconds;
		SetChargeTime(chargeTime);
	}

	public void SetChargeState(bool state)
	{
		if (isCharging != state)
		{
			TickSystem<object>.AddTickCallback(this);
			isCharging = state;
		}
	}

	public void StartCharging()
	{
		SetChargeState(state: true);
	}

	public void StopCharging()
	{
		SetChargeState(state: false);
	}

	public void ToggleCharging()
	{
		SetChargeState(!isCharging);
	}

	public void SetChargeTime(float t)
	{
		if (t >= maxChargeSeconds)
		{
			if (chargeTime < maxChargeSeconds)
			{
				RunMaxCharge();
			}
			return;
		}
		if (t <= 0f)
		{
			if (chargeTime > 0f)
			{
				RunNoCharge();
			}
			return;
		}
		TickSystem<object>.AddTickCallback(this);
		chargeTime = t;
		if (hasFractionalsCached)
		{
			RunChargeFrac();
		}
	}

	public void SetChargeFrac(float f)
	{
		SetChargeTime(f * maxChargeSeconds);
	}

	public void EmptyCharge()
	{
		SetChargeTime(0f);
	}

	public void FillCharge()
	{
		SetChargeTime(maxChargeSeconds);
	}

	public void EmptyAndStop()
	{
		isCharging = false;
		EmptyCharge();
	}

	public void FillAndStop()
	{
		StopCharging();
		FillCharge();
	}

	public void EmptyAndStart()
	{
		StartCharging();
		EmptyCharge();
	}

	public void FillAndStart()
	{
		isCharging = true;
		FillCharge();
	}

	private void OnEnable()
	{
		if ((chargeTime <= 0f && isCharging) || (chargeTime >= maxChargeSeconds && !isCharging) || (chargeTime > 0f && chargeTime < maxChargeSeconds))
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void RunMaxCharge()
	{
		if (isCharging)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
		else
		{
			TickSystem<object>.AddTickCallback(this);
		}
		chargeTime = maxChargeSeconds;
		onMaxCharge?.Invoke();
		whileCharging?.Invoke(1f);
		continuousProperties.ApplyAll(1f);
	}

	private void RunNoCharge()
	{
		if (!isCharging)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
		else
		{
			TickSystem<object>.AddTickCallback(this);
		}
		chargeTime = 0f;
		onNoCharge?.Invoke();
		whileCharging?.Invoke(0f);
		continuousProperties.ApplyAll(0f);
	}

	private void RunChargeFrac()
	{
		float num = masterChargeRemapCurve.Evaluate(chargeTime * inverseMaxChargeSeconds);
		whileCharging?.Invoke(num);
		continuousProperties.ApplyAll(num);
	}

	public void Tick()
	{
		if (isCharging && chargeTime < maxChargeSeconds)
		{
			chargeTime += Time.deltaTime * chargeGainSpeed;
			if (chargeTime >= maxChargeSeconds)
			{
				RunMaxCharge();
			}
			else if (hasFractionalsCached)
			{
				RunChargeFrac();
			}
		}
		else if (!isCharging && chargeTime > 0f)
		{
			chargeTime -= Time.deltaTime * chargeLossSpeed;
			if (chargeTime <= 0f)
			{
				RunNoCharge();
			}
			else if (hasFractionalsCached)
			{
				RunChargeFrac();
			}
		}
	}
}
