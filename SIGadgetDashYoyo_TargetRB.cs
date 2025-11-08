using UnityEngine;

public class SIGadgetDashYoyo_TargetRB : MonoBehaviour
{
	private const string preLog = "[SIGadgetDashYoyo_TargetRB]  ";

	private const string preErr = "[SIGadgetDashYoyo_TargetRB]  ERROR!!!  ";

	[SerializeField]
	private SIGadgetDashYoyo gadget;

	private readonly object[] _onHitPlayerRpcArgs = new object[2];

	protected void OnEnable()
	{
	}

	protected void OnTriggerEnter(Collider otherCollider)
	{
		if (!base.isActiveAndEnabled || !gadget.gameEntity.IsAuthority() || (gadget.gameEntity.heldByActorNumber == -1 && gadget.gameEntity.snappedByActorNumber == -1) || (!otherCollider.gameObject.IsOnLayer(UnityLayer.GorillaTagCollider) && !otherCollider.gameObject.IsOnLayer(UnityLayer.GorillaSlingshotCollider)) || ApplicationQuittingState.IsQuitting || !(GorillaGameManager.instance is SuperInfectionGame siTagGameManager))
		{
			return;
		}
		VRRig componentInParent = otherCollider.GetComponentInParent<VRRig>();
		if ((object)componentInParent != null)
		{
			NetPlayer creator = componentInParent.creator;
			if (creator != null && (object)SuperInfectionManager.GetSIManagerForZone(gadget.gameEntity.manager.zone) != null)
			{
				gadget.OnHitPlayer_Authority(siTagGameManager, creator);
			}
		}
	}
}
