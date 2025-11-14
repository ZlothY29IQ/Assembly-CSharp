using System;
using System.Collections.Generic;
using UnityEngine;

public class GRToolProgressionManager : MonoBehaviourTick
{
	public class ToolProgressionMetaData
	{
		public string name;

		public string description;

		public string annotation;

		public int shiftCreditCost;
	}

	public struct EmployeeMetadata
	{
		public string name;

		public int level;
	}

	public enum ToolParts
	{
		None,
		Baton,
		BatonDamage1,
		BatonDamage2,
		BatonDamage3,
		Flash,
		FlashDamage1,
		FlashDamage2,
		FlashDamage3,
		Collector,
		CollectorBonus1,
		CollectorBonus2,
		CollectorBonus3,
		Lantern,
		LanternIntensity1,
		LanternIntensity2,
		LanternIntensity3,
		ShieldGun,
		ShieldGunStrength1,
		ShieldGunStrength2,
		ShieldGunStrength3,
		DirectionalShield,
		DirectionalShieldSize1,
		DirectionalShieldSize2,
		DirectionalShieldSize3,
		EnergyEff,
		EnergyEff1,
		EnergyEff2,
		EnergyEff3,
		DockWrist,
		Revive,
		DropPodBasic,
		DropPodChassis1,
		DropPodChassis2,
		DropPodChassis3,
		StatusWatch,
		RattyBackpack,
		HockeyStick
	}

	[NonSerialized]
	private Dictionary<GRToolProgressionTree.EmployeeLevelRequirement, EmployeeMetadata> employeeLevelMetadata = new Dictionary<GRToolProgressionTree.EmployeeLevelRequirement, EmployeeMetadata>();

	[NonSerialized]
	private Dictionary<ToolParts, ToolProgressionMetaData> partMetadata = new Dictionary<ToolParts, ToolProgressionMetaData>();

	[NonSerialized]
	private GRToolProgressionTree toolProgressionTree = new GRToolProgressionTree();

	[NonSerialized]
	private GhostReactor reactor;

	[SerializeField]
	private List<GRResearchStation> researchStations;

	[SerializeField]
	private List<GRToolUpgradeStation> toolUpgradeStations;

	[NonSerialized]
	private bool pendingTreeToProcess;

	[NonSerialized]
	private bool pendingUpdateInventory;

	private bool sendUpdate;

	public event Action OnProgressionUpdated;

	public void SetPendingTreeToProcess()
	{
		pendingTreeToProcess = true;
	}

	public void UpdateInventory()
	{
		pendingUpdateInventory = true;
	}

	public void Init(GhostReactor ghostReactor)
	{
		reactor = ghostReactor;
		PopulateToolPartMetadata();
		PopulateEmployeeLevelMetadata();
		if (researchStations != null)
		{
			foreach (GRResearchStation researchStation in researchStations)
			{
				researchStation.Init(this, ghostReactor);
			}
		}
		if (toolUpgradeStations != null)
		{
			foreach (GRToolUpgradeStation toolUpgradeStation in toolUpgradeStations)
			{
				toolUpgradeStation.Init(this, ghostReactor);
			}
		}
		toolProgressionTree.Init(reactor, this);
		ProgressionManager.Instance.OnNodeUnlocked += delegate
		{
			NodeUnlocked();
		};
	}

	private void NodeUnlocked()
	{
		toolProgressionTree.RefreshUserInventory();
		toolProgressionTree.RefreshProgressionTree();
	}

	public override void Tick()
	{
		if (sendUpdate)
		{
			this.OnProgressionUpdated?.Invoke();
			sendUpdate = false;
		}
		if (pendingTreeToProcess)
		{
			toolProgressionTree.RefreshProgressionTree();
			pendingTreeToProcess = false;
		}
		if (pendingUpdateInventory)
		{
			toolProgressionTree.RefreshUserInventory();
			pendingUpdateInventory = false;
		}
	}

	public void SendMothershipUpdated()
	{
		sendUpdate = true;
	}

	public ToolProgressionMetaData GetPartMetadata(ToolParts part)
	{
		partMetadata.TryGetValue(part, out var value);
		return value;
	}

	private void PopulateToolPartMetadata()
	{
		PopulateClubPartMetadata();
		PopulateFlashPartMetadata();
		PopulateCollectorPartMetadata();
		PopulateLanternPartMetadata();
		PopulateShieldGunPartMetadata();
		PopulateDirectionalShieldPartMetadata();
		PopulateEnergyEfficiencyPartMetadata();
		PopulateRevivePartMetadata();
		PopulateDockWristPartMetadata();
		PopulateDropPodPartMetadata();
		PopulateHocketStickMetadata();
	}

	private void PopulateEmployeeLevelMetadata()
	{
		employeeLevelMetadata[GRToolProgressionTree.EmployeeLevelRequirement.None] = new EmployeeMetadata
		{
			name = "None",
			level = 0
		};
		employeeLevelMetadata[GRToolProgressionTree.EmployeeLevelRequirement.Intern] = new EmployeeMetadata
		{
			name = "Intern",
			level = 2
		};
		employeeLevelMetadata[GRToolProgressionTree.EmployeeLevelRequirement.PartTime] = new EmployeeMetadata
		{
			name = "Part Time",
			level = 3
		};
		employeeLevelMetadata[GRToolProgressionTree.EmployeeLevelRequirement.FullTime] = new EmployeeMetadata
		{
			name = "Full Time",
			level = 4
		};
	}

	private void PopulateClubPartMetadata()
	{
		partMetadata[ToolParts.Baton] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Charge Baton",
			description = "50,000 volts of ghost-zapping power",
			annotation = "Impact Power: ❶❶"
		};
		partMetadata[ToolParts.BatonDamage1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Lead Core",
			description = "Conductive lead sheath",
			annotation = "Attaches to Charge Baton. Impact Power: ❶❶❶"
		};
		partMetadata[ToolParts.BatonDamage2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Osmium Core",
			description = "More mass for more win",
			annotation = "Attaches to Charge Baton. Impact Power: ❶❶❶❶"
		};
		partMetadata[ToolParts.BatonDamage3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Electrified Spikes",
			description = "Impales, shocks, and crushes simultaneously",
			annotation = "Attaches to Charge Baton. Impact Power: ❶❶❶❶❶"
		};
	}

	private void PopulateFlashPartMetadata()
	{
		partMetadata[ToolParts.Flash] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Spectral Flash",
			description = "Makes strong ghosts vulnerable",
			annotation = "Damages ghost armor."
		};
		partMetadata[ToolParts.FlashDamage1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Spectral Lens",
			description = "Safety through momentary paralysis",
			annotation = "Attaches to Spectral Flash. Stuns enemies."
		};
		partMetadata[ToolParts.FlashDamage2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Parabolic Focuser",
			description = "When you want ghosts to feel it",
			annotation = "Attaches to Spectral Flash. Stuns enemies. Disintegrates armor."
		};
		partMetadata[ToolParts.FlashDamage3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Beta Wave Amplifier",
			description = "Exposure with explosive results",
			annotation = "Attaches to Spectral Flash. Stuns enemies. Shatters armor."
		};
	}

	private void PopulateCollectorPartMetadata()
	{
		partMetadata[ToolParts.Collector] = new ToolProgressionMetaData
		{
			shiftCreditCost = 50,
			name = "Collector",
			description = "Every team needs a sucker",
			annotation = "Collects essence and recharges tools"
		};
		partMetadata[ToolParts.CollectorBonus1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Vortex Intake",
			description = "Harvests ambient essence",
			annotation = "Attaches to Collector.  Recharges over time."
		};
		partMetadata[ToolParts.CollectorBonus2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Cyclone Intake",
			description = "Creates a wormhole to a twin universe",
			annotation = "Attaches to Collector. 2x collection bonus."
		};
		partMetadata[ToolParts.CollectorBonus3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Hurricane Intake",
			description = "A Category 5 commitment to teamwork",
			annotation = "Attaches to Collector. 2x collection bonus.  Area recharge."
		};
	}

	private void PopulateLanternPartMetadata()
	{
		partMetadata[ToolParts.Lantern] = new ToolProgressionMetaData
		{
			shiftCreditCost = 50,
			name = "Lantern",
			description = "Creates the gentle glow of safety",
			annotation = "Illuminates dark areas."
		};
		partMetadata[ToolParts.LanternIntensity1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Kinetic Power",
			description = "Saves batteries to optimize shareholder value",
			annotation = "Attaches to Lantern. Doesn't need recharge."
		};
		partMetadata[ToolParts.LanternIntensity2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Flare Discharge",
			description = "Blaze the trail for your team",
			annotation = "Attaches to Lantern. Drops long-lasting flares."
		};
		partMetadata[ToolParts.LanternIntensity3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Gamma Burster",
			description = "See through walls. Do not aim at important body parts",
			annotation = "Attaches to Lantern. X-ray ghost vision."
		};
	}

	private void PopulateShieldGunPartMetadata()
	{
		partMetadata[ToolParts.ShieldGun] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Forcefield Gun",
			description = "Corporate armor for fragile assets",
			annotation = "Gives forcefields."
		};
		partMetadata[ToolParts.ShieldGunStrength1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Truebright Nozzle",
			description = "Nuclear protection",
			annotation = "Attaches to Forcefield Gun. Increases light."
		};
		partMetadata[ToolParts.ShieldGunStrength2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Stealth Nozzle",
			description = "Protection they'll never see coming",
			annotation = "Attaches to Forcefield Gun. Gives temporary stealth."
		};
		partMetadata[ToolParts.ShieldGunStrength3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Medic Nozzle",
			description = "Restores productivity through impact therapy",
			annotation = "Attaches to Forcefield Gun. Heals to full."
		};
	}

	private void PopulateDirectionalShieldPartMetadata()
	{
		partMetadata[ToolParts.DirectionalShield] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Umbrella Shield",
			description = "Protects company property",
			annotation = "Blocks attacks."
		};
		partMetadata[ToolParts.DirectionalShieldSize1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Sling Shield",
			description = "Deflects danger and liability",
			annotation = "Attaches to Umbrella Shield. Reflects projectiles."
		};
		partMetadata[ToolParts.DirectionalShieldSize2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Harmshadow",
			description = "The best defense is a good offense",
			annotation = "Attaches to Umbrella Shield. Impact Power: ❶❶"
		};
		partMetadata[ToolParts.DirectionalShieldSize3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Total Defense Array",
			description = "The only safety device with a kill count",
			annotation = "Attaches to Shield. Reflects projectiles. Impact power: ❶❶"
		};
	}

	private void PopulateEnergyEfficiencyPartMetadata()
	{
		partMetadata[ToolParts.EnergyEff] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Flash",
			description = "Lead Core Does things!"
		};
		partMetadata[ToolParts.EnergyEff1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Regulator",
			description = "Do more with less",
			annotation = "Attaches to most tools. Efficiency: +❶"
		};
		partMetadata[ToolParts.EnergyEff2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Optimizer",
			description = "Half the juice, double the morale",
			annotation = "Attaches to most tools. Efficiency: +❶❶"
		};
		partMetadata[ToolParts.EnergyEff3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Peak Power",
			description = "Efficiency that borders on spiritual enlightenment",
			annotation = "Attaches to most tools. Efficiency: +❶❶❶"
		};
	}

	private void PopulateRevivePartMetadata()
	{
		partMetadata[ToolParts.Revive] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Revive",
			description = "Turns fatal injuries into teachable moments",
			annotation = "Brings defeated employees back to life."
		};
	}

	private void PopulateDockWristPartMetadata()
	{
		partMetadata[ToolParts.DockWrist] = new ToolProgressionMetaData
		{
			shiftCreditCost = 500,
			name = "Wrist Dock",
			description = "Wearable storage that maximizes output per limb",
			annotation = "Extra storage slot"
		};
		partMetadata[ToolParts.StatusWatch] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Ecto Watch",
			description = "Keep track of your location and statistics",
			annotation = "Compass and stat tracker"
		};
		partMetadata[ToolParts.RattyBackpack] = new ToolProgressionMetaData
		{
			shiftCreditCost = 300,
			name = "Ratty Backpack",
			description = "Torn up backpack we found laying around. Can store one item.",
			annotation = "Worn on the back. It's a backpack."
		};
	}

	private void PopulateDropPodPartMetadata()
	{
		partMetadata[ToolParts.DropPodBasic] = new ToolProgressionMetaData
		{
			shiftCreditCost = 100,
			name = "Starter Pod",
			description = "Descend with confidence in a personal drop pod!\nSupports drops to 5000m\nUpgradable for deeper drops",
			annotation = "DropPodBasic"
		};
		partMetadata[ToolParts.DropPodChassis1] = new ToolProgressionMetaData
		{
			shiftCreditCost = 200,
			name = "Reinforced Pod Chassis",
			description = "Upgrade your drop pod to support drops to 10000m",
			annotation = "DropPodChassis1"
		};
		partMetadata[ToolParts.DropPodChassis2] = new ToolProgressionMetaData
		{
			shiftCreditCost = 600,
			name = "Iron Pod Chassis",
			description = "Upgrade your drop pod to support drops to 15000m",
			annotation = "DropPodChassis2"
		};
		partMetadata[ToolParts.DropPodChassis3] = new ToolProgressionMetaData
		{
			shiftCreditCost = 1400,
			name = "Steel Pod Chassis",
			description = "Upgrade your drop pod to support drops to 20000m",
			annotation = "DropPodChassis3"
		};
	}

	private void PopulateHocketStickMetadata()
	{
		partMetadata[ToolParts.HockeyStick] = new ToolProgressionMetaData
		{
			shiftCreditCost = 10,
			name = "Hockey Stick",
			description = "A Used Hockey Stick",
			annotation = "Hit things with it?"
		};
	}

	public int GetRequiredEmployeeLevel(GRToolProgressionTree.EmployeeLevelRequirement employeeLevel)
	{
		return employeeLevelMetadata[employeeLevel].level;
	}

	public string GetEmployeeLevelDisplayName(GRToolProgressionTree.EmployeeLevelRequirement employeeLevel)
	{
		return employeeLevelMetadata[employeeLevel].name;
	}

	public int GetNumberOfResearchPoints()
	{
		return toolProgressionTree.GetNumberOfResearchPoints();
	}

	public List<GRTool.GRToolType> GetSupportedTools()
	{
		return toolProgressionTree.GetSupportedTools();
	}

	public List<GRToolProgressionTree.GRToolProgressionNode> GetToolUpgrades(GRTool.GRToolType tool)
	{
		return toolProgressionTree.GetToolUpgrades(tool);
	}

	public int GetRecycleShiftCredit(GRTool.GRToolType tool)
	{
		if (tool == GRTool.GRToolType.HockeyStick)
		{
			return (int)(10f / (float)reactor.vrRigs.Count);
		}
		GRToolProgressionTree.GRToolProgressionNode toolNode = toolProgressionTree.GetToolNode(tool);
		if (toolNode != null)
		{
			return (int)((float)(toolNode.partMetadata.shiftCreditCost / 2) / (float)reactor.vrRigs.Count);
		}
		return 0;
	}

	public bool GetShiftCreditCost(ToolParts part, out int shiftCreditCost)
	{
		shiftCreditCost = 0;
		if (partMetadata.ContainsKey(part))
		{
			shiftCreditCost += partMetadata[part].shiftCreditCost;
			return true;
		}
		return false;
	}

	public void AttemptToUnlockPart(ToolParts part)
	{
		if (!IsPartUnlocked(part, out var unlocked) || unlocked)
		{
			return;
		}
		int numberOfResearchPoints = GetNumberOfResearchPoints();
		if (GetPartUnlockJuiceCost(part, out var juiceCost) && numberOfResearchPoints >= juiceCost && GetPartUnlockEmployeeRequiredLevel(part, out var level))
		{
			int requiredEmployeeLevel = GetRequiredEmployeeLevel(GetCurrentEmployeeLevel());
			int requiredEmployeeLevel2 = GetRequiredEmployeeLevel(level);
			if (requiredEmployeeLevel >= requiredEmployeeLevel2)
			{
				toolProgressionTree.AttemptToUnlockPart(part);
			}
		}
	}

	public bool IsPartUnlocked(ToolParts part, out bool unlocked)
	{
		unlocked = false;
		GRToolProgressionTree.GRToolProgressionNode partNode = toolProgressionTree.GetPartNode(part);
		if (partNode == null)
		{
			return false;
		}
		unlocked = partNode.unlocked;
		return true;
	}

	public bool GetPartUnlockEmployeeRequiredLevel(ToolParts part, out GRToolProgressionTree.EmployeeLevelRequirement level)
	{
		level = GRToolProgressionTree.EmployeeLevelRequirement.None;
		GRToolProgressionTree.GRToolProgressionNode partNode = toolProgressionTree.GetPartNode(part);
		if (partNode == null)
		{
			return false;
		}
		level = partNode.requiredEmployeeLevel;
		return true;
	}

	public bool GetPartUnlockJuiceCost(ToolParts part, out int juiceCost)
	{
		juiceCost = 0;
		GRToolProgressionTree.GRToolProgressionNode partNode = toolProgressionTree.GetPartNode(part);
		if (partNode == null)
		{
			return false;
		}
		juiceCost = partNode.researchCost;
		return true;
	}

	public bool GetPartUnlockRequiredParentParts(ToolParts part, out List<ToolParts> requiredParts)
	{
		requiredParts = new List<ToolParts>();
		GRToolProgressionTree.GRToolProgressionNode partNode = toolProgressionTree.GetPartNode(part);
		if (partNode == null)
		{
			return false;
		}
		foreach (GRToolProgressionTree.GRToolProgressionNode parent in partNode.parents)
		{
			requiredParts.Add(parent.type);
		}
		return true;
	}

	public bool GetPlayerShiftCredit(out int playerShiftCredit)
	{
		playerShiftCredit = 0;
		if (VRRig.LocalRig != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
			if (gRPlayer != null)
			{
				playerShiftCredit = gRPlayer.ShiftCredits;
				return true;
			}
		}
		return false;
	}

	public GRToolProgressionTree.EmployeeLevelRequirement GetCurrentEmployeeLevel()
	{
		return toolProgressionTree.GetCurrentEmploymentLevel();
	}

	public string GetTreeId()
	{
		return toolProgressionTree.GetTreeId();
	}

	public int GetDropPodLevel()
	{
		if (IsPartUnlocked(ToolParts.DropPodBasic, out var unlocked) && unlocked)
		{
			return 1;
		}
		return 0;
	}

	public int GetDropPodChasisLevel()
	{
		if (IsPartUnlocked(ToolParts.DropPodChassis3, out var unlocked) && unlocked)
		{
			return 3;
		}
		if (IsPartUnlocked(ToolParts.DropPodChassis2, out unlocked) && unlocked)
		{
			return 2;
		}
		if (IsPartUnlocked(ToolParts.DropPodChassis1, out unlocked) && unlocked)
		{
			return 1;
		}
		return 0;
	}

	public ProgressionManager.DrillUpgradeLevel GetDrillLevel()
	{
		if (IsPartUnlocked(ToolParts.DropPodChassis3, out var unlocked) && unlocked)
		{
			return ProgressionManager.DrillUpgradeLevel.Upgrade3;
		}
		if (IsPartUnlocked(ToolParts.DropPodChassis2, out unlocked) && unlocked)
		{
			return ProgressionManager.DrillUpgradeLevel.Upgrade2;
		}
		if (IsPartUnlocked(ToolParts.DropPodChassis1, out unlocked) && unlocked)
		{
			return ProgressionManager.DrillUpgradeLevel.Upgrade1;
		}
		if (IsPartUnlocked(ToolParts.DropPodBasic, out unlocked) && unlocked)
		{
			return ProgressionManager.DrillUpgradeLevel.Base;
		}
		return ProgressionManager.DrillUpgradeLevel.None;
	}

	public int GetJuiceCostForDrillUpgrade(ProgressionManager.DrillUpgradeLevel upgradeLevel)
	{
		int juiceCost = 0;
		switch (upgradeLevel)
		{
		case ProgressionManager.DrillUpgradeLevel.Upgrade1:
			GetPartUnlockJuiceCost(ToolParts.DropPodChassis1, out juiceCost);
			break;
		case ProgressionManager.DrillUpgradeLevel.Upgrade2:
			GetPartUnlockJuiceCost(ToolParts.DropPodChassis2, out juiceCost);
			break;
		case ProgressionManager.DrillUpgradeLevel.Upgrade3:
			GetPartUnlockJuiceCost(ToolParts.DropPodChassis3, out juiceCost);
			break;
		case ProgressionManager.DrillUpgradeLevel.Base:
			GetPartUnlockJuiceCost(ToolParts.DropPodBasic, out juiceCost);
			break;
		}
		return juiceCost;
	}

	public int GetSRCostForDrillUpgradeLevel(ProgressionManager.DrillUpgradeLevel level)
	{
		return level switch
		{
			ProgressionManager.DrillUpgradeLevel.Upgrade3 => 0, 
			ProgressionManager.DrillUpgradeLevel.Upgrade2 => 0, 
			ProgressionManager.DrillUpgradeLevel.Upgrade1 => 0, 
			ProgressionManager.DrillUpgradeLevel.Base => 3600, 
			_ => 0, 
		};
	}
}
