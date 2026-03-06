using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneConditionalVisibility : MonoBehaviour
{
	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private GTZone[] zones;

	[SerializeField]
	private bool invisibleWhileLoaded;

	[SerializeField]
	private bool renderersOnly;

	private List<Renderer> renderers;

	private void Awake()
	{
		if (renderersOnly)
		{
			renderers = new List<Renderer>(32);
			GetComponentsInChildren(includeInactive: false, renderers);
		}
	}

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
		bool flag = ((zones == null || zones.Length == 0) ? ZoneManagement.IsInZone(zone) : InAnyZone());
		if (invisibleWhileLoaded)
		{
			if (renderersOnly)
			{
				for (int i = 0; i < renderers.Count; i++)
				{
					if (renderers[i] != null)
					{
						renderers[i].enabled = !flag;
					}
				}
			}
			else
			{
				base.gameObject.SetActive(!flag);
			}
		}
		else if (renderersOnly)
		{
			for (int j = 0; j < renderers.Count; j++)
			{
				if (renderers[j] != null)
				{
					renderers[j].enabled = flag;
				}
			}
		}
		else
		{
			base.gameObject.SetActive(flag);
		}
	}

	private bool InAnyZone()
	{
		for (int i = 0; i < zones.Length; i++)
		{
			if (ZoneManagement.IsInZone(zones[i]))
			{
				return true;
			}
		}
		return false;
	}
}
