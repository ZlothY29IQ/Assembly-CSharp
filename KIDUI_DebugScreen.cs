using UnityEngine;

public class KIDUI_DebugScreen : MonoBehaviour
{
	public const string KID_ENABLED_KEY = "dbg-kid-enabled";

	private void Awake()
	{
		Object.DestroyImmediate(base.gameObject);
	}

	public void OnResetUserAndQuit()
	{
	}

	public void OnClose()
	{
	}

	public static string GetOrCreateUsername()
	{
		return null;
	}

	public void ResetAll()
	{
	}
}
