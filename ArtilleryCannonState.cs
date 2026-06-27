using System;
using System.Runtime.InteropServices;
using Fusion;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(XSceneRefTarget))]
[NetworkBehaviourWeaved(8)]
public class ArtilleryCannonState : NetworkComponent
{
	internal struct CrankSyncState
	{
		public int holderActorNr;

		public bool isLeftHand;

		public float angle;
	}

	private enum ArtilleryMsg : byte
	{
		CrankGrabLeft,
		CrankGrabRight,
		CrankRelease,
		CrankInput,
		Fire
	}

	[StructLayout(LayoutKind.Explicit, Size = 32)]
	[NetworkStructWeaved(8)]
	private struct FusionSyncState : INetworkStruct
	{
		[FieldOffset(0)]
		public float pitch;

		[FieldOffset(4)]
		public float yaw;

		[FieldOffset(8)]
		public int pitchHolderActorNr;

		[FieldOffset(12)]
		public NetworkBool pitchIsLeftHand;

		[FieldOffset(16)]
		public float pitchCrankAngle;

		[FieldOffset(20)]
		public int yawHolderActorNr;

		[FieldOffset(24)]
		public NetworkBool yawIsLeftHand;

		[FieldOffset(28)]
		public float yawCrankAngle;
	}

	internal const int CRANK_PITCH = 0;

	internal const int CRANK_YAW = 1;

	[Header("Rotation Limits")]
	[SerializeField]
	private float pitchMin = -10f;

	[SerializeField]
	private float pitchMax = 60f;

	[Tooltip("How many degrees the cannon rotates per degree of crank rotation")]
	[SerializeField]
	private float degreesPerCrankDegree = 0.5f;

	[Header("Firing")]
	[SerializeField]
	private float fireCooldown = 2f;

	private float currentPitch;

	private float currentYaw;

	private float lastFireTime;

	internal CrankSyncState pitchCrankSync;

	internal CrankSyncState yawCrankSync;

	private const float GRAB_GRACE_PERIOD = 1f;

	private float pitchPendingGrabTime;

	private float yawPendingGrabTime;

	[WeaverGenerated]
	[DefaultForProperty("FusionData", 0, 8)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private FusionSyncState _FusionData;

	internal float CurrentPitch => currentPitch;

	internal float CurrentYaw => currentYaw;

	internal float PitchMin => pitchMin;

	internal float PitchMax => pitchMax;

	internal float DegreesPerCrankDegree => degreesPerCrankDegree;

	private int LocalActorNr
	{
		get
		{
			if (PhotonNetwork.LocalPlayer == null)
			{
				return -1;
			}
			return PhotonNetwork.LocalPlayer.ActorNumber;
		}
	}

	[Networked]
	[NetworkedWeaved(0, 8)]
	private unsafe FusionSyncState FusionData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ArtilleryCannonState.FusionData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(FusionSyncState*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ArtilleryCannonState.FusionData. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(FusionSyncState*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	internal event Action onRotationChanged;

	internal event Action onFired;

	protected override void Awake()
	{
		base.Awake();
		pitchCrankSync.holderActorNr = -1;
		yawCrankSync.holderActorNr = -1;
	}

	internal void UpdateLocalCrankState(int crankIndex, bool isLeftHand, float angle)
	{
		ref CrankSyncState reference = ref crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync;
		int localActorNr = LocalActorNr;
		if (reference.holderActorNr == localActorNr)
		{
			reference.isLeftHand = isLeftHand;
			reference.angle = angle;
		}
	}

	internal static VRRig FindRigForActor(int actorNr)
	{
		if (VRRigCache.Instance.TryGetVrrig(actorNr, out var playerRig))
		{
			return playerRig.Rig;
		}
		return null;
	}

	internal bool NotifyCrankGrabbed(int crankIndex, bool isLeftHand)
	{
		ref CrankSyncState reference = ref crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync;
		if (reference.holderActorNr != -1)
		{
			return false;
		}
		reference.holderActorNr = LocalActorNr;
		crankIndex == 0 ? ref pitchPendingGrabTime : ref yawPendingGrabTime = Time.time;
		if (PhotonNetwork.InRoom)
		{
			SendRPC("RPC_ArtilleryMessage", RpcTarget.MasterClient, (!isLeftHand) ? ((byte)1) : ((byte)0), (byte)crankIndex, 0f);
		}
		return true;
	}

	internal void NotifyCrankReleased(int crankIndex, float finalAngle)
	{
		ref CrankSyncState reference = ref crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync;
		reference.holderActorNr = -1;
		reference.angle = finalAngle;
		crankIndex == 0 ? ref pitchPendingGrabTime : ref yawPendingGrabTime = 0f;
		if (PhotonNetwork.InRoom)
		{
			SendRPC("RPC_ArtilleryMessage", RpcTarget.All, (byte)2, (byte)crankIndex, finalAngle);
		}
	}

	internal void NotifyCrankInput(int crankIndex, float degrees)
	{
		if (crankIndex == 0)
		{
			currentPitch = Mathf.Clamp(currentPitch + degrees * degreesPerCrankDegree, pitchMin, pitchMax);
		}
		else
		{
			currentYaw += degrees * degreesPerCrankDegree;
		}
		this.onRotationChanged?.Invoke();
		if (PhotonNetwork.InRoom)
		{
			float num = ((crankIndex == 0) ? currentPitch : currentYaw);
			SendRPC("RPC_ArtilleryMessage", RpcTarget.MasterClient, (byte)3, (byte)crankIndex, num);
		}
	}

	internal bool TryFire()
	{
		if (Time.time < lastFireTime + fireCooldown)
		{
			return false;
		}
		lastFireTime = Time.time;
		if (PhotonNetwork.InRoom)
		{
			SendRPC("RPC_ArtilleryMessage", RpcTarget.Others, (byte)4, (byte)0, 0f);
		}
		return true;
	}

	[PunRPC]
	public void RPC_ArtilleryMessage(byte msgType, byte crankIndex, float floatParam, PhotonMessageInfo info)
	{
		switch ((ArtilleryMsg)msgType)
		{
		case ArtilleryMsg.CrankGrabLeft:
		case ArtilleryMsg.CrankGrabRight:
			if (PhotonNetwork.IsMasterClient)
			{
				ref CrankSyncState reference = ref crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync;
				if (reference.holderActorNr == -1)
				{
					reference.holderActorNr = info.Sender.ActorNumber;
					reference.isLeftHand = msgType == 0;
				}
			}
			break;
		case ArtilleryMsg.CrankRelease:
		{
			ref CrankSyncState reference2 = ref crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync;
			if (reference2.holderActorNr == info.Sender.ActorNumber)
			{
				reference2.holderActorNr = -1;
				reference2.angle = floatParam;
			}
			break;
		}
		case ArtilleryMsg.CrankInput:
			if (PhotonNetwork.IsMasterClient && (crankIndex == 0 ? ref pitchCrankSync : ref yawCrankSync).holderActorNr == info.Sender.ActorNumber)
			{
				if (crankIndex == 0)
				{
					currentPitch = Mathf.Clamp(floatParam, pitchMin, pitchMax);
				}
				else
				{
					currentYaw = floatParam;
				}
				this.onRotationChanged?.Invoke();
			}
			break;
		case ArtilleryMsg.Fire:
		{
			int actorNumber = info.Sender.ActorNumber;
			if (pitchCrankSync.holderActorNr == actorNumber || yawCrankSync.holderActorNr == actorNumber)
			{
				this.onFired?.Invoke();
			}
			break;
		}
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(currentPitch);
		stream.SendNext(currentYaw);
		stream.SendNext(pitchCrankSync.holderActorNr);
		stream.SendNext(pitchCrankSync.isLeftHand);
		stream.SendNext(pitchCrankSync.angle);
		stream.SendNext(yawCrankSync.holderActorNr);
		stream.SendNext(yawCrankSync.isLeftHand);
		stream.SendNext(yawCrankSync.angle);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		float num = (float)stream.ReceiveNext();
		float num2 = (float)stream.ReceiveNext();
		int localActorNr = LocalActorNr;
		ReadCrankSyncPUN(stream, ref pitchCrankSync, ref pitchPendingGrabTime, localActorNr);
		ReadCrankSyncPUN(stream, ref yawCrankSync, ref yawPendingGrabTime, localActorNr);
		if (pitchCrankSync.holderActorNr != localActorNr)
		{
			currentPitch = num;
		}
		if (yawCrankSync.holderActorNr != localActorNr)
		{
			currentYaw = num2;
		}
		this.onRotationChanged?.Invoke();
	}

	private void ReadCrankSyncPUN(PhotonStream stream, ref CrankSyncState crank, ref float pendingTime, int localActor)
	{
		int num = (int)stream.ReceiveNext();
		bool isLeftHand = (bool)stream.ReceiveNext();
		float angle = (float)stream.ReceiveNext();
		if (pendingTime > 0f && crank.holderActorNr == localActor)
		{
			if (num == localActor)
			{
				pendingTime = 0f;
			}
			else if (num != -1)
			{
				pendingTime = 0f;
			}
			else
			{
				if (!(Time.time - pendingTime > 1f))
				{
					return;
				}
				pendingTime = 0f;
			}
		}
		crank.holderActorNr = num;
		crank.isLeftHand = isLeftHand;
		crank.angle = angle;
	}

	public override void WriteDataFusion()
	{
		FusionData = new FusionSyncState
		{
			pitch = currentPitch,
			yaw = currentYaw,
			pitchHolderActorNr = pitchCrankSync.holderActorNr,
			pitchIsLeftHand = pitchCrankSync.isLeftHand,
			pitchCrankAngle = pitchCrankSync.angle,
			yawHolderActorNr = yawCrankSync.holderActorNr,
			yawIsLeftHand = yawCrankSync.isLeftHand,
			yawCrankAngle = yawCrankSync.angle
		};
	}

	public override void ReadDataFusion()
	{
		FusionSyncState fusionData = FusionData;
		int localActorNr = LocalActorNr;
		ReadCrankSyncFusion(ref pitchCrankSync, ref pitchPendingGrabTime, localActorNr, fusionData.pitchHolderActorNr, fusionData.pitchIsLeftHand, fusionData.pitchCrankAngle);
		ReadCrankSyncFusion(ref yawCrankSync, ref yawPendingGrabTime, localActorNr, fusionData.yawHolderActorNr, fusionData.yawIsLeftHand, fusionData.yawCrankAngle);
		if (pitchCrankSync.holderActorNr != localActorNr)
		{
			currentPitch = fusionData.pitch;
		}
		if (yawCrankSync.holderActorNr != localActorNr)
		{
			currentYaw = fusionData.yaw;
		}
		this.onRotationChanged?.Invoke();
	}

	private void ReadCrankSyncFusion(ref CrankSyncState crank, ref float pendingTime, int localActor, int incomingHolder, bool incomingLeftHand, float incomingAngle)
	{
		if (pendingTime > 0f && crank.holderActorNr == localActor)
		{
			if (incomingHolder == localActor)
			{
				pendingTime = 0f;
			}
			else if (incomingHolder != -1)
			{
				pendingTime = 0f;
			}
			else
			{
				if (!(Time.time - pendingTime > 1f))
				{
					return;
				}
				pendingTime = 0f;
			}
		}
		crank.holderActorNr = incomingHolder;
		crank.isLeftHand = incomingLeftHand;
		crank.angle = incomingAngle;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		FusionData = _FusionData;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_FusionData = FusionData;
	}
}
