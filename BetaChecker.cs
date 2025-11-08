using GorillaNetworking;
using UnityEngine;

public class BetaChecker : MonoBehaviour
{
	public GameObject[] objectsToEnable;

	public bool doNotEnable;

	private void Start()
	{
		if (PlayerPrefs.GetString("CheckedBox2") == "true")
		{
			doNotEnable = true;
			base.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (doNotEnable)
		{
			return;
		}
		if (CosmeticsController.instance.confirmedDidntPlayInBeta)
		{
			PlayerPrefs.SetString("CheckedBox2", "true");
			PlayerPrefs.Save();
			base.gameObject.SetActive(value: false);
		}
		else if (CosmeticsController.instance.playedInBeta)
		{
			GameObject[] array = objectsToEnable;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: true);
			}
			doNotEnable = true;
		}
	}
}
