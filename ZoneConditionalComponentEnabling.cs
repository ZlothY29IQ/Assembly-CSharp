using System;
using UnityEngine;

public class ZoneConditionalComponentEnabling : MonoBehaviour
{
	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private bool invisibleWhileLoaded;

	[SerializeField]
	private Behaviour[] components;

	[SerializeField]
	private Renderer[] m_renderers;

	[SerializeField]
	private Collider[] m_colliders;

	private void Start()
	{
		OnZoneChanged();
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	private void OnDestroy()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.IsInZone(zone);
		bool flag2 = (invisibleWhileLoaded ? (!flag) : flag);
		if (components != null)
		{
			for (int i = 0; i < components.Length; i++)
			{
				if (components[i] != null)
				{
					components[i].enabled = flag2;
				}
			}
		}
		if (m_renderers != null)
		{
			for (int j = 0; j < m_renderers.Length; j++)
			{
				if (m_renderers[j] != null)
				{
					m_renderers[j].enabled = flag2;
				}
			}
		}
		if (m_colliders == null)
		{
			return;
		}
		for (int k = 0; k < m_colliders.Length; k++)
		{
			if (m_colliders[k] != null)
			{
				m_colliders[k].enabled = flag2;
			}
		}
	}
}
