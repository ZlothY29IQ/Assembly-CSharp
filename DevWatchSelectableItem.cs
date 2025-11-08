using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevWatchSelectableItem : MonoBehaviour
{
	public Button Button;

	public TextMeshProUGUI ItemName;

	public NetworkObject SelectedObject;

	public Action<string, NetworkObject> OnSelected;

	public void Init(NetworkObject obj)
	{
		SelectedObject = obj;
		ItemName.text = obj.name;
		Button.onClick.AddListener(delegate
		{
			OnSelected(ItemName.text, SelectedObject);
		});
	}
}
