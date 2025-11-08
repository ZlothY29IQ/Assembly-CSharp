using UnityEngine;

public class TransferrableObjectGripPosition : MonoBehaviour
{
	[SerializeField]
	private TransferrableItemSlotTransformOverride parentObject;

	[SerializeField]
	private TransferrableObject.PositionState attachmentType;

	private void Awake()
	{
		if (parentObject == null)
		{
			parentObject = base.transform.parent.GetComponent<TransferrableItemSlotTransformOverride>();
		}
		parentObject.AddGripPosition(attachmentType, this);
	}

	public SubGrabPoint CreateSubGrabPoint(SlotTransformOverride overrideContainer)
	{
		return new SubGrabPoint();
	}
}
