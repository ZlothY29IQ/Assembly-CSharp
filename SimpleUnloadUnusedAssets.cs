using System.Collections;
using UnityEngine;

public class SimpleUnloadUnusedAssets : MonoBehaviour
{
	public float WaitForUnload = 5f;

	private void OnEnable()
	{
		StartCoroutine(UnloadUnusedAssets());
	}

	private IEnumerator UnloadUnusedAssets()
	{
		yield return new WaitForSeconds(WaitForUnload);
		Debug.Log($"SimpleUnloadUnusedAssets: Forcing unload unused assets after waiting {WaitForUnload} seconds!");
		Resources.UnloadUnusedAssets();
	}
}
