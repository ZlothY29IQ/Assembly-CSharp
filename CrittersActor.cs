using System;
using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class CrittersActor : MonoBehaviour
{
	public enum CrittersActorType
	{
		Creature,
		Food,
		LoudNoise,
		BrightLight,
		Darkness,
		HidingArea,
		Disappear,
		Spawn,
		Player,
		Grabber,
		Cage,
		FoodSpawner,
		AttachPoint,
		StunBomb,
		Bag,
		BodyAttachPoint,
		NoiseMaker,
		StickyTrap,
		StickyGoo
	}

	public CrittersActorType crittersActorType;

	public bool isSceneActor;

	public bool isOnPlayer;

	[NonSerialized]
	protected bool _isOnPlayerDefault;

	public int rigPlayerId;

	public int rigIndex;

	public bool grabbable;

	protected bool isGrabDisabled;

	public int lastGrabbedPlayer;

	public UnityEvent<CrittersActor> ReleasedEvent;

	public Rigidbody rb;

	[NonSerialized]
	public int actorId;

	[NonSerialized]
	protected Transform defaultParentTransform;

	[NonSerialized]
	public int parentActorId = -1;

	[NonSerialized]
	protected int lastParentActorId;

	[NonSerialized]
	public Vector3 lastImpulsePosition;

	[NonSerialized]
	public Vector3 lastImpulseVelocity;

	[NonSerialized]
	public Vector3 lastImpulseAngularVelocity;

	[NonSerialized]
	public Quaternion lastImpulseQuaternion;

	[NonSerialized]
	public double lastImpulseTime;

	[NonSerialized]
	public bool updatedSinceLastFrame;

	public bool isEnabled = true;

	public bool wasEnabled = true;

	[NonSerialized]
	protected double localLastImpulse;

	[NonSerialized]
	protected Transform parentActor;

	public GameObject[] subObjects;

	public int subObjectIndex = -1;

	public bool usesRB;

	public bool resetPhysicsOnSpawn;

	public bool despawnWhenIdle;

	public bool preventDespawnUntilGrabbed;

	public int despawnDelay;

	public double despawnTime;

	public bool isDespawnBlocked;

	public bool equipmentStorable;

	public bool localCanStore;

	public CrittersActor lastStoredObject;

	public CapsuleCollider storeCollider;

	[NonSerialized]
	public Collider[] colliders;

	[NonSerialized]
	public ConfigurableJoint joint;

	[NonSerialized]
	public float timeLastTouched;

	private JointDrive drive;

	private JointDrive angularDrive;

	private SoftJointLimit linearLimitDrive;

	private SoftJointLimitSpring linearLimitSpringDrive;

	public CapsuleCollider equipmentStoreTriggerCollider;

	public bool disconnectJointFlag;

	public bool forceUpdate;

	public float FearAmount = 1f;

	public AnimationCurve FearCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	public float AttractionAmount = 1f;

	public AnimationCurve AttractionCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[FormerlySerializedAs("maxDetectionDistance")]
	public float maxRangeOfFearAttraction = 3f;

	protected float[] averageSpeed = new float[6];

	protected int averageSpeedIndex;

	private Vector3 lastPosition = Vector3.zero;

	public float GetAverageSpeed => (averageSpeed[0] + averageSpeed[1] + averageSpeed[2] + averageSpeed[3] + averageSpeed[4] + averageSpeed[5]) / 6f;

	public event Action<CrittersActor> OnGrabbedChild;

	public virtual void UpdateAverageSpeed()
	{
		averageSpeed[averageSpeedIndex] = (base.transform.position - lastPosition).magnitude;
		averageSpeedIndex++;
		averageSpeedIndex %= 6;
		lastPosition = base.transform.position;
	}

	protected virtual void Awake()
	{
		_isOnPlayerDefault = isOnPlayer;
	}

	public virtual void Initialize()
	{
		if (defaultParentTransform == null)
		{
			SetDefaultParent(base.transform.parent);
		}
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (rb == null)
		{
			Debug.LogError("I should have a rigidbody, but I don't!", base.gameObject);
		}
		wasEnabled = false;
		isEnabled = true;
		TogglePhysics(usesRB);
		if (!rb.isKinematic)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		if (resetPhysicsOnSpawn)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			lastImpulseVelocity = Vector3.zero;
		}
		if (subObjectIndex >= 0 && subObjectIndex < subObjects.Length)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				subObjects[i].SetActive(i == subObjectIndex);
			}
		}
		colliders = new Collider[50];
		if (preventDespawnUntilGrabbed)
		{
			isDespawnBlocked = true;
			despawnTime = 0.0;
		}
		else
		{
			isDespawnBlocked = false;
			despawnTime = (double)despawnDelay + (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		}
		rb.includeLayers = 0;
		rb.excludeLayers = CrittersManager.instance.containerLayer;
	}

	public virtual void OnEnable()
	{
		CrittersManager.RegisterActor(this);
		Initialize();
	}

	public virtual void OnDisable()
	{
		CleanupActor();
	}

	public virtual string GetActorSubtype()
	{
		if (subObjectIndex >= 0 && subObjectIndex < subObjects.Length)
		{
			return subObjects[subObjectIndex].name;
		}
		return base.name;
	}

	protected virtual void CleanupActor()
	{
		CrittersManager.DeregisterActor(this);
		if (base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
		}
		for (int i = 0; i < subObjects.Length; i++)
		{
			if (subObjects[i].activeSelf)
			{
				subObjects[i].transform.localRotation = Quaternion.identity;
				subObjects[i].transform.localPosition = Vector3.zero;
				subObjects[i].SetActive(value: false);
			}
		}
		ReleasedEvent.Invoke(this);
		ReleasedEvent.RemoveAllListeners();
		isEnabled = false;
		wasEnabled = true;
		isOnPlayer = _isOnPlayerDefault;
		rigPlayerId = -1;
		rigIndex = -1;
		despawnTime = 0.0;
		isDespawnBlocked = false;
		rb.isKinematic = false;
		if (parentActorId >= 0)
		{
			AttemptRemoveStoredObjectCollider(parentActorId, playSound: false);
		}
		parentActorId = -1;
		parentActor = null;
		lastParentActorId = -1;
		isGrabDisabled = false;
		lastGrabbedPlayer = -1;
		lastImpulsePosition = Vector3.zero;
		lastImpulseVelocity = Vector3.zero;
		lastImpulseQuaternion = Quaternion.identity;
		lastImpulseTime = -1.0;
		localLastImpulse = -1.0;
		updatedSinceLastFrame = false;
		localCanStore = false;
	}

	public virtual bool ProcessLocal()
	{
		updatedSinceLastFrame |= isEnabled != wasEnabled || parentActorId != lastParentActorId;
		lastParentActorId = parentActorId;
		wasEnabled = isEnabled;
		return updatedSinceLastFrame;
	}

	public virtual void ProcessRemote()
	{
		bool flag = forceUpdate;
		forceUpdate = false;
		if (base.gameObject.activeSelf != isEnabled)
		{
			base.gameObject.SetActive(isEnabled);
		}
		if (!isEnabled)
		{
			return;
		}
		bool flag2 = lastParentActorId == parentActorId || isOnPlayer || isSceneActor;
		bool flag3 = lastImpulseTime == localLastImpulse;
		if (flag2 && flag3 && !flag)
		{
			return;
		}
		if (!flag2)
		{
			if (lastParentActorId >= 0)
			{
				AttemptRemoveStoredObjectCollider(lastParentActorId);
			}
			lastParentActorId = parentActorId;
			if (parentActorId >= 0)
			{
				if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out var value))
				{
					parentActor = value.transform;
					base.transform.SetParent(parentActor, worldPositionStays: true);
					SetImpulse();
					if (value is CrittersBag)
					{
						((CrittersBag)value).AddStoredObjectCollider(this);
					}
					if (value.isOnPlayer)
					{
						lastGrabbedPlayer = value.rigPlayerId;
					}
					value.RemoteGrabbed(this);
				}
			}
			else if (parentActorId == -1)
			{
				parentActor = null;
				SetTransformToDefaultParent();
				HandleRemoteReleased();
				SetImpulse();
			}
		}
		else
		{
			SetImpulse();
		}
	}

	public virtual void SetImpulse()
	{
		if (!isOnPlayer && !isSceneActor)
		{
			localLastImpulse = lastImpulseTime;
			MoveActor(lastImpulsePosition, lastImpulseQuaternion, parentActorId >= 0, updateImpulses: false);
			TogglePhysics(usesRB && parentActorId == -1);
			if (!rb.isKinematic)
			{
				rb.linearVelocity = lastImpulseVelocity;
				rb.angularVelocity = lastImpulseAngularVelocity;
			}
		}
	}

	public virtual void TogglePhysics(bool enable)
	{
		if (enable)
		{
			rb.isKinematic = false;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		}
		else
		{
			rb.isKinematic = true;
			rb.interpolation = RigidbodyInterpolation.None;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		}
	}

	public void AddPlayerCrittersActorDataToList(ref List<object> objList)
	{
		objList.Add(actorId);
		objList.Add(isOnPlayer);
		objList.Add(rigPlayerId);
		objList.Add(rigIndex);
	}

	public virtual int AddActorDataToList(ref List<object> objList)
	{
		objList.Add(actorId);
		objList.Add(lastImpulseTime);
		objList.Add(lastImpulsePosition);
		objList.Add(lastImpulseVelocity);
		objList.Add(lastImpulseAngularVelocity);
		objList.Add(lastImpulseQuaternion);
		objList.Add(parentActorId);
		objList.Add(isEnabled);
		objList.Add(subObjectIndex);
		return BaseActorDataLength();
	}

	public int BaseActorDataLength()
	{
		return 9;
	}

	public virtual int TotalActorDataLength()
	{
		return 9;
	}

	public virtual int UpdateFromRPC(object[] data, int startingIndex)
	{
		if (!CrittersManager.ValidateDataType<double>(data[startingIndex + 1], out var dataAsType))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<Vector3>(data[startingIndex + 2], out var dataAsType2))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<Vector3>(data[startingIndex + 3], out var dataAsType3))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<Vector3>(data[startingIndex + 4], out var dataAsType4))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<Quaternion>(data[startingIndex + 5], out var dataAsType5))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 6], out var dataAsType6))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<bool>(data[startingIndex + 7], out var dataAsType7))
		{
			return BaseActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 8], out var dataAsType8))
		{
			return BaseActorDataLength();
		}
		lastImpulseTime = dataAsType.GetFinite();
		lastImpulsePosition.SetValueSafe(in dataAsType2);
		lastImpulseVelocity.SetValueSafe(in dataAsType3);
		lastImpulseAngularVelocity.SetValueSafe(in dataAsType4);
		lastImpulseQuaternion.SetValueSafe(in dataAsType5);
		parentActorId = dataAsType6;
		isEnabled = dataAsType7;
		subObjectIndex = dataAsType8;
		forceUpdate = true;
		if (isEnabled)
		{
			base.gameObject.SetActive(value: true);
		}
		for (int i = 0; i < subObjects.Length; i++)
		{
			subObjects[i].SetActive(i == subObjectIndex);
		}
		return BaseActorDataLength();
	}

	public int UpdatePlayerCrittersActorFromRPC(object[] data, int startingIndex)
	{
		if (!CrittersManager.ValidateDataType<bool>(data[startingIndex + 1], out var dataAsType))
		{
			return 4;
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 2], out var dataAsType2))
		{
			return 4;
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 3], out var dataAsType3))
		{
			return 4;
		}
		isOnPlayer = dataAsType;
		rigPlayerId = dataAsType2;
		rigIndex = dataAsType3;
		if (rigPlayerId == -1 && CrittersManager.instance.guard.currentOwner != null)
		{
			rigPlayerId = CrittersManager.instance.guard.currentOwner.ActorNumber;
		}
		PlacePlayerCrittersActor();
		return 4;
	}

	public virtual bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!(CrittersManager.ValidateDataType<double>(stream.ReceiveNext(), out var dataAsType) & CrittersManager.ValidateDataType<Vector3>(stream.ReceiveNext(), out var dataAsType2) & CrittersManager.ValidateDataType<Vector3>(stream.ReceiveNext(), out var dataAsType3) & CrittersManager.ValidateDataType<Vector3>(stream.ReceiveNext(), out var dataAsType4) & CrittersManager.ValidateDataType<Quaternion>(stream.ReceiveNext(), out var dataAsType5) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType6) & CrittersManager.ValidateDataType<bool>(stream.ReceiveNext(), out var dataAsType7) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType8)))
		{
			return false;
		}
		if (dataAsType2.IsValid(10000f))
		{
			lastImpulsePosition.SetValueSafe(in dataAsType2);
		}
		if (dataAsType3.IsValid(10000f))
		{
			lastImpulseVelocity.SetValueSafe(in dataAsType3);
		}
		if (dataAsType5.IsValid())
		{
			lastImpulseQuaternion.SetValueSafe(in dataAsType5);
		}
		if (dataAsType4.IsValid(10000f))
		{
			lastImpulseAngularVelocity.SetValueSafe(in dataAsType4);
		}
		if (dataAsType6 >= -1 && dataAsType6 < CrittersManager.instance.universalActorId)
		{
			parentActorId = dataAsType6;
		}
		if (dataAsType8 < subObjects.Length)
		{
			subObjectIndex = dataAsType8;
		}
		isEnabled = dataAsType7;
		lastImpulseTime = dataAsType;
		if (isEnabled != base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(isEnabled);
		}
		if (isEnabled && subObjectIndex >= 0)
		{
			subObjects[subObjectIndex].SetActive(value: true);
		}
		else if (!isEnabled && subObjectIndex >= 0)
		{
			subObjects[subObjectIndex].SetActive(value: false);
		}
		return true;
	}

	public virtual void SendDataByCrittersActorType(PhotonStream stream)
	{
		stream.SendNext(actorId);
		stream.SendNext(lastImpulseTime);
		stream.SendNext(lastImpulsePosition);
		stream.SendNext(lastImpulseVelocity);
		stream.SendNext(lastImpulseAngularVelocity);
		stream.SendNext(lastImpulseQuaternion);
		stream.SendNext(parentActorId);
		stream.SendNext(isEnabled);
		stream.SendNext(subObjectIndex);
		updatedSinceLastFrame = false;
	}

	public virtual void OnHover(bool isLeft)
	{
		GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength / 8f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
	}

	public virtual bool CanBeGrabbed(CrittersActor grabbedBy)
	{
		if (!isGrabDisabled)
		{
			return grabbable;
		}
		return false;
	}

	public static CrittersActor GetRootActor(int actorId)
	{
		if (CrittersManager.instance.actorById.TryGetValue(actorId, out var value))
		{
			if (value.parentActorId > -1)
			{
				return GetRootActor(value.parentActorId);
			}
			return value;
		}
		return null;
	}

	public static CrittersActor GetParentActor(int actorId)
	{
		if (CrittersManager.instance.actorById.TryGetValue(actorId, out var value))
		{
			return value;
		}
		return null;
	}

	public bool AllowGrabbingActor(CrittersActor grabbedBy)
	{
		if (parentActorId == -1)
		{
			return true;
		}
		if (grabbedBy.crittersActorType != CrittersActorType.Grabber)
		{
			return true;
		}
		CrittersActor rootActor = GetRootActor(grabbedBy.actorId);
		if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out var value))
		{
			if (value.crittersActorType == CrittersActorType.Bag)
			{
				if (!CrittersManager.instance.allowGrabbingFromBags)
				{
					CrittersActor rootActor2 = GetRootActor(actorId);
					Debug.Log($"Grieffing - FromBag {rootActor2.rigPlayerId} == {rootActor.rigPlayerId} || {value.parentActorId} == -1 || {rootActor.rigPlayerId} == -1  - " + $" {rootActor2.rigPlayerId == rootActor.rigPlayerId || rootActor2.rigPlayerId == -1 || rootActor.rigPlayerId == -1}");
					if (rootActor2.rigPlayerId != rootActor.rigPlayerId && rootActor2.rigPlayerId != -1)
					{
						return rootActor.rigPlayerId == -1;
					}
					return true;
				}
			}
			else if (value.crittersActorType == CrittersActorType.BodyAttachPoint)
			{
				if (!CrittersManager.instance.allowGrabbingEntireBag)
				{
					Debug.Log($"Grieffing - EntireBag {value.rigPlayerId} == {rootActor.rigPlayerId} || {value.parentActorId} == -1 || {rootActor.rigPlayerId} == -1  -  {value.rigPlayerId == rootActor.rigPlayerId || value.rigPlayerId == -1 || rootActor.rigPlayerId == -1}");
					if (value.rigPlayerId != rootActor.rigPlayerId && value.rigPlayerId != -1)
					{
						return rootActor.rigPlayerId == -1;
					}
					return true;
				}
			}
			else if (value.crittersActorType == CrittersActorType.Grabber && !CrittersManager.instance.allowGrabbingOutOfHands)
			{
				Debug.Log($"Grieffing - InHand {value.rigPlayerId} == {rootActor.rigPlayerId} || {value.parentActorId} == -1 || {rootActor.rigPlayerId} == -1  -  {value.rigPlayerId == rootActor.rigPlayerId || value.rigPlayerId == -1 || rootActor.rigPlayerId == -1}");
				if (value.rigPlayerId != rootActor.rigPlayerId && value.rigPlayerId != -1)
				{
					return rootActor.rigPlayerId == -1;
				}
				return true;
			}
		}
		return true;
	}

	public bool IsCurrentlyAttachedToBag()
	{
		if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out var value))
		{
			return value.crittersActorType == CrittersActorType.Bag;
		}
		return false;
	}

	public void SetTransformToDefaultParent(bool resetOrigin = false)
	{
		if (!this.IsNull())
		{
			base.transform.SetParent(defaultParentTransform, worldPositionStays: true);
			if (resetOrigin)
			{
				base.transform.localPosition = Vector3.zero;
				base.transform.localRotation = Quaternion.identity;
			}
		}
	}

	public void SetDefaultParent(Transform newDefaultParent)
	{
		defaultParentTransform = newDefaultParent;
	}

	protected virtual void RemoteGrabbed(CrittersActor actor)
	{
		this.OnGrabbedChild?.Invoke(actor);
		actor.RemoteGrabbedBy(this);
	}

	protected virtual void RemoteGrabbedBy(CrittersActor grabbingActor)
	{
		GlobalGrabbedBy(grabbingActor);
	}

	public virtual void GrabbedBy(CrittersActor grabbingActor, bool positionOverride = false, Quaternion localRotation = default(Quaternion), Vector3 localOffset = default(Vector3), bool disableGrabbing = false)
	{
		GlobalGrabbedBy(grabbingActor);
		if (parentActorId >= 0)
		{
			AttemptRemoveStoredObjectCollider(parentActorId);
		}
		isGrabDisabled = disableGrabbing;
		parentActorId = grabbingActor.actorId;
		if (grabbingActor.isOnPlayer)
		{
			lastGrabbedPlayer = grabbingActor.rigPlayerId;
		}
		base.transform.SetParent(grabbingActor.transform, worldPositionStays: true);
		if (localRotation.w == 0f && localRotation.x == 0f && localRotation.y == 0f && localRotation.z == 0f)
		{
			localRotation = Quaternion.identity;
		}
		if (positionOverride)
		{
			MoveActor(localOffset, localRotation, local: true, updateImpulses: false);
		}
		UpdateImpulses(local: true, updateTime: true);
		rb.isKinematic = true;
		rb.interpolation = RigidbodyInterpolation.None;
		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		if (CrittersManager.instance.IsNotNull() && PhotonNetwork.InRoom && !CrittersManager.instance.LocalAuthority())
		{
			CrittersManager.instance.SendRPC("RemoteCrittersActorGrabbedby", CrittersManager.instance.guard.currentOwner, actorId, grabbingActor.actorId, lastImpulseQuaternion, lastImpulsePosition, isGrabDisabled);
		}
		grabbingActor.OnGrabbedChild?.Invoke(this);
		AttemptAddStoredObjectCollider(grabbingActor);
	}

	protected virtual void GlobalGrabbedBy(CrittersActor grabbingActor)
	{
	}

	protected virtual void HandleRemoteReleased()
	{
		DisconnectJoint();
	}

	public virtual void Released(bool keepWorldPosition, Quaternion rotation = default(Quaternion), Vector3 position = default(Vector3), Vector3 impulseVelocity = default(Vector3), Vector3 impulseAngularVelocity = default(Vector3))
	{
		if (parentActorId >= 0)
		{
			AttemptRemoveStoredObjectCollider(parentActorId);
		}
		isGrabDisabled = false;
		parentActorId = -1;
		if (equipmentStorable)
		{
			localCanStore = false;
		}
		DisconnectJoint();
		SetTransformToDefaultParent();
		if (rotation.w == 0f && rotation.x == 0f && rotation.y == 0f && rotation.z == 0f)
		{
			rotation = Quaternion.identity;
		}
		if (!keepWorldPosition)
		{
			if (position.sqrMagnitude > 1f)
			{
				MoveActor(position, rotation, local: false, updateImpulses: false);
			}
			else
			{
				GTDev.Log($"Release called for: {base.name}, but sent in suspicious position data: {position}");
			}
		}
		if (despawnWhenIdle)
		{
			if (preventDespawnUntilGrabbed)
			{
				isDespawnBlocked = false;
			}
			despawnTime = (double)despawnDelay + (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		}
		UpdateImpulses();
		SetImpulseVelocity(impulseVelocity, impulseAngularVelocity);
		TogglePhysics(usesRB);
		SetImpulse();
		if (CrittersManager.instance.IsNotNull() && PhotonNetwork.InRoom && !CrittersManager.instance.LocalAuthority())
		{
			CrittersManager.instance.SendRPC("RemoteCritterActorReleased", CrittersManager.instance.guard.currentOwner, actorId, false, rotation, position, impulseVelocity, impulseAngularVelocity);
		}
		ReleasedEvent.Invoke(this);
		ReleasedEvent.RemoveAllListeners();
	}

	public void PlacePlayerCrittersActor()
	{
		RigContainer playerRig;
		CrittersRigActorSetup value;
		if (rigIndex == -1)
		{
			if (base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(value: false);
			}
		}
		else if (!VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(rigPlayerId), out playerRig) || !CrittersManager.instance.rigSetupByRig.TryGetValue(playerRig.Rig, out value))
		{
			_ = playerRig != null;
		}
		else if ((rigPlayerId != NetworkSystem.Instance.LocalPlayer.ActorNumber || CrittersManager.instance.rigSetupByRig.TryGetValue(GorillaTagger.Instance.offlineVRRig, out value)) && rigIndex >= 0 && rigIndex < value.rigActors.Length)
		{
			base.gameObject.SetActive(value: true);
			base.transform.parent = value.rigActors[rigIndex].location;
			MoveActor(Vector3.zero, Quaternion.identity, local: true);
			value.rigActors[rigIndex] = new CrittersRigActorSetup.RigActor
			{
				actorSet = this,
				location = value.rigActors[rigIndex].location,
				type = value.rigActors[rigIndex].type,
				subIndex = value.rigActors[rigIndex].subIndex
			};
		}
	}

	public void MoveActor(Vector3 position, Quaternion rotation, bool local = false, bool updateImpulses = true, bool updateImpulseTime = true)
	{
		bool isKinematic = rb.isKinematic;
		TogglePhysics(enable: false);
		if (local)
		{
			base.transform.localRotation = rotation;
			base.transform.localPosition = position;
			if (updateImpulses)
			{
				UpdateImpulses(local: true, updateImpulseTime);
			}
		}
		else
		{
			base.transform.rotation = rotation.normalized;
			base.transform.position = position;
			if (updateImpulses)
			{
				UpdateImpulses(local: false, updateImpulseTime);
			}
		}
		if (!isKinematic)
		{
			TogglePhysics(enable: true);
		}
	}

	public void UpdateImpulses(bool local = false, bool updateTime = false)
	{
		if (local)
		{
			lastImpulsePosition = base.transform.localPosition;
			lastImpulseQuaternion = base.transform.localRotation;
		}
		else
		{
			lastImpulsePosition = base.transform.position;
			lastImpulseQuaternion = base.transform.rotation;
		}
		if (updateTime)
		{
			SetImpulseTime();
		}
	}

	public void UpdateImpulseVelocity()
	{
		if ((bool)rb)
		{
			lastImpulseVelocity = rb.linearVelocity;
			lastImpulseAngularVelocity = rb.angularVelocity;
		}
	}

	public virtual void CalculateFear(CrittersPawn critter, float multiplier)
	{
		critter.IncreaseFear(FearCurve.Evaluate(Vector3.Distance(critter.transform.position, base.transform.position) / maxRangeOfFearAttraction) * multiplier * FearAmount * Time.deltaTime, this);
	}

	public virtual void CalculateAttraction(CrittersPawn critter, float multiplier)
	{
		critter.IncreaseAttraction(AttractionCurve.Evaluate(Vector3.Distance(critter.transform.position, base.transform.position) / maxRangeOfFearAttraction) * multiplier * AttractionAmount * Time.deltaTime, this);
	}

	public void SetImpulseVelocity(Vector3 velocity, Vector3 angularVelocity)
	{
		lastImpulseVelocity = velocity;
		lastImpulseAngularVelocity = angularVelocity;
	}

	public void SetImpulseTime()
	{
		lastImpulseTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
	}

	public virtual bool ShouldDespawn()
	{
		if (despawnWhenIdle && parentActorId == -1 && !isDespawnBlocked && 0.0 < despawnTime && despawnTime <= (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)))
		{
			return true;
		}
		return false;
	}

	public void RemoveDespawnBlock()
	{
		if (despawnWhenIdle)
		{
			isDespawnBlocked = false;
			despawnTime = (double)despawnDelay + (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		}
	}

	public virtual bool CheckStorable()
	{
		if (!localCanStore)
		{
			return false;
		}
		Vector3 vector = storeCollider.transform.up * MathF.Max(0f, storeCollider.height / 2f - storeCollider.radius);
		int num = Physics.OverlapCapsuleNonAlloc(storeCollider.transform.position + vector, storeCollider.transform.position - vector, storeCollider.radius, colliders, CrittersManager.instance.containerLayer, QueryTriggerInteraction.Collide);
		bool flag = false;
		CrittersBag crittersBag = null;
		bool flag2 = true;
		CrittersActor value = null;
		if (lastGrabbedPlayer == PhotonNetwork.LocalPlayer.ActorNumber && CrittersManager.instance.actorById.TryGetValue(parentActorId, out value) && value.GetAverageSpeed > CrittersManager.instance.MaxAttachSpeed)
		{
			return false;
		}
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				CrittersActor component = colliders[i].attachedRigidbody.GetComponent<CrittersActor>();
				if (component == null || component == this)
				{
					continue;
				}
				CrittersBag crittersBag2 = component as CrittersBag;
				if (crittersBag2 == null)
				{
					continue;
				}
				if (crittersBag2 == lastStoredObject)
				{
					flag = true;
					flag2 = false;
					break;
				}
				if (!crittersBag2.IsActorValidStore(this))
				{
					continue;
				}
				if (crittersBag2.attachableCollider != colliders[i] && !colliders[i].isTrigger)
				{
					Physics.ComputePenetration(colliders[i], colliders[i].transform.position, colliders[i].transform.rotation, storeCollider, storeCollider.transform.position, storeCollider.transform.rotation, out var _, out var distance);
					if (distance >= CrittersManager.instance.overlapDistanceMax)
					{
						flag2 = false;
						break;
					}
				}
				else
				{
					crittersBag = crittersBag2;
				}
			}
		}
		if (crittersBag.IsNotNull() && flag2)
		{
			if (value.IsNotNull())
			{
				CrittersGrabber crittersGrabber = value as CrittersGrabber;
				if (crittersGrabber.IsNotNull())
				{
					GorillaTagger.Instance.StartVibration(crittersGrabber.isLeft, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
				}
			}
			GrabbedBy(crittersBag);
			localCanStore = false;
			lastStoredObject = crittersBag;
			DisconnectJoint();
			return true;
		}
		if (!flag)
		{
			lastStoredObject = null;
		}
		return false;
	}

	public void SetJointRigid(Rigidbody rbToConnect)
	{
		if (!(joint != null))
		{
			Debug.Log("Critters SetJointRigid " + base.gameObject);
			CreateJoint(rbToConnect, setParentNull: false);
			joint.xMotion = ConfigurableJointMotion.Locked;
			joint.yMotion = ConfigurableJointMotion.Locked;
			joint.zMotion = ConfigurableJointMotion.Locked;
			joint.angularXMotion = ConfigurableJointMotion.Locked;
			joint.angularYMotion = ConfigurableJointMotion.Locked;
			joint.angularZMotion = ConfigurableJointMotion.Locked;
			rb.mass = CrittersManager.instance.heavyMass;
			TogglePhysics(enable: true);
		}
	}

	public void SetJointSoft(Rigidbody rbToConnect)
	{
		if (!(joint != null))
		{
			Debug.Log("Critters SetJointSoft " + base.gameObject);
			CreateJoint(rbToConnect);
			joint.xMotion = ConfigurableJointMotion.Limited;
			joint.yMotion = ConfigurableJointMotion.Limited;
			joint.zMotion = ConfigurableJointMotion.Limited;
			joint.angularXMotion = ConfigurableJointMotion.Limited;
			joint.angularYMotion = ConfigurableJointMotion.Limited;
			joint.angularZMotion = ConfigurableJointMotion.Limited;
			rb.mass = CrittersManager.instance.lightMass;
			TogglePhysics(enable: true);
		}
	}

	private void CreateJoint(Rigidbody rbToConnect, bool setParentNull = true)
	{
		if (!(joint != null))
		{
			joint = base.gameObject.AddComponent<ConfigurableJoint>();
			drive = new JointDrive
			{
				positionSpring = CrittersManager.instance.springForce,
				positionDamper = CrittersManager.instance.damperForce,
				maximumForce = 10000f
			};
			angularDrive = new JointDrive
			{
				positionSpring = CrittersManager.instance.springAngularForce,
				positionDamper = CrittersManager.instance.damperAngularForce,
				maximumForce = 10000f
			};
			linearLimitDrive = new SoftJointLimit
			{
				limit = CrittersManager.instance.springForce
			};
			linearLimitSpringDrive = new SoftJointLimitSpring
			{
				spring = CrittersManager.instance.springForce
			};
			joint.linearLimit = linearLimitDrive;
			joint.linearLimitSpring = linearLimitSpringDrive;
			joint.angularYLimit = joint.linearLimit;
			joint.angularZLimit = joint.linearLimit;
			joint.angularXDrive = angularDrive;
			joint.angularYZDrive = angularDrive;
			joint.xDrive = drive;
			joint.yDrive = drive;
			joint.zDrive = drive;
			joint.autoConfigureConnectedAnchor = true;
			joint.enableCollision = false;
			joint.connectedBody = rbToConnect;
			rb.excludeLayers = CrittersManager.instance.movementLayers;
			rb.useGravity = false;
			if (setParentNull)
			{
				base.transform.SetParent(null, worldPositionStays: true);
			}
		}
	}

	public void DisconnectJoint()
	{
		rb.excludeLayers = CrittersManager.instance.containerLayer;
		rb.useGravity = true;
		if (joint != null)
		{
			UnityEngine.Object.Destroy(joint);
		}
		joint = null;
		if (parentActorId != -1)
		{
			CrittersManager.instance.actorById.TryGetValue(parentActorId, out var value);
			base.transform.SetParent(value.transform, worldPositionStays: true);
			MoveActor(lastImpulsePosition, lastImpulseQuaternion, local: true, updateImpulses: false);
			TogglePhysics(enable: false);
		}
	}

	public void AttemptRemoveStoredObjectCollider(int oldParentId, bool playSound = true)
	{
		if (CrittersManager.instance.actorById.TryGetValue(oldParentId, out var value) && value is CrittersBag)
		{
			((CrittersBag)value).RemoveStoredObjectCollider(this, playSound);
		}
	}

	public void AttemptAddStoredObjectCollider(CrittersActor actor)
	{
		if (actor is CrittersBag)
		{
			((CrittersBag)actor).AddStoredObjectCollider(this);
		}
	}

	public bool AttemptSetEquipmentStorable()
	{
		if (!equipmentStorable)
		{
			return false;
		}
		localCanStore = true;
		return true;
	}
}
