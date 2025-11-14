using System;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Unity.Collections;
using UnityEngine;

public class GRTool : MonoBehaviour, IGameEntitySerialize, IGameEntityComponent, IGameEntityDebugComponent
{
	public enum GRToolType
	{
		None,
		Club,
		Collector,
		Flash,
		Lantern,
		Revive,
		ShieldGun,
		DirectionalShield,
		DockWrist,
		EnergyEfficiency,
		DropPod,
		HockeyStick,
		StatusWatch,
		RattyBackpack
	}

	[Serializable]
	public class Upgrade
	{
		public GRToolProgressionManager.ToolParts UpgradeType;

		public int Slot;

		public List<GameObject> VisibleItem;

		public List<GRBonusEntry> bonusEffects;
	}

	[Serializable]
	public class UpgradeSlot
	{
		public List<GameObject> DefaultVisibleItems;

		[NonSerialized]
		public Upgrade installedItem;
	}

	public delegate void EnergyChangeEvent(GRTool tool, int energyChange, GameEntityId chargingEntityId);

	public delegate void ToolUpgradedEvent(GRTool tool);

	public GRAttributes attributes;

	public List<Upgrade> upgrades;

	public List<UpgradeSlot> upgradeSlots = new List<UpgradeSlot>();

	public List<GRMeterEnergy> energyMeters;

	public GameEntity gameEntity;

	public GRToolType toolType;

	[ReadOnly]
	public int energy;

	public GameObject UpgradeFXNode;

	private List<MeshFilter> reservedMeshFilterSearchList = new List<MeshFilter>(32);

	private List<SkinnedMeshRenderer> reservedMeshFilterSearchListSkinned = new List<SkinnedMeshRenderer>(32);

	private Upgrade upgradeListsAreValidFor;

	public event EnergyChangeEvent OnEnergyChange;

	public event ToolUpgradedEvent onToolUpgraded;

	private void Awake()
	{
	}

	private void Start()
	{
		if (gameEntity == null)
		{
			gameEntity = GetComponent<GameEntity>();
		}
		RefreshMeters();
	}

	public void OnEntityInit()
	{
		energy = GetEnergyStart();
		GhostReactor.ToolEntityCreateData toolEntityCreateData = GhostReactor.ToolEntityCreateData.Unpack(gameEntity.createData);
		GhostReactorManager ghostReactorManager = GhostReactorManager.Get(gameEntity);
		if (ghostReactorManager != null)
		{
			GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = ghostReactorManager.GetToolUpgradeStationFullForIndex(toolEntityCreateData.stationIndex);
			if (toolUpgradeStationFullForIndex != null)
			{
				toolUpgradeStationFullForIndex.InitLinkedEntity(gameEntity);
			}
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	public int GetEnergyMax()
	{
		return attributes.CalculateFinalValueForAttribute(GRAttributeType.EnergyMax);
	}

	public int GetEnergyUseCost()
	{
		return attributes.CalculateFinalValueForAttribute(GRAttributeType.EnergyUseCost);
	}

	public int GetEnergyStart()
	{
		if (!attributes.HasValueForAttribute(GRAttributeType.EnergyStart))
		{
			return 0;
		}
		return attributes.CalculateFinalValueForAttribute(GRAttributeType.EnergyStart);
	}

	private void OnEnable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(GrabbedByPlayer));
	}

	private void OnDisable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(GrabbedByPlayer));
	}

	public void RefillEnergy(int count, GameEntityId chargingEntityId)
	{
		SetEnergyInternal(energy + count, chargingEntityId);
	}

	public void RefillEnergy()
	{
		SetEnergyInternal(GetEnergyMax(), GameEntityId.Invalid);
	}

	public void UseEnergy()
	{
		SetEnergyInternal(energy - GetEnergyUseCost(), GameEntityId.Invalid);
	}

	public bool HasEnoughEnergy()
	{
		return energy >= GetEnergyUseCost();
	}

	public void SetEnergy(int newEnergy)
	{
		SetEnergyInternal(newEnergy, GameEntityId.Invalid);
	}

	public bool IsEnergyFull()
	{
		return energy >= GetEnergyMax();
	}

	private void SetEnergyInternal(int value, GameEntityId chargingEntityId)
	{
		int num = energy;
		energy = Mathf.Clamp(value, 0, GetEnergyMax());
		int energyChange = energy - num;
		this.OnEnergyChange?.Invoke(this, energyChange, chargingEntityId);
		RefreshMeters();
	}

	public void RefreshMeters()
	{
		for (int i = 0; i < energyMeters.Count; i++)
		{
			energyMeters[i].Refresh();
		}
	}

	public bool HasUpgradeInstalled(GRToolProgressionManager.ToolParts upgradeID)
	{
		for (int i = 0; i < upgradeSlots.Count; i++)
		{
			if (upgradeSlots[i].installedItem != null && upgradeSlots[i].installedItem.UpgradeType == upgradeID)
			{
				return true;
			}
		}
		return false;
	}

	public Upgrade FindMatchingUpgrade(GRToolProgressionManager.ToolParts upgradeID)
	{
		for (int i = 0; i < upgrades.Count; i++)
		{
			if (upgrades[i].UpgradeType == upgradeID)
			{
				return upgrades[i];
			}
		}
		return null;
	}

	public float GetPointDistanceToUpgrade(Vector3 point, Upgrade upgrade)
	{
		if (upgrade.VisibleItem.Count < 1)
		{
			return -1f;
		}
		if (upgradeListsAreValidFor != upgrade)
		{
			reservedMeshFilterSearchList.Clear();
			upgrade.VisibleItem[0].GetComponentsInChildren(reservedMeshFilterSearchList);
			reservedMeshFilterSearchListSkinned.Clear();
			upgrade.VisibleItem[0].GetComponentsInChildren(includeInactive: false, reservedMeshFilterSearchListSkinned);
			upgradeListsAreValidFor = upgrade;
		}
		float num = float.MaxValue;
		foreach (MeshFilter reservedMeshFilterSearch in reservedMeshFilterSearchList)
		{
			Vector3 vector = reservedMeshFilterSearch.transform.InverseTransformPoint(point);
			Bounds bounds = reservedMeshFilterSearch.sharedMesh.bounds;
			Vector3 vector2 = new Vector3(Mathf.Clamp(vector.x, bounds.min.x, bounds.max.x), Mathf.Clamp(vector.y, bounds.min.y, bounds.max.y), Mathf.Clamp(vector.z, bounds.min.z, bounds.max.z));
			Vector3 vector3 = vector - vector2;
			float sqrMagnitude = reservedMeshFilterSearch.transform.TransformVector(vector3).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
			}
		}
		if (reservedMeshFilterSearchListSkinned != null)
		{
			foreach (SkinnedMeshRenderer item in reservedMeshFilterSearchListSkinned)
			{
				Vector3 vector4 = item.transform.InverseTransformPoint(point);
				Bounds localBounds = item.localBounds;
				Vector3 vector5 = new Vector3(Mathf.Clamp(vector4.x, localBounds.min.x, localBounds.max.x), Mathf.Clamp(vector4.y, localBounds.min.y, localBounds.max.y), Mathf.Clamp(vector4.z, localBounds.min.z, localBounds.max.z));
				Vector3 vector6 = vector4 - vector5;
				float sqrMagnitude2 = item.transform.TransformVector(vector6).sqrMagnitude;
				if (sqrMagnitude2 < num)
				{
					num = sqrMagnitude2;
				}
			}
		}
		if (num == float.MaxValue)
		{
			return Vector3.Distance(point, upgrade.VisibleItem[0].transform.position);
		}
		return Mathf.Sqrt(num);
	}

	public Transform GetUpgradeAttachTransform(Upgrade upgrade)
	{
		if (upgrade.VisibleItem.Count < 1)
		{
			return null;
		}
		return upgrade.VisibleItem[0].transform;
	}

	public void UpgradeTool(GRToolProgressionManager.ToolParts upgradeID)
	{
		for (int i = 0; i < upgrades.Count; i++)
		{
			if (upgrades[i].UpgradeType != upgradeID)
			{
				continue;
			}
			ClearUpgradeSlot(upgrades[i].Slot);
			for (int j = 0; j < upgrades[i].VisibleItem.Count; j++)
			{
				upgrades[i].VisibleItem[j].SetActive(value: true);
			}
			for (int k = 0; k < upgradeSlots[upgrades[i].Slot].DefaultVisibleItems.Count; k++)
			{
				upgradeSlots[upgrades[i].Slot].DefaultVisibleItems[k].SetActive(value: false);
			}
			foreach (GRBonusEntry bonusEffect in upgrades[i].bonusEffects)
			{
				attributes.AddBonus(bonusEffect);
			}
			upgradeSlots[upgrades[i].Slot].installedItem = upgrades[i];
			if (UpgradeFXNode != null && upgrades[i].VisibleItem.Count > 0)
			{
				UpgradeFXNode.transform.position = upgrades[i].VisibleItem[0].transform.position;
				UpgradeFXNode.transform.rotation = upgrades[i].VisibleItem[0].transform.rotation;
				ParticleSystem componentInChildren = UpgradeFXNode.GetComponentInChildren<ParticleSystem>();
				AudioSource componentInChildren2 = UpgradeFXNode.GetComponentInChildren<AudioSource>();
				if (componentInChildren != null)
				{
					componentInChildren.Play();
				}
				if (componentInChildren2 != null)
				{
					componentInChildren2.Play();
				}
			}
		}
		this.onToolUpgraded?.Invoke(this);
	}

	public void ClearUpgradeSlot(int slot)
	{
		if (upgradeSlots[slot].installedItem == null)
		{
			return;
		}
		for (int i = 0; i < upgradeSlots[slot].installedItem.VisibleItem.Count; i++)
		{
			upgradeSlots[slot].installedItem.VisibleItem[i].SetActive(value: false);
		}
		foreach (GRBonusEntry bonusEffect in upgradeSlots[slot].installedItem.bonusEffects)
		{
			attributes.RemoveBonus(bonusEffect);
		}
		for (int j = 0; j < upgradeSlots[slot].DefaultVisibleItems.Count; j++)
		{
			upgradeSlots[slot].DefaultVisibleItems[j].SetActive(value: true);
		}
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		writer.Write(upgradeSlots.Count);
		for (int i = 0; i < upgradeSlots.Count; i++)
		{
			if (upgradeSlots[i] != null)
			{
				if (upgradeSlots[i].installedItem != null)
				{
					writer.Write(upgradeSlots[i].installedItem.UpgradeType.ToString());
				}
				else
				{
					writer.Write("");
				}
			}
			else
			{
				writer.Write("");
			}
		}
		writer.Write(energy);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			GRToolProgressionManager.ToolParts result = GRToolProgressionManager.ToolParts.None;
			if (Enum.TryParse<GRToolProgressionManager.ToolParts>(reader.ReadString(), out result))
			{
				UpgradeTool(result);
			}
		}
		int num2 = reader.ReadInt32();
		SetEnergy(num2);
	}

	public void GrabbedByPlayer()
	{
		if (gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			GRPlayer gRPlayer = GRPlayer.Get(gameEntity.heldByActorNumber);
			if ((bool)gRPlayer)
			{
				gRPlayer.GrabbedItem(gameEntity.id, base.gameObject.name);
			}
		}
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"Tool Energy: <color=\"yellow\">{energy}<color=\"white\"> ");
	}
}
