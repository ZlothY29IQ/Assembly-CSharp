using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRVendingMachine : MonoBehaviour
{
	[Serializable]
	public struct VendingEntry
	{
		public Transform transportVisual;

		public GameEntity entityPrefab;

		public string itemName;

		private int entityTypeId;

		private bool entityTypeIdSet;

		public int GetEntityTypeId()
		{
			if (!entityTypeIdSet)
			{
				entityTypeId = entityPrefab.gameObject.name.GetStaticHash();
				entityTypeIdSet = true;
			}
			return entityTypeId;
		}
	}

	[SerializeField]
	private Transform horizontalTransport;

	[SerializeField]
	private Transform verticalTransport;

	[SerializeField]
	private Transform horizontalMin;

	[SerializeField]
	private Transform horizontalMax;

	[SerializeField]
	private Transform verticalMin;

	[SerializeField]
	private Transform verticalMax;

	[SerializeField]
	private Transform depositLocation;

	[SerializeField]
	private Transform itemSpawnLocation;

	[SerializeField]
	private TMP_Text cardDisplayText;

	[SerializeField]
	private int horizontalSteps = 4;

	[SerializeField]
	private int verticalSteps = 3;

	[SerializeField]
	private float horizontalSpeed = 0.25f;

	[SerializeField]
	private float verticalSpeed = 0.25f;

	[SerializeField]
	private bool debugUnlimitedPurchasing;

	[SerializeField]
	private List<VendingEntry> vendingEntries = new List<VendingEntry>();

	private int hIndex;

	private int vIndex;

	private bool currentlyVending;

	private int vendingIndex;

	private Coroutine vendingCoroutine;

	public int VendingMachineId;

	private GhostReactor reactor;

	public void Setup(GhostReactor reactor)
	{
		this.reactor = reactor;
	}

	public Transform GetSpawnMarker()
	{
		return itemSpawnLocation;
	}

	public void NavButtonPressedLeft()
	{
		hIndex = Mathf.Max(0, hIndex - 1);
		RefreshCardReaderDisplay();
	}

	public void NavButtonPressedRight()
	{
		hIndex = Mathf.Min(hIndex + 1, horizontalSteps - 1);
		RefreshCardReaderDisplay();
	}

	public void NavButtonPressedUp()
	{
		vIndex = Mathf.Max(0, vIndex - 1);
		RefreshCardReaderDisplay();
	}

	public void NavButtonPressedDown()
	{
		vIndex = Mathf.Min(vIndex + 1, verticalSteps - 1);
		RefreshCardReaderDisplay();
	}

	public void RequestPurchase()
	{
		if (currentlyVending)
		{
			return;
		}
		int num = vIndex * horizontalSteps + hIndex;
		if (num >= 0 && num < vendingEntries.Count)
		{
			vendingIndex = num;
			if (vendingCoroutine != null)
			{
				StopCoroutine(vendingCoroutine);
			}
			vendingCoroutine = StartCoroutine(VendingCoroutine());
		}
	}

	private void RefreshCardReaderDisplay()
	{
		int num = vIndex * horizontalSteps + hIndex;
		if (num >= 0 && num < vendingEntries.Count)
		{
			int entityTypeId = vendingEntries[num].GetEntityTypeId();
			int itemCost = reactor.GetItemCost(entityTypeId);
			cardDisplayText.text = vendingEntries[num].itemName + "\n" + itemCost;
		}
	}

	private void Update()
	{
		if (!currentlyVending)
		{
			MoveTransportToSlot(hIndex, vIndex, horizontalSteps, verticalSteps, horizontalSpeed, verticalSpeed, Time.deltaTime);
		}
	}

	private bool MoveTransportToSlot(int x, int y, int rows, int cols, float xSpeed, float ySpeed, float dt)
	{
		Vector3 vector = Vector3.Lerp(horizontalMin.position, horizontalMax.position, (float)x / (float)(rows - 1));
		Vector3 vector2 = Vector3.Lerp(verticalMin.position, verticalMax.position, (float)y / (float)(cols - 1));
		horizontalTransport.position = Vector3.MoveTowards(horizontalTransport.position, vector, xSpeed * dt);
		verticalTransport.position = Vector3.MoveTowards(verticalTransport.position, vector2, ySpeed * dt);
		float sqrMagnitude = (horizontalTransport.position - vector).sqrMagnitude;
		float sqrMagnitude2 = (verticalTransport.position - vector2).sqrMagnitude;
		if (!(sqrMagnitude > 0.001f))
		{
			return sqrMagnitude2 > 0.001f;
		}
		return true;
	}

	private IEnumerator VendingCoroutine()
	{
		currentlyVending = true;
		while (MoveTransportToSlot(hIndex, vIndex, horizontalSteps, verticalSteps, horizontalSpeed, verticalSpeed, Time.deltaTime))
		{
			yield return null;
		}
		int entityTypeId = vendingEntries[vendingIndex].GetEntityTypeId();
		int itemCost = reactor.GetItemCost(entityTypeId);
		if (debugUnlimitedPurchasing || VRRig.LocalRig.GetComponent<GRPlayer>().ShiftCredits >= itemCost)
		{
			vendingEntries[vendingIndex].transportVisual.gameObject.SetActive(value: true);
			while (MoveTransportToSlot(horizontalSteps - 1, verticalSteps - 1, horizontalSteps, verticalSteps, horizontalSpeed, verticalSpeed, Time.deltaTime))
			{
				yield return null;
			}
			float depositPosSqDist = (horizontalTransport.position - depositLocation.position).sqrMagnitude;
			while (depositPosSqDist > 0.001f)
			{
				horizontalTransport.position = Vector3.MoveTowards(horizontalTransport.position, depositLocation.position, horizontalSpeed * Time.deltaTime);
				depositPosSqDist = (horizontalTransport.position - depositLocation.position).sqrMagnitude;
				yield return null;
			}
			vendingEntries[vendingIndex].transportVisual.gameObject.SetActive(value: false);
			while (MoveTransportToSlot(horizontalSteps - 1, verticalSteps - 1, horizontalSteps, verticalSteps, horizontalSpeed, verticalSpeed, Time.deltaTime))
			{
				yield return null;
			}
		}
		currentlyVending = false;
	}
}
