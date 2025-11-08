using UnityEngine;

public class CosmeticRefRegistry : MonoBehaviour
{
	private GameObject[] partsTable = new GameObject[8];

	[SerializeField]
	private CosmeticRefTarget[] builtInRefTargets;

	private void Awake()
	{
		CosmeticRefTarget[] array = builtInRefTargets;
		foreach (CosmeticRefTarget cosmeticRefTarget in array)
		{
			Register(cosmeticRefTarget.id, cosmeticRefTarget.gameObject);
		}
	}

	public void Register(CosmeticRefID partID, GameObject part)
	{
		partsTable[(int)partID] = part;
	}

	public GameObject Get(CosmeticRefID partID)
	{
		return partsTable[(int)partID];
	}
}
