using UnityEngine;

[DefaultExecutionOrder(-9999)]
public class ScenePreparer : MonoBehaviour
{
	public OVRManager ovrManager;

	public GameObject[] betaDisableObjects;

	public GameObject[] betaEnableObjects;

	protected void Awake()
	{
		bool flag = false;
		GameObject[] array = betaEnableObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(flag);
		}
		array = betaDisableObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(!flag);
		}
	}
}
