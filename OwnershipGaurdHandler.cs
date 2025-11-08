using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

internal class OwnershipGaurdHandler : IPunOwnershipCallbacks
{
	private static HashSet<PhotonView> gaurdedViews;

	private static readonly OwnershipGaurdHandler callbackInstance;

	static OwnershipGaurdHandler()
	{
		gaurdedViews = new HashSet<PhotonView>();
		callbackInstance = new OwnershipGaurdHandler();
		PhotonNetwork.AddCallbackTarget(callbackInstance);
	}

	internal static void RegisterView(PhotonView view)
	{
		if (!(view == null) && !gaurdedViews.Contains(view))
		{
			gaurdedViews.Add(view);
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
			gaurdedViews.Remove(view);
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
		if (!gaurdedViews.Contains(targetView))
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
