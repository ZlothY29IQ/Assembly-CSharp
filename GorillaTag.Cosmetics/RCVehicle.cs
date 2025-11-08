using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace GorillaTag.Cosmetics;

public class RCVehicle : MonoBehaviour, ISpawnable
{
	protected enum State
	{
		Disabled,
		DockedLeft,
		DockedRight,
		Mobilized,
		Crashed
	}

	[SerializeField]
	private Transform leftDockParent;

	[SerializeField]
	private Transform rightDockParent;

	[SerializeField]
	private float maxRange = 100f;

	[SerializeField]
	private float maxDisconnectionTime = 10f;

	[SerializeField]
	private float crashRespawnDelay = 3f;

	[SerializeField]
	private bool crashOnHit;

	[SerializeField]
	private float crashOnHitSpeedThreshold = 5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float hitVelocityTransfer = 0.5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float projectileVelocityTransfer = 0.1f;

	[SerializeField]
	private float hitMaxHitSpeed = 4f;

	[SerializeField]
	[Range(0f, 1f)]
	private float joystickDeadzone = 0.1f;

	[Header("RCVehicle - Shared Event")]
	public UnityEvent OnHitImpact;

	protected State localState;

	protected State localStatePrev;

	protected float stateStartTime;

	protected RCRemoteHoldable connectedRemote;

	protected RCCosmeticNetworkSync networkSync;

	protected bool hasNetworkSync;

	protected RCRemoteHoldable.RCInput activeInput;

	protected Rigidbody rb;

	private bool waitingForTriggerRelease;

	private float disconnectionTime;

	private bool useLeftDock;

	private BoneOffset dockLeftOffset = new BoneOffset(GTHardCodedBones.EBone.forearm_L, new Vector3(-0.062f, 0.283f, -0.136f), new Vector3(275f, 0f, 25f));

	private BoneOffset dockRightOffset = new BoneOffset(GTHardCodedBones.EBone.forearm_R, new Vector3(0.069f, 0.265f, -0.128f), new Vector3(275f, 0f, 335f));

	private float networkSyncFollowRateExp = 2f;

	private Transform[] _vrRigBones;

	public bool HasLocalAuthority
	{
		get
		{
			if (PhotonNetwork.InRoom)
			{
				if (networkSync != null)
				{
					return networkSync.photonView.IsMine;
				}
				return false;
			}
			return true;
		}
	}

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	public virtual void WakeUpRemote(RCCosmeticNetworkSync sync)
	{
		networkSync = sync;
		hasNetworkSync = sync != null;
		if (!HasLocalAuthority && (!base.enabled || !base.gameObject.activeSelf))
		{
			localStatePrev = State.Disabled;
			base.enabled = true;
			base.gameObject.SetActive(value: true);
			RemoteUpdate(Time.deltaTime);
		}
	}

	public virtual void StartConnection(RCRemoteHoldable remote, RCCosmeticNetworkSync sync)
	{
		connectedRemote = remote;
		networkSync = sync;
		hasNetworkSync = sync != null;
		base.enabled = true;
		base.gameObject.SetActive(value: true);
		useLeftDock = remote.XRNode == XRNode.LeftHand;
		if (HasLocalAuthority && localState != State.Mobilized)
		{
			AuthorityBeginDocked();
		}
	}

	public virtual void EndConnection()
	{
		connectedRemote = null;
		activeInput = default(RCRemoteHoldable.RCInput);
		disconnectionTime = Time.time;
	}

	protected virtual void ResetToSpawnPosition()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (rb != null)
		{
			rb.isKinematic = true;
		}
		base.transform.parent = (useLeftDock ? leftDockParent : rightDockParent);
		base.transform.SetLocalPositionAndRotation(useLeftDock ? dockLeftOffset.pos : dockRightOffset.pos, useLeftDock ? dockLeftOffset.rot : dockRightOffset.rot);
		base.transform.localScale = (useLeftDock ? dockLeftOffset.scale : dockRightOffset.scale);
	}

	protected virtual void AuthorityBeginDocked()
	{
		localState = (useLeftDock ? State.DockedLeft : State.DockedRight);
		if (networkSync != null)
		{
			networkSync.syncedState.state = (byte)localState;
		}
		stateStartTime = Time.time;
		waitingForTriggerRelease = true;
		ResetToSpawnPosition();
		if (connectedRemote == null)
		{
			SetDisabledState();
		}
	}

	protected virtual void AuthorityBeginMobilization()
	{
		localState = State.Mobilized;
		if (networkSync != null)
		{
			networkSync.syncedState.state = (byte)localState;
		}
		stateStartTime = Time.time;
		base.transform.parent = null;
		rb.isKinematic = false;
	}

	protected virtual void AuthorityBeginCrash()
	{
		localState = State.Crashed;
		if (networkSync != null)
		{
			networkSync.syncedState.state = (byte)localState;
		}
		stateStartTime = Time.time;
	}

	protected virtual void SetDisabledState()
	{
		localState = State.Disabled;
		if (networkSync != null)
		{
			networkSync.syncedState.state = (byte)localState;
		}
		ResetToSpawnPosition();
		base.enabled = false;
		base.gameObject.SetActive(value: false);
	}

	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected virtual void OnEnable()
	{
	}

	void ISpawnable.OnSpawn(VRRig rig)
	{
		if ((object)rig == null)
		{
			GTDev.LogError("RCVehicle: Could not find VRRig in parents. If you are trying to make this a world item rather than a cosmetic then you'll have to refactor how it teleports back to the arms.", this);
			return;
		}
		if (!GTHardCodedBones.TryGetBoneXforms(rig, out _vrRigBones, out var outErrorMsg))
		{
			Debug.LogError("RCVehicle: " + outErrorMsg, this);
			return;
		}
		if (leftDockParent == null && !GTHardCodedBones.TryGetBoneXform(_vrRigBones, dockLeftOffset.bone, out leftDockParent))
		{
			GTDev.LogError("RCVehicle: Could not find left dock transform.", this);
		}
		if (rightDockParent == null && !GTHardCodedBones.TryGetBoneXform(_vrRigBones, dockRightOffset.bone, out rightDockParent))
		{
			GTDev.LogError("RCVehicle: Could not find right dock transform.", this);
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	protected virtual void OnDisable()
	{
		localState = State.Disabled;
		localStatePrev = State.Disabled;
	}

	public void ApplyRemoteControlInput(RCRemoteHoldable.RCInput rcInput)
	{
		activeInput.joystick.y = Mathf.Sign(rcInput.joystick.y) * Mathf.Lerp(0f, 1f, Mathf.InverseLerp(joystickDeadzone, 1f, Mathf.Abs(rcInput.joystick.y)));
		activeInput.joystick.x = Mathf.Sign(rcInput.joystick.x) * Mathf.Lerp(0f, 1f, Mathf.InverseLerp(joystickDeadzone, 1f, Mathf.Abs(rcInput.joystick.x)));
		activeInput.trigger = Mathf.Clamp(rcInput.trigger, -1f, 1f);
		activeInput.buttons = rcInput.buttons;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (HasLocalAuthority)
		{
			AuthorityUpdate(deltaTime);
		}
		else
		{
			RemoteUpdate(deltaTime);
		}
		SharedUpdate(deltaTime);
		localStatePrev = localState;
	}

	protected virtual void AuthorityUpdate(float dt)
	{
		switch (localState)
		{
		case State.Mobilized:
		{
			if (networkSync != null)
			{
				networkSync.syncedState.position = base.transform.position;
				networkSync.syncedState.rotation = base.transform.rotation;
			}
			bool num = (base.transform.position - leftDockParent.position).sqrMagnitude > maxRange * maxRange;
			bool flag = connectedRemote == null && Time.time - disconnectionTime > maxDisconnectionTime;
			if (num || flag)
			{
				AuthorityBeginCrash();
			}
			return;
		}
		case State.Crashed:
			if (Time.time > stateStartTime + crashRespawnDelay)
			{
				AuthorityBeginDocked();
			}
			return;
		}
		if (localState != localStatePrev)
		{
			ResetToSpawnPosition();
		}
		if (connectedRemote == null)
		{
			SetDisabledState();
			return;
		}
		if (waitingForTriggerRelease && activeInput.trigger < 0.25f)
		{
			waitingForTriggerRelease = false;
		}
		if (!waitingForTriggerRelease && activeInput.trigger > 0.25f)
		{
			AuthorityBeginMobilization();
		}
	}

	protected virtual void RemoteUpdate(float dt)
	{
		if (networkSync == null)
		{
			SetDisabledState();
			return;
		}
		localState = (State)networkSync.syncedState.state;
		switch (localState)
		{
		default:
			if (localStatePrev != State.DockedLeft)
			{
				useLeftDock = true;
				ResetToSpawnPosition();
			}
			break;
		case State.DockedRight:
			if (localStatePrev != State.DockedRight)
			{
				useLeftDock = false;
				ResetToSpawnPosition();
			}
			break;
		case State.Mobilized:
			if (localStatePrev != State.Mobilized)
			{
				rb.isKinematic = true;
				base.transform.parent = null;
			}
			base.transform.position = Vector3.Lerp(networkSync.syncedState.position, base.transform.position, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
			base.transform.rotation = Quaternion.Slerp(networkSync.syncedState.rotation, base.transform.rotation, Mathf.Exp((0f - networkSyncFollowRateExp) * dt));
			break;
		case State.Crashed:
			if (localStatePrev != State.Crashed)
			{
				rb.isKinematic = false;
				base.transform.parent = null;
				if (localStatePrev != State.Mobilized)
				{
					base.transform.position = networkSync.syncedState.position;
					base.transform.rotation = networkSync.syncedState.rotation;
				}
			}
			break;
		case State.Disabled:
			SetDisabledState();
			break;
		}
	}

	protected virtual void SharedUpdate(float dt)
	{
	}

	public virtual void AuthorityApplyImpact(Vector3 hitVelocity, bool isProjectile)
	{
		if (HasLocalAuthority && localState == State.Mobilized)
		{
			float num = (isProjectile ? projectileVelocityTransfer : hitVelocityTransfer);
			rb.AddForce(Vector3.ClampMagnitude(hitVelocity * num, hitMaxHitSpeed) * rb.mass, ForceMode.Impulse);
			if (isProjectile || (crashOnHit && hitVelocity.sqrMagnitude > crashOnHitSpeedThreshold * crashOnHitSpeedThreshold))
			{
				AuthorityBeginCrash();
			}
		}
		OnHitImpact?.Invoke();
	}

	protected float NormalizeAngle180(float angle)
	{
		angle = (angle + 180f) % 360f;
		if (angle < 0f)
		{
			angle += 360f;
		}
		return angle - 180f;
	}

	protected static void AddScaledGravityCompensationForce(Rigidbody rb, float scaleFactor, float gravityCompensation)
	{
		Vector3 gravity = Physics.gravity;
		Vector3 vector = -gravity * gravityCompensation;
		Vector3 vector2 = gravity + vector;
		Vector3 vector3 = vector2 * scaleFactor - vector2;
		rb.AddForce((vector + vector3) * rb.mass, ForceMode.Force);
	}
}
