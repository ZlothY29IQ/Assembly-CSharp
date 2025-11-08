using System;
using UnityEngine;

public class RockPiles : MonoBehaviour
{
	[Serializable]
	public struct RockPile
	{
		public GameObject visual;

		public int threshold;
	}

	[SerializeField]
	private RockPile[] _rocks;

	public void Show(int visiblePercentage)
	{
		if (visiblePercentage <= 0)
		{
			ShowRock(-1);
			return;
		}
		int rockToShow = -1;
		int num = -1;
		for (int i = 0; i < _rocks.Length; i++)
		{
			RockPile rockPile = _rocks[i];
			if (visiblePercentage >= rockPile.threshold && num < rockPile.threshold)
			{
				rockToShow = i;
				num = rockPile.threshold;
			}
		}
		ShowRock(rockToShow);
	}

	private void ShowRock(int rockToShow)
	{
		for (int i = 0; i < _rocks.Length; i++)
		{
			_rocks[i].visual.SetActive(i == rockToShow);
		}
	}
}
