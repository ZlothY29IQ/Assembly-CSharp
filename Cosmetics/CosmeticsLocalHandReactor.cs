using UnityEngine;
using UnityEngine.Events;

namespace Cosmetics;

public class CosmeticsLocalHandReactor : MonoBehaviour
{
	[SerializeField]
	private float hapticStrength = 0.2f;

	[SerializeField]
	private float hapticDuration = 0.2f;

	[Tooltip("The distance threshold (in meters) for triggering the interaction.\nIf the hand enters this range, onTrigger is fired.")]
	public float proximityThreshold = 0.15f;

	[Tooltip("Minimum time (in seconds) between consecutive triggers.\n")]
	[SerializeField]
	private float cooldownTime = 0.5f;

	public UnityEvent<bool> onTrigger;

	private VRRig ownerRig;

	private bool ownerIsLocal;

	private float lastTriggerTime = float.MinValue;

	private readonly Collider[] colliders = new Collider[1];

	private LayerMask handLayer = 1024;

	protected void Awake()
	{
		ownerRig = GetComponentInParent<VRRig>();
		if (ownerRig == null)
		{
			GorillaTagger componentInParent = GetComponentInParent<GorillaTagger>();
			if (componentInParent != null)
			{
				ownerRig = componentInParent.offlineVRRig;
				ownerIsLocal = ownerRig != null;
			}
		}
		if (ownerRig == null)
		{
			Debug.LogError("TriggerToggler: Disabling cannot find VRRig.");
			base.enabled = false;
		}
	}

	protected void LateUpdate()
	{
		if (!ownerIsLocal || Time.time < lastTriggerTime + cooldownTime)
		{
			return;
		}
		Transform transform = base.transform;
		if (Physics.OverlapSphereNonAlloc(base.transform.position, proximityThreshold * transform.lossyScale.x, colliders, handLayer) > 0)
		{
			GorillaTriggerColliderHandIndicator component = colliders[0].GetComponent<GorillaTriggerColliderHandIndicator>();
			if (component != null)
			{
				GorillaTagger.Instance.StartVibration(component.isLeftHand, hapticStrength, hapticDuration);
				onTrigger?.Invoke(component.isLeftHand);
				lastTriggerTime = Time.time;
			}
		}
	}
}
