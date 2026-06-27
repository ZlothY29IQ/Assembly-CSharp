using System;
using System.Collections.Generic;
using GorillaGameModes;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class SuperInfection : MonoBehaviour, IGorillaSliceableSimple
{
	private const string preLog = "[GT/SuperInfection]  ";

	private const string preErr = "[GT/SuperInfection]  ERROR!!!  ";

	public SICombinedTerminal[] siTerminals;

	public SIResourceDeposit[] siDeposits;

	public SIQuestBoard questBoard;

	public SIPurchaseTerminal purchaseTerminal;

	[Tooltip("Add miscellaneous zone objects here.  They'll be disabled when not in this mode.")]
	public GameObject[] zoneObjects;

	public Transform resourceNodeParent;

	public SIResourceRegion[] resourceRegions;

	public int perPlayerHourlyResourceRate = 20;

	[Tooltip("Resource generation rate varies based on population.  We'll assume at least this many players are present.")]
	public int minRoomPopulation = 4;

	public Transform perRoundResourceNodeParent;

	public SIResourceRegion[] perRoundResourceRegions;

	[NonSerialized]
	public SuperInfectionManager siManager;

	public Transform resourceResetLoc;

	private float resourceResetHeight;

	public List<SIGadget> activeGadgets = new List<SIGadget>();

	public GTZone zone;

	public SITechTreeSO techTreeSO;

	private bool retryCreatePerRoundResources;

	private float _nextResourceUpdateTime;

	private float _lastResourceSpawnTime;

	private int authorityActorNumber;

	public TextMeshProUGUI authorityName;

	private List<SIResource> _resourcePrefabs;

	public bool IsAuthorityAndActive
	{
		get
		{
			if (siManager.gameEntityManager.IsAuthority())
			{
				return siManager.gameEntityManager.IsZoneActive();
			}
			return false;
		}
	}

	public float ResourceSpawnInterval
	{
		get
		{
			if (!Application.isPlaying)
			{
				return 0f;
			}
			return GetResourceSpawnInterval();
		}
	}

	public float TimeSinceLastSpawn => Time.time - _lastResourceSpawnTime;

	public float TimeToNextSpawn
	{
		get
		{
			if (!Application.isPlaying)
			{
				return 0f;
			}
			if (!(_lastResourceSpawnTime > 0f))
			{
				return 0f;
			}
			return GetResourceSpawnInterval() - (Time.time - _lastResourceSpawnTime);
		}
	}

	public List<SIResource> ResourcePrefabs => _resourcePrefabs;

	private void Awake()
	{
		resourceRegions = ((resourceNodeParent != null) ? resourceNodeParent.GetComponentsInChildren<SIResourceRegion>(includeInactive: true) : Array.Empty<SIResourceRegion>());
		_resourcePrefabs = new List<SIResource>();
		SIResourceRegion[] array = resourceRegions;
		foreach (SIResourceRegion sIResourceRegion in array)
		{
			if (!_resourcePrefabs.Contains(sIResourceRegion.resourcePrefab))
			{
				_resourcePrefabs.Add(sIResourceRegion.resourcePrefab);
			}
		}
		perRoundResourceRegions = ((perRoundResourceNodeParent != null) ? perRoundResourceNodeParent.GetComponentsInChildren<SIResourceRegion>(includeInactive: true) : Array.Empty<SIResourceRegion>());
		resourceResetHeight = ((resourceResetLoc != null) ? resourceResetLoc.position.y : float.MinValue);
	}

	public void OnEnable()
	{
		siManager = SuperInfectionManager.GetSIManagerForZone(zone);
		if (siManager != null)
		{
			siManager.OnEnableZoneSuperInfection(this);
		}
		if (siManager == null || siManager.isActiveAndEnabled)
		{
			DisableStations();
		}
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(RemovePlayerGadgetsOnLeave);
		}
		for (int i = 0; i < siTerminals.Length; i++)
		{
			siTerminals[i].index = i;
		}
		for (int j = 0; j < siDeposits.Length; j++)
		{
			siDeposits[j].index = j;
		}
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if ((bool)siManager)
			{
				siManager.zoneSuperInfection = null;
			}
			DisableStations();
			if (NetworkSystem.Instance != null)
			{
				NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(RemovePlayerGadgetsOnLeave);
			}
			GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		}
	}

	public void OnZoneInit()
	{
		RebuildRegionItemsFromEntities();
		EnableStations();
	}

	public void OnZoneClear(ZoneClearReason reason)
	{
		if (reason != ZoneClearReason.JoinZone)
		{
			DisableStations();
			SIProgression.Instance.SendTelemetryData();
		}
	}

	private void EnableStations()
	{
		for (int i = 0; i < siTerminals.Length; i++)
		{
			siTerminals[i].gameObject.SetActive(value: true);
			if ((bool)siTerminals[i].dispenser && siTerminals[i].dispenser.isTryOn && siManager != null)
			{
				siManager.RegisterTryOnDispenser();
			}
		}
		for (int j = 0; j < siDeposits.Length; j++)
		{
			siDeposits[j].gameObject.SetActive(value: true);
		}
		if (questBoard != null)
		{
			questBoard.gameObject.SetActive(value: true);
		}
		if (purchaseTerminal != null)
		{
			purchaseTerminal.gameObject.SetActive(value: true);
		}
		for (int k = 0; k < zoneObjects.Length; k++)
		{
			GameObject gameObject = zoneObjects[k];
			if (gameObject != null)
			{
				gameObject.SetActive(value: true);
			}
			else
			{
				Debug.LogError("[GT/SuperInfection]  ERROR!!!  " + $"null ref at `zoneObjects[{k}]`.");
			}
		}
	}

	private void DisableStations()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		for (int i = 0; i < siTerminals.Length; i++)
		{
			if (siManager != null && (bool)siTerminals[i].dispenser && siTerminals[i].dispenser.isTryOn)
			{
				siManager.UnregisterTryOnDispenser();
			}
			siTerminals[i].gameObject.SetActive(value: false);
			siTerminals[i].Reset();
		}
		for (int j = 0; j < siDeposits.Length; j++)
		{
			siDeposits[j].gameObject.SetActive(value: false);
		}
		if (questBoard != null)
		{
			questBoard.gameObject.SetActive(value: false);
		}
		if (purchaseTerminal != null)
		{
			purchaseTerminal.gameObject.SetActive(value: false);
		}
		for (int k = 0; k < zoneObjects.Length; k++)
		{
			GameObject gameObject = zoneObjects[k];
			if (gameObject != null)
			{
				gameObject.SetActive(value: false);
			}
			else
			{
				Debug.LogError("[GT/SuperInfection]  ERROR!!!  " + $"null ref at `zoneObjects[{k}]`.");
			}
		}
	}

	public void Update()
	{
		if (!IsAuthorityAndActive)
		{
			return;
		}
		if (retryCreatePerRoundResources)
		{
			CreatePerRoundResources();
		}
		if (!(Time.time >= _nextResourceUpdateTime))
		{
			return;
		}
		GetResourceSpawnInterval();
		SIResourceRegion[] array = resourceRegions;
		foreach (SIResourceRegion sIResourceRegion in array)
		{
			for (int num = sIResourceRegion.ItemCount - 1; num >= 0; num--)
			{
				GameEntity gameEntity = sIResourceRegion.Items[num];
				if (!gameEntity)
				{
					sIResourceRegion.Items.RemoveAt(num);
				}
				else if (gameEntity.transform.position.y < resourceResetHeight)
				{
					siManager.gameEntityManager.RequestDestroyItem(gameEntity.id);
				}
			}
		}
		CheckResourceSpawn();
		_nextResourceUpdateTime = Time.time + 1f;
	}

	private void CheckResourceSpawn()
	{
		if (!(Time.time >= GetNextResourceSpawnTime()))
		{
			return;
		}
		SIResourceRegion sIResourceRegion = null;
		float num = float.MaxValue;
		SIResourceRegion[] array = resourceRegions;
		foreach (SIResourceRegion sIResourceRegion2 in array)
		{
			if (sIResourceRegion2.ItemCount < sIResourceRegion2.MaxItems && sIResourceRegion2.LastSpawnTime < num)
			{
				sIResourceRegion = sIResourceRegion2;
				num = sIResourceRegion2.LastSpawnTime;
			}
		}
		if (!sIResourceRegion)
		{
			_lastResourceSpawnTime = Time.time;
			return;
		}
		(bool, Vector3, Vector3) spawnPointWithNormal = sIResourceRegion.GetSpawnPointWithNormal();
		if (spawnPointWithNormal.Item1 && !(sIResourceRegion.resourcePrefab == null))
		{
			float spawnPitchVariance = sIResourceRegion.resourcePrefab.spawnPitchVariance;
			Quaternion quaternion = Quaternion.Euler(UnityEngine.Random.Range(0f - spawnPitchVariance, spawnPitchVariance), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0f - spawnPitchVariance, spawnPitchVariance));
			Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, spawnPointWithNormal.Item3), spawnPointWithNormal.Item3) * quaternion;
			GameEntity gameEntity = siManager.gameEntityManager.GetGameEntity(siManager.gameEntityManager.RequestCreateItem(sIResourceRegion.resourcePrefab.gameObject.name.GetStaticHash(), spawnPointWithNormal.Item2, rotation, 0L));
			if ((bool)gameEntity)
			{
				GTDev.Log($"Spawned {gameEntity.name} at {spawnPointWithNormal.Item2}", gameEntity);
				sIResourceRegion.AddItem(gameEntity);
				sIResourceRegion.LastSpawnTime = (_lastResourceSpawnTime = Time.time);
			}
			else
			{
				GTDev.LogError($"Failed to spawn {sIResourceRegion.resourcePrefab.gameObject.name} at {spawnPointWithNormal.Item2}");
			}
		}
	}

	private float GetNextResourceSpawnTime()
	{
		if (!(_lastResourceSpawnTime > 0f))
		{
			return 0f;
		}
		return _lastResourceSpawnTime + GetResourceSpawnInterval();
	}

	private float GetResourceSpawnInterval()
	{
		return 3600f / (float)(perPlayerHourlyResourceRate * Mathf.Max(GameMode.ParticipatingPlayers.Count, minRoomPopulation));
	}

	public void RemovePlayerGadgetsOnLeave(NetPlayer player)
	{
		SIPlayer sIPlayer = SIPlayer.Get(player.ActorNumber);
		if (sIPlayer == null)
		{
			return;
		}
		if (siManager.gameEntityManager.IsAuthority())
		{
			for (int num = sIPlayer.activePlayerGadgets.Count - 1; num >= 0; num--)
			{
				siManager.gameEntityManager.RequestDestroyItem(siManager.gameEntityManager.GetGameEntityFromNetId(sIPlayer.activePlayerGadgets[num]).id);
			}
		}
		sIPlayer.activePlayerGadgets.Clear();
	}

	public void RefreshStations(int actorNr)
	{
		for (int i = 0; i < siTerminals.Length; i++)
		{
			if (!(siTerminals[i].activePlayer == null) && siTerminals[i].activePlayer.gameObject.activeInHierarchy && siTerminals[i].activePlayer.ActorNr == actorNr)
			{
				siTerminals[i].techTree.UpdateState(siTerminals[i].techTree.currentState);
				siTerminals[i].resourceCollection.UpdateState(siTerminals[i].resourceCollection.currentState);
				siTerminals[i].dispenser.UpdateState(siTerminals[i].dispenser.currentState);
			}
		}
		if (SIPlayer.LocalPlayer.ActorNr == actorNr && purchaseTerminal != null)
		{
			purchaseTerminal.UpdateCurrentTechPoints();
		}
	}

	public void SliceUpdate()
	{
		if (!siManager.gameEntityManager.IsAuthority())
		{
			return;
		}
		for (int num = activeGadgets.Count - 1; num >= 0; num--)
		{
			if (activeGadgets[num] == null)
			{
				activeGadgets.RemoveAt(num);
			}
			else if (activeGadgets[num].transform.position.y < resourceResetHeight)
			{
				siManager.gameEntityManager.RequestDestroyItem(activeGadgets[num].gameEntity.id);
			}
		}
	}

	private void RebuildRegionItemsFromEntities()
	{
		if (resourceRegions.Length + perRoundResourceRegions.Length == 0)
		{
			return;
		}
		int[] array = new int[resourceRegions.Length];
		for (int i = 0; i < resourceRegions.Length; i++)
		{
			resourceRegions[i].Items.Clear();
			array[i] = ((resourceRegions[i].resourcePrefab != null) ? resourceRegions[i].resourcePrefab.gameObject.name.GetStaticHash() : 0);
		}
		int[] array2 = new int[perRoundResourceRegions.Length];
		for (int j = 0; j < perRoundResourceRegions.Length; j++)
		{
			perRoundResourceRegions[j].Items.Clear();
			array2[j] = ((perRoundResourceRegions[j].resourcePrefab != null) ? perRoundResourceRegions[j].resourcePrefab.gameObject.name.GetStaticHash() : 0);
		}
		List<GameEntity> gameEntities = siManager.gameEntityManager.GetGameEntities();
		int num = 0;
		int num2 = 0;
		for (int k = 0; k < gameEntities.Count; k++)
		{
			GameEntity gameEntity = gameEntities[k];
			if (gameEntity == null || gameEntity.GetComponent<SIResource>() == null)
			{
				continue;
			}
			int typeId = gameEntity.typeId;
			bool flag = false;
			SIResourceRegion sIResourceRegion = null;
			int num3 = int.MaxValue;
			for (int l = 0; l < resourceRegions.Length; l++)
			{
				if (array[l] == typeId && resourceRegions[l].ItemCount < resourceRegions[l].MaxItems && resourceRegions[l].ItemCount < num3)
				{
					sIResourceRegion = resourceRegions[l];
					num3 = resourceRegions[l].ItemCount;
				}
			}
			if (sIResourceRegion != null)
			{
				sIResourceRegion.AddItem(gameEntity);
				num++;
				flag = true;
			}
			if (!flag)
			{
				for (int m = 0; m < perRoundResourceRegions.Length; m++)
				{
					if (array2[m] == typeId && perRoundResourceRegions[m].ItemCount < perRoundResourceRegions[m].MaxItems)
					{
						perRoundResourceRegions[m].AddItem(gameEntity);
						num++;
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				num2++;
			}
		}
		if (num2 > 0 && siManager.gameEntityManager.IsAuthority())
		{
			for (int num4 = gameEntities.Count - 1; num4 >= 0; num4--)
			{
				GameEntity gameEntity2 = gameEntities[num4];
				if (!(gameEntity2 == null) && !(gameEntity2.GetComponent<SIResource>() == null) && gameEntity2.heldByActorNumber == 0)
				{
					bool flag2 = false;
					for (int n = 0; n < resourceRegions.Length; n++)
					{
						if (flag2)
						{
							break;
						}
						flag2 = resourceRegions[n].Items.Contains(gameEntity2);
					}
					for (int num5 = 0; num5 < perRoundResourceRegions.Length; num5++)
					{
						if (flag2)
						{
							break;
						}
						flag2 = perRoundResourceRegions[num5].Items.Contains(gameEntity2);
					}
					if (!flag2)
					{
						siManager.gameEntityManager.RequestDestroyItem(gameEntity2.id);
					}
				}
			}
		}
		if (num > 0)
		{
			_lastResourceSpawnTime = Time.time;
		}
	}

	public void AddGadget(SIGadget gadget)
	{
		activeGadgets.Add(gadget);
	}

	public void RemoveGadget(SIGadget gadget)
	{
		activeGadgets.Remove(gadget);
	}

	public void ResetPerRoundResources()
	{
		ClearPerRoundResources();
		CreatePerRoundResources();
	}

	private void CreatePerRoundResources()
	{
		if (!siManager.gameEntityManager.IsZoneActive())
		{
			retryCreatePerRoundResources = true;
			return;
		}
		retryCreatePerRoundResources = false;
		SIResourceRegion[] array = perRoundResourceRegions;
		foreach (SIResourceRegion sIResourceRegion in array)
		{
			for (int j = sIResourceRegion.ItemCount; j < sIResourceRegion.MaxItems; j++)
			{
				(bool, Vector3, Vector3) spawnPointWithNormal = sIResourceRegion.GetSpawnPointWithNormal();
				Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, spawnPointWithNormal.Item3), spawnPointWithNormal.Item3) * Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				GameEntity gameEntity = siManager.gameEntityManager.GetGameEntity(siManager.gameEntityManager.RequestCreateItem(sIResourceRegion.resourcePrefab.gameObject.name.GetStaticHash(), spawnPointWithNormal.Item2, rotation, 0L));
				if ((bool)gameEntity)
				{
					sIResourceRegion.AddItem(gameEntity);
					if (!spawnPointWithNormal.Item1)
					{
						Rigidbody component = gameEntity.GetComponent<Rigidbody>();
						if ((object)component != null)
						{
							component.isKinematic = false;
						}
					}
				}
				else
				{
					GTDev.LogError($"Failed to spawn {sIResourceRegion.resourcePrefab.gameObject.name} at {spawnPointWithNormal.Item2}");
				}
			}
		}
	}

	private void ClearPerRoundResources()
	{
		SIResourceRegion[] array = perRoundResourceRegions;
		foreach (SIResourceRegion sIResourceRegion in array)
		{
			for (int num = sIResourceRegion.ItemCount - 1; num >= 0; num--)
			{
				GameEntity gameEntity = sIResourceRegion.Items[num];
				if (!gameEntity)
				{
					sIResourceRegion.Items.RemoveAt(num);
				}
				else if (gameEntity.lastHeldByActorNumber == 0 || !(SIPlayer.Get(gameEntity.lastHeldByActorNumber) != null))
				{
					siManager.gameEntityManager.RequestDestroyItem(gameEntity.id);
				}
			}
		}
	}
}
