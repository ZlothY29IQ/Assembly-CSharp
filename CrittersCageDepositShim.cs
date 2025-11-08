using UnityEngine;

public class CrittersCageDepositShim : MonoBehaviour
{
	public BoxCollider cageBoxCollider;

	public CrittersActor.CrittersActorType type;

	public bool disableGrabOnAttach;

	public bool allowMultiAttach;

	public bool snapOnAttach;

	public Vector3 startLocation;

	public Vector3 endLocation;

	public float submitDuration;

	public float returnDuration;

	public AudioSource depositAudio;

	public AudioClip depositStartSound;

	public AudioClip depositEmptySound;

	public AudioClip depositCritterSound;

	public Transform attachPointTransform;

	public Transform visiblePlatformTransform;

	[ContextMenu("Copy Deposit Data To Shim")]
	private CrittersCageDeposit CopySpawnerDataInPrefab()
	{
		CrittersCageDeposit component = base.gameObject.GetComponent<CrittersCageDeposit>();
		cageBoxCollider = (BoxCollider)component.gameObject.GetComponent<Collider>();
		type = component.actorType;
		disableGrabOnAttach = component.disableGrabOnAttach;
		allowMultiAttach = component.allowMultiAttach;
		snapOnAttach = component.snapOnAttach;
		startLocation = component.depositStartLocation;
		endLocation = component.depositEndLocation;
		submitDuration = component.submitDuration;
		returnDuration = component.returnDuration;
		depositAudio = component.depositAudio;
		depositStartSound = component.depositStartSound;
		depositEmptySound = component.depositEmptySound;
		depositCritterSound = component.depositCritterSound;
		attachPointTransform = component.GetComponentInChildren<CrittersActor>().transform;
		visiblePlatformTransform = attachPointTransform.transform.GetChild(0).transform;
		return component;
	}

	[ContextMenu("Replace Deposit With Shim")]
	private void ReplaceSpawnerWithShim()
	{
		CrittersCageDeposit crittersCageDeposit = CopySpawnerDataInPrefab();
		if (crittersCageDeposit.attachPoint.GetComponent<Rigidbody>() != null)
		{
			Object.DestroyImmediate(crittersCageDeposit.attachPoint.GetComponent<Rigidbody>());
		}
		Object.DestroyImmediate(crittersCageDeposit.attachPoint);
		Object.DestroyImmediate(crittersCageDeposit);
	}
}
