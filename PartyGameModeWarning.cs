using System.Collections;
using GorillaTagScripts;
using UnityEngine;

public class PartyGameModeWarning : MonoBehaviour
{
	[SerializeField]
	private GameObject[] showParts;

	[SerializeField]
	private GameObject[] hideParts;

	[SerializeField]
	private float visibleDuration;

	private float visibleUntilTimestamp;

	private Coroutine hideCoroutine;

	public bool ShouldShowWarning
	{
		get
		{
			if (FriendshipGroupDetection.Instance.IsInParty)
			{
				return FriendshipGroupDetection.Instance.AnyPartyMembersOutsideFriendCollider();
			}
			return false;
		}
	}

	private void Awake()
	{
		GameObject[] array = showParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	public void Show()
	{
		visibleUntilTimestamp = Time.time + visibleDuration;
		if (hideCoroutine == null)
		{
			hideCoroutine = StartCoroutine(HideCo());
		}
	}

	private IEnumerator HideCo()
	{
		GameObject[] array = showParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		array = hideParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		float lastVisible;
		do
		{
			lastVisible = visibleUntilTimestamp;
			yield return new WaitForSeconds(visibleUntilTimestamp - Time.time);
		}
		while (lastVisible != visibleUntilTimestamp);
		array = showParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = hideParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		hideCoroutine = null;
	}
}
