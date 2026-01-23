using UnityEngine;

namespace GorillaExtensions;

public static class GameObjectExtensions
{
	public static bool TryGetComponentInParent<T>(this GameObject obj, out T component) where T : MonoBehaviour
	{
		do
		{
			if (obj.TryGetComponent<T>(out component))
			{
				return true;
			}
			obj = ((obj.transform.parent != null) ? obj.transform.parent.gameObject : null);
		}
		while (obj != null);
		return false;
	}
}
