using UnityEngine;

public class WhoDisabledMe : MonoBehaviour
{
	private bool destroying;

	private void OnDisable()
	{
		if (!base.gameObject.activeSelf)
		{
			Debug.Log("[WhoDisabledMe] " + base.name + " disabled directly", this);
			return;
		}
		Transform parent = base.transform.parent;
		while (parent != null && parent.gameObject.activeSelf)
		{
			parent = parent.parent;
		}
		Debug.Log("[WhoDisabledMe] " + base.name + " disabled via ancestor '" + (parent ? parent.name : "?") + "'", this);
	}
}
