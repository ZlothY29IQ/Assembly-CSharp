using UnityEngine;

public class VirtualStumpRevivePlayer : MonoBehaviour
{
	[SerializeField]
	private GhostReactorManager ghostReactorManager;

	[SerializeField]
	private GRReviveStation defaultReviveStation;

	private void OnTriggerEnter(Collider collider)
	{
		Rigidbody attachedRigidbody = collider.attachedRigidbody;
		if (!(attachedRigidbody != null))
		{
			return;
		}
		VRRig component = attachedRigidbody.GetComponent<VRRig>();
		if (!(component != null))
		{
			return;
		}
		GRPlayer component2 = component.GetComponent<GRPlayer>();
		if (component2 != null && (component2.State != GRPlayer.GRPlayerState.Alive || component2.Hp < component2.MaxHp))
		{
			if (!NetworkSystem.Instance.InRoom && component == VRRig.LocalRig)
			{
				defaultReviveStation.RevivePlayer(component2);
			}
			if (ghostReactorManager.IsAuthority())
			{
				ghostReactorManager.RequestPlayerRevive(defaultReviveStation, component2);
			}
		}
	}
}
