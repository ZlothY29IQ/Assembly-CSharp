using UnityEngine;

public class GorillaHasUITransformFollow : MonoBehaviour
{
	public GorillaUITransformFollow[] transformFollowers;

	private void Awake()
	{
		GorillaUITransformFollow[] array = transformFollowers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(base.gameObject.activeSelf);
		}
	}

	private void OnDestroy()
	{
		GorillaUITransformFollow[] array = transformFollowers;
		for (int i = 0; i < array.Length; i++)
		{
			Object.Destroy(array[i].gameObject);
		}
	}

	private void OnEnable()
	{
		GorillaUITransformFollow[] array = transformFollowers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		GorillaUITransformFollow[] array = transformFollowers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
	}
}
