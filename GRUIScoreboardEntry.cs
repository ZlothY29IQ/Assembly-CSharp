using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GRUIScoreboardEntry : MonoBehaviour
{
	[SerializeField]
	private TMP_Text playerNameLabel;

	[SerializeField]
	private TMP_Text playerCutLabel;

	public GameObject defaultUIParent;

	[SerializeField]
	private TMP_Text playerTitleLabel;

	[SerializeField]
	private TMP_Text playerCurrencyLabel;

	public GameObject shiftCutParent;

	[SerializeField]
	private TMP_Text playerTimeLabel;

	[SerializeField]
	private TMP_Text playerPercentageLabel;

	private int playerActorId = -1;

	private int currencySet = -1;

	private string titleSet = "";

	public void Setup(VRRig vrRig, int playerActorId, GRUIScoreboard.ScoreboardScreen screenType)
	{
		this.playerActorId = playerActorId;
		Refresh(vrRig, screenType);
	}

	private void Refresh(VRRig vrRig, GRUIScoreboard.ScoreboardScreen screenType)
	{
		GRPlayer gRPlayer = GRPlayer.Get(vrRig);
		if (vrRig != null && gRPlayer != null)
		{
			if (!playerNameLabel.text.Equals(vrRig.playerNameVisible))
			{
				playerNameLabel.text = vrRig.playerNameVisible;
			}
			switch (screenType)
			{
			case GRUIScoreboard.ScoreboardScreen.DefaultInfo:
			{
				defaultUIParent.SetActive(value: true);
				shiftCutParent.SetActive(value: false);
				if (gRPlayer.ShiftCredits != currencySet)
				{
					currencySet = gRPlayer.ShiftCredits;
					playerCurrencyLabel.text = currencySet.ToString();
				}
				string titleNameAndGrade = GhostReactorProgression.GetTitleNameAndGrade(gRPlayer.CurrentProgression.redeemedPoints);
				if (titleNameAndGrade != titleSet)
				{
					titleSet = titleNameAndGrade;
					playerTitleLabel.text = titleSet;
				}
				break;
			}
			case GRUIScoreboard.ScoreboardScreen.ShiftCutCalculation:
				defaultUIParent.SetActive(value: false);
				shiftCutParent.SetActive(value: true);
				if (GhostReactor.instance.shiftManager != null && (GhostReactor.instance.shiftManager.ShiftActive || GhostReactor.instance.shiftManager.ShiftTotalEarned >= 0))
				{
					int num = Mathf.FloorToInt(gRPlayer.ShiftPlayTime / 60f);
					int num2 = Mathf.FloorToInt(gRPlayer.ShiftPlayTime - (float)(num * 60));
					playerTimeLabel.text = $"{num:00}:{num2:00}";
					playerPercentageLabel.text = "%" + Mathf.Floor(gRPlayer.ShiftPlayTime / GhostReactor.instance.shiftManager.TotalPlayTime * 100f);
				}
				else
				{
					playerTimeLabel.text = "n/a";
					playerPercentageLabel.text = "n/a";
				}
				playerTitleLabel.text = titleSet;
				break;
			}
			if (GhostReactor.instance.shiftManager == null || GhostReactor.instance.shiftManager.ShiftActive)
			{
				playerCutLabel.text = "-";
			}
			else
			{
				playerCutLabel.text = gRPlayer.LastShiftCut.ToString();
			}
		}
		else
		{
			playerNameLabel.text = "";
			playerCurrencyLabel.text = "";
			playerTitleLabel.text = "";
			playerCutLabel.text = "";
			currencySet = 0;
		}
	}
}
