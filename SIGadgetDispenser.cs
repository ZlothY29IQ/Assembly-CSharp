using System;
using System.Collections.Generic;
using System.IO;
using GorillaTag;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SIGadgetDispenser : MonoBehaviour, ITouchScreenStation
{
	public enum GadgetDispenserTerminalState
	{
		WaitingForScan,
		GadgetType,
		GadgetList,
		GadgetInformation,
		GadgetDispensed,
		HelpScreen
	}

	public GadgetDispenserTerminalState handScannedState = GadgetDispenserTerminalState.GadgetList;

	public GadgetDispenserTerminalState currentState;

	public GadgetDispenserTerminalState lastState;

	public Transform gadgetDispensePosition;

	public int _currentNode;

	public SICombinedTerminal parentTerminal;

	[Header("TryOn")]
	[SerializeField]
	private bool m_isTryOn;

	[SerializeField]
	private GameEntityDelayedDestroy.Options m_tryOnOptions = new GameEntityDelayedDestroy.Options
	{
		delay = 30f,
		explosionVolume = 1f,
		beepVolume = 1f,
		beepPhases = new GameEntityDelayedDestroy.BeepPhase[3]
		{
			new GameEntityDelayedDestroy.BeepPhase
			{
				timeRemaining = 10f,
				interval = 1f
			},
			new GameEntityDelayedDestroy.BeepPhase
			{
				timeRemaining = 5f,
				interval = 0.5f
			},
			new GameEntityDelayedDestroy.BeepPhase
			{
				timeRemaining = 2f,
				interval = 0.25f
			}
		}
	};

	internal static GameEntityDelayedDestroy.Options g_tryOnOptions = new GameEntityDelayedDestroy.Options
	{
		delay = 1f
	};

	public GameObject waitingForScanScreen;

	public GameObject gadgetTypeScreen;

	public GameObject gadgetListScreen;

	public GameObject gadgetInformationScreen;

	public GameObject gadgetDispensedScreen;

	public GameObject gadgetsHelpScreen;

	[SerializeField]
	private SIScreenRegion screenRegion;

	[Header("Main Screen Shared")]
	public TextMeshProUGUI screenDescription;

	public Image background;

	public Color active;

	public Color notActive;

	public Transform uiCenter;

	[Header("Popup Shared")]
	public GameObject popupScreen;

	[Header("Gadgets Type")]
	[SerializeField]
	private RectTransform pageListParent;

	[SerializeField]
	private SIGadgetListEntry pageListEntryPrefab;

	private List<SIGadgetListEntry> gadgetPages = new List<SIGadgetListEntry>();

	[FormerlySerializedAs("noDispensableGadgetsNotif")]
	[Header("Gadgets List")]
	[SerializeField]
	private GameObject noDispensableGadgetsMessage;

	[SerializeField]
	private RectTransform gadgetListParent;

	[SerializeField]
	private SIDispenserGadgetListEntry gadgetListEntryPrefab;

	private List<SIDispenserGadgetListEntry> gadgetEntries;

	[Header("Gadgets Description")]
	public TextMeshProUGUI gadgetDescriptionText;

	[Header("Gadget Dispensed")]
	public TextMeshProUGUI gadgetDispensedText;

	[Header("Help")]
	public int helpScreenIndex;

	public GameObject[] helpPopupScreens;

	[Header("Audio")]
	[SerializeField]
	private SoundBankPlayer touchSoundBankPlayer;

	[SerializeField]
	private SoundBankPlayer dispenseSoundBankPlayer;

	[Header("Main Screen Colliders")]
	[Tooltip("Button colliders to disable while popup screen is shown.  Gets updated live to include page and gadget buttons.")]
	[SerializeField]
	private List<Collider> _nonPopupButtonColliders;

	private Dictionary<GadgetDispenserTerminalState, GameObject> screenData;

	private bool initialized;

	internal bool isTryOn => m_isTryOn;

	public SIScreenRegion ScreenRegion => screenRegion;

	public SIPlayer ActivePlayer => parentTerminal.activePlayer;

	public string ActivePlayerName => ActivePlayer.gamePlayer.rig.Creator.SanitizedNickName;

	public bool IsAuthority => parentTerminal.superInfection.siManager.gameEntityManager.IsAuthority();

	public SuperInfectionManager SIManager => parentTerminal.superInfection.siManager;

	public GameEntityManager GameEntityManager => parentTerminal.superInfection.siManager.gameEntityManager;

	public SITechTreeNode CurrentNode => parentTerminal.superInfection.techTreeSO.GetTreeNode(parentTerminal.ActivePage, _currentNode);

	public SITechTreePage CurrentPage => parentTerminal.superInfection.techTreeSO.GetTreePage((SITechTreePageId)parentTerminal.ActivePage);

	public SITechTreeSO TechTreeSO => parentTerminal.superInfection.techTreeSO;

	protected void OnEnable()
	{
		if (m_isTryOn)
		{
			g_tryOnOptions = m_tryOnOptions;
		}
		_RefreshButtonsUsableState();
	}

	private void _RefreshButtonsUsableState()
	{
		foreach (SIGadgetListEntry gadgetPage in gadgetPages)
		{
			SITechTreePageId id = (SITechTreePageId)gadgetPage.Id;
			if (TechTreeSO.TryGetTreePage(id, out var treePage))
			{
				gadgetPage.ButtonContainer.SetUsable(treePage.IsAllowed);
			}
		}
	}

	private void SetNonPopupButtonsEnabled(bool enable)
	{
		foreach (Collider nonPopupButtonCollider in _nonPopupButtonColliders)
		{
			nonPopupButtonCollider.enabled = enable;
		}
	}

	public void Initialize()
	{
		if (!initialized)
		{
			initialized = true;
			if (parentTerminal == null)
			{
				parentTerminal = GetComponentInParent<SICombinedTerminal>();
			}
			screenData = new Dictionary<GadgetDispenserTerminalState, GameObject>();
			screenData.Add(GadgetDispenserTerminalState.WaitingForScan, waitingForScanScreen);
			screenData.Add(GadgetDispenserTerminalState.GadgetType, gadgetTypeScreen);
			screenData.Add(GadgetDispenserTerminalState.GadgetList, gadgetListScreen);
			screenData.Add(GadgetDispenserTerminalState.GadgetInformation, gadgetInformationScreen);
			screenData.Add(GadgetDispenserTerminalState.GadgetDispensed, gadgetDispensedScreen);
			screenData.Add(GadgetDispenserTerminalState.HelpScreen, gadgetsHelpScreen);
			parentTerminal.superInfection.techTreeSO.EnsureInitialized();
			int num = 0;
			int count = parentTerminal.superInfection.techTreeSO.TreePages.Count;
			for (int i = 0; i < count; i++)
			{
				SITechTreePage sITechTreePage = parentTerminal.superInfection.techTreeSO.TreePages[i];
				SIGadgetListEntry sIGadgetListEntry = UnityEngine.Object.Instantiate(pageListEntryPrefab, pageListParent);
				StaticLodManager.TryAddLateInstantiatedMembers(sIGadgetListEntry.gameObject);
				sIGadgetListEntry.Configure(this, sITechTreePage, parentTerminal.zeroZeroImage, parentTerminal.onePointTwoText, SITouchscreenButton.SITouchscreenButtonType.Select, i, -0.07f, count);
				gadgetPages.Add(sIGadgetListEntry);
				num = Math.Max(num, sITechTreePage.DispensableGadgets.Count);
			}
			gadgetEntries = new List<SIDispenserGadgetListEntry>();
			for (int j = 0; j < num; j++)
			{
				SIDispenserGadgetListEntry sIDispenserGadgetListEntry = UnityEngine.Object.Instantiate(gadgetListEntryPrefab, gadgetListParent);
				sIDispenserGadgetListEntry.transform.localPosition += new Vector3(0f, (float)j * -0.07f, 0f);
				sIDispenserGadgetListEntry.SetStation(this, parentTerminal.zeroZeroImage, parentTerminal.onePointTwoText);
				gadgetEntries.Add(sIDispenserGadgetListEntry);
			}
			if (m_isTryOn && base.isActiveAndEnabled)
			{
				g_tryOnOptions = m_tryOnOptions;
			}
			_RefreshButtonsUsableState();
			Reset();
		}
	}

	public void Reset()
	{
		currentState = GadgetDispenserTerminalState.WaitingForScan;
		SetScreenVisibility(currentState, currentState);
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy)
		{
			UpdateState(GadgetDispenserTerminalState.WaitingForScan, GadgetDispenserTerminalState.WaitingForScan);
		}
		stream.SendNext(helpScreenIndex);
		stream.SendNext(_currentNode);
		stream.SendNext((int)currentState);
		stream.SendNext((int)lastState);
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		helpScreenIndex = Mathf.Clamp((int)stream.ReceiveNext(), 0, helpPopupScreens.Length - 1);
		_currentNode = (int)stream.ReceiveNext();
		if (CurrentNode == null && CurrentPage != null && CurrentPage.AllNodes.Count > 0 && CurrentPage.AllNodes[0].Value != null)
		{
			_currentNode = (int)CurrentPage.AllNodes[0].Value.upgradeType;
		}
		GadgetDispenserTerminalState gadgetDispenserTerminalState = (GadgetDispenserTerminalState)stream.ReceiveNext();
		GadgetDispenserTerminalState gadgetDispenserTerminalState2 = (GadgetDispenserTerminalState)stream.ReceiveNext();
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(GadgetDispenserTerminalState), gadgetDispenserTerminalState) || !Enum.IsDefined(typeof(GadgetDispenserTerminalState), gadgetDispenserTerminalState2))
		{
			UpdateState(GadgetDispenserTerminalState.WaitingForScan, GadgetDispenserTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(gadgetDispenserTerminalState, gadgetDispenserTerminalState2);
		}
	}

	public void ZoneDataSerializeWrite(BinaryWriter writer)
	{
		writer.Write(helpScreenIndex);
		writer.Write(_currentNode);
		writer.Write((int)currentState);
		writer.Write((int)lastState);
	}

	public void ZoneDataSerializeRead(BinaryReader reader)
	{
		helpScreenIndex = Mathf.Clamp(reader.ReadInt32(), 0, helpPopupScreens.Length - 1);
		int value = reader.ReadInt32();
		if (CurrentPage != null && CurrentPage.AllNodes != null)
		{
			_currentNode = Mathf.Clamp(value, 0, CurrentPage.AllNodes.Count - 1);
		}
		else
		{
			_currentNode = 0;
		}
		GadgetDispenserTerminalState gadgetDispenserTerminalState = (GadgetDispenserTerminalState)reader.ReadInt32();
		GadgetDispenserTerminalState gadgetDispenserTerminalState2 = (GadgetDispenserTerminalState)reader.ReadInt32();
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(GadgetDispenserTerminalState), gadgetDispenserTerminalState) || !Enum.IsDefined(typeof(GadgetDispenserTerminalState), gadgetDispenserTerminalState2))
		{
			UpdateState(GadgetDispenserTerminalState.WaitingForScan, GadgetDispenserTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(gadgetDispenserTerminalState, gadgetDispenserTerminalState2);
		}
	}

	public void UpdateState(GadgetDispenserTerminalState newState, GadgetDispenserTerminalState newLastState)
	{
		if (!IsPopupState(newLastState))
		{
			currentState = newLastState;
		}
		UpdateState(newState);
	}

	public void UpdateState(GadgetDispenserTerminalState newState)
	{
		if (!IsPopupState(currentState))
		{
			lastState = currentState;
		}
		currentState = newState;
		SetScreenVisibility(currentState, lastState);
		switch (currentState)
		{
		case GadgetDispenserTerminalState.GadgetType:
			screenDescription.text = "GADGET TYPES";
			break;
		case GadgetDispenserTerminalState.GadgetList:
			screenDescription.text = "UNLOCKED " + CurrentPage.nickName + " GADGETS";
			UpdateGadgetListVisibility();
			break;
		case GadgetDispenserTerminalState.GadgetInformation:
			screenDescription.text = CurrentNode.nickName;
			gadgetDescriptionText.text = CurrentNode.description;
			break;
		case GadgetDispenserTerminalState.GadgetDispensed:
			gadgetDispensedText.text = ActivePlayerName + " HAS DISPENSED A " + CurrentNode.nickName + "!";
			break;
		case GadgetDispenserTerminalState.HelpScreen:
			UpdateHelpButtonPage(helpScreenIndex);
			break;
		case GadgetDispenserTerminalState.WaitingForScan:
			break;
		}
	}

	public void SetScreenVisibility(GadgetDispenserTerminalState currentState, GadgetDispenserTerminalState lastState)
	{
		bool flag = IsPopupState(currentState);
		background.color = ((currentState == GadgetDispenserTerminalState.WaitingForScan) ? Color.white : ((ActivePlayer != null && ActivePlayer.gamePlayer.IsLocal()) ? active : notActive));
		foreach (GadgetDispenserTerminalState key in screenData.Keys)
		{
			bool flag2 = key == currentState || (flag && key == lastState);
			if (screenData[key].activeSelf != flag2)
			{
				screenData[key].SetActive(flag2);
			}
		}
		if (popupScreen.activeSelf != flag)
		{
			popupScreen.SetActive(flag);
		}
		screenDescription.gameObject.SetActive(currentState != GadgetDispenserTerminalState.WaitingForScan);
		SetNonPopupButtonsEnabled(!flag);
	}

	public void UpdateGadgetListVisibility()
	{
		foreach (SIDispenserGadgetListEntry gadgetEntry in gadgetEntries)
		{
			gadgetEntry.gameObject.SetActive(value: false);
		}
		int num = 0;
		foreach (SITechTreeNode dispensableGadget in CurrentPage.DispensableGadgets)
		{
			if (m_isTryOn || ActivePlayer.CurrentProgression.IsUnlocked(dispensableGadget.upgradeType))
			{
				SIDispenserGadgetListEntry sIDispenserGadgetListEntry = gadgetEntries[num++];
				sIDispenserGadgetListEntry.SetTechTreeNode(dispensableGadget);
				sIDispenserGadgetListEntry.gameObject.SetActive(value: true);
				sIDispenserGadgetListEntry.DispenseButton.SetUsable(m_isTryOn || dispensableGadget.IsAllowed);
			}
		}
		noDispensableGadgetsMessage.SetActive(num == 0);
	}

	public bool IsPopupState(GadgetDispenserTerminalState state)
	{
		if (state != GadgetDispenserTerminalState.GadgetDispensed)
		{
			return state == GadgetDispenserTerminalState.HelpScreen;
		}
		return true;
	}

	public void PlayerHandScanned(int actorNr)
	{
		UpdateState(handScannedState);
	}

	public void AddButton(SITouchscreenButton button, bool isPopupButton = false)
	{
		if (!isPopupButton)
		{
			_nonPopupButtonColliders.Add(button.GetComponent<Collider>());
		}
	}

	public void TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr)
	{
		if (actorNr == SIPlayer.LocalPlayer.ActorNr && (ActivePlayer == null || ActivePlayer != SIPlayer.LocalPlayer))
		{
			parentTerminal.PlayWrongPlayerBuzz(uiCenter);
		}
		else
		{
			touchSoundBankPlayer.Play();
		}
		if (!IsAuthority)
		{
			parentTerminal.TouchscreenButtonPressed(buttonType, data, actorNr, SICombinedTerminal.TerminalSubFunction.GadgetDispenser);
		}
		else
		{
			if (actorNr != ActivePlayer.ActorNr)
			{
				return;
			}
			touchSoundBankPlayer.Play();
			switch (currentState)
			{
			case GadgetDispenserTerminalState.WaitingForScan:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(GadgetDispenserTerminalState.HelpScreen);
				}
				break;
			case GadgetDispenserTerminalState.GadgetType:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Select)
				{
					parentTerminal.SetActivePage(data);
				}
				break;
			case GadgetDispenserTerminalState.GadgetList:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(GadgetDispenserTerminalState.HelpScreen);
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Back)
				{
					UpdateState(GadgetDispenserTerminalState.GadgetType);
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Select)
				{
					SITechTreeNode treeNode = TechTreeSO.GetTreeNode((int)CurrentPage.pageId, data);
					if (treeNode != null && treeNode.IsDispensableGadget)
					{
						_currentNode = data;
						UpdateState(GadgetDispenserTerminalState.GadgetInformation);
					}
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Dispense)
				{
					SITechTreeNode treeNode2 = TechTreeSO.GetTreeNode((int)CurrentPage.pageId, data);
					if (treeNode2 != null && treeNode2.IsDispensableGadget)
					{
						_currentNode = data;
						AuthorityDispenseGadgetForPlayer(ActivePlayer);
						UpdateState(GadgetDispenserTerminalState.GadgetDispensed);
					}
				}
				break;
			case GadgetDispenserTerminalState.GadgetInformation:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(GadgetDispenserTerminalState.HelpScreen);
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Back)
				{
					UpdateState(GadgetDispenserTerminalState.GadgetList);
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Dispense)
				{
					AuthorityDispenseGadgetForPlayer(ActivePlayer);
					UpdateState(GadgetDispenserTerminalState.GadgetDispensed);
				}
				break;
			case GadgetDispenserTerminalState.GadgetDispensed:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Exit)
				{
					UpdateState(lastState);
				}
				break;
			case GadgetDispenserTerminalState.HelpScreen:
				switch (buttonType)
				{
				case SITouchscreenButton.SITouchscreenButtonType.Exit:
					helpScreenIndex = 0;
					UpdateState(lastState);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Next:
					helpScreenIndex = Mathf.Clamp(helpScreenIndex + 1, 0, helpPopupScreens.Length - 1);
					UpdateHelpButtonPage(helpScreenIndex);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Back:
					helpScreenIndex = Mathf.Clamp(helpScreenIndex - 1, 0, helpPopupScreens.Length - 1);
					UpdateHelpButtonPage(helpScreenIndex);
					break;
				}
				break;
			}
		}
	}

	public void TouchscreenToggleButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr, bool isToggledOn)
	{
	}

	public void UpdateHelpButtonPage(int helpButtonPageIndex)
	{
		for (int i = 0; i < helpPopupScreens.Length; i++)
		{
			helpPopupScreens[i].SetActive(i == helpButtonPageIndex);
		}
	}

	public void AuthorityDispenseGadgetForPlayer(SIPlayer player)
	{
		if (!IsAuthority)
		{
			return;
		}
		int num = 0;
		int staticHash = CurrentNode.unlockedGadgetPrefab.name.GetStaticHash();
		for (int num2 = player.activePlayerGadgets.Count - 1; num2 >= 0; num2--)
		{
			GameEntity gameEntityFromNetId = GameEntityManager.GetGameEntityFromNetId(player.activePlayerGadgets[num2]);
			if (gameEntityFromNetId == null)
			{
				player.activePlayerGadgets.RemoveAt(num2);
			}
			else
			{
				num++;
				if (num >= player.TotalGadgetLimit)
				{
					GameEntityManager.RequestDestroyItem(gameEntityFromNetId.id);
					break;
				}
			}
		}
		SIUpgradeSet upgrades = player.GetUpgrades(CurrentPage.pageId);
		int num3 = 0;
		foreach (GraphNode<SITechTreeNode> allNode in CurrentPage.AllNodes)
		{
			num3 |= 1 << allNode.Value.upgradeType.GetNodeId();
		}
		upgrades.SetBits(m_isTryOn ? num3 : (upgrades.GetBits() & num3));
		foreach (SITechTreeNode dispensableGadget in CurrentPage.DispensableGadgets)
		{
			if (dispensableGadget != CurrentNode)
			{
				upgrades.Remove(dispensableGadget.upgradeType);
			}
		}
		long num4 = upgrades.GetCreateData(player);
		if (m_isTryOn)
		{
			num4 |= long.MinValue;
		}
		GameEntityManager.RequestCreateItem(staticHash, gadgetDispensePosition.position, gadgetDispensePosition.rotation, num4);
		dispenseSoundBankPlayer.Play();
	}

	public void SetActivePage()
	{
		if (CurrentNode == null)
		{
			_currentNode = CurrentPage.AllNodes[0].Value.upgradeType.GetNodeId();
		}
		if (ActivePlayer != null)
		{
			UpdateState(GadgetDispenserTerminalState.GadgetList);
		}
		else
		{
			UpdateState(GadgetDispenserTerminalState.WaitingForScan);
		}
	}

	public bool IsValidPage(int pageId)
	{
		if (pageId < 0)
		{
			return false;
		}
		foreach (SIGadgetListEntry gadgetPage in gadgetPages)
		{
			if (gadgetPage.Id == pageId)
			{
				return true;
			}
		}
		return false;
	}
}
