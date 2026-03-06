using System;
using UnityEngine;

public class FortuneResults : ScriptableObject
{
	public enum FortuneCategoryType
	{
		Invalid,
		Positive,
		Neutral,
		Negative,
		Seasonal
	}

	[Serializable]
	public struct FortuneCategory
	{
		public FortuneCategoryType fortuneType;

		public float weightedChance;

		public string[] textResults;
	}

	public struct FortuneResult(FortuneCategoryType fortuneType, int resultIndex)
	{
		public FortuneCategoryType fortuneType = fortuneType;

		public int resultIndex = resultIndex;
	}

	[SerializeField]
	private FortuneCategory[] fortuneResults;

	[SerializeField]
	private float totalChance;

	private void OnValidate()
	{
		totalChance = 0f;
		for (int i = 0; i < fortuneResults.Length; i++)
		{
			totalChance += fortuneResults[i].weightedChance;
		}
	}

	public FortuneResult GetResult()
	{
		float num = UnityEngine.Random.Range(0f, totalChance);
		for (int i = 0; i < fortuneResults.Length; i++)
		{
			FortuneCategory fortuneCategory = fortuneResults[i];
			if (num <= fortuneCategory.weightedChance)
			{
				if (fortuneCategory.textResults.Length == 0)
				{
					return new FortuneResult(FortuneCategoryType.Invalid, -1);
				}
				int resultIndex = UnityEngine.Random.Range(0, fortuneCategory.textResults.Length);
				return new FortuneResult(fortuneCategory.fortuneType, resultIndex);
			}
			num -= fortuneCategory.weightedChance;
		}
		return new FortuneResult(FortuneCategoryType.Invalid, -1);
	}

	public string GetResultText(FortuneResult result)
	{
		for (int i = 0; i < fortuneResults.Length; i++)
		{
			if (fortuneResults[i].fortuneType == result.fortuneType && result.resultIndex >= 0 && result.resultIndex < fortuneResults[i].textResults.Length)
			{
				return fortuneResults[i].textResults[result.resultIndex];
			}
		}
		return "!! Invalid Fortune !!";
	}
}
