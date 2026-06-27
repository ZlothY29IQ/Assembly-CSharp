using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Serialization;

public class BodyDockPositions : MonoBehaviour
{
	[Flags]
	public enum DropPositions
	{
		LeftArm = 1,
		RightArm = 2,
		Chest = 4,
		LeftBack = 8,
		RightBack = 0x10,
		MaxDropPostions = 5,
		All = 0x1F,
		None = 0
	}

	public class DockingResult
	{
		public List<DropPositions> positionsDisabled;

		public List<DropPositions> dockedPosition;

		public DockingResult()
		{
			dockedPosition = new List<DropPositions>(2);
			positionsDisabled = new List<DropPositions>(2);
		}
	}

	public VRRig myRig;

	public GameObject[] leftHandThrowables;

	public GameObject[] rightHandThrowables;

	[FormerlySerializedAs("allObjects")]
	public TransferrableObject[] _allObjects;

	private List<int> objectsToEnable = new List<int>();

	private List<int> objectsToDisable = new List<int>();

	public Transform leftHandTransform;

	public Transform rightHandTransform;

	public Transform chestTransform;

	public Transform leftArmTransform;

	public Transform rightArmTransform;

	public Transform leftBackTransform;

	public Transform rightBackTransform;

	public WorldShareableItem leftBackSharableItem;

	public WorldShareableItem rightBackShareableItem;

	public GameObject SharableItemInstance;

	private int[] throwableDisabledIndex = new int[2] { -1, -1 };

	private float[] throwableDisabledTime = new float[2];

	public TransferrableObject[] allObjects
	{
		get
		{
			return _allObjects;
		}
		set
		{
			_allObjects = value;
		}
	}

	internal int PreviousLeftHandThrowableIndex => throwableDisabledIndex[0];

	internal int PreviousRightHandThrowableIndex => throwableDisabledIndex[1];

	internal float PreviousLeftHandThrowableDisabledTime => throwableDisabledTime[0];

	internal float PreviousRightHandThrowableDisabledTime => throwableDisabledTime[1];

	public void Awake()
	{
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		if (object.Equals(myRig.creator, otherPlayer))
		{
			DeallocateSharableInstances();
		}
	}

	public void OnLeftRoom()
	{
		DeallocateSharableInstances();
	}

	public WorldShareableItem AllocateSharableInstance(DropPositions position, NetPlayer owner)
	{
		switch (position)
		{
		case DropPositions.LeftBack:
			if ((object)leftBackSharableItem == null)
			{
				leftBackSharableItem = ObjectPools.instance.Instantiate(SharableItemInstance).GetComponent<WorldShareableItem>();
				leftBackSharableItem.GetComponent<RequestableOwnershipGuard>().SetOwnership(owner, isLocalOnly: false, dontPropigate: true);
				leftBackSharableItem.GetComponent<WorldShareableItem>().SetupSharableViewIDs(owner, 3);
			}
			return leftBackSharableItem;
		case DropPositions.RightBack:
			if ((object)rightBackShareableItem == null)
			{
				rightBackShareableItem = ObjectPools.instance.Instantiate(SharableItemInstance).GetComponent<WorldShareableItem>();
				rightBackShareableItem.GetComponent<RequestableOwnershipGuard>().SetOwnership(owner, isLocalOnly: false, dontPropigate: true);
				rightBackShareableItem.GetComponent<WorldShareableItem>().SetupSharableViewIDs(owner, 4);
			}
			return rightBackShareableItem;
		default:
			throw new ArgumentOutOfRangeException("position", position, null);
		}
	}

	public void DeallocateSharableInstance(WorldShareableItem worldShareable)
	{
		if (worldShareable == null)
		{
			return;
		}
		if (worldShareable == leftBackSharableItem)
		{
			if ((object)leftBackSharableItem == null)
			{
				return;
			}
			leftBackSharableItem.ResetViews();
			ObjectPools.instance.Destroy(leftBackSharableItem.gameObject);
			leftBackSharableItem = null;
		}
		if (worldShareable == rightBackShareableItem && (object)rightBackShareableItem != null)
		{
			rightBackShareableItem.ResetViews();
			ObjectPools.instance.Destroy(rightBackShareableItem.gameObject);
			rightBackShareableItem = null;
		}
	}

	public void DeallocateSharableInstances()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if ((object)rightBackShareableItem != null)
			{
				rightBackShareableItem.ResetViews();
				ObjectPools.instance.Destroy(rightBackShareableItem.gameObject);
			}
			if ((object)leftBackSharableItem != null)
			{
				leftBackSharableItem.ResetViews();
				ObjectPools.instance.Destroy(leftBackSharableItem.gameObject);
			}
			leftBackSharableItem = null;
			rightBackShareableItem = null;
		}
	}

	public static bool IsPositionLeft(DropPositions pos)
	{
		if (pos != DropPositions.LeftArm)
		{
			return pos == DropPositions.LeftBack;
		}
		return true;
	}

	public int DropZoneStorageUsed(DropPositions dropPosition)
	{
		if (myRig == null)
		{
			Debug.Log("BodyDockPositions lost reference to VR Rig, resetting it now", this);
			myRig = GetComponent<VRRig>();
		}
		if (myRig == null)
		{
			Debug.Log("Unable to reset reference");
			return -1;
		}
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myRig.ActiveTransferrableObjectIndex(i) >= 0 && allObjects[myRig.ActiveTransferrableObjectIndex(i)] != null && allObjects[myRig.ActiveTransferrableObjectIndex(i)].gameObject.activeInHierarchy && allObjects[myRig.ActiveTransferrableObjectIndex(i)].storedZone == dropPosition)
			{
				return myRig.ActiveTransferrableObjectIndex(i);
			}
		}
		return -1;
	}

	public TransferrableObject ItemPositionInUse(DropPositions dropPosition)
	{
		TransferrableObject.PositionState positionState = MapDropPositionToState(dropPosition);
		if (myRig == null)
		{
			Debug.Log("BodyDockPositions lost reference to VR Rig, resetting it now", this);
			myRig = GetComponent<VRRig>();
		}
		if (myRig == null)
		{
			Debug.Log("Unable to reset reference");
			return null;
		}
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myRig.ActiveTransferrableObjectIndex(i) != -1 && allObjects[myRig.ActiveTransferrableObjectIndex(i)].gameObject.activeInHierarchy && allObjects[myRig.ActiveTransferrableObjectIndex(i)].currentState == positionState)
			{
				return allObjects[myRig.ActiveTransferrableObjectIndex(i)];
			}
		}
		return null;
	}

	private int EnableTransferrableItem(int allItemsIndex, DropPositions startingPosition, TransferrableObject.PositionState startingState)
	{
		if (allItemsIndex < 0 || allItemsIndex >= allObjects.Length)
		{
			return -1;
		}
		if (myRig != null && myRig.isOfflineVRRig)
		{
			for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
			{
				if (myRig.ActiveTransferrableObjectIndex(i) == allItemsIndex)
				{
					DisableTransferrableItem(allItemsIndex);
				}
			}
			for (int j = 0; j < myRig.ActiveTransferrableObjectIndexLength(); j++)
			{
				if (myRig.ActiveTransferrableObjectIndex(j) == -1)
				{
					string itemNameFromDisplayName = CosmeticsController.instance.GetItemNameFromDisplayName(allObjects[allItemsIndex].gameObject.name);
					if (myRig.IsItemAllowed(itemNameFromDisplayName))
					{
						myRig.SetActiveTransferrableObjectIndex(j, allItemsIndex);
						myRig.SetTransferrablePosStates(j, startingState);
						myRig.SetTransferrableItemStates(j, (TransferrableObject.ItemStates)0);
						myRig.SetTransferrableDockPosition(j, startingPosition);
						EnableTransferrableGameObject(allItemsIndex, startingPosition, startingState);
						return j;
					}
				}
			}
		}
		return -1;
	}

	public DropPositions ItemActive(int allItemsIndex)
	{
		if (!allObjects[allItemsIndex].gameObject.activeSelf)
		{
			return DropPositions.None;
		}
		return allObjects[allItemsIndex].storedZone;
	}

	public static DropPositions OfflineItemActive(int allItemsIndex)
	{
		BodyDockPositions bodyDockPositions = null;
		if (GorillaTagger.Instance == null || GorillaTagger.Instance.offlineVRRig == null)
		{
			return DropPositions.None;
		}
		bodyDockPositions = GorillaTagger.Instance.offlineVRRig.GetComponent<BodyDockPositions>();
		if (bodyDockPositions == null)
		{
			return DropPositions.None;
		}
		if (bodyDockPositions.allObjects[allItemsIndex] == null || !bodyDockPositions.allObjects[allItemsIndex].gameObject.activeSelf)
		{
			return DropPositions.None;
		}
		return bodyDockPositions.allObjects[allItemsIndex].storedZone;
	}

	public void DisableTransferrableItem(int index)
	{
		TransferrableObject transferrableObject = allObjects[index];
		if (transferrableObject.gameObject.activeSelf)
		{
			transferrableObject.gameObject.Disable();
			transferrableObject.storedZone = DropPositions.None;
		}
		if (!myRig.isOfflineVRRig)
		{
			return;
		}
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myRig.ActiveTransferrableObjectIndex(i) == index)
			{
				myRig.SetActiveTransferrableObjectIndex(i, -1);
			}
		}
	}

	public void DisableAllTransferableItems()
	{
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			int num = myRig.ActiveTransferrableObjectIndex(i);
			if (num >= 0 && num < allObjects.Length)
			{
				TransferrableObject transferrableObject = allObjects[num];
				if (transferrableObject != null)
				{
					transferrableObject.gameObject?.Disable();
					transferrableObject.storedZone = DropPositions.None;
				}
				myRig.SetActiveTransferrableObjectIndex(i, -1);
				myRig.SetTransferrableItemStates(i, (TransferrableObject.ItemStates)0);
				myRig.SetTransferrablePosStates(i, TransferrableObject.PositionState.None);
			}
		}
		DeallocateSharableInstances();
	}

	private bool AllItemsIndexValid(int allItemsIndex)
	{
		if (allItemsIndex != -1)
		{
			return allItemsIndex < allObjects.Length;
		}
		return false;
	}

	public bool PositionAvailable(int allItemIndex, DropPositions startPos)
	{
		return (allObjects[allItemIndex].dockPositions & startPos) != 0;
	}

	public DropPositions FirstAvailablePosition(int allItemIndex)
	{
		for (int i = 0; i < 5; i++)
		{
			DropPositions dropPositions = (DropPositions)(1 << i);
			if ((allObjects[allItemIndex].dockPositions & dropPositions) != DropPositions.None)
			{
				return dropPositions;
			}
		}
		return DropPositions.None;
	}

	public int TransferrableItemDisable(int allItemsIndex)
	{
		if (OfflineItemActive(allItemsIndex) != DropPositions.None)
		{
			DisableTransferrableItem(allItemsIndex);
		}
		return 0;
	}

	public void TransferrableItemDisableAtPosition(DropPositions dropPositions)
	{
		int num = DropZoneStorageUsed(dropPositions);
		if (num >= 0)
		{
			TransferrableItemDisable(num);
		}
	}

	public void TransferrableItemEnableAtPosition(string itemName, DropPositions dropPosition)
	{
		if (DropZoneStorageUsed(dropPosition) >= 0)
		{
			return;
		}
		List<int> list = TransferrableObjectIndexFromName(itemName);
		if (list.Count != 0)
		{
			TransferrableObject.PositionState startingState = MapDropPositionToState(dropPosition);
			if (list.Count == 1)
			{
				EnableTransferrableItem(list[0], dropPosition, startingState);
				return;
			}
			int allItemsIndex = (IsPositionLeft(dropPosition) ? list[0] : list[1]);
			EnableTransferrableItem(allItemsIndex, dropPosition, startingState);
		}
	}

	public bool TransferrableItemActive(string transferrableItemName)
	{
		List<int> list = TransferrableObjectIndexFromName(transferrableItemName);
		if (list.Count == 0)
		{
			return false;
		}
		foreach (int item in list)
		{
			if (TransferrableItemActive(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool TransferrableItemActiveAtPos(string transferrableItemName, DropPositions dropPosition)
	{
		List<int> list = TransferrableObjectIndexFromName(transferrableItemName);
		if (list.Count == 0)
		{
			return false;
		}
		foreach (int item in list)
		{
			DropPositions dropPositions = TransferrableItemPosition(item);
			if (dropPositions != DropPositions.None && dropPositions == dropPosition)
			{
				return true;
			}
		}
		return false;
	}

	public bool TransferrableItemActive(int allItemsIndex)
	{
		return ItemActive(allItemsIndex) != DropPositions.None;
	}

	public TransferrableObject TransferrableItem(int allItemsIndex)
	{
		return allObjects[allItemsIndex];
	}

	public DropPositions TransferrableItemPosition(int allItemsIndex)
	{
		return ItemActive(allItemsIndex);
	}

	public bool DisableTransferrableItem(string transferrableItemName)
	{
		List<int> list = TransferrableObjectIndexFromName(transferrableItemName);
		if (list.Count == 0)
		{
			return false;
		}
		foreach (int item in list)
		{
			DisableTransferrableItem(item);
		}
		return true;
	}

	public DropPositions OppositePosition(DropPositions pos)
	{
		return pos switch
		{
			DropPositions.LeftArm => DropPositions.RightArm, 
			DropPositions.RightArm => DropPositions.LeftArm, 
			DropPositions.LeftBack => DropPositions.RightBack, 
			DropPositions.RightBack => DropPositions.LeftBack, 
			_ => pos, 
		};
	}

	public DockingResult ToggleWithHandedness(string transferrableItemName, bool isLeftHand, bool bothHands)
	{
		List<int> list = TransferrableObjectIndexFromName(transferrableItemName);
		if (list.Count == 0)
		{
			return new DockingResult();
		}
		if (!AllItemsIndexValid(list[0]))
		{
			return new DockingResult();
		}
		DropPositions startingPos = ((!isLeftHand) ? (((allObjects[list[0]].dockPositions & DropPositions.LeftArm) != DropPositions.None) ? DropPositions.LeftArm : DropPositions.RightBack) : (((allObjects[list[0]].dockPositions & DropPositions.RightArm) != DropPositions.None) ? DropPositions.RightArm : DropPositions.LeftBack));
		return ToggleTransferrableItem(transferrableItemName, startingPos, bothHands);
	}

	public DockingResult ToggleTransferrableItem(string transferrableItemName, DropPositions startingPos, bool bothHands)
	{
		DockingResult dockingResult = new DockingResult();
		List<int> list = TransferrableObjectIndexFromName(transferrableItemName);
		if (list.Count == 0)
		{
			return dockingResult;
		}
		if (bothHands && list.Count == 2)
		{
			for (int i = 0; i < list.Count; i++)
			{
				int allItemsIndex = list[i];
				DropPositions dropPositions = OfflineItemActive(allItemsIndex);
				if (dropPositions != DropPositions.None)
				{
					TransferrableItemDisable(allItemsIndex);
					dockingResult.positionsDisabled.Add(dropPositions);
				}
			}
			if (dockingResult.positionsDisabled.Count >= 1)
			{
				return dockingResult;
			}
		}
		DropPositions dropPositions2 = startingPos;
		for (int j = 0; j < list.Count; j++)
		{
			int num = list[j];
			dropPositions2 = startingPos;
			if (bothHands && j != 0)
			{
				dropPositions2 = OppositePosition(dropPositions2);
			}
			if (!PositionAvailable(num, dropPositions2))
			{
				dropPositions2 = FirstAvailablePosition(num);
				if (dropPositions2 == DropPositions.None)
				{
					return dockingResult;
				}
			}
			if (OfflineItemActive(num) == dropPositions2)
			{
				TransferrableItemDisable(num);
				dockingResult.positionsDisabled.Add(dropPositions2);
				continue;
			}
			TransferrableItemDisableAtPosition(dropPositions2);
			dockingResult.dockedPosition.Add(dropPositions2);
			TransferrableObject.PositionState positionState = MapDropPositionToState(dropPositions2);
			if (TransferrableItemActive(num))
			{
				DropPositions item = TransferrableItemPosition(num);
				dockingResult.positionsDisabled.Add(item);
				MoveTransferableItem(num, dropPositions2, positionState);
			}
			else
			{
				EnableTransferrableItem(num, dropPositions2, positionState);
			}
		}
		return dockingResult;
	}

	private void MoveTransferableItem(int allItemsIndex, DropPositions newPosition, TransferrableObject.PositionState newPositionState)
	{
		allObjects[allItemsIndex].storedZone = newPosition;
		allObjects[allItemsIndex].currentState = newPositionState;
		allObjects[allItemsIndex].ResetToDefaultState();
	}

	public void EnableTransferrableGameObject(int allItemsIndex, DropPositions dropZone, TransferrableObject.PositionState startingPosition)
	{
		if (allObjects[allItemsIndex] == null)
		{
			return;
		}
		GameObject gameObject = allObjects[allItemsIndex].gameObject;
		TransferrableObject component = gameObject.GetComponent<TransferrableObject>();
		if ((component.dockPositions & dropZone) == 0 || !component.ValidateState(startingPosition))
		{
			gameObject.Disable();
			return;
		}
		MoveTransferableItem(allItemsIndex, dropZone, startingPosition);
		gameObject.SetActive(value: true);
		ProjectileWeapon component2;
		if ((component2 = gameObject.GetComponent<ProjectileWeapon>()) != null)
		{
			component2.enabled = true;
		}
	}

	public void RefreshTransferrableItems()
	{
		if (!myRig)
		{
			myRig = GetComponentInParent<VRRig>(includeInactive: true);
			if (!myRig)
			{
				Debug.LogError("BodyDockPositions.RefreshTransferrableItems: (should never happen) myRig is null and could not be found on same GameObject or parents. Path: " + base.transform.GetPathQ(), this);
			}
		}
		objectsToEnable.Clear();
		objectsToDisable.Clear();
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			int num = myRig.ActiveTransferrableObjectIndex(i);
			if (num == -1)
			{
				continue;
			}
			if (num < 0 || num >= allObjects.Length)
			{
				Debug.LogError($"Transferrable object index {num} out of range, expected [0..{allObjects.Length})");
				continue;
			}
			string displayName = allObjects[num]?.gameObject.name;
			string itemNameFromDisplayName = CosmeticsController.instance.GetItemNameFromDisplayName(displayName);
			if (!myRig.IsItemAllowed(itemNameFromDisplayName))
			{
				continue;
			}
			int num2 = myRig.ActiveTransferrableObjectIndex(i);
			if (!(allObjects[num2] == null))
			{
				if (allObjects[num2].gameObject.activeSelf)
				{
					allObjects[num2].objectIndex = i;
				}
				else
				{
					objectsToEnable.Add(i);
				}
			}
		}
		for (int j = 0; j < allObjects.Length; j++)
		{
			if (!(allObjects[j] != null) || !allObjects[j].gameObject.activeSelf)
			{
				continue;
			}
			bool flag = true;
			for (int k = 0; k < myRig.ActiveTransferrableObjectIndexLength(); k++)
			{
				if (myRig.ActiveTransferrableObjectIndex(k) == j && myRig.IsItemAllowed(CosmeticsController.instance.GetItemNameFromDisplayName(allObjects[myRig.ActiveTransferrableObjectIndex(k)].gameObject.name)))
				{
					flag = false;
				}
			}
			if (flag)
			{
				objectsToDisable.Add(j);
			}
		}
		foreach (int item in objectsToDisable)
		{
			DisableTransferrableItem(item);
		}
		foreach (int item2 in objectsToEnable)
		{
			int allItemsIndex = myRig.ActiveTransferrableObjectIndex(item2);
			EnableTransferrableGameObject(allItemsIndex, myRig.TransferrableDockPosition(item2), myRig.TransferrablePosStates(item2));
		}
		UpdateHandState();
	}

	public int ReturnTransferrableItemIndex(int allItemsIndex)
	{
		for (int i = 0; i < myRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myRig.ActiveTransferrableObjectIndex(i) == allItemsIndex)
			{
				return i;
			}
		}
		return -1;
	}

	public List<int> TransferrableObjectIndexFromName(string transObjectName)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < allObjects.Length; i++)
		{
			if (!(allObjects[i] == null) && allObjects[i].gameObject.name == transObjectName)
			{
				list.Add(i);
			}
		}
		return list;
	}

	private TransferrableObject.PositionState MapDropPositionToState(DropPositions pos)
	{
		return pos switch
		{
			DropPositions.RightArm => TransferrableObject.PositionState.OnRightArm, 
			DropPositions.LeftArm => TransferrableObject.PositionState.OnLeftArm, 
			DropPositions.LeftBack => TransferrableObject.PositionState.OnLeftShoulder, 
			DropPositions.RightBack => TransferrableObject.PositionState.OnRightShoulder, 
			_ => TransferrableObject.PositionState.OnChest, 
		};
	}

	private void UpdateHandState()
	{
		for (int i = 0; i < 2; i++)
		{
			GameObject[] array = ((i == 0) ? leftHandThrowables : rightHandThrowables);
			int num = ((i == 0) ? myRig.LeftThrowableProjectileIndex : myRig.RightThrowableProjectileIndex);
			if (num > -1 && CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(i == 0, num, out var playfabId))
			{
				myRig.cosmeticsObjectRegistry.Cosmetic(playfabId);
			}
			for (int j = 0; j < array.Length; j++)
			{
				GameObject gameObject = array[j];
				if (!(gameObject == null))
				{
					bool activeSelf = gameObject.activeSelf;
					bool flag = gameObject.GetComponent<SnowballThrowable>().throwableMakerIndex == num;
					array[j].SetActive(flag);
					if (activeSelf && !flag)
					{
						throwableDisabledIndex[i] = j;
						throwableDisabledTime[i] = Time.time + 0.02f;
					}
				}
			}
		}
	}

	internal GameObject GetLeftHandThrowable()
	{
		return GetLeftHandThrowable(myRig.LeftThrowableProjectileIndex);
	}

	internal GameObject GetLeftHandThrowable(int throwableIndex)
	{
		if (throwableIndex < 0 || throwableIndex >= leftHandThrowables.Length)
		{
			throwableIndex = PreviousLeftHandThrowableIndex;
			if (throwableIndex < 0 || throwableIndex >= leftHandThrowables.Length || PreviousLeftHandThrowableDisabledTime < Time.time)
			{
				return null;
			}
		}
		return leftHandThrowables[throwableIndex];
	}

	internal GameObject GetRightHandThrowable()
	{
		return GetRightHandThrowable(myRig.RightThrowableProjectileIndex);
	}

	internal GameObject GetRightHandThrowable(int throwableIndex)
	{
		if (throwableIndex < 0 || throwableIndex >= rightHandThrowables.Length)
		{
			throwableIndex = PreviousRightHandThrowableIndex;
			if (throwableIndex < 0 || throwableIndex >= rightHandThrowables.Length || PreviousRightHandThrowableDisabledTime < Time.time)
			{
				return null;
			}
		}
		return rightHandThrowables[throwableIndex];
	}
}
