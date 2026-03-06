using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SITechTreeUIPage : MonoBehaviour
{
	[SerializeField]
	private SITechTreeUINode nodePrefab;

	[SerializeField]
	private Image upgradeLinePrefab;

	[SerializeField]
	private RectTransform nodeContainer;

	public SITechTreePageId id;

	private readonly List<SITechTreeUINode> _pageNodes = new List<SITechTreeUINode>();

	public void Configure(SITechTreeStation techTreeStation, SITechTreePage treePage, Transform imageTarget, Transform textTarget)
	{
		base.name = treePage.nickName;
		id = treePage.pageId;
		int count = treePage.Roots.Count;
		Vector3 vector = new Vector3(0f, nodeContainer.rect.min.y + 20f, 0f);
		if (count < 2)
		{
			float num = treePage.Roots[0].GetSubtreeWidth() * 50 + 100;
			if (num > nodeContainer.rect.width)
			{
				float num2 = (nodeContainer.rect.width - num) / 2f;
				vector.x += num2;
			}
		}
		float num3 = nodeContainer.rect.width / (float)count;
		for (int i = 0; i < count; i++)
		{
			float x = ((count < 2) ? 0f : (-22f + (0f - num3) * (float)(count - 1) / 2f + num3 * (float)i));
			AddNodes(null, treePage.Roots[i], vector + new Vector3(x, 0f, 0f));
		}
		foreach (SITechTreeUINode pageNode in _pageNodes)
		{
			AddUpgradeLines(pageNode);
			pageNode.SetNodeLockStateColor(Color.black);
			techTreeStation.AddButton(pageNode.button);
		}
		void AddNodes(GraphNode<SITechTreeNode> parent, GraphNode<SITechTreeNode> node, Vector3 position)
		{
			float num4 = ((parent == null) ? 40 : 25);
			int num5 = ((parent == null) ? 10 : 5);
			List<float> subtreeWidths = new List<float>();
			float num6 = 50f;
			foreach (GraphNode<SITechTreeNode> child in node.Children)
			{
				subtreeWidths.Add(num6 * (float)child.GetSubtreeWidth());
			}
			SITechTreeUINode sITechTreeUINode = GetOrInstantiateUINode(node.Value.upgradeType);
			if (parent != null)
			{
				SITechTreeUINode uINode = GetUINode(parent.Value.upgradeType);
				sITechTreeUINode.Parents.Add(uINode);
				uINode.Children.Add(sITechTreeUINode);
			}
			if (sITechTreeUINode.IsConfigured)
			{
				if (sITechTreeUINode.Parents.Count > 1)
				{
					float num7 = 0f;
					foreach (SITechTreeUINode parent in sITechTreeUINode.Parents)
					{
						num7 += parent.transform.localPosition.x;
					}
					position.x = num7 / (float)sITechTreeUINode.Parents.Count;
				}
				position.y = Mathf.Max(sITechTreeUINode.transform.localPosition.y, position.y);
				sITechTreeUINode.AdjustPosition(position - sITechTreeUINode.transform.localPosition);
			}
			else
			{
				sITechTreeUINode.transform.localPosition = position;
				sITechTreeUINode.SetTechTreeNode(techTreeStation, node.Value.upgradeType);
				_pageNodes.Add(sITechTreeUINode);
				int count2 = node.Children.Count;
				float num8 = 0f;
				if (count2 > 1)
				{
					int index = 0;
					for (int j = 0; j < count2; j++)
					{
						float num9 = subtreeWidths[index];
						float num10 = ((j == 0 || j == count2 - 1) ? (num9 / 2f) : num9);
						num8 -= num10 / 2f;
					}
				}
				for (int k = 0; k < count2; k++)
				{
					float y = num4 + (float)((k + 1) % 2 * num5);
					GraphNode<SITechTreeNode> node2 = node.Children[k];
					Vector3 position2 = position + new Vector3(num8, y, 0f);
					AddNodes(node, node2, position2);
					num8 += GetSpacing(k, count2);
				}
				sITechTreeUINode.imageFlattener.overrideParentTransform = imageTarget;
				sITechTreeUINode.textFlattener.overrideParentTransform = textTarget;
				sITechTreeUINode.imageFlattener.enabled = true;
				sITechTreeUINode.textFlattener.enabled = true;
			}
			float GetSpacing(int num12, int childCount)
			{
				int num11 = num12 + 1;
				float num13 = ((num12 >= 0 && num12 < childCount) ? subtreeWidths[num12] : 0f);
				float num14 = ((num11 >= 0 && num11 < childCount) ? subtreeWidths[num11] : 0f);
				return (num13 + num14) / 2f;
			}
		}
		void AddUpgradeLines(SITechTreeUINode uiNode)
		{
			foreach (SITechTreeUINode parent2 in uiNode.Parents)
			{
				Vector3 localPosition = parent2.transform.localPosition;
				Vector3 vector2 = uiNode.transform.localPosition - localPosition;
				Vector3 normalized = vector2.normalized;
				Image image = Object.Instantiate(upgradeLinePrefab, nodeContainer);
				ObjectHierarchyFlattener component = image.GetComponent<ObjectHierarchyFlattener>();
				image.transform.SetSiblingIndex(0);
				uiNode.UpgradeLines.Add(image);
				RectTransform rectTransform = image.rectTransform;
				rectTransform.localPosition = localPosition + vector2 * 0.5f;
				rectTransform.localRotation = Quaternion.FromToRotation(Vector3.up, normalized);
				Vector2 sizeDelta = rectTransform.sizeDelta;
				sizeDelta.y = vector2.magnitude - 20f;
				rectTransform.sizeDelta = sizeDelta;
				component.overrideParentTransform = imageTarget;
				component.enabled = true;
			}
		}
		SITechTreeUINode GetOrInstantiateUINode(SIUpgradeType upgradeType)
		{
			SITechTreeUINode uINode = GetUINode(upgradeType);
			if ((bool)uINode)
			{
				return uINode;
			}
			return Object.Instantiate(nodePrefab, nodeContainer);
		}
	}

	private SITechTreeUINode GetUINode(SIUpgradeType upgradeType)
	{
		foreach (SITechTreeUINode pageNode in _pageNodes)
		{
			if (pageNode.upgradeType == upgradeType)
			{
				return pageNode;
			}
		}
		return null;
	}

	public void PopulateDefaultNodeData()
	{
		foreach (SITechTreeUINode pageNode in _pageNodes)
		{
			pageNode.SetNodeLockStateColor(Color.black);
		}
	}

	public void PopulatePlayerNodeData(SIPlayer player)
	{
		foreach (SITechTreeUINode pageNode in _pageNodes)
		{
			Color nodeLockStateColor = (player.NodeResearched(pageNode.upgradeType) ? Color.green : (player.NodeParentsUnlocked(pageNode.upgradeType) ? Color.red : Color.black));
			pageNode.SetNodeLockStateColor(nodeLockStateColor);
		}
	}
}
