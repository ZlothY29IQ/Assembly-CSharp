using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ActivateGO : MonoBehaviour
{
	[SerializeField]
	private GameObject targetGO;

	[SerializeField]
	private PlayerPrefFlags.Flag flag;

	[SerializeField]
	private int flashes;

	[SerializeField]
	private LayerMask layerMask;

	private bool active;

	private bool flashing;

	private void OnEnable()
	{
		active = PlayerPrefFlags.Check(flag);
		SetGOsActive(0);
		PlayerPrefFlags.OnFlagChange = (Action<PlayerPrefFlags.Flag, bool>)Delegate.Combine(PlayerPrefFlags.OnFlagChange, new Action<PlayerPrefFlags.Flag, bool>(OnFlagChange));
	}

	private void OnDisable()
	{
		PlayerPrefFlags.OnFlagChange = (Action<PlayerPrefFlags.Flag, bool>)Delegate.Remove(PlayerPrefFlags.OnFlagChange, new Action<PlayerPrefFlags.Flag, bool>(OnFlagChange));
	}

	private void OnDestroy()
	{
		PlayerPrefFlags.OnFlagChange = (Action<PlayerPrefFlags.Flag, bool>)Delegate.Remove(PlayerPrefFlags.OnFlagChange, new Action<PlayerPrefFlags.Flag, bool>(OnFlagChange));
	}

	public void OnFlagChange(PlayerPrefFlags.Flag f, bool value)
	{
		if (f == flag)
		{
			active = value;
			SetGOsActive(flashes);
		}
	}

	private async void SetGOsActive(int fls)
	{
		if (flashing)
		{
			return;
		}
		List<Renderer> renderers = new List<Renderer>();
		renderers.AddRange(targetGO.GetComponentsInChildren<MeshRenderer>(includeInactive: true));
		renderers.AddRange(targetGO.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true));
		FirstPersonToggleOverride[] componentsInChildren = targetGO.GetComponentsInChildren<FirstPersonToggleOverride>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].Toggle && !renderers.Contains(componentsInChildren[i].Renderer))
			{
				renderers.Add(componentsInChildren[i].Renderer);
			}
			else if (!componentsInChildren[i].Toggle && renderers.Contains(componentsInChildren[i].Renderer))
			{
				renderers.Remove(componentsInChildren[i].Renderer);
			}
		}
		for (int j = 0; j < fls; j++)
		{
			flashing = true;
			toggle(renderers, active);
			await Task.Delay(150);
			toggle(renderers, !active);
			await Task.Delay(100);
		}
		toggle(renderers, active);
		flashing = false;
	}

	private void toggle(List<Renderer> renderers, bool state)
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			if ((layerMask.value & (1 << renderers[i].gameObject.layer)) != 0)
			{
				renderers[i].forceRenderingOff = !state;
			}
		}
	}
}
