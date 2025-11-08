using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevWatch : MonoBehaviour
{
	public DevWatchButton SearchButton;

	public GameObject Panel1;

	public GameObject Panel2;

	public DevWatchSelectableItem SelectableItemPrefab;

	public List<DevWatchSelectableItem> Items;

	public Transform RayCastStartPos;

	public Transform RayCastDirection;

	public Transform ItemsFoundContainer;

	public Button TakeOwnershipButton;

	public Button DestroyObjectButton;

	public List<NetworkObject> FoundNetworkObjects = new List<NetworkObject>();

	public TextMeshProUGUI SelectedItemName;

	public DevWatchSelectableItem SelectedItem;

	private void Awake()
	{
		SearchButton.SearchEvent.AddListener(SearchItems);
		TakeOwnershipButton.onClick.AddListener(TakeOwneshipOfItem);
		DestroyObjectButton.onClick.AddListener(TryDestroyItem);
	}

	public void SearchItems()
	{
		FoundNetworkObjects.Clear();
		RaycastHit[] array = Physics.SphereCastAll(new Ray(RayCastStartPos.position, RayCastDirection.position - RayCastStartPos.position), 0.3f, 100f);
		if (array.Length == 0)
		{
			return;
		}
		RaycastHit[] array2 = array;
		foreach (RaycastHit raycastHit in array2)
		{
			if (raycastHit.collider.gameObject.TryGetComponent<NetworkObject>(out var component))
			{
				FoundNetworkObjects.Add(component);
			}
		}
	}

	public void Cleanup()
	{
		FoundNetworkObjects.Clear();
		if (Items.Count > 0)
		{
			for (int num = Items.Count - 1; num >= 0; num--)
			{
				Object.Destroy(Items[num]);
			}
		}
		Items.Clear();
		Panel1.SetActive(value: true);
		Panel2.SetActive(value: false);
	}

	public void ItemSelected(DevWatchSelectableItem item)
	{
		Panel1.SetActive(value: false);
		Panel2.SetActive(value: true);
		SelectedItem = item;
		SelectedItemName.text = item.ItemName.text;
	}

	public void TryDestroyItem()
	{
	}

	public void TakeOwneshipOfItem()
	{
	}
}
