using GorillaExtensions;
using GorillaTag;
using UnityEngine;

public class LckEntitlementsNetworked : MonoBehaviour
{
	[SerializeField]
	private VRRigSerializer m_rigNetworkController;

	public void Awake()
	{
		if (m_rigNetworkController.IsNull())
		{
			m_rigNetworkController = GetComponentInParent<VRRigSerializer>();
		}
		if (m_rigNetworkController.IsNull())
		{
			Debug.LogError("LCK: Unable to find VRRigSerializer for LckEntitlementsNetworked.");
		}
		else
		{
			m_rigNetworkController.SuccesfullSpawnEvent?.Add(new InAction<RigContainer, PhotonMessageInfoWrapped>(OnSuccessfulSpawn));
		}
	}

	private void OnSuccessfulSpawn(in RigContainer rig, in PhotonMessageInfoWrapped info)
	{
		if (LckEntitlementsManager.Instance == null)
		{
			Debug.LogError("LCK: LckEntitlementsManager.Instance is not available in the scene!");
			return;
		}
		string userId = m_rigNetworkController.VRRig.OwningNetPlayer.UserId;
		if (userId.IsNullOrEmpty())
		{
			Debug.LogError("LCK: owningUserId is null on spawn. Cannot process entitlements.");
		}
		else if (rig.Rig.isLocal)
		{
			LckEntitlementsManager.Instance.OnLocalPlayerSpawned(userId);
		}
		else
		{
			LckEntitlementsManager.Instance.OnRemotePlayerSpawned(userId);
		}
	}

	private void OnDestroy()
	{
		if (m_rigNetworkController != null && m_rigNetworkController.SuccesfullSpawnEvent != null)
		{
			m_rigNetworkController.SuccesfullSpawnEvent.Remove(new InAction<RigContainer, PhotonMessageInfoWrapped>(OnSuccessfulSpawn));
		}
	}
}
