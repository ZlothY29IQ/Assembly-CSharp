using System;
using System.Runtime.InteropServices;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(15)]
internal class GorillaNetworkTransform : NetworkComponent, ITickSystemTick
{
	[StructLayout(LayoutKind.Explicit, Size = 60)]
	[NetworkStructWeaved(15)]
	private struct NetTransformData : INetworkStruct
	{
		[FieldOffset(0)]
		public Vector3 position;

		[FieldOffset(12)]
		public Vector3 velocity;

		[FieldOffset(24)]
		public Quaternion rotation;

		[FieldOffset(40)]
		public Vector3 scale;

		[FieldOffset(52)]
		public double SentTime;
	}

	[Tooltip("Indicates if localPosition and localRotation should be used. Scale ignores this setting, and always uses localScale to avoid issues with lossyScale.")]
	public bool m_UseLocal;

	[SerializeField]
	private bool respectOwnership;

	[SerializeField]
	private bool clampDistanceFromSpawn = true;

	[SerializeField]
	private float maxDistance = 100f;

	private float maxDistanceSquare;

	[SerializeField]
	private bool clampToSpawn = true;

	[Tooltip("Use this if clampToSpawn is false, to set the center point to check the synced position against")]
	[SerializeField]
	private Vector3 clampOriginPoint;

	public bool m_SynchronizePosition = true;

	public bool m_SynchronizeRotation = true;

	public bool m_SynchronizeScale;

	private float m_Distance;

	private float m_Angle;

	private Vector3 m_Velocity;

	private Vector3 m_NetworkPosition;

	private Vector3 m_StoredPosition;

	private Vector3 m_NetworkScale;

	private Quaternion m_NetworkRotation;

	private bool m_firstTake;

	[WeaverGenerated]
	[DefaultForProperty("data", 0, 15)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private NetTransformData _data;

	public bool RespectOwnership => respectOwnership;

	public bool TickRunning { get; set; }

	[Networked]
	[NetworkedWeaved(0, 15)]
	private unsafe NetTransformData data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GorillaNetworkTransform.data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(NetTransformData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GorillaNetworkTransform.data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(NetTransformData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	public new void Awake()
	{
		m_StoredPosition = base.transform.localPosition;
		m_NetworkPosition = Vector3.zero;
		m_NetworkScale = Vector3.zero;
		m_NetworkRotation = Quaternion.identity;
		maxDistanceSquare = maxDistance * maxDistance;
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		m_firstTake = true;
		if (clampToSpawn)
		{
			clampOriginPoint = (m_UseLocal ? base.transform.localPosition : base.transform.position);
		}
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		if (!base.IsLocallyOwned)
		{
			if (m_UseLocal)
			{
				base.transform.SetLocalPositionAndRotation(Vector3.MoveTowards(base.transform.localPosition, m_NetworkPosition, m_Distance * Time.deltaTime * (float)NetworkSystem.Instance.TickRate), Quaternion.RotateTowards(base.transform.localRotation, m_NetworkRotation, m_Angle * Time.deltaTime * (float)NetworkSystem.Instance.TickRate));
			}
			else
			{
				base.transform.SetPositionAndRotation(Vector3.MoveTowards(base.transform.position, m_NetworkPosition, m_Distance * Time.deltaTime * (float)NetworkSystem.Instance.TickRate), Quaternion.RotateTowards(base.transform.rotation, m_NetworkRotation, m_Angle * Time.deltaTime * (float)NetworkSystem.Instance.TickRate));
			}
		}
	}

	public override void WriteDataFusion()
	{
		NetTransformData netTransformData = SharedWrite();
		double sentTime = (double)(uint)NetworkSystem.Instance.SimTick / 1000.0;
		netTransformData.SentTime = sentTime;
		data = netTransformData;
	}

	public override void ReadDataFusion()
	{
		SharedRead(data);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!respectOwnership || player == base.Owner)
		{
			NetTransformData netTransformData = SharedWrite();
			if (m_SynchronizePosition)
			{
				stream.SendNext(netTransformData.position);
				stream.SendNext(netTransformData.velocity);
			}
			if (m_SynchronizeRotation)
			{
				stream.SendNext(netTransformData.rotation);
			}
			if (m_SynchronizeScale)
			{
				stream.SendNext(netTransformData.scale);
			}
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!respectOwnership || player == base.Owner)
		{
			NetTransformData netTransformData = default(NetTransformData);
			if (m_SynchronizePosition)
			{
				netTransformData.position = (Vector3)stream.ReceiveNext();
				netTransformData.velocity = (Vector3)stream.ReceiveNext();
			}
			if (m_SynchronizeRotation)
			{
				netTransformData.rotation = (Quaternion)stream.ReceiveNext();
			}
			if (m_SynchronizeScale)
			{
				netTransformData.scale = (Vector3)stream.ReceiveNext();
			}
			netTransformData.SentTime = (float)info.SentServerTime;
			SharedRead(netTransformData);
		}
	}

	private void SharedRead(NetTransformData data)
	{
		if (m_SynchronizePosition)
		{
			m_NetworkPosition.SetValueSafe(in data.position);
			m_Velocity.SetValueSafe(in data.velocity);
			if (clampDistanceFromSpawn && Vector3.SqrMagnitude(clampOriginPoint - m_NetworkPosition) > maxDistanceSquare)
			{
				m_NetworkPosition = clampOriginPoint + m_Velocity.normalized * maxDistance;
				m_Velocity = Vector3.zero;
			}
			if (m_firstTake)
			{
				if (m_UseLocal)
				{
					base.transform.localPosition = m_NetworkPosition;
				}
				else
				{
					base.transform.position = m_NetworkPosition;
				}
				m_Distance = 0f;
			}
			else
			{
				float num = Mathf.Abs((float)(NetworkSystem.Instance.SimTime - data.SentTime));
				m_NetworkPosition += m_Velocity * num;
				if (m_UseLocal)
				{
					m_Distance = Vector3.Distance(base.transform.localPosition, m_NetworkPosition);
				}
				else
				{
					m_Distance = Vector3.Distance(base.transform.position, m_NetworkPosition);
				}
			}
		}
		if (m_SynchronizeRotation)
		{
			m_NetworkRotation.SetValueSafe(in data.rotation);
			if (m_firstTake)
			{
				m_Angle = 0f;
				if (m_UseLocal)
				{
					base.transform.localRotation = m_NetworkRotation;
				}
				else
				{
					base.transform.rotation = m_NetworkRotation;
				}
			}
			else if (m_UseLocal)
			{
				m_Angle = Quaternion.Angle(base.transform.localRotation, m_NetworkRotation);
			}
			else
			{
				m_Angle = Quaternion.Angle(base.transform.rotation, m_NetworkRotation);
			}
		}
		if (m_SynchronizeScale)
		{
			m_NetworkScale.SetValueSafe(in data.scale);
			base.transform.localScale = m_NetworkScale;
		}
		if (m_firstTake)
		{
			m_firstTake = false;
		}
	}

	private NetTransformData SharedWrite()
	{
		NetTransformData result = default(NetTransformData);
		if (m_SynchronizePosition)
		{
			if (m_UseLocal)
			{
				m_Velocity = base.transform.localPosition - m_StoredPosition;
				m_StoredPosition = base.transform.localPosition;
				result.position = base.transform.localPosition;
				result.velocity = m_Velocity;
			}
			else
			{
				m_Velocity = base.transform.position - m_StoredPosition;
				m_StoredPosition = base.transform.position;
				result.position = base.transform.position;
				result.velocity = m_Velocity;
			}
		}
		if (m_SynchronizeRotation)
		{
			if (m_UseLocal)
			{
				result.rotation = base.transform.localRotation;
			}
			else
			{
				result.rotation = base.transform.rotation;
			}
		}
		if (m_SynchronizeScale)
		{
			result.scale = base.transform.localScale;
		}
		return result;
	}

	public void GTAddition_DoTeleport()
	{
		m_firstTake = true;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		data = _data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_data = data;
	}
}
