using GorillaExtensions;
using UnityEngine;

public class CrittersActorDeposit : MonoBehaviour
{
	public CrittersActor attachPoint;

	public CrittersActor.CrittersActorType actorType;

	public bool disableGrabOnAttach;

	public bool allowMultiAttach;

	public bool snapOnAttach;

	private CrittersActor currentAttach;

	public void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody.IsNotNull())
		{
			CrittersActor component = other.attachedRigidbody.GetComponent<CrittersActor>();
			if (CrittersManager.instance.LocalAuthority() && component.IsNotNull() && CanDeposit(component) && IsAttachAvailable())
			{
				HandleDeposit(component);
			}
		}
	}

	protected virtual bool CanDeposit(CrittersActor depositActor)
	{
		if (depositActor.crittersActorType == actorType)
		{
			if (CrittersManager.instance.actorById.TryGetValue(depositActor.parentActorId, out var value))
			{
				return value.crittersActorType == CrittersActor.CrittersActorType.Grabber;
			}
			return depositActor.parentActorId == -1;
		}
		return false;
	}

	private bool IsAttachAvailable()
	{
		if (allowMultiAttach)
		{
			return true;
		}
		return currentAttach == null;
	}

	protected virtual void HandleDeposit(CrittersActor depositedActor)
	{
		currentAttach = depositedActor;
		depositedActor.ReleasedEvent.AddListener(HandleDetach);
		CrittersActor grabbingActor = attachPoint;
		bool positionOverride = snapOnAttach;
		bool disableGrabbing = disableGrabOnAttach;
		depositedActor.GrabbedBy(grabbingActor, positionOverride, default(Quaternion), default(Vector3), disableGrabbing);
	}

	protected virtual void HandleDetach(CrittersActor detachingActor)
	{
		currentAttach = null;
	}
}
