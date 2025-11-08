using TMPro;
using UnityEngine;

public class GorillaTagCompetitiveRankDisplay : MonoBehaviour
{
	[SerializeField]
	private SpriteRenderer progressBar;

	[SerializeField]
	private float progressBarSize = 100f;

	[SerializeField]
	private SpriteRenderer currentRankSprite;

	[SerializeField]
	private SpriteRenderer prevRankSprite;

	[SerializeField]
	private SpriteRenderer nextRankSprite;

	[SerializeField]
	private TextMeshPro currentRank_Name;

	[SerializeField]
	private TextMeshPro prevText;

	[SerializeField]
	private TextMeshPro nextText;

	[SerializeField]
	private TextMeshPro prevRank_Name;

	[SerializeField]
	private TextMeshPro nextRank_Name;

	private void OnEnable()
	{
		VRRig.LocalRig.OnRankedSubtierChanged += HandleRankedSubtierChanged;
		HandleRankedSubtierChanged(0, 0);
	}

	private void OnDisable()
	{
		VRRig.LocalRig.OnRankedSubtierChanged -= HandleRankedSubtierChanged;
	}

	public void HandleRankedSubtierChanged(int questSubTier, int pcSubTier)
	{
		float currentELO = RankedProgressionManager.Instance.GetCurrentELO();
		int progressionRankIndex = RankedProgressionManager.Instance.GetProgressionRankIndex(currentELO);
		UpdateRankIcons(progressionRankIndex);
		UpdateRankProgress(RankedProgressionManager.Instance.GetProgressionRankProgress());
	}

	private void UpdateRankIcons(int currentRank)
	{
		currentRankSprite.sprite = RankedProgressionManager.Instance.GetProgressionRankIcon(currentRank);
		currentRank_Name.text = RankedProgressionManager.Instance.GetProgressionRankName().ToUpper();
		bool flag = currentRank < RankedProgressionManager.Instance.MaxRank;
		bool flag2 = currentRank > 0;
		nextRankSprite.gameObject.SetActive(flag);
		nextText.gameObject.SetActive(flag);
		nextRank_Name.gameObject.SetActive(flag);
		if (flag)
		{
			nextRankSprite.sprite = RankedProgressionManager.Instance.GetNextProgressionRankIcon(currentRank);
			nextRank_Name.text = RankedProgressionManager.Instance.GetNextProgressionRankName(currentRank).ToUpper();
		}
		prevRankSprite.gameObject.SetActive(flag2);
		prevText.gameObject.SetActive(flag2);
		prevRank_Name.gameObject.SetActive(flag2);
		if (flag2)
		{
			prevRankSprite.sprite = RankedProgressionManager.Instance.GetPrevProgressionRankIcon(currentRank);
			prevRank_Name.text = RankedProgressionManager.Instance.GetPrevProgressionRankName(currentRank).ToUpper();
		}
	}

	private void UpdateRankProgress(float percent)
	{
		percent = Mathf.Clamp01(percent);
		Vector2 size = progressBar.size;
		size.x = progressBarSize * percent;
		progressBar.size = size;
	}
}
