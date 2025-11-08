using UnityEngine;

namespace MTAssets.EasyMeshCombiner;

public class CombineInRuntimeDemo : MonoBehaviour
{
	public GameObject combineButton;

	public GameObject undoButton;

	public RuntimeMeshCombiner runtimeCombiner;

	private void Update()
	{
		if (!runtimeCombiner.isTargetMeshesMerged())
		{
			combineButton.SetActive(value: true);
			undoButton.SetActive(value: false);
		}
		if (runtimeCombiner.isTargetMeshesMerged())
		{
			combineButton.SetActive(value: false);
			undoButton.SetActive(value: true);
		}
	}

	public void CombineMeshes()
	{
		runtimeCombiner.CombineMeshes();
	}

	public void UndoMerge()
	{
		runtimeCombiner.UndoMerge();
	}
}
