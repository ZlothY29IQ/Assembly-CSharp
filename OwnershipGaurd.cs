using Photon.Pun;
using UnityEngine;

internal class OwnershipGaurd : MonoBehaviour
{
	[SerializeField]
	private PhotonView[] NetViews;

	[SerializeField]
	private bool autoRegisterAll = true;

	private void Start()
	{
		if (autoRegisterAll)
		{
			NetViews = GetComponents<PhotonView>();
		}
		if (NetViews != null)
		{
			OwnershipGuardHandler.RegisterViews(NetViews);
		}
	}

	private void OnDestroy()
	{
		if (NetViews != null)
		{
			OwnershipGuardHandler.RemoveViews(NetViews);
		}
	}
}
