using System;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(3)]
public class BarrelCannon : NetworkComponent
{
	private enum BarrelCannonState
	{
		Idle,
		Loaded,
		MovingToFirePosition,
		Firing,
		PostFireCooldown,
		ReturningToIdlePosition
	}

	private class BarrelCannonSyncedState
	{
		public BarrelCannonState currentState;

		public bool hasAuthorityPassenger;

		public float firingPositionLerpValue;
	}

	[StructLayout(LayoutKind.Explicit, Size = 12)]
	[NetworkStructWeaved(3)]
	private struct BarrelCannonSyncedStateData : INetworkStruct
	{
		[FieldOffset(0)]
		[FixedBufferProperty(typeof(BarrelCannonState), typeof(UnityValueSurrogate_0040ReaderWriter_0040BarrelCannon__BarrelCannonState), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00401 _CurrentState;

		[FieldOffset(4)]
		[FixedBufferProperty(typeof(NetworkBool), typeof(UnityValueSurrogate_0040ElementReaderWriterNetworkBool), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00401 _HasAuthorityPassenger;

		[Networked]
		[NetworkedWeaved(0, 1)]
		public unsafe BarrelCannonState CurrentState
		{
			readonly get
			{
				return *(BarrelCannonState*)Native.ReferenceToPointer(ref _CurrentState);
			}
			set
			{
				*(BarrelCannonState*)Native.ReferenceToPointer(ref _CurrentState) = value;
			}
		}

		[Networked]
		[NetworkedWeaved(1, 1)]
		public unsafe NetworkBool HasAuthorityPassenger
		{
			readonly get
			{
				return *(NetworkBool*)Native.ReferenceToPointer(ref _HasAuthorityPassenger);
			}
			set
			{
				*(NetworkBool*)Native.ReferenceToPointer(ref _HasAuthorityPassenger) = value;
			}
		}

		[field: FieldOffset(8)]
		public float FiringPositionLerpValue { get; set; }

		public BarrelCannonSyncedStateData(BarrelCannonState state, bool hasAuthPassenger, float firingPosLerpVal)
		{
			CurrentState = state;
			HasAuthorityPassenger = hasAuthPassenger;
			FiringPositionLerpValue = firingPosLerpVal;
		}

		public static implicit operator BarrelCannonSyncedStateData(BarrelCannonSyncedState state)
		{
			return new BarrelCannonSyncedStateData(state.currentState, state.hasAuthorityPassenger, state.firingPositionLerpValue);
		}
	}

	[SerializeField]
	private float firingSpeed = 10f;

	[Header("Cannon's Movement Before Firing")]
	[SerializeField]
	private Vector3 firingPositionOffset = Vector3.zero;

	[SerializeField]
	private Vector3 firingRotationOffset = Vector3.zero;

	[SerializeField]
	private AnimationCurve firePositionAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve fireRotationAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Cannon State Change Timing Parameters")]
	[SerializeField]
	private float moveToFiringPositionTime = 0.5f;

	[SerializeField]
	[Tooltip("The minimum time to wait after a gorilla enters the cannon before it starts moving into the firing position.")]
	private float cannonEntryDelayTime = 0.25f;

	[SerializeField]
	[Tooltip("The minimum time to wait after a gorilla enters the cannon before it starts moving into the firing position.")]
	private float preFiringDelayTime = 0.25f;

	[SerializeField]
	[Tooltip("The minimum time to wait after the cannon fires before it starts moving back to the idle position.")]
	private float postFiringCooldownTime = 0.25f;

	[SerializeField]
	private float returnToIdlePositionTime = 1f;

	[Header("Component References")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private CapsuleCollider triggerCollider;

	[SerializeField]
	private Collider[] colliders;

	private BarrelCannonSyncedState syncedState = new BarrelCannonSyncedState();

	private Collider[] triggerOverlapResults = new Collider[16];

	private bool localPlayerInside;

	private Rigidbody localPlayerRigidbody;

	private float stateStartTime;

	private float localFiringPositionLerpValue;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 3)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private BarrelCannonSyncedStateData _Data;

	[Networked]
	[NetworkedWeaved(0, 3)]
	private unsafe BarrelCannonSyncedStateData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BarrelCannon.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(BarrelCannonSyncedStateData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BarrelCannon.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(BarrelCannonSyncedStateData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	private void Update()
	{
		if (base.IsMine)
		{
			AuthorityUpdate();
		}
		else
		{
			ClientUpdate();
		}
		SharedUpdate();
	}

	private void AuthorityUpdate()
	{
		float time = Time.time;
		syncedState.hasAuthorityPassenger = localPlayerInside;
		switch (syncedState.currentState)
		{
		default:
			if (localPlayerInside)
			{
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.Loaded;
			}
			break;
		case BarrelCannonState.Loaded:
			if (time - stateStartTime > cannonEntryDelayTime)
			{
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.MovingToFirePosition;
			}
			break;
		case BarrelCannonState.MovingToFirePosition:
			if (moveToFiringPositionTime > Mathf.Epsilon)
			{
				syncedState.firingPositionLerpValue = Mathf.Clamp01((time - stateStartTime) / moveToFiringPositionTime);
			}
			else
			{
				syncedState.firingPositionLerpValue = 1f;
			}
			if (syncedState.firingPositionLerpValue >= 1f - Mathf.Epsilon)
			{
				syncedState.firingPositionLerpValue = 1f;
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.Firing;
			}
			break;
		case BarrelCannonState.Firing:
			if (localPlayerInside && localPlayerRigidbody != null)
			{
				Vector3 vector = base.transform.position - GorillaTagger.Instance.headCollider.transform.position;
				localPlayerRigidbody.MovePosition(localPlayerRigidbody.position + vector);
			}
			if (time - stateStartTime > preFiringDelayTime)
			{
				base.transform.localPosition = firingPositionOffset;
				base.transform.localRotation = Quaternion.Euler(firingRotationOffset);
				FireBarrelCannonLocal(base.transform.position, base.transform.up);
				if (PhotonNetwork.InRoom && GorillaGameManager.instance != null)
				{
					SendRPC("FireBarrelCannonRPC", RpcTarget.Others, base.transform.position, base.transform.up);
				}
				Collider[] array = colliders;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].enabled = false;
				}
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.PostFireCooldown;
			}
			break;
		case BarrelCannonState.PostFireCooldown:
			if (time - stateStartTime > postFiringCooldownTime)
			{
				Collider[] array = colliders;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].enabled = true;
				}
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.ReturningToIdlePosition;
			}
			break;
		case BarrelCannonState.ReturningToIdlePosition:
			if (returnToIdlePositionTime > Mathf.Epsilon)
			{
				syncedState.firingPositionLerpValue = 1f - Mathf.Clamp01((time - stateStartTime) / returnToIdlePositionTime);
			}
			else
			{
				syncedState.firingPositionLerpValue = 0f;
			}
			if (syncedState.firingPositionLerpValue <= Mathf.Epsilon)
			{
				syncedState.firingPositionLerpValue = 0f;
				stateStartTime = time;
				syncedState.currentState = BarrelCannonState.Idle;
			}
			break;
		}
	}

	private void ClientUpdate()
	{
		if (!syncedState.hasAuthorityPassenger && syncedState.currentState == BarrelCannonState.Idle && localPlayerInside)
		{
			RequestOwnership();
		}
	}

	private void SharedUpdate()
	{
		if (syncedState.firingPositionLerpValue != localFiringPositionLerpValue)
		{
			localFiringPositionLerpValue = syncedState.firingPositionLerpValue;
			base.transform.localPosition = Vector3.Lerp(Vector3.zero, firingPositionOffset, firePositionAnimationCurve.Evaluate(localFiringPositionLerpValue));
			base.transform.localRotation = Quaternion.Euler(Vector3.Lerp(Vector3.zero, firingRotationOffset, fireRotationAnimationCurve.Evaluate(localFiringPositionLerpValue)));
		}
	}

	private void FireBarrelCannonRPC(Vector3 cannonCenter, Vector3 firingDirection)
	{
	}

	private void FireBarrelCannonLocal(Vector3 cannonCenter, Vector3 firingDirection)
	{
		if (audioSource != null)
		{
			audioSource.GTPlay();
		}
		if (localPlayerInside && localPlayerRigidbody != null)
		{
			Vector3 vector = cannonCenter - GorillaTagger.Instance.headCollider.transform.position;
			localPlayerRigidbody.position += vector;
			localPlayerRigidbody.linearVelocity = firingDirection * firingSpeed;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (LocalPlayerTriggerFilter(other, out var rb))
		{
			localPlayerInside = true;
			localPlayerRigidbody = rb;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (LocalPlayerTriggerFilter(other, out var _))
		{
			localPlayerInside = false;
			localPlayerRigidbody = null;
		}
	}

	private bool LocalPlayerTriggerFilter(Collider other, out Rigidbody rb)
	{
		rb = null;
		if (other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			rb = GorillaTagger.Instance.GetComponent<Rigidbody>();
		}
		return rb != null;
	}

	private bool IsLocalPlayerInCannon()
	{
		GetCapsulePoints(triggerCollider, out var pointA, out var pointB);
		Physics.OverlapCapsuleNonAlloc(pointA, pointB, triggerCollider.radius, triggerOverlapResults);
		for (int i = 0; i < triggerOverlapResults.Length; i++)
		{
			if (LocalPlayerTriggerFilter(triggerOverlapResults[i], out var _))
			{
				return true;
			}
		}
		return false;
	}

	private void GetCapsulePoints(CapsuleCollider capsule, out Vector3 pointA, out Vector3 pointB)
	{
		float num = capsule.height * 0.5f - capsule.radius;
		pointA = capsule.transform.position + capsule.transform.up * num;
		pointB = capsule.transform.position - capsule.transform.up * num;
	}

	public override void WriteDataFusion()
	{
		Data = syncedState;
	}

	public override void ReadDataFusion()
	{
		syncedState.currentState = Data.CurrentState;
		syncedState.hasAuthorityPassenger = Data.HasAuthorityPassenger;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(syncedState.currentState);
		stream.SendNext(syncedState.hasAuthorityPassenger);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		syncedState.currentState = (BarrelCannonState)stream.ReceiveNext();
		syncedState.hasAuthorityPassenger = (bool)stream.ReceiveNext();
	}

	public override void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
	{
		if (!localPlayerInside)
		{
			targetView.TransferOwnership(requestingPlayer);
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
}
