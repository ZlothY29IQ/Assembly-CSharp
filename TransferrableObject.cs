using System;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using JetBrains.Annotations;
using Photon.Pun;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;

public class TransferrableObject : HoldableObject, ISelfValidator, IRequestableOwnershipGuardCallbacks, IPreDisable, ISpawnable, IBuildValidation
{
	public enum SyncOptions
	{
		None,
		Bool,
		Int
	}

	public enum ItemStates
	{
		State0 = 1,
		State1 = 2,
		State2 = 4,
		State3 = 8,
		State4 = 0x10,
		State5 = 0x20,
		Part0Held = 0x40,
		Part1Held = 0x80
	}

	public enum GrabType
	{
		Default,
		Free
	}

	[Flags]
	public enum PositionState
	{
		OnLeftArm = 1,
		OnRightArm = 2,
		InLeftHand = 4,
		InRightHand = 8,
		OnChest = 0x10,
		OnLeftShoulder = 0x20,
		OnRightShoulder = 0x40,
		Dropped = 0x80,
		None = 0
	}

	public enum InterpolateState
	{
		None,
		Interpolating
	}

	private VRRig _myRig;

	private VRRig _myOnlineRig;

	public bool latched;

	private float indexTrigger;

	public bool testActivate;

	public bool testDeactivate;

	[Tooltip("When the grip/trigger input is greater than this value the transferrable object is activated")]
	public float myThreshold = 0.8f;

	[Tooltip("When the grip/trigger input is less than (myThreshold - hysterisis) the transferrable object is deactivated")]
	public float hysterisis = 0.05f;

	[Tooltip("Set the x scale to -1 when held in left hand")]
	public bool flipOnXForLeftHand;

	[Tooltip("Set the y scale to -1 when held in left hand")]
	public bool flipOnYForLeftHand;

	[Tooltip("Set the x scale to -1 when docked on left arm")]
	public bool flipOnXForLeftArm;

	[Tooltip("disable grabbing the item from out of your other hand")]
	public bool disableStealing;

	[Tooltip("Allow other players to pick up this item")]
	public bool allowPlayerStealing;

	private PositionState initState;

	public ItemStates itemState;

	protected ItemStates previousItemState;

	protected const int HELD_BIT_MASK = 64;

	private const int BOOL_A_BITMASK = 1;

	private const int BOOL_B_BITMASK = 2;

	private const int BOOL_C_BITMASK = 4;

	private const int BOOL_D_BITMASK = 8;

	[DevInspectorShow]
	public BodyDockPositions.DropPositions storedZone;

	protected PositionState previousState;

	[DevInspectorYellow]
	[DevInspectorShow]
	public PositionState currentState;

	public BodyDockPositions.DropPositions dockPositions;

	[DevInspectorCyan]
	[DevInspectorShow]
	public AdvancedItemState advancedGrabState;

	[DevInspectorShow]
	[DevInspectorCyan]
	public VRRig targetRig;

	[HideInInspector]
	public bool targetRigSet;

	public GrabType useGrabType;

	[DevInspectorShow]
	[DevInspectorCyan]
	public VRRig ownerRig;

	[NonSerialized]
	[DebugReadout]
	public BodyDockPositions targetDockPositions;

	private VRRigAnchorOverrides anchorOverrides;

	public bool canAutoGrabLeft;

	public bool canAutoGrabRight;

	[DevInspectorShow]
	public int objectIndex;

	[NonSerialized]
	public Transform anchor;

	[Tooltip("In Functional prefab, assign to the Collider to grab this object")]
	public InteractionPoint gripInteractor;

	[Tooltip("(Optional) Use this to override the transform used when the object is in the hand.\nExample: 'GHOST BALLOON' uses child 'grabPtAnchor' which is the end of the balloon's string.")]
	public Transform grabAnchor;

	[Tooltip("(Optional) Use this (with the GorillaHandClosed_Left mesh) to intuitively define how\nthe player holds this object, by placing a representation of their hand gripping it.")]
	public Transform handPoseLeft;

	[Tooltip("(Optional) Use this (with the GorillaHandClosed_Right mesh) to intuitively define how\nthe player holds this object, by placing a representation of their hand gripping it.")]
	public Transform handPoseRight;

	[HideInInspector]
	public bool isGrabAnchorSet;

	private static Vector3 handPoseRightReferencePoint = new Vector3(-0.0141f, 0.0065f, -0.278f);

	private static Quaternion handPoseRightReferenceRotation = Quaternion.Euler(-2.058f, -17.2f, 65.05f);

	private static Vector3 handPoseLeftReferencePoint = new Vector3(0.0136f, 0.0045f, -0.2809f);

	private static Quaternion handPoseLeftReferenceRotation = Quaternion.Euler(-0.58f, 21.356f, -63.965f);

	public TransferrableItemSlotTransformOverride transferrableItemSlotTransformOverride;

	public int myIndex;

	[Tooltip("(Optional) objects to enable when held in hand and disable when not in hand")]
	public GameObject[] gameObjectsActiveOnlyWhileHeld;

	[Tooltip("(Optional) objects to disable when held in hand and enable when not in hand")]
	public GameObject[] gameObjectsActiveOnlyWhileDocked;

	[Tooltip("(Optional) components to enable when held in hand and disable when not in hand")]
	public Behaviour[] behavioursEnabledOnlyWhileHeld;

	[Tooltip("(Optional) components to disable when held in hand and enable when not in hand")]
	public Behaviour[] behavioursEnabledOnlyWhileDocked;

	[SerializeField]
	protected internal WorldShareableItem worldShareableInstance;

	private float interpTime = 0.2f;

	private float interpDt;

	private Vector3 interpStartPos;

	private Quaternion interpStartRot;

	protected int enabledOnFrame = -1;

	protected Vector3 initOffset;

	protected Quaternion initRotation;

	private Matrix4x4 initMatrix = Matrix4x4.identity;

	private Matrix4x4 leftHandMatrix = Matrix4x4.identity;

	private Matrix4x4 rightHandMatrix = Matrix4x4.identity;

	private bool positionInitialized;

	public bool isSceneObject;

	public Rigidbody rigidbodyInstance;

	public bool canDrop;

	[Tooltip("completely drop the item instead of auto-returning to a stored zone")]
	public bool allowReparenting;

	[Tooltip("(Scene object) has a worldSharableInstance")]
	public bool shareable;

	[Tooltip("(Balloon) Unparent this object from the rig when grabbed")]
	public bool detatchOnGrab;

	[Tooltip("(Balloon) is this cosmetic droppable in the world")]
	public bool allowWorldSharableInstance;

	[ItemCanBeNull]
	public Transform originPoint;

	[ItemCanBeNull]
	public float maxDistanceFromOriginBeforeRespawn;

	public AudioClip resetPositionAudioClip;

	public float maxDistanceFromTargetPlayerBeforeRespawn;

	private bool wasHover;

	private bool isHover;

	private bool disableItem;

	protected bool loaded;

	public bool ClearLocalPositionOnReset;

	[SerializeField]
	protected SyncOptions networkedStateEvents;

	[SerializeField]
	protected bool resetOnDocked = true;

	[SerializeField]
	protected string boolADebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolFalse;

	[SerializeField]
	protected string boolBDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolBTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolBFalse;

	[SerializeField]
	protected string boolCDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolCTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolCFalse;

	[SerializeField]
	protected string boolDDebugName;

	[SerializeField]
	protected UnityEvent OnItemStateBoolDTrue;

	[SerializeField]
	protected UnityEvent OnItemStateBoolDFalse;

	[SerializeField]
	protected UnityEvent<int> OnItemStateIntChanged;

	[FormerlySerializedAs("OnUndocked")]
	[SerializeField]
	private UnityEvent OnHeldLocal;

	[SerializeField]
	private UnityEvent OnHeldShared;

	[FormerlySerializedAs("OnDocked")]
	[SerializeField]
	private UnityEvent OnDockedLocal;

	[FormerlySerializedAs("OnDockedLocal")]
	[SerializeField]
	private UnityEvent OnDockedShared;

	private bool wasHeldLocal;

	private bool wasHeldShared;

	[Tooltip("(Optional) name broadcast by PlayerGameEvents")]
	public string interactEventName;

	public const int kPositionStateCount = 8;

	[DevInspectorShow]
	public InterpolateState interpState;

	public bool startInterpolation;

	public Transform InitialDockObject;

	private AudioSource audioSrc;

	protected Transform _defaultAnchor;

	protected bool _isDefaultAnchorSet;

	private Matrix4x4? transferrableItemSlotTransformOverrideCachedMatrix;

	private bool transferrableItemSlotTransformOverrideApplicable;

	public VRRig myRig
	{
		get
		{
			return _myRig;
		}
		private set
		{
			_myRig = value;
		}
	}

	public bool isMyRigValid { get; private set; }

	public VRRig myOnlineRig
	{
		get
		{
			return _myOnlineRig;
		}
		private set
		{
			_myOnlineRig = value;
			isMyOnlineRigValid = true;
		}
	}

	public bool isMyOnlineRigValid { get; private set; }

	public bool IsLocalOwnedWorldShareable
	{
		get
		{
			if (!worldShareableInstance)
			{
				return false;
			}
			return worldShareableInstance.guard.isTrulyMine;
		}
	}

	public bool isRigidbodySet { get; private set; }

	public bool shouldUseGravity { get; private set; }

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void FixTransformOverride()
	{
		transferrableItemSlotTransformOverride = GetComponent<TransferrableItemSlotTransformOverride>();
	}

	public void Validate(SelfValidationResult result)
	{
	}

	public void SetTargetRig(VRRig rig)
	{
		if (rig == null)
		{
			targetRigSet = false;
			if (isSceneObject)
			{
				targetRig = rig;
				targetDockPositions = null;
				anchorOverrides = null;
				return;
			}
			if ((bool)myRig)
			{
				SetTargetRig(myRig);
			}
			if ((bool)myOnlineRig)
			{
				SetTargetRig(myOnlineRig);
			}
			return;
		}
		targetRigSet = true;
		targetRig = rig;
		BodyDockPositions component = rig.GetComponent<BodyDockPositions>();
		VRRigAnchorOverrides component2 = rig.GetComponent<VRRigAnchorOverrides>();
		if (!component)
		{
			Debug.LogError("There is no dock attached to this rig", this);
			return;
		}
		if (!component2)
		{
			Debug.LogError("There is no overrides attached to this rig", this);
			return;
		}
		anchorOverrides = component2;
		targetDockPositions = component;
		if (interpState == InterpolateState.Interpolating)
		{
			interpState = InterpolateState.None;
		}
	}

	public void WorldShareableRequestOwnership()
	{
		if (worldShareableInstance != null && !worldShareableInstance.guard.isMine)
		{
			worldShareableInstance.guard.RequestOwnershipImmediately(delegate
			{
			});
		}
	}

	protected virtual void Awake()
	{
		if (isSceneObject)
		{
			IsSpawned = true;
			OnSpawn(null);
		}
	}

	public virtual void OnSpawn(VRRig rig)
	{
		try
		{
			if (!isSceneObject)
			{
				if (!rig)
				{
					Debug.LogError("Disabling TransferrableObject because could not find VRRig! \"" + base.transform.GetPath() + "\"", this);
					base.enabled = false;
					isMyRigValid = false;
					isMyOnlineRigValid = false;
					return;
				}
				myRig = (rig.isOfflineVRRig ? rig : null);
				myOnlineRig = (rig.isOfflineVRRig ? null : rig);
				targetDockPositions = rig.myBodyDockPositions;
			}
			else
			{
				myRig = null;
				myOnlineRig = null;
			}
			isMyRigValid = true;
			isMyOnlineRigValid = true;
			if (isSceneObject)
			{
				targetDockPositions = GetComponentInParent<BodyDockPositions>();
			}
			anchor = base.transform.parent;
			if (rigidbodyInstance == null)
			{
				rigidbodyInstance = GetComponent<Rigidbody>();
			}
			if (rigidbodyInstance != null)
			{
				isRigidbodySet = true;
				shouldUseGravity = rigidbodyInstance.useGravity;
			}
			audioSrc = GetComponent<AudioSource>();
			latched = false;
			if (!positionInitialized)
			{
				SetInitMatrix();
				positionInitialized = true;
			}
			if (anchor == null)
			{
				InitialDockObject = base.transform.parent;
			}
			else
			{
				InitialDockObject = anchor.parent;
			}
			isGrabAnchorSet = grabAnchor != null;
			if (!isSceneObject)
			{
				return;
			}
			ISpawnable[] componentsInChildren = GetComponentsInChildren<ISpawnable>(includeInactive: true);
			foreach (ISpawnable spawnable in componentsInChildren)
			{
				if (spawnable != this)
				{
					spawnable.IsSpawned = true;
					spawnable.CosmeticSelectedSide = CosmeticSelectedSide;
					spawnable.OnSpawn(myRig);
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
			base.enabled = false;
			base.gameObject.SetActive(value: false);
			Debug.LogError("TransferrableObject: Disabled & deactivated self because of the exception logged above. Path: " + base.transform.GetPathQ(), this);
		}
	}

	public virtual void OnDespawn()
	{
		try
		{
			if (isSceneObject)
			{
				return;
			}
			ISpawnable[] componentsInChildren = GetComponentsInChildren<ISpawnable>(includeInactive: true);
			foreach (ISpawnable spawnable in componentsInChildren)
			{
				if (spawnable != this)
				{
					spawnable.IsSpawned = false;
					spawnable.OnDespawn();
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
			base.enabled = false;
			base.gameObject.SetActive(value: false);
			Debug.LogError("TransferrableObject: Disabled & deactivated self because of the exception logged above. Path: " + base.transform.GetPathQ(), this);
		}
	}

	private void SetInitMatrix()
	{
		initMatrix = base.transform.LocalMatrixRelativeToParentWithScale();
		if (handPoseLeft != null)
		{
			base.transform.localRotation = handPoseLeftReferenceRotation * Quaternion.Inverse(handPoseLeft.localRotation);
			base.transform.position += base.transform.parent.TransformPoint(handPoseLeftReferencePoint) - handPoseLeft.transform.position;
			leftHandMatrix = base.transform.LocalMatrixRelativeToParentWithScale();
		}
		else
		{
			leftHandMatrix = initMatrix;
		}
		if (handPoseRight != null)
		{
			base.transform.localRotation = handPoseRightReferenceRotation * Quaternion.Inverse(handPoseRight.localRotation);
			base.transform.position += base.transform.parent.TransformPoint(handPoseRightReferencePoint) - handPoseRight.transform.position;
			rightHandMatrix = base.transform.LocalMatrixRelativeToParentWithScale();
		}
		else
		{
			rightHandMatrix = initMatrix;
		}
		base.transform.localPosition = initMatrix.Position();
		base.transform.localRotation = initMatrix.Rotation();
		positionInitialized = true;
	}

	protected virtual void Start()
	{
	}

	internal virtual void OnEnable()
	{
		try
		{
			if (ApplicationQuittingState.IsQuitting)
			{
				return;
			}
			RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
			RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
			OnEnable_AfterAllCosmeticsSpawnedOrIsSceneObject();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
			base.enabled = false;
			base.gameObject.SetActive(value: false);
			Debug.LogError("TransferrableObject: Disabled & deactivated self because of the exception logged above. Path: " + base.transform.GetPathQ(), this);
		}
		if (networkedStateEvents != SyncOptions.None)
		{
			previousItemState = (ItemStates)0;
			itemState = (ItemStates)0;
		}
	}

	public virtual void OnEnable_AfterAllCosmeticsSpawnedOrIsSceneObject()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (!base.enabled)
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			try
			{
				TransferrableObjectManager.Register(this);
				transferrableItemSlotTransformOverride = GetComponent<TransferrableItemSlotTransformOverride>();
				if (!positionInitialized)
				{
					SetInitMatrix();
					positionInitialized = true;
				}
				if (isSceneObject)
				{
					if (!worldShareableInstance)
					{
						Debug.LogError("Missing Sharable Instance on Scene enabled object: " + base.gameObject.name);
						return;
					}
					worldShareableInstance.SyncToSceneObject(this);
					worldShareableInstance.GetComponent<RequestableOwnershipGuard>().AddCallbackTarget(this);
					return;
				}
				if (!isSceneObject && !myRig && !myOnlineRig && !ownerRig)
				{
					ownerRig = GetComponentInParent<VRRig>(includeInactive: true);
					if (ownerRig.isOfflineVRRig)
					{
						myRig = ownerRig;
					}
					else
					{
						myOnlineRig = ownerRig;
					}
				}
				if (!myRig && (bool)myOnlineRig)
				{
					ownerRig = myOnlineRig;
					SetTargetRig(myOnlineRig);
				}
				if (!IsSpawned)
				{
					IsSpawned = true;
					OnSpawn((myRig != null) ? myRig : myOnlineRig);
				}
				if (myRig == null && myOnlineRig == null)
				{
					if (!isSceneObject)
					{
						base.gameObject.SetActive(value: false);
					}
					return;
				}
				objectIndex = targetDockPositions.ReturnTransferrableItemIndex(myIndex);
				if (currentState == PositionState.OnLeftArm)
				{
					storedZone = BodyDockPositions.DropPositions.LeftArm;
				}
				else if (currentState == PositionState.OnRightArm)
				{
					storedZone = BodyDockPositions.DropPositions.RightArm;
				}
				else if (currentState == PositionState.OnLeftShoulder)
				{
					storedZone = BodyDockPositions.DropPositions.LeftBack;
				}
				else if (currentState == PositionState.OnRightShoulder)
				{
					storedZone = BodyDockPositions.DropPositions.RightBack;
				}
				else if (currentState == PositionState.OnChest)
				{
					storedZone = BodyDockPositions.DropPositions.Chest;
				}
				if (IsLocalObject())
				{
					ownerRig = GorillaTagger.Instance.offlineVRRig;
					SetTargetRig(GorillaTagger.Instance.offlineVRRig);
				}
				if (objectIndex == -1)
				{
					base.gameObject.SetActive(value: false);
					return;
				}
				if (currentState == PositionState.OnLeftArm && flipOnXForLeftArm)
				{
					Transform transform = GetAnchor(currentState);
					transform.localScale = new Vector3(0f - transform.localScale.x, transform.localScale.y, transform.localScale.z);
				}
				initState = currentState;
				enabledOnFrame = Time.frameCount;
				startInterpolation = true;
				if (NetworkSystem.Instance.InRoom && (canDrop || shareable))
				{
					SpawnTransferableObjectViews();
					if ((bool)myRig && myRig != null && worldShareableInstance != null)
					{
						OnWorldShareableItemSpawn();
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception, this);
				base.enabled = false;
				base.gameObject.SetActive(value: false);
				Debug.LogError("TransferrableObject: Disabled & deactivated self because of the exception logged above. Path: " + base.transform.GetPathQ(), this);
			}
		}
	}

	internal virtual void OnDisable()
	{
		TransferrableObjectManager.Unregister(this);
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
		RoomSystem.LeftRoomEvent -= new Action(OnLeftRoom);
		enabledOnFrame = -1;
		base.transform.localScale = Vector3.one;
		try
		{
			if (!isSceneObject && IsLocalObject() && (bool)worldShareableInstance && !IsMyItem())
			{
				worldShareableInstance.GetComponent<RequestableOwnershipGuard>().RequestOwnershipImmediately(delegate
				{
				});
			}
			if ((bool)worldShareableInstance)
			{
				worldShareableInstance.Invalidate();
				worldShareableInstance.GetComponent<RequestableOwnershipGuard>().RemoveCallbackTarget(this);
				if ((bool)targetDockPositions)
				{
					targetDockPositions.DeallocateSharableInstance(worldShareableInstance);
				}
				if (!isSceneObject)
				{
					worldShareableInstance = null;
				}
			}
			PlayDestroyedOrDisabledEffect();
			if (isSceneObject)
			{
				IsSpawned = false;
				OnDespawn();
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
			base.enabled = false;
			base.gameObject.SetActive(value: false);
			Debug.LogError("TransferrableObject: Disabled & deactivated self because of the exception logged above. Path: " + base.transform.GetPathQ(), this);
		}
	}

	protected new virtual void OnDestroy()
	{
		TransferrableObjectManager.Unregister(this);
	}

	public void CleanupDisable()
	{
		currentState = PositionState.None;
		enabledOnFrame = -1;
		if ((bool)anchor)
		{
			anchor.parent = InitialDockObject;
			if (anchor != base.transform)
			{
				base.transform.parent = anchor;
			}
		}
		else
		{
			base.transform.parent = anchor;
		}
		interpState = InterpolateState.None;
		base.transform.SetLocalMatrixRelativeToParentWithXParity(GetDefaultTransformationMatrix());
	}

	public virtual void PreDisable()
	{
		itemState = ItemStates.State0;
		if (networkedStateEvents != SyncOptions.None)
		{
			previousItemState = (ItemStates)0;
			itemState = (ItemStates)0;
		}
		currentState = PositionState.None;
		interpState = InterpolateState.None;
		ResetToDefaultState();
	}

	public virtual Matrix4x4 GetDefaultTransformationMatrix()
	{
		return currentState switch
		{
			PositionState.InLeftHand => leftHandMatrix, 
			PositionState.InRightHand => rightHandMatrix, 
			_ => initMatrix, 
		};
	}

	public virtual bool ShouldBeKinematic()
	{
		if (detatchOnGrab)
		{
			if (currentState == PositionState.Dropped || currentState == PositionState.InLeftHand || currentState == PositionState.InRightHand)
			{
				return false;
			}
			return true;
		}
		if (currentState == PositionState.Dropped)
		{
			return false;
		}
		return true;
	}

	private void SpawnShareableObject()
	{
		if (isSceneObject)
		{
			if (!(worldShareableInstance == null))
			{
				worldShareableInstance.GetComponent<WorldShareableItem>().SetupSceneObjectOnNetwork(NetworkSystem.Instance.MasterClient);
			}
		}
		else if (NetworkSystem.Instance.InRoom)
		{
			SpawnTransferableObjectViews();
			if ((bool)myRig && (canDrop || shareable) && myRig != null && worldShareableInstance != null)
			{
				OnWorldShareableItemSpawn();
			}
		}
	}

	public void SpawnTransferableObjectViews()
	{
		NetPlayer owner = NetworkSystem.Instance.LocalPlayer;
		if (!ownerRig.isOfflineVRRig)
		{
			owner = ownerRig.creator;
		}
		if (worldShareableInstance == null)
		{
			worldShareableInstance = targetDockPositions.AllocateSharableInstance(storedZone, owner);
		}
		GorillaTagger.OnPlayerSpawned(delegate
		{
			worldShareableInstance.SetupSharableObject(myIndex, owner, base.transform);
		});
	}

	public virtual void OnJoinedRoom()
	{
		if (isSceneObject)
		{
			_ = worldShareableInstance == null;
		}
		else if (NetworkSystem.Instance.InRoom && (canDrop || shareable))
		{
			SpawnTransferableObjectViews();
			if ((bool)myRig && myRig != null && worldShareableInstance != null)
			{
				OnWorldShareableItemSpawn();
			}
		}
	}

	public virtual void OnLeftRoom()
	{
		if (ApplicationQuittingState.IsQuitting || isSceneObject || (!shareable && !allowWorldSharableInstance && !canDrop))
		{
			return;
		}
		if (base.gameObject.activeSelf && (bool)worldShareableInstance)
		{
			worldShareableInstance.Invalidate();
			worldShareableInstance.GetComponent<RequestableOwnershipGuard>().RemoveCallbackTarget(this);
			if ((bool)targetDockPositions)
			{
				targetDockPositions.DeallocateSharableInstance(worldShareableInstance);
			}
			else
			{
				worldShareableInstance.ResetViews();
				ObjectPools.instance.Destroy(worldShareableInstance.gameObject);
			}
			worldShareableInstance = null;
		}
		if (!IsLocalObject())
		{
			OnItemDestroyedOrDisabled();
			base.gameObject.Disable();
		}
	}

	public bool IsLocalObject()
	{
		if ((object)myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	public void SetWorldShareableItem(WorldShareableItem item)
	{
		worldShareableInstance = item;
		OnWorldShareableItemSpawn();
	}

	protected virtual void OnWorldShareableItemSpawn()
	{
	}

	protected virtual void PlayDestroyedOrDisabledEffect()
	{
	}

	protected virtual void OnItemDestroyedOrDisabled()
	{
		if ((bool)worldShareableInstance)
		{
			worldShareableInstance.Invalidate();
			worldShareableInstance.GetComponent<RequestableOwnershipGuard>().RemoveCallbackTarget(this);
			if ((bool)targetDockPositions)
			{
				targetDockPositions.DeallocateSharableInstance(worldShareableInstance);
			}
			Debug.LogError("Setting WSI to null in OnItemDestroyedOrDisabled", this);
			worldShareableInstance = null;
		}
		PlayDestroyedOrDisabledEffect();
		enabledOnFrame = -1;
		currentState = PositionState.None;
	}

	public virtual void TriggeredLateUpdate()
	{
		if (IsLocalObject() && canDrop)
		{
			LocalMyObjectValidation();
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
	}

	protected Transform DefaultAnchor()
	{
		if (_isDefaultAnchorSet)
		{
			return _defaultAnchor;
		}
		_isDefaultAnchorSet = true;
		_defaultAnchor = ((anchor == null) ? base.transform : anchor);
		return _defaultAnchor;
	}

	private Transform GetAnchor(PositionState pos)
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

	private Transform GetTargetStorageZone(BodyDockPositions.DropPositions state)
	{
		return state switch
		{
			BodyDockPositions.DropPositions.LeftArm => targetDockPositions.leftArmTransform, 
			BodyDockPositions.DropPositions.RightArm => targetDockPositions.rightArmTransform, 
			BodyDockPositions.DropPositions.Chest => targetDockPositions.chestTransform, 
			BodyDockPositions.DropPositions.LeftBack => targetDockPositions.leftBackTransform, 
			BodyDockPositions.DropPositions.RightBack => targetDockPositions.rightBackTransform, 
			BodyDockPositions.DropPositions.None => null, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public static Transform GetTargetDock(PositionState state, VRRig rig)
	{
		return GetTargetDock(state, rig.myBodyDockPositions, rig.GetComponent<VRRigAnchorOverrides>());
	}

	public static Transform GetTargetDock(PositionState state, BodyDockPositions dockPositions, VRRigAnchorOverrides anchorOverrides)
	{
		return state switch
		{
			PositionState.OnLeftArm => anchorOverrides.AnchorOverride(state, dockPositions.leftArmTransform), 
			PositionState.OnRightArm => anchorOverrides.AnchorOverride(state, dockPositions.rightArmTransform), 
			PositionState.InLeftHand => anchorOverrides.AnchorOverride(state, dockPositions.leftHandTransform), 
			PositionState.InRightHand => anchorOverrides.AnchorOverride(state, dockPositions.rightHandTransform), 
			PositionState.OnChest => anchorOverrides.AnchorOverride(state, dockPositions.chestTransform), 
			PositionState.OnLeftShoulder => anchorOverrides.AnchorOverride(state, dockPositions.leftBackTransform), 
			PositionState.OnRightShoulder => anchorOverrides.AnchorOverride(state, dockPositions.rightBackTransform), 
			_ => null, 
		};
	}

	private void UpdateFollowXform()
	{
		if (!targetRigSet)
		{
			return;
		}
		Transform transform = GetAnchor(currentState);
		Transform transform2 = transform;
		try
		{
			transform2 = GetTargetDock(currentState, targetDockPositions, anchorOverrides);
		}
		catch
		{
			Debug.LogError("anchorOverrides or targetDock has been destroyed", this);
			SetTargetRig(null);
		}
		if (currentState != PositionState.Dropped && (bool)rigidbodyInstance && ShouldBeKinematic() && !rigidbodyInstance.isKinematic)
		{
			rigidbodyInstance.isKinematic = true;
		}
		if (detatchOnGrab && (currentState == PositionState.InLeftHand || currentState == PositionState.InRightHand))
		{
			base.transform.parent = null;
		}
		if (interpState == InterpolateState.None)
		{
			try
			{
				if ((object)transform == null)
				{
					return;
				}
				startInterpolation |= transform2 != transform.parent;
			}
			catch
			{
			}
			if (!startInterpolation && !isGrabAnchorSet && base.transform.parent != transform && transform != base.transform)
			{
				startInterpolation = true;
			}
			if (startInterpolation)
			{
				Vector3 position = base.transform.position;
				Quaternion rotation = base.transform.rotation;
				if (base.transform.parent != transform && transform != base.transform)
				{
					base.transform.parent = transform;
				}
				transform.parent = transform2;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				if (currentState == PositionState.InLeftHand)
				{
					if (flipOnXForLeftHand)
					{
						transform.localScale = new Vector3(-1f, 1f, 1f);
					}
					else if (flipOnYForLeftHand)
					{
						transform.localScale = new Vector3(1f, -1f, 1f);
					}
					else
					{
						transform.localScale = Vector3.one;
					}
				}
				else
				{
					transform.localScale = Vector3.one;
				}
				if (Time.frameCount == enabledOnFrame || Time.frameCount == enabledOnFrame + 1)
				{
					Matrix4x4 matrix4x = GetDefaultTransformationMatrix();
					if ((currentState != PositionState.InLeftHand || !(handPoseLeft != null)) && currentState == PositionState.InRightHand)
					{
						_ = handPoseRight != null;
					}
					if ((bool)transferrableItemSlotTransformOverride && transferrableItemSlotTransformOverride.GetTransformFromPositionState(currentState, advancedGrabState, transform2, out var matrix4X))
					{
						matrix4x = matrix4X;
					}
					Matrix4x4 matrix = transform.localToWorldMatrix * matrix4x;
					base.transform.SetLocalToWorldMatrixNoScale(matrix);
					base.transform.localScale = matrix.lossyScale;
				}
				else
				{
					interpState = InterpolateState.Interpolating;
					if (IsMyItem() && useGrabType == GrabType.Free)
					{
						bool flag = currentState == PositionState.InLeftHand;
						if (!flag)
						{
							_ = EquipmentInteractor.instance.rightHand;
						}
						else
						{
							_ = EquipmentInteractor.instance.leftHand;
						}
						Transform targetDock = GetTargetDock(currentState, GorillaTagger.Instance.offlineVRRig);
						SetupMatrixForFreeGrab(position, rotation, targetDock, flag);
					}
					interpDt = interpTime;
					interpStartRot = rotation;
					interpStartPos = position;
					base.transform.position = position;
					base.transform.rotation = rotation;
				}
				startInterpolation = false;
			}
		}
		if (interpState != InterpolateState.Interpolating)
		{
			return;
		}
		Matrix4x4 matrix4x2 = GetDefaultTransformationMatrix();
		if ((object)transferrableItemSlotTransformOverride != null)
		{
			if (!transferrableItemSlotTransformOverrideCachedMatrix.HasValue)
			{
				transferrableItemSlotTransformOverrideApplicable = transferrableItemSlotTransformOverride.GetTransformFromPositionState(currentState, advancedGrabState, transform2, out var matrix4X2);
				transferrableItemSlotTransformOverrideCachedMatrix = matrix4X2;
			}
			if (transferrableItemSlotTransformOverrideApplicable)
			{
				matrix4x2 = transferrableItemSlotTransformOverrideCachedMatrix.Value;
			}
		}
		float t = Mathf.Clamp((interpTime - interpDt) / interpTime, 0f, 1f);
		Mathf.SmoothStep(0f, 1f, t);
		Matrix4x4 m = transform.localToWorldMatrix * matrix4x2;
		base.transform.position = interpStartPos.LerpToUnclamped(m.Position(), t);
		base.transform.rotation = Quaternion.Slerp(interpStartRot, m.Rotation(), t);
		base.transform.localScale = matrix4x2.lossyScale;
		interpDt -= Time.deltaTime;
		if (interpDt <= 0f)
		{
			transform.parent = transform2;
			interpState = InterpolateState.None;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
			if (flipOnXForLeftHand && currentState == PositionState.InLeftHand)
			{
				transform.localScale = new Vector3(-1f, 1f, 1f);
			}
			if (flipOnYForLeftHand && currentState == PositionState.InLeftHand)
			{
				transform.localScale = new Vector3(1f, -1f, 1f);
			}
			m = transform.localToWorldMatrix * matrix4x2;
			base.transform.SetLocalToWorldMatrixNoScale(m);
			base.transform.localScale = matrix4x2.lossyScale;
		}
	}

	public virtual void DropItem()
	{
		if (EquipmentInteractor.instance.leftHandHeldEquipment == this)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand: true);
		}
		if (EquipmentInteractor.instance.rightHandHeldEquipment == this)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand: false);
		}
		currentState = PositionState.Dropped;
		if ((bool)worldShareableInstance)
		{
			worldShareableInstance.transferableObjectState = currentState;
		}
		if (canDrop)
		{
			base.transform.parent = null;
			if ((bool)anchor)
			{
				anchor.parent = InitialDockObject;
			}
			if ((bool)rigidbodyInstance && ShouldBeKinematic() && !rigidbodyInstance.isKinematic)
			{
				rigidbodyInstance.isKinematic = true;
			}
		}
	}

	protected virtual void OnStateChanged()
	{
		if (!IsLocalObject() || networkedStateEvents == SyncOptions.None || !resetOnDocked)
		{
			return;
		}
		int num = (int)(itemState & (ItemStates)(-65));
		if (!InHand() && num != 0)
		{
			switch (networkedStateEvents)
			{
			case SyncOptions.Bool:
				ResetStateBools();
				break;
			case SyncOptions.Int:
				SetItemStateInt(0);
				break;
			}
		}
	}

	protected virtual void LateUpdateShared()
	{
		disableItem = true;
		if (isSceneObject)
		{
			disableItem = false;
		}
		else
		{
			for (int i = 0; i < ownerRig.ActiveTransferrableObjectIndexLength(); i++)
			{
				if (ownerRig.ActiveTransferrableObjectIndex(i) == myIndex)
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
		}
		if (previousState != currentState)
		{
			previousState = currentState;
			if (!Attached())
			{
				base.transform.parent = null;
				if (!ShouldBeKinematic() && rigidbodyInstance.isKinematic)
				{
					rigidbodyInstance.isKinematic = false;
				}
			}
			if (currentState == PositionState.None)
			{
				ResetToHome();
			}
			transferrableItemSlotTransformOverrideCachedMatrix = null;
			if (interpState == InterpolateState.Interpolating)
			{
				interpState = InterpolateState.None;
			}
			OnStateChanged();
		}
		if (currentState == PositionState.Dropped)
		{
			if (canDrop && !allowReparenting)
			{
				if ((object)base.transform.parent != null)
				{
					base.transform.parent = null;
				}
				try
				{
					if ((object)anchor != null && anchor.parent != InitialDockObject)
					{
						anchor.parent = InitialDockObject;
					}
				}
				catch
				{
				}
			}
		}
		else if (currentState != PositionState.None)
		{
			UpdateFollowXform();
		}
		if (InHand() && !wasHeldShared)
		{
			OnHeldShared?.Invoke();
			wasHeldShared = true;
		}
		else if (!InHand() && !Dropped() && wasHeldShared)
		{
			OnDockedShared?.Invoke();
			wasHeldShared = false;
		}
		if (!isRigidbodySet || rigidbodyInstance.isKinematic == ShouldBeKinematic())
		{
			return;
		}
		rigidbodyInstance.isKinematic = ShouldBeKinematic();
		if ((bool)worldShareableInstance)
		{
			if (currentState == PositionState.Dropped)
			{
				worldShareableInstance.EnableRemoteSync = true;
			}
			else
			{
				worldShareableInstance.EnableRemoteSync = !ShouldBeKinematic();
			}
		}
	}

	public virtual void ResetToHome()
	{
		if (isSceneObject)
		{
			currentState = PositionState.None;
		}
		ResetXf();
		if (isRigidbodySet && ShouldBeKinematic() && !rigidbodyInstance.isKinematic)
		{
			rigidbodyInstance.isKinematic = true;
		}
	}

	protected void ResetXf()
	{
		if (!positionInitialized)
		{
			initOffset = base.transform.localPosition;
			initRotation = base.transform.localRotation;
		}
		if (!canDrop && !allowWorldSharableInstance)
		{
			return;
		}
		Transform transform = DefaultAnchor();
		if (base.transform != transform && base.transform.parent != transform)
		{
			base.transform.parent = transform;
		}
		if (ClearLocalPositionOnReset)
		{
			base.transform.localPosition = Vector3.zero;
			base.transform.localRotation = Quaternion.identity;
			base.transform.localScale = Vector3.one;
		}
		if ((bool)InitialDockObject)
		{
			anchor.localPosition = Vector3.zero;
			anchor.localRotation = Quaternion.identity;
			anchor.localScale = Vector3.one;
		}
		if ((bool)grabAnchor)
		{
			if (grabAnchor.parent != base.transform)
			{
				grabAnchor.parent = base.transform;
			}
			grabAnchor.localPosition = Vector3.zero;
			grabAnchor.localRotation = Quaternion.identity;
			grabAnchor.localScale = Vector3.one;
		}
		if ((bool)transferrableItemSlotTransformOverride)
		{
			Transform transformFromPositionState = transferrableItemSlotTransformOverride.GetTransformFromPositionState(currentState);
			if ((bool)transformFromPositionState)
			{
				base.transform.position = transformFromPositionState.position;
				base.transform.rotation = transformFromPositionState.rotation;
			}
			else if (anchorOverrides != null)
			{
				Transform obj = GetAnchor(currentState);
				Transform targetDock = GetTargetDock(currentState, targetDockPositions, anchorOverrides);
				Matrix4x4 matrix4x = GetDefaultTransformationMatrix();
				if (transferrableItemSlotTransformOverride.GetTransformFromPositionState(currentState, advancedGrabState, targetDock, out var matrix4X))
				{
					matrix4x = matrix4X;
				}
				Matrix4x4 matrix = obj.localToWorldMatrix * matrix4x;
				base.transform.SetLocalToWorldMatrixNoScale(matrix);
				base.transform.localScale = matrix.lossyScale;
			}
		}
		else
		{
			base.transform.SetLocalMatrixRelativeToParent(GetDefaultTransformationMatrix());
		}
	}

	protected void ReDock()
	{
		if (IsMyItem())
		{
			currentState = initState;
		}
		if ((bool)rigidbodyInstance && ShouldBeKinematic() && !rigidbodyInstance.isKinematic)
		{
			rigidbodyInstance.isKinematic = true;
		}
		ResetXf();
	}

	private void HandleLocalInput()
	{
		GameObject[] array;
		Behaviour[] array2;
		if (Dropped())
		{
			array = gameObjectsActiveOnlyWhileHeld;
			foreach (GameObject gameObject in array)
			{
				if (gameObject.activeSelf)
				{
					gameObject.SetActive(value: false);
				}
			}
			array2 = behavioursEnabledOnlyWhileHeld;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = false;
			}
			array = gameObjectsActiveOnlyWhileDocked;
			foreach (GameObject gameObject2 in array)
			{
				if (gameObject2.activeSelf)
				{
					gameObject2.SetActive(value: false);
				}
			}
			array2 = behavioursEnabledOnlyWhileDocked;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = false;
			}
			return;
		}
		if (!InHand())
		{
			array = gameObjectsActiveOnlyWhileHeld;
			foreach (GameObject gameObject3 in array)
			{
				if (gameObject3.activeSelf)
				{
					gameObject3.SetActive(value: false);
				}
			}
			array2 = behavioursEnabledOnlyWhileHeld;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = false;
			}
			array = gameObjectsActiveOnlyWhileDocked;
			foreach (GameObject gameObject4 in array)
			{
				if (!gameObject4.activeSelf)
				{
					gameObject4.SetActive(value: true);
				}
			}
			array2 = behavioursEnabledOnlyWhileDocked;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = true;
			}
			return;
		}
		array = gameObjectsActiveOnlyWhileHeld;
		foreach (GameObject gameObject5 in array)
		{
			if (!gameObject5.activeSelf)
			{
				gameObject5.SetActive(value: true);
			}
		}
		array2 = behavioursEnabledOnlyWhileHeld;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		array = gameObjectsActiveOnlyWhileDocked;
		foreach (GameObject gameObject6 in array)
		{
			if (gameObject6.activeSelf)
			{
				gameObject6.SetActive(value: false);
			}
		}
		array2 = behavioursEnabledOnlyWhileDocked;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		XRNode node = ((currentState == PositionState.InLeftHand) ? XRNode.LeftHand : XRNode.RightHand);
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

	protected virtual void LocalMyObjectValidation()
	{
	}

	protected virtual void LocalPersistanceValidation()
	{
		if (maxDistanceFromOriginBeforeRespawn != 0f && Vector3.Distance(base.transform.position, originPoint.position) > maxDistanceFromOriginBeforeRespawn)
		{
			if (audioSrc != null && resetPositionAudioClip != null)
			{
				audioSrc.GTPlayOneShot(resetPositionAudioClip);
			}
			if (currentState != PositionState.Dropped)
			{
				DropItem();
				currentState = PositionState.Dropped;
			}
			base.transform.position = originPoint.position;
			if (!rigidbodyInstance.isKinematic)
			{
				rigidbodyInstance.linearVelocity = Vector3.zero;
			}
		}
		if ((bool)rigidbodyInstance && rigidbodyInstance.linearVelocity.sqrMagnitude > 10000f)
		{
			Debug.Log("Moving too fast, Assuming ive fallen out of the map. Ressetting position", this);
			ResetToHome();
		}
	}

	public void ObjectBeingTaken()
	{
		if (EquipmentInteractor.instance.leftHandHeldEquipment == this)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand: true);
		}
		if (EquipmentInteractor.instance.rightHandHeldEquipment == this)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand: false);
		}
	}

	protected virtual void LateUpdateLocal()
	{
		wasHover = isHover;
		isHover = false;
		LocalPersistanceValidation();
		if (NetworkSystem.Instance.InRoom)
		{
			if (!isSceneObject && IsLocalObject())
			{
				myRig.SetTransferrablePosStates(objectIndex, currentState);
				myRig.SetTransferrableItemStates(objectIndex, itemState);
				myRig.SetTransferrableDockPosition(objectIndex, storedZone);
			}
			if ((bool)worldShareableInstance)
			{
				worldShareableInstance.transferableObjectState = currentState;
				worldShareableInstance.transferableObjectItemState = itemState;
			}
		}
		HandleLocalInput();
		if (InHand() && !wasHeldLocal)
		{
			OnHeldLocal?.Invoke();
			wasHeldLocal = true;
		}
		else if (!InHand() && !Dropped() && wasHeldLocal)
		{
			OnDockedLocal?.Invoke();
			wasHeldLocal = false;
		}
	}

	protected void LateUpdateReplicatedSceneObject()
	{
		if ((object)myOnlineRig != null)
		{
			storedZone = myOnlineRig.TransferrableDockPosition(objectIndex);
		}
		if ((object)worldShareableInstance != null)
		{
			currentState = worldShareableInstance.transferableObjectState;
			itemState = worldShareableInstance.transferableObjectItemState;
			worldShareableInstance.EnableRemoteSync = !ShouldBeKinematic() || currentState == PositionState.Dropped;
		}
		if (isRigidbodySet && ShouldBeKinematic() && !rigidbodyInstance.isKinematic)
		{
			rigidbodyInstance.isKinematic = true;
		}
	}

	protected virtual void LateUpdateReplicated()
	{
		if (isSceneObject || shareable)
		{
			LateUpdateReplicatedSceneObject();
		}
		else
		{
			if ((object)myOnlineRig == null)
			{
				return;
			}
			currentState = myOnlineRig.TransferrablePosStates(objectIndex);
			if (!ValidateState(currentState))
			{
				if (previousState == PositionState.None)
				{
					base.gameObject.Disable();
				}
				currentState = previousState;
			}
			if (isRigidbodySet)
			{
				rigidbodyInstance.isKinematic = ShouldBeKinematic();
			}
			bool flag = true;
			previousItemState = itemState;
			itemState = myOnlineRig.TransferrableItemStates(objectIndex);
			storedZone = myOnlineRig.TransferrableDockPosition(objectIndex);
			int num = myOnlineRig.ActiveTransferrableObjectIndexLength();
			for (int i = 0; i < num; i++)
			{
				if (myOnlineRig.ActiveTransferrableObjectIndex(i) != myIndex)
				{
					continue;
				}
				flag = false;
				GameObject[] array = gameObjectsActiveOnlyWhileHeld;
				foreach (GameObject gameObject in array)
				{
					bool flag2 = InHand();
					if (gameObject.activeSelf != flag2)
					{
						gameObject.SetActive(flag2);
					}
				}
				Behaviour[] array2 = behavioursEnabledOnlyWhileHeld;
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].enabled = InHand();
				}
				array = gameObjectsActiveOnlyWhileDocked;
				foreach (GameObject gameObject2 in array)
				{
					bool flag3 = InHand();
					if (gameObject2.activeSelf == flag3)
					{
						gameObject2.SetActive(!flag3);
					}
				}
				array2 = behavioursEnabledOnlyWhileDocked;
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].enabled = !InHand();
				}
			}
			if (networkedStateEvents != SyncOptions.None && previousItemState != itemState)
			{
				ItemStates num2 = previousItemState & (ItemStates)(-65);
				int num3 = (int)(itemState & (ItemStates)(-65));
				if (num2 != (ItemStates)num3)
				{
					OnNetworkItemStateChanged(num3);
				}
			}
			if (flag)
			{
				base.gameObject.SetActive(value: false);
			}
		}
	}

	public virtual void ResetToDefaultState()
	{
		canAutoGrabLeft = true;
		canAutoGrabRight = true;
		wasHover = false;
		isHover = false;
		if (!IsLocalObject() && (bool)worldShareableInstance && !isSceneObject)
		{
			if (IsMyItem())
			{
				return;
			}
			worldShareableInstance.GetComponent<RequestableOwnershipGuard>().RequestOwnershipImmediately(delegate
			{
			});
		}
		ResetXf();
		switch (networkedStateEvents)
		{
		case SyncOptions.Bool:
			ResetStateBools();
			break;
		case SyncOptions.Int:
			SetItemStateInt(0);
			break;
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!(worldShareableInstance == null) && !worldShareableInstance.guard.isTrulyMine)
		{
			if (!IsGrabbable())
			{
				return;
			}
			worldShareableInstance.guard.RequestOwnershipImmediately(delegate
			{
			});
		}
		if (grabbingHand == EquipmentInteractor.instance.leftHand && currentState != PositionState.OnLeftArm)
		{
			if (currentState == PositionState.InRightHand && disableStealing)
			{
				return;
			}
			canAutoGrabLeft = false;
			if (interpState == InterpolateState.Interpolating)
			{
				startInterpolation = true;
			}
			interpState = InterpolateState.None;
			currentState = PositionState.InLeftHand;
			if ((bool)transferrableItemSlotTransformOverride)
			{
				advancedGrabState = transferrableItemSlotTransformOverride.GetAdvancedItemStateFromHand(PositionState.InLeftHand, EquipmentInteractor.instance.leftHand.transform, GetTargetDock(currentState, GorillaTagger.Instance.offlineVRRig));
			}
			EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: true);
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		}
		else if (grabbingHand == EquipmentInteractor.instance.rightHand && currentState != PositionState.OnRightArm)
		{
			if (currentState == PositionState.InLeftHand && disableStealing)
			{
				return;
			}
			canAutoGrabRight = false;
			if (interpState == InterpolateState.Interpolating)
			{
				startInterpolation = true;
			}
			interpState = InterpolateState.None;
			currentState = PositionState.InRightHand;
			if ((bool)transferrableItemSlotTransformOverride)
			{
				advancedGrabState = transferrableItemSlotTransformOverride.GetAdvancedItemStateFromHand(PositionState.InRightHand, EquipmentInteractor.instance.rightHand.transform, GetTargetDock(currentState, GorillaTagger.Instance.offlineVRRig));
			}
			EquipmentInteractor.instance.UpdateHandEquipment(this, forLeftHand: false);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		}
		if ((bool)rigidbodyInstance && !rigidbodyInstance.isKinematic && ShouldBeKinematic())
		{
			rigidbodyInstance.isKinematic = true;
		}
		PlayerGameEvents.GrabbedObject(interactEventName);
	}

	private void SetupMatrixForFreeGrab(Vector3 worldPosition, Quaternion worldRotation, Transform attachPoint, bool leftHand)
	{
		Quaternion rotation = attachPoint.transform.rotation;
		Vector3 position = attachPoint.transform.position;
		Quaternion localRotation = Quaternion.Inverse(rotation) * worldRotation;
		Vector3 localPosition = Quaternion.Inverse(rotation) * (worldPosition - position);
		OnHandMatrixUpdate(localPosition, localRotation, leftHand);
	}

	protected void SetupHandMatrix(Vector3 leftHandPos, Quaternion leftHandRot, Vector3 rightHandPos, Quaternion rightHandRot)
	{
		leftHandMatrix = Matrix4x4.TRS(leftHandPos, leftHandRot, Vector3.one);
		rightHandMatrix = Matrix4x4.TRS(rightHandPos, rightHandRot, Vector3.one);
	}

	protected virtual void OnHandMatrixUpdate(Vector3 localPosition, Quaternion localRotation, bool leftHand)
	{
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (!IsMyItem())
		{
			return false;
		}
		if (!CanDeactivate())
		{
			return false;
		}
		if (!IsHeld())
		{
			return false;
		}
		if (releasingHand == EquipmentInteractor.instance.leftHand)
		{
			canAutoGrabLeft = true;
		}
		else
		{
			canAutoGrabRight = true;
		}
		if ((object)zoneReleased != null)
		{
			bool num = currentState == PositionState.InLeftHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.LeftArm;
			bool flag = currentState == PositionState.InRightHand && zoneReleased.dropPosition == BodyDockPositions.DropPositions.RightArm;
			if (num || flag)
			{
				return false;
			}
			if (targetDockPositions.DropZoneStorageUsed(zoneReleased.dropPosition) == -1 && zoneReleased.forBodyDock == targetDockPositions && (zoneReleased.dropPosition & dockPositions) != BodyDockPositions.DropPositions.None)
			{
				storedZone = zoneReleased.dropPosition;
			}
		}
		bool flag2 = false;
		interpState = InterpolateState.None;
		if (isSceneObject || canDrop || allowWorldSharableInstance)
		{
			if (!rigidbodyInstance)
			{
				return false;
			}
			if ((bool)worldShareableInstance)
			{
				worldShareableInstance.EnableRemoteSync = true;
			}
			if (!flag2)
			{
				currentState = PositionState.Dropped;
			}
			if (rigidbodyInstance.isKinematic && !ShouldBeKinematic())
			{
				rigidbodyInstance.isKinematic = false;
			}
			GorillaVelocityEstimator component = GetComponent<GorillaVelocityEstimator>();
			if (component != null && rigidbodyInstance != null)
			{
				rigidbodyInstance.linearVelocity = component.linearVelocity;
				rigidbodyInstance.angularVelocity = component.angularVelocity;
			}
		}
		else
		{
			_ = allowWorldSharableInstance;
		}
		DropItemCleanup();
		EquipmentInteractor.instance.ForceDropEquipment(this);
		PlayerGameEvents.DroppedObject(interactEventName);
		return true;
	}

	public override void DropItemCleanup()
	{
		if (currentState != PositionState.Dropped)
		{
			switch (storedZone)
			{
			case BodyDockPositions.DropPositions.LeftArm:
				currentState = PositionState.OnLeftArm;
				break;
			case BodyDockPositions.DropPositions.RightArm:
				currentState = PositionState.OnRightArm;
				break;
			case BodyDockPositions.DropPositions.Chest:
				currentState = PositionState.OnChest;
				break;
			case BodyDockPositions.DropPositions.LeftBack:
				currentState = PositionState.OnLeftShoulder;
				break;
			case BodyDockPositions.DropPositions.RightBack:
				currentState = PositionState.OnRightShoulder;
				break;
			}
		}
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
		if (IsGrabbable())
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
		bool flag = currentState == PositionState.InLeftHand;
		if (myRig.netView != null)
		{
			myRig.netView.SendRPC("RPC_PlayHandTap", RpcTarget.Others, soundIndex, flag, 0.1f);
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
		if ((object)GorillaTagger.Instance == null)
		{
			return true;
		}
		if ((object)targetRig == null)
		{
			return false;
		}
		return targetRig == GorillaTagger.Instance.offlineVRRig;
	}

	protected virtual bool IsHeld()
	{
		if ((object)EquipmentInteractor.instance == null)
		{
			return false;
		}
		if (EquipmentInteractor.instance.leftHandHeldEquipment != this)
		{
			return EquipmentInteractor.instance.rightHandHeldEquipment == this;
		}
		return true;
	}

	public virtual bool IsGrabbable()
	{
		if (IsMyItem())
		{
			return true;
		}
		if (isSceneObject || shareable)
		{
			if (!isSceneObject && !shareable)
			{
				return false;
			}
			if (allowPlayerStealing)
			{
				return true;
			}
			if (currentState == PositionState.Dropped)
			{
				return true;
			}
			if (currentState == PositionState.None)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public bool InHand()
	{
		if (currentState != PositionState.InLeftHand)
		{
			return currentState == PositionState.InRightHand;
		}
		return true;
	}

	public bool Dropped()
	{
		return currentState == PositionState.Dropped;
	}

	public bool InLeftHand()
	{
		return currentState == PositionState.InLeftHand;
	}

	public bool InRightHand()
	{
		return currentState == PositionState.InRightHand;
	}

	public bool OnChest()
	{
		return currentState == PositionState.OnChest;
	}

	public bool OnShoulder()
	{
		if (currentState != PositionState.OnLeftShoulder)
		{
			return currentState == PositionState.OnRightShoulder;
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

	public bool ValidateState(PositionState state)
	{
		switch (state)
		{
		case PositionState.InLeftHand:
		case PositionState.InRightHand:
			return true;
		case PositionState.OnLeftArm:
			if ((dockPositions & BodyDockPositions.DropPositions.LeftArm) != BodyDockPositions.DropPositions.None)
			{
				return true;
			}
			break;
		case PositionState.OnRightArm:
			if ((dockPositions & BodyDockPositions.DropPositions.RightArm) != BodyDockPositions.DropPositions.None)
			{
				return true;
			}
			break;
		case PositionState.OnChest:
			if ((dockPositions & BodyDockPositions.DropPositions.Chest) != BodyDockPositions.DropPositions.None)
			{
				return true;
			}
			break;
		case PositionState.OnLeftShoulder:
			if ((dockPositions & BodyDockPositions.DropPositions.LeftBack) != BodyDockPositions.DropPositions.None)
			{
				return true;
			}
			break;
		case PositionState.OnRightShoulder:
			if ((dockPositions & BodyDockPositions.DropPositions.RightBack) != BodyDockPositions.DropPositions.None)
			{
				return true;
			}
			break;
		case PositionState.Dropped:
			if (canDrop || shareable)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	private void OnNetworkItemStateChanged(int stateBits)
	{
		switch (networkedStateEvents)
		{
		case SyncOptions.Bool:
		{
			int num = (int)(previousItemState & ItemStates.State0);
			int num2 = (int)(itemState & ItemStates.State0);
			if (num != num2 && num2 == 0)
			{
				OnItemStateBoolFalse?.Invoke();
			}
			else if (num != num2)
			{
				OnItemStateBoolTrue?.Invoke();
			}
			num = (int)(previousItemState & ItemStates.State1);
			num2 = (int)(itemState & ItemStates.State1);
			if (num != num2 && num2 == 0)
			{
				OnItemStateBoolBFalse?.Invoke();
			}
			else if (num != num2)
			{
				OnItemStateBoolBTrue?.Invoke();
			}
			num = (int)(previousItemState & ItemStates.State2);
			num2 = (int)(itemState & ItemStates.State2);
			if (num != num2 && num2 == 0)
			{
				OnItemStateBoolCFalse?.Invoke();
			}
			else if (num != num2)
			{
				OnItemStateBoolCTrue?.Invoke();
			}
			num = (int)(previousItemState & ItemStates.State3);
			num2 = (int)(itemState & ItemStates.State3);
			if (num != num2 && num2 == 0)
			{
				OnItemStateBoolDFalse?.Invoke();
			}
			else if (num != num2)
			{
				OnItemStateBoolDTrue?.Invoke();
			}
			break;
		}
		case SyncOptions.Int:
			OnItemStateIntChanged?.Invoke(stateBits);
			break;
		}
	}

	public void ToggleNetworkedItemStateBool()
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			ToggleStateBit(1);
		}
	}

	public void ToggleNetworkedItemStateBoolB()
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			ToggleStateBit(2);
		}
	}

	public void ToggleNetworkedItemStateBoolC()
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			ToggleStateBit(4);
		}
	}

	public void ToggleNetworkedItemStateBoolD()
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			ToggleStateBit(8);
		}
	}

	protected void ResetStateBools()
	{
		if (networkedStateEvents == SyncOptions.Bool && IsLocalObject())
		{
			int bitmask = 15;
			SetStateBit(value: false, bitmask);
		}
	}

	public void SetItemStateBool(bool newState)
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			SetStateBit(newState, 1);
		}
	}

	public void SetItemStateBoolB(bool newState)
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			SetStateBit(newState, 2);
		}
	}

	public void SetItemStateBoolC(bool newState)
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			SetStateBit(newState, 4);
		}
	}

	public void SetItemStateBoolD(bool newState)
	{
		if (networkedStateEvents == SyncOptions.Bool)
		{
			SetStateBit(newState, 8);
		}
	}

	private void SetStateBit(bool value, int bitmask)
	{
		if (IsLocalObject())
		{
			int num = (int)itemState;
			num = ((!value) ? (num & ~bitmask) : (num | bitmask));
			ItemStates itemStates = (ItemStates)num;
			if (itemState != itemStates)
			{
				previousItemState = itemState;
				itemState = itemStates;
				OnNetworkItemStateChanged(num);
			}
		}
	}

	private void ToggleStateBit(int bitmask)
	{
		if (IsLocalObject())
		{
			int num = (int)itemState & bitmask;
			int num2 = (int)itemState;
			num2 = ((num != 0) ? (num2 & ~bitmask) : (num2 | bitmask));
			previousItemState = itemState;
			itemState = (ItemStates)num2;
			OnNetworkItemStateChanged(num2);
		}
	}

	public void SetItemStateInt(int newState)
	{
		if (IsLocalObject() && networkedStateEvents == SyncOptions.Int)
		{
			newState = Mathf.Clamp(newState, 0, 63);
			int num = newState & -65;
			int num2 = (int)(itemState & ItemStates.Part0Held);
			ItemStates itemStates = (ItemStates)(num | num2);
			if (itemState != itemStates)
			{
				previousItemState = itemState;
				itemState = itemStates;
				OnNetworkItemStateChanged(num);
			}
		}
	}

	public virtual void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		if (toPlayer != null && toPlayer.Equals(fromPlayer))
		{
			return;
		}
		if (object.Equals(fromPlayer, NetworkSystem.Instance.LocalPlayer) && IsHeld())
		{
			DropItem();
		}
		if (toPlayer == null)
		{
			SetTargetRig(null);
			return;
		}
		rigidbodyInstance.useGravity = shouldUseGravity && object.Equals(toPlayer, NetworkSystem.Instance.LocalPlayer);
		if (!shareable && !isSceneObject)
		{
			return;
		}
		if (object.Equals(toPlayer, NetworkSystem.Instance.LocalPlayer))
		{
			if (GorillaTagger.Instance == null)
			{
				Debug.LogError("OnOwnershipTransferred has been initiated too quickly, The local player is not ready");
			}
			else
			{
				SetTargetRig(GorillaTagger.Instance.offlineVRRig);
			}
			return;
		}
		VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(toPlayer);
		if (!vRRig)
		{
			Debug.LogError("failed to find target rig for ownershiptransfer");
		}
		else
		{
			SetTargetRig(vRRig);
		}
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		if (!VRRigCache.Instance.TryGetVrrig(fromPlayer, out var playerRig))
		{
			return false;
		}
		if (Vector3.SqrMagnitude(base.transform.position - playerRig.transform.position) > 16f)
		{
			Debug.Log("Player whos trying to get is too far, Denying takeover");
			return false;
		}
		if (allowPlayerStealing || currentState == PositionState.Dropped || currentState == PositionState.None)
		{
			return true;
		}
		if (isSceneObject)
		{
			return false;
		}
		if (canDrop)
		{
			if (ownerRig == null || ownerRig.creator == null)
			{
				return true;
			}
			if (ownerRig.creator.Equals(fromPlayer))
			{
				return true;
			}
		}
		return false;
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		if (!VRRigCache.Instance.TryGetVrrig(fromPlayer, out var playerRig))
		{
			return true;
		}
		if (Vector3.SqrMagnitude(base.transform.position - playerRig.transform.position) > 16f)
		{
			Debug.Log("Player whos trying to get is too far, Denying takeover");
			return false;
		}
		if (currentState == PositionState.Dropped || currentState == PositionState.None)
		{
			return true;
		}
		if (canDrop)
		{
			if (ownerRig == null || ownerRig.creator == null)
			{
				return true;
			}
			if (ownerRig.creator.Equals(fromPlayer))
			{
				return true;
			}
		}
		return false;
	}

	public void OnMyOwnerLeft()
	{
		if (currentState != PositionState.None && currentState != PositionState.Dropped)
		{
			DropItem();
			if ((bool)anchor)
			{
				anchor.parent = InitialDockObject;
				anchor.localPosition = Vector3.zero;
				anchor.localRotation = Quaternion.identity;
			}
		}
	}

	public void OnMyCreatorLeft()
	{
		OnItemDestroyedOrDisabled();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public bool BuildValidationCheck()
	{
		int num = 0;
		if (storedZone.HasFlag(BodyDockPositions.DropPositions.LeftArm))
		{
			num++;
		}
		if (storedZone.HasFlag(BodyDockPositions.DropPositions.RightArm))
		{
			num++;
		}
		if (storedZone.HasFlag(BodyDockPositions.DropPositions.Chest))
		{
			num++;
		}
		if (storedZone.HasFlag(BodyDockPositions.DropPositions.LeftBack))
		{
			num++;
		}
		if (storedZone.HasFlag(BodyDockPositions.DropPositions.RightBack))
		{
			num++;
		}
		if (num > 1)
		{
			Debug.LogError("transferrableitem is starting with multiple storedzones: " + base.transform.parent.name, base.gameObject);
			return false;
		}
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (((int)GTPlayer.LocomotionEnabledLayers & (1 << componentsInChildren[i].gameObject.layer)) != 0)
			{
				Debug.LogError("Holdable cosmetic " + base.transform.name + " has a collider on a player movement layer! Players will fly around! Dear god, please fix! It's on the " + componentsInChildren[i].name + " collider", base.gameObject);
				return false;
			}
		}
		return true;
	}
}
