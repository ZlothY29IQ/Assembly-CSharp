using Photon.Pun;
using UnityEngine;

namespace GorillaNetworking;

public class SubCosmeticCycleController : MonoBehaviour
{
	[SerializeField]
	private bool syncCycleOverNetwork = true;

	[SerializeField]
	private bool syncBroadcastOverNetwork = true;

	private CosmeticCollectionDisplay display;

	private readonly CallLimiter receiveSignalLimiter = new CallLimiter(10, 1f);

	private CosmeticCollectionDisplay Display
	{
		get
		{
			if (display == null)
			{
				display = GetComponentInChildren<CosmeticCollectionDisplay>();
			}
			return display;
		}
	}

	public CosmeticsController.CosmeticItem? ActiveCollectable => Display?.ActiveCollectable;

	public int ActiveIndex => Display?.ActiveIndex ?? 0;

	public int Count => Display?.Count ?? 0;

	private bool HasAuthority
	{
		get
		{
			if (Display != null)
			{
				return Display.IsLocal;
			}
			return false;
		}
	}

	public string GetAppliedCosmeticID()
	{
		return ActiveCollectable?.appliedCosmeticPlayFabID ?? string.Empty;
	}

	public void CycleForward()
	{
		if (HasAuthority && Display.Count > 1)
		{
			int activeIndex = (Display.ActiveIndex + 1) % Display.Count;
			Display.SetActiveIndex(activeIndex);
			SendStateRPC();
		}
	}

	public void CycleBackward()
	{
		if (HasAuthority && Display.Count > 1)
		{
			int activeIndex = (Display.ActiveIndex - 1 + Display.Count) % Display.Count;
			Display.SetActiveIndex(activeIndex);
			SendStateRPC();
		}
	}

	public void CycleRandom()
	{
		if (HasAuthority && Display.Count > 1)
		{
			int num;
			do
			{
				num = Random.Range(0, Display.Count);
			}
			while (num == Display.ActiveIndex);
			Display.SetActiveIndex(num);
			SendStateRPC();
		}
	}

	public void SetIndex(int index)
	{
		if (HasAuthority)
		{
			Display.SetActiveIndex(index);
			SendStateRPC();
		}
	}

	public void SetDisplayVisible(bool visible)
	{
		Display?.SetVisible(visible);
	}

	public void Equip(int canonicalIndex)
	{
		if (HasAuthority && Display.SetEquippedAtCanonical(canonicalIndex, equipped: true))
		{
			SendStateRPC();
		}
	}

	public void Unequip(int canonicalIndex)
	{
		if (HasAuthority && Display.SetEquippedAtCanonical(canonicalIndex, equipped: false))
		{
			SendStateRPC();
		}
	}

	public void EquipActive()
	{
		if (HasAuthority)
		{
			int activeCanonicalIndex = Display.ActiveCanonicalIndex;
			if (activeCanonicalIndex >= 0 && Display.SetEquippedAtCanonical(activeCanonicalIndex, equipped: true))
			{
				SendStateRPC();
			}
		}
	}

	public void UnequipActive()
	{
		if (HasAuthority)
		{
			int activeCanonicalIndex = Display.ActiveCanonicalIndex;
			if (activeCanonicalIndex >= 0 && Display.SetEquippedAtCanonical(activeCanonicalIndex, equipped: false))
			{
				SendStateRPC();
			}
		}
	}

	public void EquipAll()
	{
		if (HasAuthority)
		{
			Display.SetVisibleMask(-1);
			SendStateRPC();
		}
	}

	public void UnequipAll()
	{
		if (HasAuthority)
		{
			Display.SetVisibleMask(0);
			SendStateRPC();
		}
	}

	public bool IsEquipped(int canonicalIndex)
	{
		if (Display != null)
		{
			return Display.IsEquippedAtCanonical(canonicalIndex);
		}
		return false;
	}

	public void BroadcastSignal(int signal)
	{
		if (HasAuthority)
		{
			BroadcastSignalLocal(signal);
			SendBroadcastSignalRPC(signal);
		}
	}

	public void ReceiveNetworkSignal(int signal)
	{
		if (receiveSignalLimiter.CheckCallTime(Time.time))
		{
			BroadcastSignalLocal(signal);
		}
	}

	public void BroadcastSignalLocal(int signal)
	{
		SubCosmeticSignalReceiver[] componentsInChildren = GetComponentsInChildren<SubCosmeticSignalReceiver>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] != null)
			{
				componentsInChildren[i].ReceiveSignal(signal);
			}
		}
	}

	private void SendBroadcastSignalRPC(int signal)
	{
		if (syncBroadcastOverNetwork && !(Display == null) && Display.IsLocal)
		{
			string text = Display?.ParentPlayFabID;
			if (!string.IsNullOrEmpty(text) && text.Length >= 5 && NetworkSystem.Instance.InRoom)
			{
				int num = text[0] - 65 + 26 * (text[1] - 65 + 26 * (text[2] - 65 + 26 * (text[3] - 65 + 26 * (text[4] - 65))));
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_BroadcastSubCosmeticSignal", RpcTarget.Others, new int[2] { num, signal });
			}
		}
	}

	private void SendStateRPC()
	{
		if (!syncCycleOverNetwork || Display == null || !Display.IsLocal)
		{
			return;
		}
		string text = Display?.ParentPlayFabID;
		if (string.IsNullOrEmpty(text) || text.Length < 5 || !NetworkSystem.Instance.InRoom)
		{
			return;
		}
		int num = Display.ActiveIndex;
		CosmeticsController.CosmeticItem? activeCollectable = Display.ActiveCollectable;
		if (activeCollectable.HasValue && CosmeticsController.hasInstance)
		{
			int canonicalCollectableIndex = CosmeticsController.instance.GetCanonicalCollectableIndex(text, activeCollectable.Value.itemName);
			if (canonicalCollectableIndex >= 0)
			{
				num = canonicalCollectableIndex;
			}
		}
		int visibleMask = Display.VisibleMask;
		int num2 = text[0] - 65 + 26 * (text[1] - 65 + 26 * (text[2] - 65 + 26 * (text[3] - 65 + 26 * (text[4] - 65))));
		GorillaTagger.Instance.myVRRig.SendRPC("RPC_SetCollectionCycleIndex", RpcTarget.Others, new int[3] { num2, num, visibleMask });
	}
}
