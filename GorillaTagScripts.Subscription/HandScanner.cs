using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription;

public class HandScanner : ObservableBehavior, IClickable
{
	[SerializeField]
	private float scanTime = 1f;

	public UnityEvent<NetPlayer> onHandScanStart;

	public UnityEvent<NetPlayer> onHandScanAbort;

	public UnityEvent<NetPlayer> onHandScanSuccess;

	public UnityEvent onHandScanInRange;

	public UnityEvent onHandScanOutOfRange;

	private VRRig scanningRig;

	private float scanStart;

	protected override void ObservableSliceUpdate()
	{
		if (!(scanningRig == null) && Time.time - scanStart > scanTime)
		{
			onHandScanSuccess?.Invoke(scanningRig.creator);
			scanningRig = null;
		}
	}

	protected override void OnBecameObservable()
	{
		onHandScanInRange?.Invoke();
	}

	protected override void OnLostObservable()
	{
		onHandScanOutOfRange?.Invoke();
	}

	private void OnTriggerEnter(Collider other)
	{
		SIScannableHand component = other.GetComponent<SIScannableHand>();
		if (!(component == null))
		{
			VRRig componentInParent = component.GetComponentInParent<VRRig>();
			if (componentInParent != null && componentInParent.isLocal)
			{
				scanningRig = componentInParent;
				scanStart = Time.time;
				onHandScanStart?.Invoke(scanningRig.creator);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		SIScannableHand component = other.GetComponent<SIScannableHand>();
		if (!(component == null))
		{
			VRRig componentInParent = component.GetComponentInParent<VRRig>();
			if (componentInParent != null && componentInParent == scanningRig && componentInParent.isLocal)
			{
				onHandScanAbort?.Invoke(scanningRig.creator);
				scanningRig = null;
			}
		}
	}

	public void Click(bool leftHand = false)
	{
		onHandScanStart?.Invoke(VRRig.LocalRig.creator);
		onHandScanSuccess?.Invoke(VRRig.LocalRig.creator);
	}
}
