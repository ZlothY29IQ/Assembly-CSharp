using GorillaLocomotion;
using UnityEngine;

public class SizeLayerChanger : MonoBehaviour
{
	public float maxScale;

	public float minScale;

	public bool isAssurance;

	public bool affectLayerA = true;

	public bool affectLayerB = true;

	public bool affectLayerC = true;

	public bool affectLayerD = true;

	[SerializeField]
	private bool applyOnTriggerEnter = true;

	[SerializeField]
	private bool applyOnTriggerExit;

	[SerializeField]
	private bool triggerWithBodyCollider;

	public int SizeLayerMask
	{
		get
		{
			int num = 0;
			if (affectLayerA)
			{
				num |= 1;
			}
			if (affectLayerB)
			{
				num |= 2;
			}
			if (affectLayerC)
			{
				num |= 4;
			}
			if (affectLayerD)
			{
				num |= 8;
			}
			return num;
		}
	}

	private void Awake()
	{
		minScale = Mathf.Max(minScale, 0.01f);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (!triggerWithBodyCollider && !other.GetComponent<SphereCollider>())
		{
			return;
		}
		VRRig vRRig;
		if (triggerWithBodyCollider)
		{
			if (other != GTPlayer.Instance.bodyCollider)
			{
				return;
			}
			vRRig = GorillaTagger.Instance.offlineVRRig;
		}
		else
		{
			vRRig = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		}
		if (!(vRRig == null) && applyOnTriggerEnter)
		{
			vRRig.sizeManager.currentSizeLayerMaskValue = SizeLayerMask;
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (!triggerWithBodyCollider && !other.GetComponent<SphereCollider>())
		{
			return;
		}
		VRRig vRRig;
		if (triggerWithBodyCollider)
		{
			if (other != GTPlayer.Instance.bodyCollider)
			{
				return;
			}
			vRRig = GorillaTagger.Instance.offlineVRRig;
		}
		else
		{
			vRRig = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		}
		if (!(vRRig == null) && applyOnTriggerExit)
		{
			vRRig.sizeManager.currentSizeLayerMaskValue = SizeLayerMask;
		}
	}
}
