using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

public class EnvItem : MonoBehaviour, IPunInstantiateMagicCallback
{
	public int spawnedByPhotonViewId;

	public void OnEnable()
	{
	}

	public void OnDisable()
	{
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		spawnedByPhotonViewId = (int)instantiationData[0];
	}
}
