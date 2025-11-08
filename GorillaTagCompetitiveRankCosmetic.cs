using System.Collections;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class GorillaTagCompetitiveRankCosmetic : MonoBehaviour, ISpawnable
{
	[Tooltip("If enabled, display PC rank. Otherwise, display Quest rank")]
	[SerializeField]
	private bool usePCELO;

	[SerializeField]
	private bool forWardrobe;

	[SerializeField]
	private VRRig myRig;

	[SerializeField]
	private GameObject[] rankCosmetics;

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
		myRig.OnRankedSubtierChanged += OnRankedScoreChanged;
		OnRankedScoreChanged(myRig.GetCurrentRankedSubTier(getPC: false), myRig.GetCurrentRankedSubTier(getPC: true));
	}

	public void OnDespawn()
	{
	}

	private void OnEnable()
	{
		if (forWardrobe)
		{
			UpdateDisplayedCosmetic(-1, -1);
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
			myRig.OnRankedSubtierChanged -= OnRankedScoreChanged;
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
			myRig.OnRankedSubtierChanged += OnRankedScoreChanged;
			OnRankedScoreChanged(myRig.GetCurrentRankedSubTier(getPC: false), myRig.GetCurrentRankedSubTier(getPC: true));
			return true;
		}
		return false;
	}

	private void OnRankedScoreChanged(int questRank, int pcRank)
	{
		UpdateDisplayedCosmetic(questRank, pcRank);
	}

	private void UpdateDisplayedCosmetic(int questRank, int pcRank)
	{
		if (rankCosmetics != null)
		{
			int num = (usePCELO ? pcRank : questRank);
			if (num <= 0)
			{
				num = 0;
			}
			for (int i = 0; i < rankCosmetics.Length; i++)
			{
				rankCosmetics[i].SetActive(i == num);
			}
		}
	}
}
