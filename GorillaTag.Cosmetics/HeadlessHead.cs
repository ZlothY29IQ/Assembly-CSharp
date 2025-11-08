using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class HeadlessHead : HoldableObject
{
	[Tooltip("The slot this cosmetic resides.")]
	public VRRig.WearablePackedStateSlots wearablePackedStateSlot = VRRig.WearablePackedStateSlots.Face;

	[SerializeField]
	private Vector3 offsetFromLeftHand = new Vector3(0f, 0.0208f, 0.171f);

	[SerializeField]
	private Vector3 offsetFromRightHand = new Vector3(0f, 0.0208f, 0.171f);

	[SerializeField]
	private Quaternion rotationFromLeftHand = Quaternion.Euler(14.063973f, 52.56744f, 10.067408f);

	[SerializeField]
	private Quaternion rotationFromRightHand = Quaternion.Euler(14.063973f, 52.56744f, 10.067408f);

	private Vector3 baseLocalPosition;

	private VRRig ownerRig;

	private bool isLocal;

	private bool isHeld;

	private bool isHeldLeftHand;

	private GTBitOps.BitWriteInfo stateBitsWriteInfo;

	[SerializeField]
	private MeshRenderer firstPersonRenderer;

	[SerializeField]
	private float firstPersonHiddenRadius;

	[SerializeField]
	private Transform firstPersonHideCenter;

	[SerializeField]
	private Transform holdAnchorPoint;

	private bool hasFirstPersonRenderer;

	private Vector3 blendingFromPosition;

	private Quaternion blendingFromRotation;

	private float blendFraction;

	private bool wasHeld;

	private bool wasHeldLeftHand;

	[SerializeField]
	private float blendDuration = 0.3f;

	protected void Awake()
	{
		ownerRig = GetComponentInParent<VRRig>();
		if (ownerRig == null)
		{
			ownerRig = GorillaTagger.Instance.offlineVRRig;
		}
		isLocal = ownerRig.isOfflineVRRig;
		stateBitsWriteInfo = VRRig.WearablePackedStatesBitWriteInfos[(int)wearablePackedStateSlot];
		baseLocalPosition = base.transform.localPosition;
		hasFirstPersonRenderer = firstPersonRenderer != null;
	}

	protected void OnEnable()
	{
		if (ownerRig == null)
		{
			Debug.LogError("HeadlessHead \"" + base.transform.GetPath() + "\": Deactivating because ownerRig is null.", this);
			base.gameObject.SetActive(value: false);
		}
		else
		{
			ownerRig.bodyRenderer.SetCosmeticBodyType(GorillaBodyType.NoHead);
		}
	}

	private void OnDisable()
	{
		ownerRig.bodyRenderer.SetCosmeticBodyType(GorillaBodyType.Default);
	}

	protected virtual void LateUpdate()
	{
		if (isLocal)
		{
			LateUpdateLocal();
		}
		else
		{
			LateUpdateReplicated();
		}
		LateUpdateShared();
	}

	protected virtual void LateUpdateLocal()
	{
		ownerRig.WearablePackedStates = GTBitOps.WriteBits(ownerRig.WearablePackedStates, stateBitsWriteInfo, (isHeld ? 1 : 0) + (isHeldLeftHand ? 2 : 0));
	}

	protected virtual void LateUpdateReplicated()
	{
		int num = GTBitOps.ReadBits(ownerRig.WearablePackedStates, stateBitsWriteInfo.index, stateBitsWriteInfo.valueMask);
		isHeld = num != 0;
		isHeldLeftHand = (num & 2) != 0;
	}

	protected virtual void LateUpdateShared()
	{
		if (isHeld != wasHeld || isHeldLeftHand != wasHeldLeftHand)
		{
			blendingFromPosition = base.transform.position;
			blendingFromRotation = base.transform.rotation;
			blendFraction = 0f;
		}
		Quaternion quaternion;
		Vector3 vector;
		if (isHeldLeftHand)
		{
			quaternion = ownerRig.leftHandTransform.rotation * rotationFromLeftHand;
			vector = ownerRig.leftHandTransform.TransformPoint(offsetFromLeftHand) - quaternion * holdAnchorPoint.transform.localPosition;
		}
		else if (isHeld)
		{
			quaternion = ownerRig.rightHandTransform.rotation * rotationFromRightHand;
			vector = ownerRig.rightHandTransform.TransformPoint(offsetFromRightHand) - quaternion * holdAnchorPoint.transform.localPosition;
		}
		else
		{
			quaternion = base.transform.parent.rotation;
			vector = base.transform.parent.TransformPoint(baseLocalPosition);
		}
		if (blendFraction < 1f)
		{
			blendFraction += Time.deltaTime / blendDuration;
			quaternion = Quaternion.Lerp(blendingFromRotation, quaternion, blendFraction);
			vector = Vector3.Lerp(blendingFromPosition, vector, blendFraction);
		}
		base.transform.rotation = quaternion;
		base.transform.position = vector;
		if (hasFirstPersonRenderer)
		{
			float x = base.transform.lossyScale.x;
			firstPersonRenderer.enabled = (firstPersonHideCenter.transform.position - GTPlayer.Instance.headCollider.transform.position).IsLongerThan(firstPersonHiddenRadius * x);
		}
		wasHeld = isHeld;
		wasHeldLeftHand = isHeldLeftHand;
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		isHeld = true;
		isHeldLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
		EquipmentInteractor.instance.UpdateHandEquipment(this, isHeldLeftHand);
	}

	public override void DropItemCleanup()
	{
		isHeld = false;
		isHeldLeftHand = false;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (EquipmentInteractor.instance.rightHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.rightHand)
		{
			return false;
		}
		if (EquipmentInteractor.instance.leftHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.leftHand)
		{
			return false;
		}
		EquipmentInteractor.instance.UpdateHandEquipment(null, isHeldLeftHand);
		isHeld = false;
		isHeldLeftHand = false;
		return true;
	}
}
