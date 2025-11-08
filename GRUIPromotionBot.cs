using System;
using System.Text;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class GRUIPromotionBot : MonoBehaviourTick
{
	public enum PromotionBotState
	{
		WaitingForLogin,
		ChoosePromotion,
		ChooseCreditIncrease,
		ChoosePurchaseCredits,
		ConfirmPurchaseCredits,
		CelebratePromotion,
		TryingLogIn
	}

	private static string EVENT_PROMOTED = "GRPromoted";

	private GhostReactor reactor;

	public TMP_Text startScreenText;

	public TMP_Text userInfo;

	public TMP_Text menuText;

	public TMP_Text descriptionText;

	public TMP_Text yesText;

	public TMP_Text noText;

	public TMP_Text purchaseSuccessText;

	public IDCardScanner scanner;

	public GameObject particlesGO;

	public AudioSource levelUpSound;

	public AudioSource popSound;

	private string defaultText = "-N/A-\n-N/A-\n-N/A-\n-N/A-\n-N/A-\n\n-N/A-";

	private string promotionTextStr1 = "CONGRATULATIONS\n ";

	private string promotionTextStr2 = ".\n\nYOU ARE NOW A GRADE ";

	private string promotionTextStr3 = ".\n\nYOU MAY TAKE TWO UNPAID MINUTES TO CELEBRATE, THEN RETURN TO WORK.";

	private string inertButtonText = "-";

	private string buttonReturnText = "-RETURN-";

	private string requestPromotionText = "REQUEST PROMOTION";

	public const string newLine = "\n";

	public int currentPlayerActorNumber;

	public PromotionBotState currentState;

	public float timeOutTime;

	public float distanceForAutoLogout = 2.5f;

	private StringBuilder cachedStringBuilder = new StringBuilder(512);

	private float timeLastDistanceCheck;

	private float timeBetweenDistanceChecks = 0.5f;

	public string FormattedUserInfo()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer == null)
		{
			return "ERROR";
		}
		(int tier, int grade, int totalPointsToNextLevel, int partialPointsToNextLevel) gradePointDetails = GhostReactorProgression.GetGradePointDetails(gRPlayer.CurrentProgression.redeemedPoints);
		int item = gradePointDetails.totalPointsToNextLevel;
		int item2 = gradePointDetails.partialPointsToNextLevel;
		NetPlayer player = NetworkSystem.Instance.GetPlayer(currentPlayerActorNumber);
		string titleNameAndGrade = GhostReactorProgression.GetTitleNameAndGrade(gRPlayer.CurrentProgression.redeemedPoints);
		int num = 1000 + gRPlayer.ShiftCreditCapIncreases * 100;
		int num2 = gRPlayer.CurrentProgression.points - gRPlayer.CurrentProgression.redeemedPoints + item2;
		string text = ((player != null) ? player.SanitizedNickName : "RANDO MONKE");
		cachedStringBuilder.Clear();
		cachedStringBuilder.Append("<color=#808080>EMPLOYEE:</color>     " + text + "\n");
		cachedStringBuilder.Append("<color=#808080>TITLE:</color>        " + titleNameAndGrade + "\n");
		cachedStringBuilder.Append($"<color=#808080>XP:</color>           {num2}/{item}\n");
		if (gRPlayer == GRPlayer.GetLocal())
		{
			cachedStringBuilder.Append($"<color=#808080>CREDITS:</color>      <color=#00ff00>⑭ {gRPlayer.ShiftCredits}</color>\n");
			cachedStringBuilder.Append($"<color=#808080>CREDIT LIMIT:</color> <color=#00a000>⑭ {num}</color>\n");
			int num3 = -1;
			if (reactor != null && reactor.toolProgression != null)
			{
				num3 = reactor.toolProgression.GetNumberOfResearchPoints();
				cachedStringBuilder.Append($"<color=#808080>JUICE:</color>        <color=purple>⑮ {num3}</color>\n");
			}
			int num4 = -1;
			if (ProgressionManager.Instance != null)
			{
				num4 = ProgressionManager.Instance.GetShinyRocksTotal();
				cachedStringBuilder.Append($"<color=#808080>SHINY ROCKS:</color>  <color=white>⑯ {num4}</color>\n");
			}
		}
		return cachedStringBuilder.ToString();
	}

	public bool ActivePlayerEligibleForPromotion()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer == null)
		{
			return false;
		}
		(int tier, int grade, int totalPointsToNextLevel, int partialPointsToNextLevel) gradePointDetails = GhostReactorProgression.GetGradePointDetails(gRPlayer.CurrentProgression.redeemedPoints);
		int item = gradePointDetails.totalPointsToNextLevel;
		int item2 = gradePointDetails.partialPointsToNextLevel;
		if (item - item2 < gRPlayer.CurrentProgression.points - gRPlayer.CurrentProgression.redeemedPoints)
		{
			return true;
		}
		return false;
	}

	public void Init(GhostReactor _reactor)
	{
		reactor = _reactor;
		currentPlayerActorNumber = -1;
		currentState = PromotionBotState.WaitingForLogin;
	}

	public void Refresh()
	{
		RefreshPlayerData();
	}

	public override void Tick()
	{
		if (reactor == null || reactor.grManager == null || !reactor.grManager.IsAuthority())
		{
			return;
		}
		float time = Time.time;
		if (currentPlayerActorNumber != -1 && (timeLastDistanceCheck > time || time > timeLastDistanceCheck + timeBetweenDistanceChecks))
		{
			GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
			if (gRPlayer == null || (base.transform.position - gRPlayer.transform.position).sqrMagnitude > distanceForAutoLogout * distanceForAutoLogout)
			{
				SwitchState(PromotionBotState.WaitingForLogin);
			}
		}
	}

	public bool CheckIsActivePlayer()
	{
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		GRPlayer gRPlayer2 = GRPlayer.Get(currentPlayerActorNumber);
		return gRPlayer == gRPlayer2;
	}

	public void UpPressed()
	{
		if (CheckIsActivePlayer())
		{
			switch (currentState)
			{
			case PromotionBotState.ChoosePurchaseCredits:
				SwitchState(PromotionBotState.ChooseCreditIncrease);
				break;
			case PromotionBotState.ChooseCreditIncrease:
				SwitchState(PromotionBotState.ChoosePromotion);
				break;
			}
		}
	}

	public void DownPressed()
	{
		if (CheckIsActivePlayer())
		{
			switch (currentState)
			{
			case PromotionBotState.ChoosePromotion:
				SwitchState(PromotionBotState.ChooseCreditIncrease);
				break;
			case PromotionBotState.ChooseCreditIncrease:
				SwitchState(PromotionBotState.ChoosePurchaseCredits);
				break;
			}
		}
	}

	public void YesPressed()
	{
		if (CheckIsActivePlayer())
		{
			switch (currentState)
			{
			case PromotionBotState.ChoosePurchaseCredits:
				SwitchState(PromotionBotState.ConfirmPurchaseCredits);
				break;
			case PromotionBotState.ConfirmPurchaseCredits:
				SwitchState(PromotionBotState.ChoosePurchaseCredits);
				break;
			case PromotionBotState.ChooseCreditIncrease:
				AttemptPurchaseShiftCreditIncrease();
				break;
			case PromotionBotState.ChoosePromotion:
				AttemptPromotion();
				break;
			}
		}
	}

	public void NoPressed()
	{
		if (CheckIsActivePlayer())
		{
			switch (currentState)
			{
			case PromotionBotState.ConfirmPurchaseCredits:
				AttemptPurchaseShiftCreditRefillToMax();
				break;
			case PromotionBotState.ChoosePromotion:
			case PromotionBotState.ChooseCreditIncrease:
			case PromotionBotState.ChoosePurchaseCredits:
				SwitchState(PromotionBotState.WaitingForLogin);
				break;
			}
		}
	}

	public void SwitchState(PromotionBotState newState, bool fromRPC = false)
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		GRPlayer gRPlayer2 = GRPlayer.Get(VRRig.LocalRig);
		if (gRPlayer2 == null)
		{
			return;
		}
		RefreshPlayerData();
		_ = currentState;
		currentState = newState;
		SetScreenVisibility();
		SetMenuText(newState);
		switch (newState)
		{
		case PromotionBotState.ChoosePromotion:
			if (ActivePlayerEligibleForPromotion())
			{
				descriptionText.text = "<color=#c0c0c0>     YOU ARE ELIGIBLE FOR A PROMOTION!\n     PRESS 'YES' TO CONTINUE</color>";
			}
			else
			{
				descriptionText.text = "<color=#c04040>     YOU ARE NOT ELIGIBLE FOR A PROMOTION\n     EARN MORE XP BY COMPLETING SHIFT GOALS</color>";
			}
			break;
		case PromotionBotState.ChooseCreditIncrease:
			if (gRPlayer.ShiftCreditCapIncreases != gRPlayer.ShiftCreditCapIncreasesMax)
			{
				descriptionText.text = "<color=#c0c0c0>     INCREASE CREDIT LIMIT BY <color=#00ff00>⑭ 100</color>\n     FOR <color=purple>⑮ 2</color> JUICE?</color>";
			}
			else
			{
				descriptionText.text = "<color=#c0c0c0>     CREDIT LIMIT CAN'T BE INCREASED AT THIS TIME\n</color>";
			}
			break;
		case PromotionBotState.ChoosePurchaseCredits:
		{
			if (gRPlayer == null)
			{
				descriptionText.text = "No active player";
				break;
			}
			int purchaseToCreditCapAmount2 = GetPurchaseToCreditCapAmount();
			if (purchaseToCreditCapAmount2 > 0)
			{
				descriptionText.text = $"<color=#c0c0c0>     PURCHASE <color=#00ff00>+⑭{purchaseToCreditCapAmount2}</color> CREDITS\n     FOR <color=white>100 SHINY ROCKS?</color>";
			}
			else
			{
				descriptionText.text = "<color=#c0c0c0>     YOU ARE AT FULL CREDITS";
			}
			break;
		}
		case PromotionBotState.ConfirmPurchaseCredits:
		{
			int purchaseToCreditCapAmount = GetPurchaseToCreditCapAmount();
			descriptionText.text = $"<color=#c0c0c0>     CONFIRM PURCHASE <color=#00ff00>+⑭{purchaseToCreditCapAmount}</color>\n     FOR <color=white>100 SHINY ROCKS?</color>";
			break;
		}
		}
		if (currentState == PromotionBotState.ConfirmPurchaseCredits)
		{
			yesText.text = "<size=0.4>CANCEL</size>";
			noText.text = "<size=0.4>CONFIRM</size>";
		}
		else
		{
			if (yesText.text != "YES")
			{
				yesText.text = "YES";
			}
			if (noText.text != "NO")
			{
				noText.text = "NO";
			}
		}
		if (reactor != null && reactor.grManager != null && !fromRPC && (gRPlayer == gRPlayer2 || reactor.grManager.IsAuthority()))
		{
			reactor.grManager.PromotionBotActivePlayerRequest((int)currentState);
		}
	}

	public int GetPurchaseToCreditCapAmount()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		int shiftCredits = gRPlayer.ShiftCredits;
		int num = 1000 + gRPlayer.ShiftCreditCapIncreases * 100;
		return Math.Max(0, num - shiftCredits);
	}

	public void CelebratePromotion()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (!(gRPlayer == null))
		{
			particlesGO.SetActive(value: false);
			particlesGO.SetActive(value: true);
			levelUpSound.Play();
			popSound.Play();
			PlayerGameEvents.MiscEvent(EVENT_PROMOTED);
			gRPlayer.SendRankUpTelemetry(GhostReactorProgression.GetTitleNameAndGrade(gRPlayer.CurrentProgression.redeemedPoints));
		}
	}

	public void SetMenuText(PromotionBotState menuState)
	{
		switch (menuState)
		{
		case PromotionBotState.ChoosePromotion:
			menuText.text = "-> REQUEST PROMOTION\n   INCREASE CREDIT LIMIT\n   BRIBE ACCOUNTING FOR CREDITS\n";
			break;
		case PromotionBotState.ChooseCreditIncrease:
			menuText.text = "   REQUEST PROMOTION\n-> INCREASE CREDIT LIMIT\n   BRIBE ACCOUNTING FOR CREDITS\n";
			break;
		case PromotionBotState.ChoosePurchaseCredits:
		case PromotionBotState.ConfirmPurchaseCredits:
			menuText.text = "   REQUEST PROMOTION\n   INCREASE CREDIT LIMIT\n-> BRIBE ACCOUNTING FOR CREDITS\n";
			break;
		}
	}

	public void SetScreenVisibility()
	{
		startScreenText.gameObject.SetActive(currentState == PromotionBotState.WaitingForLogin);
		userInfo.gameObject.SetActive(currentState != PromotionBotState.WaitingForLogin);
		menuText.gameObject.SetActive(currentState != PromotionBotState.WaitingForLogin);
		descriptionText.gameObject.SetActive(currentState != PromotionBotState.WaitingForLogin);
		purchaseSuccessText.gameObject.SetActive(value: false);
	}

	public void RefreshPlayerData()
	{
		userInfo.text = FormattedUserInfo();
	}

	public void OnPurchaseCallback(bool success)
	{
		if (success)
		{
			purchaseSuccessText.text = "<color=#80ff80>     PURCHASE SUCCEEDED!</color>";
			RefreshPlayerData();
			purchaseSuccessText.gameObject.SetActive(value: true);
			scanner.onSucceeded?.Invoke();
		}
		else
		{
			purchaseSuccessText.text = "<color=#ff8080>     FAILED PURCHASE. NO CHARGE.</color>";
			RefreshPlayerData();
			purchaseSuccessText.gameObject.SetActive(value: true);
			scanner.onFailed?.Invoke();
		}
	}

	public void OnJuiceUpdated()
	{
		RefreshPlayerData();
	}

	public void OnGetShiftCredit(string mothershipId, int credit)
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer != null && gRPlayer.mothershipId == mothershipId)
		{
			RefreshPlayerData();
		}
	}

	public void OnShinyRocksUpdated()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer != null && gRPlayer.gamePlayer.IsLocal())
		{
			RefreshPlayerData();
		}
	}

	public void OnGetShiftCreditCapData(string mothershipId, int creditCap, int creditCapMax)
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer != null && gRPlayer.mothershipId == mothershipId)
		{
			RefreshPlayerData();
		}
	}

	public void AttemptPromotion()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if ((bool)gRPlayer && gRPlayer.AttemptPromotion() && reactor != null && reactor.grManager != null)
		{
			CelebratePromotion();
			RefreshPlayerData();
			RefreshActivePlayerBadge();
			string titleName = GhostReactorProgression.GetTitleName(gRPlayer.CurrentProgression.redeemedPoints);
			int grade = GhostReactorProgression.GetGrade(gRPlayer.CurrentProgression.redeemedPoints);
			purchaseSuccessText.text = $"CONGRATULATIONS, {titleName} {grade}!";
			purchaseSuccessText.gameObject.SetActive(value: true);
		}
	}

	public void AttemptPurchaseShiftCreditIncrease()
	{
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer == null)
		{
			Debug.Log("AttemptPurchaseShiftCreditIncrease currentPlayer null");
		}
		else if (gRPlayer.ShiftCreditCapIncreases != gRPlayer.ShiftCreditCapIncreasesMax)
		{
			Debug.Log($"AttemptPurchaseShiftCreditIncrease currentPlayer ShiftCreditCapIncreases {gRPlayer.ShiftCreditCapIncreases} ShiftCreditCapIncreasesMax {gRPlayer.ShiftCreditCapIncreasesMax}");
			int num = 2;
			if (gRPlayer != null && gRPlayer.gamePlayer.IsLocal() && gRPlayer.ShiftCreditCapIncreases < gRPlayer.ShiftCreditCapIncreasesMax && reactor.toolProgression.GetNumberOfResearchPoints() >= num && ProgressionManager.Instance != null)
			{
				ProgressionManager.Instance.PurchaseShiftCreditCapIncrease();
			}
			RefreshPlayerData();
		}
	}

	public void AttemptPurchaseShiftCreditRefillToMax()
	{
		if (GetPurchaseToCreditCapAmount() == 0)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer == null)
		{
			Debug.Log("AttemptPurchaseShiftCreditIncrease currentPlayer null");
			return;
		}
		int num = 100;
		int num2 = 1000 + gRPlayer.ShiftCreditCapIncreases * num;
		Debug.Log($"AttemptPurchaseShiftCreditIncrease currentPlayer ShiftCredits {gRPlayer.ShiftCredits} ShiftCreditMax {num2}");
		if (gRPlayer != null && gRPlayer.gamePlayer.IsLocal() && gRPlayer.ShiftCredits < num2)
		{
			int num3 = 100;
			if (ProgressionManager.Instance != null && ProgressionManager.Instance.GetShinyRocksTotal() >= num3)
			{
				ProgressionManager.Instance.PurchaseShiftCredit();
			}
		}
		RefreshPlayerData();
		SwitchState(PromotionBotState.ChoosePurchaseCredits);
	}

	public void PlayerSwipedID()
	{
		if (!(reactor == null) && !(reactor.grManager == null))
		{
			if (currentPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				scanner.onSucceeded?.Invoke();
				return;
			}
			if (currentPlayerActorNumber != -1 && GRPlayer.Get(currentPlayerActorNumber) != null)
			{
				scanner.onFailed?.Invoke();
				return;
			}
			reactor.grManager.PromotionBotActivePlayerRequest(6);
			scanner.onSucceeded?.Invoke();
		}
	}

	public void RefreshActivePlayerBadge()
	{
		if (currentPlayerActorNumber == -1)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(currentPlayerActorNumber);
		if (gRPlayer != null && currentPlayerActorNumber != -1)
		{
			NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(currentPlayerActorNumber);
			if (netPlayerByID != null && gRPlayer.badge != null)
			{
				gRPlayer.badge.RefreshText(netPlayerByID);
			}
		}
	}

	public void SetActivePlayerStateChange(int actorNumber, int state)
	{
		if (state == 0)
		{
			RefreshActivePlayerBadge();
			actorNumber = -1;
		}
		bool flag = currentPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		bool flag2 = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		if (flag && !flag2)
		{
			if (ProgressionManager.Instance != null)
			{
				ProgressionManager.Instance.OnPurchaseShiftCredit -= OnPurchaseCallback;
				ProgressionManager.Instance.OnPurchaseShiftCreditCapIncrease -= OnPurchaseCallback;
				ProgressionManager.Instance.OnInventoryUpdated -= OnJuiceUpdated;
				ProgressionManager.Instance.OnGetShiftCredit -= OnGetShiftCredit;
				ProgressionManager.Instance.OnGetShiftCreditCapData -= OnGetShiftCreditCapData;
			}
			if (CosmeticsController.instance != null)
			{
				CosmeticsController instance = CosmeticsController.instance;
				instance.OnGetCurrency = (Action)Delegate.Remove(instance.OnGetCurrency, new Action(OnShinyRocksUpdated));
			}
		}
		else if (!flag && flag2)
		{
			if (ProgressionManager.Instance != null)
			{
				ProgressionManager.Instance.OnPurchaseShiftCredit += OnPurchaseCallback;
				ProgressionManager.Instance.OnPurchaseShiftCreditCapIncrease += OnPurchaseCallback;
				ProgressionManager.Instance.OnInventoryUpdated += OnJuiceUpdated;
				ProgressionManager.Instance.OnGetShiftCredit += OnGetShiftCredit;
				ProgressionManager.Instance.OnGetShiftCreditCapData += OnGetShiftCreditCapData;
			}
			if (CosmeticsController.instance != null)
			{
				CosmeticsController instance2 = CosmeticsController.instance;
				instance2.OnGetCurrency = (Action)Delegate.Combine(instance2.OnGetCurrency, new Action(OnShinyRocksUpdated));
			}
		}
		currentPlayerActorNumber = actorNumber;
		SwitchState((PromotionBotState)state, fromRPC: true);
	}

	public int GetCurrentPlayerActorNumber()
	{
		return currentPlayerActorNumber;
	}
}
