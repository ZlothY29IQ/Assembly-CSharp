using Photon.Pun;
using UnityEngine;

namespace GorillaNetworking;

public class GorillaNetworkDisconnectTrigger : GorillaTriggerBox
{
	public PhotonNetworkController photonNetworkController;

	public GameObject offlineVRRig;

	public GameObject makeSureThisIsEnabled;

	public GameObject[] makeSureTheseAreEnabled;

	public string componentTypeToRemove;

	public GameObject componentTarget;

	public override void OnBoxTriggered()
	{
		base.OnBoxTriggered();
		if (makeSureThisIsEnabled != null)
		{
			makeSureThisIsEnabled.SetActive(value: true);
		}
		GameObject[] array = makeSureTheseAreEnabled;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		if (PhotonNetwork.InRoom)
		{
			if (componentTypeToRemove != "" && componentTarget.GetComponent(componentTypeToRemove) != null)
			{
				Object.Destroy(componentTarget.GetComponent(componentTypeToRemove));
			}
			PhotonNetwork.Disconnect();
			SkinnedMeshRenderer[] array2 = photonNetworkController.offlineVRRig;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = true;
			}
			PhotonNetwork.ConnectUsingSettings();
		}
	}
}
