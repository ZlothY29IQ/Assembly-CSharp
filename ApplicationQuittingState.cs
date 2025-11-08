using UnityEngine;

public static class ApplicationQuittingState
{
	[field: OnExitPlay_Set(false)]
	public static bool IsQuitting { get; private set; }

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Application.quitting += HandleApplicationQuitting;
	}

	private static void HandleApplicationQuitting()
	{
		IsQuitting = true;
	}
}
