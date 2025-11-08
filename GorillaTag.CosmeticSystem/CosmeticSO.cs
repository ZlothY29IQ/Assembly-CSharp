using UnityEngine;

namespace GorillaTag.CosmeticSystem;

[CreateAssetMenu(fileName = "Untitled_CosmeticSO", menuName = "- Gorilla Tag/CosmeticSO", order = 0)]
public class CosmeticSO : ScriptableObject
{
	public CosmeticInfoV2 info = new CosmeticInfoV2("UNNAMED");

	public int propHuntWeight = 1;

	private bool ShowPropHuntWeight()
	{
		return true;
	}

	public void OnEnable()
	{
		info.debugCosmeticSOName = base.name;
	}
}
