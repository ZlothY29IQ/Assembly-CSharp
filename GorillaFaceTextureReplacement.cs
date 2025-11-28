using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class GorillaFaceTextureReplacement : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private Material newFaceMaterial;

	private VRRig myRig;

	[SerializeField]
	private MeshRenderer[] alsoApplyFaceTo;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnDespawn()
	{
	}

	public void OnSpawn(VRRig rig)
	{
		myRig = rig;
	}

	private void OnEnable()
	{
		Material sharedMaterial = myRig.GetComponent<GorillaMouthFlap>().SetFaceMaterialReplacement(newFaceMaterial);
		MeshRenderer[] array = alsoApplyFaceTo;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sharedMaterial = sharedMaterial;
		}
	}

	private void OnDisable()
	{
		myRig.GetComponent<GorillaMouthFlap>().ClearFaceMaterialReplacement();
	}
}
