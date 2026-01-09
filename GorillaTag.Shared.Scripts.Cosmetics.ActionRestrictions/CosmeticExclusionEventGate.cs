using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public class CosmeticExclusionEventGate : MonoBehaviour
{
	[Header("Context")]
	[Tooltip("Optional effect source.\nIf set and has CosmeticExclusionSource, world position will be checked.")]
	[SerializeField]
	private GameObject effectSource;

	[Header("Forwarded Events")]
	[SerializeField]
	private UnityEvent onNormal;

	[SerializeField]
	private UnityEvent onRestricted;

	private VRRig ownerRig;

	private void Awake()
	{
		ownerRig = GetComponentInParent<VRRig>();
	}

	public void InvokeEvent()
	{
		if (CosmeticExclusionQuery.IsRestricted(ownerRig, effectSource))
		{
			onRestricted?.Invoke();
		}
		else
		{
			onNormal?.Invoke();
		}
	}
}
