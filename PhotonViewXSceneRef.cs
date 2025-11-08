using Photon.Pun;
using UnityEngine;

public class PhotonViewXSceneRef : MonoBehaviour
{
	[SerializeField]
	private XSceneRef reference;

	public PhotonView photonView
	{
		get
		{
			if (reference.TryResolve(out PhotonView result))
			{
				return result;
			}
			return null;
		}
	}
}
