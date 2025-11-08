using UnityEngine;

public class ParentedObjectStressTestMain : MonoBehaviour
{
	public GameObject Object;

	public Vector3 NumObjects;

	public Vector3 Spacing;

	public void Start()
	{
		for (int i = 0; i < (int)NumObjects.x; i++)
		{
			for (int j = 0; j < (int)NumObjects.y; j++)
			{
				for (int k = 0; k < (int)NumObjects.z; k++)
				{
					UnityEngine.Object.Instantiate(Object).transform.position = new Vector3(2f * ((float)i / (NumObjects.x - 1f) - 0.5f) * NumObjects.x * Spacing.x, 2f * ((float)j / (NumObjects.y - 1f) - 0.5f) * NumObjects.y * Spacing.y, 2f * ((float)k / (NumObjects.z - 1f) - 0.5f) * NumObjects.z * Spacing.z);
				}
			}
		}
	}
}
