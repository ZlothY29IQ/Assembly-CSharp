using System;
using System.Collections;
using UnityEngine;

public class AtticHider : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer AtticRenderer;

	private Coroutine _coroutine;

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
		if (AtticRenderer == null)
		{
			return;
		}
		if (ZoneManagement.instance.IsZoneActive(GTZone.attic))
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			_coroutine = StartCoroutine(WaitForAtticLoad());
		}
		else
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			AtticRenderer.enabled = true;
		}
	}

	private IEnumerator WaitForAtticLoad()
	{
		while (!ZoneManagement.instance.IsSceneLoaded(GTZone.attic))
		{
			yield return new WaitForSeconds(0.2f);
		}
		yield return null;
		AtticRenderer.enabled = false;
		_coroutine = null;
	}
}
