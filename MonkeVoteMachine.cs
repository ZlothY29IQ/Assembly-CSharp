using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameObjectScheduling;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MonkeVoteMachine : MonoBehaviour
{
	public enum VotingState
	{
		None,
		Voting,
		Predicting,
		Complete
	}

	public class PollEntry
	{
		public int PollId;

		public string Question;

		public string[] VoteOptions;

		public int[] VoteCount;

		public int[] PredictionCount;

		public DateTime StartTime;

		public DateTime EndTime;

		public bool IsValid
		{
			get
			{
				string[] voteOptions = VoteOptions;
				if (voteOptions != null)
				{
					return voteOptions.Length == 2;
				}
				return false;
			}
		}

		public PollEntry(int pollId, string question, string[] voteOptions)
		{
			PollId = pollId;
			Question = question;
			VoteOptions = voteOptions;
			VoteCount = new int[2];
			VoteCount[0] = UnityEngine.Random.Range(0, 50000);
			VoteCount[1] = UnityEngine.Random.Range(0, 50000);
			PredictionCount = new int[2];
			PredictionCount[0] = UnityEngine.Random.Range(0, 50000);
			PredictionCount[1] = UnityEngine.Random.Range(0, 50000);
			StartTime = DateTime.Now;
			EndTime = DateTime.Now + TimeSpan.FromSeconds(20.0);
		}

		public PollEntry(MonkeVoteController.FetchPollsResponse poll)
		{
			PollId = poll.PollId;
			Question = poll.Question;
			VoteOptions = poll.VoteOptions.ToArray();
			VoteCount = poll.VoteCount.ToArray();
			PredictionCount = poll.PredictionCount.ToArray();
			StartTime = poll.StartTime;
			EndTime = poll.EndTime;
		}

		public int GetWinner()
		{
			if (VoteCount == null || VoteCount.Length == 0)
			{
				return -1;
			}
			int num = int.MinValue;
			int result = -1;
			for (int i = 0; i < VoteCount.Length; i++)
			{
				if (VoteCount[i] > num)
				{
					num = VoteCount[i];
					result = i;
				}
			}
			return result;
		}
	}

	private const string kVoteCurrentIdKey = "Vote_Current_Id";

	private const string kVoteCurrentOptionKey = "Vote_Current_Option";

	private const string kVoteCurrentPredictionKey = "Vote_Current_Prediction";

	private const string kVoteCurrentStreak = "Vote_Current_Streak";

	private const string kVotePreviousIdKey = "Vote_Previous_Id";

	private const string kVotePreviousOptionKey = "Vote_Previous_Option";

	private const string kVotePreviousPredictionKey = "Vote_Previous_Prediction";

	private const string kVotePreviousStreak = "Vote_Previous_Streak";

	[SerializeField]
	private MonkeVoteProximityTrigger _proximityTrigger;

	[Header("VOTING")]
	[SerializeField]
	private string _pollsClosedText = "POLLS CLOSED";

	[SerializeField]
	private string _defaultTitle = "MONKE VOTE";

	[SerializeField]
	private string _voteTitle = "VOTE";

	[SerializeField]
	private string _predictTitle = "GUESS";

	[SerializeField]
	private string _completeTitle = "VOTING COMPLETE";

	[SerializeField]
	private string _defaultQuestion = "COME BACK LATER";

	[SerializeField]
	private string _predictQuestion = "WHICH WILL BE MORE POPULAR?";

	[Tooltip("Must be in the format \"STREAK: {0}\"")]
	[SerializeField]
	private string _streakBlurb = "PREDICTION STREAK: {0}";

	[Tooltip("Must be in the format \"LOST {0} PREDICTION STREAK! STREAK: {1}\"")]
	[SerializeField]
	private string _streakLostBlurb = "<color=red>{0} POLL STREAK LOST!</color>  STREAK: {1}";

	[SerializeField]
	private float _voteCooldown = 1f;

	[SerializeField]
	private MonkeVoteOption[] _votingOptions;

	[SerializeField]
	private CountdownText _timerText;

	[SerializeField]
	private TMP_Text _titleText;

	[SerializeField]
	private TMP_Text _questionText;

	[Header("RESULTS")]
	[SerializeField]
	private string _defaultResultsTitle = "PREVIOUS QUESTION";

	[SerializeField]
	private TMP_Text _resultsTitleText;

	[SerializeField]
	private TMP_Text _resultsQuestionText;

	[SerializeField]
	private TMP_Text _resultsStreakText;

	[SerializeField]
	private MonkeVoteResult[] _results;

	[FormerlySerializedAs("_sound")]
	[Header("FX")]
	[SerializeField]
	private AudioSource _audio;

	[FormerlySerializedAs("_voteProcessingAudio")]
	[SerializeField]
	private AudioSource _voteTubeAudio;

	[SerializeField]
	private AudioClip[] _voteFailSound;

	[SerializeField]
	private AudioClip[] _voteSuccessDing;

	[FormerlySerializedAs("_voteSuccessSound")]
	[SerializeField]
	private AudioClip[] _voteProcessingSound;

	private VotingState _state;

	private float _voteCooldownEnd;

	private bool _waitingOnVote;

	private PollEntry _currentPoll;

	private PollEntry _previousPoll;

	private DateTime _nextPollUpdate;

	private bool _isTestingPoll;

	private void Reset()
	{
		Configure();
	}

	private void Awake()
	{
		_proximityTrigger.OnEnter += OnPlayerEnteredVoteProximity;
	}

	private void Start()
	{
		MonkeVoteController.instance.OnPollsUpdated += HandleOnPollsUpdated;
		MonkeVoteController.instance.OnVoteAccepted += HandleOnVoteAccepted;
		MonkeVoteController.instance.OnVoteFailed += HandleOnVoteFailed;
		MonkeVoteController.instance.OnCurrentPollEnded += HandleCurrentPollEnded;
		Init();
	}

	private void OnDestroy()
	{
		_proximityTrigger.OnEnter -= OnPlayerEnteredVoteProximity;
		MonkeVoteController.instance.OnPollsUpdated -= HandleOnPollsUpdated;
		MonkeVoteController.instance.OnVoteAccepted -= HandleOnVoteAccepted;
		MonkeVoteController.instance.OnVoteFailed -= HandleOnVoteFailed;
		MonkeVoteController.instance.OnCurrentPollEnded -= HandleCurrentPollEnded;
	}

	public void Init()
	{
		_isTestingPoll = false;
		_previousPoll = (_currentPoll = null);
		_waitingOnVote = false;
		MonkeVoteOption[] votingOptions = _votingOptions;
		foreach (MonkeVoteOption obj in votingOptions)
		{
			obj.ResetState();
			obj.OnVote += OnVoteEntered;
		}
		UpdatePollDisplays();
	}

	private void OnPlayerEnteredVoteProximity()
	{
		MonkeVoteController.instance.RequestPolls();
	}

	private void HandleOnPollsUpdated()
	{
		UpdatePollDisplays();
	}

	private void UpdatePollDisplays()
	{
		if (MonkeVoteController.instance == null)
		{
			SetState(VotingState.None);
			ShowResults(null);
			return;
		}
		MonkeVoteController.FetchPollsResponse lastPollData = MonkeVoteController.instance.GetLastPollData();
		if (lastPollData != null)
		{
			_previousPoll = new PollEntry(lastPollData);
			ShowResults(_previousPoll);
		}
		else
		{
			ShowResults(null);
		}
		MonkeVoteController.FetchPollsResponse currentPollData = MonkeVoteController.instance.GetCurrentPollData();
		if (currentPollData != null)
		{
			_nextPollUpdate = MonkeVoteController.instance.GetCurrentPollCompletionTime();
			_currentPoll = new PollEntry(currentPollData);
			PollEntry currentPoll = _currentPoll;
			if (currentPoll != null && currentPoll.IsValid)
			{
				(int voteOption, int predictionOption) vote = GetVote(_currentPoll.PollId);
				int item = vote.voteOption;
				int item2 = vote.predictionOption;
				VotingState newState = ((item < 0) ? VotingState.Voting : ((item2 < 0) ? VotingState.Predicting : VotingState.Complete));
				SetState(newState);
			}
			else
			{
				SetState(VotingState.None);
			}
		}
		else
		{
			SetState(VotingState.None);
		}
	}

	private void HandleOnVoteAccepted()
	{
		int lastVotePollId = MonkeVoteController.instance.GetLastVotePollId();
		int lastVoteSelectedOption = MonkeVoteController.instance.GetLastVoteSelectedOption();
		bool lastVoteWasPrediction = MonkeVoteController.instance.GetLastVoteWasPrediction();
		OnVoteResponseReceived(lastVotePollId, lastVoteSelectedOption, lastVoteWasPrediction, success: true);
	}

	private void HandleOnVoteFailed()
	{
		_waitingOnVote = false;
		int lastVotePollId = MonkeVoteController.instance.GetLastVotePollId();
		int lastVoteSelectedOption = MonkeVoteController.instance.GetLastVoteSelectedOption();
		bool lastVoteWasPrediction = MonkeVoteController.instance.GetLastVoteWasPrediction();
		OnVoteResponseReceived(lastVotePollId, lastVoteSelectedOption, lastVoteWasPrediction, success: false);
	}

	private void HandleCurrentPollEnded()
	{
		if (_proximityTrigger.isPlayerNearby)
		{
			MonkeVoteController.instance.RequestPolls();
		}
	}

	[Tooltip("Hide dynamic child meshes to avoid them getting combined into the parent mesh on awake")]
	private void HideDynamicMeshes()
	{
		SetDynamicMeshesVisible(enabled: false);
	}

	[Tooltip("Show dynamic child meshes to allow easy visualization")]
	private void ShowDynamicMeshes()
	{
		SetDynamicMeshesVisible(enabled: true);
	}

	private void SetDynamicMeshesVisible(bool enabled)
	{
		MonkeVoteOption[] votingOptions = _votingOptions;
		for (int i = 0; i < votingOptions.Length; i++)
		{
			votingOptions[i].SetDynamicMeshesVisible(enabled);
		}
		MonkeVoteResult[] results = _results;
		for (int i = 0; i < results.Length; i++)
		{
			results[i].SetDynamicMeshesVisible(enabled);
		}
	}

	private void Configure()
	{
		_audio = GetComponentInChildren<AudioSource>();
		_audio.spatialBlend = 1f;
		_votingOptions = GetComponentsInChildren<MonkeVoteOption>();
		_results = GetComponentsInChildren<MonkeVoteResult>();
	}

	public void CreateNextDummyPoll()
	{
		_isTestingPoll = true;
		if (_currentPoll != null)
		{
			_previousPoll = _currentPoll;
		}
		else
		{
			_previousPoll = null;
		}
		ShowResults(_previousPoll);
		int pollId = 0;
		if (_previousPoll != null)
		{
			pollId = _previousPoll.PollId + 1;
		}
		string question = "Test Question Number: " + UnityEngine.Random.Range(1, 101);
		string text = "Answer " + UnityEngine.Random.Range(1, 101);
		string text2 = "Answer " + UnityEngine.Random.Range(1, 101);
		string[] voteOptions = new string[2] { text, text2 };
		_currentPoll = new PollEntry(pollId, question, voteOptions);
		PollEntry currentPoll = _currentPoll;
		if (currentPoll != null && currentPoll.IsValid)
		{
			(int voteOption, int predictionOption) vote = GetVote(_currentPoll.PollId);
			int item = vote.voteOption;
			int item2 = vote.predictionOption;
			VotingState newState = ((item < 0) ? VotingState.Voting : ((item2 < 0) ? VotingState.Predicting : VotingState.Complete));
			SetState(newState);
		}
		else
		{
			SetState(VotingState.None);
		}
	}

	private void VoteLeft()
	{
		OnVoteEntered(_votingOptions[0], null);
	}

	private void VoteRight()
	{
		OnVoteEntered(_votingOptions[1], null);
	}

	private void VoteWinner()
	{
		if (_currentPoll != null)
		{
			if (_currentPoll.VoteCount[0] > _currentPoll.VoteCount[1])
			{
				OnVoteEntered(_votingOptions[0], null);
			}
			else
			{
				OnVoteEntered(_votingOptions[1], null);
			}
		}
	}

	private void ClearLocalData()
	{
		ClearLocalVoteAndPredictionData();
		UpdatePollDisplays();
	}

	private void SetState(VotingState newState, bool instant = true)
	{
		_state = newState;
		bool flag = _currentPoll?.IsValid ?? false;
		if (_state < VotingState.None || _state > VotingState.Complete || (_state != VotingState.None && !flag))
		{
			_state = VotingState.None;
		}
		if (flag)
		{
			int item = GetVote(_currentPoll.PollId).predictionOption;
			if (_state < VotingState.Predicting)
			{
				SaveVote(_currentPoll.PollId, -1, item);
			}
			int item2 = GetVote(_currentPoll.PollId).voteOption;
			if (_state < VotingState.Complete)
			{
				SaveVote(_currentPoll.PollId, item2, -1);
			}
		}
		bool flag2 = true;
		switch (_state)
		{
		case VotingState.None:
			_timerText.SetFixedText(_pollsClosedText);
			_titleText.text = _defaultTitle;
			_questionText.text = _defaultQuestion;
			flag2 = false;
			break;
		case VotingState.Voting:
			_timerText.SetCountdownTime(_nextPollUpdate);
			_titleText.text = _voteTitle;
			_questionText.text = _currentPoll.Question;
			break;
		case VotingState.Predicting:
			_timerText.SetCountdownTime(_nextPollUpdate);
			_titleText.text = _predictTitle;
			_questionText.text = _predictQuestion;
			break;
		case VotingState.Complete:
			_timerText.SetCountdownTime(_nextPollUpdate);
			_titleText.text = _completeTitle;
			_questionText.text = _currentPoll.Question;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		int num;
		int num2;
		if (flag)
		{
			(num, num2) = GetVote(_currentPoll.PollId);
		}
		else
		{
			num = -1;
			num2 = -1;
		}
		if (flag2)
		{
			for (int i = 0; i < _votingOptions.Length; i++)
			{
				_votingOptions[i].Text = _currentPoll.VoteOptions[i];
				_votingOptions[i].ShowIndicators(num == i, num2 == i, instant);
			}
			return;
		}
		MonkeVoteOption[] votingOptions = _votingOptions;
		foreach (MonkeVoteOption obj in votingOptions)
		{
			obj.Text = string.Empty;
			obj.ShowIndicators(showVote: false, showPrediction: false);
		}
	}

	private void ShowResults(PollEntry entry)
	{
		if (entry != null && entry.IsValid)
		{
			var (num, num2) = GetVote(entry.PollId);
			GTDev.Log($"Showing {entry.Question} V:{num} P:{num2}");
			List<int> list = ConvertToPercentages(entry.VoteCount);
			int num3 = 0;
			int num4 = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] > num3)
				{
					num3 = list[i];
					num4 = i;
				}
			}
			_resultsTitleText.text = _defaultResultsTitle;
			_resultsQuestionText.text = entry.Question;
			for (int j = 0; j < entry.VoteOptions.Length; j++)
			{
				_results[j].ShowResult(entry.VoteOptions[j], list[j], num == j, num2 == j, num4 == j);
			}
			int prePollStreak = GetPrePollStreak(entry.PollId);
			int postPollStreak = GetPostPollStreak(entry);
			_resultsStreakText.text = ((postPollStreak >= prePollStreak) ? string.Format(_streakBlurb, postPollStreak) : string.Format(_streakLostBlurb, prePollStreak, postPollStreak));
		}
		else
		{
			_resultsTitleText.text = _defaultResultsTitle;
			_resultsQuestionText.text = _defaultQuestion;
			_resultsStreakText.text = string.Empty;
			MonkeVoteResult[] results = _results;
			for (int k = 0; k < results.Length; k++)
			{
				results[k].HideResult();
			}
		}
	}

	private List<int> ConvertToPercentages(int[] votes)
	{
		List<int> list = new List<int>();
		List<float> list2 = new List<float>();
		if (votes == null || votes.Length == 0)
		{
			list.Add(-1);
			list.Add(-1);
			return list;
		}
		if (votes.Length == 1)
		{
			list.Add(100);
			list.Add(0);
			return list;
		}
		int num = Sum(votes);
		if (num == 0)
		{
			list.Add(-1);
			list.Add(-1);
			return list;
		}
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < votes.Length; i++)
		{
			if (votes[i] > num2)
			{
				num2 = votes[i];
				num3 = i;
			}
			float num4 = (float)votes[i] / (float)num * 100f;
			list.Add((int)num4);
			list2.Add(num4 - (float)(int)num4);
		}
		int num5 = Sum(list);
		int num6 = 100 - num5;
		for (int j = 0; j < num6; j++)
		{
			int index = LargestFractionIndex(list2);
			list[index]++;
			list2[index] = 0f;
		}
		if (list.Count == 2 && list[num3] == 50)
		{
			list[num3]++;
			list[1 - num3]--;
		}
		return list;
		static int LargestFractionIndex(IList<float> fractions)
		{
			float num7 = float.NegativeInfinity;
			int result = -1;
			for (int k = 0; k < fractions.Count; k++)
			{
				if (fractions[k] > num7)
				{
					num7 = fractions[k];
					result = k;
				}
			}
			return result;
		}
		static int Sum(IList<int> items)
		{
			int num7 = 0;
			foreach (int item in items)
			{
				num7 += item;
			}
			return num7;
		}
	}

	private void OnVoteEntered(MonkeVoteOption option, Collider votingCollider)
	{
		if (_waitingOnVote || (Time.realtimeSinceStartup < _voteCooldownEnd && !_isTestingPoll))
		{
			PlayVoteFailEffects();
			return;
		}
		int num = Array.IndexOf(_votingOptions, option);
		if (num >= 0)
		{
			switch (_state)
			{
			case VotingState.Voting:
				Vote(_currentPoll.PollId, num, isPrediction: false);
				break;
			case VotingState.Predicting:
				Vote(_currentPoll.PollId, num, isPrediction: true);
				break;
			default:
				PlayVoteFailEffects();
				break;
			}
		}
	}

	private void Vote(int id, int option, bool isPrediction)
	{
		if (option >= 0 && !_waitingOnVote)
		{
			_waitingOnVote = true;
			if (_isTestingPoll)
			{
				OnVoteResponseReceived(id, option, isPrediction, success: true);
			}
			else
			{
				MonkeVoteController.instance.Vote(id, option, isPrediction);
			}
		}
	}

	private void OnVoteResponseReceived(int id, int option, bool isPrediction, bool success)
	{
		_waitingOnVote = false;
		if (success)
		{
			PlayVoteSuccessEffects();
			_voteCooldownEnd = Time.realtimeSinceStartup + _voteCooldown;
			int num;
			int num2;
			(num, num2) = GetVote(id);
			if (!isPrediction)
			{
				int num3 = num2;
				num = option;
				num2 = num3;
			}
			else
			{
				int num4 = num;
				int num3 = option;
				num = num4;
				num2 = num3;
			}
			SaveVote(id, num, num2);
			switch (_state)
			{
			case VotingState.Voting:
				SetState(VotingState.Predicting, instant: false);
				break;
			case VotingState.Predicting:
				SetState(VotingState.Complete, instant: false);
				break;
			}
			if (isPrediction && id == _currentPoll.PollId)
			{
				SavePrePollStreak(id, GetPostPollStreak(_previousPoll));
			}
		}
		else
		{
			PlayVoteFailEffects();
		}
	}

	private async void PlayVoteSuccessEffects()
	{
		_audio.GTPlayOneShot(_voteSuccessDing, _audio.volume);
		await Task.Delay(500);
		_voteTubeAudio.GTPlayOneShot(_voteProcessingSound, _voteTubeAudio.volume);
	}

	private void PlayVoteFailEffects()
	{
		_audio.GTPlayOneShot(_voteFailSound, _audio.volume);
	}

	private void SaveVote(int id, int voteOption, int predictionOption)
	{
		int num = PlayerPrefs.GetInt("Vote_Current_Id", -1);
		if (num == -1 || num == id)
		{
			PlayerPrefs.SetInt("Vote_Current_Id", id);
			PlayerPrefs.SetInt("Vote_Current_Option", voteOption);
			PlayerPrefs.SetInt("Vote_Current_Prediction", predictionOption);
		}
		else
		{
			PlayerPrefs.SetInt("Vote_Previous_Id", num);
			PlayerPrefs.SetInt("Vote_Previous_Option", PlayerPrefs.GetInt("Vote_Current_Option"));
			PlayerPrefs.SetInt("Vote_Previous_Prediction", PlayerPrefs.GetInt("Vote_Current_Prediction"));
			PlayerPrefs.SetInt("Vote_Previous_Streak", PlayerPrefs.GetInt("Vote_Current_Streak"));
			PlayerPrefs.SetInt("Vote_Current_Id", id);
			PlayerPrefs.SetInt("Vote_Current_Option", voteOption);
			PlayerPrefs.SetInt("Vote_Current_Prediction", predictionOption);
			PlayerPrefs.SetInt("Vote_Current_Streak", 0);
		}
		PlayerPrefs.Save();
	}

	private (int voteOption, int predictionOption) GetVote(int voteId)
	{
		if (PlayerPrefs.GetInt("Vote_Current_Id", -1) == voteId)
		{
			int item = PlayerPrefs.GetInt("Vote_Current_Option", -1);
			int item2 = PlayerPrefs.GetInt("Vote_Current_Prediction", -1);
			return (voteOption: item, predictionOption: item2);
		}
		if (PlayerPrefs.GetInt("Vote_Previous_Id", -1) == voteId)
		{
			int item3 = PlayerPrefs.GetInt("Vote_Previous_Option", -1);
			int item4 = PlayerPrefs.GetInt("Vote_Previous_Prediction", -1);
			return (voteOption: item3, predictionOption: item4);
		}
		return (voteOption: -1, predictionOption: -1);
	}

	private void SavePrePollStreak(int id, int streak)
	{
		if (id >= 0)
		{
			if (PlayerPrefs.GetInt("Vote_Current_Id", -1) == id)
			{
				PlayerPrefs.SetInt("Vote_Current_Streak", streak);
			}
			else if (PlayerPrefs.GetInt("Vote_Previous_Id", -1) == id)
			{
				PlayerPrefs.SetInt("Vote_Previous_Streak", streak);
			}
		}
	}

	private int GetPrePollStreak(int id)
	{
		if (id < 0)
		{
			return 0;
		}
		if (PlayerPrefs.GetInt("Vote_Current_Id", -1) == id)
		{
			return PlayerPrefs.GetInt("Vote_Current_Streak", 0);
		}
		if (PlayerPrefs.GetInt("Vote_Previous_Id", -1) == id)
		{
			return PlayerPrefs.GetInt("Vote_Previous_Streak", 0);
		}
		return 0;
	}

	private int GetPostPollStreak(PollEntry entry)
	{
		if (entry == null || !entry.IsValid)
		{
			return 0;
		}
		int item = GetVote(entry.PollId).predictionOption;
		if (item < 0)
		{
			return 0;
		}
		int prePollStreak = GetPrePollStreak(entry.PollId);
		if (item != entry.GetWinner())
		{
			return 0;
		}
		return prePollStreak + 1;
	}

	private void ClearLocalVoteAndPredictionData()
	{
		PlayerPrefs.DeleteKey("Vote_Current_Id");
		PlayerPrefs.DeleteKey("Vote_Current_Option");
		PlayerPrefs.DeleteKey("Vote_Current_Prediction");
		PlayerPrefs.DeleteKey("Vote_Current_Streak");
		PlayerPrefs.DeleteKey("Vote_Previous_Id");
		PlayerPrefs.DeleteKey("Vote_Previous_Option");
		PlayerPrefs.DeleteKey("Vote_Previous_Prediction");
		PlayerPrefs.DeleteKey("Vote_Previous_Streak");
	}
}
