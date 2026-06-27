using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SIResourceCollection : MonoBehaviour, ITouchScreenStation
{
	public enum FailReason
	{
		NotEnoughRocks,
		ResourcesFull,
		Unknown
	}

	public enum ResourceCollectorTerminalState
	{
		WaitingForScan,
		CurrentResources,
		HelpScreen,
		PurchaseRemote,
		PurchaseStart,
		PurchaseInProgress,
		PurchaseSuccess,
		PurchaseFailure
	}

	public const int REFILL_PURCHASE_SHINY_ROCK_COST = 500;

	private const string lineBreak = "\n";

	private const string appendToMax = " -> 20";

	public ResourceCollectorTerminalState currentState;

	public ResourceCollectorTerminalState lastState;

	public int resourceDepositedCount;

	private int currentHelpButtonPageIndex;

	public GameObject waitingForScanScreen;

	public GameObject currentResourcesScreen;

	public GameObject helpScreen;

	public SICombinedTerminal parentTerminal;

	public Sprite[] resourceImageSprites;

	[SerializeField]
	private SIScreenRegion screenRegion;

	public GameObject[] helpPopupScreens;

	public GameObject purchasingRemote;

	public GameObject purchasingStart;

	public GameObject purchaseInProgress;

	public GameObject purchasingSuccess;

	public GameObject purchasingFailure;

	public GameObject popupScreen;

	public Transform uiCenter;

	[Header("Purchasing Pages")]
	public TextMeshProUGUI shinyRockInfo;

	public TextMeshProUGUI currentResourceCountsLocal;

	public TextMeshProUGUI currentResourceCountsRemote;

	public TextMeshProUGUI failureReasonText;

	public const string failureFull = "YOU ARE ALREADY AT MAX RESOURCES! DONATE YOUR SHINY ROCKS TO A GOOD CAUSE INSTEAD OF US, KNUCKLEHEAD!";

	public const string failureNotEnoughRocks = "NOT ENOUGH SHINY ROCKS! PLEASE TRY AGAIN LATER, OR PURCHASE MORE SHINY ROCKS!";

	public const string failureUnknown = "UHHHHH SOMETHING WENT WRONG, I'M NOT SURE WHAT, SORRY TRY AGAIN LATER MAYBE!";

	private FailReason failureReason;

	public Image background;

	public Color active;

	public Color notActive;

	public TextMeshProUGUI currentResourcesResourceCounts;

	private Dictionary<ResourceCollectorTerminalState, GameObject> screenData;

	private bool initialized;

	[SerializeField]
	private SoundBankPlayer soundBankPlayer;

	[Tooltip("Button colliders to disable while popup screen is shown.")]
	[SerializeField]
	private List<Collider> _nonPopupButtonColliders;

	public SIScreenRegion ScreenRegion => screenRegion;

	public bool IsAuthority => SIManager.gameEntityManager.IsAuthority();

	public SIPlayer ActivePlayer => parentTerminal.activePlayer;

	public SuperInfectionManager SIManager => parentTerminal.superInfection.siManager;

	private void CollectButtonColliders()
	{
		List<SITouchscreenButton> buttons = GetComponentsInChildren<SITouchscreenButton>(includeInactive: true).ToList();
		RemoveButtonsInside((from d in GetComponentsInChildren<DestroyIfNotBeta>()
			select d.gameObject).ToArray());
		RemoveButtonsInside(new GameObject[1] { helpScreen });
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

	public void Initialize()
	{
		if (!initialized)
		{
			initialized = true;
			if (parentTerminal == null)
			{
				parentTerminal = GetComponentInParent<SICombinedTerminal>();
			}
			screenData = new Dictionary<ResourceCollectorTerminalState, GameObject>();
			screenData.Add(ResourceCollectorTerminalState.WaitingForScan, waitingForScanScreen);
			screenData.Add(ResourceCollectorTerminalState.CurrentResources, currentResourcesScreen);
			screenData.Add(ResourceCollectorTerminalState.HelpScreen, helpScreen);
			screenData.Add(ResourceCollectorTerminalState.PurchaseRemote, purchasingRemote);
			screenData.Add(ResourceCollectorTerminalState.PurchaseStart, purchasingStart);
			screenData.Add(ResourceCollectorTerminalState.PurchaseInProgress, purchaseInProgress);
			screenData.Add(ResourceCollectorTerminalState.PurchaseSuccess, purchasingSuccess);
			screenData.Add(ResourceCollectorTerminalState.PurchaseFailure, purchasingFailure);
			Reset();
		}
	}

	public void Reset()
	{
		currentState = ResourceCollectorTerminalState.WaitingForScan;
		lastState = currentState;
		SetScreenVisibility(currentState, lastState);
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy)
		{
			UpdateState(ResourceCollectorTerminalState.WaitingForScan, ResourceCollectorTerminalState.WaitingForScan);
		}
		stream.SendNext(currentHelpButtonPageIndex);
		stream.SendNext((int)currentState);
		stream.SendNext((int)lastState);
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		currentHelpButtonPageIndex = Mathf.Clamp((int)stream.ReceiveNext(), 0, helpPopupScreens.Length - 1);
		UpdateHelpButtonPage(currentHelpButtonPageIndex);
		ResourceCollectorTerminalState resourceCollectorTerminalState = (ResourceCollectorTerminalState)stream.ReceiveNext();
		ResourceCollectorTerminalState resourceCollectorTerminalState2 = (ResourceCollectorTerminalState)stream.ReceiveNext();
		if (!Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState) || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState2))
		{
			resourceCollectorTerminalState = ResourceCollectorTerminalState.WaitingForScan;
			resourceCollectorTerminalState2 = ResourceCollectorTerminalState.WaitingForScan;
		}
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState) || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState2))
		{
			UpdateState(ResourceCollectorTerminalState.WaitingForScan, ResourceCollectorTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(resourceCollectorTerminalState, resourceCollectorTerminalState2);
		}
	}

	public void ZoneDataSerializeWrite(BinaryWriter writer)
	{
		writer.Write(currentHelpButtonPageIndex);
		writer.Write((int)currentState);
		writer.Write((int)lastState);
	}

	public void ZoneDataSerializeRead(BinaryReader reader)
	{
		currentHelpButtonPageIndex = Mathf.Clamp(reader.ReadInt32(), 0, helpPopupScreens.Length - 1);
		UpdateHelpButtonPage(currentHelpButtonPageIndex);
		ResourceCollectorTerminalState resourceCollectorTerminalState = (ResourceCollectorTerminalState)reader.ReadInt32();
		ResourceCollectorTerminalState resourceCollectorTerminalState2 = (ResourceCollectorTerminalState)reader.ReadInt32();
		if (!Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState) || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState2))
		{
			resourceCollectorTerminalState = ResourceCollectorTerminalState.WaitingForScan;
			resourceCollectorTerminalState2 = ResourceCollectorTerminalState.WaitingForScan;
		}
		if (ActivePlayer == null || !ActivePlayer.gameObject.activeInHierarchy || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState) || !Enum.IsDefined(typeof(ResourceCollectorTerminalState), resourceCollectorTerminalState2))
		{
			UpdateState(ResourceCollectorTerminalState.WaitingForScan, ResourceCollectorTerminalState.WaitingForScan);
		}
		else
		{
			UpdateState(resourceCollectorTerminalState, resourceCollectorTerminalState2);
		}
	}

	public bool PopupActive()
	{
		return IsPopupState(currentState);
	}

	public bool IsPopupState(ResourceCollectorTerminalState state)
	{
		if (state != ResourceCollectorTerminalState.HelpScreen && state != ResourceCollectorTerminalState.PurchaseInProgress && state != ResourceCollectorTerminalState.PurchaseRemote && state != ResourceCollectorTerminalState.PurchaseStart && state != ResourceCollectorTerminalState.PurchaseFailure)
		{
			return state == ResourceCollectorTerminalState.PurchaseSuccess;
		}
		return true;
	}

	public bool HasHelpButton(ResourceCollectorTerminalState state)
	{
		if (state != ResourceCollectorTerminalState.CurrentResources)
		{
			return state == ResourceCollectorTerminalState.WaitingForScan;
		}
		return true;
	}

	public void UpdateState(ResourceCollectorTerminalState newState, ResourceCollectorTerminalState newLastState)
	{
		if (!IsPopupState(newLastState))
		{
			currentState = newLastState;
		}
		UpdateState(newState);
	}

	public void UpdateState(ResourceCollectorTerminalState newState)
	{
		if (!IsPopupState(currentState))
		{
			lastState = currentState;
		}
		currentState = newState;
		SetScreenVisibility(currentState, lastState);
		switch (currentState)
		{
		case ResourceCollectorTerminalState.CurrentResources:
			currentResourcesResourceCounts.text = FormattedPlayerResourceCount(ActivePlayer);
			break;
		case ResourceCollectorTerminalState.HelpScreen:
			UpdateHelpButtonPage(currentHelpButtonPageIndex);
			break;
		case ResourceCollectorTerminalState.PurchaseStart:
			if (ActivePlayer != null && ActivePlayer != SIPlayer.LocalPlayer)
			{
				UpdateState(ResourceCollectorTerminalState.PurchaseRemote);
			}
			else
			{
				shinyRockInfo.text = "PRICE: 500 SHINY ROCKS\n\nYOU HAVE:\n" + ProgressionManager.Instance.GetShinyRocksTotal() + " SHINY ROCKS";
			}
			currentResourceCountsLocal.text = FormattedPlayerResourceCountWithMax(ActivePlayer);
			break;
		case ResourceCollectorTerminalState.PurchaseRemote:
			if (ActivePlayer != null && ActivePlayer == SIPlayer.LocalPlayer)
			{
				UpdateState(ResourceCollectorTerminalState.PurchaseStart);
			}
			currentResourceCountsLocal.text = FormattedPlayerResourceCountWithMax(ActivePlayer);
			break;
		case ResourceCollectorTerminalState.PurchaseFailure:
			switch (failureReason)
			{
			case FailReason.NotEnoughRocks:
				failureReasonText.text = "NOT ENOUGH SHINY ROCKS! PLEASE TRY AGAIN LATER, OR PURCHASE MORE SHINY ROCKS!";
				break;
			case FailReason.ResourcesFull:
				failureReasonText.text = "YOU ARE ALREADY AT MAX RESOURCES! DONATE YOUR SHINY ROCKS TO A GOOD CAUSE INSTEAD OF US, KNUCKLEHEAD!";
				break;
			case FailReason.Unknown:
				failureReasonText.text = "UHHHHH SOMETHING WENT WRONG, I'M NOT SURE WHAT, SORRY TRY AGAIN LATER MAYBE!";
				break;
			}
			break;
		case ResourceCollectorTerminalState.WaitingForScan:
		case ResourceCollectorTerminalState.PurchaseInProgress:
		case ResourceCollectorTerminalState.PurchaseSuccess:
			break;
		}
	}

	public string FormattedPlayerResourceCount(SIPlayer player)
	{
		return GetFormattedResource(player, SIResource.ResourceType.TechPoint) + "\n" + GetFormattedResource(player, SIResource.ResourceType.StrangeWood) + "\n" + GetFormattedResource(player, SIResource.ResourceType.WeirdGear) + "\n" + GetFormattedResource(player, SIResource.ResourceType.VibratingSpring) + "\n" + GetFormattedResource(player, SIResource.ResourceType.BouncySand) + "\n" + GetFormattedResource(player, SIResource.ResourceType.FloppyMetal);
	}

	public string FormattedPlayerResourceCountWithMax(SIPlayer player)
	{
		return GetFormattedResource(player, SIResource.ResourceType.StrangeWood) + " -> 20\n" + GetFormattedResource(player, SIResource.ResourceType.WeirdGear) + " -> 20\n" + GetFormattedResource(player, SIResource.ResourceType.VibratingSpring) + " -> 20\n" + GetFormattedResource(player, SIResource.ResourceType.BouncySand) + " -> 20\n" + GetFormattedResource(player, SIResource.ResourceType.FloppyMetal) + " -> 20";
	}

	private string GetFormattedResource(SIPlayer player, SIResource.ResourceType resource)
	{
		int resourceMaxCap = SIProgression.Instance.GetResourceMaxCap(resource);
		if (resourceMaxCap == int.MaxValue)
		{
			return player.CurrentProgression.resourceArray[(int)resource].ToString();
		}
		return $"{player.CurrentProgression.resourceArray[(int)resource]}/{resourceMaxCap}";
	}

	public void UpdateHelpButtonPage(int helpButtonPageIndex)
	{
		for (int i = 0; i < helpPopupScreens.Length; i++)
		{
			helpPopupScreens[i].SetActive(i == helpButtonPageIndex);
		}
	}

	public void SetScreenVisibility(ResourceCollectorTerminalState currentState, ResourceCollectorTerminalState lastState)
	{
		bool flag = IsPopupState(currentState);
		background.color = ((currentState == ResourceCollectorTerminalState.WaitingForScan) ? Color.white : ((ActivePlayer != null && ActivePlayer.gamePlayer.IsLocal()) ? active : notActive));
		foreach (ResourceCollectorTerminalState key in screenData.Keys)
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
		SetNonPopupButtonsEnabled(!flag);
	}

	public void PlayerHandScanned(int actorNr)
	{
		UpdateState(ResourceCollectorTerminalState.CurrentResources);
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
		if (actorNr == SIPlayer.LocalPlayer.ActorNr && ActivePlayer == SIPlayer.LocalPlayer && currentState == ResourceCollectorTerminalState.PurchaseStart && buttonType == SITouchscreenButton.SITouchscreenButtonType.Confirm)
		{
			bool flag = ProgressionManager.Instance.GetShinyRocksTotal() >= 500;
			bool flag2 = SIProgression.ResourcesMaxed();
			if (flag && !flag2)
			{
				ProgressionManager.Instance.PurchaseResources(delegate
				{
					SIProgression.Instance.SendPurchaseResourcesData();
					ProgressionManager.Instance.RefreshUserInventory();
					TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType.Collect, -1, SIPlayer.LocalPlayer.ActorNr);
				}, delegate(string error)
				{
					FailReason data2 = ((!(error == "Not enough Shiny Rocks to complete this purchase")) ? ((error == "already maxed resources") ? FailReason.ResourcesFull : FailReason.Unknown) : FailReason.NotEnoughRocks);
					TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType.OverrideFailure, (int)data2, SIPlayer.LocalPlayer.ActorNr);
				});
			}
			else
			{
				buttonType = SITouchscreenButton.SITouchscreenButtonType.OverrideFailure;
				data = (flag ? (flag2 ? 1 : 2) : 0);
			}
		}
		if (!IsAuthority)
		{
			parentTerminal.TouchscreenButtonPressed(buttonType, data, actorNr, SICombinedTerminal.TerminalSubFunction.ResourceCollection);
		}
		else
		{
			if (ActivePlayer == null || actorNr != ActivePlayer.ActorNr)
			{
				return;
			}
			soundBankPlayer.Play();
			switch (currentState)
			{
			case ResourceCollectorTerminalState.WaitingForScan:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Help)
				{
					UpdateState(ResourceCollectorTerminalState.HelpScreen);
				}
				break;
			case ResourceCollectorTerminalState.CurrentResources:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Purchase)
				{
					UpdateState(ResourceCollectorTerminalState.PurchaseStart);
				}
				break;
			case ResourceCollectorTerminalState.PurchaseRemote:
			case ResourceCollectorTerminalState.PurchaseStart:
				switch (buttonType)
				{
				case SITouchscreenButton.SITouchscreenButtonType.Confirm:
					UpdateState(ResourceCollectorTerminalState.PurchaseInProgress);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Cancel:
					UpdateState(ResourceCollectorTerminalState.CurrentResources);
					break;
				default:
					failureReason = (FailReason)data;
					UpdateState(ResourceCollectorTerminalState.PurchaseFailure);
					break;
				}
				break;
			case ResourceCollectorTerminalState.PurchaseInProgress:
				switch (buttonType)
				{
				case SITouchscreenButton.SITouchscreenButtonType.OverrideFailure:
					failureReason = (FailReason)data;
					UpdateState(ResourceCollectorTerminalState.PurchaseFailure);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Collect:
					UpdateState(ResourceCollectorTerminalState.PurchaseSuccess);
					break;
				}
				break;
			case ResourceCollectorTerminalState.PurchaseSuccess:
			case ResourceCollectorTerminalState.PurchaseFailure:
				if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Exit)
				{
					UpdateState(ResourceCollectorTerminalState.CurrentResources);
				}
				break;
			case ResourceCollectorTerminalState.HelpScreen:
				switch (buttonType)
				{
				case SITouchscreenButton.SITouchscreenButtonType.Exit:
					currentHelpButtonPageIndex = 0;
					UpdateState(lastState);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Next:
					currentHelpButtonPageIndex = Mathf.Clamp(currentHelpButtonPageIndex + 1, 0, helpPopupScreens.Length - 1);
					UpdateHelpButtonPage(currentHelpButtonPageIndex);
					break;
				case SITouchscreenButton.SITouchscreenButtonType.Back:
					currentHelpButtonPageIndex = Mathf.Clamp(currentHelpButtonPageIndex - 1, 0, helpPopupScreens.Length - 1);
					UpdateHelpButtonPage(currentHelpButtonPageIndex);
					break;
				}
				break;
			}
		}
	}

	public void TouchscreenToggleButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr, bool isToggledOn)
	{
	}

	public void AddButton(SITouchscreenButton button, bool isPopupButton = false)
	{
	}
}
