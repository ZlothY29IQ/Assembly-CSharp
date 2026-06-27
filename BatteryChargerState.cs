using System;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(XSceneRefTarget))]
[NetworkBehaviourWeaved(62)]
public class BatteryChargerState : NetworkComponent
{
	internal struct CrankSyncState
	{
		public int holderActorNr;

		public bool isLeftHand;

		public float angle;
	}

	private enum BatteryMsg : byte
	{
		CrankGrabLeft,
		CrankGrabRight,
		CrankRelease,
		CrankInput
	}

	[StructLayout(LayoutKind.Explicit, Size = 12)]
	[NetworkStructWeaved(3)]
	private struct FusionCrankData : INetworkStruct
	{
		[FieldOffset(0)]
		public int holderActorNr;

		[FieldOffset(4)]
		public NetworkBool isLeftHand;

		[FieldOffset(8)]
		public float angle;
	}

	[StructLayout(LayoutKind.Explicit, Size = 8)]
	[NetworkStructWeaved(2)]
	private struct FusionSyncState : INetworkStruct
	{
		[FieldOffset(0)]
		public float charge;

		[FieldOffset(4)]
		public int eventPhase;
	}

	internal const int MAX_CRANKS = 20;

	[Header("Charging")]
	[Tooltip("Charge added per degree of crank rotation (across all cranks)")]
	[SerializeField]
	private float chargePerCrankDegree = 0.001f;

	[Tooltip("Charge drains at this rate per second when no one is cranking")]
	[SerializeField]
	private float drainPerSecond = 0.02f;

	[Tooltip("Maximum charge level (0 to 1)")]
	[SerializeField]
	private float maxCharge = 1f;

	private float currentCharge;

	private int activeCrankerCount;

	private int eventPhase = -1;

	internal CrankSyncState[] crankSyncs = new CrankSyncState[20];

	private const float GRAB_GRACE_PERIOD = 1f;

	private float[] pendingGrabTime = new float[20];

	private const float CRANK_RPC_INTERVAL = 1f;

	private float nextCrankRPCTimestamp;

	private float pendingCrankCharge;

	private int pendingCrankIndex = -1;

	private bool m_disableNetworking;

	[WeaverGenerated]
	[DefaultForProperty("FusionData", 0, 2)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private FusionSyncState _FusionData;

	[WeaverGenerated]
	[DefaultForProperty("FusionCranks", 2, 60)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private FusionCrankData[] _FusionCranks;

	internal float CurrentCharge => currentCharge;

	internal float MaxCharge => maxCharge;

	internal float ChargePercent
	{
		get
		{
			if (!(maxCharge > 0f))
			{
				return 0f;
			}
			return currentCharge / maxCharge;
		}
	}

	internal float ChargePerCrankDegree => chargePerCrankDegree;

	internal int EventPhase => eventPhase;

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
	[NetworkedWeaved(0, 2)]
	private unsafe FusionSyncState FusionData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BatteryChargerState.FusionData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(FusionSyncState*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BatteryChargerState.FusionData. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(FusionSyncState*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	[Networked]
	[Capacity(20)]
	[NetworkedWeaved(2, 60)]
	[NetworkedWeavedArray(20, 3, typeof(ReaderWriter_0040BatteryChargerState__FusionCrankData))]
	private unsafe NetworkArray<FusionCrankData> FusionCranks
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BatteryChargerState.FusionCranks. Networked properties can only be accessed when Spawned() has been called.");
			}
			return new NetworkArray<FusionCrankData>((byte*)((NetworkBehaviour)this).Ptr + 8, 20, ReaderWriter_0040BatteryChargerState__FusionCrankData.GetInstance());
		}
	}

	internal event Action onChargeChanged;

	internal event Action onFullyCharged;

	internal event Action<int> onEventPhaseChanged;

	public void SetChargePerCrankDegree(float chargeRate)
	{
		chargePerCrankDegree = chargeRate;
	}

	protected override void Awake()
	{
		base.Awake();
		for (int i = 0; i < 20; i++)
		{
			crankSyncs[i].holderActorNr = -1;
		}
	}

	private void Update()
	{
		if (activeCrankerCount <= 0 && currentCharge > 0f)
		{
			currentCharge = Mathf.Max(0f, currentCharge - drainPerSecond * Time.deltaTime);
			this.onChargeChanged?.Invoke();
		}
		activeCrankerCount = 0;
		for (int i = 0; i < 20; i++)
		{
			if (crankSyncs[i].holderActorNr != -1)
			{
				activeCrankerCount++;
			}
		}
	}

	internal void UpdateLocalCrankState(int crankIndex, bool isLeftHand, float angle)
	{
		if (crankIndex >= 0 && crankIndex < 20)
		{
			ref CrankSyncState reference = ref crankSyncs[crankIndex];
			int localActorNr = LocalActorNr;
			if (reference.holderActorNr == localActorNr)
			{
				reference.isLeftHand = isLeftHand;
				reference.angle = angle;
			}
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
		if (crankIndex < 0 || crankIndex >= 20)
		{
			return false;
		}
		ref CrankSyncState reference = ref crankSyncs[crankIndex];
		if (reference.holderActorNr != -1)
		{
			return false;
		}
		reference.holderActorNr = LocalActorNr;
		pendingGrabTime[crankIndex] = Time.time;
		if (PhotonNetwork.InRoom)
		{
			SendRPC("RPC_BatteryMessage", RpcTarget.MasterClient, (!isLeftHand) ? ((byte)1) : ((byte)0), (byte)crankIndex, 0f);
		}
		return true;
	}

	internal void NotifyCrankReleased(int crankIndex, float finalAngle)
	{
		if (crankIndex >= 0 && crankIndex < 20)
		{
			FlushCrankRPC();
			ref CrankSyncState reference = ref crankSyncs[crankIndex];
			reference.holderActorNr = -1;
			reference.angle = finalAngle;
			pendingGrabTime[crankIndex] = 0f;
			if (PhotonNetwork.InRoom)
			{
				SendRPC("RPC_BatteryMessage", RpcTarget.All, (byte)2, (byte)crankIndex, finalAngle);
			}
		}
	}

	internal void NotifyCrankInput(int crankIndex, float degrees)
	{
		if (crankIndex < 0 || crankIndex >= 20)
		{
			return;
		}
		float num = Mathf.Abs(degrees) * chargePerCrankDegree;
		if (num <= 0f)
		{
			return;
		}
		float num2 = currentCharge;
		currentCharge = Mathf.Clamp(currentCharge + num, 0f, maxCharge);
		this.onChargeChanged?.Invoke();
		if (num2 < maxCharge && currentCharge >= maxCharge)
		{
			this.onFullyCharged?.Invoke();
		}
		if (PhotonNetwork.InRoom)
		{
			pendingCrankCharge = currentCharge;
			pendingCrankIndex = crankIndex;
			if (Time.time >= nextCrankRPCTimestamp)
			{
				FlushCrankRPC();
			}
		}
	}

	private void FlushCrankRPC()
	{
		if (pendingCrankIndex >= 0 && !(pendingCrankCharge <= 0f))
		{
			SendRPC("RPC_BatteryMessage", RpcTarget.MasterClient, (byte)3, (byte)pendingCrankIndex, pendingCrankCharge);
			nextCrankRPCTimestamp = Time.time + 1f;
			pendingCrankIndex = -1;
		}
	}

	public void DisableNetworking()
	{
		m_disableNetworking = true;
	}

	public void EnableNetworking()
	{
		m_disableNetworking = false;
	}

	[PunRPC]
	public void RPC_BatteryMessage(byte msgType, byte crankIndex, float floatParam, PhotonMessageInfo info)
	{
		if (m_disableNetworking)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "RPC_BatteryMessage");
		if (crankIndex >= 20)
		{
			return;
		}
		switch ((BatteryMsg)msgType)
		{
		case BatteryMsg.CrankGrabLeft:
		case BatteryMsg.CrankGrabRight:
			if (PhotonNetwork.IsMasterClient)
			{
				ref CrankSyncState reference2 = ref crankSyncs[crankIndex];
				if (reference2.holderActorNr == -1)
				{
					reference2.holderActorNr = info.Sender.ActorNumber;
					reference2.isLeftHand = msgType == 0;
				}
			}
			break;
		case BatteryMsg.CrankRelease:
		{
			ref CrankSyncState reference = ref crankSyncs[crankIndex];
			if (reference.holderActorNr == info.Sender.ActorNumber)
			{
				reference.holderActorNr = -1;
				reference.angle = floatParam.ClampSafe(-360f, 360f);
			}
			break;
		}
		case BatteryMsg.CrankInput:
			if (PhotonNetwork.IsMasterClient && crankSyncs[crankIndex].holderActorNr == info.Sender.ActorNumber)
			{
				float num = currentCharge;
				currentCharge = floatParam.ClampSafe(0f, maxCharge);
				this.onChargeChanged?.Invoke();
				if (num < maxCharge && currentCharge >= maxCharge)
				{
					this.onFullyCharged?.Invoke();
				}
			}
			break;
		}
	}

	public void SetEventPhase(int phase)
	{
		if ((!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) && phase != eventPhase)
		{
			eventPhase = phase;
			this.onEventPhaseChanged?.Invoke(phase);
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!m_disableNetworking)
		{
			stream.SendNext(currentCharge);
			stream.SendNext(eventPhase);
			for (int i = 0; i < 20; i++)
			{
				stream.SendNext(crankSyncs[i].holderActorNr);
				stream.SendNext(crankSyncs[i].isLeftHand);
				stream.SendNext(crankSyncs[i].angle);
			}
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient || m_disableNetworking)
		{
			return;
		}
		float num = (float)stream.ReceiveNext();
		int num2 = (int)stream.ReceiveNext();
		int localActorNr = LocalActorNr;
		for (int i = 0; i < 20; i++)
		{
			int num3 = (int)stream.ReceiveNext();
			bool isLeftHand = (bool)stream.ReceiveNext();
			float angle = (float)stream.ReceiveNext();
			if (pendingGrabTime[i] > 0f && crankSyncs[i].holderActorNr == localActorNr)
			{
				if (num3 == localActorNr)
				{
					pendingGrabTime[i] = 0f;
				}
				else if (num3 != -1)
				{
					pendingGrabTime[i] = 0f;
				}
				else
				{
					if (!(Time.time - pendingGrabTime[i] > 1f))
					{
						continue;
					}
					pendingGrabTime[i] = 0f;
				}
			}
			crankSyncs[i].holderActorNr = num3;
			crankSyncs[i].isLeftHand = isLeftHand;
			crankSyncs[i].angle = angle;
		}
		bool flag = false;
		for (int j = 0; j < 20; j++)
		{
			if (crankSyncs[j].holderActorNr == localActorNr)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			currentCharge = num;
		}
		this.onChargeChanged?.Invoke();
		if (num2 != eventPhase)
		{
			eventPhase = num2;
			this.onEventPhaseChanged?.Invoke(eventPhase);
		}
	}

	public override void WriteDataFusion()
	{
		FusionData = new FusionSyncState
		{
			charge = currentCharge,
			eventPhase = eventPhase
		};
		for (int i = 0; i < 20; i++)
		{
			FusionCranks.Set(i, new FusionCrankData
			{
				holderActorNr = crankSyncs[i].holderActorNr,
				isLeftHand = crankSyncs[i].isLeftHand,
				angle = crankSyncs[i].angle
			});
		}
	}

	public override void ReadDataFusion()
	{
		FusionSyncState fusionData = FusionData;
		int localActorNr = LocalActorNr;
		for (int i = 0; i < 20; i++)
		{
			FusionCrankData fusionCrankData = FusionCranks[i];
			int holderActorNr = fusionCrankData.holderActorNr;
			if (pendingGrabTime[i] > 0f && crankSyncs[i].holderActorNr == localActorNr)
			{
				if (holderActorNr == localActorNr)
				{
					pendingGrabTime[i] = 0f;
				}
				else if (holderActorNr != -1)
				{
					pendingGrabTime[i] = 0f;
				}
				else
				{
					if (!(Time.time - pendingGrabTime[i] > 1f))
					{
						continue;
					}
					pendingGrabTime[i] = 0f;
				}
			}
			crankSyncs[i].holderActorNr = holderActorNr;
			crankSyncs[i].isLeftHand = fusionCrankData.isLeftHand;
			crankSyncs[i].angle = fusionCrankData.angle;
		}
		bool flag = false;
		for (int j = 0; j < 20; j++)
		{
			if (crankSyncs[j].holderActorNr == localActorNr)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			currentCharge = fusionData.charge;
		}
		this.onChargeChanged?.Invoke();
		if (fusionData.eventPhase != eventPhase)
		{
			eventPhase = fusionData.eventPhase;
			this.onEventPhaseChanged?.Invoke(eventPhase);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		FusionData = _FusionData;
		NetworkBehaviourUtils.InitializeNetworkArray(FusionCranks, _FusionCranks, "FusionCranks");
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_FusionData = FusionData;
		NetworkBehaviourUtils.CopyFromNetworkArray(FusionCranks, ref _FusionCranks);
	}
}
