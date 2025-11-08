using System.Collections.Generic;
using UnityEngine;

public class BuilderTriggerEnable : MonoBehaviour
{
	public List<GameObject> activateOnEnter;

	public List<GameObject> deactivateOnEnter;

	public List<GameObject> activateOnExit;

	public List<GameObject> deactivateOnExit;

	private void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody == null)
		{
			return;
		}
		VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (component == null || component.OwningNetPlayer == null || !component.OwningNetPlayer.IsLocal)
		{
			return;
		}
		if (activateOnEnter != null)
		{
			for (int i = 0; i < activateOnEnter.Count; i++)
			{
				if (activateOnEnter[i] != null)
				{
					activateOnEnter[i].SetActive(value: true);
				}
			}
		}
		if (deactivateOnEnter == null)
		{
			return;
		}
		for (int j = 0; j < deactivateOnEnter.Count; j++)
		{
			if (deactivateOnEnter[j] != null)
			{
				deactivateOnEnter[j].SetActive(value: false);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.attachedRigidbody == null)
		{
			return;
		}
		VRRig component = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (component == null || component.OwningNetPlayer == null || !component.OwningNetPlayer.IsLocal)
		{
			return;
		}
		if (activateOnExit != null)
		{
			for (int i = 0; i < activateOnExit.Count; i++)
			{
				if (activateOnExit[i] != null)
				{
					activateOnExit[i].SetActive(value: true);
				}
			}
		}
		if (deactivateOnExit == null)
		{
			return;
		}
		for (int j = 0; j < deactivateOnExit.Count; j++)
		{
			if (deactivateOnExit[j] != null)
			{
				deactivateOnExit[j].SetActive(value: false);
			}
		}
	}
}
