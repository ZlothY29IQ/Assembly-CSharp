using Photon.Pun;
using UnityEngine;

public class NonCosmeticItemProvider : MonoBehaviour
{
	public enum ItemType
	{
		honeycomb
	}

	public GTZone zone;

	[Tooltip("only for honeycomb")]
	public bool useCondition;

	public int conditionThreshold;

	public ItemType itemType;

	private void OnTriggerEnter(Collider other)
	{
		GorillaTriggerColliderHandIndicator component = other.GetComponent<GorillaTriggerColliderHandIndicator>();
		if (component != null)
		{
			GorillaGameManager.instance.FindPlayerVRRig(NetworkSystem.Instance.LocalPlayer).netView.SendRPC("EnableNonCosmeticHandItemRPC", RpcTarget.All, true, component.isLeftHand);
		}
	}
}
