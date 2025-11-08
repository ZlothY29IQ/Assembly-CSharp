using UnityEngine;

[RequireComponent(typeof(PlayerColoredCosmetic))]
public class FXModifierPlayerColorSetter : FXModifier
{
	[SerializeField]
	private PlayerColoredCosmetic playerColoredCosmetic;

	public override void UpdateScale(float scale, Color color)
	{
		playerColoredCosmetic.UpdateColor(color);
	}
}
