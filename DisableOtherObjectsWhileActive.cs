using UnityEngine;

public class DisableOtherObjectsWhileActive : MonoBehaviour
{
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
		GameObject[] array = otherObjects;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(active);
			}
		}
		XSceneRef[] array2 = otherXSceneObjects;
		foreach (XSceneRef xSceneRef in array2)
		{
			if (xSceneRef.TryResolve(out GameObject result))
			{
				result.SetActive(active);
			}
		}
	}
}
