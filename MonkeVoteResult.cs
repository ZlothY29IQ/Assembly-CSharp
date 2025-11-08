using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MonkeVoteResult : MonoBehaviour
{
	[SerializeField]
	private GameObject _optionIndicator;

	[SerializeField]
	private TMP_Text _optionText;

	[FormerlySerializedAs("_scoreLabelPost")]
	[SerializeField]
	private GameObject _scoreIndicator;

	[SerializeField]
	private TMP_Text _scoreText;

	[SerializeField]
	private GameObject _voteIndicator;

	[SerializeField]
	private GameObject _guessWinIndicator;

	[SerializeField]
	private GameObject _guessLoseIndicator;

	[SerializeField]
	private GameObject _mostPopularIndicator;

	[SerializeField]
	private GameObject _youWinIndicator;

	[SerializeField]
	private RockPiles _rockPiles;

	private MonkeVoteMachine _machine;

	private string _text = string.Empty;

	private bool _canVote;

	private float _rockPileHeight;

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_optionText.text = (_text = value);
		}
	}

	public void ShowResult(string questionOption, int percentage, bool showVote, bool showPrediction, bool isWinner)
	{
		_optionText.text = questionOption;
		_optionIndicator.SetActive(value: true);
		_scoreText.text = ((percentage >= 0) ? $"{percentage}%" : "--");
		_voteIndicator.SetActive(showVote);
		_guessWinIndicator.SetActive(showPrediction && isWinner);
		_guessLoseIndicator.SetActive(showPrediction && !isWinner);
		_youWinIndicator.SetActive(isWinner && showPrediction);
		_mostPopularIndicator.SetActive(isWinner);
		ShowRockPile(percentage);
	}

	public void HideResult()
	{
		_optionIndicator.SetActive(value: false);
		_voteIndicator.SetActive(value: false);
		_guessWinIndicator.SetActive(value: false);
		_guessLoseIndicator.SetActive(value: false);
		_youWinIndicator.SetActive(value: false);
		_mostPopularIndicator.SetActive(value: false);
		ShowRockPile(0);
	}

	private void ShowRockPile(int percentage)
	{
		_rockPiles.Show(percentage);
	}

	public void SetDynamicMeshesVisible(bool visible)
	{
		_mostPopularIndicator.SetActive(visible);
		_voteIndicator.SetActive(visible);
		_guessWinIndicator.SetActive(visible);
		_guessLoseIndicator.SetActive(visible);
		_rockPiles.Show(visible ? 100 : (-1));
	}
}
