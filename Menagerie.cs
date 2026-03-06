using System;
using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using Newtonsoft.Json;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Menagerie : MonoBehaviour
{
	public class CritterData
	{
		public int critterType;

		public CritterAppearance appearance;

		[NonSerialized]
		public MenagerieCritter instance;

		public CritterConfiguration GetConfiguration()
		{
			return CrittersManager.instance.creatureIndex[critterType];
		}

		public CritterData()
		{
		}

		public CritterData(CritterConfiguration config, CritterAppearance appearance)
		{
			critterType = CrittersManager.instance.creatureIndex.critterTypes.IndexOf(config);
			this.appearance = appearance;
		}

		public CritterData(int critterType, CritterAppearance appearance)
		{
			this.critterType = critterType;
			this.appearance = appearance;
		}

		public CritterData(CritterVisuals visuals)
		{
			critterType = visuals.critterType;
			appearance = visuals.Appearance;
		}

		public CritterData(CritterData source)
		{
			critterType = source.critterType;
			appearance = source.appearance;
		}

		public override string ToString()
		{
			return $"{critterType} {appearance} [instance]";
		}
	}

	[Serializable]
	public class CritterSaveData
	{
		public List<CritterData> newCritters = new List<CritterData>();

		public Dictionary<int, CritterData> collectedCritters = new Dictionary<int, CritterData>();

		public int donatedCritterCount;

		public int favoriteCritter = -1;

		public void Clear()
		{
			newCritters.Clear();
			collectedCritters.Clear();
			donatedCritterCount = 0;
			favoriteCritter = -1;
		}
	}

	[FormerlySerializedAs("creatureIndex")]
	public CritterIndex critterIndex;

	public MenagerieCritter prefab;

	private List<MenagerieCritter> _critters = new List<MenagerieCritter>();

	private CritterSaveData _savedCritters = new CritterSaveData();

	public MenagerieSlot[] collection;

	public MenagerieSlot[] newCritterPen;

	public MenagerieSlot favoriteCritterSlot;

	private int _collectionPageIndex;

	private int _totalPages;

	public MenagerieDepositBox DonationBox;

	public MenagerieDepositBox FavoriteBox;

	public MenagerieDepositBox CollectionBox;

	public TextMeshPro donationCounter;

	public string DonationText = "DONATED:{0}";

	private const string CrittersSavePrefsKey = "_SavedCritters";

	private void Start()
	{
		CrittersCageDeposit[] array = UnityEngine.Object.FindObjectsByType<CrittersCageDeposit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnDepositCritter += OnDepositCritter;
		}
		CrittersManager.CheckInitialize();
		_totalPages = critterIndex.critterTypes.Count / collection.Length + ((critterIndex.critterTypes.Count % collection.Length != 0) ? 1 : 0);
		Load();
		MenagerieDepositBox donationBox = DonationBox;
		donationBox.OnCritterInserted = (Action<MenagerieCritter>)Delegate.Combine(donationBox.OnCritterInserted, new Action<MenagerieCritter>(CritterDepositedInDonationBox));
		MenagerieDepositBox favoriteBox = FavoriteBox;
		favoriteBox.OnCritterInserted = (Action<MenagerieCritter>)Delegate.Combine(favoriteBox.OnCritterInserted, new Action<MenagerieCritter>(CritterDepositedInFavoriteBox));
		MenagerieDepositBox collectionBox = CollectionBox;
		collectionBox.OnCritterInserted = (Action<MenagerieCritter>)Delegate.Combine(collectionBox.OnCritterInserted, new Action<MenagerieCritter>(CritterDepositedInCollectionBox));
	}

	private void CritterDepositedInDonationBox(MenagerieCritter critter)
	{
		if (Enumerable.Contains(newCritterPen, critter.Slot))
		{
			critter.currentState = MenagerieCritter.MenagerieCritterState.Donating;
			DonateCritter(critter.CritterData);
			_savedCritters.newCritters.Remove(critter.CritterData);
			DespawnCritterFromSlot(critter.Slot);
			Save();
			PlayerGameEvents.CritterEvent("Donate" + critterIndex[critter.CritterData.critterType].critterName);
		}
	}

	private void CritterDepositedInFavoriteBox(MenagerieCritter critter)
	{
		if (Enumerable.Contains(collection, critter.Slot))
		{
			_savedCritters.favoriteCritter = critter.CritterData.critterType;
			Save();
			UpdateFavoriteCritter();
			PlayerGameEvents.CritterEvent("Favorite" + critterIndex[critter.CritterData.critterType].critterName);
		}
	}

	private void CritterDepositedInCollectionBox(MenagerieCritter critter)
	{
		if (Enumerable.Contains(newCritterPen, critter.Slot))
		{
			AddCritterToCollection(critter.CritterData);
			_savedCritters.newCritters.Remove(critter.CritterData);
			DespawnCritterFromSlot(critter.Slot);
			Save();
			UpdateFavoriteCritter();
			PlayerGameEvents.CritterEvent("Collect" + critterIndex[critter.CritterData.critterType].critterName);
		}
	}

	private void OnDepositCritter(CritterData depositedCritter, int playerID)
	{
		try
		{
			if (playerID == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				AddCritterToNewCritterPen(depositedCritter);
				Save();
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void AddCritterToNewCritterPen(CritterData critterData)
	{
		if (_savedCritters.newCritters.Count < newCritterPen.Length)
		{
			MenagerieSlot[] array = newCritterPen;
			foreach (MenagerieSlot menagerieSlot in array)
			{
				if (!menagerieSlot.critter)
				{
					SpawnCritterInSlot(menagerieSlot, critterData);
					_savedCritters.newCritters.Add(critterData);
					return;
				}
			}
		}
		DonateCritter(critterData);
		Save();
	}

	private void AddCritterToCollection(CritterData critterData)
	{
		if (_savedCritters.collectedCritters.TryGetValue(critterData.critterType, out var value))
		{
			DonateCritter(value);
		}
		_savedCritters.collectedCritters[critterData.critterType] = critterData;
		SpawnCollectionCritterIfShowing(critterData);
	}

	private void DonateCritter(CritterData critterData)
	{
		_savedCritters.donatedCritterCount++;
		donationCounter.SetText(string.Format(DonationText, _savedCritters.donatedCritterCount));
	}

	private void SpawnCritterInSlot(MenagerieSlot slot, CritterData critterData)
	{
		if (!slot.IsNull() && critterData != null)
		{
			DespawnCritterFromSlot(slot);
			MenagerieCritter menagerieCritter = UnityEngine.Object.Instantiate(prefab, slot.critterMountPoint);
			menagerieCritter.Slot = slot;
			menagerieCritter.ApplyCritterData(critterData);
			_critters.Add(menagerieCritter);
			if ((bool)slot.label)
			{
				slot.label.text = critterIndex[critterData.critterType].critterName;
			}
		}
	}

	private void SpawnCollectionCritterIfShowing(CritterData critter)
	{
		int num = critter.critterType - _collectionPageIndex * collection.Length;
		if (num >= 0 && num < collection.Length)
		{
			SpawnCritterInSlot(collection[num], critter);
		}
	}

	private void UpdateMenagerie()
	{
		UpdateNewCritterPen();
		UpdateCollection();
		UpdateFavoriteCritter();
		donationCounter.SetText(string.Format(DonationText, _savedCritters.donatedCritterCount));
	}

	private void UpdateNewCritterPen()
	{
		for (int i = 0; i < newCritterPen.Length; i++)
		{
			if (i < _savedCritters.newCritters.Count)
			{
				SpawnCritterInSlot(newCritterPen[i], _savedCritters.newCritters[i]);
			}
			else
			{
				DespawnCritterFromSlot(newCritterPen[i]);
			}
		}
	}

	private void UpdateCollection()
	{
		int num = _collectionPageIndex * collection.Length;
		for (int i = 0; i < collection.Length; i++)
		{
			int num2 = num + i;
			MenagerieSlot menagerieSlot = collection[i];
			if (_savedCritters.collectedCritters.TryGetValue(num2, out var value))
			{
				SpawnCritterInSlot(menagerieSlot, value);
				continue;
			}
			DespawnCritterFromSlot(menagerieSlot);
			CritterConfiguration critterConfiguration = critterIndex[num2];
			menagerieSlot.label.text = ((critterConfiguration == null) ? "" : "??????");
		}
	}

	private void UpdateFavoriteCritter()
	{
		if (_savedCritters.collectedCritters.TryGetValue(_savedCritters.favoriteCritter, out var value))
		{
			SpawnCritterInSlot(favoriteCritterSlot, value);
		}
		else
		{
			ClearSlot(favoriteCritterSlot);
		}
	}

	public void NextGroupCollectedCritters()
	{
		_collectionPageIndex++;
		if (_collectionPageIndex >= _totalPages)
		{
			_collectionPageIndex = 0;
		}
		UpdateCollection();
	}

	public void PrevGroupCollectedCritters()
	{
		_collectionPageIndex--;
		if (_collectionPageIndex < 0)
		{
			_collectionPageIndex = _totalPages - 1;
		}
		UpdateCollection();
	}

	private void GenerateNewCritters()
	{
		GenerateNewCritterCount(UnityEngine.Random.Range(Mathf.Min(1, newCritterPen.Length), newCritterPen.Length + 1));
	}

	private void GenerateLegalNewCritters()
	{
		ClearNewCritterPen();
		for (int i = 0; i < newCritterPen.Length; i++)
		{
			int randomCritterType = critterIndex.GetRandomCritterType();
			if (randomCritterType < 0)
			{
				Debug.LogError("Failed to spawn valid critter. No critter configuration found.");
				break;
			}
			CritterData critterData = new CritterData(randomCritterType, critterIndex[randomCritterType].GenerateAppearance());
			AddCritterToNewCritterPen(critterData);
		}
	}

	private void GenerateNewCritterCount(int critterCount)
	{
		ClearNewCritterPen();
		for (int i = 0; i < critterCount; i++)
		{
			int num = UnityEngine.Random.Range(0, critterIndex.critterTypes.Count);
			CritterConfiguration critterConfiguration = critterIndex[num];
			CritterData critterData = new CritterData(num, critterConfiguration.GenerateAppearance());
			AddCritterToNewCritterPen(critterData);
		}
	}

	private void GenerateCollectedCritters(float spawnChance)
	{
		ClearCollection();
		for (int i = 0; i < critterIndex.critterTypes.Count; i++)
		{
			if (UnityEngine.Random.value <= spawnChance)
			{
				CritterConfiguration critterConfiguration = critterIndex[i];
				CritterData critterData = new CritterData(i, critterConfiguration.GenerateAppearance());
				AddCritterToCollection(critterData);
				_ = (bool)critterData.instance;
			}
		}
	}

	private void MoveNewCrittersToCollection()
	{
		MenagerieSlot[] array = newCritterPen;
		foreach (MenagerieSlot menagerieSlot in array)
		{
			if ((bool)menagerieSlot.critter)
			{
				CritterDepositedInCollectionBox(menagerieSlot.critter);
			}
		}
	}

	private void DonateNewCritters()
	{
		MenagerieSlot[] array = newCritterPen;
		foreach (MenagerieSlot menagerieSlot in array)
		{
			if ((bool)menagerieSlot.critter)
			{
				CritterDepositedInDonationBox(menagerieSlot.critter);
			}
		}
	}

	private void ClearSlot(MenagerieSlot slot)
	{
		DespawnCritterFromSlot(slot);
		if ((bool)slot.label)
		{
			slot.label.text = "";
		}
	}

	private void DespawnCritterFromSlot(MenagerieSlot slot)
	{
		if (!slot.IsNull() && (bool)slot.critter)
		{
			_critters.Remove(slot.critter);
			UnityEngine.Object.Destroy(slot.critter.gameObject);
			slot.critter = null;
			if ((bool)slot.label)
			{
				slot.label.text = "";
			}
		}
	}

	private void ClearNewCritterPen()
	{
		_savedCritters.newCritters.Clear();
		UpdateNewCritterPen();
	}

	private void ClearCollection()
	{
		_savedCritters.collectedCritters.Clear();
		UpdateCollection();
		UpdateFavoriteCritter();
	}

	private void ClearAll()
	{
		_savedCritters.Clear();
		UpdateMenagerie();
	}

	private void ResetSavedCreatures()
	{
		ClearAll();
		Save();
	}

	private void Load()
	{
		ClearAll();
		string jsonString = PlayerPrefs.GetString("_SavedCritters", string.Empty);
		LoadCrittersFromJson(jsonString);
		UpdateMenagerie();
	}

	private void Save()
	{
		Debug.Log($"Saving {_critters.Count} critters");
		string value = SaveCrittersToJson();
		PlayerPrefs.SetString("_SavedCritters", value);
	}

	private void LoadCrittersFromJson(string jsonString)
	{
		_savedCritters.Clear();
		if (!string.IsNullOrEmpty(jsonString))
		{
			try
			{
				_savedCritters = JsonConvert.DeserializeObject<CritterSaveData>(jsonString);
			}
			catch (Exception exception)
			{
				Debug.LogError("Unable to deserialize critters from json: " + jsonString);
				Debug.LogException(exception);
			}
		}
		ValidateSaveData();
	}

	private string SaveCrittersToJson()
	{
		ValidateSaveData();
		string text = JsonConvert.SerializeObject(_savedCritters, Formatting.Indented);
		Debug.Log("Critters save to JSON: " + text);
		return text;
	}

	private void ValidateSaveData()
	{
		if (_savedCritters.newCritters.Count > newCritterPen.Length)
		{
			Debug.LogError($"Too many new critters in CrittersSaveData ({_savedCritters.newCritters.Count} vs {newCritterPen.Length}) - correcting.");
			while (_savedCritters.newCritters.Count > newCritterPen.Length)
			{
				_savedCritters.newCritters.RemoveAt(newCritterPen.Length);
			}
			Save();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		MenagerieSlot[] array = newCritterPen;
		for (int i = 0; i < array.Length; i++)
		{
			Gizmos.DrawWireSphere(array[i].critterMountPoint.position, 0.1f);
		}
		array = collection;
		for (int i = 0; i < array.Length; i++)
		{
			Gizmos.DrawWireSphere(array[i].critterMountPoint.position, 0.1f);
		}
		Gizmos.DrawWireSphere(favoriteCritterSlot.critterMountPoint.position, 0.1f);
	}
}
