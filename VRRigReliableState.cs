using System;
using System.Collections.Generic;
using Fusion;
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;

public class VRRigReliableState : MonoBehaviour, IWrappedSerializable, INetworkStruct
{
	public enum StateSyncSlots
	{
		Hat,
		Shirt,
		Face,
		Length
	}

	[NonSerialized]
	private ICosmeticStateSync[] m_cosmeticStateTargets = new ICosmeticStateSync[3];

	[NonSerialized]
	private int[] m_cosmeticStates = new int[3];

	[NonSerialized]
	public int[] activeTransferrableObjectIndex;

	[NonSerialized]
	public TransferrableObject.PositionState[] transferrablePosStates;

	[NonSerialized]
	public TransferrableObject.ItemStates[] transferrableItemStates;

	[NonSerialized]
	public BodyDockPositions.DropPositions[] transferableDockPositions;

	[NonSerialized]
	public int wearablesPackedStates;

	[NonSerialized]
	public int lThrowableProjectileIndex = -1;

	[NonSerialized]
	public int rThrowableProjectileIndex = -1;

	[NonSerialized]
	public Color32 lThrowableProjectileColor = Color.white;

	[NonSerialized]
	public Color32 rThrowableProjectileColor = Color.white;

	[NonSerialized]
	public int randomThrowableIndex;

	[NonSerialized]
	public bool isMicEnabled;

	private bool isOfflineVRRig;

	private BodyDockPositions bDock;

	[NonSerialized]
	public int sizeLayerMask = 1;

	private const long IS_MIC_ENABLED_BIT = 32L;

	private const long BRACELET_LEFTHAND_BIT = 64L;

	private const long BUILDER_WATCH_ENABLED_BIT = 128L;

	private const int BRACELET_NUM_BEADS_SHIFT = 12;

	private const int LPROJECTILECOLOR_R_SHIFT = 16;

	private const int LPROJECTILECOLOR_G_SHIFT = 24;

	private const int LPROJECTILECOLOR_B_SHIFT = 32;

	private const int RPROJECTILECOLOR_R_SHIFT = 40;

	private const int RPROJECTILECOLOR_G_SHIFT = 48;

	private const int RPROJECTILECOLOR_B_SHIFT = 56;

	private const int POS_STATES_SHIFT = 32;

	private const int ITEM_STATES_SHIFT = 40;

	private const int DOCK_POSITIONS_SHIFT = 48;

	private const int BRACELET_SELF_INDEX_SHIFT = 60;

	[NonSerialized]
	public bool isBraceletLeftHanded;

	[NonSerialized]
	public int braceletSelfIndex;

	[NonSerialized]
	public List<Color> braceletBeadColors = new List<Color>(10);

	[NonSerialized]
	public bool isBuilderWatchEnabled;

	private ReliableStateData Data;

	public bool HasBracelet => braceletBeadColors.Count > 0;

	public bool isDirty { get; private set; } = true;

	private void Awake()
	{
		VRRig.newPlayerJoined = (Action)Delegate.Combine(VRRig.newPlayerJoined, new Action(SetIsDirty));
		RoomSystem.JoinedRoomEvent += new Action(SetIsDirty);
	}

	private void OnDestroy()
	{
		VRRig.newPlayerJoined = (Action)Delegate.Remove(VRRig.newPlayerJoined, new Action(SetIsDirty));
	}

	public void SetIsDirty()
	{
		isDirty = true;
	}

	public void SetIsNotDirty()
	{
		isDirty = false;
	}

	public void SharedStart(bool isOfflineVRRig_, BodyDockPositions bDock_)
	{
		isOfflineVRRig = isOfflineVRRig_;
		bDock = bDock_;
		activeTransferrableObjectIndex = new int[5];
		for (int i = 0; i < activeTransferrableObjectIndex.Length; i++)
		{
			activeTransferrableObjectIndex[i] = -1;
		}
		transferrablePosStates = new TransferrableObject.PositionState[5];
		transferrableItemStates = new TransferrableObject.ItemStates[5];
		transferableDockPositions = new BodyDockPositions.DropPositions[5];
	}

	public void RegisterCosmeticStateSyncTarget(StateSyncSlots slot, ICosmeticStateSync target)
	{
		if (m_cosmeticStateTargets[(int)slot] != null)
		{
			Debug.LogWarning(string.Format("{0}-CosmeticStateSync: instance already registered at slot {1}, this will be overriden", "VRRigReliableState", slot));
		}
		m_cosmeticStateTargets[(int)slot] = target;
		if (bDock.myRig.isOfflineVRRig)
		{
			m_cosmeticStates[(int)slot] = target.StateValue;
			isDirty = true;
		}
		else
		{
			target.OnStateUpdate(m_cosmeticStates[(int)slot]);
		}
	}

	public void UnRegisterCosmeticStateSyncTarget(StateSyncSlots slot, ICosmeticStateSync target)
	{
		if (m_cosmeticStateTargets[(int)slot] != target)
		{
			Debug.LogWarning(string.Format("{0}-CosmeticStateSync: target is not the value stored at slot {1}, ignoring", "VRRigReliableState", slot));
			return;
		}
		m_cosmeticStateTargets[(int)slot] = null;
		m_cosmeticStates[(int)slot] = -1;
		if (bDock.myRig.isOfflineVRRig)
		{
			isDirty = true;
		}
	}

	private void CopyStateSyncToSyncArray()
	{
		for (int i = 0; i < m_cosmeticStateTargets.Length; i++)
		{
			int num = m_cosmeticStateTargets[i]?.StateValue ?? (-1);
			if (num != m_cosmeticStates[i])
			{
				isDirty = true;
			}
			m_cosmeticStates[i] = num;
		}
	}

	public int GetCachedStateAtSlot(StateSyncSlots slot)
	{
		if (slot < StateSyncSlots.Hat || (int)slot >= m_cosmeticStates.Length)
		{
			return -1;
		}
		return m_cosmeticStates[(int)slot];
	}

	void IWrappedSerializable.OnSerializeRead(object newData)
	{
		Data = (ReliableStateData)newData;
		long header = Data.Header;
		SetHeader(header, out var numBeadsToRead);
		for (int i = 0; i < activeTransferrableObjectIndex.Length; i++)
		{
			if ((header & (1 << i)) != 0L)
			{
				long num = Data.TransferrableStates[i];
				activeTransferrableObjectIndex[i] = (int)num;
				transferrablePosStates[i] = (TransferrableObject.PositionState)((num >> 32) & 0xFF);
				transferrableItemStates[i] = (TransferrableObject.ItemStates)((num >> 40) & 0xFF);
				transferableDockPositions[i] = (BodyDockPositions.DropPositions)((num >> 48) & 0xFF);
			}
			else
			{
				activeTransferrableObjectIndex[i] = -1;
				transferrablePosStates[i] = TransferrableObject.PositionState.None;
				transferrableItemStates[i] = (TransferrableObject.ItemStates)0;
				transferableDockPositions[i] = BodyDockPositions.DropPositions.None;
			}
		}
		wearablesPackedStates = Data.WearablesPackedState;
		lThrowableProjectileIndex = Data.LThrowableProjectileIndex;
		rThrowableProjectileIndex = Data.RThrowableProjectileIndex;
		sizeLayerMask = Data.SizeLayerMask;
		randomThrowableIndex = Data.RandomThrowableIndex;
		braceletBeadColors.Clear();
		if (numBeadsToRead > 0)
		{
			if (numBeadsToRead <= 3)
			{
				int num2 = (int)Data.PackedBeads;
				braceletSelfIndex = num2 >> 30;
				UnpackBeadColors(num2, 0, numBeadsToRead, braceletBeadColors);
			}
			else
			{
				long packedBeads = Data.PackedBeads;
				braceletSelfIndex = (int)(packedBeads >> 60);
				if (numBeadsToRead <= 6)
				{
					UnpackBeadColors(packedBeads, 0, numBeadsToRead, braceletBeadColors);
				}
				else
				{
					UnpackBeadColors(packedBeads, 0, 6, braceletBeadColors);
					UnpackBeadColors(Data.PackedBeadsMoreThan6, 6, numBeadsToRead, braceletBeadColors);
				}
			}
		}
		bDock.RefreshTransferrableItems();
		bDock.myRig.UpdateFriendshipBracelet();
	}

	object IWrappedSerializable.OnSerializeWrite()
	{
		isDirty = false;
		ReliableStateData reliableStateData = default(ReliableStateData);
		long header = (reliableStateData.Header = GetHeader());
		long[] array = GetTransferrableStates(header).ToArray();
		reliableStateData.TransferrableStates.CopyFrom(array, 0, array.Length);
		reliableStateData.WearablesPackedState = wearablesPackedStates;
		reliableStateData.LThrowableProjectileIndex = lThrowableProjectileIndex;
		reliableStateData.RThrowableProjectileIndex = rThrowableProjectileIndex;
		reliableStateData.SizeLayerMask = sizeLayerMask;
		reliableStateData.RandomThrowableIndex = randomThrowableIndex;
		if (braceletBeadColors.Count > 0)
		{
			long num = PackBeadColors(braceletBeadColors, 0);
			if (braceletBeadColors.Count <= 3)
			{
				num |= (long)braceletSelfIndex << 30;
				reliableStateData.PackedBeads = num;
			}
			else
			{
				num |= (long)braceletSelfIndex << 60;
				reliableStateData.PackedBeads = num;
				if (braceletBeadColors.Count > 6)
				{
					reliableStateData.PackedBeadsMoreThan6 = PackBeadColors(braceletBeadColors, 6);
				}
			}
		}
		Data = reliableStateData;
		return reliableStateData;
	}

	void IWrappedSerializable.OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		CopyStateSyncToSyncArray();
		if (!isDirty)
		{
			return;
		}
		isDirty = false;
		long header = GetHeader();
		stream.SendNext(header);
		foreach (long transferrableState in GetTransferrableStates(header))
		{
			stream.SendNext(transferrableState);
		}
		stream.SendNext(wearablesPackedStates);
		stream.SendNext(lThrowableProjectileIndex);
		stream.SendNext(rThrowableProjectileIndex);
		stream.SendNext(sizeLayerMask);
		stream.SendNext(randomThrowableIndex);
		int[] cosmeticStates = m_cosmeticStates;
		foreach (int num in cosmeticStates)
		{
			stream.SendNext(num);
		}
		if (braceletBeadColors.Count <= 0)
		{
			return;
		}
		long num2 = PackBeadColors(braceletBeadColors, 0);
		if (braceletBeadColors.Count <= 3)
		{
			num2 |= (long)braceletSelfIndex << 30;
			stream.SendNext((int)num2);
			return;
		}
		num2 |= (long)braceletSelfIndex << 60;
		stream.SendNext(num2);
		if (braceletBeadColors.Count > 6)
		{
			stream.SendNext(PackBeadColors(braceletBeadColors, 6));
		}
	}

	void IWrappedSerializable.OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		long num = (long)stream.ReceiveNext();
		isMicEnabled = (num & 0x20) != 0;
		isBraceletLeftHanded = (num & 0x40) != 0;
		isBuilderWatchEnabled = (num & 0x80) != 0;
		int num2 = (int)(num >> 12) & 0xF;
		lThrowableProjectileColor.r = (byte)(num >> 16);
		lThrowableProjectileColor.g = (byte)(num >> 24);
		lThrowableProjectileColor.b = (byte)(num >> 32);
		rThrowableProjectileColor.r = (byte)(num >> 40);
		rThrowableProjectileColor.g = (byte)(num >> 48);
		rThrowableProjectileColor.b = (byte)(num >> 56);
		for (int i = 0; i < activeTransferrableObjectIndex.Length; i++)
		{
			if ((num & (1 << i)) != 0L)
			{
				long num3 = (long)stream.ReceiveNext();
				activeTransferrableObjectIndex[i] = (int)num3;
				transferrablePosStates[i] = (TransferrableObject.PositionState)((num3 >> 32) & 0xFF);
				transferrableItemStates[i] = (TransferrableObject.ItemStates)((num3 >> 40) & 0xFF);
				transferableDockPositions[i] = (BodyDockPositions.DropPositions)((num3 >> 48) & 0xFF);
			}
			else
			{
				activeTransferrableObjectIndex[i] = -1;
				transferrablePosStates[i] = TransferrableObject.PositionState.None;
				transferrableItemStates[i] = (TransferrableObject.ItemStates)0;
				transferableDockPositions[i] = BodyDockPositions.DropPositions.None;
			}
		}
		wearablesPackedStates = (int)stream.ReceiveNext();
		lThrowableProjectileIndex = (int)stream.ReceiveNext();
		rThrowableProjectileIndex = (int)stream.ReceiveNext();
		sizeLayerMask = (int)stream.ReceiveNext();
		randomThrowableIndex = (int)stream.ReceiveNext();
		for (int j = 0; j < m_cosmeticStates.Length; j++)
		{
			int num4 = (int)stream.ReceiveNext();
			m_cosmeticStates[j] = num4;
			m_cosmeticStateTargets[j]?.OnStateUpdate(num4);
		}
		braceletBeadColors.Clear();
		if (num2 > 0)
		{
			if (num2 <= 3)
			{
				int num5 = (int)stream.ReceiveNext();
				braceletSelfIndex = num5 >> 30;
				UnpackBeadColors(num5, 0, num2, braceletBeadColors);
			}
			else
			{
				long num6 = (long)stream.ReceiveNext();
				braceletSelfIndex = (int)(num6 >> 60);
				if (num2 <= 6)
				{
					UnpackBeadColors(num6, 0, num2, braceletBeadColors);
				}
				else
				{
					UnpackBeadColors(num6, 0, 6, braceletBeadColors);
					UnpackBeadColors((long)stream.ReceiveNext(), 6, num2, braceletBeadColors);
				}
			}
		}
		bDock.RefreshTransferrableItems();
		bDock.myRig.UpdateFriendshipBracelet();
		bDock.myRig.EnableBuilderResizeWatch(isBuilderWatchEnabled);
	}

	private long GetHeader()
	{
		long num = 0L;
		if (CosmeticsController.instance.isHidingCosmeticsFromRemotePlayers)
		{
			for (int i = 0; i < activeTransferrableObjectIndex.Length; i++)
			{
				if (activeTransferrableObjectIndex[i] != -1 && (transferrablePosStates[i] == TransferrableObject.PositionState.InLeftHand || transferrablePosStates[i] == TransferrableObject.PositionState.InRightHand))
				{
					num |= (byte)(1 << i);
				}
			}
		}
		else
		{
			for (int j = 0; j < activeTransferrableObjectIndex.Length; j++)
			{
				if (activeTransferrableObjectIndex[j] != -1)
				{
					num |= (byte)(1 << j);
				}
			}
		}
		if (isBraceletLeftHanded)
		{
			num |= 0x40;
		}
		if (isMicEnabled)
		{
			num |= 0x20;
		}
		if (isBuilderWatchEnabled && !CosmeticsController.instance.isHidingCosmeticsFromRemotePlayers)
		{
			num |= 0x80;
		}
		num |= (long)(((ulong)braceletBeadColors.Count & 0xFuL) << 12);
		num |= (long)((ulong)lThrowableProjectileColor.r << 16);
		num |= (long)((ulong)lThrowableProjectileColor.g << 24);
		num |= (long)((ulong)lThrowableProjectileColor.b << 32);
		num |= (long)((ulong)rThrowableProjectileColor.r << 40);
		num |= (long)((ulong)rThrowableProjectileColor.g << 48);
		return num | (long)((ulong)rThrowableProjectileColor.b << 56);
	}

	private void SetHeader(long header, out int numBeadsToRead)
	{
		isMicEnabled = (header & 0x20) != 0;
		isBraceletLeftHanded = (header & 0x40) != 0;
		numBeadsToRead = (int)(header >> 12) & 0xF;
		lThrowableProjectileColor.r = (byte)(header >> 16);
		lThrowableProjectileColor.g = (byte)(header >> 24);
		lThrowableProjectileColor.b = (byte)(header >> 32);
		rThrowableProjectileColor.r = (byte)(header >> 40);
		rThrowableProjectileColor.g = (byte)(header >> 48);
		rThrowableProjectileColor.b = (byte)(header >> 56);
	}

	private List<long> GetTransferrableStates(long header)
	{
		List<long> list = new List<long>();
		for (int i = 0; i < activeTransferrableObjectIndex.Length; i++)
		{
			if ((header & (1 << i)) != 0L && activeTransferrableObjectIndex[i] != -1)
			{
				long num = (uint)activeTransferrableObjectIndex[i];
				num |= (long)transferrablePosStates[i] << 32;
				num |= (long)transferrableItemStates[i] << 40;
				num |= (long)transferableDockPositions[i] << 48;
				list.Add(num);
			}
		}
		return list;
	}

	private static long PackBeadColors(List<Color> beadColors, int fromIndex)
	{
		long num = 0L;
		int num2 = Mathf.Min(fromIndex + 6, beadColors.Count);
		int num3 = 0;
		for (int i = fromIndex; i < num2; i++)
		{
			long num4 = FriendshipGroupDetection.PackColor(beadColors[i]);
			num |= num4 << num3;
			num3 += 10;
		}
		return num;
	}

	private static void UnpackBeadColors(long packed, int startIndex, int endIndex, List<Color> beadColorsResult)
	{
		int num = Mathf.Min(startIndex + 6, endIndex);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			short data = (short)((packed >> num2) & 0x3FF);
			beadColorsResult.Add(FriendshipGroupDetection.UnpackColor(data));
			num2 += 10;
		}
	}
}
