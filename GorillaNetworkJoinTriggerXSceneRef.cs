using GorillaNetworking;
using UnityEngine;
using UnityEngine.Serialization;

public class GorillaNetworkJoinTriggerXSceneRef : MonoBehaviour
{
	[FormerlySerializedAs("joinTriggerRef")]
	[SerializeField]
	private XSceneRef m_joinTriggerRef;

	private GorillaNetworkJoinTrigger _joinTrigger;

	protected void Awake()
	{
		if (!m_joinTriggerRef.TryResolve(out _joinTrigger) || !(_joinTrigger != null))
		{
			m_joinTriggerRef.AddCallbackOnLoad(_OnTargetSceneLoaded);
		}
	}

	protected void OnDestroy()
	{
		m_joinTriggerRef.RemoveCallbackOnLoad(_OnTargetSceneLoaded);
	}

	private void _OnTargetSceneLoaded()
	{
		m_joinTriggerRef.TryResolve(out _joinTrigger);
	}

	public void SubsPublicJoin()
	{
		if (_joinTrigger != null)
		{
			_joinTrigger.SubsPublicJoin();
		}
	}
}
