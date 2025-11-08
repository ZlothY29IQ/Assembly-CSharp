using GorillaExtensions;
using UnityEngine;

public class AutoSyncTransforms : MonoBehaviour
{
	[SerializeField]
	private Transform m_transform;

	[SerializeField]
	private Rigidbody m_rigidbody;

	private bool clean;

	public Transform TargetTransform => m_transform;

	public Rigidbody TargetRigidbody => m_rigidbody;

	private void Awake()
	{
		if (m_transform.IsNull())
		{
			m_transform = base.transform;
		}
		if (m_rigidbody.IsNull())
		{
			m_rigidbody = GetComponent<Rigidbody>();
		}
		if (m_transform.IsNull() || m_rigidbody.IsNull())
		{
			base.enabled = false;
			Debug.LogError("AutoSyncTransforms: Rigidbody or Transform is null, disabling!! Please add the missing reference or component", this);
		}
		else
		{
			clean = true;
		}
	}

	private void OnEnable()
	{
		if (clean)
		{
			PostVRRigPhysicsSynch.AddSyncTarget(this);
		}
	}

	private void OnDisable()
	{
		if (clean)
		{
			PostVRRigPhysicsSynch.RemoveSyncTarget(this);
		}
	}
}
