using GorillaExtensions;
using UnityEngine;

namespace Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class RigOwnedRigidbodyView : MonoBehaviourPun, IPunObservable
{
	private float m_Distance;

	private float m_Angle;

	private Rigidbody m_Body;

	private Vector3 m_NetworkPosition;

	private Quaternion m_NetworkRotation;

	public bool m_SynchronizeVelocity = true;

	public bool m_SynchronizeAngularVelocity;

	public bool m_TeleportEnabled;

	public float m_TeleportIfDistanceGreaterThan = 3f;

	public bool IsMine { get; private set; }

	public void SetIsMine(bool isMine)
	{
		IsMine = isMine;
	}

	public void Awake()
	{
		m_Body = GetComponent<Rigidbody>();
		m_NetworkPosition = default(Vector3);
		m_NetworkRotation = default(Quaternion);
	}

	public void FixedUpdate()
	{
		if (!IsMine)
		{
			m_Body.position = Vector3.MoveTowards(m_Body.position, m_NetworkPosition, m_Distance * (1f / (float)PhotonNetwork.SerializationRate));
			m_Body.rotation = Quaternion.RotateTowards(m_Body.rotation, m_NetworkRotation, m_Angle * (1f / (float)PhotonNetwork.SerializationRate));
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != info.photonView.Owner)
		{
			return;
		}
		try
		{
			if (stream.IsWriting)
			{
				stream.SendNext(m_Body.position);
				stream.SendNext(m_Body.rotation);
				if (m_SynchronizeVelocity)
				{
					stream.SendNext(m_Body.linearVelocity);
				}
				if (m_SynchronizeAngularVelocity)
				{
					stream.SendNext(m_Body.angularVelocity);
				}
				stream.SendNext(m_Body.IsSleeping());
				return;
			}
			m_NetworkPosition.SetValueSafe((Vector3)stream.ReceiveNext());
			m_NetworkRotation.SetValueSafe((Quaternion)stream.ReceiveNext());
			if (m_TeleportEnabled && Vector3.Distance(m_Body.position, m_NetworkPosition) > m_TeleportIfDistanceGreaterThan)
			{
				m_Body.position = m_NetworkPosition;
			}
			if (m_SynchronizeVelocity || m_SynchronizeAngularVelocity)
			{
				float num = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
				if (m_SynchronizeVelocity)
				{
					Vector3 v = (Vector3)stream.ReceiveNext();
					if (!v.IsValid(10000f))
					{
						v = Vector3.zero;
					}
					if (!m_Body.isKinematic)
					{
						m_Body.linearVelocity = v;
					}
					m_NetworkPosition += m_Body.linearVelocity * num;
					m_Distance = Vector3.Distance(m_Body.position, m_NetworkPosition);
				}
				if (m_SynchronizeAngularVelocity)
				{
					Vector3 v2 = (Vector3)stream.ReceiveNext();
					if (!v2.IsValid(10000f))
					{
						v2 = Vector3.zero;
					}
					m_Body.angularVelocity = v2;
					m_NetworkRotation = Quaternion.Euler(m_Body.angularVelocity * num) * m_NetworkRotation;
					m_Angle = Quaternion.Angle(m_Body.rotation, m_NetworkRotation);
				}
			}
			if ((bool)stream.ReceiveNext())
			{
				m_Body.Sleep();
			}
		}
		catch
		{
		}
	}
}
