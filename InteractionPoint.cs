using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class InteractionPoint : MonoBehaviour, ISpawnable, IBuildValidation
{
	[SerializeField]
	[FormerlySerializedAs("parentTransferrableObject")]
	public GameObject parentHoldableObject;

	private IHoldableObject parentHoldable;

	[SerializeField]
	private bool isNonSpawnedObject;

	[SerializeField]
	private float interactionRadius;

	public Collider myCollider;

	public EquipmentInteractor interactor;

	public bool wasInLeft;

	public bool wasInRight;

	public bool forLocalPlayer;

	[field: SerializeField]
	public bool ignoreLeftHand { get; private set; }

	[field: SerializeField]
	public bool ignoreRightHand { get; private set; }

	public IHoldableObject Holdable => parentHoldable;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	public void OnSpawn(VRRig rig)
	{
		if (!IsSpawned)
		{
			IsSpawned = true;
		}
		interactor = EquipmentInteractor.instance;
		myCollider = GetComponent<Collider>();
		if (parentHoldableObject != null)
		{
			parentHoldable = parentHoldableObject.GetComponent<IHoldableObject>();
		}
		else
		{
			parentHoldable = GetComponentInParent<IHoldableObject>(includeInactive: true);
			parentHoldableObject = parentHoldable.gameObject;
		}
		if (parentHoldable == null)
		{
			if (parentHoldableObject == null)
			{
				Debug.LogError("InteractionPoint: Disabling because expected field `parentHoldableObject` is null. Path=" + base.transform.GetPathQ());
				base.enabled = false;
				return;
			}
			Debug.LogError("InteractionPoint: Disabling because `parentHoldableObject` does not have a IHoldableObject component. Path=" + base.transform.GetPathQ());
		}
		TransferrableObject transferrableObject = parentHoldable as TransferrableObject;
		forLocalPlayer = transferrableObject == null || transferrableObject.IsLocalObject() || transferrableObject.isSceneObject || transferrableObject.canDrop;
	}

	void ISpawnable.OnDespawn()
	{
	}

	private void Awake()
	{
		if (isNonSpawnedObject)
		{
			OnSpawn(null);
		}
	}

	private void OnEnable()
	{
		wasInLeft = false;
		wasInRight = false;
	}

	public void OnDisable()
	{
		if (forLocalPlayer && !(interactor == null))
		{
			interactor.InteractionPointDisabled(this);
		}
	}

	protected void LateUpdate()
	{
		if (!IsSpawned)
		{
			return;
		}
		if (!forLocalPlayer)
		{
			base.enabled = false;
			if (myCollider.IsNotNull())
			{
				myCollider.enabled = false;
			}
		}
		else if (interactor == null)
		{
			interactor = EquipmentInteractor.instance;
		}
		else
		{
			if (!(interactionRadius > 0f) && !(myCollider != null))
			{
				return;
			}
			if (!ignoreLeftHand && OverlapCheck(interactor.leftHand.transform.position) != wasInLeft)
			{
				if (!wasInLeft && !interactor.overlapInteractionPointsLeft.Contains(this))
				{
					interactor.overlapInteractionPointsLeft.Add(this);
					wasInLeft = true;
				}
				else if (wasInLeft && interactor.overlapInteractionPointsLeft.Contains(this))
				{
					interactor.overlapInteractionPointsLeft.Remove(this);
					wasInLeft = false;
				}
			}
			if (!ignoreRightHand && OverlapCheck(interactor.rightHand.transform.position) != wasInRight)
			{
				if (!wasInRight && !interactor.overlapInteractionPointsRight.Contains(this))
				{
					interactor.overlapInteractionPointsRight.Add(this);
					wasInRight = true;
				}
				else if (wasInRight && interactor.overlapInteractionPointsRight.Contains(this))
				{
					interactor.overlapInteractionPointsRight.Remove(this);
					wasInRight = false;
				}
			}
		}
	}

	public bool OverlapCheck(Vector3 point)
	{
		if (interactionRadius > 0f)
		{
			return (base.transform.position - point).IsShorterThan(interactionRadius * base.transform.lossyScale);
		}
		if (myCollider != null)
		{
			return myCollider.bounds.Contains(point);
		}
		return false;
	}

	public bool BuildValidationCheck()
	{
		return true;
	}
}
