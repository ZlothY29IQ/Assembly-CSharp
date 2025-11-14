using UnityEngine;

public class ZoneGraphBSP : MonoBehaviour
{
	[SerializeField]
	private SerializableBSPTree bspTree;

	public static ZoneGraphBSP Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Object.Destroy(this);
		}
	}

	public void Preprocess()
	{
		BoxCollider[] componentsInChildren = GetComponentsInChildren<BoxCollider>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return;
		}
		BoxCollider[] array = componentsInChildren;
		foreach (BoxCollider boxCollider in array)
		{
			if (boxCollider.transform.GetComponent<ZoneDef>() != null)
			{
				Object.Destroy(boxCollider);
			}
			else
			{
				Object.Destroy(boxCollider.gameObject);
			}
		}
	}

	public void CompileBSP()
	{
		ZoneDef[] componentsInChildren = base.gameObject.GetComponentsInChildren<ZoneDef>();
		bspTree = BSPTreeBuilder.BuildTree(componentsInChildren);
		if (bspTree != null && bspTree.nodes != null)
		{
			Debug.Log($"BSP Tree compiled with {componentsInChildren.Length} zones, {bspTree.nodes.Length} nodes");
		}
		else
		{
			Debug.Log("BSP Tree compilation failed - no zones found");
		}
	}

	public ZoneDef FindZoneAtPoint(Vector3 worldPoint)
	{
		return bspTree?.FindZone(worldPoint);
	}

	public bool IsPointInAnyZone(Vector3 worldPoint)
	{
		return FindZoneAtPoint(worldPoint) != null;
	}

	public bool HasCompiledTree()
	{
		if (bspTree != null && bspTree.nodes != null)
		{
			return bspTree.nodes.Length != 0;
		}
		return false;
	}

	public SerializableBSPTree GetBSPTree()
	{
		return bspTree;
	}
}
