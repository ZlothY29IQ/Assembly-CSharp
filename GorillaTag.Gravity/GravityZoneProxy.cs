using UnityEngine;

namespace GorillaTag.Gravity;

public class GravityZoneProxy : MonoBehaviour
{
	public enum ProxyBehaviour
	{
		None,
		EnterZone,
		ExitZone
	}

	[SerializeField]
	private BasicGravityZone zone;

	[SerializeField]
	private ProxyBehaviour OnEnter = ProxyBehaviour.EnterZone;

	[SerializeField]
	private ProxyBehaviour OnExit = ProxyBehaviour.ExitZone;

	[SerializeField]
	private float delay;

	private void OnTriggerEnter(Collider other)
	{
		DoBehaviour(OnEnter, other);
	}

	private void OnTriggerExit(Collider other)
	{
		DoBehaviour(OnExit, other);
	}

	private void DoBehaviour(ProxyBehaviour behaviour, Collider other)
	{
		if (!(zone == null))
		{
			(bool, MonkeGravityController) monkeGravityController = MonkeGravityManager.GetMonkeGravityController(other);
			if (monkeGravityController.Item1)
			{
				ApplyBehaviour(behaviour, monkeGravityController.Item2);
			}
		}
	}

	private void ApplyBehaviour(ProxyBehaviour behaviour, MonkeGravityController target)
	{
		if (!(zone == null))
		{
			switch (behaviour)
			{
			case ProxyBehaviour.EnterZone:
				zone.AddTarget(target, delay);
				break;
			case ProxyBehaviour.ExitZone:
				zone.RemoveTarget(target, delay);
				break;
			case ProxyBehaviour.None:
				break;
			}
		}
	}
}
