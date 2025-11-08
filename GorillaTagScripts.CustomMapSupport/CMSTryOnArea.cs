using GorillaExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaTagScripts.CustomMapSupport;

public class CMSTryOnArea : MonoBehaviour
{
	private Scene originalScene;

	public BoxCollider tryOnAreaCollider;

	public void InitializeForCustomMap(CompositeTriggerEvents customMapTryOnArea, Scene customMapScene)
	{
		originalScene = customMapScene;
		if (!tryOnAreaCollider.IsNull())
		{
			customMapTryOnArea.AddCollider(tryOnAreaCollider);
		}
	}

	public void RemoveFromCustomMap(CompositeTriggerEvents customMapTryOnArea)
	{
		if (!tryOnAreaCollider.IsNull())
		{
			customMapTryOnArea.RemoveCollider(tryOnAreaCollider);
		}
	}

	public bool IsFromScene(Scene unloadingScene)
	{
		return unloadingScene == originalScene;
	}
}
