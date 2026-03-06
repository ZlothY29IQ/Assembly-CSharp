using UnityEngine;

public class DisableOtherObjectsWhileActive : MonoBehaviour
{
	public const string preErr = "[GT/DisableOtherObjectsWhileActive]  ERROR!!!  ";

	public GameObject[] otherObjects;

	public XSceneRef[] otherXSceneObjects;

	private void OnEnable()
	{
		SetAllActive(active: false);
	}

	private void OnDisable()
	{
		SetAllActive(active: true);
	}

	private void SetAllActive(bool active)
	{
		for (int i = 0; i < otherObjects.Length; i++)
		{
			GameObject gameObject = otherObjects[i];
			if (gameObject != null)
			{
				gameObject.SetActive(active);
			}
		}
		for (int j = 0; j < otherXSceneObjects.Length; j++)
		{
			XSceneRef xSceneRef = otherXSceneObjects[j];
			if (xSceneRef.TryResolve(out GameObject result) && result != null)
			{
				result.SetActive(active);
			}
		}
	}
}
