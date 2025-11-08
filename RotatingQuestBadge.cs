using System;
using System.Collections;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using TMPro;
using UnityEngine;

public class RotatingQuestBadge : MonoBehaviour, ISpawnable
{
	[Serializable]
	public struct BadgeLevel
	{
		public GameObject badge;

		public int requiredPoints;
	}

	[SerializeField]
	private TextMeshPro displayField;

	[SerializeField]
	private bool forWardrobe;

	[SerializeField]
	private VRRig myRig;

	[SerializeField]
	private BadgeLevel[] badgeLevels;

	public bool IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		if (forWardrobe && !myRig)
		{
			TryGetRig();
			return;
		}
		myRig = rig;
		myRig.OnQuestScoreChanged += OnProgressScoreChanged;
		OnProgressScoreChanged(myRig.GetCurrentQuestScore());
	}

	public void OnDespawn()
	{
	}

	private void OnEnable()
	{
		if (forWardrobe)
		{
			SetBadgeLevel(-1);
			if (!TryGetRig())
			{
				StartCoroutine(DoFindRig());
			}
		}
	}

	private void OnDisable()
	{
		if (forWardrobe && (bool)myRig)
		{
			myRig.OnQuestScoreChanged -= OnProgressScoreChanged;
			myRig = null;
		}
	}

	private IEnumerator DoFindRig()
	{
		WaitForSeconds intervalWait = new WaitForSeconds(0.1f);
		while (!TryGetRig())
		{
			yield return intervalWait;
		}
	}

	private bool TryGetRig()
	{
		myRig = GorillaTagger.Instance?.offlineVRRig;
		if ((bool)myRig)
		{
			myRig.OnQuestScoreChanged += OnProgressScoreChanged;
			OnProgressScoreChanged(myRig.GetCurrentQuestScore());
			return true;
		}
		return false;
	}

	private void OnProgressScoreChanged(int score)
	{
		score = Mathf.Clamp(score, 0, 99999);
		displayField.text = score.ToString();
		UpdateBadge(score);
	}

	private void UpdateBadge(int score)
	{
		int num = -1;
		int badgeLevel = -1;
		for (int i = 0; i < badgeLevels.Length; i++)
		{
			if (badgeLevels[i].requiredPoints <= score && badgeLevels[i].requiredPoints > num)
			{
				num = badgeLevels[i].requiredPoints;
				badgeLevel = i;
			}
		}
		SetBadgeLevel(badgeLevel);
	}

	private void SetBadgeLevel(int level)
	{
		level = Mathf.Clamp(level, 0, badgeLevels.Length - 1);
		for (int i = 0; i < badgeLevels.Length; i++)
		{
			badgeLevels[i].badge.SetActive(i == level);
		}
	}
}
