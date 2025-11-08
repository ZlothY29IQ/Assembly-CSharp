using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class PropHuntPropZone : MonoBehaviour, IDelayedExecListener
{
	private const string preLog = "PropHuntPropZone: ";

	private const string preLogEd = "(editor only log) PropHuntPropZone: ";

	private const string preLogBeta = "(beta only log) PropHuntPropZone: ";

	private const string preErr = "ERROR!!!  PropHuntPropZone: ";

	private const string preErrEd = "ERROR!!!  (editor only log) PropHuntPropZone: ";

	private const string preErrBeta = "ERROR!!!  (beta only log) PropHuntPropZone: ";

	private const bool _k__GT_PROP_HUNT__USE_POOLING__ = true;

	[SerializeField]
	private PropPlacementRB propPlacementPrefab;

	[SerializeField]
	private int seedOffset;

	[SerializeField]
	private float radius = 1f;

	[SerializeField]
	private int numProps = 10;

	[SerializeField]
	private float m_simDurationBeforeFreeze = 2f;

	private BoxCollider boxCollider;

	private bool hasBoxCollider;

	private int nextUnusedPropPlacement;

	private readonly List<PropPlacementRB> propPlacementRBs = new List<PropPlacementRB>(64);

	private void Awake()
	{
		hasBoxCollider = TryGetComponent<BoxCollider>(out boxCollider);
	}

	private void OnEnable()
	{
		GorillaPropHuntGameManager.RegisterPropZone(this);
	}

	private void OnDisable()
	{
		DestroyDecoys();
		GorillaPropHuntGameManager.UnregisterPropZone(this);
	}

	public void DestroyDecoys()
	{
		foreach (PropPlacementRB propPlacementRB in propPlacementRBs)
		{
			if (propPlacementRB != null)
			{
				PropHuntPools.ReturnDecoyProp(propPlacementRB);
			}
		}
		propPlacementRBs.Clear();
	}

	public void OnRoundStart()
	{
		if (!PropHuntPools.IsReady)
		{
			Debug.LogError("ERROR!!!  PropHuntPropZone: (this should never happen) props not ready to be spawned so aborting. you should only be calling this if `PropHuntPools.IsReady` is true or from the callback `PropHuntPools.OnReady`.");
		}
		CreateDecoys(GorillaPropHuntGameManager.instance.GetSeed());
	}

	public void CreateDecoys(int seed)
	{
		DestroyDecoys();
		SRand sRand = new SRand(seed + seedOffset);
		for (int i = 0; i < numProps; i++)
		{
			if (!PropHuntPools.TryGetDecoyProp(GorillaPropHuntGameManager.instance.GetCosmeticId(sRand.NextUInt()), out var out_prop))
			{
				return;
			}
			Vector3 position2;
			if (hasBoxCollider)
			{
				Vector3 position = new Vector3(sRand.NextFloat(0f - boxCollider.size.x, boxCollider.size.x) / 2f, sRand.NextFloat(0f - boxCollider.size.y, boxCollider.size.y) / 2f, sRand.NextFloat(0f - boxCollider.size.z, boxCollider.size.z) / 2f);
				position2 = base.transform.TransformPoint(position);
			}
			else
			{
				position2 = base.transform.position + sRand.NextPointInsideSphere(radius);
			}
			out_prop.gameObject.SetActive(value: false);
			out_prop.transform.SetParent(null, worldPositionStays: false);
			out_prop.transform.position = position2;
			out_prop.transform.rotation = Quaternion.Euler(sRand.NextFloat(360f), sRand.NextFloat(360f), sRand.NextFloat(360f));
			out_prop._placingProp.SetActive(value: false);
			out_prop._placingProp.transform.SetParent(null, worldPositionStays: false);
			propPlacementRBs.Add(out_prop);
		}
		for (int j = 0; j < propPlacementRBs.Count; j++)
		{
			propPlacementRBs[j].gameObject.SetActive(value: true);
		}
		GTDelayedExec.Add(this, m_simDurationBeforeFreeze, 0);
	}

	public void OnDelayedAction(int contextId)
	{
		for (int i = 0; i < propPlacementRBs.Count; i++)
		{
			PropPlacementRB propPlacementRB = propPlacementRBs[i];
			propPlacementRB.gameObject.SetActive(value: false);
			Transform transform = propPlacementRB.transform;
			GameObject placingProp = propPlacementRB._placingProp;
			placingProp.transform.SetPositionAndRotation(transform.position, transform.rotation);
			placingProp.SetActive(value: true);
		}
	}

	private PropPlacementRB _GetOrCreatePropPlacementObj_NoPool()
	{
		PropPlacementRB propPlacementRB;
		if (nextUnusedPropPlacement < propPlacementRBs.Count)
		{
			propPlacementRB = propPlacementRBs[nextUnusedPropPlacement];
		}
		else
		{
			propPlacementRB = Object.Instantiate(propPlacementPrefab, base.transform);
			propPlacementRBs.Add(propPlacementRB);
		}
		nextUnusedPropPlacement++;
		return propPlacementRB;
	}

	private void SpawnProp_NoPool(GTAssetRef<GameObject> item, Vector3 pos, Quaternion rot, CosmeticSO debugCosmeticSO)
	{
		_GetOrCreatePropPlacementObj_NoPool().PlaceProp_NoPool(this, item, pos, rot, debugCosmeticSO);
	}
}
