using UnityEngine;

public class MonoBehaviourStatic<T> : MonoBehaviour where T : MonoBehaviour
{
	protected static T gInstance;

	public static T Instance => gInstance;

	protected void Awake()
	{
		if ((bool)gInstance && gInstance != this)
		{
			Object.Destroy(this);
		}
		gInstance = this as T;
		OnAwake();
	}

	protected virtual void OnAwake()
	{
	}
}
