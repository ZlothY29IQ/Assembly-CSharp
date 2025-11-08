using UnityEngine;

public static class GlobalDeactivatedSpawnRoot
{
	private static Transform _xform;

	public static Transform GetOrCreate()
	{
		if (!_xform)
		{
			_xform = new GameObject("GlobalDeactivatedSpawnRoot").transform;
			_xform.gameObject.SetActive(value: false);
			Object.DontDestroyOnLoad(_xform.gameObject);
		}
		_xform.gameObject.SetActive(value: false);
		return _xform;
	}
}
