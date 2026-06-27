using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GorillaTag;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class SITechTreeStation : MonoBehaviour, ITouchScreenStation
{
	public enum NodePopupState
	{
		Description,
		NotEnoughResources,
		Success,
		PurchaseInitiation,
		Loading
	}

	public enum TechTreeStationTerminalState
	{
		WaitingForScan,
		TechTreePagesList,
		TechTreePage,
		TechTreeNodePopup,
		HelpScreen
	}

	private const string preLog = "[GT/SITechTreeStation]  ";

	private const string preErr = "ERROR!!!  ";

	private Dictionary<TechTreeStationTerminalState, GameObject> screenData;

	public TechTreeStationTerminalState currentState;

	public TechTreeStationTerminalState lastState;

	public SICombinedTerminal parentTerminal;

	public Sprite techPointSprite;

	public Sprite strangeWoodSprite;

	public Sprite weirdGearSprite;

	public Sprite vibratingSpringSprite;

	public Sprite bouncySandSprite;

	public Sprite floppyMetalSprite;

	public int currentNodeId;

	public SITechTreeSO techTreeSO;

	public GameObject waitingForScanScreen;

	public GameObject pagesListScreen;

	public GameObject pageScreen;

	public GameObject nodePopupScreen;

	public GameObject techTreeHelpScreen;

	[SerializeField]
	private SIScreenRegion screenRegion;

	public Color active;

	public Color notActive;

	[Header("Main Screen Shared")]
	public TextMeshProUGUI screenDescriptionText;

	public TextMeshProUGUI playerNameText;

	public Image background;

	public Transform uiCenter;

	[Header("Popup Shared")]
	public GameObject popupScreen;

	[Header("Pages List")]
	[SerializeField]
	private Transform pageListParent;

	[SerializeField]
	private SIGadgetListEntry pageListEntryPrefab;

	private List<SIGadgetListEntry> pageButtons = new List<SIGadgetListEntry>(11);

	[Header("Tree Page")]
	[SerializeField]
	private Transform pageParent;

	[SerializeField]
	private SITechTreeUIPage pagePrefab;

	private List<SITechTreeUIPage> techTreePages = new List<SITechTreeUIPage>(11);

	[SerializeField]
	private SpriteRenderer techTreeIcon;

	[Header("Node Popup")]
	public GameObject[] nodePopupScreens;

	[Header("Research Node Description")]
	public TextMeshProUGUI nodeNameText;

	public TextMeshProUGUI nodeDescriptionText;

	public TextMeshProUGUI nodeResourceTypeText;

	public TextMeshProUGUI nodeResourceCostText;

	public TextMeshProUGUI playerCurrentResourceAmountsText;

	public GameObject nodeAvailable;

	public GameObject nodeLocked;

	public GameObject nodeResearched;

	public GameObject canAffordNode;

	public GameObject cantAffordNode;

	public GameObject nodeResearchButton;

	public SpriteRenderer techPointCost;

	public SpriteRenderer resourceCost;

	[Header("Research Attempt")]
	public TextMeshProUGUI nodeNameResearchMessageText;

	public NodePopupState nodePopupState;

	[Header("Help")]
	public int helpScreenIndex;

	public GameObject[] helpPopupScreens;

	[Header("Audio")]
	[SerializeField]
	private SoundBankPlayer soundBankPlayer;

	[Header("Main Screen Colliders")]
	[Tooltip("Button colliders to disable while popup screen is shown.  Gets updated live to include page and gadget node buttons.")]
	[SerializeField]
	private List<Collider> _nonPopupButtonColliders;

	private Dictionary<SIResource.ResourceType, Sprite> spriteByType = new Dictionary<SIResource.ResourceType, Sprite>();

	private Dictionary<SITechTreePageId, Sprite> techTreeIconById = new Dictionary<SITechTreePageId, Sprite>();

	private bool initialized;

	public SIScreenRegion ScreenRegion => screenRegion;

	public SITechTreeNode CurrentNode => techTreeSO.GetTreeNode(parentTerminal.ActivePage, currentNodeId);

	public SITechTreePage CurrentPage => parentTerminal.superInfection.techTreeSO.GetTreePage((SITechTreePageId)parentTerminal.ActivePage);

	public SIPlayer ActivePlayer => parentTerminal.activePlayer;

	public string ActivePlayerName => ActivePlayer.gamePlayer.rig.Creator?.SanitizedNickName;

	public bool IsAuthority => parentTerminal.superInfection.siManager.gameEntityManager.IsAuthority();

	public GameEntityManager GameEntityManager => parentTerminal.superInfection.siManager.gameEntityManager;

	public SuperInfectionManager SIManager => parentTerminal.superInfection.siManager;

	private void CollectButtonColliders()
	{
		List<SITouchscreenButton> buttons = GetComponentsInChildren<SITouchscreenButton>(includeInactive: true).ToList();
		RemoveButtonsInside((from d in GetComponentsInChildren<DestroyIfNotBeta>()
			select d.gameObject).ToArray());
		RemoveButtonsInside(new GameObject[2] { techTreeHelpScreen, nodePopupScreen });
		_nonPopupButtonColliders = buttons.Select((SITouchscreenButton b) => b.GetComponent<Collider>()).ToList();
		void RemoveButtonsInside(GameObject[] roots)
		{
			for (int i = 0; i < roots.Length; i++)
			{
				SITouchscreenButton[] componentsInChildren = roots[i].GetComponentsInChildren<SITouchscreenButton>(includeInactive: true);
				foreach (SITouchscreenButton item in componentsInChildren)
				{
					buttons.Remove(item);
				}
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

	private void OnEnable()
	{
		SIProgression instance = SIProgression.Instance;
		instance.OnTreeReady = (Action)Delegate.Combine(instance.OnTreeReady, new Action(OnProgressionUpdate));
		SIProgression instance2 = SIProgression.Instance;
		instance2.OnInventoryReady = (Action)Delegate.Combine(instance2.OnInventoryReady, new Action(OnProgressionUpdate));
		SIProgression instance3 = SIProgression.Instance;
		instance3.OnNodeUnlocked = (Action<SIUpgradeType>)Delegate.Combine(instance3.OnNodeUnlocked, new Action<SIUpgradeType>(OnProgressionUpdateNode));
		_RefreshButtonsUsableState();
	}

	private void OnDisable()
	{
		SIProgression instance = SIProgression.Instance;
		instance.OnTreeReady = (Action)Delegate.Remove(instance.OnTreeReady, new Action(OnProgressionUpdate));
		SIProgression instance2 = SIProgression.Instance;
		instance2.OnInventoryReady = (Action)Delegate.Remove(instance2.OnInventoryReady, new Action(OnProgressionUpdate));
		SIProgression instance3 = SIProgression.Instance;
		instance3.OnNodeUnlocked = (Action<SIUpgradeType>)Delegate.Remove(instance3.OnNodeUnlocked, new Action<SIUpgradeType>(OnProgressionUpdateNode));
	}

	public void Initialize()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		if (parentTerminal == null)
		{
			parentTerminal = GetComponentInParent<SICombinedTerminal>();
		}
		screenData = new Dictionary<TechTreeStationTerminalState, GameObject>();
		screenData.Add(TechTreeStationTerminalState.WaitingForScan, waitingForScanScreen);
		screenData.Add(TechTreeStationTerminalState.TechTreePagesList, pagesListScreen);
		screenData.Add(TechTreeStationTerminalState.TechTreePage, pageScreen);
		screenData.Add(TechTreeStationTerminalState.TechTreeNodePopup, nodePopupScreen);
		screenData.Add(TechTreeStationTerminalState.HelpScreen, techTreeHelpScreen);
		techTreeSO.EnsureInitialized();
		pageButtons = new List<SIGadgetListEntry>();
		techTreePages = new List<SITechTreeUIPage>();
		spriteByType.Add(SIResource.ResourceType.TechPoint, techPointSprite);
		spriteByType.Add(SIResource.ResourceType.StrangeWood, strangeWoodSprite);
		spriteByType.Add(SIResource.ResourceType.WeirdGear, weirdGearSprite);
		spriteByType.Add(SIResource.ResourceType.VibratingSpring, vibratingSpringSprite);
		spriteByType.Add(SIResource.ResourceType.BouncySand, bouncySandSprite);
		spriteByType.Add(SIResource.ResourceType.FloppyMetal, floppyMetalSprite);
		int count = techTreeSO.TreePages.Count;
		for (int i = 0; i < count; i++)
		{
			SITechTreePage sITechTreePage = techTreeSO.TreePages[i];
			if (sITechTreePage.IsValid)
			{
				techTreeIconById.Add(sITechTreePage.pageId, sITechTreePage.icon);
				SIGadgetListEntry sIGadgetListEntry = UnityEngine.Object.Instantiate(pageListEntryPrefab, pageListParent);
				StaticLodManager.TryAddLateInstantiatedMembers(sIGadgetListEntry.gameObject);
				sIGadgetListEntry.Configure(this, sITechTreePage, parentTerminal.zeroZeroImage, parentTerminal.onePointTwoText, SITouchscreenButton.SITouchscreenButtonType.PageSelect, i, -0.07f, count);
				pageButtons.Add(sIGadgetListEntry);
				SITechTreeUIPage sITechTreeUIPage = UnityEngine.Object.Instantiate(pagePrefab, pageParent);
				StaticLodManager.TryAddLateInstantiatedMembers(sITechTreeUIPage.gameObject);
				sITechTreeUIPage.Configure(this, sITechTreePage, parentTerminal.zeroZeroImage, parentTerminal.onePointTwoText);
				techTreePages.Add(sITechTreeUIPage);
			}
		}
		Reset();
	}

	private void _RefreshButtonsUsableState()
	{
		foreach (SIGadgetListEntry pageButton in pageButtons)
		{
			SITechTreePageId id = (SITechTreePageId)pageButton.Id;
			if (techTreeSO.TryGetTreePage(id, out var treePage))
			{
				pageButton.ButtonContainer.SetUsable(treePage.IsAllowed);
			}
		}
	}

	public void Reset()
	{
		currentState = TechTreeStationTerminalState.WaitingForScan;
		nodePopupState = NodePopupState.Description;
		SetScreenVisibility(currentState, currentState);
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy)
		{
			UpdateState(TechTreeStationTerminalState.WaitingForScan, TechTreeStationTerminalState.WaitingForScan);
		}
		stream.SendNext(currentNodeId);
		stream.SendNext(helpScreenIndex);
		stream.SendNext((int)nodePopupState);
		stream.SendNext((int)currentState);
		stream.SendNext((int)lastState);
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		currentNodeId = (int)stream.ReceiveNext();
		if (CurrentNode == null)
		{
			currentNodeId = (int)CurrentPage.AllNodes[0].Value.upgradeType;
		}
		helpScreenIndex = Mathf.Clamp((int)stream.ReceiveNext(), 0, helpPopupScreens.Length - 1);
		nodePopupState = (NodePopupState)stream.ReceiveNext();
		if (!Enum.IsDefined(typeof(NodePopupState), nodePopupState))
		{
			nodePopupState = NodePopupState.Description;
		}
		TechTreeStationTerminalState techTreeStationTerminalState = (TechTreeStationTerminalState)stream.ReceiveNext();
		TechTreeStationTerminalState techTreeStationTerminalState2 = (TechTreeStationTerminalState)stream.ReceiveNext();
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(TechTreeStationTerminalState), techTreeStationTerminalState) || !Enum.IsDefined(typeof(TechTreeStationTerminalState), techTreeStationTerminalState2))
		{
			UpdateState(TechTreeStationTerminalState.WaitingForScan, TechTreeStationTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(techTreeStationTerminalState, techTreeStationTerminalState2);
		}
	}

	public void ZoneDataSerializeWrite(BinaryWriter writer)
	{
		writer.Write(currentNodeId);
		writer.Write(helpScreenIndex);
		writer.Write((int)nodePopupState);
		writer.Write((int)currentState);
		writer.Write((int)lastState);
	}

	public void ZoneDataSerializeRead(BinaryReader reader)
	{
		currentNodeId = reader.ReadInt32();
		if (CurrentNode == null || !Enum.IsDefined(typeof(SIUpgradeType), CurrentNode.upgradeType))
		{
			GTDev.LogError($"SITechTreeStation.ZoneDataSerializeRead: Invalid currentNodeId {currentNodeId} for page {parentTerminal.ActivePage}. Falling back to first node.");
			currentNodeId = (int)CurrentPage.AllNodes[0].Value.upgradeType;
		}
		helpScreenIndex = Mathf.Clamp(reader.ReadInt32(), 0, helpPopupScreens.Length - 1);
		nodePopupState = (NodePopupState)reader.ReadInt32();
		if (!Enum.IsDefined(typeof(NodePopupState), nodePopupState))
		{
			nodePopupState = NodePopupState.Description;
		}
		TechTreeStationTerminalState techTreeStationTerminalState = (TechTreeStationTerminalState)reader.ReadInt32();
		TechTreeStationTerminalState techTreeStationTerminalState2 = (TechTreeStationTerminalState)reader.ReadInt32();
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(TechTreeStationTerminalState), techTreeStationTerminalState) || !Enum.IsDefined(typeof(TechTreeStationTerminalState), techTreeStationTerminalState2))
		{
			UpdateState(TechTreeStationTerminalState.WaitingForScan, TechTreeStationTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(techTreeStationTerminalState, techTreeStationTerminalState2);
		}
	}

	public void UpdateState(TechTreeStationTerminalState newState, TechTreeStationTerminalState newLastState)
	{
		if (!IsPopupState(newLastState))
		{
			currentState = newLastState;
		}
		UpdateState(newState);
	}

	public void UpdateState(TechTreeStationTerminalState newState)
	{
		if (!IsPopupState(currentState))
		{
			lastState = currentState;
		}
		currentState = newState;
		SetScreenVisibility(currentState, lastState);
		switch (currentState)
		{
		case TechTreeStationTerminalState.TechTreePagesList:
			playerNameText.text = ActivePlayerName;
			screenDescriptionText.text = "TECH TREE PAGES";
			break;
		case TechTreeStationTerminalState.TechTreePage:
		{
			playerNameText.text = ActivePlayerName;
			UpdateNodeData(ActivePlayer);
			screenDescriptionText.text = techTreeSO.GetTreePage((SITechTreePageId)parentTerminal.ActivePage)?.nickName;
			foreach (SIGadgetListEntry pageButton in pageButtons)
			{
				pageButton.selectionIndicator.SetActive(pageButton.Id == parentTerminal.ActivePage);
			}
			foreach (SITechTreeUIPage techTreePage in techTreePages)
			{
				techTreePage.gameObject.SetActive(techTreePage.id == (SITechTreePageId)parentTerminal.ActivePage);
			}
			techTreeIconById.TryGetValue((SITechTreePageId)parentTerminal.ActivePage, out var value);
			techTreeIcon.sprite = value;
			break;
		}
		case TechTreeStationTerminalState.TechTreeNodePopup:
			switch (nodePopupState)
			{
			case NodePopupState.Description:
				nodeNameText.text = CurrentNode.nickName;
				nodeDescriptionText.text = CurrentNode.description;
				if (ActivePlayer.NodeResearched(CurrentNode.upgradeType))
				{
					nodeResearched.SetActive(value: true);
					nodeLocked.SetActive(value: false);
					nodeAvailable.SetActive(value: false);
					nodeResearchButton.SetActive(value: false);
					canAffordNode.SetActive(value: false);
					cantAffordNode.SetActive(value: false);
				}
				else if (ActivePlayer.NodeParentsUnlocked(CurrentNode.upgradeType))
				{
					nodeResearched.SetActive(value: false);
					nodeLocked.SetActive(value: false);
					nodeAvailable.SetActive(value: true);
					nodeResearchButton.SetActive(value: true);
					bool flag = ActivePlayer.PlayerCanAffordNode(CurrentNode);
					canAffordNode.SetActive(flag);
					cantAffordNode.SetActive(!flag);
				}
				else
				{
					nodeResearched.SetActive(value: false);
					nodeAvailable.SetActive(value: false);
					nodeLocked.SetActive(value: true);
					nodeResearchButton.SetActive(value: false);
					canAffordNode.SetActive(value: false);
					cantAffordNode.SetActive(value: false);
				}
				nodeResourceTypeText.text = FormattedCurrentResourceTypesForNode(CurrentNode);
				nodeResourceCostText.text = FormattedResearchCost(CurrentNode);
				playerCurrentResourceAmountsText.text = FormattedCurrentResourceAmountsForNode(CurrentNode);
				break;
			case NodePopupState.Loading:
				if (ActivePlayer.NodeResearched(CurrentNode.upgradeType))
				{
					nodePopupState = NodePopupState.Success;
					nodeNameResearchMessageText.text = "SUCCESSFULLY UNLOCKED TECH NODE!";
				}
				else
				{
					nodeNameResearchMessageText.text = "ATTEMPTING TO UNLOCK NODE\n\nLOADING . . .";
				}
				break;
			case NodePopupState.Success:
				nodeNameResearchMessageText.text = "SUCCESSFULLY UNLOCKED TECH NODE!";
				break;
			case NodePopupState.NotEnoughResources:
				nodeNameResearchMessageText.text = "NOT ENOUGH RESOURCES TO UNLOCK NODE! GATHER MORE AND TRY AGAIN!";
				break;
			}
			UpdateNodePopupPage();
			break;
		case TechTreeStationTerminalState.HelpScreen:
			UpdateHelpButtonPage(helpScreenIndex);
			break;
		case TechTreeStationTerminalState.WaitingForScan:
			break;
		}
	}

	public void SetScreenVisibility(TechTreeStationTerminalState currentState, TechTreeStationTerminalState lastState)
	{
		bool flag = IsPopupState(currentState);
		background.color = ((currentState == TechTreeStationTerminalState.WaitingForScan) ? Color.white : ((ActivePlayer != null && ActivePlayer.gamePlayer.IsLocal()) ? active : notActive));
		foreach (TechTreeStationTerminalState key in screenData.Keys)
		{
			if (key == TechTreeStationTerminalState.TechTreePagesList)
			{
				screenData[key].SetActive(currentState != TechTreeStationTerminalState.WaitingForScan);
				continue;
			}
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
		bool flag3 = currentState != TechTreeStationTerminalState.WaitingForScan;
		screenDescriptionText.gameObject.SetActive(flag3);
		playerNameText.gameObject.SetActive(flag3);
		SetNonPopupButtonsEnabled(!flag);
	}

	public bool IsPopupState(TechTreeStationTerminalState state)
	{
		if (state != TechTreeStationTerminalState.TechTreeNodePopup)
		{
			return state == TechTreeStationTerminalState.HelpScreen;
		}
		return true;
	}

	public void PlayerHandScanned(int actorNr)
	{
		if (!IsAuthority)
		{
			parentTerminal.PlayerHandScanned(actorNr);
		}
		else
		{
			UpdateState(TechTreeStationTerminalState.TechTreePage);
		}
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
			soundBankPlayer.Play();
		}
		if (actorNr == SIPlayer.LocalPlayer.ActorNr && ActivePlayer == SIPlayer.LocalPlayer && currentState == TechTreeStationTerminalState.TechTreeNodePopup && nodePopupState == NodePopupState.Description && buttonType == SITouchscreenButton.SITouchscreenButtonType.Research && !SIPlayer.LocalPlayer.NodeResearched(CurrentNode.upgradeType) && SIPlayer.LocalPlayer.NodeParentsUnlocked(CurrentNode.upgradeType))
		{
			SIProgression.Instance.TryUnlock(CurrentNode.upgradeType);
		}
		if (!IsAuthority)
		{
			parentTerminal.TouchscreenButtonPressed(buttonType, data, actorNr, SICombinedTerminal.TerminalSubFunction.TechTree);
		}
		else
		{
			if (ActivePlayer == null || actorNr != ActivePlayer.ActorNr)
			{
				return;
			}
			soundBankPlayer.Play();
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.PageSelect)
			{
				parentTerminal.SetActivePage(data);
				UpdateState(TechTreeStationTerminalState.TechTreePage);
				return;
			}
			switch (currentState)
			{
			case TechTreeStationTerminalState.WaitingForScan:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(TechTreeStationTerminalState.HelpScreen);
				}
				break;
			case TechTreeStationTerminalState.TechTreePagesList:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(TechTreeStationTerminalState.HelpScreen);
				}
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Select)
				{
					parentTerminal.SetActivePage(data);
					UpdateState(TechTreeStationTerminalState.TechTreePage);
				}
				break;
			case TechTreeStationTerminalState.TechTreePage:
				switch (buttonType)
				{
				case SITouchscreenButton.SITouchscreenButtonType.Select:
					currentNodeId = data;
					UpdateState(TechTreeStationTerminalState.TechTreeNodePopup);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Back:
					UpdateState(TechTreeStationTerminalState.TechTreePagesList);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Help:
					UpdateState(TechTreeStationTerminalState.HelpScreen);
					break;
				}
				break;
			case TechTreeStationTerminalState.TechTreeNodePopup:
				if (nodePopupState == NodePopupState.Description)
				{
					if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Exit)
					{
						UpdateState(lastState);
					}
					if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Research)
					{
						if (ActivePlayer.PlayerCanAffordNode(CurrentNode))
						{
							nodePopupState = NodePopupState.Loading;
						}
						else
						{
							nodePopupState = NodePopupState.NotEnoughResources;
						}
						UpdateState(TechTreeStationTerminalState.TechTreeNodePopup);
					}
				}
				else if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Back)
				{
					nodePopupState = NodePopupState.Description;
					UpdateState(TechTreeStationTerminalState.TechTreeNodePopup);
				}
				break;
			case TechTreeStationTerminalState.HelpScreen:
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

	public void UpdateNodePopupPage()
	{
		int num = ((nodePopupState != NodePopupState.Description) ? 1 : 0);
		if (nodePopupScreens[0].activeSelf != (num == 0))
		{
			nodePopupScreens[0].SetActive(num == 0);
		}
		if (nodePopupScreens[1].activeSelf != (num == 1))
		{
			nodePopupScreens[1].SetActive(num == 1);
		}
	}

	public void UpdateNodeData(SIPlayer player)
	{
		if (player == null)
		{
			for (int i = 0; i < techTreePages.Count; i++)
			{
				techTreePages[i].PopulateDefaultNodeData();
			}
		}
		else
		{
			for (int j = 0; j < techTreePages.Count; j++)
			{
				techTreePages[j].PopulatePlayerNodeData(player);
			}
		}
	}

	public string FormattedResearchCost(SITechTreeNode node)
	{
		if (SIProgression.Instance.GetOnlineNode(node.upgradeType, out var node2))
		{
			string text = "";
			text = text + node2.costs[SIResource.ResourceType.TechPoint] + "\n";
			{
				foreach (KeyValuePair<SIResource.ResourceType, int> cost in node2.costs)
				{
					if (cost.Key != SIResource.ResourceType.TechPoint)
					{
						return text + cost.Value;
					}
				}
				return text;
			}
		}
		return string.Join("\n", node.nodeCost.Select((SIResource.ResourceCost c) => c.amount));
	}

	public string FormattedCurrentResourceAmountsForNode(SITechTreeNode node)
	{
		string text = "";
		if (SIProgression.Instance.GetOnlineNode(node.upgradeType, out var node2))
		{
			text = text + ActivePlayer.CurrentProgression.resourceArray[0] + "\n";
			foreach (KeyValuePair<SIResource.ResourceType, int> cost in node2.costs)
			{
				if (cost.Key != SIResource.ResourceType.TechPoint)
				{
					text = text + ActivePlayer.CurrentProgression.resourceArray[(int)cost.Key] + "\n";
				}
			}
		}
		else
		{
			for (int i = 0; i < node.nodeCost.Length; i++)
			{
				text = text + ActivePlayer.CurrentProgression.resourceArray[(int)node.nodeCost[i].type] + "\n";
			}
		}
		return text;
	}

	public string FormattedCurrentResourceTypesForNode(SITechTreeNode node)
	{
		string text = "";
		if (SIProgression.Instance.GetOnlineNode(node.upgradeType, out var node2))
		{
			text = text + SIResource.ResourceType.TechPoint.ToString().ToUpperInvariant() + "\n";
			foreach (KeyValuePair<SIResource.ResourceType, int> cost in node2.costs)
			{
				if (cost.Key != SIResource.ResourceType.TechPoint)
				{
					text = text + cost.Key.ToString().ToUpperInvariant() + "\n";
					resourceCost.sprite = spriteByType[cost.Key];
				}
			}
		}
		else
		{
			for (int i = 0; i < node.nodeCost.Length; i++)
			{
				text = text + node.nodeCost[i].type.ToString().ToUpperInvariant() + "\n";
			}
		}
		return text;
	}

	private void OnProgressionUpdate()
	{
		UpdateNodeData(ActivePlayer);
		UpdateState(currentState);
	}

	private void OnProgressionUpdateNode(SIUpgradeType type)
	{
		OnProgressionUpdate();
	}

	public void SetActivePage()
	{
		if (CurrentNode == null)
		{
			currentNodeId = CurrentPage.AllNodes[0].Value.upgradeType.GetNodeId();
		}
		if (ActivePlayer != null)
		{
			UpdateState(TechTreeStationTerminalState.TechTreePage);
		}
		else
		{
			UpdateState(TechTreeStationTerminalState.WaitingForScan);
		}
	}

	public bool IsValidPage(int pageId)
	{
		if (pageId < 0)
		{
			return false;
		}
		foreach (SITechTreeUIPage techTreePage in techTreePages)
		{
			if (techTreePage.id == (SITechTreePageId)pageId)
			{
				return true;
			}
		}
		return false;
	}
}
