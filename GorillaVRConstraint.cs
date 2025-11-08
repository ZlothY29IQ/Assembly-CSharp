using UnityEngine;

public class GorillaVRConstraint : MonoBehaviourTick
{
	public bool isConstrained;

	public float angle = 3600f;

	public override void Tick()
	{
		if (NetworkSystem.Instance.WrongVersion)
		{
			isConstrained = true;
		}
		if (isConstrained && Time.realtimeSinceStartup > angle)
		{
			GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
		}
	}
}
