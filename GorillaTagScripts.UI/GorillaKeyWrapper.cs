using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.UI;

public class GorillaKeyWrapper<TBinding> : MonoBehaviour where TBinding : Enum
{
	public UnityEvent<TBinding> OnKeyPressed = new UnityEvent<TBinding>();

	public bool defineButtonsManually;

	public List<GorillaKeyButton<TBinding>> buttons = new List<GorillaKeyButton<TBinding>>();

	public void Start()
	{
		if (!defineButtonsManually)
		{
			FindMatchingButtons(base.gameObject);
		}
		else
		{
			if (buttons.Count <= 0)
			{
				return;
			}
			for (int num = buttons.Count - 1; num >= 0; num--)
			{
				if (buttons[num].IsNull())
				{
					buttons.RemoveAt(num);
				}
				else
				{
					buttons[num].OnKeyButtonPressed.AddListener(OnKeyButtonPressed);
				}
			}
		}
	}

	public void OnDestroy()
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i].IsNotNull())
			{
				buttons[i].OnKeyButtonPressed.RemoveListener(OnKeyButtonPressed);
			}
		}
	}

	public void FindMatchingButtons(GameObject obj)
	{
		if (obj.IsNull())
		{
			return;
		}
		for (int i = 0; i < obj.transform.childCount; i++)
		{
			Transform child = obj.transform.GetChild(i);
			if (child.IsNotNull())
			{
				FindMatchingButtons(child.gameObject);
			}
		}
		GorillaKeyButton<TBinding> component = obj.GetComponent<GorillaKeyButton<TBinding>>();
		if (component.IsNotNull() && !buttons.Contains(component))
		{
			buttons.Add(component);
			component.OnKeyButtonPressed.AddListener(OnKeyButtonPressed);
		}
	}

	private void OnKeyButtonPressed(TBinding binding)
	{
		OnKeyPressed?.Invoke(binding);
	}
}
