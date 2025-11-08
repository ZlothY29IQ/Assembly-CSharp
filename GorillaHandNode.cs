using System;
using UnityEngine;

public class GorillaHandNode : MonoBehaviour
{
	public VRRig rig;

	public Collider collider;

	public Rigidbody rigidbody;

	[NonSerialized]
	[Space]
	public VRMapIndex vrIndex;

	[NonSerialized]
	public VRMapThumb vrThumb;

	[NonSerialized]
	public VRMapMiddle vrMiddle;

	[Space]
	public GorillaHandSocket attachedToSocket;

	[Space]
	[SerializeField]
	private bool _isLeftHand;

	[SerializeField]
	private bool _isRightHand;

	public bool ignoreSockets;

	public bool isGripping => PollGrip();

	public bool isLeftHand => _isLeftHand;

	public bool isRightHand => _isRightHand;

	private void Awake()
	{
		Setup();
	}

	private bool PollGrip()
	{
		if (rig == null)
		{
			return false;
		}
		bool num = PollThumb() >= 0.25f;
		bool flag = PollIndex() >= 0.25f;
		bool flag2 = PollMiddle() >= 0.25f;
		return num && flag && flag2;
	}

	private void Setup()
	{
		if (rig == null)
		{
			rig = GetComponentInParent<VRRig>();
		}
		if (rigidbody == null)
		{
			rigidbody = GetComponent<Rigidbody>();
		}
		if (collider == null)
		{
			collider = GetComponent<Collider>();
		}
		if ((bool)rig)
		{
			vrIndex = (_isLeftHand ? rig.leftIndex : rig.rightIndex);
			vrThumb = (_isLeftHand ? rig.leftThumb : rig.rightThumb);
			vrMiddle = (_isLeftHand ? rig.leftMiddle : rig.rightMiddle);
		}
		_isLeftHand = base.name.Contains("left", StringComparison.OrdinalIgnoreCase);
		_isRightHand = base.name.Contains("right", StringComparison.OrdinalIgnoreCase);
		int num = 0;
		num |= 0x400;
		num |= 0x200000;
		num |= 0x1000000;
		base.gameObject.SetTag(_isLeftHand ? UnityTag.GorillaHandLeft : UnityTag.GorillaHandRight);
		base.gameObject.SetLayer(UnityLayer.GorillaHand);
		rigidbody.includeLayers = num;
		rigidbody.excludeLayers = ~num;
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		rigidbody.constraints = RigidbodyConstraints.FreezeAll;
		collider.isTrigger = true;
		collider.includeLayers = num;
		collider.excludeLayers = ~num;
	}

	private void OnTriggerStay(Collider other)
	{
	}

	private float PollIndex()
	{
		return Mathf.Clamp01(vrIndex.calcT / 0.88f);
	}

	private float PollMiddle()
	{
		return vrIndex.calcT;
	}

	private float PollThumb()
	{
		return vrIndex.calcT;
	}
}
