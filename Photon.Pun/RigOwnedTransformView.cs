using GorillaExtensions;
using UnityEngine;

namespace Photon.Pun;

[HelpURL("https://doc.photonengine.com/en-us/pun/v2/gameplay/synchronization-and-state")]
public class RigOwnedTransformView : MonoBehaviourPun, IPunObservable
{
	private float m_Distance;

	private float m_Angle;

	private Vector3 m_Direction;

	private Vector3 m_NetworkPosition;

	private Vector3 m_StoredPosition;

	private Vector3 m_networkScale;

	private Quaternion m_NetworkRotation;

	public bool m_SynchronizePosition = true;

	public bool m_SynchronizeRotation = true;

	public bool m_SynchronizeScale;

	[Tooltip("Indicates if localPosition and localRotation should be used. Scale ignores this setting, and always uses localScale to avoid issues with lossyScale.")]
	public bool m_UseLocal;

	private bool m_firstTake;

	public bool IsMine { get; private set; }

	public void SetIsMine(bool isMine)
	{
		IsMine = isMine;
	}

	public void Awake()
	{
		m_StoredPosition = base.transform.localPosition;
		m_NetworkPosition = Vector3.zero;
		m_networkScale = Vector3.one;
		m_NetworkRotation = Quaternion.identity;
	}

	private void Reset()
	{
		m_UseLocal = true;
	}

	private void OnEnable()
	{
		m_firstTake = true;
	}

	public void Update()
	{
		Transform transform = base.transform;
		if (!IsMine && IsValid(m_NetworkPosition) && IsValid(m_NetworkRotation))
		{
			if (m_UseLocal)
			{
				transform.localPosition = Vector3.MoveTowards(transform.localPosition, m_NetworkPosition, m_Distance * Time.deltaTime * (float)PhotonNetwork.SerializationRate);
				transform.localRotation = Quaternion.RotateTowards(transform.localRotation, m_NetworkRotation, m_Angle * Time.deltaTime * (float)PhotonNetwork.SerializationRate);
			}
			else
			{
				transform.position = Vector3.MoveTowards(transform.position, m_NetworkPosition, m_Distance * Time.deltaTime * (float)PhotonNetwork.SerializationRate);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, m_NetworkRotation, m_Angle * Time.deltaTime * (float)PhotonNetwork.SerializationRate);
			}
		}
	}

	private bool IsValid(Vector3 v)
	{
		if (!float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y))
		{
			return !float.IsInfinity(v.z);
		}
		return false;
	}

	private bool IsValid(Quaternion q)
	{
		if (!float.IsNaN(q.x) && !float.IsNaN(q.y) && !float.IsNaN(q.z) && !float.IsNaN(q.w) && !float.IsInfinity(q.x) && !float.IsInfinity(q.y) && !float.IsInfinity(q.z))
		{
			return !float.IsInfinity(q.w);
		}
		return false;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != info.photonView.Owner)
		{
			return;
		}
		try
		{
			Transform transform = base.transform;
			if (stream.IsWriting)
			{
				if (m_SynchronizePosition)
				{
					if (m_UseLocal)
					{
						m_Direction = transform.localPosition - m_StoredPosition;
						m_StoredPosition = transform.localPosition;
						stream.SendNext(transform.localPosition);
						stream.SendNext(m_Direction);
					}
					else
					{
						m_Direction = transform.position - m_StoredPosition;
						m_StoredPosition = transform.position;
						stream.SendNext(transform.position);
						stream.SendNext(m_Direction);
					}
				}
				if (m_SynchronizeRotation)
				{
					if (m_UseLocal)
					{
						stream.SendNext(transform.localRotation);
					}
					else
					{
						stream.SendNext(transform.rotation);
					}
				}
				if (m_SynchronizeScale)
				{
					stream.SendNext(transform.localScale);
				}
				return;
			}
			if (m_SynchronizePosition)
			{
				m_NetworkPosition.SetValueSafe((Vector3)stream.ReceiveNext());
				m_Direction.SetValueSafe((Vector3)stream.ReceiveNext());
				if (m_firstTake)
				{
					if (m_UseLocal)
					{
						transform.localPosition = m_NetworkPosition;
					}
					else
					{
						transform.position = m_NetworkPosition;
					}
					m_Distance = 0f;
				}
				else
				{
					float num = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
					m_NetworkPosition += m_Direction * num;
					if (m_UseLocal)
					{
						m_Distance = Vector3.Distance(transform.localPosition, m_NetworkPosition);
					}
					else
					{
						m_Distance = Vector3.Distance(transform.position, m_NetworkPosition);
					}
				}
			}
			if (m_SynchronizeRotation)
			{
				m_NetworkRotation.SetValueSafe((Quaternion)stream.ReceiveNext());
				if (m_firstTake)
				{
					m_Angle = 0f;
					if (m_UseLocal)
					{
						transform.localRotation = m_NetworkRotation;
					}
					else
					{
						transform.rotation = m_NetworkRotation;
					}
				}
				else if (m_UseLocal)
				{
					m_Angle = Quaternion.Angle(transform.localRotation, m_NetworkRotation);
				}
				else
				{
					m_Angle = Quaternion.Angle(transform.rotation, m_NetworkRotation);
				}
			}
			if (m_SynchronizeScale)
			{
				m_networkScale.SetValueSafe((Vector3)stream.ReceiveNext());
				transform.localScale = m_networkScale;
			}
			if (m_firstTake)
			{
				m_firstTake = false;
			}
		}
		catch
		{
		}
	}

	public void GTAddition_DoTeleport()
	{
		m_firstTake = true;
	}
}
