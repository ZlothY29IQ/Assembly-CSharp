using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class DecorativeItem : TransferrableObject
{
	private enum DecorativeItemState
	{
		isHeld = 1,
		dropped = 2,
		snapped = 4,
		respawn = 8,
		none = 0x10
	}

	public DecorativeItemReliableState reliableState;

	public UnityAction<DecorativeItem> respawnItem;

	public LayerMask breakItemLayerMask;

	private Coroutine respawnTimer;

	private Transform parent;

	private float _respawnTimestamp;

	private bool isSnapped;

	private Vector3 currentPosition;

	[SerializeField]
	private AudioSource audioSource;

	public AudioClip snapAudio;

	public GameObject shatterVFX;

	private new DecorativeItemState previousItemState = DecorativeItemState.dropped;

	public override bool ShouldBeKinematic()
	{
		if (itemState != ItemStates.State2 && itemState != ItemStates.State4)
		{
			return base.ShouldBeKinematic();
		}
		return true;
	}

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		parent = base.transform.parent;
	}

	protected override void Start()
	{
		base.Start();
		itemState = ItemStates.State4;
		currentState = PositionState.Dropped;
	}

	private new void OnStateChanged()
	{
		switch (itemState)
		{
		case ItemStates.State2:
			SnapItem(reliableState.isSnapped, reliableState.snapPosition);
			break;
		case ItemStates.State3:
			Respawn(reliableState.respawnPosition, reliableState.respawnRotation);
			break;
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (InHand())
		{
			itemState = ItemStates.State0;
		}
		DecorativeItemState decorativeItemState = (DecorativeItemState)itemState;
		if (decorativeItemState != previousItemState)
		{
			OnStateChanged();
		}
		previousItemState = decorativeItemState;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (itemState == ItemStates.State4 && (bool)worldShareableInstance && worldShareableInstance.guard.isTrulyMine)
		{
			InvokeRespawn();
		}
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		base.OnGrab(pointGrabbed, grabbingHand);
		itemState = ItemStates.State0;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		itemState = ItemStates.State1;
		Reparent(null);
		return true;
	}

	private void SetWillTeleport()
	{
		worldShareableInstance.SetWillTeleport();
	}

	public void Respawn(Vector3 randPosition, Quaternion randRotation)
	{
		if (!InHand())
		{
			if ((bool)shatterVFX && ShouldPlayFX())
			{
				PlayVFX(shatterVFX);
			}
			itemState = ItemStates.State3;
			SetWillTeleport();
			Transform obj = base.transform;
			obj.position = randPosition;
			obj.rotation = randRotation;
			if ((bool)reliableState)
			{
				reliableState.respawnPosition = randPosition;
				reliableState.respawnRotation = randRotation;
			}
		}
	}

	private void PlayVFX(GameObject vfx)
	{
		ObjectPools.instance.Instantiate(vfx, base.transform.position);
	}

	private bool Reparent(Transform _transform)
	{
		if (!allowReparenting)
		{
			return false;
		}
		if ((bool)parent)
		{
			parent.SetParent(_transform);
			base.transform.SetParent(parent);
			return true;
		}
		return false;
	}

	public void SnapItem(bool snap, Vector3 attachPoint)
	{
		if (!reliableState)
		{
			return;
		}
		if (snap)
		{
			AttachPoint currentAttachPointByPosition = DecorativeItemsManager.Instance.getCurrentAttachPointByPosition(attachPoint);
			if (!currentAttachPointByPosition)
			{
				reliableState.isSnapped = false;
				reliableState.snapPosition = Vector3.zero;
				return;
			}
			Transform attachPoint2 = currentAttachPointByPosition.attachPoint;
			if (!Reparent(attachPoint2))
			{
				reliableState.isSnapped = false;
				reliableState.snapPosition = Vector3.zero;
				return;
			}
			itemState = ItemStates.State2;
			base.transform.parent.localPosition = Vector3.zero;
			base.transform.localPosition = Vector3.zero;
			reliableState.isSnapped = true;
			if ((bool)audioSource && (bool)snapAudio && ShouldPlayFX())
			{
				audioSource.GTPlayOneShot(snapAudio);
			}
			currentAttachPointByPosition.SetIsHook(isHooked: true);
		}
		else
		{
			Reparent(null);
			reliableState.isSnapped = false;
		}
		reliableState.snapPosition = attachPoint;
	}

	private void InvokeRespawn()
	{
		if (itemState != ItemStates.State2)
		{
			respawnItem?.Invoke(this);
		}
	}

	private bool ShouldPlayFX()
	{
		if (previousItemState == DecorativeItemState.isHeld || previousItemState == DecorativeItemState.dropped)
		{
			return true;
		}
		return false;
	}

	private void OnCollisionEnter(Collision other)
	{
		if ((int)breakItemLayerMask == ((int)breakItemLayerMask | (1 << other.gameObject.layer)))
		{
			InvokeRespawn();
		}
	}
}
