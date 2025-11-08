using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

[NetworkBehaviourWeaved(13)]
public class FlowersManager : NetworkComponent
{
	[Serializable]
	public class FlowersInZone
	{
		public GTZone zone;

		public List<GameObject> sections;
	}

	public List<FlowersInZone> sections;

	public int flowersToCheck = 1;

	public int flowerCheckIndex;

	private readonly List<Flower> allFlowers = new List<Flower>();

	private SlingshotProjectileHitNotifier[] hitNotifiers;

	private readonly Dictionary<GameObject, List<Flower>> sectionToFlowersDict = new Dictionary<GameObject, List<Flower>>();

	private readonly Dictionary<GameObject, GTZone> sectionToZonesDict = new Dictionary<GameObject, GTZone>();

	private bool hasBeenSerialized;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 13)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private FlowersDataStruct _Data;

	public static FlowersManager Instance { get; private set; }

	[Networked]
	[NetworkedWeaved(0, 13)]
	private unsafe FlowersDataStruct Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing FlowersManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(FlowersDataStruct*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing FlowersManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(FlowersDataStruct*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
		hitNotifiers = GetComponentsInChildren<SlingshotProjectileHitNotifier>();
		SlingshotProjectileHitNotifier[] array = hitNotifiers;
		foreach (SlingshotProjectileHitNotifier slingshotProjectileHitNotifier in array)
		{
			if (slingshotProjectileHitNotifier != null)
			{
				slingshotProjectileHitNotifier.OnProjectileTriggerEnter += ProjectileHitReceiver;
			}
			else
			{
				Debug.LogError("Needs SlingshotProjectileHitNotifier added to this GameObject children");
			}
		}
		foreach (FlowersInZone section in sections)
		{
			foreach (GameObject section2 in section.sections)
			{
				sectionToZonesDict[section2] = section.zone;
				Flower[] componentsInChildren = section2.GetComponentsInChildren<Flower>();
				allFlowers.AddRange(componentsInChildren);
				sectionToFlowersDict[section2] = componentsInChildren.ToList();
			}
		}
	}

	private new void Start()
	{
		NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(HandleOnZoneChanged));
		if (!base.IsMine)
		{
			return;
		}
		foreach (Flower allFlower in allFlowers)
		{
			allFlower.UpdateFlowerState(Flower.FlowerState.Healthy, isWatered: false, updateVisual: false);
		}
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		SlingshotProjectileHitNotifier[] array = hitNotifiers;
		foreach (SlingshotProjectileHitNotifier slingshotProjectileHitNotifier in array)
		{
			if (slingshotProjectileHitNotifier != null)
			{
				slingshotProjectileHitNotifier.OnProjectileTriggerEnter -= ProjectileHitReceiver;
			}
		}
		Instance = null;
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(HandleOnZoneChanged));
	}

	private void ProjectileHitReceiver(SlingshotProjectile projectile, Collider collider)
	{
		if (projectile.CompareTag("WaterBalloonProjectile"))
		{
			WaterFlowers(collider);
		}
	}

	private void WaterFlowers(Collider collider)
	{
		if (!base.IsMine)
		{
			return;
		}
		GameObject gameObject = collider.gameObject;
		if (gameObject == null)
		{
			Debug.LogError("Could not find any flowers section");
			return;
		}
		foreach (Flower item in sectionToFlowersDict[gameObject])
		{
			item.WaterFlower(isWatered: true);
		}
	}

	private void HandleOnZoneChanged()
	{
		foreach (KeyValuePair<GameObject, GTZone> item in sectionToZonesDict)
		{
			bool enable = ZoneManagement.instance.IsZoneActive(item.Value);
			foreach (Flower item2 in sectionToFlowersDict[item.Key])
			{
				item2.UpdateVisuals(enable);
			}
		}
	}

	public int GetHealthyFlowersInZoneCount(GTZone zone)
	{
		int num = 0;
		foreach (KeyValuePair<GameObject, GTZone> item in sectionToZonesDict)
		{
			if (item.Value != zone)
			{
				continue;
			}
			foreach (Flower item2 in sectionToFlowersDict[item.Key])
			{
				if (item2.GetCurrentState() == Flower.FlowerState.Healthy)
				{
					num++;
				}
			}
		}
		return num;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			stream.SendNext(allFlowers.Count);
			for (int i = 0; i < allFlowers.Count; i++)
			{
				stream.SendNext(allFlowers[i].IsWatered);
				stream.SendNext(allFlowers[i].GetCurrentState());
			}
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender != PhotonNetwork.MasterClient)
		{
			return;
		}
		int num = (int)stream.ReceiveNext();
		for (int i = 0; i < num; i++)
		{
			bool isWatered = (bool)stream.ReceiveNext();
			Flower.FlowerState currentState = allFlowers[i].GetCurrentState();
			Flower.FlowerState flowerState = (Flower.FlowerState)stream.ReceiveNext();
			if (currentState != flowerState)
			{
				allFlowers[i].UpdateFlowerState(flowerState, isWatered);
			}
		}
	}

	public override void WriteDataFusion()
	{
		if (base.HasStateAuthority)
		{
			Data = new FlowersDataStruct(allFlowers);
		}
	}

	public override void ReadDataFusion()
	{
		if (Data.FlowerCount <= 0)
		{
			return;
		}
		for (int i = 0; i < Data.FlowerCount; i++)
		{
			bool isWatered = Data.FlowerWateredData[i] == 1;
			Flower.FlowerState currentState = allFlowers[i].GetCurrentState();
			Flower.FlowerState flowerState = (Flower.FlowerState)Data.FlowerStateData[i];
			if (currentState != flowerState)
			{
				allFlowers[i].UpdateFlowerState(flowerState, isWatered);
			}
		}
	}

	private void Update()
	{
		for (int i = flowerCheckIndex + 1; i < allFlowers.Count && i < flowerCheckIndex + flowersToCheck; i++)
		{
			allFlowers[i].AnimCatch();
		}
		flowerCheckIndex = ((flowerCheckIndex + flowersToCheck < allFlowers.Count) ? (flowerCheckIndex + flowersToCheck) : 0);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
