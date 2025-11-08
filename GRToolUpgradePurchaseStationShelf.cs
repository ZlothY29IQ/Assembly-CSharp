using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRToolUpgradePurchaseStationShelf : MonoBehaviour
{
	[Serializable]
	public class GRPurchaseSlot
	{
		public TMP_Text Name;

		public TMP_Text Price;

		public Transform SlotPivot;

		public GRToolProgressionManager.ToolParts PurchaseID;

		public GameEntity ToolEntityPrefab;

		public float RopeYaw;

		public float RopePitch;

		public MeshRenderer BacklightRenderer;

		[NonSerialized]
		public Material overrideMaterial;

		[NonSerialized]
		public bool canAfford;

		[NonSerialized]
		public string purchaseText = "";
	}

	public string ShelfName;

	private List<Material[][]> slotOriginalMaterials = new List<Material[][]>();

	private List<Renderer[]> slotRenderers = new List<Renderer[]>();

	public List<GRPurchaseSlot> gRPurchaseSlots;

	public void Awake()
	{
		for (int i = 0; i < gRPurchaseSlots.Count; i++)
		{
			Renderer[] componentsInChildren = gRPurchaseSlots[i].SlotPivot.gameObject.GetComponentsInChildren<Renderer>();
			slotRenderers.Add(componentsInChildren);
			Material[][] array = new Material[componentsInChildren.Length][];
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				array[j] = componentsInChildren[j].sharedMaterials;
			}
			slotOriginalMaterials.Add(array);
		}
	}

	public void SetMaterialOverride(int slotID, Material overrideMaterial)
	{
		if (slotID < 0 || slotID >= gRPurchaseSlots.Count || gRPurchaseSlots[slotID].overrideMaterial == overrideMaterial || slotID >= slotRenderers.Count)
		{
			return;
		}
		gRPurchaseSlots[slotID].overrideMaterial = overrideMaterial;
		for (int i = 0; i < slotRenderers[slotID].Length; i++)
		{
			Renderer renderer = slotRenderers[slotID][i];
			if (overrideMaterial == null)
			{
				renderer.materials = slotOriginalMaterials[slotID][i];
				continue;
			}
			Material[] array = new Material[renderer.sharedMaterials.Length];
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = overrideMaterial;
			}
			renderer.materials = array;
		}
	}

	public void SetBacklightStateAndMaterial(int slotID, bool isEnabled, Material materialOverride)
	{
		if (slotID >= 0 && slotID < gRPurchaseSlots.Count && gRPurchaseSlots[slotID].BacklightRenderer != null)
		{
			if (!isEnabled)
			{
				gRPurchaseSlots[slotID].BacklightRenderer.enabled = false;
				return;
			}
			gRPurchaseSlots[slotID].BacklightRenderer.enabled = true;
			gRPurchaseSlots[slotID].BacklightRenderer.sharedMaterial = materialOverride;
		}
	}
}
