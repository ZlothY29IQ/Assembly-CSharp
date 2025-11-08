using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class HierarchyFlattenerRemoveXform : MonoBehaviour
{
	private bool _didIt;

	protected void Awake()
	{
		_DoIt();
	}

	private void _DoIt()
	{
		if (!_didIt && !(GetComponentInChildren<HierarchyFlattenerRemoveXform>(includeInactive: true) != null))
		{
			HierarchyFlattenerRemoveXform componentInParent = GetComponentInParent<HierarchyFlattenerRemoveXform>(includeInactive: true);
			_didIt = true;
			Transform transform = base.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				transform.GetChild(i).SetParent(transform.parent, worldPositionStays: true);
			}
			Object.Destroy(base.gameObject);
			if (componentInParent != null)
			{
				componentInParent._DoIt();
			}
		}
	}
}
