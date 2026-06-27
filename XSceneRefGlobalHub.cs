using System.Collections.Generic;
using UnityEngine;

public static class XSceneRefGlobalHub
{
	private static List<Dictionary<int, XSceneRefTarget>> registry = new List<Dictionary<int, XSceneRefTarget>>
	{
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } },
		new Dictionary<int, XSceneRefTarget> { { 0, null } }
	};

	public static void Register(int ID, XSceneRefTarget obj)
	{
		if (ID > 0)
		{
			int sceneIndex = (int)obj.GetSceneIndex();
			if (sceneIndex >= 0)
			{
				registry[sceneIndex][ID] = obj;
			}
		}
	}

	public static void Unregister(int ID, XSceneRefTarget obj)
	{
		int sceneIndex = (int)obj.GetSceneIndex();
		if (ID > 0 && sceneIndex >= 0)
		{
			if (sceneIndex < 0 || sceneIndex >= registry.Count)
			{
				Debug.LogErrorFormat(obj, "Invalid scene index {0} cannot remove ID {1}", sceneIndex, ID);
			}
			registry[sceneIndex].Remove(ID);
		}
	}

	public static bool TryResolve(SceneIndex sceneIndex, int ID, out XSceneRefTarget result)
	{
		return registry[(int)sceneIndex].TryGetValue(ID, out result);
	}
}
