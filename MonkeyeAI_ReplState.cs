using System;
using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(42)]
public class MonkeyeAI_ReplState : NetworkComponent
{
	public enum EStates
	{
		Sleeping,
		Patrolling,
		Chasing,
		ReturnToSleepPt,
		GoToSleep,
		BeginAttack,
		OpenFloor,
		DropPlayer,
		CloseFloor
	}

	[StructLayout(LayoutKind.Explicit, Size = 168)]
	[NetworkStructWeaved(42)]
	public struct MonkeyeAI_RepStateData : INetworkStruct
	{
		[FieldOffset(0)]
		[FixedBufferProperty(typeof(NetworkString<_32>), typeof(UnityValueSurrogate_0040ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004033 _UserId;

		[FieldOffset(132)]
		[FixedBufferProperty(typeof(Vector3), typeof(UnityValueSurrogate_0040ElementReaderWriterVector3), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00403 _AttackPos;

		[FieldOffset(144)]
		[FixedBufferProperty(typeof(float), typeof(UnityValueSurrogate_0040ElementReaderWriterSingle), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00401 _Timer;

		[FieldOffset(160)]
		[FixedBufferProperty(typeof(float), typeof(UnityValueSurrogate_0040ElementReaderWriterSingle), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_00401 _Alpha;

		[Networked]
		[NetworkedWeaved(0, 33)]
		public unsafe NetworkString<_32> UserId
		{
			readonly get
			{
				return *(NetworkString<_32>*)Native.ReferenceToPointer(ref _UserId);
			}
			set
			{
				*(NetworkString<_32>*)Native.ReferenceToPointer(ref _UserId) = value;
			}
		}

		[Networked]
		[NetworkedWeaved(33, 3)]
		public unsafe Vector3 AttackPos
		{
			readonly get
			{
				return *(Vector3*)Native.ReferenceToPointer(ref _AttackPos);
			}
			set
			{
				*(Vector3*)Native.ReferenceToPointer(ref _AttackPos) = value;
			}
		}

		[Networked]
		[NetworkedWeaved(36, 1)]
		public unsafe float Timer
		{
			readonly get
			{
				return *(float*)Native.ReferenceToPointer(ref _Timer);
			}
			set
			{
				*(float*)Native.ReferenceToPointer(ref _Timer) = value;
			}
		}

		[field: FieldOffset(148)]
		public NetworkBool FloorEnabled { get; set; }

		[field: FieldOffset(152)]
		public NetworkBool PortalEnabled { get; set; }

		[field: FieldOffset(156)]
		public NetworkBool FreezePlayer { get; set; }

		[Networked]
		[NetworkedWeaved(40, 1)]
		public unsafe float Alpha
		{
			readonly get
			{
				return *(float*)Native.ReferenceToPointer(ref _Alpha);
			}
			set
			{
				*(float*)Native.ReferenceToPointer(ref _Alpha) = value;
			}
		}

		[field: FieldOffset(164)]
		public EStates State { get; set; }

		public MonkeyeAI_RepStateData(string id, Vector3 atPos, float timer, bool floorOn, bool portalOn, bool freezePlayer, float alpha, EStates state)
		{
			UserId = id;
			AttackPos = atPos;
			Timer = timer;
			FloorEnabled = floorOn;
			PortalEnabled = portalOn;
			FreezePlayer = freezePlayer;
			Alpha = alpha;
			State = state;
		}
	}

	public EStates state;

	public string userId;

	public Vector3 attackPos;

	public float timer;

	public bool floorEnabled;

	public bool portalEnabled;

	public bool freezePlayer;

	public float alpha;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 42)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private MonkeyeAI_RepStateData _Data;

	[Networked]
	[NetworkedWeaved(0, 42)]
	private unsafe MonkeyeAI_RepStateData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing MonkeyeAI_ReplState.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(MonkeyeAI_RepStateData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing MonkeyeAI_ReplState.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(MonkeyeAI_RepStateData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	public override void WriteDataFusion()
	{
		MonkeyeAI_RepStateData data = new MonkeyeAI_RepStateData(userId, attackPos, timer, floorEnabled, portalEnabled, freezePlayer, alpha, state);
		Data = data;
	}

	public override void ReadDataFusion()
	{
		userId = Data.UserId.Value;
		attackPos = Data.AttackPos;
		timer = Data.Timer;
		floorEnabled = Data.FloorEnabled;
		portalEnabled = Data.PortalEnabled;
		freezePlayer = Data.FreezePlayer;
		alpha = Data.Alpha;
		state = Data.State;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(userId);
		stream.SendNext(attackPos);
		stream.SendNext(timer);
		stream.SendNext(floorEnabled);
		stream.SendNext(portalEnabled);
		stream.SendNext(freezePlayer);
		stream.SendNext(alpha);
		stream.SendNext(state);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.photonView.Owner != null && info.Sender.ActorNumber == info.photonView.Owner.ActorNumber)
		{
			userId = (string)stream.ReceiveNext();
			attackPos.SetValueSafe((Vector3)stream.ReceiveNext());
			timer = (float)stream.ReceiveNext();
			floorEnabled = (bool)stream.ReceiveNext();
			portalEnabled = (bool)stream.ReceiveNext();
			freezePlayer = (bool)stream.ReceiveNext();
			alpha = ((float)stream.ReceiveNext()).ClampSafe(0f, 1f);
			state = (EStates)stream.ReceiveNext();
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
