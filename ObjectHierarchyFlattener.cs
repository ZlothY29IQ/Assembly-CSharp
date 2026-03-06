using GorillaTag;
using UnityEngine;

[DefaultExecutionOrder(2001)]
public class ObjectHierarchyFlattener : MonoBehaviour, IGorillaSimpleBackgroundWorker
{
	public const int k_monoDefaultExecutionOrder = 2001;

	[DebugReadout]
	private GameObject originalParentGO;

	private Transform originalParentTransform;

	private Vector3 originalLocalPosition;

	private Vector3 calcOffset;

	private Quaternion originalLocalRotation;

	private Vector3 originalScale;

	private float originalParentScale;

	public bool trackTransformOfParent;

	public bool maintainRelativeScale;

	private FlattenerCrumb crumb;

	public Transform overrideParentTransform;

	private bool isAttachedToOverride;

	private bool initialized;

	private bool abandonWork = true;

	private void ResetTransform()
	{
		if (initialized && (!(originalParentGO != null) || !originalParentGO.activeInHierarchy))
		{
			base.transform.SetParent(originalParentTransform);
			isAttachedToOverride = false;
			base.transform.localPosition = originalLocalPosition;
			base.transform.localRotation = originalLocalRotation;
			base.transform.localScale = originalScale;
			initialized = false;
		}
	}

	public void CrumbDisabled()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if (trackTransformOfParent)
			{
				ObjectHierarchyFlattenerManager.UnregisterOHF(this);
			}
			if (this != null)
			{
				Invoke("ResetTransform", 0f);
			}
		}
	}

	public void InvokeLateUpdate()
	{
		if (maintainRelativeScale)
		{
			base.transform.localScale = Vector3.Scale(originalParentTransform.lossyScale, originalScale);
		}
		base.transform.rotation = originalParentTransform.rotation * originalLocalRotation;
		base.transform.position = originalParentTransform.position + base.transform.rotation * calcOffset * (originalParentTransform.lossyScale.x / originalParentScale) * originalParentScale;
	}

	private void OnEnable()
	{
		abandonWork = false;
		GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
	}

	private void OnDisable()
	{
		abandonWork = true;
		ObjectHierarchyFlattenerManager.UnregisterOHF(this);
		if (base.enabled)
		{
			Invoke("ResetTransformIfStillDisabled", 0f);
		}
	}

	private void OnDestroy()
	{
		CancelInvoke();
	}

	private void ResetTransformIfStillDisabled()
	{
		if (!base.isActiveAndEnabled)
		{
			ResetTransform();
		}
	}

	public void SimpleWork()
	{
		if (initialized || abandonWork)
		{
			return;
		}
		if (trackTransformOfParent)
		{
			ObjectHierarchyFlattenerManager.RegisterOHF(this);
		}
		if (!isAttachedToOverride)
		{
			originalParentTransform = base.transform.parent;
			originalParentGO = originalParentTransform.gameObject;
			originalLocalPosition = base.transform.localPosition;
			originalLocalRotation = base.transform.localRotation;
			originalParentScale = base.transform.parent.lossyScale.x;
			originalScale = base.transform.localScale;
			calcOffset = Vector3.Scale(originalLocalPosition, originalScale);
			FlattenerCrumb flattenerCrumb = originalParentGO.GetComponent<FlattenerCrumb>();
			if (flattenerCrumb == null)
			{
				flattenerCrumb = originalParentGO.AddComponent<FlattenerCrumb>();
			}
			flattenerCrumb.AddFlattenerReference(this);
		}
		base.transform.SetParent((overrideParentTransform != null) ? overrideParentTransform : null);
		isAttachedToOverride = true;
		initialized = true;
	}
}
