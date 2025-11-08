using GorillaExtensions;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;

public class StickyHand : MonoBehaviour, ISpawnable
{
	[SerializeField]
	private MeshRenderer flatHand;

	[SerializeField]
	private MeshRenderer regularHand;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private GameObject stringParent;

	[SerializeField]
	private float surfaceOffsetDistance;

	[SerializeField]
	private float stringMaxAttachLength;

	[SerializeField]
	private float stringDetachLength;

	[SerializeField]
	private float stringTeleportLength;

	[SerializeField]
	private SoundBankPlayer thwackSound;

	[SerializeField]
	private SoundBankPlayer schlupSound;

	private VRRig myRig;

	private bool isLocal;

	private int stateBitIndex;

	private Vector3 defaultLocalPosition;

	bool ISpawnable.IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
		isLocal = rig.isLocal;
		flatHand.enabled = false;
		defaultLocalPosition = stringParent.transform.InverseTransformPoint(rb.transform.position);
		int num = ((CosmeticSelectedSide == ECosmeticSelectSide.Left) ? 1 : 2);
		stateBitIndex = VRRig.WearablePackedStatesBitWriteInfos[num].index;
	}

	void ISpawnable.OnDespawn()
	{
	}

	private void Update()
	{
		if (isLocal)
		{
			if (rb.isKinematic && (rb.transform.position - stringParent.transform.position).IsLongerThan(stringDetachLength))
			{
				Unstick();
			}
			else if (!rb.isKinematic && (rb.transform.position - stringParent.transform.position).IsLongerThan(stringTeleportLength))
			{
				rb.transform.position = stringParent.transform.TransformPoint(defaultLocalPosition);
			}
			myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, stateBitIndex, rb.isKinematic);
		}
		else if (GTBitOps.ReadBit(myRig.WearablePackedStates, stateBitIndex) != rb.isKinematic)
		{
			if (rb.isKinematic)
			{
				Unstick();
			}
			else
			{
				Stick();
			}
		}
	}

	private void Stick()
	{
		thwackSound.Play();
		flatHand.enabled = true;
		regularHand.enabled = false;
		rb.isKinematic = true;
	}

	private void Unstick()
	{
		schlupSound.Play();
		rb.isKinematic = false;
		flatHand.enabled = false;
		regularHand.enabled = true;
	}

	private void OnCollisionStay(Collision collision)
	{
		if (isLocal && !rb.isKinematic && !(rb.transform.position - stringParent.transform.position).IsLongerThan(stringMaxAttachLength))
		{
			Stick();
			Vector3 point = collision.contacts[0].point;
			Vector3 normal = collision.contacts[0].normal;
			rb.transform.rotation = Quaternion.LookRotation(normal, rb.transform.up);
			Vector3 vector = rb.transform.position - point;
			vector -= Vector3.Dot(vector, normal) * normal;
			rb.transform.position = point + vector + surfaceOffsetDistance * normal;
		}
	}
}
