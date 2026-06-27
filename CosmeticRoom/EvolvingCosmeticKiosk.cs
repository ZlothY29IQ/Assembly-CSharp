using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaNetworking;
using UnityEngine;

namespace CosmeticRoom;

public class EvolvingCosmeticKiosk : MonoBehaviour
{
	private record CosmeticData
	{
		public EvolvingCosmetic EvolvingCosmetic;

		public string PlayfabId;
	}

	[SerializeField]
	private EvolvingCosmeticKioskButtonSet[] _buttonSets;

	private readonly List<CosmeticData> _cosmetics = new List<CosmeticData>();

	private int _cosmeticIdx;

	public bool Initialized { get; private set; }

	public VRRig VRRig => VRRig.LocalRig;

	public bool CosmeticsListBuilding { get; private set; }

	private void Awake()
	{
		EvolvingCosmeticKioskButtonSet[] buttonSets = _buttonSets;
		for (int i = 0; i < buttonSets.Length; i++)
		{
			buttonSets[i].RegisterKiosk(this);
		}
		Initialized = true;
	}

	private async Task BuildCosmeticsList()
	{
		_cosmetics.Clear();
		UpdateButtonSets();
		CosmeticsListBuilding = true;
		while (CosmeticsController.instance == null || !CosmeticsController.instance.v2_isCosmeticPlayFabCatalogDataLoaded || !CosmeticsV2Spawner_Dirty.isPrepared)
		{
			await Task.Yield();
		}
		CosmeticItemRegistry registry = VRRig.cosmeticsObjectRegistry;
		HashSet<string> loadedCosmetics = new HashSet<string>();
		CosmeticsController.CosmeticItem[] items = CosmeticsController.instance.currentWornSet.items;
		for (int i = 0; i < items.Length; i++)
		{
			CosmeticsController.CosmeticItem item = items[i];
			if (string.IsNullOrEmpty(item.itemName) || item.itemName == "null")
			{
				continue;
			}
			await Task.Yield();
			CosmeticItemInstance cosmeticItemInstance;
			try
			{
				Debug.Log("Fetching cosmetic " + item.itemName);
				cosmeticItemInstance = registry.Cosmetic(item.itemName);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				continue;
			}
			if (cosmeticItemInstance == null)
			{
				continue;
			}
			foreach (GameObject @object in cosmeticItemInstance.objects)
			{
				EvolvingCosmetic component = @object.GetComponent<EvolvingCosmetic>();
				if ((object)component != null && loadedCosmetics.Add(item.itemName))
				{
					_cosmetics.Add(new CosmeticData
					{
						EvolvingCosmetic = component,
						PlayfabId = item.itemName
					});
				}
			}
		}
		Debug.Log($"EvolvingCosmetics loaded ({_cosmetics.Count} found).");
		CosmeticsListBuilding = false;
		ResetButtonSets();
		UpdateButtonSets();
	}

	private void ResetButtonSets()
	{
		_cosmeticIdx = 0;
		EvolvingCosmeticKioskButtonSet[] buttonSets = _buttonSets;
		for (int i = 0; i < buttonSets.Length; i++)
		{
			buttonSets[i].Reset();
		}
	}

	private void UpdateButtonSets()
	{
		for (int i = 0; i < _buttonSets.Length; i++)
		{
			int num = _cosmeticIdx + i;
			if (num >= _cosmetics.Count)
			{
				_buttonSets[i].Reset();
				continue;
			}
			CosmeticData cosmeticData = _cosmetics[num];
			_buttonSets[i].SetCosmetic(cosmeticData.PlayfabId, cosmeticData.EvolvingCosmetic);
		}
	}

	public async void OnHandScanned(NetPlayer player)
	{
		if (player.IsLocal)
		{
			await BuildCosmeticsList();
		}
	}

	public void ScrollForward()
	{
		Scroll(1);
	}

	public void ScrollBackward()
	{
		Scroll(-1);
	}

	private void Scroll(int direction)
	{
		_cosmeticIdx = Math.Clamp(_cosmeticIdx + direction, 0, _cosmetics.Count - 1);
		UpdateButtonSets();
	}
}
