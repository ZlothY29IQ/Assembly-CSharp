using UnityEngine;

public class CrittersStickyGoo : CrittersActor
{
	[Header("Sticky Goo")]
	public float range = 1f;

	public float slowModifier = 0.3f;

	public float slowDuration = 3f;

	public bool destroyOnApply = true;

	private bool readyToDisable;

	public override void Initialize()
	{
		base.Initialize();
		readyToDisable = false;
	}

	public bool CanAffect(Vector3 position)
	{
		return (base.transform.position - position).magnitude < range;
	}

	public void EffectApplied(CrittersPawn critter)
	{
		if (destroyOnApply)
		{
			readyToDisable = true;
		}
		CrittersManager.instance.TriggerEvent(CrittersManager.CritterEvent.StickyTriggered, actorId, critter.transform.position, Quaternion.LookRotation(critter.transform.up));
	}

	public override bool ProcessLocal()
	{
		bool result = base.ProcessLocal();
		if (readyToDisable)
		{
			base.gameObject.SetActive(value: false);
			return true;
		}
		return result;
	}
}
