using GorillaNetworking;
using UnityEngine;

public class CosmeticBoundaryTrigger : GorillaTriggerBox
{
	public VRRig rigRef;

	private static TimeSince sinceLastTryOnEvent = 0f;

	public void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody == null)
		{
			return;
		}
		rigRef = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (!(rigRef == null))
		{
			if (sinceLastTryOnEvent.HasElapsed(0.5f, resetOnElapsed: true))
			{
				GorillaTelemetry.PostShopEvent(rigRef, GTShopEventType.item_try_on, rigRef.tryOnSet.items);
			}
			rigRef.inTryOnRoom = true;
			rigRef.LocalUpdateCosmeticsWithTryon(rigRef.cosmeticSet, rigRef.tryOnSet, playfx: false);
			rigRef.myBodyDockPositions.RefreshTransferrableItems();
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (other.attachedRigidbody == null)
		{
			return;
		}
		rigRef = other.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (!(rigRef == null))
		{
			rigRef.inTryOnRoom = false;
			if (rigRef.isOfflineVRRig)
			{
				rigRef.tryOnSet.ClearSet(CosmeticsController.instance.nullItem);
				CosmeticsController.instance.ClearCheckout(sendEvent: false);
				CosmeticsController.instance.UpdateShoppingCart();
				CosmeticsController.instance.UpdateWornCosmetics(sync: true);
				CosmeticsController.ClearTryOnCollectable();
			}
			rigRef.LocalUpdateCosmeticsWithTryon(rigRef.cosmeticSet, rigRef.tryOnSet, playfx: false);
			rigRef.myBodyDockPositions.RefreshTransferrableItems();
		}
	}
}
