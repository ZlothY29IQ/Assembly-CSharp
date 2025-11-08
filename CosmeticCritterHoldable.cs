using GorillaNetworking;
using UnityEngine;

public abstract class CosmeticCritterHoldable : MonoBehaviour
{
	protected TransferrableObject transferrableObject;

	protected CallLimiter callLimiter;

	public int OwnerID { get; private set; }

	public bool IsLocal => transferrableObject.IsLocalObject();

	public bool OwningPlayerMatches(PhotonMessageInfoWrapped info)
	{
		return transferrableObject.targetRig.creator == info.Sender;
	}

	protected virtual CallLimiter CreateCallLimiter()
	{
		return new CallLimiter(10, 2f);
	}

	public void ResetCallLimiter()
	{
		callLimiter.Reset();
	}

	private void TrySetID()
	{
		if (IsLocal)
		{
			PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
			if (instance != null)
			{
				OwnerID = (instance.GetPlayFabPlayerId() + GetType()).GetStaticHash();
			}
		}
		else if (transferrableObject.targetRig != null && transferrableObject.targetRig.creator != null)
		{
			OwnerID = (transferrableObject.targetRig.creator.UserId + GetType()).GetStaticHash();
		}
	}

	protected virtual void Awake()
	{
		transferrableObject = GetComponentInParent<TransferrableObject>();
		callLimiter = CreateCallLimiter();
		if (IsLocal)
		{
			CosmeticCritterManager.Instance.RegisterLocalHoldable(this);
		}
	}

	protected virtual void OnEnable()
	{
		TrySetID();
	}

	protected virtual void OnDisable()
	{
	}
}
