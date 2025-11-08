using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MonkeVoteOption : MonoBehaviour
{
	[SerializeField]
	private Collider _trigger;

	[SerializeField]
	private TMP_Text _optionText;

	[SerializeField]
	private VotingCard _voteIndicator;

	[FormerlySerializedAs("_predictionIndicator")]
	[SerializeField]
	private VotingCard _guessIndicator;

	private string _text = string.Empty;

	private bool _canVote;

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

	public bool CanVote
	{
		get
		{
			return _canVote;
		}
		set
		{
			_trigger.enabled = (_canVote = value);
		}
	}

	public event Action<MonkeVoteOption, Collider> OnVote;

	private void Reset()
	{
		Configure();
	}

	private void Configure()
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.isTrigger)
			{
				_trigger = collider;
				break;
			}
		}
		if (!_optionText)
		{
			_optionText = GetComponentInChildren<TMP_Text>();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsValidVotingRock(other))
		{
			this.OnVote?.Invoke(this, other);
		}
	}

	private bool IsValidVotingRock(Collider other)
	{
		SlingshotProjectile component = other.GetComponent<SlingshotProjectile>();
		if ((bool)component)
		{
			return component.projectileOwner.IsLocal;
		}
		return false;
	}

	public void ResetState()
	{
		this.OnVote = null;
		ShowIndicators(showVote: false, showPrediction: false);
	}

	public void ShowIndicators(bool showVote, bool showPrediction, bool instant = true)
	{
		_voteIndicator.SetVisible(showVote, instant);
		_guessIndicator.SetVisible(showPrediction, instant);
	}

	private void Vote()
	{
		SendVote(null);
	}

	private void SendVote(Collider other)
	{
		if (_canVote)
		{
			this.OnVote?.Invoke(this, other);
		}
	}

	public void SetDynamicMeshesVisible(bool visible)
	{
		_voteIndicator.SetVisible(visible, instant: true);
		_guessIndicator.SetVisible(visible, instant: true);
	}
}
