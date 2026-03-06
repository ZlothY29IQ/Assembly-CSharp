using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GRUIStoreDisplay : MonoBehaviour
{
	[Serializable]
	public class GRPurchaseSlot
	{
		public TMP_Text Name;

		public TMP_Text Price;

		public TMP_Text Description;

		public GRToolProgressionManager.ToolParts PurchaseID;

		[NonSerialized]
		public Material overrideMaterial;

		[NonSerialized]
		public bool canAfford;

		[NonSerialized]
		public string purchaseText = "";

		public ProgressionManager.DrillUpgradeLevel drillUpgradeLevel;
	}

	public IDCardScanner scanner;

	public GRPurchaseSlot slot;

	private GhostReactor reactor;

	private GRToolProgressionManager toolProgressionManager;

	private int playerActorId;

	private Color colorPurchaseButtonCanAfford = GRToolUpgradePurchaseStationFull.ColorFromRGB32(0, 0, 0);

	private Color colorCanBuyCredits = GRToolUpgradePurchaseStationFull.ColorFromRGB32(140, 229, 37);

	private Color colorCanBuyJuice = GRToolUpgradePurchaseStationFull.ColorFromRGB32(232, 65, 255);

	private Color colorCantBuy = GRToolUpgradePurchaseStationFull.ColorFromRGB32(140, 38, 38);

	private Color colorSelectedItem = GRToolUpgradePurchaseStationFull.ColorFromRGB32(251, 240, 229);

	private Color colorUnselectedItem = GRToolUpgradePurchaseStationFull.ColorFromRGB32(147, 145, 140);

	private Color colorUnresearchedItem = GRToolUpgradePurchaseStationFull.ColorFromRGB32(230, 19, 17);

	private Color colorUnselectedUnresearchedItem = GRToolUpgradePurchaseStationFull.ColorFromRGB32(133, 11, 10);

	private List<GRToolProgressionManager.ToolParts> cachedRequiredPartsList = new List<GRToolProgressionManager.ToolParts>(5);

	public void Awake()
	{
	}

	public void OnEnable()
	{
		RefreshUI();
	}

	public void OnDisable()
	{
	}

	public void Setup(int playerActorId, GhostReactor reactor)
	{
		this.reactor = reactor;
		toolProgressionManager = reactor.toolProgression;
		this.playerActorId = playerActorId;
		RefreshUI();
		toolProgressionManager.OnProgressionUpdated += onProgressionUpdated;
	}

	private void onProgressionUpdated()
	{
		RefreshUI();
	}

	private void RefreshUI()
	{
		RefreshItemInfo();
	}

	public void OnBuy(int playerActorNumber)
	{
		if (playerActorNumber != playerActorId || GRPlayer.Get(playerActorId) == null)
		{
			return;
		}
		if (!CanLocalPlayerPurchaseItem())
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
		if (reactor.grManager.DebugIsToolStationHacked() || (toolProgressionManager.IsPartUnlocked(slot.PurchaseID, out var unlocked) && unlocked))
		{
			return;
		}
		if (slot.drillUpgradeLevel == ProgressionManager.DrillUpgradeLevel.Base)
		{
			if (ProgressionManager.Instance.GetShinyRocksTotal() >= 2500)
			{
				ProgressionManager.Instance.PurchaseDrillUpgrade(ProgressionManager.DrillUpgradeLevel.Base);
			}
		}
		else
		{
			toolProgressionManager.AttemptToUnlockPart(slot.PurchaseID);
		}
	}

	private bool CanLocalPlayerPurchaseItem()
	{
		return slot.canAfford;
	}

	public void RefreshItemInfo()
	{
		bool flag = true;
		if (!(toolProgressionManager != null))
		{
			return;
		}
		GRToolProgressionManager.ToolProgressionMetaData partMetadata = toolProgressionManager.GetPartMetadata(slot.PurchaseID);
		if (partMetadata == null)
		{
			slot.Name.text = "ERROR";
			return;
		}
		string text = "ERROR";
		string text2 = "";
		_ = Color.white;
		bool flag2 = true;
		bool flag3 = false;
		int juiceCost = 10000;
		toolProgressionManager.GetPlayerShiftCredit(out var playerShiftCredit);
		int numberOfResearchPoints = toolProgressionManager.GetNumberOfResearchPoints();
		slot.canAfford = false;
		slot.purchaseText = "LOCKED";
		if (slot.Description != null)
		{
			slot.Description.text = partMetadata.description;
		}
		if (!toolProgressionManager.IsPartUnlocked(slot.PurchaseID, out var unlocked))
		{
			return;
		}
		if (unlocked)
		{
			if (slot.drillUpgradeLevel != ProgressionManager.DrillUpgradeLevel.None)
			{
				slot.Price.color = colorCanBuyCredits;
				slot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
				slot.canAfford = true;
				slot.purchaseText = "Purchased";
				text = slot.purchaseText;
				slot.Price.text = text;
				return;
			}
			if (toolProgressionManager.GetShiftCreditCost(slot.PurchaseID, out juiceCost))
			{
				text = $"⑭ {juiceCost}";
			}
			flag3 = playerShiftCredit >= juiceCost;
			slot.Name.text = partMetadata.name;
			slot.Name.color = (flag ? colorSelectedItem : colorUnselectedItem);
			slot.Price.text = text;
			slot.Price.color = (flag3 ? colorCanBuyCredits : colorCantBuy);
			slot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
			slot.canAfford = flag3;
			if (flag3)
			{
				slot.purchaseText = $"BUY FOR\n⑭ {juiceCost}";
			}
			else
			{
				slot.purchaseText = $"NEED\n⑭ {juiceCost}";
			}
			return;
		}
		slot.Name.text = partMetadata.name;
		slot.Name.color = (flag ? colorUnresearchedItem : colorUnselectedUnresearchedItem);
		flag2 = true;
		if (toolProgressionManager.GetPartUnlockEmployeeRequiredLevel(slot.PurchaseID, out var level) && toolProgressionManager.GetCurrentEmployeeLevel() < level)
		{
			toolProgressionManager.GetEmployeeLevelDisplayName(level);
			text2 += $"⑱ {level}\n";
			flag2 = false;
		}
		cachedRequiredPartsList.Clear();
		if (toolProgressionManager.GetPartUnlockRequiredParentParts(slot.PurchaseID, out cachedRequiredPartsList))
		{
			foreach (GRToolProgressionManager.ToolParts cachedRequiredParts in cachedRequiredPartsList)
			{
				bool unlocked2 = false;
				GRToolProgressionManager.ToolProgressionMetaData partMetadata2 = toolProgressionManager.GetPartMetadata(cachedRequiredParts);
				if (partMetadata2 == null)
				{
					text2 += "⑱ ERROR\n";
					flag2 = false;
				}
				else if (!toolProgressionManager.IsPartUnlocked(cachedRequiredParts, out unlocked2) || !unlocked2)
				{
					text2 = text2 + "⑱ " + partMetadata2.name + "\n";
					flag2 = false;
				}
			}
		}
		if (!flag2)
		{
			slot.Price.text = text2;
			slot.Price.color = colorCantBuy;
			slot.Price.fontSize = ((text2.Length <= 8) ? 2.25f : 1.6f);
			slot.canAfford = false;
			slot.purchaseText = "LOCKED";
			return;
		}
		if (slot.drillUpgradeLevel == ProgressionManager.DrillUpgradeLevel.Base)
		{
			slot.Price.color = colorCanBuyCredits;
			slot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
			slot.canAfford = true;
			slot.purchaseText = $"Cost {2500}⑯ Shiny Rocks";
			text = slot.purchaseText;
			slot.Price.text = text;
			return;
		}
		if (toolProgressionManager.GetPartUnlockJuiceCost(slot.PurchaseID, out juiceCost))
		{
			text = $"⑮ {juiceCost}";
		}
		flag3 = numberOfResearchPoints >= juiceCost;
		slot.Price.text = text;
		slot.Price.color = (flag3 ? colorCanBuyJuice : colorCantBuy);
		slot.Price.fontSize = ((text.Length <= 8) ? 2.25f : 1.6f);
		slot.canAfford = flag3;
		if (flag3)
		{
			slot.purchaseText = $"RESEARCH\n⑮ {juiceCost}";
		}
		else
		{
			slot.purchaseText = $"NEED\n⑮ {juiceCost}";
		}
	}
}
