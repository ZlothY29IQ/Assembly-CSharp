using System;
using System.Collections.Generic;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GRResearchStation : MonoBehaviour
{
	public Color selectedUpgradeColor = Color.yellow;

	public Color unselectedUpgradeColor = Color.black;

	public Color lockedToolColor = Color.red;

	public Color unlockedToolColor = Color.green;

	private int selectedUpgradeIndex;

	[SerializeField]
	private IDCardScanner scanner;

	[SerializeField]
	private TMP_Text BonusText;

	[SerializeField]
	private TMP_Text CostText;

	[SerializeField]
	private TMP_Text DescriptionText;

	[SerializeField]
	private TMP_Text LevelText;

	[SerializeField]
	private TMP_Text ResearchPointsTex;

	[SerializeField]
	private TMP_Text RequiredLevelText;

	[SerializeField]
	private TMP_Text ToolNameText;

	[SerializeField]
	private TMP_Text UnlockedText;

	[SerializeField]
	private TMP_Text[] UpgradePointerText;

	[SerializeField]
	private TMP_Text[] UpgradeTitlesText;

	[SerializeField]
	private Image[] LockedImage;

	[SerializeField]
	private GorillaPressableButton[] UpgradeButton;

	private string _costString;

	private string _levelString;

	private string _researchPointsString;

	private string _requiredLevelString;

	private int selectedToolIndex;

	private int totalTools;

	[NonSerialized]
	private GRToolProgressionManager toolProgressionManager;

	[NonSerialized]
	private List<GRTool.GRToolType> supportedTools = new List<GRTool.GRToolType>();

	[NonSerialized]
	private List<GRToolProgressionTree.GRToolProgressionNode> selectedToolUpgrades = new List<GRToolProgressionTree.GRToolProgressionNode>();

	[NonSerialized]
	private GRToolProgressionTree.GRToolProgressionNode currentlySelectedToolUpgrade = new GRToolProgressionTree.GRToolProgressionNode();

	[NonSerialized]
	private GRToolProgressionManager.ToolProgressionMetaData currentlySelectedUpgradeMetadata = new GRToolProgressionManager.ToolProgressionMetaData();

	[NonSerialized]
	private GhostReactor reactor;

	public void Init(GRToolProgressionManager tree, GhostReactor ghostReactor)
	{
		toolProgressionManager = tree;
		toolProgressionManager.OnProgressionUpdated += ResearchTreeUpdated;
		reactor = ghostReactor;
		totalTools = 0;
		selectedToolIndex = 0;
		_levelString = LevelText.text;
		_costString = CostText.text;
		_researchPointsString = ResearchPointsTex.text;
		_requiredLevelString = RequiredLevelText.text;
		UpdateUI();
		SelectTool(0);
	}

	private void SelectTool(int index)
	{
		if (!(toolProgressionManager == null) && totalTools != 0 && index < totalTools && index > -1)
		{
			selectedToolIndex = index;
			selectedToolUpgrades = toolProgressionManager.GetToolUpgrades(supportedTools[selectedToolIndex]);
			SelectUpgrade(0);
			UpdateUI();
		}
	}

	public void ResearchTreeUpdated()
	{
		supportedTools = toolProgressionManager.GetSupportedTools();
		totalTools = supportedTools.Count;
		SelectTool(selectedToolIndex);
		UpdateUI();
	}

	public void UpdateUI()
	{
		UpdateToolName();
		UpdateUpgradeTitles();
		UpdateLocked();
		UpdateRequiredLevel();
		UpdateCost();
		UpdateResearchPoints(toolProgressionManager.GetNumberOfResearchPoints());
	}

	public void SelectUpgrade(int UpgradeIndex)
	{
		if (!(toolProgressionManager == null))
		{
			selectedUpgradeIndex = UpgradeIndex;
			if (selectedToolUpgrades.Count > selectedUpgradeIndex)
			{
				currentlySelectedToolUpgrade = selectedToolUpgrades[selectedUpgradeIndex];
				currentlySelectedUpgradeMetadata = currentlySelectedToolUpgrade.partMetadata;
				SetUpgradeTextColors(selectedUpgradeIndex);
				UpdateDescriptionText(currentlySelectedUpgradeMetadata.description);
			}
			UpdateUI();
		}
	}

	private void SetUpgradeTextColors(int index)
	{
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			UpgradeButton[i].isOn = false;
			UpgradeButton[i].UpdateColor();
		}
		UpgradeButton[index].isOn = true;
		UpgradeButton[index].UpdateColor();
	}

	private void UpdateUpgradeTitles()
	{
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			if (totalTools >= selectedToolIndex && selectedToolUpgrades.Count > i)
			{
				UpgradeTitlesText[i].text = selectedToolUpgrades[i].partMetadata.name;
			}
			else
			{
				UpgradeTitlesText[i].text = null;
			}
		}
	}

	public void UpdateLocked()
	{
		if (currentlySelectedToolUpgrade.unlocked)
		{
			UnlockedText.color = unlockedToolColor;
			UnlockedText.text = "UNLOCKED";
		}
		else
		{
			UnlockedText.color = lockedToolColor;
			UnlockedText.text = "LOCKED";
		}
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			if (totalTools >= selectedToolIndex && selectedToolUpgrades.Count > i)
			{
				bool unlocked = selectedToolUpgrades[i].unlocked;
				UpgradeTitlesText[i].color = (unlocked ? unlockedToolColor : lockedToolColor);
				LockedImage[i].gameObject.SetActive(!unlocked);
			}
			else
			{
				UpgradeTitlesText[i].color = Color.black;
				LockedImage[i].gameObject.SetActive(value: true);
			}
		}
	}

	public void UpdateRequiredLevel()
	{
		int requiredEmployeeLevel = toolProgressionManager.GetRequiredEmployeeLevel(currentlySelectedToolUpgrade.requiredEmployeeLevel);
		string titleNameFromLevel = GhostReactorProgression.GetTitleNameFromLevel(requiredEmployeeLevel);
		int num = 0;
		GRPlayer gRPlayer = GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber);
		if (gRPlayer != null)
		{
			num = GhostReactorProgression.GetTitleLevel(gRPlayer.CurrentProgression.redeemedPoints);
		}
		string titleNameFromLevel2 = GhostReactorProgression.GetTitleNameFromLevel(num);
		RequiredLevelText.text = string.Format(_requiredLevelString, titleNameFromLevel);
		LevelText.text = string.Format(_levelString, titleNameFromLevel2);
		RequiredLevelText.color = ((num >= requiredEmployeeLevel) ? unlockedToolColor : lockedToolColor);
	}

	public void UpdateDescriptionText(string description)
	{
		DescriptionText.text = description;
	}

	public void UpdateCost()
	{
		if (selectedToolUpgrades != null && selectedToolUpgrades.Count > 0 && selectedToolUpgrades.Count > selectedUpgradeIndex)
		{
			int numberOfResearchPoints = toolProgressionManager.GetNumberOfResearchPoints();
			int researchCost = selectedToolUpgrades[selectedUpgradeIndex].researchCost;
			CostText.text = string.Format(_costString, researchCost);
			CostText.color = ((numberOfResearchPoints >= researchCost) ? unlockedToolColor : lockedToolColor);
		}
	}

	public void UpdateToolName()
	{
		if (supportedTools.Count > 0)
		{
			ToolNameText.text = GRUtils.GetToolName(supportedTools[selectedToolIndex]);
		}
	}

	public void UpdateResearchPoints(int ResearchPoints)
	{
		ResearchPointsTex.text = string.Format(_researchPointsString, ResearchPoints);
	}

	public void MFDButton0Pressed()
	{
		SelectUpgrade(0);
	}

	public void MFDButton1Pressed()
	{
		SelectUpgrade(1);
	}

	public void MFDButton2Pressed()
	{
		SelectUpgrade(2);
	}

	public void MFDButton3Pressed()
	{
		SelectUpgrade(3);
	}

	public void MFDButton4Pressed()
	{
		SelectUpgrade(4);
	}

	public void MFDButton5Pressed()
	{
		SelectUpgrade(5);
	}

	public void NextToolButtonPressed()
	{
		selectedToolIndex = (selectedToolIndex + 1) % totalTools;
		SelectTool(selectedToolIndex);
	}

	public void PreviousToolButtonPressed()
	{
		selectedToolIndex = (selectedToolIndex - 1).PositiveModulo(totalTools);
		SelectTool(selectedToolIndex);
	}

	public void UpgradeButtonPressed()
	{
		scanner.onSucceeded?.Invoke();
		GhostReactorProgression.instance.UnlockProgressionTreeNode(toolProgressionManager.GetTreeId(), currentlySelectedToolUpgrade.id, reactor);
	}

	public void ResearchCompleted(bool success, string researchID)
	{
		UpdateUI();
	}
}
