using UnityEngine;

public class GRReviveMeter : MonoBehaviourTick
{
	[SerializeField]
	private GRReviveStation reviveStation;

	[SerializeField]
	private Transform meter;

	public void Awake()
	{
	}

	public override void Tick()
	{
		float value = 0f;
		if (reviveStation != null && VRRig.LocalRig.OwningNetPlayer != null && reviveStation.GetReviveCooldownSeconds() > 0.0)
		{
			value = (float)reviveStation.CalculateRemainingReviveCooldownSeconds(VRRig.LocalRig.OwningNetPlayer.ActorNumber) / (float)reviveStation.GetReviveCooldownSeconds();
		}
		value = Mathf.Clamp(value, 0f, 1f);
		value = 1f - value;
		meter.localScale = new Vector3(1f, value, 1f);
	}
}
