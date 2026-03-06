using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

internal class OwnershipGuardHandler : IPunOwnershipCallbacks
{
	private static HashSet<PhotonView> guardedViews;

	private static readonly OwnershipGuardHandler callbackInstance;

	static OwnershipGuardHandler()
	{
		guardedViews = new HashSet<PhotonView>();
		callbackInstance = new OwnershipGuardHandler();
		PhotonNetwork.AddCallbackTarget(callbackInstance);
	}

	internal static void RegisterView(PhotonView view)
	{
		if (!(view == null) && !guardedViews.Contains(view))
		{
			guardedViews.Add(view);
		}
	}

	internal static void RegisterViews(PhotonView[] photonViews)
	{
		for (int i = 0; i < photonViews.Length; i++)
		{
			RegisterView(photonViews[i]);
		}
	}

	internal static void RemoveView(PhotonView view)
	{
		if (!(view == null))
		{
			guardedViews.Remove(view);
		}
	}

	internal static void RemoveViews(PhotonView[] photonViews)
	{
		for (int i = 0; i < photonViews.Length; i++)
		{
			RemoveView(photonViews[i]);
		}
	}

	void IPunOwnershipCallbacks.OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
	{
		if (!guardedViews.Contains(targetView))
		{
			return;
		}
		if (targetView.IsRoomView)
		{
			if (targetView.Owner != PhotonNetwork.MasterClient)
			{
				targetView.OwnerActorNr = 0;
				targetView.ControllerActorNr = 0;
			}
		}
		else if (targetView.OwnerActorNr != targetView.CreatorActorNr || targetView.ControllerActorNr != targetView.CreatorActorNr)
		{
			targetView.OwnerActorNr = targetView.CreatorActorNr;
			targetView.ControllerActorNr = targetView.CreatorActorNr;
		}
	}

	void IPunOwnershipCallbacks.OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
	{
	}

	void IPunOwnershipCallbacks.OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
	{
	}
}
