using UnityEngine;

namespace CosmeticRoom.ItemScripts;

public class TrickTreatHoldable : TransferrableObject
{
	public MeshCollider candyCollider;

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if ((bool)candyCollider)
		{
			candyCollider.enabled = IsMyItem() && IsHeld();
		}
	}
}
