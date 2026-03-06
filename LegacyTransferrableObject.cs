using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class LegacyTransferrableObject : HoldableObject
{
	public enum InterpolateState
	{
		None,
		Interpolating
	}

	protected EquipmentInteractor interactor;

	public VRRig myRig;

	public VRRig myOnlineRig;

	public bool latched;

	private float indexTrigger;

	public bool testActivate;

	public bool testDeactivate;

	public float myThreshold = 0.8f;

	public float hysterisis = 0.05f;

	public bool flipOnXForLeftHand;

	public bool flipOnYForLeftHand;

	public bool flipOnXForLeftArm;

	public bool disableStealing;

	private TransferrableObject.PositionState initState;

	public TransferrableObject.ItemStates itemState;

	public BodyDockPositions.DropPositions storedZone;

	protected TransferrableObject.PositionState previousState;

	public TransferrableObject.PositionState currentState;

	public BodyDockPositions.DropPositions dockPositions;

	public VRRig targetRig;

	public BodyDockPositions targetDock;

	private VRRigAnchorOverrides anchorOverrides;

	public bool canAutoGrabLeft;

	public bool canAutoGrabRight;

	public int objectIndex;

	[Tooltip("In Holdables.prefab, assign to the parent of this transform.\nExample: 'Holdables/YellowHandBootsRight' is the anchor of 'Holdables/YellowHandBootsRight/YELLOW HAND BOOTS'")]
	public Transform anchor;

	[Tooltip("In Holdables.prefab, assign to the Collider to grab this object")]
	public InteractionPoint gripInteractor;

	[Tooltip("(Optional) Use this to override the transform used when the object is in the hand.\nExample: 'GHOST BALLOON' uses child 'grabPtAnchor' which is the end of the balloon's string.")]
	public Transform grabAnchor;

	public int myIndex;

	[Tooltip("(Optional)")]
	public GameObject[] gameObjectsActiveOnlyWhileHeld;

	protected GameObject worldShareableInstance;

	private float interpTime = 0.1f;

	private float interpDt;

	private Vector3 interpStartPos;

	private Quaternion interpStartRot;

	protected int enabledOnFrame = -1;

	private Vector3 initOffset;

	private Quaternion initRotation;

	public bool canDrop;

	public bool shareable;

	public bool detatchOnGrab;

	private bool wasHover;

	private bool isHover;

	private bool disableItem;

	public const int kPositionStateCount = 8;

	public InterpolateState interpState;

	protected void Awake()
	{
		latched = false;
		initOffset = base.transform.localPosition;
		initRotation = base.transform.localRotation;
	}

	protected virtual void Start()
	{
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	public void OnEnable()
	{
		if (myRig == null && myOnlineRig != null && myOnlineRig.netView != null && myOnlineRig.netView.IsMine)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (myRig == null && myOnlineRig == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		objectIndex = targetDock.ReturnTransferrableItemIndex(myIndex);
		if (myRig != null && myRig.isOfflineVRRig)
		{
			if (currentState == TransferrableObject.PositionState.OnLeftArm)
			{
				storedZone = BodyDockPositions.DropPositions.LeftArm;
			}
			else if (currentState == TransferrableObject.PositionState.OnRightArm)
			{
				storedZone = BodyDockPositions.DropPositions.RightArm;
			}
			else if (currentState == TransferrableObject.PositionState.OnLeftShoulder)
			{
				storedZone = BodyDockPositions.DropPositions.LeftBack;
			}
			else if (currentState == TransferrableObject.PositionState.OnRightShoulder)
			{
				storedZone = BodyDockPositions.DropPositions.RightBack;
			}
			else
			{
				storedZone = BodyDockPositions.DropPositions.Chest;
			}
		}
		if (objectIndex == -1)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (currentState == TransferrableObject.PositionState.OnLeftArm && flipOnXForLeftArm)
		{
			Transform transform = GetAnchor(currentState);
			transform.localScale = new Vector3(0f - transform.localScale.x, transform.localScale.y, transform.localScale.z);
		}
		initState = currentState;
		enabledOnFrame = Time.frameCount;
		SpawnShareableObject();
	}

	public void OnDisable()
	{
		enabledOnFrame = -1;
	}

	private void SpawnShareableObject()
	{
		if (PhotonNetwork.InRoom && (canDrop || shareable) && !(worldShareableInstance != null))
		{
			object[] data = new object[2]
			{
				myIndex,
				PhotonNetwork.LocalPlayer
			};
			worldShareableInstance = PhotonNetwork.Instantiate("Objects/equipment/WorldShareableItem", base.transform.position, base.transform.rotation, 0, data);
			if (myRig != null && worldShareableInstance != null)
			{
				OnWorldShareableItemSpawn();
			}
		}
	}

	public void OnJoinedRoom()
	{
		Debug.Log("Here");
		SpawnShareableObject();
	}

	public void OnLeftRoom()
	{
		if (worldShareableInstance != null)
		{
			PhotonNetwork.Destroy(worldShareableInstance);
		}
		OnWorldShareableItemDeallocated(NetworkSystem.Instance.LocalPlayer);
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		OnWorldShareableItemDeallocated(otherPlayer);
	}

	public void SetWorldShareableItem(GameObject item)
	{
		worldShareableInstance = item;
		OnWorldShareableItemSpawn();
	}

	protected virtual void OnWorldShareableItemSpawn()
	{
	}

	protected virtual void OnWorldShareableItemDeallocated(NetPlayer player)
	{
	}

	public virtual void LateUpdate()
	{
		if (interactor == null)
		{
			interactor = EquipmentInteractor.instance;
		}
		if (IsMyItem())
		{
			LateUpdateLocal();
		}
		else
		{
			LateUpdateReplicated();
		}
		LateUpdateShared();
		previousState = currentState;
	}

	protected Transform DefaultAnchor()
	{
		if (!(anchor == null))
		{
			return anchor;
		}
		return base.transform;
	}

	private Transform GetAnchor(TransferrableObject.PositionState pos)
	{
		if (grabAnchor == null)
		{
			return DefaultAnchor();
		}
		if (InHand())
		{
			return grabAnchor;
		}
		return DefaultAnchor();
	}

	protected bool Attached()
	{
		bool flag = InHand() && detatchOnGrab;
		if (!Dropped())
		{
			return !flag;
		}
		return false;
	}

	private void UpdateFollowXform()
	{
		if (targetRig == null)
		{
			return;
		}
		if (targetDock == null)
		{
			targetDock = targetRig.GetComponent<BodyDockPositions>();
		}
		if (anchorOverrides == null)
		{
			anchorOverrides = targetRig.GetComponent<VRRigAnchorOverrides>();
		}
		Transform transform = GetAnchor(currentState);
		Transform transform2 = transform;
		switch (currentState)
		{
		case TransferrableObject.PositionState.OnLeftArm:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftArmTransform);
			break;
		case TransferrableObject.PositionState.OnRightArm:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightArmTransform);
			break;
		case TransferrableObject.PositionState.InLeftHand:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftHandTransform);
			break;
		case TransferrableObject.PositionState.InRightHand:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightHandTransform);
			break;
		case TransferrableObject.PositionState.OnChest:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.chestTransform);
			break;
		case TransferrableObject.PositionState.OnLeftShoulder:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.leftBackTransform);
			break;
		case TransferrableObject.PositionState.OnRightShoulder:
			transform2 = anchorOverrides.AnchorOverride(currentState, targetDock.rightBackTransform);
			break;
		}
		switch (interpState)
		{
		case InterpolateState.None:
			if (transform2 != transform.parent)
			{
				if (Time.frameCount == enabledOnFrame)
				{
					transform.parent = transform2;
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
				else
				{
					interpState = InterpolateState.Interpolating;
					interpDt = interpTime;
					interpStartPos = transform.transform.position;
					interpStartRot = transform.transform.rotation;
				}
			}
			break;
		case InterpolateState.Interpolating:
		{
			float t = Mathf.Clamp((interpTime - interpDt) / interpTime, 0f, 1f);
			transform.transform.position = Vector3.Lerp(interpStartPos, transform2.transform.position, t);
			transform.transform.rotation = Quaternion.Slerp(interpStartRot, transform2.transform.rotation, t);
			interpDt -= Time.deltaTime;
			if (interpDt <= 0f)
			{
				transform.parent = transform2;
				interpState = InterpolateState.None;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
				if (flipOnXForLeftHand && currentState == TransferrableObject.PositionState.InLeftHand)
				{
					transform.localScale = new Vector3(-1f, 1f, 1f);
				}
				if (flipOnYForLeftHand && currentState == TransferrableObject.PositionState.InLeftHand)
				{
					transform.localScale = new Vector3(1f, -1f, 1f);
				}
			}
			break;
		}
		}
	}

	public void DropItem()
	{
		base.transform.parent = null;
	}

	protected virtual void LateUpdateShared()
	{
		disableItem = true;
		for (int i = 0; i < targetRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (targetRig.ActiveTransferrableObjectIndex(i) == myIndex)
			{
				disableItem = false;
				break;
			}
		}
		if (disableItem)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (previousState != currentState && detatchOnGrab && InHand())
		{
			base.transform.parent = null;
		}
		if (currentState != TransferrableObject.PositionState.Dropped)
		{
			UpdateFollowXform();
		}
		else if (canDrop)
		{
			DropItem();
		}
	}

	protected void ResetXf()
	{
		if (canDrop)
		{
			Transform transform = DefaultAnchor();
			if (base.transform != transform && base.transform.parent != transform)
			{
				base.transform.parent = transform;
			}
			base.transform.localPosition = initOffset;
			base.transform.localRotation = initRotation;
		}
	}

	protected void ReDock()
	{
		if (IsMyItem())
		{
			currentState = initState;
		}
		ResetXf();
	}

	private void HandleLocalInput()
	{
		GameObject[] array;
		if (!InHand())
		{
			array = gameObjectsActiveOnlyWhileHeld;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			return;
		}
		array = gameObjectsActiveOnlyWhileHeld;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		XRNode node = ((currentState == TransferrableObject.PositionState.InLeftHand) ? XRNode.LeftHand : XRNode.RightHand);
		indexTrigger = ControllerInputPoller.TriggerFloat(node);
		bool num = !latched && indexTrigger >= myThreshold;
		bool flag = latched && indexTrigger < myThreshold - hysterisis;
		if (num || testActivate)
		{
			testActivate = false;
			if (CanActivate())
			{
				OnActivate();
			}
		}
		else if (flag || testDeactivate)
		{
			testDeactivate = false;
			if (CanDeactivate())
			{
				OnDeactivate();
			}
		}
	}

	protected virtual void LateUpdateLocal()
	{
		wasHover = isHover;
		isHover = false;
		if (PhotonNetwork.InRoom)
		{
			myRig.SetTransferrablePosStates(objectIndex, currentState);
			myRig.SetTransferrableItemStates(objectIndex, itemState);
		}
		targetRig = myRig;
		HandleLocalInput();
	}

	protected virtual void LateUpdateReplicated()
	{
		currentState = myOnlineRig.TransferrablePosStates(objectIndex);
		if (currentState == TransferrableObject.PositionState.Dropped && !canDrop && !shareable)
		{
			if (previousState == TransferrableObject.PositionState.None)
			{
				base.gameObject.SetActive(value: false);
			}
			currentState = previousState;
		}
		itemState = myOnlineRig.TransferrableItemStates(objectIndex);
		targetRig = myOnlineRig;
		if (!(myOnlineRig != null))
		{
			return;
		}
		bool flag = true;
		for (int i = 0; i < myOnlineRig.ActiveTransferrableObjectIndexLength(); i++)
		{
			if (myOnlineRig.ActiveTransferrableObjectIndex(i) == myIndex)
			{
				flag = false;
				GameObject[] array = gameObjectsActiveOnlyWhileHeld;
				for (int j = 0; j < array.Length; j++)
				{
					array[j].SetActive(InHand());
				}
			}
		}
		if (flag)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public virtual void ResetToDefaultState()
	{
		canAutoGrabLeft = true;
		canAutoGrabRight = true;
		wasHover = false;
		isHover = false;
		ResetXf();
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!IsMyItem())
		{
			return;
		}
		if (grabbingHand == interactor.leftHand && currentState != TransferrableObject.PositionState.OnLeftArm)
		{
			if (currentState != TransferrableObject.PositionState.InRightHand || !disableStealing)
			{
				canAutoGrabLeft = false;
				currentState = TransferrableObject.PositionState.InLeftHand;
				EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: true);
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
		}
		else if (grabbingHand == interactor.rightHand && currentState != TransferrableObject.PositionState.OnRightArm && (currentState != TransferrableObject.PositionState.InLeftHand || !disableStealing))
		{
			canAutoGrabRight = false;
			currentState = TransferrableObject.PositionState.InRightHand;
			EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: false);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!IsMyItem())
		{
			return false;
		}
		if (!CanDeactivate())
		{
			return false;
		}
		if (IsHeld() && ((releasingHand == EquipmentInteractor.instance.rightHand && EquipmentInteractor.instance.rightHandHeldEquipment != null && this == (LegacyTransferrableObject)EquipmentInteractor.instance.rightHandHeldEquipment) || (releasingHand == EquipmentInteractor.instance.leftHand && EquipmentInteractor.instance.leftHandHeldEquipment != null && this == (LegacyTransferrableObject)EquipmentInteractor.instance.leftHandHeldEquipment)))
		{
			if (releasingHand == EquipmentInteractor.instance.leftHand)
			{
				canAutoGrabLeft = true;
			}
			else
			{
				canAutoGrabRight = true;
			}
			if (zoneReleased != null)
			{
				bool num = currentState == TransferrableObject.PositionState.InLeftHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.LeftArm;
				bool flag = currentState == TransferrableObject.PositionState.InRightHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.RightArm;
				if (num || flag)
				{
					return false;
				}
				if (targetDock.DropZoneStorageUsed(zoneReleased.dropPosition) == -1 && zoneReleased.forBodyDock == targetDock && (zoneReleased.dropPosition & dockPositions) != BodyDockPositions.DropPositions.None)
				{
					storedZone = zoneReleased.dropPosition;
				}
			}
			DropItemCleanup();
			EquipmentInteractor.instance.UpdateHandEquipment(null, releasingHand == EquipmentInteractor.instance.leftHand);
			return true;
		}
		return false;
	}

	public override void DropItemCleanup()
	{
		if (canDrop)
		{
			currentState = TransferrableObject.PositionState.Dropped;
			return;
		}
		switch (storedZone)
		{
		case BodyDockPositions.DropPositions.LeftArm:
			currentState = TransferrableObject.PositionState.OnLeftArm;
			break;
		case BodyDockPositions.DropPositions.RightArm:
			currentState = TransferrableObject.PositionState.OnRightArm;
			break;
		case BodyDockPositions.DropPositions.Chest:
			currentState = TransferrableObject.PositionState.OnChest;
			break;
		case BodyDockPositions.DropPositions.LeftBack:
			currentState = TransferrableObject.PositionState.OnLeftShoulder;
			break;
		case BodyDockPositions.DropPositions.RightBack:
			currentState = TransferrableObject.PositionState.OnRightShoulder;
			break;
		}
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
		if (IsMyItem())
		{
			if (!wasHover)
			{
				GorillaTagger.Instance.StartVibration(hoveringHand == EquipmentInteractor.instance.leftHand, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
			isHover = true;
		}
	}

	protected void ActivateItemFX(float hapticStrength, float hapticDuration, int soundIndex, float soundVolume)
	{
		bool flag = currentState == TransferrableObject.PositionState.InLeftHand;
		if ((bool)targetRig?.netView)
		{
			targetRig.rigSerializer.RPC_PlayHandTap(soundIndex, flag, 0.1f);
		}
		myRig.PlayHandTapLocal(soundIndex, flag, soundVolume);
		GorillaTagger.Instance.StartVibration(flag, hapticStrength, hapticDuration);
	}

	public virtual void PlayNote(int note, float volume)
	{
	}

	public virtual bool AutoGrabTrue(bool leftGrabbingHand)
	{
		if (!leftGrabbingHand)
		{
			return canAutoGrabRight;
		}
		return canAutoGrabLeft;
	}

	public virtual bool CanActivate()
	{
		return true;
	}

	public virtual bool CanDeactivate()
	{
		return true;
	}

	public virtual void OnActivate()
	{
		latched = true;
	}

	public virtual void OnDeactivate()
	{
		latched = false;
	}

	public virtual bool IsMyItem()
	{
		if (myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	protected virtual bool IsHeld()
	{
		if (EquipmentInteractor.instance.leftHandHeldEquipment == null || !((LegacyTransferrableObject)EquipmentInteractor.instance.leftHandHeldEquipment == this))
		{
			if (EquipmentInteractor.instance.rightHandHeldEquipment != null)
			{
				return (LegacyTransferrableObject)EquipmentInteractor.instance.rightHandHeldEquipment == this;
			}
			return false;
		}
		return true;
	}

	public bool InHand()
	{
		if (currentState != TransferrableObject.PositionState.InLeftHand)
		{
			return currentState == TransferrableObject.PositionState.InRightHand;
		}
		return true;
	}

	public bool Dropped()
	{
		return currentState == TransferrableObject.PositionState.Dropped;
	}

	public bool InLeftHand()
	{
		return currentState == TransferrableObject.PositionState.InLeftHand;
	}

	public bool InRightHand()
	{
		return currentState == TransferrableObject.PositionState.InRightHand;
	}

	public bool OnChest()
	{
		return currentState == TransferrableObject.PositionState.OnChest;
	}

	public bool OnShoulder()
	{
		if (currentState != TransferrableObject.PositionState.OnLeftShoulder)
		{
			return currentState == TransferrableObject.PositionState.OnRightShoulder;
		}
		return true;
	}

	protected NetPlayer OwningPlayer()
	{
		if (myRig == null)
		{
			return myOnlineRig.netView.Owner;
		}
		return NetworkSystem.Instance.LocalPlayer;
	}
}
