using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace GorillaTagScripts;

[NetworkBehaviourWeaved(1)]
public class DecorativeItemsManager : NetworkComponent
{
	public GameObject decorativeItemsContainer;

	public GameObject respawnableHooksContainer;

	public List<GameObject> nonRespawnableHooksContainer = new List<GameObject>();

	private readonly List<DecorativeItem> itemsList = new List<DecorativeItem>();

	private readonly List<AttachPoint> respawnableHooks = new List<AttachPoint>();

	private readonly List<AttachPoint> allHooks = new List<AttachPoint>();

	private int lastIndex;

	private int currentIndex;

	private int arrayIndex = -1;

	private bool shouldRunUpdate;

	private ZoneBasedObject zone;

	private bool wasInZone;

	[OnEnterPlay_SetNull]
	private static DecorativeItemsManager _instance;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private int _Data;

	public static DecorativeItemsManager Instance => _instance;

	[Networked]
	[NetworkedWeaved(0, 1)]
	public unsafe int Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing DecorativeItemsManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(int*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing DecorativeItemsManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(int*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
		currentIndex = -1;
		shouldRunUpdate = true;
		zone = GetComponent<ZoneBasedObject>();
		DecorativeItem[] componentsInChildren = decorativeItemsContainer.GetComponentsInChildren<DecorativeItem>(includeInactive: false);
		foreach (DecorativeItem decorativeItem in componentsInChildren)
		{
			if ((bool)decorativeItem)
			{
				itemsList.Add(decorativeItem);
				decorativeItem.respawnItem = (UnityAction<DecorativeItem>)Delegate.Combine(decorativeItem.respawnItem, new UnityAction<DecorativeItem>(OnRequestToRespawn));
			}
		}
		AttachPoint[] componentsInChildren2 = respawnableHooksContainer.GetComponentsInChildren<AttachPoint>(includeInactive: false);
		foreach (AttachPoint attachPoint in componentsInChildren2)
		{
			if ((bool)attachPoint)
			{
				respawnableHooks.Add(attachPoint);
			}
		}
		allHooks.AddRange(respawnableHooks);
		foreach (GameObject item in nonRespawnableHooksContainer)
		{
			componentsInChildren2 = item.GetComponentsInChildren<AttachPoint>(includeInactive: false);
			foreach (AttachPoint attachPoint2 in componentsInChildren2)
			{
				if ((bool)attachPoint2)
				{
					allHooks.Add(attachPoint2);
				}
			}
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		foreach (DecorativeItem items in itemsList)
		{
			items.respawnItem = (UnityAction<DecorativeItem>)Delegate.Remove(items.respawnItem, new UnityAction<DecorativeItem>(OnRequestToRespawn));
		}
		itemsList.Clear();
		respawnableHooks.Clear();
		if (_instance == this)
		{
			_instance = null;
		}
	}

	private void Update()
	{
		if (!PhotonNetwork.InRoom)
		{
			return;
		}
		if (wasInZone != zone.IsLocalPlayerInZone())
		{
			shouldRunUpdate = true;
		}
		if (!shouldRunUpdate || !base.IsMine)
		{
			return;
		}
		if (wasInZone != zone.IsLocalPlayerInZone())
		{
			foreach (AttachPoint allHook in allHooks)
			{
				allHook.SetIsHook(isHooked: false);
			}
			for (int i = 0; i < itemsList.Count; i++)
			{
				itemsList[i].itemState = TransferrableObject.ItemStates.State2;
				SpawnItem(i);
			}
			shouldRunUpdate = false;
		}
		wasInZone = zone.IsLocalPlayerInZone();
		SpawnItem(UpdateListPerFrame());
	}

	private void SpawnItem(int index)
	{
		if (!NetworkSystem.Instance.InRoom || index < 0 || index >= itemsList.Count || respawnableHooks == null || itemsList == null)
		{
			return;
		}
		if (itemsList.Count > respawnableHooks.Count)
		{
			Debug.LogError("Trying to snap more decorative items than allowed! Some items will be left un-hooked!");
			return;
		}
		Transform transform = RandomSpawn();
		if (!(transform == null))
		{
			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;
			DecorativeItem decorativeItem = itemsList[index];
			decorativeItem.WorldShareableRequestOwnership();
			decorativeItem.Respawn(position, rotation);
			SendRPC("RespawnItemRPC", RpcTarget.Others, index, position, rotation);
		}
	}

	[PunRPC]
	private void RespawnItemRPC(int index, Vector3 _transformPos, Quaternion _transformRot, PhotonMessageInfo info)
	{
		RespawnItemShared(index, _transformPos, _transformRot, info);
	}

	[Rpc]
	private unsafe void RPC_RespawnItem(int index, Vector3 _transformPos, Quaternion _transformRot, RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTagScripts.DecorativeItemsManager::RPC_RespawnItem(System.Int32,UnityEngine.Vector3,UnityEngine.Quaternion,Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			num += 4;
			num += 12;
			num += 16;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTagScripts.DecorativeItemsManager::RPC_RespawnItem(System.Int32,UnityEngine.Vector3,UnityEngine.Quaternion,Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
				int num2 = 8;
				*(int*)(ptr2 + num2) = index;
				num2 += 4;
				*(Vector3*)(ptr2 + num2) = _transformPos;
				num2 += 12;
				*(Quaternion*)(ptr2 + num2) = _transformRot;
				num2 += 16;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		RespawnItemShared(index, _transformPos, _transformRot, info);
	}

	protected void RespawnItemShared(int index, Vector3 _transformPos, Quaternion _transformRot, PhotonMessageInfoWrapped info)
	{
		if (index >= 0 && index <= itemsList.Count - 1 && _transformPos.IsValid(10000f) && _transformRot.IsValid() && info.Sender == NetworkSystem.Instance.MasterClient)
		{
			MonkeAgent.IncrementRPCCall(info, "RespawnItemRPC");
			itemsList[index].Respawn(_transformPos, _transformRot);
		}
	}

	private Transform RandomSpawn()
	{
		lastIndex = currentIndex;
		bool flag = false;
		bool flag2 = zone.IsLocalPlayerInZone();
		int index = UnityEngine.Random.Range(0, respawnableHooks.Count);
		while (!flag)
		{
			index = UnityEngine.Random.Range(0, respawnableHooks.Count);
			if (!respawnableHooks[index].inForest == flag2)
			{
				flag = true;
			}
		}
		if (!respawnableHooks[index].IsHooked())
		{
			currentIndex = index;
		}
		else
		{
			currentIndex = -1;
		}
		if (currentIndex != lastIndex && currentIndex > -1)
		{
			return respawnableHooks[currentIndex].attachPoint;
		}
		currentIndex = -1;
		return null;
	}

	private int UpdateListPerFrame()
	{
		arrayIndex++;
		if (arrayIndex >= itemsList.Count || arrayIndex < 0)
		{
			shouldRunUpdate = false;
			return -1;
		}
		return arrayIndex;
	}

	private void OnRequestToRespawn(DecorativeItem item)
	{
		if (base.IsMine && !(item == null))
		{
			int index = itemsList.IndexOf(item);
			SpawnItem(index);
		}
	}

	public AttachPoint getCurrentAttachPointByPosition(Vector3 _attachPoint)
	{
		foreach (AttachPoint allHook in allHooks)
		{
			if (allHook.attachPoint.position == _attachPoint)
			{
				return allHook;
			}
		}
		return null;
	}

	public override void WriteDataFusion()
	{
		Data = currentIndex;
	}

	public override void ReadDataFusion()
	{
		currentIndex = Data;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(currentIndex);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			currentIndex = (int)stream.ReceiveNext();
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}

	[NetworkRpcWeavedInvoker(1, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_RespawnItem_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int index = num2;
		Vector3 vector = *(Vector3*)(ptr + num);
		num += 12;
		Vector3 transformPos = vector;
		Quaternion quaternion = *(Quaternion*)(ptr + num);
		num += 16;
		Quaternion transformRot = quaternion;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((DecorativeItemsManager)behaviour).RPC_RespawnItem(index, transformPos, transformRot, info);
	}
}
