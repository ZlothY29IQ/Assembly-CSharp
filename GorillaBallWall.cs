using UnityEngine;

public class GorillaBallWall : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile GorillaBallWall instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Update()
	{
	}
}
