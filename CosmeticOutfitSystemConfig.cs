using UnityEngine;

[CreateAssetMenu(fileName = "CosmeticOutfitSystemConfig", menuName = "Gorilla Tag/Cosmetics/OutfitSystem", order = 0)]
public class CosmeticOutfitSystemConfig : ScriptableObject
{
	public int nonSubscriberMaxOutfits = 5;

	public int subscriberMaxOutfits = 10;

	public string mothershipKey;

	public char outfitSeparator;

	public char itemSeparator;

	public string selectedOutfitPref;
}
