using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class AttachPoint : MonoBehaviour
{
	public Transform attachPoint;

	public UnityAction onHookedChanged;

	private bool isHooked;

	private bool wasHooked;

	public bool inForest;

	private void Start()
	{
		base.transform.parent.parent = null;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (attachPoint.childCount == 0)
		{
			UpdateHookState(isHooked: false);
		}
		DecorativeItem componentInParent = other.GetComponentInParent<DecorativeItem>();
		if (!(componentInParent == null) && !componentInParent.InHand() && !IsHooked())
		{
			UpdateHookState(isHooked: true);
			componentInParent.SnapItem(snap: true, attachPoint.position);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		DecorativeItem componentInParent = other.GetComponentInParent<DecorativeItem>();
		if (!(componentInParent == null) && componentInParent.InHand())
		{
			UpdateHookState(isHooked: false);
			componentInParent.SnapItem(snap: false, Vector3.zero);
		}
	}

	private void UpdateHookState(bool isHooked)
	{
		SetIsHook(isHooked);
	}

	internal void SetIsHook(bool isHooked)
	{
		this.isHooked = isHooked;
		onHookedChanged?.Invoke();
	}

	public bool IsHooked()
	{
		if (!isHooked)
		{
			return attachPoint.childCount != 0;
		}
		return true;
	}
}
