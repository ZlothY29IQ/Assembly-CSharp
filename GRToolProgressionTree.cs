using System;
using System.Collections.Generic;
using GorillaNetworking;

public class GRToolProgressionTree
{
	public enum EmployeeLevelRequirement
	{
		None,
		Intern,
		PartTime,
		FullTime
	}

	public class GRToolProgressionNode
	{
		public string id;

		public string name;

		public bool unlocked;

		public int researchCost;

		public bool rootNode;

		public GRToolProgressionManager.ToolParts type;

		public GRToolProgressionManager.ToolProgressionMetaData partMetadata;

		public List<GRToolProgressionNode> children = new List<GRToolProgressionNode>();

		public List<GRToolProgressionNode> parents = new List<GRToolProgressionNode>();

		public EmployeeLevelRequirement requiredEmployeeLevel;
	}

	private class GRToolProgressionRawNode
	{
		public GRToolProgressionNode progressionNode = new GRToolProgressionNode();

		public List<string> requiredByIds = new List<string>();

		public List<string> requiredEntitlements = new List<string>();
	}

	private string treeName = "GRTools";

	private string treeId = string.Empty;

	private string researchPointsEntitlement = "GR_ResearchPoints";

	private Dictionary<GRTool.GRToolType, GRToolProgressionNode> toolTree = new Dictionary<GRTool.GRToolType, GRToolProgressionNode>();

	private Dictionary<GRToolProgressionManager.ToolParts, GRToolProgressionNode> partTree = new Dictionary<GRToolProgressionManager.ToolParts, GRToolProgressionNode>();

	private Dictionary<string, GRToolProgressionRawNode> nodeTree = new Dictionary<string, GRToolProgressionRawNode>();

	private Dictionary<string, GRTool.GRToolType> toolMapping = new Dictionary<string, GRTool.GRToolType>();

	private Dictionary<string, GRToolProgressionManager.ToolParts> partMapping = new Dictionary<string, GRToolProgressionManager.ToolParts>();

	private string autoUnlockNodeId = string.Empty;

	private int currentResearchPoints;

	[NonSerialized]
	private GhostReactor reactor;

	[NonSerialized]
	private GRToolProgressionManager manager;

	[NonSerialized]
	private EmployeeLevelRequirement currentEmploymentLevel;

	private string internEntitlement = "Intern";

	private string partTimeEntitlement = "PartTime";

	private string fullTimeEntitlement = "FullTime";

	private GRToolProgressionManager.ToolParts pendingPartUnlock;

	public GRToolProgressionTree()
	{
		InitializeToolMapping();
		InitializeClubPartMapping();
		InitializeFlashPartMapping();
		InitializeRevivePartMapping();
		InitializeCollectorPartMapping();
		InitializeLanternPartMapping();
		InitializeShieldGunPartMapping();
		InitializeDirectionalShieldPartMapping();
		InitializeEnergyEfficiencyPartMapping();
		InitializeDockWristPartMapping();
		InitializeDropPodPartMapping();
	}

	public void Init(GhostReactor ghostReactor, GRToolProgressionManager toolManager)
	{
		reactor = ghostReactor;
		manager = toolManager;
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.OnTreeUpdated += OnProgressionTreeUpdate;
			ProgressionManager.Instance.OnInventoryUpdated += OnInventoryUpdated;
		}
		RefreshProgressionTree();
		RefreshUserInventory();
	}

	public string GetTreeId()
	{
		return treeId;
	}

	public List<GRTool.GRToolType> GetSupportedTools()
	{
		List<GRTool.GRToolType> list = new List<GRTool.GRToolType>();
		foreach (GRTool.GRToolType key in toolTree.Keys)
		{
			list.Add(key);
		}
		return list;
	}

	public List<GRToolProgressionNode> GetToolUpgrades(GRTool.GRToolType tool)
	{
		List<GRToolProgressionNode> list = new List<GRToolProgressionNode>();
		AddToolProgressionChildren(toolTree[tool], ref list);
		return list;
	}

	public GRToolProgressionNode GetToolNode(GRTool.GRToolType tool)
	{
		if (toolTree.ContainsKey(tool))
		{
			return toolTree[tool];
		}
		return null;
	}

	public GRToolProgressionNode GetPartNode(GRToolProgressionManager.ToolParts part)
	{
		if (partTree.ContainsKey(part))
		{
			return partTree[part];
		}
		return null;
	}

	public void RefreshProgressionTree()
	{
		ProgressionManager.Instance.RefreshProgressionTree();
	}

	public void RefreshUserInventory()
	{
		ProgressionManager.Instance.RefreshUserInventory();
	}

	private void OnProgressionTreeUpdate()
	{
		UserHydratedProgressionTreeResponse tree = ProgressionManager.Instance.GetTree(treeName);
		if (tree != null)
		{
			ProcessToolProgressionTree(tree);
		}
		manager?.SendMothershipUpdated();
	}

	private void OnInventoryUpdated()
	{
		if (ProgressionManager.Instance.GetInventoryItem(researchPointsEntitlement, out var item))
		{
			currentResearchPoints = item.Quantity;
		}
		ProgressionManager.MothershipItemSummary item3;
		ProgressionManager.MothershipItemSummary item4;
		if (ProgressionManager.Instance.GetInventoryItem(fullTimeEntitlement, out var _))
		{
			currentEmploymentLevel = EmployeeLevelRequirement.FullTime;
		}
		else if (ProgressionManager.Instance.GetInventoryItem(partTimeEntitlement, out item3))
		{
			currentEmploymentLevel = EmployeeLevelRequirement.PartTime;
		}
		else if (ProgressionManager.Instance.GetInventoryItem(internEntitlement, out item4))
		{
			currentEmploymentLevel = EmployeeLevelRequirement.Intern;
		}
		else
		{
			currentEmploymentLevel = EmployeeLevelRequirement.None;
		}
		manager?.SendMothershipUpdated();
	}

	public EmployeeLevelRequirement GetCurrentEmploymentLevel()
	{
		return currentEmploymentLevel;
	}

	private void AddToolProgressionChildren(GRToolProgressionNode currentNode, ref List<GRToolProgressionNode> list)
	{
		foreach (GRToolProgressionNode child in currentNode.children)
		{
			list.Add(child);
			AddToolProgressionChildren(child, ref list);
		}
	}

	public int GetNumberOfResearchPoints()
	{
		return currentResearchPoints;
	}

	private void InitializeToolMapping()
	{
		toolMapping["ChargeBaton"] = GRTool.GRToolType.Club;
		toolMapping["FlashTool"] = GRTool.GRToolType.Flash;
		toolMapping["Revive"] = GRTool.GRToolType.Revive;
		toolMapping["Collector"] = GRTool.GRToolType.Collector;
		toolMapping["Lantern"] = GRTool.GRToolType.Lantern;
		toolMapping["ShieldGun"] = GRTool.GRToolType.ShieldGun;
		toolMapping["DirectionalShield"] = GRTool.GRToolType.DirectionalShield;
		toolMapping["DockWrist"] = GRTool.GRToolType.DockWrist;
		toolMapping["EnergyEfficiency"] = GRTool.GRToolType.EnergyEfficiency;
		toolMapping["DropPodBasic"] = GRTool.GRToolType.DropPod;
	}

	private void InitializeClubPartMapping()
	{
		partMapping["ChargeBaton"] = GRToolProgressionManager.ToolParts.Baton;
		partMapping["BatonDamage1"] = GRToolProgressionManager.ToolParts.BatonDamage1;
		partMapping["BatonDamage2"] = GRToolProgressionManager.ToolParts.BatonDamage2;
		partMapping["BatonDamage3"] = GRToolProgressionManager.ToolParts.BatonDamage3;
	}

	private void InitializeFlashPartMapping()
	{
		partMapping["FlashTool"] = GRToolProgressionManager.ToolParts.Flash;
		partMapping["FlashDamage1"] = GRToolProgressionManager.ToolParts.FlashDamage1;
		partMapping["FlashDamage2"] = GRToolProgressionManager.ToolParts.FlashDamage2;
		partMapping["FlashDamage3"] = GRToolProgressionManager.ToolParts.FlashDamage3;
	}

	private void InitializeCollectorPartMapping()
	{
		partMapping["Collector"] = GRToolProgressionManager.ToolParts.Collector;
		partMapping["CollectorBonus1"] = GRToolProgressionManager.ToolParts.CollectorBonus1;
		partMapping["CollectorBonus2"] = GRToolProgressionManager.ToolParts.CollectorBonus2;
		partMapping["CollectorBonus3"] = GRToolProgressionManager.ToolParts.CollectorBonus3;
	}

	private void InitializeRevivePartMapping()
	{
		partMapping["Revive"] = GRToolProgressionManager.ToolParts.Revive;
	}

	private void InitializeLanternPartMapping()
	{
		partMapping["Lantern"] = GRToolProgressionManager.ToolParts.Lantern;
		partMapping["LanternIntensity1"] = GRToolProgressionManager.ToolParts.LanternIntensity1;
		partMapping["LanternIntensity2"] = GRToolProgressionManager.ToolParts.LanternIntensity2;
		partMapping["LanternIntensity3"] = GRToolProgressionManager.ToolParts.LanternIntensity3;
	}

	private void InitializeShieldGunPartMapping()
	{
		partMapping["ShieldGun"] = GRToolProgressionManager.ToolParts.ShieldGun;
		partMapping["ShieldGunStrength1"] = GRToolProgressionManager.ToolParts.ShieldGunStrength1;
		partMapping["ShieldGunStrength2"] = GRToolProgressionManager.ToolParts.ShieldGunStrength2;
		partMapping["ShieldGunStrength3"] = GRToolProgressionManager.ToolParts.ShieldGunStrength3;
	}

	private void InitializeDirectionalShieldPartMapping()
	{
		partMapping["DirectionalShield"] = GRToolProgressionManager.ToolParts.DirectionalShield;
		partMapping["DirectionalShieldSize1"] = GRToolProgressionManager.ToolParts.DirectionalShieldSize1;
		partMapping["DirectionalShieldSize2"] = GRToolProgressionManager.ToolParts.DirectionalShieldSize2;
		partMapping["DirectionalShieldSize3"] = GRToolProgressionManager.ToolParts.DirectionalShieldSize3;
	}

	private void InitializeEnergyEfficiencyPartMapping()
	{
		partMapping["EnergyEfficiency"] = GRToolProgressionManager.ToolParts.EnergyEff;
		partMapping["EnergyEff1"] = GRToolProgressionManager.ToolParts.EnergyEff1;
		partMapping["EnergyEff2"] = GRToolProgressionManager.ToolParts.EnergyEff2;
		partMapping["EnergyEff3"] = GRToolProgressionManager.ToolParts.EnergyEff3;
	}

	private void InitializeDockWristPartMapping()
	{
		partMapping["DockWrist"] = GRToolProgressionManager.ToolParts.DockWrist;
		partMapping["StatusWatch"] = GRToolProgressionManager.ToolParts.StatusWatch;
		partMapping["RattyBackpack"] = GRToolProgressionManager.ToolParts.RattyBackpack;
	}

	private void InitializeDropPodPartMapping()
	{
		partMapping["DropPodBasic"] = GRToolProgressionManager.ToolParts.DropPodBasic;
		partMapping["DropPodChassis01"] = GRToolProgressionManager.ToolParts.DropPodChassis1;
		partMapping["DropPodChassis02"] = GRToolProgressionManager.ToolParts.DropPodChassis2;
		partMapping["DropPodChassis03"] = GRToolProgressionManager.ToolParts.DropPodChassis3;
	}

	private void AddFakeNodes()
	{
		if (!toolTree.ContainsKey(GRTool.GRToolType.Club))
		{
			toolTree[GRTool.GRToolType.Club] = new GRToolProgressionNode
			{
				name = "Baton",
				unlocked = true,
				researchCost = 0,
				rootNode = true,
				type = GRToolProgressionManager.ToolParts.Baton,
				partMetadata = manager.GetPartMetadata(GRToolProgressionManager.ToolParts.Baton),
				requiredEmployeeLevel = EmployeeLevelRequirement.None
			};
		}
		if (!partTree.ContainsKey(GRToolProgressionManager.ToolParts.Baton))
		{
			partTree[GRToolProgressionManager.ToolParts.Baton] = toolTree[GRTool.GRToolType.Club];
		}
		if (!toolTree.ContainsKey(GRTool.GRToolType.EnergyEfficiency))
		{
			toolTree[GRTool.GRToolType.EnergyEfficiency] = new GRToolProgressionNode
			{
				name = "EnergyEfficiency",
				unlocked = true,
				researchCost = 0,
				rootNode = true,
				type = GRToolProgressionManager.ToolParts.EnergyEff,
				partMetadata = manager.GetPartMetadata(GRToolProgressionManager.ToolParts.EnergyEff),
				requiredEmployeeLevel = EmployeeLevelRequirement.None
			};
		}
		if (!partTree.ContainsKey(GRToolProgressionManager.ToolParts.EnergyEff))
		{
			partTree[GRToolProgressionManager.ToolParts.EnergyEff] = toolTree[GRTool.GRToolType.EnergyEfficiency];
		}
		if (!toolTree.ContainsKey(GRTool.GRToolType.Collector))
		{
			toolTree[GRTool.GRToolType.Collector] = new GRToolProgressionNode
			{
				name = "Collector",
				unlocked = true,
				researchCost = 0,
				rootNode = true,
				type = GRToolProgressionManager.ToolParts.Collector,
				partMetadata = manager.GetPartMetadata(GRToolProgressionManager.ToolParts.Collector),
				requiredEmployeeLevel = EmployeeLevelRequirement.None
			};
		}
		if (!partTree.ContainsKey(GRToolProgressionManager.ToolParts.Collector))
		{
			partTree[GRToolProgressionManager.ToolParts.Collector] = toolTree[GRTool.GRToolType.Collector];
		}
		if (!toolTree.ContainsKey(GRTool.GRToolType.Lantern))
		{
			toolTree[GRTool.GRToolType.Lantern] = new GRToolProgressionNode
			{
				name = "Lantern",
				unlocked = true,
				researchCost = 0,
				rootNode = true,
				type = GRToolProgressionManager.ToolParts.Lantern,
				partMetadata = manager.GetPartMetadata(GRToolProgressionManager.ToolParts.Lantern),
				requiredEmployeeLevel = EmployeeLevelRequirement.None
			};
		}
		if (!partTree.ContainsKey(GRToolProgressionManager.ToolParts.Lantern))
		{
			partTree[GRToolProgressionManager.ToolParts.Lantern] = toolTree[GRTool.GRToolType.Lantern];
		}
	}

	private void ProcessNodes()
	{
		foreach (KeyValuePair<string, GRToolProgressionRawNode> item in nodeTree)
		{
			GRToolProgressionRawNode value = item.Value;
			foreach (string requiredById in value.requiredByIds)
			{
				if (nodeTree.ContainsKey(requiredById))
				{
					nodeTree[requiredById].progressionNode.children.Add(value.progressionNode);
					value.progressionNode.parents.Add(nodeTree[requiredById].progressionNode);
				}
			}
			value.progressionNode.requiredEmployeeLevel = GetEmployeeLevel(value.requiredEntitlements);
			string key = value.progressionNode.name.Trim();
			if (toolMapping.ContainsKey(key))
			{
				GRTool.GRToolType key2 = toolMapping[key];
				value.progressionNode.rootNode = true;
				if (!value.progressionNode.unlocked && autoUnlockNodeId == string.Empty && value.progressionNode.researchCost == 0 && value.progressionNode.requiredEmployeeLevel == EmployeeLevelRequirement.None)
				{
					autoUnlockNodeId = value.progressionNode.id;
				}
				toolTree[key2] = value.progressionNode;
			}
			partTree[value.progressionNode.type] = value.progressionNode;
		}
	}

	private void PopulateMetadata()
	{
		foreach (KeyValuePair<string, GRToolProgressionRawNode> item in nodeTree)
		{
			item.Value.progressionNode.partMetadata = manager.GetPartMetadata(item.Value.progressionNode.type);
		}
	}

	private EmployeeLevelRequirement GetEmployeeLevel(List<string> rawRequiredEntitlements)
	{
		foreach (string rawRequiredEntitlement in rawRequiredEntitlements)
		{
			switch (rawRequiredEntitlement.Trim())
			{
			case "Intern":
				return EmployeeLevelRequirement.Intern;
			case "PartTime":
				return EmployeeLevelRequirement.PartTime;
			case "FullTime":
				return EmployeeLevelRequirement.FullTime;
			}
		}
		return EmployeeLevelRequirement.None;
	}

	private void ProcessTreeNode(UserHydratedNodeDefinition treeNode)
	{
		GRToolProgressionRawNode gRToolProgressionRawNode = new GRToolProgressionRawNode();
		gRToolProgressionRawNode.progressionNode.id = treeNode.id;
		gRToolProgressionRawNode.progressionNode.name = treeNode.name;
		gRToolProgressionRawNode.progressionNode.unlocked = treeNode.unlocked;
		if (partMapping.ContainsKey(gRToolProgressionRawNode.progressionNode.name))
		{
			if (toolMapping.ContainsKey(gRToolProgressionRawNode.progressionNode.name))
			{
				gRToolProgressionRawNode.progressionNode.rootNode = true;
			}
			gRToolProgressionRawNode.progressionNode.type = partMapping[gRToolProgressionRawNode.progressionNode.name];
		}
		if (treeNode.cost != null && treeNode.cost.items != null)
		{
			foreach (KeyValuePair<string, MothershipHydratedInventoryChange> item in treeNode.cost.items)
			{
				if (item.Key.Trim() == researchPointsEntitlement)
				{
					gRToolProgressionRawNode.progressionNode.researchCost = item.Value.Delta;
				}
			}
		}
		foreach (MothershipEntitlementCatalogItem prerequisite_entitlement in treeNode.prerequisite_entitlements)
		{
			gRToolProgressionRawNode.requiredEntitlements.Add(prerequisite_entitlement.name);
		}
		foreach (SWIGTYPE_p_std__variantT_MothershipApiShared__NodeReference_MothershipApiShared__ComplexPrerequisiteNodes_t node in treeNode.prerequisite_nodes.nodes)
		{
			ComplexPrerequisiteNodes value = new ComplexPrerequisiteNodes();
			NodeReference nodeReference = new NodeReference();
			if (!MothershipApi.TryGetComplexPrerequisiteNodeFromVariant(node, value) && MothershipApi.TryGetNodeReferenceFromVariant(node, nodeReference))
			{
				gRToolProgressionRawNode.requiredByIds.Add(nodeReference.node_id);
			}
		}
		if (pendingPartUnlock != GRToolProgressionManager.ToolParts.None && pendingPartUnlock == gRToolProgressionRawNode.progressionNode.type)
		{
			GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
			if (pendingPartUnlock == GRToolProgressionManager.ToolParts.DropPodBasic || pendingPartUnlock == GRToolProgressionManager.ToolParts.DropPodChassis1 || pendingPartUnlock == GRToolProgressionManager.ToolParts.DropPodChassis2 || pendingPartUnlock == GRToolProgressionManager.ToolParts.DropPodChassis3)
			{
				if (pendingPartUnlock != GRToolProgressionManager.ToolParts.DropPodBasic)
				{
					gRPlayer.SendPodUpgradeTelemetry(gRToolProgressionRawNode.progressionNode.name, treeNode.prerequisite_entitlements.Count, 0, gRToolProgressionRawNode.progressionNode.researchCost);
				}
			}
			else
			{
				gRPlayer.SendToolUpgradeTelemetry("Research", gRToolProgressionRawNode.progressionNode.name, treeNode.prerequisite_entitlements.Count, gRToolProgressionRawNode.progressionNode.researchCost, 0, 0);
			}
			pendingPartUnlock = GRToolProgressionManager.ToolParts.None;
		}
		nodeTree[gRToolProgressionRawNode.progressionNode.id] = gRToolProgressionRawNode;
	}

	private void ProcessToolProgressionTree(UserHydratedProgressionTreeResponse tree)
	{
		if (tree.Tree.name != treeName)
		{
			return;
		}
		toolTree = new Dictionary<GRTool.GRToolType, GRToolProgressionNode>();
		nodeTree = new Dictionary<string, GRToolProgressionRawNode>();
		treeId = tree.Tree.id;
		foreach (UserHydratedNodeDefinition node in tree.Nodes)
		{
			ProcessTreeNode(node);
		}
		PopulateMetadata();
		ProcessNodes();
		AddFakeNodes();
		if (autoUnlockNodeId != string.Empty)
		{
			string nodeId = autoUnlockNodeId;
			autoUnlockNodeId = string.Empty;
			GhostReactorProgression.instance.UnlockProgressionTreeNode(treeId, nodeId, reactor);
		}
		manager?.SendMothershipUpdated();
	}

	public void AttemptToUnlockPart(GRToolProgressionManager.ToolParts part)
	{
		if (partTree.ContainsKey(part))
		{
			pendingPartUnlock = part;
			GhostReactorProgression.instance.UnlockProgressionTreeNode(treeId, partTree[part].id, reactor);
		}
	}
}
