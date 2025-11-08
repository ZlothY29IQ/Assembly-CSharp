using System.Collections;
using UnityEngine;

public class VotingCard : MonoBehaviour
{
	[SerializeField]
	private GameObject _card;

	[SerializeField]
	private Transform _offPosition;

	[SerializeField]
	private Transform _onPosition;

	[SerializeField]
	private float activationTime = 0.5f;

	private bool _isVisible;

	private void MoveToOffPosition()
	{
		_card.transform.position = _offPosition.position;
	}

	private void MoveToOnPosition()
	{
		_card.transform.position = _onPosition.position;
	}

	public void SetVisible(bool showVote, bool instant)
	{
		if (_isVisible != showVote)
		{
			StopAllCoroutines();
		}
		if (instant)
		{
			_card.transform.position = (showVote ? _onPosition.position : _offPosition.position);
			_card.SetActive(showVote);
		}
		else if (showVote)
		{
			if (_isVisible != showVote)
			{
				StartCoroutine(DoActivate());
			}
		}
		else
		{
			_card.SetActive(value: false);
			_card.transform.position = _offPosition.position;
		}
		_isVisible = showVote;
	}

	private IEnumerator DoActivate()
	{
		Vector3 from = _offPosition.position;
		Vector3 to = _onPosition.position;
		_card.transform.position = from;
		_card.SetActive(value: true);
		float lerpVal = 0f;
		while (lerpVal < 1f)
		{
			lerpVal += Time.deltaTime / activationTime;
			_card.transform.position = Vector3.Lerp(from, to, lerpVal);
			yield return null;
		}
	}
}
