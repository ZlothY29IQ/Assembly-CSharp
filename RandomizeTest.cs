using System.Collections.Generic;
using UnityEngine;

public class RandomizeTest : MonoBehaviour
{
	public List<int> testList = new List<int>();

	public int[] testListArray = new int[10];

	public int randomIterator;

	public int tempRandIndex;

	public int tempRandValue;

	private void Start()
	{
		for (int i = 0; i < 10; i++)
		{
			testList.Add(i);
		}
		for (int j = 0; j < 10; j++)
		{
			testListArray[j] = 0;
		}
		for (int k = 0; k < testList.Count; k++)
		{
			testListArray[k] = testList[k];
		}
		RandomizeList(ref testList);
		for (int l = 0; l < 10; l++)
		{
			testListArray[l] = 0;
		}
		for (int m = 0; m < testList.Count; m++)
		{
			testListArray[m] = testList[m];
		}
	}

	public void RandomizeList(ref List<int> listToRandomize)
	{
		for (randomIterator = 0; randomIterator < listToRandomize.Count; randomIterator++)
		{
			tempRandIndex = Random.Range(randomIterator, listToRandomize.Count);
			tempRandValue = listToRandomize[randomIterator];
			listToRandomize[randomIterator] = listToRandomize[tempRandIndex];
			listToRandomize[tempRandIndex] = tempRandValue;
		}
	}
}
