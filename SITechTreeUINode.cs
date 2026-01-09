using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SITechTreeUINode : MonoBehaviour
{
	public SIUpgradeType upgradeType;

	public TextMeshProUGUI nodeNickName;

	public MeshRenderer circle;

	public MeshRenderer triangle;

	public SITouchscreenButton button;

	public Material greenMat;

	public Material redMat;

	public Material blackMat;

	public ObjectHierarchyFlattener imageFlattener;

	public ObjectHierarchyFlattener textFlattener;

	private GraphNode<SITechTreeNode> _node;

	public List<Image> UpgradeLines { get; } = new List<Image>();

	public List<SITechTreeUINode> Parents { get; } = new List<SITechTreeUINode>();

	public List<SITechTreeUINode> Children { get; } = new List<SITechTreeUINode>();

	public bool IsConfigured => _node != null;

	public void SetTechTreeNode(SITechTreeStation techTreeStation, SIUpgradeType nodeUpgradeType)
	{
		if (!techTreeStation.techTreeSO.TryGetNode(nodeUpgradeType, out _node))
		{
			Debug.LogError($"Node {nodeUpgradeType} doesn't exist in tree.  Disabling.");
			base.gameObject.SetActive(value: false);
			return;
		}
		upgradeType = nodeUpgradeType;
		float num = Mathf.Min(GetMaxWordLength(_node.Value.nickName), 14) * 4;
		Vector2 sizeDelta = nodeNickName.rectTransform.sizeDelta;
		if (sizeDelta.x < num)
		{
			sizeDelta.x = num;
			nodeNickName.rectTransform.sizeDelta = sizeDelta;
		}
		string text = (nodeNickName.text = _node.Value.nickName);
		base.name = text;
		button.data = _node.Value.upgradeType.GetNodeId();
		button.buttonPressed.RemoveAllListeners();
		button.buttonPressed.AddListener(techTreeStation.TouchscreenButtonPressed);
		SetGadgetUnlockNode(_node.Value.unlockedGadgetPrefab);
	}

	public void SetNodeLockStateColor(Color color)
	{
		if (color == Color.red)
		{
			circle.sharedMaterial = redMat;
		}
		else if (color == Color.black)
		{
			circle.sharedMaterial = blackMat;
		}
		else if (color == Color.green)
		{
			circle.sharedMaterial = greenMat;
		}
		foreach (Image upgradeLine in UpgradeLines)
		{
			upgradeLine.color = color;
		}
	}

	private void SetGadgetUnlockNode(bool isUnlockNode)
	{
		triangle.gameObject.SetActive(isUnlockNode);
	}

	private int GetMaxWordLength(string text)
	{
		string[] array = text.Split(' ');
		int num = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Length > num)
			{
				num = text2.Length;
			}
		}
		return num;
	}

	public void AdjustPosition(Vector3 positionOffset)
	{
		base.transform.localPosition += positionOffset;
		foreach (SITechTreeUINode child in Children)
		{
			child.AdjustPosition(positionOffset);
		}
	}
}
