using UnityEngine;

namespace GorillaTag;

public static class GTAppState
{
	[field: OnEnterPlay_Set(false)]
	public static bool isQuitting { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void HandleOnSubsystemRegistration()
	{
		isQuitting = false;
		Application.quitting += delegate
		{
			isQuitting = true;
		};
		Debug.Log("GTAppState:\n- SystemInfo.operatingSystem=" + SystemInfo.operatingSystem + "\n- SystemInfo.maxTextureArraySlices=" + SystemInfo.maxTextureArraySlices + "\n");
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void HandleOnAfterSceneLoad()
	{
	}
}
