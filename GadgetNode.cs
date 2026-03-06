using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

public class GadgetNode : TechTreeNodeBase
{
	[Input(ShowBackingValue.Unconnected, ConnectionType.Multiple, TypeConstraint.None, false)]
	public Empty input;

	[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.None, false)]
	public Empty output;

	public SIUpgradeType upgradeType;

	public string nickName;

	[TextArea]
	public string description;

	public SIResource.ResourceCost[] nodeCost;

	public bool costOverride;

	[Header("Prefab")]
	public GameEntity unlockedGadgetPrefab;

	public ESuperGameModes excludedGameModes;

	public EAssetReleaseTier releaseTier = (EAssetReleaseTier)(-1);

	private static bool InEditor => NodeInspectorBridge.InNodeEditor;

	public bool IsValid
	{
		get
		{
			EAssetReleaseTier eAssetReleaseTier = releaseTier;
			if (eAssetReleaseTier != EAssetReleaseTier.Disabled)
			{
				return eAssetReleaseTier <= EAssetReleaseTier.PublicRC;
			}
			return false;
		}
	}

	public bool IsDispensableGadget => unlockedGadgetPrefab;

	private bool ShowGadgetPrefab
	{
		get
		{
			if (InEditor)
			{
				return IsDispensableGadget;
			}
			return true;
		}
	}

	private bool ShowExcludedGameModes
	{
		get
		{
			if (InEditor)
			{
				return excludedGameModes != (ESuperGameModes)0;
			}
			return true;
		}
	}

	private bool ShowReleaseTier
	{
		get
		{
			if (InEditor)
			{
				return releaseTier != EAssetReleaseTier.PublicRC;
			}
			return true;
		}
	}

	public void ConfigureFrom(SITechTreeNode sourceNode)
	{
		releaseTier = sourceNode.EdReleaseTier;
		upgradeType = sourceNode.upgradeType;
		nickName = sourceNode.nickName;
		description = sourceNode.description;
		unlockedGadgetPrefab = sourceNode.unlockedGadgetPrefab;
		excludedGameModes = sourceNode.excludedGameModes;
		nodeCost = sourceNode.nodeCost.ToArray();
		costOverride = sourceNode.costOverride;
		base.name = nickName;
	}

	public void AssignParentUpgrades(SIUpgradeType[] prerequisites)
	{
		NodePort port = GetPort("input");
		port.ClearConnections();
		foreach (SIUpgradeType id in prerequisites)
		{
			GadgetNode gadgetNode = graph.nodes.FirstOrDefault(delegate(Node n)
			{
				GadgetNode obj = n as GadgetNode;
				return (object)obj != null && obj.upgradeType == id;
			}) as GadgetNode;
			if (gadgetNode != null)
			{
				NodePort port2 = gadgetNode.GetPort("output");
				port.Connect(port2);
			}
		}
	}

	public List<SIUpgradeType> GetParentUpgradeTypes()
	{
		List<SIUpgradeType> list = new List<SIUpgradeType>();
		foreach (Node item in from n in GetPort("input").GetConnections()
			select n.node)
		{
			if (item is GadgetNode gadgetNode)
			{
				list.Add(gadgetNode.upgradeType);
			}
		}
		return list;
	}

	public SITechTreeNode GenerateTechTreeNode()
	{
		return new SITechTreeNode
		{
			upgradeType = upgradeType,
			nickName = nickName,
			description = description,
			unlockedGadgetPrefab = unlockedGadgetPrefab,
			nodeCost = nodeCost.ToArray(),
			excludedGameModes = excludedGameModes,
			EdReleaseTier = releaseTier,
			parentUpgrades = GetParentUpgradeTypes().ToArray()
		};
	}

	public int GetDepth()
	{
		int num = 0;
		IEnumerable<NodePort> enumerable = GetInputPort("input")?.GetConnections();
		foreach (NodePort item in enumerable ?? Enumerable.Empty<NodePort>())
		{
			if (item.node is GadgetNode gadgetNode)
			{
				num = Mathf.Max(num, gadgetNode.GetDepth() + 1);
			}
		}
		return num;
	}

	public int GetTreeDepth()
	{
		int num = GetDepth();
		foreach (NodePort connection in GetOutputPort("output").GetConnections())
		{
			if (connection.node is GadgetNode gadgetNode)
			{
				num = Mathf.Max(num, gadgetNode.GetTreeDepth());
			}
		}
		return num;
	}

	public List<GadgetNode> GetTreeNodes()
	{
		List<GadgetNode> list = new List<GadgetNode>();
		GetTreeNodes(list);
		return list;
	}

	public void GetTreeNodes(List<GadgetNode> nodes)
	{
		nodes.Add(this);
		foreach (NodePort connection in GetOutputPort("output").GetConnections())
		{
			if (connection.node is GadgetNode gadgetNode)
			{
				gadgetNode.GetTreeNodes(nodes);
			}
		}
	}

	public List<GadgetNode> GetParentNodes()
	{
		List<GadgetNode> list = new List<GadgetNode>();
		foreach (NodePort connection in GetPort("input").GetConnections())
		{
			if (connection.node is GadgetNode item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public List<GadgetNode> GetChildNodes()
	{
		List<GadgetNode> list = new List<GadgetNode>();
		foreach (NodePort connection in GetOutputPort("output").GetConnections())
		{
			if (connection.node is GadgetNode item)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public int GetTreeWidth()
	{
		List<GadgetNode> childNodes = GetChildNodes();
		if (childNodes.Count == 0)
		{
			return 1;
		}
		int num = 0;
		foreach (GadgetNode item in childNodes)
		{
			num += item.GetTreeWidth();
		}
		return num;
	}

	public bool CostEquals(SIResource.ResourceCost[] cost)
	{
		if (cost.Length != nodeCost.Length)
		{
			return false;
		}
		for (int i = 0; i < cost.Length; i++)
		{
			if (!cost[i].Equals(nodeCost[i]))
			{
				return false;
			}
		}
		return true;
	}
}
