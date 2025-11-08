using UnityEngine;

public class DevInspectorManager : MonoBehaviour
{
	private static DevInspectorManager _instance;

	public static DevInspectorManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<DevInspectorManager>();
			}
			return _instance;
		}
	}
}
