using System;
using System.Collections.Generic;
using System.Linq;
using GorillaGameModes;
using UnityEngine;

[Serializable]
public class SITechTreePage
{
	[SerializeField]
	private EAssetReleaseTier m_edReleaseTier = (EAssetReleaseTier)(-1);

	public string nickName;

	public SITechTreePageId pageId;

	public Sprite icon;

	public ESuperGameModes excludedGameModes;

	[SerializeField]
	private SITechTreeNode[] treeNodes;

	public float costMultiplier = 1f;

	public EAssetReleaseTier EdReleaseTier
	{
		get
		{
			return m_edReleaseTier;
		}
		set
		{
			m_edReleaseTier = value;
		}
	}

	public bool IsValid
	{
		get
		{
			EAssetReleaseTier edReleaseTier = m_edReleaseTier;
			if (edReleaseTier != EAssetReleaseTier.Disabled && edReleaseTier <= EAssetReleaseTier.PublicRC)
			{
				SITechTreeNode[] array = treeNodes;
				if (array == null)
				{
					return false;
				}
				return array.Length != 0;
			}
			return false;
		}
	}

	public bool IsAllowed => ((uint)excludedGameModes & (uint)GameMode.CurrentGameModeFlag) == 0;

	public List<GraphNode<SITechTreeNode>> Roots { get; private set; }

	public List<GraphNode<SITechTreeNode>> AllNodes { get; private set; }

	public List<SITechTreeNode> DispensableGadgets { get; private set; }

	public void ClearGraph()
	{
		Roots = null;
		AllNodes = null;
	}

	public void BuildGraph()
	{
		Roots = new List<GraphNode<SITechTreeNode>>();
		AllNodes = new List<GraphNode<SITechTreeNode>>();
		DispensableGadgets = new List<SITechTreeNode>();
		if (!IsValid)
		{
			return;
		}
		Dictionary<SIUpgradeType, GraphNode<SITechTreeNode>> nodeLookup = new Dictionary<SIUpgradeType, GraphNode<SITechTreeNode>>();
		SITechTreeNode[] array = treeNodes;
		foreach (SITechTreeNode sITechTreeNode in array)
		{
			if (sITechTreeNode.IsValid && (sITechTreeNode.parentUpgrades == null || sITechTreeNode.parentUpgrades.Length == 0))
			{
				Roots.Add(PopulateGraph(sITechTreeNode, excludedGameModes));
			}
		}
		foreach (GraphNode<SITechTreeNode> allNode in AllNodes)
		{
			if (allNode.Value.IsDispensableGadget)
			{
				DispensableGadgets.Add(allNode.Value);
			}
		}
		GraphNode<SITechTreeNode> PopulateGraph(SITechTreeNode node, ESuperGameModes parentExcludedGameModes)
		{
			node.excludedGameModes |= parentExcludedGameModes;
			if (!nodeLookup.TryGetValue(node.upgradeType, out var value))
			{
				value = new GraphNode<SITechTreeNode>(node);
				nodeLookup.Add(node.upgradeType, value);
				AllNodes.Add(value);
			}
			SIUpgradeType upgradeType = node.upgradeType;
			SITechTreeNode[] array2 = treeNodes;
			foreach (SITechTreeNode sITechTreeNode2 in array2)
			{
				if (sITechTreeNode2.IsValid && sITechTreeNode2.parentUpgrades != null)
				{
					SIUpgradeType[] parentUpgrades = sITechTreeNode2.parentUpgrades;
					for (int k = 0; k < parentUpgrades.Length; k++)
					{
						if (parentUpgrades[k] == upgradeType)
						{
							GraphNode<SITechTreeNode> graphNode = PopulateGraph(sITechTreeNode2, node.excludedGameModes);
							if (!value.Children.Contains(graphNode))
							{
								value.AddChild(graphNode);
							}
						}
					}
				}
			}
			return value;
		}
	}

	public void PrintGraph()
	{
		foreach (GraphNode<SITechTreeNode> root in Roots)
		{
			foreach (GraphNode<SITechTreeNode> item in root.TraversePreOrderDistinct())
			{
				Debug.Log("[SI] Graph node: " + item.Value.nickName + " [" + NodeListText(item.Parents) + "]");
			}
		}
		static string NodeListText(List<GraphNode<SITechTreeNode>> nodes)
		{
			return string.Join("|", nodes.Select((GraphNode<SITechTreeNode> n) => n.Value.nickName));
		}
	}
}
