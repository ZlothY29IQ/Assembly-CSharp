using GorillaNetworking;
using UnityEngine;

public class CosmeticsControllerUpdateStand : MonoBehaviour
{
	public CosmeticsController cosmeticsController;

	public bool FailEntitlement;

	public bool PlayerUnlocked;

	public bool ItemNotGrantedYet;

	public bool ItemSuccessfullyGranted;

	public bool AttemptToConsumeEntitlement;

	public bool EntitlementSuccessfullyConsumed;

	public bool LockSuccessfullyCleared;

	public bool RunDebug;

	public Transform textParent;

	private CosmeticsController.CosmeticItem outItem;

	public HeadModel[] inventoryHeadModels;

	public string headModelsPrefabPath;

	public GameObject ReturnChildWithCosmeticNameMatch(Transform parentTransform)
	{
		GameObject gameObject = null;
		foreach (Transform child in parentTransform)
		{
			if (child.gameObject.activeInHierarchy && cosmeticsController.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => child.name == x.itemName) > -1)
			{
				return child.gameObject;
			}
			gameObject = ReturnChildWithCosmeticNameMatch(child);
			if (gameObject != null)
			{
				return gameObject;
			}
		}
		return gameObject;
	}
}
