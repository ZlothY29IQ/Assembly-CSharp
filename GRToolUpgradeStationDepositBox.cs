using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class GRToolUpgradeStationDepositBox : MonoBehaviour
{
	public GRToolUpgradeStation upgradeStation;

	public void OnTriggerEnter(Collider other)
	{
		GRTool component = other.attachedRigidbody.GetComponent<GRTool>();
		if (component.IsNotNull() && component.gameEntity.IsNotNull() && component.gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && component.gameEntity.IsHeldByLocalPlayer())
		{
			Debug.LogError("Tool Deposited");
			upgradeStation.ToolInserted(component);
		}
	}
}
