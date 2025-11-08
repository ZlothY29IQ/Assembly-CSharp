using TMPro;
using UnityEngine;

public class GorillaTagCompetitiveScoreboardLine : MonoBehaviour
{
	public SpriteRenderer rankSprite;

	public TMP_Text playerNameDisplay;

	public TMP_Text untaggedTimeDisplay;

	public TMP_Text tagCountDisplay;

	public SpriteRenderer resultSprite;

	public Sprite[] resultSprites;

	public void SetPlayer(string playerName, Sprite icon)
	{
		playerNameDisplay.text = playerName;
		rankSprite.sprite = icon;
	}

	public void SetScore(float untaggedTime, int tagCount)
	{
		int num = Mathf.FloorToInt(untaggedTime);
		int num2 = num / 60;
		int num3 = num % 60;
		untaggedTimeDisplay.text = $"{num2}:{num3:D2}";
		tagCountDisplay.text = tagCount.ToString();
	}

	public void SetPredictedResult(GorillaTagCompetitiveScoreboard.PredictedResult result)
	{
		resultSprite.sprite = resultSprites[(int)result];
	}

	public void DisplayPredictedResults(bool bShow)
	{
		resultSprite.gameObject.SetActive(bShow);
	}

	public void SetInfected(bool infected)
	{
		playerNameDisplay.color = (infected ? Color.red : Color.white);
	}
}
