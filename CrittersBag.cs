using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersBag : CrittersActor
{
	public AudioSource audioSrc;

	public CrittersAttachPoint.AnchoredLocationTypes anchorLocation;

	public Collider attachableCollider;

	public BoxCollider dropCube;

	private Collider[] overlapColliders;

	public List<Collider> attachDisableColliders;

	public Dictionary<int, GameObject> attachedColliders;

	[Header("Child object attachment sounds")]
	public AudioClip attachSound;

	public AudioClip detachSound;

	[Header("Monke equip sounds")]
	public AudioClip equipSound;

	public AudioClip unequipSound;

	[Header("Attachment Blocking")]
	public List<CrittersActorType> blockAttachTypes;

	private bool isAttachedToPlayer;

	private bool attachedToLocalPlayer;

	protected override void Awake()
	{
		base.Awake();
		overlapColliders = new Collider[20];
		attachedColliders = new Dictionary<int, GameObject>();
		isAttachedToPlayer = false;
	}

	public override void OnHover(bool isLeft)
	{
		if (isAttachedToPlayer)
		{
			GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength / 4f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		}
		else
		{
			base.OnHover(isLeft);
		}
	}

	protected override void CleanupActor()
	{
		base.CleanupActor();
		for (int num = attachedColliders.Count - 1; num >= 0; num--)
		{
			attachedColliders[attachedColliders.ElementAt(num).Key].gameObject.Destroy();
		}
		attachedColliders.Clear();
	}

	protected override void GlobalGrabbedBy(CrittersActor grabbedBy)
	{
		base.GlobalGrabbedBy(grabbedBy);
		bool flag = attachedToLocalPlayer;
		if (grabbedBy.IsNotNull() && grabbedBy is CrittersAttachPoint crittersAttachPoint)
		{
			isAttachedToPlayer = true;
			attachedToLocalPlayer = crittersAttachPoint.rigPlayerId == PhotonNetwork.LocalPlayer.ActorNumber;
		}
		else
		{
			isAttachedToPlayer = false;
			attachedToLocalPlayer = false;
		}
		if (attachedToLocalPlayer != flag)
		{
			bool flag2 = attachedToLocalPlayer || flag;
			audioSrc.transform.localPosition = Vector3.zero;
			audioSrc.GTPlayOneShot(attachedToLocalPlayer ? equipSound : unequipSound, flag2 ? 1f : 0.5f);
		}
	}

	public override void GrabbedBy(CrittersActor grabbedBy, bool positionOverride = false, Quaternion localRotation = default(Quaternion), Vector3 localOffset = default(Vector3), bool disableGrabbing = false)
	{
		base.GrabbedBy(grabbedBy, positionOverride, localRotation, localOffset, disableGrabbing);
	}

	public override void Released(bool keepWorldPosition, Quaternion rotation = default(Quaternion), Vector3 position = default(Vector3), Vector3 impulse = default(Vector3), Vector3 impulseRotation = default(Vector3))
	{
		if (parentActorId >= 0)
		{
			AttemptRemoveStoredObjectCollider(parentActorId);
		}
		int num = Physics.OverlapBoxNonAlloc(dropCube.transform.position, dropCube.size / 2f, overlapColliders, dropCube.transform.rotation, CrittersManager.instance.objectLayers, QueryTriggerInteraction.Collide);
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				Rigidbody attachedRigidbody = overlapColliders[i].attachedRigidbody;
				if (attachedRigidbody == null)
				{
					continue;
				}
				CrittersAttachPoint component = attachedRigidbody.GetComponent<CrittersAttachPoint>();
				if (component == null || component.anchorLocation != anchorLocation || component.GetComponentInChildren<CrittersBag>() != null)
				{
					continue;
				}
				if (lastGrabbedPlayer == PhotonNetwork.LocalPlayer.ActorNumber && CrittersManager.instance.actorById.TryGetValue(parentActorId, out var value))
				{
					CrittersGrabber crittersGrabber = value as CrittersGrabber;
					if (crittersGrabber != null)
					{
						GorillaTagger.Instance.StartVibration(crittersGrabber.isLeft, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
					}
				}
				GrabbedBy(component, positionOverride: true);
				return;
			}
		}
		base.Released(keepWorldPosition, rotation, position, impulse, impulseRotation);
	}

	public void AddStoredObjectCollider(CrittersActor actor)
	{
		if (attachedColliders.ContainsKey(actor.actorId))
		{
			if (attachedColliders[actor.actorId].IsNull())
			{
				attachedColliders[actor.actorId] = CrittersManager.DuplicateCapsuleCollider(base.transform, actor.storeCollider).gameObject;
			}
		}
		else
		{
			attachedColliders.Add(actor.actorId, CrittersManager.DuplicateCapsuleCollider(base.transform, actor.storeCollider).gameObject);
		}
		audioSrc.transform.position = actor.transform.position;
		audioSrc.GTPlayOneShot(attachSound);
	}

	public void RemoveStoredObjectCollider(CrittersActor actor, bool playSound = true)
	{
		if (attachedColliders.TryGetValue(actor.actorId, out var value))
		{
			Object.Destroy(value);
			attachedColliders.Remove(actor.actorId);
		}
		if (playSound)
		{
			audioSrc.transform.position = actor.transform.position;
			audioSrc.GTPlayOneShot(detachSound);
		}
	}

	public bool IsActorValidStore(CrittersActor actor)
	{
		if (blockAttachTypes != null && blockAttachTypes.Contains(actor.crittersActorType))
		{
			return false;
		}
		return true;
	}
}
