using System;
using System.Collections;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription;

public class FreeCosmeticKiosk : MonoBehaviourPun
{
	[SerializeField]
	private bool _vimRequired = true;

	[SerializeField]
	private string _playfabId;

	[SerializeField]
	private string _toBeClaimedText = "HOLD TO CLAIM";

	[SerializeField]
	private string _alreadyClaimedText = "ALREADY CLAIMED";

	[SerializeField]
	private string _unclaimableText = "CANNOT CLAIM";

	[SerializeField]
	private TMP_Text _claimLabel;

	[SerializeField]
	private TMP_Text _nameLabel;

	[SerializeField]
	private GameObject _vimLogo;

	public UnityEvent OnCosmeticRedeemed;

	private CosmeticsController.CosmeticItem _cosmeticItem;

	private bool _initialized;

	private bool VimRequirementMet
	{
		get
		{
			if (_vimRequired)
			{
				return SubscriptionManager.IsLocalSubscribed();
			}
			return true;
		}
	}

	private bool HasBeenClaimed => CosmeticsController.instance.IsOwnedByPlayFabID(_playfabId);

	private void OnEnable()
	{
		if (!_initialized)
		{
			StartCoroutine(InitializeCoroutine());
		}
	}

	private void OnDisable()
	{
		SubscriptionManager.OnSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnSubscriptionData, new Action(UpdateState));
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnCosmeticsUpdated = (Action)Delegate.Remove(instance.OnCosmeticsUpdated, new Action(UpdateState));
	}

	private IEnumerator InitializeCoroutine()
	{
		while (CosmeticsController.instance == null || !CosmeticsController.instance.allCosmeticsDict_isInitialized)
		{
			yield return null;
		}
		SubscriptionManager.OnSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnSubscriptionData, new Action(UpdateState));
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnCosmeticsUpdated = (Action)Delegate.Combine(instance.OnCosmeticsUpdated, new Action(UpdateState));
		if (!CosmeticsController.instance.allCosmeticsDict.TryGetValue(_playfabId, out _cosmeticItem))
		{
			Debug.LogError("No cosmetic found with playfab ID \"" + _playfabId + "\".");
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			UpdateState();
			_initialized = true;
		}
	}

	public void OnHandScan(NetPlayer player)
	{
		if (player.IsLocal && !HasBeenClaimed && VimRequirementMet && !(GorillaServer.Instance == null))
		{
			GorillaServer.Instance.ClaimItem(_playfabId, delegate(GorillaServer.ClaimItemResponse response)
			{
				OnSuccessfulRedeem(response, player);
			}, delegate(string error)
			{
				Debug.LogError(error.ToString());
			});
		}
	}

	private void OnSuccessfulRedeem(GorillaServer.ClaimItemResponse response, NetPlayer player)
	{
		if (!response.granted)
		{
			return;
		}
		CosmeticsController.instance.UnlockItem(_playfabId);
		CosmeticsController.instance.UpdateMyCosmetics();
		GorillaTagger.Instance.offlineVRRig.AddCosmetic(_playfabId);
		CosmeticsController.CosmeticItem itemFromDict = CosmeticsController.instance.GetItemFromDict(_playfabId);
		if (itemFromDict.bundledItems != null)
		{
			string[] bundledItems = itemFromDict.bundledItems;
			foreach (string cosmeticId in bundledItems)
			{
				GorillaTagger.Instance.offlineVRRig.AddCosmetic(cosmeticId);
			}
		}
		TriggerCelebration(player);
		UpdateState(cosmeticGranted: true);
	}

	private void UpdateState()
	{
		UpdateState(cosmeticGranted: false);
	}

	private void UpdateState(bool cosmeticGranted)
	{
		if (_vimLogo != null)
		{
			_vimLogo.SetActive(_vimRequired);
		}
		_claimLabel.text = ((HasBeenClaimed || cosmeticGranted) ? _alreadyClaimedText : (VimRequirementMet ? _toBeClaimedText : _unclaimableText));
		_nameLabel.text = _cosmeticItem.overrideDisplayName;
	}

	private void UpdateState(NetPlayer _)
	{
		UpdateState();
	}

	[ContextMenu("Trigger Celebration")]
	private void TestTriggerCelebration()
	{
		TriggerCelebration(PhotonNetwork.LocalPlayer);
	}

	public void TriggerCelebration(NetPlayer redeemingPlayer)
	{
		if (PhotonNetwork.InRoom)
		{
			base.photonView.RPC("ActivateClaimVFX", RpcTarget.All);
		}
	}

	[PunRPC]
	private void ActivateClaimVFX(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ActivateClaimVFX");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[26].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			OnCosmeticRedeemed?.Invoke();
		}
	}

	private void PlayClaimFX()
	{
		OnCosmeticRedeemed?.Invoke();
	}
}
