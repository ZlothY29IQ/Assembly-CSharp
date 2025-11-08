using System;
using System.Collections.Generic;
using UnityEngine;

public class BuilderZoneRenderers : MonoBehaviour
{
	public List<Renderer> renderers;

	public List<Canvas> canvases;

	public List<GameObject> rootObjects;

	private bool inBuilderZone;

	private List<Renderer> allRenderers = new List<Renderer>(200);

	private void Start()
	{
		allRenderers.Clear();
		allRenderers.AddRange(renderers);
		foreach (GameObject rootObject in rootObjects)
		{
			allRenderers.AddRange(rootObject.GetComponentsInChildren<Renderer>(includeInactive: true));
		}
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		inBuilderZone = true;
		OnZoneChanged();
	}

	private void OnDestroy()
	{
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(GTZone.monkeBlocks);
		if (flag && !inBuilderZone)
		{
			inBuilderZone = flag;
			foreach (Renderer allRenderer in allRenderers)
			{
				allRenderer.enabled = true;
			}
			{
				foreach (Canvas canvase in canvases)
				{
					canvase.enabled = true;
				}
				return;
			}
		}
		if (flag || !inBuilderZone)
		{
			return;
		}
		inBuilderZone = flag;
		foreach (Renderer allRenderer2 in allRenderers)
		{
			allRenderer2.enabled = false;
		}
		foreach (Canvas canvase2 in canvases)
		{
			canvase2.enabled = false;
		}
	}
}
