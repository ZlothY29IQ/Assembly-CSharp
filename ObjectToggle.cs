using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToggle : MonoBehaviour
{
	public List<GameObject> objectsToToggle = new List<GameObject>();

	[SerializeField]
	private bool _ignoreHierarchyState;

	[NonSerialized]
	private bool? _toggled;

	public void Toggle(bool initialState = true)
	{
		if (!_toggled.HasValue)
		{
			if (initialState)
			{
				Enable();
			}
			else
			{
				Disable();
			}
		}
		else if (_toggled.Value)
		{
			Disable();
		}
		else
		{
			Enable();
		}
	}

	public void Enable()
	{
		if (objectsToToggle == null)
		{
			return;
		}
		for (int i = 0; i < objectsToToggle.Count; i++)
		{
			GameObject gameObject = objectsToToggle[i];
			if (!(gameObject == null))
			{
				if (_ignoreHierarchyState)
				{
					gameObject.SetActive(value: true);
				}
				else if (!gameObject.activeInHierarchy)
				{
					gameObject.SetActive(value: true);
				}
			}
		}
		_toggled = true;
	}

	public void Disable()
	{
		if (objectsToToggle == null)
		{
			return;
		}
		for (int i = 0; i < objectsToToggle.Count; i++)
		{
			GameObject gameObject = objectsToToggle[i];
			if (!(gameObject == null))
			{
				if (_ignoreHierarchyState)
				{
					gameObject.SetActive(value: false);
				}
				else if (gameObject.activeInHierarchy)
				{
					gameObject.SetActive(value: false);
				}
			}
		}
		_toggled = false;
	}
}
