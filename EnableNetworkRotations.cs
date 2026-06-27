using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

public class EnableNetworkRotations : MonoBehaviour
{
	private static HashSet<EnableNetworkRotations> m_enabledRotationEnablers = new HashSet<EnableNetworkRotations>(2);

	private void OnEnable()
	{
		m_enabledRotationEnablers.Add(this);
		if (m_enabledRotationEnablers.Count == 1)
		{
			GTPlayerTransform.EnableNetworkRotations();
		}
	}

	private void OnDisable()
	{
		m_enabledRotationEnablers.Remove(this);
		if (m_enabledRotationEnablers.Count == 0)
		{
			GTPlayerTransform.DisableNetworkRotations();
		}
	}
}
