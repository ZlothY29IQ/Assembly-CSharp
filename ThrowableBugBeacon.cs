using UnityEngine;

public class ThrowableBugBeacon : MonoBehaviour
{
	public delegate void ThrowableBugBeaconEvent(ThrowableBugBeacon tbb);

	public delegate void ThrowableBugBeaconFloatEvent(ThrowableBugBeacon tbb, float f);

	[SerializeField]
	private float range;

	[SerializeField]
	private ThrowableBug.BugName bugName;

	public ThrowableBug.BugName BugName => bugName;

	public float Range => range;

	public static event ThrowableBugBeaconEvent OnCall;

	public static event ThrowableBugBeaconEvent OnDismiss;

	public static event ThrowableBugBeaconEvent OnLock;

	public static event ThrowableBugBeaconEvent OnUnlock;

	public static event ThrowableBugBeaconFloatEvent OnChangeSpeedMultiplier;

	public void Call()
	{
		if (ThrowableBugBeacon.OnCall != null)
		{
			ThrowableBugBeacon.OnCall(this);
		}
	}

	public void Dismiss()
	{
		if (ThrowableBugBeacon.OnDismiss != null)
		{
			ThrowableBugBeacon.OnDismiss(this);
		}
	}

	public void Lock()
	{
		if (ThrowableBugBeacon.OnLock != null)
		{
			ThrowableBugBeacon.OnLock(this);
		}
	}

	public void Unlock()
	{
		if (ThrowableBugBeacon.OnUnlock != null)
		{
			ThrowableBugBeacon.OnUnlock(this);
		}
	}

	public void ChangeSpeedMultiplier(float f)
	{
		if (ThrowableBugBeacon.OnChangeSpeedMultiplier != null)
		{
			ThrowableBugBeacon.OnChangeSpeedMultiplier(this, f);
		}
	}

	private void OnDisable()
	{
		if (ThrowableBugBeacon.OnUnlock != null)
		{
			ThrowableBugBeacon.OnUnlock(this);
		}
	}
}
