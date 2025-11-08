using UnityEngine;

public class SafeOwnershipRequestsCallbacks : MonoBehaviour, IRequestableOwnershipGuardCallbacks
{
	[SerializeField]
	private RequestableOwnershipGuard _requestableOwnershipGuard;

	private void Awake()
	{
		_requestableOwnershipGuard.AddCallbackTarget(this);
	}

	void IRequestableOwnershipGuardCallbacks.OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
	}

	bool IRequestableOwnershipGuardCallbacks.OnOwnershipRequest(NetPlayer fromPlayer)
	{
		return false;
	}

	void IRequestableOwnershipGuardCallbacks.OnMyOwnerLeft()
	{
	}

	bool IRequestableOwnershipGuardCallbacks.OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return false;
	}

	void IRequestableOwnershipGuardCallbacks.OnMyCreatorLeft()
	{
	}
}
