using System.Collections.Generic;
using UnityEngine;

public class CrittersBagSettings : CrittersActorSettings
{
	public Collider attachableCollider;

	public BoxCollider dropCube;

	public CrittersAttachPoint.AnchoredLocationTypes anchorLocation;

	public List<Collider> attachDisableColliders;

	public AudioClip attachSound;

	public AudioClip detachSound;

	public List<CrittersActor.CrittersActorType> blockAttachTypes;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersBag obj = (CrittersBag)parentActor;
		obj.attachableCollider = attachableCollider;
		obj.dropCube = dropCube;
		obj.anchorLocation = anchorLocation;
		obj.attachDisableColliders = attachDisableColliders;
		obj.attachSound = attachSound;
		obj.detachSound = detachSound;
		obj.blockAttachTypes = blockAttachTypes;
	}
}
