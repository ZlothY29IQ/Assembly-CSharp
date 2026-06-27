using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GRToolUpgradeStation : MonoBehaviour
{
	private enum UpgradeStationState
	{
		Idle,
		ItemInserted,
		Upgrading,
		Complete
	}

	private GRTool insertedTool;

	private GRTool.GRToolType insertedToolType;

	private GameEntity insertedToolEntity;

	[NonSerialized]
	private GhostReactor _reactor;

	[NonSerialized]
	private GRToolProgressionManager toolProgressionManager;

	[NonSerialized]
	private List<GRToolProgressionTree.GRToolProgressionNode> selectedToolUpgrades = new List<GRToolProgressionTree.GRToolProgressionNode>();

	[NonSerialized]
	public bool bIsToolInserted;

	public Transform startingLocation;

	public Transform upgradingLocation;

	public Transform depositedLocation;

	public Transform ejectionTransform;

	public float ejectionVelocity;

	public Color selectedColor;

	public Color unSelectedColor;

	public Color lockedColor;

	public Color unlockedColor;

	public TMP_Text[] UpgradeTitlesText;

	public TMP_Text[] MFD_ButtonTexts;

	public GorillaPressableButton[] UpgradeButtons;

	public Image[] UpgradeLockedImage;

	public TMP_Text ToolNameText;

	public TMP_Text DescriptionText;

	public TMP_Text CostText;

	private string defaultCostText;

	public IDCardScanner IDCardScanner;

	private int selectedUpgradeIndex;

	private double upgradeStartTime;

	public double upgradeAnimationLength;

	public Vector3 rotationAnimation;

	private UpgradeStationState currentState;

	public GameEntity attachedItem;

	public bool canInsertTool
	{
		get
		{
			if (currentState == UpgradeStationState.Idle)
			{
				return !bIsToolInserted;
			}
			return false;
		}
	}

	public void Init(GRToolProgressionManager tree, GhostReactor reactor)
	{
		_reactor = reactor;
		defaultCostText = CostText.text;
		toolProgressionManager = tree;
		toolProgressionManager.OnProgressionUpdated += ResearchTreeUpdated;
		ResetScreen();
	}

	public void ResearchTreeUpdated()
	{
		UpdateUI();
	}

	public void Update()
	{
		if (currentState == UpgradeStationState.Upgrading)
		{
			UpgradingUpdate(PhotonNetwork.Time);
		}
	}

	public void ToolInserted(GRTool tool)
	{
		if (canInsertTool)
		{
			bIsToolInserted = true;
			insertedTool = tool;
			insertedToolType = insertedTool.toolType;
			selectedToolUpgrades = toolProgressionManager.GetToolUpgrades(insertedToolType);
			ResetScreen();
			UpdateUI();
			SelectUpgrade(0);
			LocalPlacedToolInUpgradeStation(tool.gameEntity.id);
		}
	}

	public void UpdateUI()
	{
		UpdateUpgradeTexts();
		UpdateSelectedUpgrade();
	}

	public void UpdateUpgradeTexts()
	{
		ToolNameText.text = GRUtils.GetToolName(insertedToolType);
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			if (selectedToolUpgrades.Count > i)
			{
				UpgradeTitlesText[i].text = selectedToolUpgrades[i].partMetadata.name;
			}
			else
			{
				UpgradeTitlesText[i].text = null;
			}
		}
	}

	public void UnlockAllUpgrades()
	{
	}

	public void UpdateSelectedUpgrade()
	{
		if (selectedToolUpgrades != null && selectedToolUpgrades.Count > selectedUpgradeIndex && selectedToolUpgrades[selectedUpgradeIndex] != null)
		{
			if (selectedToolUpgrades[selectedUpgradeIndex].unlocked)
			{
				DescriptionText.text = selectedToolUpgrades[selectedUpgradeIndex].partMetadata.description;
				int researchCost = selectedToolUpgrades[selectedUpgradeIndex].researchCost;
				CostText.text = string.Format(defaultCostText, researchCost.ToString());
				GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
				CostText.color = ((researchCost > gRPlayer.ShiftCredits) ? lockedColor : unlockedColor);
			}
			else
			{
				CostText.text = "NEEDS RESEARCH";
				CostText.color = lockedColor;
			}
		}
	}

	public void ResetScreen()
	{
		DescriptionText.text = "PLEASE INSERT A TOOL";
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			UpgradeTitlesText[i].text = "----";
			UpgradeTitlesText[i].color = lockedColor;
			MFD_ButtonTexts[i].color = unSelectedColor;
		}
		ToolNameText.text = "----";
		CostText.text = "-";
		ToolNameText.color = unSelectedColor;
		DescriptionText.color = unSelectedColor;
		CostText.color = unSelectedColor;
	}

	public void SelectUpgrade(int index)
	{
		if (index >= selectedToolUpgrades.Count)
		{
			return;
		}
		selectedUpgradeIndex = index;
		for (int i = 0; i < UpgradeTitlesText.Length; i++)
		{
			if (i < selectedToolUpgrades.Count)
			{
				bool unlocked = selectedToolUpgrades[i].unlocked;
				UpgradeTitlesText[i].color = (unlocked ? unlockedColor : lockedColor);
				UpgradeLockedImage[i].gameObject.SetActive(!unlocked);
			}
			else
			{
				UpgradeLockedImage[i].gameObject.SetActive(value: true);
				UpgradeTitlesText[i].color = lockedColor;
			}
			UpgradeButtons[i].isOn = false;
			UpgradeButtons[i].UpdateColor();
		}
		if (selectedToolUpgrades != null && selectedToolUpgrades.Count > selectedUpgradeIndex && selectedToolUpgrades[selectedUpgradeIndex] != null)
		{
			UpgradeButtons[selectedUpgradeIndex].isOn = true;
			UpgradeButtons[selectedUpgradeIndex].UpdateColor();
			DescriptionText.color = UpgradeTitlesText[selectedUpgradeIndex].color;
			CostText.color = UpgradeTitlesText[selectedUpgradeIndex].color;
		}
		UpdateUI();
	}

	public void UpgradeTool()
	{
		_reactor.grManager.ToolUpgradeStationRequestUpgrade(selectedToolUpgrades[selectedUpgradeIndex].type, insertedToolEntity.GetNetId());
	}

	public void LocalPlacedToolInUpgradeStation(GameEntityId entityId)
	{
		GameEntity gameEntity = _reactor.grManager.gameEntityManager.GetGameEntity(entityId);
		currentState = UpgradeStationState.ItemInserted;
		if (gameEntity.heldByActorNumber >= 0)
		{
			GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
			int handIndex = gamePlayer.FindHandIndex(entityId);
			gamePlayer.ClearGrabbedIfHeld(entityId, gameEntity.manager);
			if (gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				GamePlayerLocal.instance.gamePlayer.ClearGrabbed(handIndex);
				GamePlayerLocal.instance.ClearGrabbed(handIndex);
			}
			gameEntity.heldByActorNumber = -1;
			gameEntity.heldByHandIndex = -1;
			gameEntity.OnReleased?.Invoke();
			PositionInsertedTool(gameEntity);
			SelectUpgrade(0);
		}
	}

	public void PositionInsertedTool(GameEntity entity)
	{
		insertedToolEntity = entity;
		entity.transform.SetParent(startingLocation);
		entity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		Rigidbody component = entity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = startingLocation.position;
			component.rotation = startingLocation.rotation;
			component.linearVelocity = Vector3.zero;
			component.angularVelocity = Vector3.zero;
		}
		entity.pickupable = false;
	}

	public void PayForUpgrade(int Player)
	{
		if (Player == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			int researchCost = selectedToolUpgrades[selectedUpgradeIndex].researchCost;
			GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
			bool num = researchCost <= gRPlayer.ShiftCredits;
			bool unlocked = selectedToolUpgrades[selectedUpgradeIndex].unlocked;
			if (num && unlocked)
			{
				IDCardScanner.onSucceeded?.Invoke();
				StartUpgrade(PhotonNetwork.Time);
			}
		}
	}

	public void StartUpgrade(double startTime)
	{
		if (currentState == UpgradeStationState.ItemInserted)
		{
			upgradeStartTime = startTime;
			insertedToolEntity.transform.SetParent(startingLocation);
			insertedToolEntity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			currentState = UpgradeStationState.Upgrading;
		}
	}

	public void UpgradingUpdate(double currentTime)
	{
		if (currentTime >= upgradeStartTime + upgradeAnimationLength)
		{
			CompleteUpgrade();
		}
	}

	public void CompleteUpgrade()
	{
		currentState = UpgradeStationState.Complete;
		ResetScreen();
		MoveToolToFinished();
	}

	public void MoveItemToUpgradeSlot()
	{
		insertedToolEntity.transform.SetParent(upgradingLocation);
		insertedToolEntity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		Rigidbody component = insertedToolEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = upgradingLocation.position;
			component.rotation = upgradingLocation.rotation;
			component.linearVelocity = Vector3.zero;
			component.angularVelocity = Vector3.zero;
		}
		insertedToolEntity.pickupable = false;
	}

	public void MoveToolToFinished()
	{
		insertedToolEntity.transform.SetParent(depositedLocation);
		insertedToolEntity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		currentState = UpgradeStationState.Complete;
		Rigidbody component = insertedToolEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = startingLocation.position;
			component.rotation = startingLocation.rotation;
			component.linearVelocity = ejectionTransform.forward * ejectionVelocity;
			component.angularVelocity = Vector3.zero;
		}
		insertedToolEntity.pickupable = true;
		UpgradeTool();
		EjectToolFromEnd();
		ResetScreen();
	}

	public void EjectToolFromStart()
	{
		insertedToolEntity.transform.SetParent(startingLocation);
		insertedToolEntity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		insertedToolEntity.transform.SetParent(null, worldPositionStays: true);
		Rigidbody component = insertedToolEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = startingLocation.position;
			component.rotation = startingLocation.rotation;
			component.linearVelocity = ejectionTransform.forward * ejectionVelocity;
			component.angularVelocity = Vector3.zero;
		}
		insertedToolEntity.pickupable = true;
		insertedToolEntity = null;
		insertedTool = null;
		insertedToolType = GRTool.GRToolType.None;
		bIsToolInserted = false;
		ResetScreen();
		currentState = UpgradeStationState.Idle;
	}

	public void EjectToolFromEnd()
	{
		insertedToolEntity.transform.SetParent(depositedLocation);
		insertedToolEntity.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		insertedToolEntity.transform.SetParent(null, worldPositionStays: true);
		Rigidbody component = insertedToolEntity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = depositedLocation.position;
			component.rotation = depositedLocation.rotation;
			component.linearVelocity = ejectionTransform.forward * ejectionVelocity;
			component.angularVelocity = Vector3.zero;
		}
		insertedToolEntity.pickupable = true;
		insertedToolEntity = null;
		insertedTool = null;
		insertedToolType = GRTool.GRToolType.None;
		bIsToolInserted = false;
		currentState = UpgradeStationState.Idle;
	}
}
