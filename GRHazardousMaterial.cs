using GorillaLocomotion;
using UnityEngine;

public class GRHazardousMaterial : MonoBehaviour
{
	private GhostReactor reactor;

	public void Init(GhostReactor reactor)
	{
		this.reactor = reactor;
	}

	public void OnLocalPlayerOverlap()
	{
		GRPlayer component = VRRig.LocalRig.GetComponent<GRPlayer>();
		if (component != null && component.State == GRPlayer.GRPlayerState.Alive)
		{
			reactor.grManager.RequestPlayerStateChange(component, GRPlayer.GRPlayerState.Ghost);
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (collider == GTPlayer.Instance.headCollider || collider == GTPlayer.Instance.bodyCollider)
		{
			OnLocalPlayerOverlap();
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.collider == GTPlayer.Instance.headCollider || collision.collider == GTPlayer.Instance.bodyCollider)
		{
			OnLocalPlayerOverlap();
		}
	}
}
