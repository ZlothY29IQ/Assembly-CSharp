using UnityEngine;

public class CrittersActorSettings : MonoBehaviour
{
	public CrittersActor parentActor;

	public bool usesRB;

	public bool canBeStored;

	public CapsuleCollider storeCollider;

	public CapsuleCollider equipmentStoreTriggerCollider;

	public virtual void OnEnable()
	{
		UpdateActorSettings();
	}

	public virtual void UpdateActorSettings()
	{
		parentActor.usesRB = usesRB;
		parentActor.rb.isKinematic = !usesRB;
		parentActor.equipmentStorable = canBeStored;
		parentActor.storeCollider = storeCollider;
		parentActor.equipmentStoreTriggerCollider = equipmentStoreTriggerCollider;
	}
}
