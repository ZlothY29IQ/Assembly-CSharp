using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class GRToolUpgradePurchaseStationFull : MonoBehaviour, ITickSystemTick
{
	public enum ShelfMovementState
	{
		Idle,
		MoveCurrentShelfBackward,
		MoveCurrentShelfForward,
		MoveNextShelfUpward,
		MoveNextShelfDownward,
		Count
	}

	private GhostReactor reactor;

	private GhostReactorManager grManager;

	public List<GRToolUpgradePurchaseStationShelf> gameShelves;

	[NonSerialized]
	private GRToolProgressionManager toolProgressionManager;

	private Color colorPurchaseButtonCanAfford = ColorFromRGB32(0, 0, 0);

	private Color colorCanBuyCredits = ColorFromRGB32(140, 229, 37);

	private Color colorCanBuyJuice = ColorFromRGB32(232, 65, 255);

	private Color colorCantBuy = ColorFromRGB32(140, 38, 38);

	private Color colorSelectedItem = ColorFromRGB32(251, 240, 229);

	private Color colorUnselectedItem = ColorFromRGB32(147, 145, 140);

	private Color colorUnresearchedItem = ColorFromRGB32(230, 19, 17);

	private Color colorUnselectedUnresearchedItem = ColorFromRGB32(133, 11, 10);

	private int selectedShelf;

	private int selectedItem;

	[NonSerialized]
	public int currentActivePlayerActorNumber = -1;

	private ShelfMovementState shelfMovementState;

	private int currentVisibleShelfIndex;

	private int nextVisibleShelfIndex;

	private GRSpringMovement frontBackShelfMovement;

	private GRSpringMovement raiseLowerShelfMovement;

	public Transform shelfRootTransform;

	public Transform shelfBackTransform;

	public Transform shelfLowerTransform;

	public TMP_Text shelfSelectionText;

	public TMP_Text playerInfo;

	public TMP_Text itemDescription;

	public TMP_Text itemDescriptionName;

	public TMP_Text itemDescriptionAnnotation;

	public TMP_Text purchaseButtonText;

	public GorillaPhysicalButton select1;

	public GorillaPhysicalButton select2;

	public GorillaPhysicalButton select3;

	public GorillaPhysicalButton select4;

	public AudioSource audioSourceLooping;

	public AudioSource audioSourceClang;

	public float audioSourceLoopingVolume = 0.5f;

	public Material unresearchedItemMaterial;

	public AudioSource interactAudioSource;

	public IDCardScanner scanner;

	public UnityEvent purchaseSucceded;

	public UnityEvent purchaseFailed;

	public Material backlightPurchase;

	public Material backlightResearch;

	public Material backlightLocked;

	private int lastKnownLocalPlayerCredits;

	private int lastKnownLocalPlayerJuice;

	private bool needsUIRefresh;

	public Transform ropeTop;

	public Transform ropeEnd;

	public Transform magnet;

	private GameEntity currentMagnetEntity;

	private int currentMagnetEntityTypeId = -1;

	private int desiredMagnetEntityTypeId = -1;

	private float prefabMagnetHeightOffset;

	public float maxMagnetDistance = 0.75f;

	private GRSpringMovement magnetMovement;

	public GRSelectionWheel pageSelectionWheel;

	public GameObject pageSelectionHandle;

	public GameObject pageSelectionLever;

	public float playerQueueTimeLimit = 30f;

	private bool disablePurchaseButton;

	private float purchaseButtonCooldown = 2f;

	private float purchaseButtonPressed;

	private const int ShelfIndex_None = -1;

	public bool currentlyShowingText = true;

	private List<GRToolProgressionManager.ToolParts> cachedRequiredPartsList = new List<GRToolProgressionManager.ToolParts>(5);

	private float lastRequestedActivePlayerTokenTime;

	private float requestActivePlayerTokenThrottleTime = 0.25f;

	private bool bIsGrippingLeft;

	private bool bIsGrippingRight;

	private bool bGripLeftLastFrame;

	private bool bGripRightLastFrame;

	private float maxHandleRange = 0.09f;

	private float timeSinceLastHandleBroadcast;

	private float angleOfLastHandleBroadcast;

	private float selectionWheelAngleOfLastBroadcast;

	private float quantMult = 100000f;

	private float lastHandleAngle = -10000f;

	public int SelectedShelf => selectedShelf;

	public int SelectedItem => selectedItem;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Init(GRToolProgressionManager progression, GhostReactor reactor)
	{
		this.reactor = reactor;
		grManager = reactor.grManager;
		toolProgressionManager = progression;
		toolProgressionManager.OnProgressionUpdated += ProgressionUpdated;
		nextVisibleShelfIndex = -1;
		prefabMagnetHeightOffset = ropeTop.position.y;
		frontBackShelfMovement = new GRSpringMovement(0.5f, 0.7f);
		raiseLowerShelfMovement = new GRSpringMovement(1f, 0.7f);
		magnetMovement = new GRSpringMovement(1f, 0.7f);
		ProgressionManager.Instance.OnGetShiftCredit += OnShiftCreditChanged;
		needsUIRefresh = true;
		InitPageSelectionWheel();
		ChangeShelfMovementState(ShelfMovementState.Idle);
		SetActivePlayer(-1);
	}

	public void OnShiftCreditChanged(string targetMothershipId, int newShiftCredits)
	{
		needsUIRefresh = true;
	}

	public void HideOrShowTextBasedOnLocalPlayerDistance()
	{
		Vector3 position = GRPlayer.Get(VRRig.LocalRig).transform.position;
		Vector3 position2 = base.transform.position;
		float num = (currentlyShowingText ? 8f : 6f);
		bool flag = (position - position2).sqrMagnitude < num * num;
		if (flag != currentlyShowingText)
		{
			shelfSelectionText.enabled = flag;
			playerInfo.enabled = flag;
			itemDescription.enabled = flag;
			itemDescriptionName.enabled = flag;
			itemDescriptionAnnotation.enabled = flag;
			purchaseButtonText.enabled = flag;
			pageSelectionWheel.ShowText(flag);
			for (int i = 0; i < gameShelves.Count; i++)
			{
				if (gameShelves[i] == null)
				{
					continue;
				}
				foreach (GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot in gameShelves[i].gRPurchaseSlots)
				{
					if (gRPurchaseSlot.Name != null)
					{
						gRPurchaseSlot.Name.enabled = flag;
					}
					if (gRPurchaseSlot.Price != null)
					{
						gRPurchaseSlot.Price.enabled = flag;
					}
				}
			}
		}
		currentlyShowingText = flag;
	}

	public void Tick()
	{
		HideOrShowTextBasedOnLocalPlayerDistance();
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (toolProgressionManager == null)
		{
			return;
		}
		if (gRPlayer != null && (lastKnownLocalPlayerCredits != gRPlayer.ShiftCredits || lastKnownLocalPlayerJuice != toolProgressionManager.GetNumberOfResearchPoints()))
		{
			needsUIRefresh = true;
			lastKnownLocalPlayerCredits = gRPlayer.ShiftCredits;
			lastKnownLocalPlayerJuice = toolProgressionManager.GetNumberOfResearchPoints();
		}
		UpdateActivePlayer();
		UpdateSelectionLever();
		UpdateShelf();
		UpdateMagnet();
		if (disablePurchaseButton)
		{
			if (purchaseButtonPressed > 0f)
			{
				purchaseButtonPressed -= Time.deltaTime;
			}
			else
			{
				disablePurchaseButton = false;
			}
		}
		if (needsUIRefresh)
		{
			needsUIRefresh = false;
			UpdateShelfDisplayElements(currentVisibleShelfIndex);
			UpdateShelfDisplayElements(nextVisibleShelfIndex);
			UpdateShelfDisplayElements(selectedShelf);
			UpdatePlayerCurrencyUI();
			UpdatePurchaseButtonText();
		}
	}

	public void SetActivePlayer(int actorNum)
	{
		currentActivePlayerActorNumber = actorNum;
		needsUIRefresh = true;
		if (currentActivePlayerActorNumber == -1)
		{
			itemDescriptionName.text = "SWIPE FOR ACCESS";
			itemDescription.text = "Welcome to the Tool-o-matic v2 automated vending machine. Please swipe your ID card for access.";
			itemDescriptionAnnotation.text = "Remember: Compliance leads to success!";
		}
		else if (IsValidShelfItemIndex(selectedShelf, selectedItem) && toolProgressionManager != null)
		{
			GRToolProgressionManager.ToolProgressionMetaData partMetadata = toolProgressionManager.GetPartMetadata(gameShelves[selectedShelf].gRPurchaseSlots[selectedItem].PurchaseID);
			if (partMetadata != null)
			{
				itemDescriptionName.text = partMetadata.name;
				itemDescription.text = partMetadata.description;
				itemDescriptionAnnotation.text = partMetadata.annotation;
			}
			select1.SetButtonState(selectedItem == 0);
			select2.SetButtonState(selectedItem == 1);
			select3.SetButtonState(selectedItem == 2);
			select4.SetButtonState(selectedItem == 3);
		}
	}

	public void UpdateActivePlayer()
	{
		if (!grManager.IsAuthority() || currentActivePlayerActorNumber == -1)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(currentActivePlayerActorNumber);
		if (gRPlayer != null)
		{
			BoxCollider component = GetComponent<BoxCollider>();
			Vector3 position = gRPlayer.transform.position;
			Vector3 vector = component.transform.worldToLocalMatrix.MultiplyPoint(position) - component.center;
			Vector3 vector2 = component.size * 0.5f;
			if (Mathf.Abs(vector.x) > vector2.x || Mathf.Abs(vector.y) > vector2.y || Mathf.Abs(vector.z) > vector2.z)
			{
				grManager.SetActivePlayerAuthority(this, -1);
			}
		}
		else
		{
			currentActivePlayerActorNumber = -1;
		}
	}

	private void UpdateShelf()
	{
		switch (shelfMovementState)
		{
		case ShelfMovementState.Idle:
			if (currentVisibleShelfIndex != selectedShelf)
			{
				SetNextShelf(selectedShelf);
				ChangeShelfMovementState(ShelfMovementState.MoveCurrentShelfBackward);
			}
			else
			{
				SetNextShelf(-1);
			}
			break;
		case ShelfMovementState.MoveCurrentShelfBackward:
			if (currentVisibleShelfIndex != selectedShelf)
			{
				frontBackShelfMovement.target = 1f;
				frontBackShelfMovement.Update();
				float pos4 = frontBackShelfMovement.pos;
				gameShelves[currentVisibleShelfIndex].transform.position = Vector3.Lerp(shelfRootTransform.position, shelfBackTransform.position, pos4);
				UpdateSoundsForMovement(frontBackShelfMovement);
				if (frontBackShelfMovement.IsAtTarget())
				{
					ChangeShelfMovementState(ShelfMovementState.MoveNextShelfUpward);
				}
			}
			else
			{
				ChangeShelfMovementState(ShelfMovementState.MoveCurrentShelfForward);
			}
			break;
		case ShelfMovementState.MoveCurrentShelfForward:
			if (currentVisibleShelfIndex == selectedShelf)
			{
				frontBackShelfMovement.target = 0f;
				frontBackShelfMovement.Update();
				float pos2 = frontBackShelfMovement.pos;
				gameShelves[currentVisibleShelfIndex].transform.position = Vector3.Lerp(shelfRootTransform.position, shelfBackTransform.position, pos2);
				UpdateSoundsForMovement(frontBackShelfMovement);
				if (frontBackShelfMovement.IsAtTarget())
				{
					ChangeShelfMovementState(ShelfMovementState.Idle);
				}
			}
			else
			{
				SetNextShelf(selectedShelf);
				ChangeShelfMovementState(ShelfMovementState.MoveCurrentShelfBackward);
			}
			break;
		case ShelfMovementState.MoveNextShelfUpward:
			if (nextVisibleShelfIndex == -1)
			{
				ChangeShelfMovementState(ShelfMovementState.Idle);
			}
			else if (nextVisibleShelfIndex == selectedShelf || raiseLowerShelfMovement.pos > 0.5f)
			{
				raiseLowerShelfMovement.target = 1f;
				raiseLowerShelfMovement.Update();
				float pos3 = raiseLowerShelfMovement.pos;
				gameShelves[nextVisibleShelfIndex].transform.position = Vector3.Lerp(shelfLowerTransform.position, shelfRootTransform.position, pos3);
				UpdateSoundsForMovement(raiseLowerShelfMovement);
				if (raiseLowerShelfMovement.IsAtTarget())
				{
					SetCurrentShelf(nextVisibleShelfIndex);
					if (nextVisibleShelfIndex == selectedShelf)
					{
						ChangeShelfMovementState(ShelfMovementState.Idle);
					}
					else
					{
						ChangeShelfMovementState(ShelfMovementState.MoveCurrentShelfBackward);
					}
				}
			}
			else
			{
				ChangeShelfMovementState(ShelfMovementState.MoveNextShelfDownward);
			}
			break;
		case ShelfMovementState.MoveNextShelfDownward:
			if (nextVisibleShelfIndex == -1)
			{
				ChangeShelfMovementState(ShelfMovementState.Idle);
			}
			else if (nextVisibleShelfIndex != selectedShelf)
			{
				raiseLowerShelfMovement.target = 0f;
				raiseLowerShelfMovement.Update();
				float pos = raiseLowerShelfMovement.pos;
				gameShelves[nextVisibleShelfIndex].transform.position = Vector3.Lerp(shelfLowerTransform.position, shelfRootTransform.position, pos);
				UpdateSoundsForMovement(raiseLowerShelfMovement);
				if (raiseLowerShelfMovement.IsAtTarget())
				{
					SetNextShelf(selectedShelf);
					ChangeShelfMovementState(ShelfMovementState.MoveNextShelfUpward);
				}
			}
			else
			{
				ChangeShelfMovementState(ShelfMovementState.MoveNextShelfUpward);
			}
			break;
		}
	}

	private void UpdateSoundsForMovement(GRSpringMovement movement)
	{
		if (movement.IsAtTarget())
		{
			audioSourceLooping.volume = 0f;
			if (movement.HitTargetLastUpdate())
			{
				audioSourceClang.Play();
			}
		}
		else
		{
			audioSourceLooping.volume = Mathf.Clamp01(Math.Abs(movement.speed) * audioSourceLoopingVolume);
		}
	}

	public void SetCurrentShelf(int idx)
	{
		if (idx != -1 && idx != currentVisibleShelfIndex && IsValidShelfItemIndex(idx, 0))
		{
			if (idx == nextVisibleShelfIndex)
			{
				SetNextShelf(-1);
			}
			UpdateShelfVisibility(currentVisibleShelfIndex, isVisible: false);
			frontBackShelfMovement.Reset();
			gameShelves[idx].transform.position = shelfRootTransform.position;
			UpdateShelfVisibility(idx, isVisible: true);
			currentVisibleShelfIndex = idx;
		}
	}

	public void SetNextShelf(int idx)
	{
		if (idx != nextVisibleShelfIndex && idx != currentVisibleShelfIndex)
		{
			if (nextVisibleShelfIndex != -1)
			{
				UpdateShelfVisibility(nextVisibleShelfIndex, isVisible: false);
			}
			if (idx != -1)
			{
				raiseLowerShelfMovement.Reset();
				gameShelves[idx].transform.position = shelfLowerTransform.position;
				UpdateShelfVisibility(idx, isVisible: true);
			}
			nextVisibleShelfIndex = idx;
		}
	}

	public void ChangeShelfMovementState(ShelfMovementState newState)
	{
		shelfMovementState = newState;
		switch (newState)
		{
		case ShelfMovementState.Idle:
			SetCurrentShelf(selectedShelf);
			SetNextShelf(-1);
			break;
		case ShelfMovementState.MoveCurrentShelfBackward:
		case ShelfMovementState.MoveCurrentShelfForward:
		case ShelfMovementState.MoveNextShelfDownward:
			audioSourceLooping.volume = 0f;
			audioSourceLooping.GTPlay();
			break;
		case ShelfMovementState.MoveNextShelfUpward:
			if (currentVisibleShelfIndex == selectedShelf)
			{
				ChangeShelfMovementState(ShelfMovementState.MoveCurrentShelfForward);
			}
			else
			{
				SetNextShelf(selectedShelf);
			}
			audioSourceLooping.volume = 0f;
			audioSourceLooping.GTPlay();
			break;
		}
	}

	public void UpdateShelfVisibility(int shelfID, bool isVisible)
	{
		if (IsValidShelfItemIndex(shelfID, 0))
		{
			gameShelves[shelfID].gameObject.SetActive(isVisible);
			if (isVisible)
			{
				UpdateShelfDisplayElements(shelfID);
			}
		}
	}

	public void UpdateShelfDisplayElements(int shelfID)
	{
		if (IsValidShelfItemIndex(shelfID, 0))
		{
			GRToolUpgradePurchaseStationShelf gRToolUpgradePurchaseStationShelf = gameShelves[shelfID];
			for (int i = 0; i < gRToolUpgradePurchaseStationShelf.gRPurchaseSlots.Count; i++)
			{
				UpdateShelfItemDisplayElements(shelfID, i);
			}
		}
	}

	public void UpdatePurchaseButtonText()
	{
		if (!IsValidShelfItemIndex(selectedShelf, selectedItem))
		{
			purchaseButtonText.text = "ERROR";
			return;
		}
		GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[selectedShelf].gRPurchaseSlots[selectedItem];
		Color color = (gRPurchaseSlot.canAfford ? colorPurchaseButtonCanAfford : colorCantBuy);
		string purchaseText = gRPurchaseSlot.purchaseText;
		if (color != purchaseButtonText.color)
		{
			purchaseButtonText.color = color;
		}
		if (purchaseText != purchaseButtonText.text)
		{
			purchaseButtonText.text = purchaseText;
		}
	}

	public void UpdateShelfItemDisplayElements(int shelf, int slotID)
	{
		if (!IsValidShelfItemIndex(shelf, slotID))
		{
			return;
		}
		GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[shelf].gRPurchaseSlots[slotID];
		if (!toolProgressionManager)
		{
			return;
		}
		GRToolProgressionManager.ToolProgressionMetaData partMetadata = toolProgressionManager.GetPartMetadata(gRPurchaseSlot.PurchaseID);
		if (partMetadata == null)
		{
			gRPurchaseSlot.Name.text = "ERROR";
			return;
		}
		string text = "ERROR";
		string text2 = "";
		_ = Color.white;
		bool flag = true;
		bool flag2 = false;
		int juiceCost = 10000;
		toolProgressionManager.GetPlayerShiftCredit(out var playerShiftCredit);
		int numberOfResearchPoints = toolProgressionManager.GetNumberOfResearchPoints();
		gRPurchaseSlot.canAfford = false;
		gRPurchaseSlot.purchaseText = "LOCKED";
		if (toolProgressionManager.IsPartUnlocked(gRPurchaseSlot.PurchaseID, out var unlocked))
		{
			if (unlocked)
			{
				gameShelves[shelf].SetMaterialOverride(slotID, null);
				if (toolProgressionManager.GetShiftCreditCost(gRPurchaseSlot.PurchaseID, out juiceCost))
				{
					text = $"⑭ {juiceCost}";
				}
				flag2 = playerShiftCredit >= juiceCost;
				gRPurchaseSlot.Name.text = partMetadata.name;
				gRPurchaseSlot.Name.color = ((slotID == selectedItem) ? colorSelectedItem : colorUnselectedItem);
				gRPurchaseSlot.Price.text = text;
				gRPurchaseSlot.Price.color = (flag2 ? colorCanBuyCredits : colorCantBuy);
				gRPurchaseSlot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
				gRPurchaseSlot.canAfford = flag2;
				if (flag2)
				{
					gRPurchaseSlot.purchaseText = $"BUY FOR\n⑭ {juiceCost}";
				}
				else
				{
					gRPurchaseSlot.purchaseText = $"NEED\n⑭ {juiceCost}";
				}
			}
			else
			{
				gameShelves[shelf].SetMaterialOverride(slotID, unresearchedItemMaterial);
				gRPurchaseSlot.Name.text = partMetadata.name;
				gRPurchaseSlot.Name.color = ((slotID == selectedItem) ? colorUnresearchedItem : colorUnselectedUnresearchedItem);
				flag = true;
				if (toolProgressionManager.GetPartUnlockEmployeeRequiredLevel(gRPurchaseSlot.PurchaseID, out var level) && toolProgressionManager.GetCurrentEmployeeLevel() < level)
				{
					toolProgressionManager.GetEmployeeLevelDisplayName(level);
					text2 += $"⑱ {level}\n";
					flag = false;
				}
				cachedRequiredPartsList.Clear();
				if (toolProgressionManager.GetPartUnlockRequiredParentParts(gRPurchaseSlot.PurchaseID, out cachedRequiredPartsList))
				{
					foreach (GRToolProgressionManager.ToolParts cachedRequiredParts in cachedRequiredPartsList)
					{
						bool unlocked2 = false;
						GRToolProgressionManager.ToolProgressionMetaData partMetadata2 = toolProgressionManager.GetPartMetadata(cachedRequiredParts);
						if (partMetadata2 == null)
						{
							text2 += "⑱ ERROR\n";
							flag = false;
						}
						else if (!toolProgressionManager.IsPartUnlocked(cachedRequiredParts, out unlocked2) || !unlocked2)
						{
							text2 = text2 + "⑱ " + partMetadata2.name + "\n";
							flag = false;
						}
					}
				}
				if (!flag)
				{
					gRPurchaseSlot.Price.text = text2;
					gRPurchaseSlot.Price.color = colorCantBuy;
					gRPurchaseSlot.Price.fontSize = ((text2.Length <= 8) ? 2.25f : 1.6f);
					gRPurchaseSlot.canAfford = false;
					gRPurchaseSlot.purchaseText = "LOCKED";
				}
				else
				{
					if (toolProgressionManager.GetPartUnlockJuiceCost(gRPurchaseSlot.PurchaseID, out juiceCost))
					{
						text = $"⑮ {juiceCost}";
					}
					flag2 = numberOfResearchPoints >= juiceCost;
					gRPurchaseSlot.Price.text = text;
					gRPurchaseSlot.Price.color = (flag2 ? colorCanBuyJuice : colorCantBuy);
					gRPurchaseSlot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
					gRPurchaseSlot.canAfford = flag2;
					if (flag2)
					{
						gRPurchaseSlot.purchaseText = $"RESEARCH\n⑮ {juiceCost}";
					}
					else
					{
						gRPurchaseSlot.purchaseText = $"NEED\n⑮ {juiceCost}";
					}
				}
			}
		}
		if (slotID != selectedItem)
		{
			gameShelves[shelf].SetBacklightStateAndMaterial(slotID, isEnabled: false, backlightLocked);
		}
		else if (gRPurchaseSlot.Price.color == colorCanBuyJuice)
		{
			gameShelves[shelf].SetBacklightStateAndMaterial(slotID, isEnabled: true, backlightResearch);
		}
		else if (gRPurchaseSlot.Price.color == colorCanBuyCredits)
		{
			gameShelves[shelf].SetBacklightStateAndMaterial(slotID, isEnabled: true, backlightPurchase);
		}
		else
		{
			gameShelves[shelf].SetBacklightStateAndMaterial(slotID, isEnabled: true, backlightLocked);
		}
	}

	public void UpdatePlayerCurrencyUI()
	{
		if (currentActivePlayerActorNumber == -1)
		{
			playerInfo.text = "AVAILABLE";
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		string text = "";
		GRPlayer gRPlayer2 = GRPlayer.Get(currentActivePlayerActorNumber);
		if (gRPlayer2 == null)
		{
			currentActivePlayerActorNumber = -1;
			playerInfo.text = "AVAILABLE";
			return;
		}
		if (gRPlayer2 == gRPlayer)
		{
			int shiftCredits = gRPlayer2.ShiftCredits;
			int numberOfResearchPoints = toolProgressionManager.GetNumberOfResearchPoints();
			NetPlayer player = NetworkSystem.Instance.GetPlayer(currentActivePlayerActorNumber);
			string text2 = ((player != null) ? player.SanitizedNickName : "RANDO MONKE");
			string employeeLevelDisplayName = toolProgressionManager.GetEmployeeLevelDisplayName(toolProgressionManager.GetCurrentEmployeeLevel());
			text = $"<color=#c0c0c0>{text2}\n{employeeLevelDisplayName}</color>\n\n<color=purple><size=2>⑮ {numberOfResearchPoints}</size></color>\n<color=white><size=2>⑭ {shiftCredits}</size></color>\n";
		}
		else
		{
			NetPlayer player2 = NetworkSystem.Instance.GetPlayer(currentActivePlayerActorNumber);
			text = ((player2 != null) ? player2.SanitizedNickName : "RANDO MONKE") ?? "";
		}
		playerInfo.text = text;
	}

	public bool CanLocalPlayerPurchaseItem(int shelf, int slotID)
	{
		if (!IsValidShelfItemIndex(shelf, slotID))
		{
			return false;
		}
		if ((bool)grManager && grManager.DebugIsToolStationHacked())
		{
			return true;
		}
		UpdateShelfItemDisplayElements(shelf, slotID);
		return gameShelves[shelf].gRPurchaseSlots[slotID].canAfford;
	}

	public bool CheckActivePlayer()
	{
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (currentActivePlayerActorNumber == -1)
		{
			RequestActivePlayerToken();
			return false;
		}
		GRPlayer gRPlayer2 = GRPlayer.Get(currentActivePlayerActorNumber);
		if (gRPlayer2 == null)
		{
			currentActivePlayerActorNumber = -1;
		}
		if (gRPlayer2 != gRPlayer)
		{
			return false;
		}
		return true;
	}

	public void SelectOption1()
	{
		OnLocalSelectionButtonPressed(0);
	}

	public void SelectOption2()
	{
		OnLocalSelectionButtonPressed(1);
	}

	public void SelectOption3()
	{
		OnLocalSelectionButtonPressed(2);
	}

	public void SelectOption4()
	{
		OnLocalSelectionButtonPressed(3);
	}

	public void OnLocalSelectionButtonPressed(int index)
	{
		if (!CheckActivePlayer())
		{
			if (index == 0 && selectedItem != 0)
			{
				select1.SetButtonState(setToOn: false);
			}
			if (index == 1 && selectedItem != 1)
			{
				select2.SetButtonState(setToOn: false);
			}
			if (index == 2 && selectedItem != 2)
			{
				select3.SetButtonState(setToOn: false);
			}
			if (index == 3 && selectedItem != 3)
			{
				select4.SetButtonState(setToOn: false);
			}
			return;
		}
		if (index != 0)
		{
			select1.SetButtonState(setToOn: false);
		}
		if (index != 1)
		{
			select2.SetButtonState(setToOn: false);
		}
		if (index != 2)
		{
			select3.SetButtonState(setToOn: false);
		}
		if (index != 3)
		{
			select4.SetButtonState(setToOn: false);
		}
		if (shelfMovementState == ShelfMovementState.Idle)
		{
			SetSelectedShelfAndItem(selectedShelf, index, fromNetworkRPC: false);
		}
	}

	public void SelectPageDown()
	{
		OnLocalSelectionPageChange(1);
	}

	public void SelectPageUp()
	{
		OnLocalSelectionPageChange(-1);
	}

	public void OnLocalSelectionPageChange(int delta)
	{
		if (CheckActivePlayer())
		{
			pageSelectionWheel.SetTargetShelf((pageSelectionWheel.targetPage + delta + gameShelves.Count) % gameShelves.Count);
		}
	}

	public void CardSwiped()
	{
		RequestActivePlayerToken();
	}

	public void PurchaseButtonPressed()
	{
		if (!disablePurchaseButton)
		{
			purchaseButtonPressed = purchaseButtonCooldown;
			disablePurchaseButton = true;
			if (CheckActivePlayer() && shelfMovementState == ShelfMovementState.Idle && desiredMagnetEntityTypeId == currentMagnetEntityTypeId)
			{
				RequestPurchaseItem(selectedShelf, selectedItem);
			}
		}
	}

	public void DEBUGSetHackToolStation()
	{
	}

	public void RequestActivePlayerToken()
	{
		if (lastRequestedActivePlayerTokenTime > Time.time || lastRequestedActivePlayerTokenTime + requestActivePlayerTokenThrottleTime < Time.time)
		{
			lastRequestedActivePlayerTokenTime = Time.time;
			grManager.RequestStationExclusivity(this);
		}
	}

	private void UpdateMagnet()
	{
		if (desiredMagnetEntityTypeId != currentMagnetEntityTypeId || currentMagnetEntityTypeId == -1 || currentMagnetEntity == null)
		{
			magnetMovement.SetHardStopAtTarget(_hardStopAtTarget: true);
			magnetMovement.target = 0f;
			magnetMovement.Update();
			Vector3 position = ropeTop.transform.position;
			position.y = Mathf.Lerp(prefabMagnetHeightOffset, prefabMagnetHeightOffset - maxMagnetDistance, magnetMovement.pos);
			if (position.y != ropeTop.transform.position.y)
			{
				ropeTop.transform.position = position;
			}
			if (magnetMovement.IsAtTarget() && grManager.IsAuthority() && grManager.IsZoneActive())
			{
				if (currentMagnetEntity != null)
				{
					currentMagnetEntity.transform.parent = null;
					currentMagnetEntity.gameObject.SetActive(value: false);
					grManager.gameEntityManager.RequestDestroyItem(currentMagnetEntity.id);
					currentMagnetEntity = null;
					currentMagnetEntityTypeId = -1;
				}
				if (desiredMagnetEntityTypeId != -1)
				{
					GhostReactor.ToolEntityCreateData toolEntityCreateData = new GhostReactor.ToolEntityCreateData
					{
						decayTime = 0f,
						stationIndex = grManager.GetIndexForToolUpgradeStationFull(this)
					};
					grManager.gameEntityManager.RequestCreateItem(desiredMagnetEntityTypeId, ropeEnd.position, ropeEnd.rotation, toolEntityCreateData.Pack());
					currentMagnetEntityTypeId = desiredMagnetEntityTypeId;
				}
			}
		}
		else if (desiredMagnetEntityTypeId == currentMagnetEntityTypeId && currentMagnetEntity != null)
		{
			magnetMovement.SetHardStopAtTarget(_hardStopAtTarget: false);
			magnetMovement.target = 1f;
			magnetMovement.Update();
			Vector3 position2 = ropeTop.transform.position;
			position2.y = Mathf.Lerp(prefabMagnetHeightOffset, prefabMagnetHeightOffset - maxMagnetDistance, magnetMovement.pos);
			if (ropeTop.transform.position.y != position2.y)
			{
				ropeTop.transform.position = position2;
			}
		}
	}

	public void InitLinkedEntity(GameEntity entity)
	{
		if (currentMagnetEntity != null)
		{
			currentMagnetEntity.gameObject.SetActive(value: false);
		}
		entity.pickupable = false;
		Rigidbody component = entity.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = true;
		}
		GRToolUpgradePurchaseStationMagnetPoint component2 = entity.GetComponent<GRToolUpgradePurchaseStationMagnetPoint>();
		GameDockable component3 = entity.GetComponent<GameDockable>();
		Transform dock = ((component2 != null) ? component2.magnetAttachTransform : ((component3 != null) ? component3.dockablePoint : entity.transform));
		AttachEntityToMagnet_DockGoesToLocation(magnet, entity.transform, dock, new Vector3(0f, -0.03f, 0f));
		float angle = 0f;
		float angle2 = 0f;
		bool flag = false;
		for (int i = 0; i < gameShelves.Count; i++)
		{
			for (int j = 0; j < gameShelves[i].gRPurchaseSlots.Count; j++)
			{
				GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[i].gRPurchaseSlots[j];
				if (gRPurchaseSlot != null && !(gRPurchaseSlot.ToolEntityPrefab == null) && gRPurchaseSlot.ToolEntityPrefab.name != null && gRPurchaseSlot.ToolEntityPrefab.name.GetStaticHash() == entity.typeId)
				{
					angle = gRPurchaseSlot.RopeYaw;
					angle2 = gRPurchaseSlot.RopePitch;
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		Quaternion quaternion = Quaternion.Euler(0f, 0f, 180f);
		quaternion = Quaternion.AngleAxis(angle, Vector3.up) * quaternion;
		quaternion = Quaternion.AngleAxis(angle2, Vector3.forward) * quaternion;
		magnet.localRotation = quaternion;
		magnet.localPosition = quaternion * new Vector3(0f, 0.055f, 0f);
		currentMagnetEntity = entity;
		currentMagnetEntityTypeId = entity.typeId;
	}

	public void UpdateSelectionLever()
	{
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		GRPlayer gRPlayer2 = GRPlayer.Get(currentActivePlayerActorNumber);
		bool flag = ControllerInputPoller.GripFloat(XRNode.LeftHand) > 0.7f;
		bool flag2 = ControllerInputPoller.GripFloat(XRNode.RightHand) > 0.7f;
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		Transform handTransform = gamePlayer.GetHandTransform(0);
		Transform handTransform2 = gamePlayer.GetHandTransform(1);
		Vector3 position = pageSelectionHandle.transform.position;
		Vector3 lhs = handTransform.position - position;
		Vector3 lhs2 = handTransform2.position - position;
		float num = pageSelectionLever.transform.localRotation.eulerAngles.x;
		float num2 = 0.2f;
		float num3 = (bIsGrippingLeft ? 0.15f : 0.1f);
		float num4 = (bIsGrippingRight ? 0.15f : 0.1f);
		if (lhs.sqrMagnitude > num3 * num3)
		{
			flag = false;
		}
		if (lhs2.sqrMagnitude > num4 * num4)
		{
			flag2 = false;
		}
		if (!bGripLeftLastFrame && flag)
		{
			bIsGrippingLeft = true;
		}
		else if (bGripLeftLastFrame && flag)
		{
			Vector3 forward = pageSelectionHandle.transform.forward;
			float num5 = Vector3.Dot(lhs, forward);
			num += num5 / num2 * 180f / 3.1415925f;
		}
		else
		{
			bIsGrippingLeft = false;
		}
		if (!bGripRightLastFrame && flag2)
		{
			bIsGrippingRight = true;
		}
		else if (bGripRightLastFrame && flag2)
		{
			Vector3 forward2 = pageSelectionHandle.transform.forward;
			float num6 = Vector3.Dot(lhs2, forward2);
			num += num6 / num2 * 180f / 3.1415925f;
		}
		else
		{
			bIsGrippingRight = false;
		}
		if (!bIsGrippingLeft && !bIsGrippingRight && gRPlayer == gRPlayer2)
		{
			num = 30f + (num - 30f) * Mathf.Exp(-20f * Time.deltaTime);
		}
		num = Mathf.Clamp(num, 0f, 60f);
		if ((gRPlayer == gRPlayer2 || currentActivePlayerActorNumber == -1) && lastHandleAngle != num)
		{
			pageSelectionLever.transform.localRotation = Quaternion.Euler(num, 0f, 0f);
			lastHandleAngle = num;
		}
		float rotationSpeed = 0f;
		if (bIsGrippingLeft || bIsGrippingRight)
		{
			rotationSpeed = (num - 30f) / 30f;
		}
		bGripLeftLastFrame = flag;
		bGripRightLastFrame = flag2;
		if (gRPlayer == gRPlayer2)
		{
			pageSelectionWheel.isBeingDrivenRemotely = false;
			pageSelectionWheel.SetRotationSpeed(rotationSpeed);
			if (pageSelectionWheel.targetPage != selectedShelf)
			{
				SetSelectedShelfAndItem(pageSelectionWheel.targetPage, 0, fromNetworkRPC: false);
			}
			float num7 = 0.25f;
			timeSinceLastHandleBroadcast += Time.deltaTime;
			if (timeSinceLastHandleBroadcast > num7 && (Math.Abs(num - angleOfLastHandleBroadcast) > 0.02f || Math.Abs(pageSelectionWheel.currentAngle - selectionWheelAngleOfLastBroadcast) > 0.02f))
			{
				timeSinceLastHandleBroadcast = 0f;
				angleOfLastHandleBroadcast = num;
				selectionWheelAngleOfLastBroadcast = pageSelectionWheel.currentAngle;
				grManager.BroadcastHandleAndSelectionWheelPosition(this, (int)(num * quantMult), (int)(selectionWheelAngleOfLastBroadcast * quantMult));
			}
		}
		else if (bIsGrippingLeft || bIsGrippingRight)
		{
			CheckActivePlayer();
		}
	}

	public static void AttachEntityToMagnet_DockGoesToLocation(Transform magnet, Transform entity, Transform dock, Vector3 magnetDockOffset)
	{
		if (!(magnet == null) && !(entity == null) && !(dock == null) && dock.IsChildOf(entity))
		{
			Matrix4x4 m = entity.worldToLocalMatrix * dock.localToWorldMatrix;
			Vector3 s = ExtractLossyScale(m);
			DecomposeTRS(Matrix4x4.TRS(magnetDockOffset, Quaternion.identity, s) * m.inverse, out var pos, out var rot, out var scale);
			entity.SetParent(magnet, worldPositionStays: false);
			entity.localPosition = pos;
			entity.localRotation = rot;
			entity.localScale = scale;
		}
	}

	public void SetHandleAndSelectionWheelPositionRemote(int handlePos, int wheelPos)
	{
		pageSelectionWheel.isBeingDrivenRemotely = true;
		float value = (float)handlePos / quantMult;
		value = Mathf.Clamp(value, 0f, 60f);
		pageSelectionLever.transform.localRotation = Quaternion.Euler(value, 0f, 0f);
		pageSelectionWheel.SetTargetAngle((float)wheelPos / quantMult);
	}

	public void ProgressionUpdated()
	{
		needsUIRefresh = true;
	}

	public void SetSelectedShelfAndItem(int shelf, int item, bool fromNetworkRPC)
	{
		if (!IsValidShelfItemIndex(shelf, item) || toolProgressionManager == null)
		{
			return;
		}
		GRToolProgressionManager.ToolProgressionMetaData partMetadata = toolProgressionManager.GetPartMetadata(gameShelves[shelf].gRPurchaseSlots[item].PurchaseID);
		if (partMetadata != null)
		{
			itemDescriptionName.text = partMetadata.name;
			itemDescription.text = partMetadata.description;
			itemDescriptionAnnotation.text = partMetadata.annotation;
		}
		shelfSelectionText.text = gameShelves[shelf].ShelfName;
		if (gameShelves[shelf].gRPurchaseSlots[item].ToolEntityPrefab != null)
		{
			desiredMagnetEntityTypeId = gameShelves[shelf].gRPurchaseSlots[item].ToolEntityPrefab.name.GetStaticHash();
		}
		else
		{
			desiredMagnetEntityTypeId = -1;
		}
		bool flag = selectedShelf != shelf;
		bool flag2 = selectedItem != item;
		selectedShelf = shelf;
		selectedItem = item;
		needsUIRefresh = true;
		if (!fromNetworkRPC)
		{
			if (flag || flag2)
			{
				grManager.RequestNetworkShelfAndItemChange(this, selectedShelf, selectedItem);
			}
		}
		else
		{
			pageSelectionWheel.SetTargetShelf(selectedShelf);
			select1.SetButtonState(selectedItem == 0);
			select2.SetButtonState(selectedItem == 1);
			select3.SetButtonState(selectedItem == 2);
			select4.SetButtonState(selectedItem == 3);
		}
	}

	public void RequestPurchaseItem(int shelf, int item)
	{
		if (!IsValidShelfItemIndex(shelf, item))
		{
			return;
		}
		GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[shelf].gRPurchaseSlots[item];
		if (!CanLocalPlayerPurchaseItem(shelf, item))
		{
			if (scanner != null)
			{
				scanner.onFailed?.Invoke();
			}
			return;
		}
		if (scanner != null)
		{
			scanner.onSucceeded?.Invoke();
		}
		if (!grManager.DebugIsToolStationHacked() && (!toolProgressionManager.IsPartUnlocked(gRPurchaseSlot.PurchaseID, out var unlocked) || !unlocked))
		{
			toolProgressionManager.AttemptToUnlockPart(gRPurchaseSlot.PurchaseID);
		}
		else
		{
			grManager.RequestPurchaseToolOrUpgrade(this, shelf, item);
		}
	}

	public (bool, bool) TryPurchaseAuthority(GRPlayer player, int shelf, int item)
	{
		if (currentActivePlayerActorNumber == -1)
		{
			return (false, false);
		}
		GRPlayer gRPlayer = GRPlayer.Get(currentActivePlayerActorNumber);
		if (gRPlayer == null)
		{
			currentActivePlayerActorNumber = -1;
			return (false, false);
		}
		if (player != gRPlayer)
		{
			return (false, false);
		}
		if (!grManager.IsAuthority())
		{
			return (false, false);
		}
		if (!IsValidShelfItemIndex(shelf, item))
		{
			return (false, false);
		}
		if (!toolProgressionManager)
		{
			return (false, false);
		}
		GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[shelf].gRPurchaseSlots[item];
		toolProgressionManager.GetPartMetadata(gRPurchaseSlot.PurchaseID);
		return (true, true);
	}

	public void ToolPurchaseResponseLocal(GRPlayer player, int shelf, int item, bool success)
	{
		if (!IsValidShelfItemIndex(shelf, item) || !toolProgressionManager)
		{
			return;
		}
		GRToolUpgradePurchaseStationShelf.GRPurchaseSlot gRPurchaseSlot = gameShelves[shelf].gRPurchaseSlots[item];
		GRToolProgressionManager.ToolProgressionMetaData partMetadata = toolProgressionManager.GetPartMetadata(gRPurchaseSlot.PurchaseID);
		if (partMetadata == null)
		{
			return;
		}
		if (success)
		{
			int shiftCreditCost = partMetadata.shiftCreditCost;
			if (player != null)
			{
				if (player == GRPlayer.Get(VRRig.LocalRig))
				{
					player.IncrementCoresSpentPlayer(shiftCreditCost);
					player.SendToolPurchasedTelemetry(partMetadata.name, item, shiftCreditCost, 0);
				}
				else
				{
					player.IncrementCoresSpentGroup(shiftCreditCost);
				}
				player.AddItemPurchased(partMetadata.name);
				player.SubtractShiftCredit(shiftCreditCost);
				player.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.SpentCredits, shiftCreditCost);
				reactor.RefreshScoreboards();
			}
			if (currentMagnetEntity != null)
			{
				currentMagnetEntity.transform.parent = null;
				currentMagnetEntity.GetComponent<Rigidbody>().isKinematic = false;
				currentMagnetEntity.pickupable = true;
				currentMagnetEntity.createData = 0L;
				currentMagnetEntity = null;
				currentMagnetEntityTypeId = -1;
			}
			purchaseSucceded?.Invoke();
		}
		else
		{
			purchaseFailed?.Invoke();
		}
	}

	public void InitPageSelectionWheel()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < gameShelves.Count; i++)
		{
			list.Add(gameShelves[i].ShelfName);
		}
		pageSelectionWheel.InitFromNameList(list);
	}

	public static Color ColorFromRGB32(int r, int g, int b)
	{
		return new Color((float)r / 255f, (float)g / 255f, (float)b / 255f);
	}

	public bool IsValidShelfItemIndex(int shelf, int idx)
	{
		if (shelf >= 0 && shelf < gameShelves.Count && gameShelves[shelf].gRPurchaseSlots != null && idx >= 0 && idx < gameShelves[shelf].gRPurchaseSlots.Count)
		{
			return gameShelves[shelf].gRPurchaseSlots[idx].PurchaseID != GRToolProgressionManager.ToolParts.None;
		}
		return false;
	}

	private static Vector3 ExtractLossyScale(Matrix4x4 m)
	{
		float magnitude = new Vector3(m.m00, m.m10, m.m20).magnitude;
		float magnitude2 = new Vector3(m.m01, m.m11, m.m21).magnitude;
		float magnitude3 = new Vector3(m.m02, m.m12, m.m22).magnitude;
		return new Vector3(magnitude, magnitude2, magnitude3);
	}

	private static void DecomposeTRS(Matrix4x4 m, out Vector3 pos, out Quaternion rot, out Vector3 scale)
	{
		pos = m.GetColumn(3);
		Vector3 vector = m.GetColumn(0);
		Vector3 vector2 = m.GetColumn(1);
		Vector3 vector3 = m.GetColumn(2);
		scale = new Vector3(vector.magnitude, vector2.magnitude, vector3.magnitude);
		_ = vector / scale.x;
		Vector3 upwards = vector2 / scale.y;
		Vector3 forward = vector3 / scale.z;
		rot = Quaternion.LookRotation(forward, upwards);
	}
}
