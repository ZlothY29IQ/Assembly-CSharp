using UnityEngine;

public class SuperInfectionHandDisplay : MonoBehaviour
{
	[SerializeField]
	private GameObject[] gameObjects;

	public void EnableHands(bool on)
	{
		for (int i = 0; i < gameObjects.Length; i++)
		{
			gameObjects[i].SetActive(on);
		}
	}
}
